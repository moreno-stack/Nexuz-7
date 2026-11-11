using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace UnityEngine
{
    /// <summary>JsonUtility simplificado para las pruebas: lee campos simples del primer nivel y lanza con texto no válido.</summary>
    public static class JsonUtility
    {
        public static T FromJson<T>(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            string t = json.Trim();
            int depth = 0;
            foreach (char c in t) { if (c == '{') depth++; else if (c == '}') depth--; if (depth < 0) break; }
            if (t.Length < 2 || t[0] != '{' || depth != 0) throw new ArgumentException("JSON parse error: Invalid value.");
            var obj = (T)Activator.CreateInstance(typeof(T));
            foreach (Match m in Regex.Matches(t, "\"(\\w+)\"\\s*:\\s*(\"(?<s>[^\"]*)\"|(?<v>[-\\w.]+))"))
            {
                var f = typeof(T).GetField(m.Groups[1].Value, BindingFlags.Public | BindingFlags.Instance);
                if (f == null) continue;
                if (f.FieldType == typeof(string)) f.SetValue(obj, m.Groups["s"].Success ? m.Groups["s"].Value : m.Groups["v"].Value);
                else if (f.FieldType == typeof(int)) f.SetValue(obj, int.Parse(m.Groups["v"].Value));
                else if (f.FieldType == typeof(long)) f.SetValue(obj, long.Parse(m.Groups["v"].Value));
                else if (f.FieldType == typeof(float)) f.SetValue(obj, float.Parse(m.Groups["v"].Value, System.Globalization.CultureInfo.InvariantCulture));
                else if (f.FieldType == typeof(bool)) f.SetValue(obj, m.Groups["v"].Value == "true");
            }
            return obj;
        }
        public static string ToJson(object o) { return "{}"; }
        public static string ToJson(object o, bool pretty) { return "{}"; }
    }
}
