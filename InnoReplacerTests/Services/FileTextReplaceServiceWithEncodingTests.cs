using InnoReplacer.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace InnoReplacer.Services.Tests
{
    [TestClass]
    public class FileTextReplaceServiceWithEncodingTests
    {
        // ===== Helpers (既存テストと同一のユーティリティ) =====

        private static string NewTempPath()
        {
            return Path.Combine(Path.GetTempPath(), "EPR_" + Guid.NewGuid().ToString("N"));
        }

        private static string CreateFile(string content, Encoding enc)
        {
            var path = NewTempPath();
            File.WriteAllText(path, content, enc);
            return path;
        }

        private static byte[] ReadHead(string path, int n)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var buf = new byte[n];
                var r = fs.Read(buf, 0, n);
                if (r == n) return buf;
                var outBuf = new byte[r];
                Array.Copy(buf, outBuf, r);
                return outBuf;
            }
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        // ===== Tests =====

        [TestMethod]
        public void Force_Utf8_NoBom_Keeps_NoBom_And_Replaces()
        {
            // Arrange: UTF-8 (no BOM)
            var enc = new UTF8Encoding(false);
            var file = CreateFile("X__B__Y", enc);
            var svc = new FileTextReplaceServiceWithEncoding(enc, emitBom: false);

            // Act
            var code = svc.ReplaceInPlace(file, "__B__", "MID");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, new UTF8Encoding(false));
            Assert.AreEqual("XMIDY", text);

            var head = ReadHead(file, 3);
            Assert.IsFalse(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head)); // no BOM
        }

        [TestMethod]
        public void Force_Utf8_NoBom_Removes_BOM_If_InputHadBom()
        {
            // Arrange: 入力は UTF-8 (BOMあり) だが、出力は BOMなしを強制
            var file = CreateFile("A__X__B", new UTF8Encoding(true));
            var svc = new FileTextReplaceServiceWithEncoding(new UTF8Encoding(false), emitBom: false);

            // Act
            var code = svc.ReplaceInPlace(file, "__X__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, new UTF8Encoding(false));
            Assert.AreEqual("AOKB", text);

            var head = ReadHead(file, 3);
            Assert.IsFalse(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head)); // BOM が除去されている
        }

        [TestMethod]
        public void Force_Utf8_WithBom_Adds_BOM_If_InputHadNoBom()
        {
            // Arrange: 入力は UTF-8 (no BOM) だが、出力は BOMありを強制
            var file = CreateFile("__X__", new UTF8Encoding(false));
            var svc = new FileTextReplaceServiceWithEncoding(new UTF8Encoding(true), emitBom: true);

            // Act
            var code = svc.ReplaceInPlace(file, "__X__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, new UTF8Encoding(true));
            Assert.AreEqual("OK", text);

            var head = ReadHead(file, 3);
            Assert.IsTrue(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head)); // BOM が付与されている
        }

        [TestMethod]
        public void Force_SJIS_Preserves_SJIS_And_Replaces()
        {
            // Arrange: Shift-JIS で作成（日本語を含む）
            var sjis = Encoding.GetEncoding(932);
            var content = "あ__K__い";
            var file = CreateFile(content, sjis);
            var svc = new FileTextReplaceServiceWithEncoding(sjis);

            // Act
            var code = svc.ReplaceInPlace(file, "__K__", "漢");

            // Assert
            Assert.AreEqual(0, code);
            // SJIS で正しく読めること（エンコーディングが崩れていない）
            var text = File.ReadAllText(file, sjis);
            Assert.AreEqual("あ漢い", text);

            // SJIS は BOM を持たない
            var head = ReadHead(file, 3);
            Assert.IsFalse(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head));
        }

        [TestMethod]
        public void Force_Utf16LE_WithBom_Preserves_BOM_And_Replaces()
        {
            // Arrange: UTF-16 LE (BOMあり)
            var enc = Encoding.Unicode; // UTF-16 LE, BOMあり
            var file = CreateFile("__K__", enc);
            var svc = new FileTextReplaceServiceWithEncoding(enc, emitBom: true);

            // Act
            var code = svc.ReplaceInPlace(file, "__K__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, enc);
            Assert.AreEqual("OK", text);

            var head = ReadHead(file, 2);
            Assert.IsTrue(BytesEqual(new byte[] { 0xFF, 0xFE }, head)); // FF FE (LE)
        }

        [TestMethod]
        public void Force_Utf32LE_WithBom_Preserves_BOM_And_Replaces()
        {
            // Arrange: UTF-32 LE (BOMあり)
            var enc = new UTF32Encoding(false, true);
            var file = CreateFile("AA__R__", enc);
            var svc = new FileTextReplaceServiceWithEncoding(enc, emitBom: true);

            // Act
            var code = svc.ReplaceInPlace(file, "__R__", "BB");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, enc);
            Assert.AreEqual("AABB", text);

            var head = ReadHead(file, 4);
            Assert.IsTrue(BytesEqual(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, head)); // FF FE 00 00
        }

        [TestMethod]
        public void Force_Mode_FileNotFound_Returns2()
        {
            var enc = new UTF8Encoding(false);
            var svc = new FileTextReplaceServiceWithEncoding(enc);
            var code = svc.ReplaceInPlace(Path.Combine(Path.GetTempPath(), "no_such_file.tmp"), "A", "B");
            Assert.AreEqual(2, code);
        }

        [TestMethod]
        public void Force_Utf16BE_WithBom_Preserves_BOM_And_Replaces()
        {
            // Arrange: UTF-16 BE (BOMあり)
            var enc = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            var file = CreateFile("__K__", enc);
            var svc = new FileTextReplaceServiceWithEncoding(enc, emitBom: true);

            // Act
            var code = svc.ReplaceInPlace(file, "__K__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, enc);
            Assert.AreEqual("OK", text);

            var head = ReadHead(file, 2);
            // UTF-16BE BOM = FE FF
            Assert.IsTrue(BytesEqual(new byte[] { 0xFE, 0xFF }, head));
        }

        [TestMethod]
        public void Force_Utf16BE_NoBom_Removes_BOM_If_InputHadBom()
        {
            // Arrange: 入力は UTF-16 BE (BOMあり) だが、出力は BOMなしを強制
            var inputEnc = new UnicodeEncoding(bigEndian: true, byteOrderMark: true);
            var file = CreateFile("A__X__B", inputEnc);

            var outEnc = new UnicodeEncoding(bigEndian: true, byteOrderMark: false);
            var svc = new FileTextReplaceServiceWithEncoding(outEnc, emitBom: false);

            // Act
            var code = svc.ReplaceInPlace(file, "__X__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, outEnc);
            Assert.AreEqual("AOKB", text);

            var head = ReadHead(file, 2);
            // BOM が除去されていること（FE FF ではない）
            Assert.IsFalse(BytesEqual(new byte[] { 0xFE, 0xFF }, head));
        }

        [TestMethod]
        public void Force_Utf32BE_WithBom_Preserves_BOM_And_Replaces()
        {
            // Arrange: UTF-32 BE (BOMあり)
            var enc = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
            var file = CreateFile("AA__R__", enc);
            var svc = new FileTextReplaceServiceWithEncoding(enc, emitBom: true);

            // Act
            var code = svc.ReplaceInPlace(file, "__R__", "BB");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, enc);
            Assert.AreEqual("AABB", text);

            var head = ReadHead(file, 4);
            // UTF-32BE BOM = 00 00 FE FF
            Assert.IsTrue(BytesEqual(new byte[] { 0x00, 0x00, 0xFE, 0xFF }, head));
        }

        [TestMethod]
        public void Force_Utf32BE_NoBom_Removes_BOM_If_InputHadBom()
        {
            // Arrange: 入力は UTF-32 BE (BOMあり) だが、出力は BOMなしを強制
            var inputEnc = new UTF32Encoding(bigEndian: true, byteOrderMark: true);
            var file = CreateFile("__X__", inputEnc);

            var outEnc = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
            var svc = new FileTextReplaceServiceWithEncoding(outEnc, emitBom: false);

            // Act
            var code = svc.ReplaceInPlace(file, "__X__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, outEnc);
            Assert.AreEqual("OK", text);

            var head = ReadHead(file, 4);
            // BOM が除去されていること（00 00 FE FF ではない）
            Assert.IsFalse(BytesEqual(new byte[] { 0x00, 0x00, 0xFE, 0xFF }, head));
        }

    }
}