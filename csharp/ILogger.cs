using System;
using System.Diagnostics;

namespace DevPlatform.Base
{
    /// <summary>
    /// Interface for Logger.
    /// LoggerFactory에 등록하여 사용하기 위해서는 파라미터 없는 생성자 혹은 Caller 이름을 입력 받는 생성자를 포함해야 합니다.
    /// </summary>
    /// <remarks>
    /// Caller 이름을 입력 받는 생성자가 없을 경우, logging 시에 Caller 이름을 표시할 수 없습니다.
    /// </remarks>
    public interface ILogger
    {
        ///// <summary>
        ///// 기본 생성자
        ///// </summary>
        ///// <param name="callerName">Caller 이름</param>
        //ILogger(string callerName);

        /// <summary>
        /// Log를 출력합니다.
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="trace">Stack Trace 정보</param>
        /// <param name="args">Log 파라미터</param>
        void Log(LogLevelType logLevel, object logMessage, StackTrace trace = null, params object[] args);
    }
}
