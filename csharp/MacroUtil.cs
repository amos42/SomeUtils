/*
The MIT License (MIT)

Copyright (c) <year> <copyright holders>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

/*
MacroUtil.cs : Macro 치환 문자열 처리
Made by : Amos42
*/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace DevPlatform.Base
{
    /// <summary>
    /// IRunner 인터페이스
    /// </summary>
    public interface IMacroRunner
    {
        /// <summary>
        /// 매크로 전개 함수
        /// </summary>
        /// <param name="macroKey">매크로 전개 키값</param>
        /// <returns></returns>
        string Run(string macroKey);
    }

    /// <summary>
    /// 문자열이나 숫자 배열을 다양한 형태의 데이터로 변환하는 유틸리티
    /// </summary>
    public static class MacroUtil
    {
        /// <summary>
        /// 매크로 시작 식별자
        /// </summary>
        public const string DEFAULT_START_LITER = "${";
        /// <summary>
        /// 매크로 종료 식별자
        /// </summary>
        public const string DEFAULT_END_LITER = "}";
        /// <summary>
        /// 네임스페이스 구분자
        /// </summary>
        public const string DEFAULT_NAMESPACE_SEP = ".";
        /// <summary>
        /// 기본 버퍼 크기
        /// </summary>
        public const int BUFFER_SIZE = 1024 * 8;

        //private static readonly ILogger logger = LoggerFactory.GetLogger();

        /// <summary>
        /// Stream 입력을 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="inputStream">입력 스트림</param>
        /// <param name="outputStream">출력 스트림</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <param name="nameSpace">매크로명의 namespace명</param>
        /// <param name="nameSpaceSep">namespace의 구분자</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <param name="invalideStr">무효한 매크로일 경우 치환할 문자열</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static int RunMacro(Stream inputStream, Stream outputStream, string startLiter, string endLiter,
            string nameSpace, string nameSpaceSep, IMacroRunner runner, string invalideStr)
        {
            if (inputStream == null || outputStream == null) return 0;

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
                ILogger logger = LoggerFactory.GetLogger();
                logger?.Error(ex.Message, true);
                return 0;
            }

            return 1;
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <param name="nameSpace">매크로명의 namespace명</param>
        /// <param name="nameSpaceSep">namespace의 구분자</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <param name="invalideStr">무효한 매크로일 경우 치환할 문자열</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string RunMacro(string source, string startLiter, string endLiter,
            string nameSpace, string nameSpaceSep, IMacroRunner runner, string invalideStr)
        {
            if (String.IsNullOrEmpty(source) || startLiter == null || endLiter == null)
            {
                return source;
            }

            if (source.IndexOf(startLiter, StringComparison.Ordinal) < 0)
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
            var sb = new StringBuilder();

            int len = source.Length;
            int idx = 0;
            while (idx < len)
            {
                int idx2 = source.IndexOf(startLiter, idx, StringComparison.Ordinal);
                if (idx2 < 0)
                {
                    break;
                }

                if (idx < idx2)
                {
                    sb.Append(source.Substring(idx, idx2 - idx));
                }

                int idx3 = source.IndexOf(endLiter, idx2, StringComparison.Ordinal);
                if (idx3 >= 0)
                {
                    if (runner != null)
                    {
                        string label = source.Substring(idx2 + startLiterLen, idx3 - idx2 - startLiterLen);
                        if (!String.IsNullOrEmpty(nameSpace))
                        {
                            if (!label.StartsWith(nameSpace, StringComparison.Ordinal))
                            {
                                sb.Append(startLiter + label + endLiter);
                                label = null;
                            }
                        }
                        if (label != null)
                        {
                            string value = runner.Run(label);
                            if (value != null)
                            {
                                value = RunMacro(value, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
                                sb.Append(value);
                            }
                            else
                            {
                                if (invalideStr != null)
                                {
                                    sb.Append(invalideStr);
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
                sb.Append(source.Substring(idx));
            }

            return sb.ToString();
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string RunMacro(string source, string startLiter, string endLiter, IMacroRunner runner)
        {
            return RunMacro(source, startLiter, endLiter, null, null, runner, null);
        }

        /// <summary>
        /// 매크로 적용하는 IRunner 구현체
        /// </summary>
        private class MacroRunner : IMacroRunner
        {
            private readonly IDictionary<string, object> macros;

            public MacroRunner(IDictionary<string, object> macros)
            {
                this.macros = macros;
            }

            public string Run(string macroKey)
            {
                if (macros.TryGetValue(macroKey, out var value))
                {
                    if (value == null) return null;
                    return (value is string) ? value as string : value.ToString();
                }
                return null;
            }
        }

        /// <summary>
        /// 매크로 적용하는 IRunner 구현체
        /// </summary>
        private class MultiMacroRunner : IMacroRunner
        {
            private readonly IEnumerable<IDictionary<string, object>> macrosList;

            public MultiMacroRunner(IEnumerable<IDictionary<string, object>> macrosList)
            {
                this.macrosList = macrosList;
            }

            public string Run(string macroKey)
            {
                foreach (var macros in macrosList)
                {
                    if (macros != null && macros.TryGetValue(macroKey, out var value))
                    {
                        if (value == null) return null;
                        return (value is string) ? value as string : value.ToString();
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// 매크로 컬렉션이 NameValueCollection일 경우의 IRunner 구현체
        /// </summary>
        private class MacroRunner2 : IMacroRunner
        {
            private readonly NameValueCollection macros;

            public MacroRunner2(NameValueCollection macros)
            {
                this.macros = macros;
            }

            public string Run(string macroKey)
            {
                var value = macros.Get(macroKey);
                if (value != null && value is string)
                {
                    return value as string;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <param name="nameSpace">매크로명의 namespace명</param>
        /// <param name="nameSpaceSep">namespace의 구분자</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <param name="invalideStr">무효한 매크로일 경우 치환할 문자열</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, string startLiter, string endLiter,
                string nameSpace, string nameSpaceSep, IMacroRunner runner, string invalideStr)
        {
            return RunMacro(source, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
        }

        /// <summary>
        /// Stream 입력을 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="startLiter"></param>
        /// <param name="endLiter"></param>
        /// <param name="nameSpace"></param>
        /// <param name="nameSpaceSep"></param>
        /// <param name="runner"></param>
        /// <param name="invalideStr"></param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static int ProcessMacro(Stream inputStream, Stream outputStream, string startLiter, string endLiter,
                string nameSpace, string nameSpaceSep, IMacroRunner runner, string invalideStr)
        {
            return RunMacro(inputStream, outputStream, startLiter, endLiter, nameSpace, nameSpaceSep, runner, invalideStr);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, string startLiter, string endLiter, IMacroRunner runner)
        {
            return RunMacro(source, startLiter, endLiter, null, null, runner, null);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, IDictionary<string, object> macros, string startLiter, string endLiter)
        {
            return RunMacro(source, startLiter, endLiter, new MacroRunner(macros));
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, IEnumerable<IDictionary<string, object>> macrosList, string startLiter, string endLiter)
        {
            return RunMacro(source, startLiter, endLiter, new MultiMacroRunner(macrosList));
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <param name="startLiter">매크로 시작 식별 문자</param>
        /// <param name="endLiter">매크로 종료 식별 문자</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, NameValueCollection macros, string startLiter, string endLiter)
        {
            return RunMacro(source, startLiter, endLiter, new MacroRunner2(macros));
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="runner">매크로 치환 수행 함수</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, IMacroRunner runner)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, runner);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, IDictionary<string, object> macros)
        {
            return ProcessMacro(source, macros, DEFAULT_START_LITER, DEFAULT_END_LITER);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, IEnumerable<IDictionary<string, object>> macrosList)
        {
            return ProcessMacro(source, macrosList, DEFAULT_START_LITER, DEFAULT_END_LITER);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, NameValueCollection macros)
        {
            return ProcessMacro(source, macros, DEFAULT_START_LITER, DEFAULT_END_LITER);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="nameSpace"></param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, string nameSpace, IDictionary<string, object> macros)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner(macros), null);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="nameSpace"></param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, string nameSpace, IEnumerable<IDictionary<string, object>> macrosList)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MultiMacroRunner(macrosList), null);
        }

        /// <summary>
        /// 문자열을 입력 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="source">입력 문자열</param>
        /// <param name="nameSpace"></param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static string ProcessMacro(string source, string nameSpace, NameValueCollection macros)
        {
            return ProcessMacro(source, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner2(macros), null);
        }

        /// <summary>
        /// Stream 입력을 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="nameSpace"></param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static int ProcessMacro(Stream inputStream, Stream outputStream, string nameSpace, IDictionary<string, object> macros)
        {
            return ProcessMacro(inputStream, outputStream, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner(macros), null);
        }

        /// <summary>
        /// Stream 입력을 받아 매크로 프로세스를 진행합니다.
        /// </summary>
        /// <param name="inputStream"></param>
        /// <param name="outputStream"></param>
        /// <param name="nameSpace"></param>
        /// <param name="macros">매크로 사전</param>
        /// <returns>매크로 처리 결과 문자열</returns>
        public static int ProcessMacro(Stream inputStream, Stream outputStream, string nameSpace, NameValueCollection macros)
        {
            return ProcessMacro(inputStream, outputStream, DEFAULT_START_LITER, DEFAULT_END_LITER, nameSpace, null, new MacroRunner2(macros), null);
        }
    }

}
