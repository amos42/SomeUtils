/// Java like properties utility
/// 
/// https://docs.oracle.com/cd/E23095_01/Platform.93/ATGProgGuide/html/s0204propertiesfileformat01.html 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevPlatform.DevTools.CommonControls.Service
{
    public class ExtProperties : Dictionary<string, object>
    {
        //public bool InsertNewLineWhiteSpace { get; set; } = false;

        public bool SplitWithWhiteSpace { get; set; } = false;

        public static Dictionary<string, object> Load(string filename, Dictionary<string, object> properties = null)
        {
            if(!new FileInfo(filename).Exists)
            {
                return null;
            }

            var rows = File.ReadAllLines(filename);
            if(rows == null || !rows.Any())
            {
                return null;
            }

            if (properties == null)
            {
                properties = new Dictionary<string, object>();
            }

            int mode = 0;
            string name = null;
            //var valueData = new StringBuilder();
            var valueData = new List<String>();
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row)) continue;
                var trimRow = row.TrimStart();
                if (string.IsNullOrEmpty(trimRow)) continue;
                if (trimRow[0] == '#' || trimRow[0] == '!') continue;

                switch (mode)
                {
                    case 0:
                        {
                            var tokens = trimRow.Split('=');
                            name = tokens[0].TrimEnd();
                            var value = tokens[1].Trim();
                            if (value.EndsWith("\\"))
                            {
                                valueData.Add(value.Substring(0, value.Length - 1));
                                mode = 1;
                            }
                            else
                            {
                                if (value.StartsWith("\"") && value.EndsWith("\""))
                                {
                                    value = value.Substring(1, value.Length - 2);
                                }
                                properties.Add(name, value);
                            }
                        }
                        break;
                    case 1:
                        {
                            trimRow = trimRow.TrimEnd();
                            if (trimRow.EndsWith("\\"))
                            {
                                trimRow = trimRow.Substring(0, trimRow.Length - 1);
                                if (!String.IsNullOrEmpty(trimRow))
                                {
                                    valueData.Add(trimRow);
                                }
                            }
                            else
                            {
                                valueData.Add(trimRow);
                                var value = valueData.ToArray();
                                properties.Add(name, value);
                                valueData.Clear();
                                mode = 0;
                            }

                        }
                        break;
                }
            }
            if (mode == 1 && valueData.Count > 0)
            {
                properties.Add(name, valueData.ToArray());
            }

            return properties;
        }

        public static bool Save(Dictionary<string, object> properties, string path, bool splitWithWhiteSpace = false)
        {
            var sb = new StringBuilder();
            foreach(var prop in properties)
            {
                if (prop.Value is string)
                {
                    sb.AppendLine($"{prop.Key} = {prop.Value}");
                } 
                else {
                    var strLst = prop.Value as IEnumerable<string>;
                    if (strLst != null)
                    {
                        var name = $"{prop.Key} = ";
                        sb.Append(name);
                        var whitespace = new String(' ', name.Length);
                        int idx = 0;
                        foreach (var val in strLst)
                        {
                            if (idx > 0)
                            {
                                if (splitWithWhiteSpace) sb.Append(' ');
                                sb.AppendLine("\\");
                                sb.Append(whitespace);
                            }
                            sb.Append(val);
                            idx++;
                        }
                        sb.AppendLine();
                    }
                }
            }
            File.WriteAllText(path, sb.ToString());

            return true;
        }

        public bool Load(string filename)
        {
            return Load(filename, this) != null;
        }

        public bool Save(string path)
        {
            return Save(this, path, SplitWithWhiteSpace);
        }
    }
}
