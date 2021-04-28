using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DevPlatform.Base
{
    /// <summary>
    /// Helper class for IUserInterface interface
    /// </summary>
    public static class LoggerHelper
    {
        /// <summary>
        /// Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="trace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Log(this ILogger log, LogLevelType logLevel, object logMessage, StackTrace trace, params object[] args)
        {
            log?.Log(logLevel, logMessage, trace, args);
        }

        /// <summary>
        /// Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="trace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Log<T>(this ILogger log, LogLevelType logLevel, IFormatProvider formatProvider, string format, T value, StackTrace trace, params object[] args)
        {
            if (log != null && value is IFormattable)
            {
                if (formatProvider == null) formatProvider = CultureInfo.CurrentCulture;
                var msg = (value as IFormattable).ToString(format, formatProvider);
                Log(log, logLevel, msg, trace, args);
            }
        }

        /// <summary>
        /// Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Log(this ILogger log, LogLevelType logLevel, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            log.Log(logLevel, logMessage, trace, args);
        }

        /// <summary>
        /// Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Log<T>(this ILogger log, LogLevelType logLevel, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, logLevel, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Exception Log 메시지 문자열을 만듭니다.
        /// </summary>
        /// <param name="exception">대상 exception</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <returns>생성된 Log 메시지 문자열</returns>
        public static string BuildExceptionMessage(this Exception exception, bool isTrace = false)
        {
            var builder = new StringBuilder();

            while (exception != null)
            {
                builder.AppendLine(exception.Message);
                if (isTrace)
                {
                    builder.AppendLine("StackTrace=");
                    builder.AppendLine(exception.StackTrace ?? "None");
                }

                exception = exception.InnerException;
                if(exception != null)
                {
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Debug Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Debug(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Debug, logMessage, trace, trace, args);
        }

        /// <summary>
        /// Debug Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Debug<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Debug, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Error Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Error(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Error, logMessage, trace, args);
        }

        /// <summary>
        /// Error Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Error<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Error, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Fatal Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Fatal(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Fatal, logMessage, trace, args);
        }

        /// <summary>
        /// Fatal Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Fatal<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Fatal, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Info Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Info(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Info, logMessage, trace, args);
        }

        /// <summary>
        /// Info Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Info<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Info, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Trace Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Trace(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Trace, logMessage, trace, args);
        }

        /// <summary>
        /// Trace Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Trace<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Trace, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// Warn Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Warn(this ILogger log, object logMessage, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log(log, LogLevelType.Warn, logMessage, trace, args);
        }

        /// <summary>
        /// Warn Level의 Log를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="formatProvider">Log 출력 포맷 provider</param>
        /// <param name="format">Log 출력 포맷</param>
        /// <param name="value">Log 출력 값</param>
        /// <param name="isTrace">Stack Trace 정보 표시 여부</param>
        /// <param name="args">출력할 Log 파라미터</param>
        public static void Warn<T>(this ILogger log, IFormatProvider formatProvider, string format, T value, bool isTrace = false, params object[] args)
        {
            if(log == null) return;
            var trace = (isTrace) ? new StackTrace(1) : null;
            Log<T>(log, LogLevelType.Warn, formatProvider, format, value, trace, args);
        }

        /// <summary>
        /// ILogMessage 타입의 Log Message를 출력합니다.
        /// </summary>
        /// <param name="log">Logger 인스턴스</param>
        /// <param name="logMessage">LogMessage 인스턴스</param>
        public static void Log(this ILogger log, ILogMessage logMessage)
        {
            if (log == null || logMessage == null) return;
            log.Log(logMessage.LogLevel, logMessage.LogMessage, logMessage.TraceInfo, logMessage.LogParameters);
        }
    }
}
