using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DevPlatform.Base
{
    /// <summary>
    /// 간단한 ILogger 구현체
    /// </summary>
    public class SimpleLogger : LoggerBase
    {
        /// <inheritdoc/>
        public override string ModuleSignature => nameof(SimpleLogger);

        /// <summary>
        /// Log 출력 대상
        /// </summary>
        public enum OutputTargetType
        {
            /// <summary>
            /// Console 출력
            /// </summary>
            ConsoleOut,

            /// <summary>
            /// IDE Debug 출력
            /// </summary>
            DebugOut,

            /// <summary>
            /// IDE Trace 출력
            /// </summary>
            TraceOut,

            /// <summary>
            /// TextWriter로 출력
            /// </summary>
            TextWriterOut
        }

        /// <summary>
        /// Log 출력 대상
        /// </summary>
        [ModuleProperty()]
        public OutputTargetType OutputTarget { get; set; } = OutputTargetType.ConsoleOut;

        /// <summary>
        /// TextWriter 개체
        /// </summary>
        public TextWriter OutputTextWriter { get; set; }

        /// <summary>
        /// 쓰레드 동기화 개체
        /// </summary>
        private object lockObject = new object();

        /// <summary>
        /// caller 이름을 지정한 생성자
        /// </summary>
        /// <param name="callerName">Logger 생성한 caller의 이름</param>
        public SimpleLogger(string callerName) : base(callerName)
        {
        }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public SimpleLogger() : base(null)
        {
        }

        /// <inheritdoc/>
        protected override void LogOut(IEnumerable<IDictionary<string, object>> logMacrosList, params object[] args)
        {
            var message = GetFullLogMessage(logMacrosList, args);
            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            lock (lockObject)
            {
                switch (OutputTarget)
                {
                    case OutputTargetType.ConsoleOut:
                        Console.WriteLine(message);
                        break;
                    case OutputTargetType.DebugOut:
                        Debug.WriteLine(message);
                        break;
                    case OutputTargetType.TraceOut:
                        Trace.WriteLine(message);
                        break;
                    case OutputTargetType.TextWriterOut:
                        OutputTextWriter?.WriteLine(message);
                        break;
                }
            }
        }
    }
}
