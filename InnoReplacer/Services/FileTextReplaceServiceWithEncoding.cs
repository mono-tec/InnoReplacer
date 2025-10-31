using InnoReplacer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InnoReplacer.Services
{
    /// <summary>
    /// 文字コードを「推測せず」、指定の Encoding で置換を行うサービス。
    /// - 読み込み：指定エンコーディング（BOM自動は無効）
    /// - 書き込み：同じエンコーディングで書き戻し（BOM有無は emitBom で制御）
    /// 返り値: 0=OK, 2=FileNotFound, 9=その他エラー（既存実装に合わせる）
    /// </summary>
    public sealed class FileTextReplaceServiceWithEncoding : IFileTextReplaceService
    {
        private readonly Encoding _encoding;  // 書き戻し時に使用
        private readonly bool _emitBom;       // UTF系のBOM有無

        /// <param name="encoding">
        /// 例:
        ///   - Encoding.GetEncoding(932)     // Shift-JIS
        ///   - new UTF8Encoding(false)       // UTF-8 (no BOM)
        ///   - new UTF8Encoding(true)        // UTF-8 (with BOM)
        ///   - new UnicodeEncoding(false,true) // UTF-16LE (BOM付き)
        /// </param>
        /// <param name="emitBom">UTF系のBOMを出力するか（UTF-8/16/32のみ影響）</param>
        public FileTextReplaceServiceWithEncoding(Encoding encoding, bool emitBom = false)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            _emitBom = emitBom;
        }

        /// <summary>
        /// 単一ペアの置換。既存 IFileTextReplaceService と同シグネチャ。
        /// </summary>
        public int ReplaceInPlace(string path, string searchText, string replaceText)
        {
            if (!File.Exists(path)) return 2;

            try
            {
                var enc = NormalizeEncoding(_encoding, _emitBom);

                // detectEncodingFromByteOrderMarks:false で勝手な推測を抑止
                string text;
                using (var sr = new StreamReader(path, enc, detectEncodingFromByteOrderMarks: false))
                    text = sr.ReadToEnd();

                // ReplaceInPlace 内の ReadToEnd() 後に追加
                bool targetNoBom = _encoding is UTF8Encoding && !_emitBom
                                || _encoding.CodePage == 1200 && !_emitBom   // UTF-16 LE
                                || _encoding.CodePage == 1201 && !_emitBom   // UTF-16 BE
                                || _encoding.CodePage == 12000 && !_emitBom  // UTF-32 LE
                                || _encoding.CodePage == 12001 && !_emitBom; // UTF-32 BE

                if (targetNoBom && !string.IsNullOrEmpty(text) && text[0] == '\uFEFF')
                    text = text.Substring(1);

                text = text.Replace(searchText, replaceText);

                using (var sw = new StreamWriter(path, append: false, enc))
                    sw.Write(text);

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[ERROR] {ex.Message}");
                return 9;
            }
        }

        /// <summary>
        /// UTF系の BOM 有無を外から制御できるように正規化。
        /// 非UTF系（例: SJIS/CP932）はそのまま返す。
        /// </summary>
        private static Encoding NormalizeEncoding(Encoding enc, bool emitBom)
        {
            // UTF-8 は BOM 有無だけ指定し直す
            if (enc is UTF8Encoding)
                return new UTF8Encoding(emitBom);

            // CodePage でUTF-16/32のエンディアンを判定
            switch (enc.CodePage)
            {
                case 1200: // UTF-16 LE
                    return new UnicodeEncoding(bigEndian: false, byteOrderMark: emitBom);
                case 1201: // UTF-16 BE
                    return new UnicodeEncoding(bigEndian: true, byteOrderMark: emitBom);
                case 12000: // UTF-32 LE
                    return new UTF32Encoding(bigEndian: false, byteOrderMark: emitBom);
                case 12001: // UTF-32 BE
                    return new UTF32Encoding(bigEndian: true, byteOrderMark: emitBom);
                default:
                    // 例: Shift-JIS(932) などはそのまま返す
                    return enc;
            }
        }


        public int ReplaceInPlace(string filePath, IEnumerable<KeyValuePair<string, string>> pairs)
        {
            throw new NotImplementedException();
        }
    }
}
