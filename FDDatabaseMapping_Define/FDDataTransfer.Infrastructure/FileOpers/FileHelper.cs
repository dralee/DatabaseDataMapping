using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FDDataTransfer.Infrastructure.FileOpers
{
    public class FileHelper
    {
        public static bool IsFileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public static string ReadFile(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                string content = sr.ReadToEnd();
                sr.Dispose();
                return content;
            }
        }
    }
}
