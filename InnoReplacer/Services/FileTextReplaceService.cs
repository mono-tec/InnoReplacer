using InnoReplacer.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InnoReplacer.Services
{
    /// <summary>
    /// ファイルのテキストを「元のエンコーディング特性をできるだけ維持」しつつ置換するサービス。
    /// 仕様:
    /// - 先頭BOMで UTF-8 / UTF-16LE/BE / UTF-32LE/BE を判定。BOMなしなら OS 既定 (Encoding.Default)。
    /// - 読み込みは検出した Encoding を使いつつ、BOM 自動判定 (detectEncodingFromByteOrderMarks=true) で安全に行う。
    /// - 書き戻しは「検出した Encoding のまま」。UTF-8 でも BOM 付与/削除は行わない（= 入力の性質を尊重）。
    /// - 戻り値: 0=成功 / 2=ファイル無し / 9=その他エラー。
    /// </summary>
    public sealed class FileTextReplaceService : IFileTextReplaceService
    {
        /// <inheritdoc />
        public int ReplaceInPlace(string filePath, string searchText, string replaceText)
        {
            if (!File.Exists(filePath))
                return 2;

            try
            {
                bool hasBom;
                var enc = DetectEncoding(filePath, out hasBom);

                // --- 読み込み ---
                // detectEncodingFromByteOrderMarks=true により、UTF-32/UTF-16 の BOM を確実に解釈。
                // enc は DetectEncoding の結果を与える（BOMなしなら OS 既定にフォールバック）。
                string text;
                using (var sr = new StreamReader(filePath, enc, true))
                {
                    text = sr.ReadToEnd();
                }

                // --- 置換 ---
                var src = searchText ?? string.Empty;
                var dst = replaceText ?? string.Empty;
                var newText = text.Replace(src, dst);

                // --- 書き戻し ---
                // エンコーディングは検出結果 enc をそのまま使用（UTF-8 の BOM 有無もそのまま維持）。
                using (var sw = new StreamWriter(filePath, false, enc))
                {
                    sw.Write(newText);
                }

                return 0;
            }
            catch (Exception)
            {
                return 9;
            }
        }

        /// <inheritdoc />
        public int ReplaceInPlace(string filePath, IEnumerable<KeyValuePair<string, string>> pairs)
        {
            if (!File.Exists(filePath))
                return 2;

            try
            {
                bool hasBom;
                var enc = DetectEncoding(filePath, out hasBom);

                string text;
                using (var sr = new StreamReader(filePath, enc, true))
                {
                    text = sr.ReadToEnd();
                }

                if (pairs != null)
                {
                    foreach (var kv in pairs)
                    {
                        var k = kv.Key ?? string.Empty;
                        var v = kv.Value ?? string.Empty;
                        text = text.Replace(k, v);
                    }
                }

                using (var sw = new StreamWriter(filePath, false, enc))
                {
                    sw.Write(text);
                }

                return 0;
            }
            catch (Exception)
            {
                return 9;
            }
        }

        /// <summary>
        /// 先頭バイトから BOM を検出し、代表的な Unicode 符号化を判定する。
        /// 一致が無い場合は OS 既定 (日本語OSなら CP932=Shift_JIS 等) にフォールバックする。
        /// ※ 判定順序が重要: UTF-32 → UTF-16 → UTF-8 の順に確認する。
        ///   （UTF-32LE の BOM FF FE 00 00 は UTF-16LE の先頭 FF FE と先頭2バイトが重なるため）
        /// </summary>
        private Encoding DetectEncoding(string path, out bool hasBom)
        {
            hasBom = false;
            var head = new byte[4];
            int read;

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                read = fs.Read(head, 0, head.Length);
            }

            // --- UTF-32 を先に判定する（UTF-16 と先頭が被るため） ---

            // UTF-32 BE BOM: 00 00 FE FF
            if (read >= 4 && head[0] == 0x00 && head[1] == 0x00 && head[2] == 0xFE && head[3] == 0xFF)
            {
                hasBom = true;
                return new UTF32Encoding(true, true);  // BE + BOM
            }
            // UTF-32 LE BOM: FF FE 00 00
            if (read >= 4 && head[0] == 0xFF && head[1] == 0xFE && head[2] == 0x00 && head[3] == 0x00)
            {
                hasBom = true;
                return new UTF32Encoding(false, true); // LE + BOM
            }

            // --- 続いて UTF-16 を判定 ---

            // UTF-16 BE BOM: FE FF
            if (read >= 2 && head[0] == 0xFE && head[1] == 0xFF)
            {
                hasBom = true;
                return Encoding.BigEndianUnicode;      // UTF-16 BE (BOM付)
            }
            // UTF-16 LE BOM: FF FE
            if (read >= 2 && head[0] == 0xFF && head[1] == 0xFE)
            {
                hasBom = true;
                return Encoding.Unicode;               // UTF-16 LE (BOM付)
            }

            // --- 最後に UTF-8 BOM ---

            // UTF-8 BOM: EF BB BF
            if (read >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
            {
                hasBom = true;
                return new UTF8Encoding(true);         // UTF-8 (BOM付)
            }

            // BOM なし → OS 既定（例: Windows 日本語環境なら CP932）
            return Encoding.Default;
        }
    }
}
