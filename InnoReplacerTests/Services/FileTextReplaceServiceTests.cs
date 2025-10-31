using InnoReplacer.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EncodingPreservingReplacer.Tests
{
    [TestClass]
    public class FileTextReplaceServiceTests
    {
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


        [TestMethod]
        public void ReplaceInPlace_Utf8_BOM_Input_Keeps_BOM_And_Replaced()
        {
            // Arrange: UTF-8 (with BOM)
            var encIn = new UTF8Encoding(true);
            var file = CreateFile("X__B__Y", encIn);
            var svc = new FileTextReplaceService();

            // Act
            var code = svc.ReplaceInPlace(file, "__B__", "MID");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, new UTF8Encoding(true));
            Assert.AreEqual("XMIDY", text);

            var head = ReadHead(file, 3);
            Assert.IsTrue(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head));
        }

        [TestMethod]
        public void ReplaceInPlace_Utf16LE_Input_Preserves_Encoding_And_Replaced()
        {
            // Arrange: UTF-16 LE (BOM あり)
            var encIn = Encoding.Unicode; // UTF-16 LE BOM
            var file = CreateFile("__K__", encIn);
            var svc = new FileTextReplaceService();

            // Act
            var code = svc.ReplaceInPlace(file, "__K__", "OK");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, encIn);
            Assert.AreEqual("OK", text);

            var head = ReadHead(file, 2);
            Assert.IsTrue(BytesEqual(new byte[] { 0xFF, 0xFE }, head)); // FF FE
        }

        [TestMethod]
        public void ReplaceInPlace_Utf32LE_Input_Preserves_Encoding_And_Replaced()
        {
            // Arrange: UTF-32 LE (BOM あり)
            var encIn = new UTF32Encoding(false, true);
            var file = CreateFile("AA__R__", encIn);
            var svc = new FileTextReplaceService();

            // Act
            var code = svc.ReplaceInPlace(file, "__R__", "BB");

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, encIn);
            Assert.AreEqual("AABB", text);

            var head = ReadHead(file, 4);
            Assert.IsTrue(BytesEqual(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }, head)); // FF FE 00 00
        }

        [TestMethod]
        public void ReplaceInPlace_MultiplePairs_AllApplied_And_Utf8_NoBom_Remains_NoBom()
        {
            // Arrange: UTF-8 (no BOM)
            var encIn = new UTF8Encoding(false);
            var file = CreateFile("A __X__ and __Y__", encIn);
            var svc = new FileTextReplaceService();

            var pairs = new List<KeyValuePair<string, string>>();
            pairs.Add(new KeyValuePair<string, string>("__X__", "X"));
            pairs.Add(new KeyValuePair<string, string>("__Y__", "Y"));

            // Act
            var code = svc.ReplaceInPlace(file, pairs);

            // Assert
            Assert.AreEqual(0, code);
            var text = File.ReadAllText(file, new UTF8Encoding(false));
            Assert.AreEqual("A X and Y", text);

            // BOM が付与されていないこと
            var head = ReadHead(file, 3);
            Assert.IsFalse(BytesEqual(new byte[] { 0xEF, 0xBB, 0xBF }, head));
        }

        [TestMethod]
        public void ReplaceInPlace_FileNotFound_Returns2()
        {
            var svc = new FileTextReplaceService();
            var code = svc.ReplaceInPlace(Path.Combine(Path.GetTempPath(), "no_such_file.tmp"), "A", "B");
            Assert.AreEqual(2, code);
        }
    }
}
