using InnoReplacer.Services;
using InnoReplacer.Services.Interfaces;
using System;
using System.Text;

namespace InnoReplacer
{
    internal class Program
    {
        internal static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: InnoReplacer.exe <filepath> <searchText> <replaceText> [encoding]");
                Console.Error.WriteLine("Example: InnoReplacer.exe sample.sql #DB# appdb utf8");
                return 1;
            }

            string filePath = args[0];
            string searchText = args[1];
            string replaceText = args[2];

            // ---- エンコーディングオプション判定 ----
            IFileTextReplaceService svc;
            if (args.Length >= 4)
            {
                string encodingOption = args[3].Trim().ToLower();
                var encoding = ParseEncoding(encodingOption);
                bool emitBom = encodingOption.Contains("bom");
                svc = new FileTextReplaceServiceWithEncoding(encoding, emitBom);

                Console.WriteLine($"[INFO] Forcing encoding: {encoding.WebName} (BOM: {emitBom})");
            }
            else
            {
                svc = new FileTextReplaceService();
                Console.WriteLine("[INFO] Auto-detect mode (preserve encoding)");
            }

            // ---- 置換実行 ----
            var code = svc.ReplaceInPlace(filePath, searchText, replaceText);

            if (code == 2)
            {
                Console.Error.WriteLine($"[ERROR] File not found: {filePath}");
            }
            else if (code != 0 && code != 9)
            {
                code = 9; // 念のため集約
            }

            return code;
        }

        // ---- 文字コード指定のパース ----
        private static Encoding ParseEncoding(string name)
        {
            switch (name)
            {
                case "sjis":
                case "shift-jis":
                    return Encoding.GetEncoding(932);
                case "utf8":
                    return new UTF8Encoding(false);
                case "utf8bom":
                    return new UTF8Encoding(true);
                case "utf16":
                    return new UnicodeEncoding(false, true);
                case "utf16be":
                    return new UnicodeEncoding(true, true);
                default:
                    throw new ArgumentException($"Unsupported encoding option: {name}");
            }
        }
    }

}
