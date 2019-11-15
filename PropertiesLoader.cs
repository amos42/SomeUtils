/* https://docs.oracle.com/cd/E23095_01/Platform.93/ATGProgGuide/html/s0204propertiesfileformat01.html */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PropertiesUtil
{
    public class PropertiesLoader
    {
        public bool InsertNewLineWhiteSpace { get; set; } = false;

        public Dictionary<string, string> Load(string filename)
        {
            var rows = File.ReadAllLines(filename);

            var properties = new Dictionary<string, string>();

            int mode = 0;
            string name = null;
            var valueData = new StringBuilder();
            foreach (var row in rows)
            {
                var trimRow = row.TrimStart();
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
                                valueData.Append(value.Substring(0, value.Length - 1));
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
                            if (valueData.Length > 0 && InsertNewLineWhiteSpace)
                            {
                                valueData.Append(" ");
                            }
                            trimRow = trimRow.TrimEnd();
                            if (trimRow.EndsWith("\\"))
                            {
                                valueData.Append(trimRow.Substring(0, trimRow.Length - 1));
                            }
                            else
                            {
                                valueData.Append(trimRow);
                                var value = valueData.ToString();
                                if (value.StartsWith("\"") && value.EndsWith("\""))
                                {
                                    value = value.Substring(1, value.Length - 2);
                                }
                                properties.Add(name, value);
                                valueData.Clear();
                                mode = 0;
                            }

                        }
                        break;
                }
            }
            if (mode == 1 && valueData.Length > 0)
            {
                properties.Add(name, valueData.ToString());
            }

            return properties;
        }
    }
}
