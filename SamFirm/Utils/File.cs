using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm.Utils
{
    internal static class File
    {
        public static long FileSize { get; set; } = 0;
        private static byte[] KEY;

        public static void UnzipFromStream(Stream zipStream, string outFolder)
        {
            using (var zipInputStream = new ZipInputStream(zipStream))
            {
                while (zipInputStream.GetNextEntry() is ZipEntry zipEntry)
                {
                    var entryFileName = zipEntry.Name;

                    // 4K is optimum
                    var buffer = new byte[4096];

                    // Manipulate the output filename here as desired.
                    var fullZipToPath = Path.Combine(outFolder, entryFileName);
                    var directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    // Skip directory entry
                    if (Path.GetFileName(fullZipToPath).Length == 0)
                    {
                        continue;
                    }

                    Console.WriteLine(entryFileName);

                    // Unzip file in buffered chunks. This is just as fast as unpacking
                    // to a buffer the full size of the file, but does not waste memory.
                    // The "using" will close the stream even if an exception occurs.
                    using (FileStream streamWriter = System.IO.File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipInputStream, streamWriter, buffer);
                    }
                }
            }
        }

        public static int HandleEncryptedFile(Stream stream1, string outputDir)
        {
            using (RijndaelManaged managed = new RijndaelManaged())
            {
                managed.Mode = CipherMode.ECB;
                managed.BlockSize = 0x80;
                managed.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform transform = managed.CreateDecryptor(KEY, null))
                {
                    try
                    {
                        using (CryptoStream stream3 = new CryptoStream(stream1, transform, CryptoStreamMode.Read))
                        {
                            UnzipFromStream(stream3, outputDir);
                        }
                    }
                    catch (CryptographicException)
                    {
                        Console.WriteLine("Error DecryptFile(): Wrong key.");
                        return -1;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error DecryptFile() -> " + e);
                        return -1;
                    }
                }
            }
            return 0;
        }

        public static void SetDecryptKey(string version, string LogicValue)
        {
            string logicCheck = Auth.GetLogicCheck(version, LogicValue);
            byte[] bytes = Encoding.ASCII.GetBytes(logicCheck);
            using (MD5 md = MD5.Create())
            {
                KEY = md.ComputeHash(bytes);
            }
        }
    }
}
