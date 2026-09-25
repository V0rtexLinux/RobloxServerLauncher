using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;

namespace RobloxPlayerLauncher
{
    /// <summary>Small helpers over JavaScriptSerializer (part of .NET 4, nothing to ship).</summary>
    public static class Json
    {
        public static Dictionary<string, object> Parse(string text)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            var value = serializer.DeserializeObject(text ?? "") as Dictionary<string, object>;
            if (value == null)
            {
                throw new FormatException("The website did not answer with a JSON object.");
            }
            return value;
        }

        public static string Str(this Dictionary<string, object> json, string key)
        {
            object value;
            return json.TryGetValue(key, out value) && value != null ? Convert.ToString(value, CultureInfo.InvariantCulture) : null;
        }

        public static long Long(this Dictionary<string, object> json, string key)
        {
            long number;
            return long.TryParse(json.Str(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out number) ? number : 0;
        }

        public static int Int(this Dictionary<string, object> json, string key)
        {
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, json.Long(key)));
        }

        public static bool Bool(this Dictionary<string, object> json, string key)
        {
            object value;
            return json.TryGetValue(key, out value) && value is bool && (bool)value;
        }

        public static List<Dictionary<string, object>> Objects(this Dictionary<string, object> json, string key)
        {
            var list = new List<Dictionary<string, object>>();
            object value;
            var items = json.TryGetValue(key, out value) ? value as IEnumerable : null;
            if (items != null && !(items is string))
            {
                foreach (object item in items)
                {
                    var obj = item as Dictionary<string, object>;
                    if (obj != null)
                    {
                        list.Add(obj);
                    }
                }
            }
            return list;
        }
    }
}
