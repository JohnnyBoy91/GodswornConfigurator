using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using static JCGodSwornConfigurator.Plugin.ModManager;

namespace JCGodSwornConfigurator
{
    //helper functions
    internal class Utilities
    {
        private static readonly StringBuilder sb = new StringBuilder();
        public static string CombineStrings(params string[] strings)
        {
            sb.Clear();
            foreach (string s in strings)
            {
                sb.Append(s);
            }
            return sb.ToString();
        }

        public static float GetFloatByKey(float originalFloat, string key)
        {
            if (float.TryParse(GetValue(key), out float outVal))
            {
                return outVal;
            }
            else
            {
                if (VerboseLogging()) Log(CombineStrings("Failed to parse Float: ", key, dlmKey, originalFloat.ToString()));
                return originalFloat;
            }
        }

        public static int GetIntByKey(int originalInt, string key)
        {
            if (int.TryParse(GetValue(key), out int outVal))
            {
                return outVal;
            }
            else
            {
                if (VerboseLogging()) Log(CombineStrings("Failed to parse Int: ", key, dlmKey, originalInt.ToString()));
                return originalInt;
            }
        }

        public static bool GetBoolByKey(bool originalBool, string key)
        {
            if (bool.TryParse(GetValue(key), out bool boolVal))
            {
                return boolVal;
            }
            else
            {
                if (VerboseLogging()) Log(CombineStrings("Failed to parse Bool: ", key, dlmKey, originalBool.ToString()));
                return originalBool;
            }
        }

        public static void TryMethod(Action method)
        {
            try
            {
                method();
            }
            catch (Exception ex)
            {
                Log(CombineStrings("Failure in ", method.Method.Name, ": ", ex.Message), 3);
            }
        }

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

        public static void WriteConfig(string fileName, string text)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            StreamWriter writer = new StreamWriter(fileName, true);
            writer.Write(text);
            writer.Close();
        }

        public static void WriteJsonConfig(string filePath, object dataToSerialize)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            string jsonString = JsonSerializer.Serialize(dataToSerialize, options);
            WriteConfig(filePath, jsonString);
        }

        public static object ReadJsonConfig<T>(string filePath)
        {
            StreamReader reader = new StreamReader(filePath, true);
            string jsonString = reader.ReadToEnd();
            reader.Close();

            var optionsRead = new JsonSerializerOptions { WriteIndented = true, IncludeFields = true };
            T dataToDeserializeInto = JsonSerializer.Deserialize<T> (jsonString, optionsRead);
            return dataToDeserializeInto;
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
