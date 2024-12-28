using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JCGodSwornConfigurator
{
    internal class Utilities
    {

        /// <summary>
        /// Write to text file
        /// </summary>
        public static void WriteConfig(string fileName, List<string> text)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            StreamWriter writer = new StreamWriter(fileName, true);
            for (int i = 0; i < text.Count; i++)
            {
                writer.Write('\n' + text[i]);
            }
            writer.Close();
        }

        /// <summary>
        /// "2 Wood" -> "Wood"
        /// </summary>
        public static string GetSanitizedResourceName(string inputString)
        {
            if (inputString.ToLower().Contains("food")) return "Food";
            if (inputString.ToLower().Contains("wood")) return "Wood";
            if (inputString.ToLower().Contains("faith")) return "Faith";
            if (inputString.ToLower().Contains("wealth")) return "Wealth";
            return inputString;
        }
    }
}
