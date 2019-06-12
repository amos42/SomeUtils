/*
MacroUtil.cs : Macro 치환 문자열 처리
Made by : gyeongmin.ju
Created : 2019-04-24 오후 1:08:08
Last Update : 2019-04-24 오후 1:08:08
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Synergy.Base
{
    /// <summary>
    /// 문자열이나 숫자 배열을 다양한 형태의 데이터로 변환하는 유틸리티
    /// </summary>
    public static class MacroUtil
    {
        public const string DEFAULT_START_LITER = "${";
        public const string DEFAULT_END_LITER = "}";
        public const string DEFAULT_NAMESPACE_SEP = ".";
        public const int BUFFER_SIZE = 1024 * 8;

        private static readonly ILogger logger = Logger.GetLogger();

        public interface IRunner
        {
            string Run(string macroKey);
        }

        public static int RunMacro(Stream inputStream, Stream outputStream, string startLiter, string endLiter, 
            string nameSpace, string nameSpaceSep, IRunner runner, string invalideStr)
        {
            try
            {
                /*
                byte[] buffer = new byte[BUFFER_SIZE];
                int len = 0;
                while ((len = inputStream.Read(buffer)) != -1)
                {
                    string tempStr = new string(buffer, 0, len);
                    tempStr = RunMacro(tempStr, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
                    outputStream.Write(tempStr);
                }
                */

                StreamReader reader = new StreamReader(inputStream);
                string text = reader.ReadToEnd();
                reader.Close();

                text = RunMacro(text, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
                byte[] byteArray = Encoding.ASCII.GetBytes(text);
                outputStream.Write(byteArray, 0, byteArray.Length);
            }
            catch (IOException ex)
            {
                logger.Error(ex.Message);
                return 0;
            }

            return 1;
        }

        public static string RunMacro(string source, string startLiter, string endLiter, 
            string nameSpace, string nameSpaceSep, IRunner runner, string invalideStr)
        {
            if (String.IsNullOrEmpty(source))
            {
                return source;
            }

            if (source.IndexOf(startLiter) < 0)
            {
                return source;
            }

            int startLiterLen = startLiter.Length;
            int endLiterLen = endLiter.Length;
            if (!String.IsNullOrEmpty(nameSpace))
            {
                if (!String.IsNullOrEmpty(nameSpaceSep))
                {
                    nameSpace += nameSpaceSep;
                }
                else
                {
                    nameSpace += DEFAULT_NAMESPACE_SEP;
                }
            }
            else
            {
                nameSpace = null;
            }
            string str = "";

            int len = source.Length;
            int idx = 0;
            while (idx < len)
            {
                int idx2 = source.IndexOf(startLiter, idx);
                if (idx2 < 0)
                {
                    break;
                }

                if (idx < idx2)
                {
                    str += source.Substring(idx, idx2 - idx);
                }

                int idx3 = source.IndexOf(endLiter, idx2);
                if (idx3 >= 0)
                {
                    if (runner != null)
                    {
                        string label = source.Substring(idx2 + startLiterLen, idx3 - idx2 - startLiterLen);
                        if (!String.IsNullOrEmpty(nameSpace))
                        {
                            if (!label.StartsWith(nameSpace))
                            {
                                str += startLiter + label + endLiter;
                                label = null;
                            }
                        }
                        if (label != null)
                        {
                            string value = runner.Run(label);
                            if (value != null)
                            {
                                value = RunMacro(value, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
                                str += value;
                            }
                            else
                            {
                                if (invalideStr != null)
                                {
                                    str += invalideStr;
                                }
                            }
                        }
                    }

                    idx = idx3 + endLiterLen;
                }
                else
                {
                    break;
                }
            }
            if (idx < len)
            {
                str += source.Substring(idx);
            }

            return str;
        }

        public static string RunMacro(string source, string startLiter, string endLiter, IRunner runner)
        {
            return RunMacro(source, startLiter, endLiter, null, null, runner, null);
        }

        private class MacroRunner : IRunner
        {
            private IDictionary<string, object> macros;

            public MacroRunner(IDictionary<string, object> macros)
            {
                this.macros = macros;
            }

            public string Run(string macroKey)
            {
                if(macros.TryGetValue(macroKey, out var value))
                {
                    return value as string;
                } else
                {
                    return null;
                }
            }
        }

        public static string ProcessMacro(string source, string startLiter, string endLiter,
                string nameSpace, string nameSpaceSep, IRunner runner, string invalideStr)
        {
            return RunMacro(source, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
        }

        public static int ProcessMacro(Stream inputStream, Stream outputStream, string startLiter, string endLiter,
                string nameSpace, string nameSpaceSep, IRunner runner, string invalideStr)
        {
            return RunMacro(inputStream, outputStream, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
        }

        public static string ProcessMacro(string source, string startLiter, string endLiter, IRunner runner)
        {
            return RunMacro(source, startLiter, endLiter, null, null, runner, null);
        }

        public static string ProcessMacro(string source, IDictionary<string, object> macros, string startLiter, string endLiter)
        {
            return RunMacro(source, startLiter, endLiter, new MacroRunner(macros));
        }

        public static string ProcessMacro(string source, IRunner runner)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, runner);
        }

        public static string ProcessMacro(string source, IDictionary<string, object> macros)
        {
            return ProcessMacro(source, macros, DEFAULT_START_LITER, DEFAULT_END_LITER);
        }

        public static string ProcessMacro(string source, string nameSpace, IDictionary<string, object> macros)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner(macros), null);
        }

        public static int ProcessMacro(Stream inputStream, Stream outputStream, string nameSpace, IDictionary<string, object> macros)
        {
            return ProcessMacro(inputStream, outputStream, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner(macros), null);
        }
    }

}
