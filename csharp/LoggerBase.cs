using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace DevPlatform.Base
{
    using ObjectDictionary = IDictionary<string, object>;

    /// <summary>
    /// ILogger abstract 구현체
    /// </summary>
    public abstract class LoggerBase : ModuleBase, ILogger
    {
        #region public const values
        /// <summary>
        /// 기본 제공 log 매크로명 - 로그 호출자
        /// </summary>
        public const string LogMacroCaller = "log.caller";
        /// <summary>
        /// 기본 제공 log 매크로명 - 구분 문자
        /// </summary>
        public const string LogMacroSeparator = "log.sep";
        /// <summary>
        /// 기본 제공 log 매크로명 - 구분 문자 (이전 버전과의 호환성을 위해)
        /// </summary>
        public const string LogMacroSeparator2 = "log.separator";
        /// <summary>
        /// 기본 제공 log 매크로명 - 메시지 prefix
        /// </summary>
        public const string LogMacroPrefix = "log.prefix";
        /// <summary>
        /// 기본 제공 log 매크로명 - Log 레벨
        /// </summary>
        public const string LogMacroLogLevel = "log.level";
        /// <summary>
        /// 기본 제공 log 매크로명 - DateTime 타입의 날자/시간
        /// </summary>
        public const string LogMacroRawTime = "log.rawtime";
        /// <summary>
        /// 기본 제공 log 매크로명 - 날자
        /// </summary>
        public const string LogMacroDate = "log.date";
        /// <summary>
        /// 기본 제공 log 매크로명 - 시간
        /// </summary>
        public const string LogMacroTime = "log.time";
        /// <summary>
        /// 기본 제공 log 매크로명 - 날자/시간
        /// </summary>
        public const string LogMacroDateTime = "log.datetime";
        /// <summary>
        /// 기본 제공 log 매크로명 - Log 메시지
        /// </summary>
        public const string LogMacroMessage = "log.message";
        /// <summary>
        /// 기본 제공 log 매크로명 - Log 발생 시점의 스택 정보
        /// </summary>
        public const string LogMacroStackTrace = "log.stacktrace";
        /// <summary>
        /// 기본 제공 log 매크로명 - NewLine 문자
        /// </summary>
        public const string LogMacroNewLine = "log.newline";
        /// <summary>
        /// 기본 제공 log 매크로명 - 기본 디렉토리
        /// </summary>
        public const string LogMacroCurrentDirectory = "log.curdir";
        #endregion

        #region private const values
        private const string DefaultSeparator = "|";                                // Log 메시지 기본 구분 문자
        private const string DefaultDateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";     // 기본 날자/시간 포맷
        private const string DefaultDateFormat = "yyyy-MM-dd";                      // 기본 날자 포맷
        private const string DefaultTimeFormat = "HH:mm:ss.fff";                    // 기본 시간 포맷
        private const string DefaultMessageFormat = "${log.datetime}${log.sep}${log.level}${log.sep}${log.caller}${log.sep}${log.prefix}${log.message}"; // 기본 메시지 포맷
        #endregion

        #region private static values
        private static readonly ObjectDictionary BaseEnvironmentMacros = new Dictionary<string, object>()
        {
            { LogMacroNewLine, Environment.NewLine },
            { LogMacroCurrentDirectory, Environment.CurrentDirectory }
        };
        #endregion

        private SystemProperties properties = null;

        /// <summary>
        /// Logger 호출자 이름
        /// </summary>
        public virtual string CallerName => properties?.GetProperty<string>(LogMacroCaller);

        /// <summary>
        /// 로그 문자열 간 구분자
        /// </summary>
        [ModuleProperty()]
        public virtual string Separator 
        { 
            get => properties?.GetProperty<string>(LogMacroSeparator); 
            set {
                properties?.SetProperty<string>(LogMacroSeparator, value);
                properties?.SetProperty<string>(LogMacroSeparator2, value); // 호환성을 위해
            }
        }

        /// <summary>
        /// 모든 Log Message의 공통 Prefix 메시지
        /// </summary>
        [ModuleProperty()]
        public virtual string MessagePrefix { get => properties?.GetProperty<string>(LogMacroPrefix); set => properties?.SetProperty<string>(LogMacroPrefix, value); }

        /// <summary>
        /// 날자 포맷
        /// </summary>
        [ModuleProperty()]
        public virtual string DateFormat { get; set; } = DefaultDateFormat;

        /// <summary>
        /// 시간 포맷
        /// </summary>
        [ModuleProperty()]
        public virtual string TimeFormat { get; set; } = DefaultTimeFormat;

        /// <summary>
        /// 날자/시간 포맷
        /// </summary>
        [ModuleProperty()]
        public virtual string DateTimeFormat { get; set; } = DefaultDateTimeFormat;

        /// <summary>
        /// 최소 Log 출력 레벨. Off일 경우엔 Log 출력 중지
        /// </summary>
        [ModuleProperty()]
        public virtual LogLevelType LogLevelLimit { get; set; } = LogLevelType.Info;

        /// <summary>
        /// 프로퍼티 개체
        /// </summary>
        public virtual SystemProperties Properties { get => properties;  }

        /// <summary>
        /// Logging 오류시 사용할 Logger
        /// </summary>
        public virtual ILogger LogExceptionLogger { get; set; } = null;

        /// <summary>
        /// 메시지 포맷
        /// </summary>
        [ModuleProperty()]
        public virtual string MessageFormat { get; set; } = DefaultMessageFormat;

        /// <summary>
        /// caller 이름을 지정한 생성자
        /// </summary>
        /// <param name="callerName">Logger 생성한 caller의 이름</param>
        protected LoggerBase(string callerName = null)
        {
            properties = new SystemProperties();

            if (String.IsNullOrEmpty(callerName))
            {
                callerName = LoggerFactory.GetCurrentCallerType().ToString();
            }

            Separator = DefaultSeparator;
            properties.SetProperty(LogMacroCaller, callerName);
        }

        /// <summary>
        /// 프로퍼티의 인스턴스를 지정합니다.
        /// </summary>
        /// <param name="propertiesInstance">프로퍼티 인스턴스</param>
        /// <param name="restoreProperties">이전 프로퍼티를 복구할 것인가 여부</param>
        public void SetPropertiesInstance(ObjectDictionary propertiesInstance, bool restoreProperties = false)
        {
            ObjectDictionary oldProperteis = properties.Properties;
            if (restoreProperties)
            {
                oldProperteis = properties.Properties;
            }

            properties = new SystemProperties(propertiesInstance);

            if (restoreProperties)
            {
                foreach(var item in oldProperteis)
                {
                    if (propertiesInstance.ContainsKey(item.Key))
                    {
                        propertiesInstance[item.Key] = item.Value;
                    }
                    else
                    {
                        propertiesInstance.Add(item.Key, item.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 기본 매크로 사전의 목록을 얻습니다.
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="stackTrace">Stack Trace 정보</param>
        /// <returns>매크로 리스트</returns>
        public virtual IEnumerable<ObjectDictionary> GetLogMacros(LogLevelType logLevel, object logMessage, StackTrace stackTrace = null)
        {
            var now = DateTime.Now;
            var macros = new Dictionary<string, object>()
            {
                { LogMacroRawTime, now },
                { LogMacroDate, now.ToString(DateFormat, CultureInfo.CurrentCulture) },
                { LogMacroTime, now.ToString(TimeFormat, CultureInfo.CurrentCulture) },
                { LogMacroDateTime, now.ToString(DateTimeFormat, CultureInfo.CurrentCulture) },
                { LogMacroLogLevel, logLevel.ToString() }
            };
            if (stackTrace != null)
            {
                macros.Add(LogMacroStackTrace, stackTrace);
            }

            var macrosList = new List<ObjectDictionary>() 
            {
                macros
            };

            if (logMessage != null)
            {
                if (logMessage is IEnumerable<ObjectDictionary> msgDictLst) 
                { 
                    macrosList.AddRange(msgDictLst);
                }
                else
                {
                    if (logMessage is ObjectDictionary msgDict)
                    {
                        macrosList.Add(msgDict);
                    }
                    else
                    {
                        macros.Add(LogMacroMessage, logMessage.ToString());
                    }
                }
            }

            // 매크로 적용 순서에 맞춰 사전 추가
            macrosList.Add(properties.Properties);
            macrosList.Add(SystemProperties.Instance.Properties);
            macrosList.Add(BaseEnvironmentMacros);

            return macrosList;
        }

        /// <summary>
        /// 전체 Log 문자열을 생성합니다.
        /// </summary>
        /// <param name="logMacrosList">Log 정보 매크로</param>
        /// <param name="args">Log 파라미터</param>
        /// <returns>생성된 Log 문자열</returns>
        public virtual string GetFullLogMessage(IEnumerable<ObjectDictionary> logMacrosList, params object[] args)
        {
            var message = MacroUtil.ProcessMacro(MessageFormat, logMacrosList);

            if (logMacrosList.TryGetMacroValue<StackTrace>(LogMacroStackTrace, out var trace))
            {
                message += Environment.NewLine + trace.ToString();
            }

            return message;
        }

        /// <summary>
        /// 전체 Log 문자열을 생성합니다.
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="stackTrace">Stack Trace 정보</param>
        /// <param name="args">Log 파라미터</param>
        /// <returns>생성된 Log 문자열</returns>
        public virtual (string, IEnumerable<ObjectDictionary>) GetFullLogMessage(LogLevelType logLevel, object logMessage, StackTrace stackTrace = null, params object[] args)
        {
            if (logLevel < LogLevelLimit)
            {
                return (null, null);
            }

            var macrosList = GetLogMacros(logLevel, logMessage, stackTrace);
            var message = GetFullLogMessage(macrosList, args);

            return (message, macrosList);
        }

        /// <summary>
        /// 기본 매크로 사전을 얻습니다.
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="stackTrace">Stack Trace 정보</param>
        /// <returns>매크로 리스트</returns>
        [Obsolete("GetLogMacros() 메소드를 사용하세요.")]
        public IList<ObjectDictionary> GetBaseMacros(LogLevelType logLevel, object logMessage, StackTrace stackTrace = null)
        {
            return GetLogMacros(logLevel, logMessage, stackTrace) as IList<ObjectDictionary>;
        }

        /// <summary>
        /// 전체 Log 문자열을 생성합니다.
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="stackTrace">Stack Trace 정보</param>
        /// <param name="args">Log 파라미터</param>
        /// <returns>생성된 Log 문자열</returns>
        [Obsolete("GetFullLogMessage()를 사용하세요.")]
        public virtual (string, IList<ObjectDictionary>) GenerateLogMessage(LogLevelType logLevel, object logMessage, StackTrace stackTrace = null, params object[] args)
        {
            if (logLevel < LogLevelLimit)
            {
                return (null, null);
            }

            var macrosList = GetBaseMacros(logLevel, logMessage, stackTrace);
            var message = GetFullLogMessage(macrosList, args);

            return (message, macrosList as IList<ObjectDictionary>);
        }

        /// <summary>
        /// ILogger의 Log 함수 기본 구현체. 
        /// </summary>
        /// <param name="logLevel">Log 수준</param>
        /// <param name="logMessage">출력할 Log 메시지</param>
        /// <param name="stackTrace">Stack Trace 정보</param>
        /// <param name="args">Log 파라미터</param>
        /// <example>
        /// <code>
        /// public override void Log(LogLevelType logLevel, object logMessage, StackTrace stackTrace, params object[] args)
        /// {
        ///     if (logLevel &lt; LogLevelLimit) return;
        ///     
        ///     try
        ///     {
        ///         // Log 메시지를 출력합니다.
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         if (LogExceptionLogger?.Equals(this) == false)
        ///         {
        ///             LogExceptionLogger.Error(LoggerHelper.BuildExceptionMessage(ex), true, this, logMessage, ex);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="ILogger"/>
        public virtual void Log(LogLevelType logLevel, object logMessage, StackTrace stackTrace = null, params object[] args)
        {
            if (logLevel < LogLevelLimit) return;
            
            try
            {
                var macrosList = GetLogMacros(logLevel, logMessage);
                LogOut(macrosList, args);
            }
            catch (Exception ex)
            {
                if (LogExceptionLogger?.Equals(this) == false)
                {
                    LogExceptionLogger.Error(LoggerHelper.BuildExceptionMessage(ex), true, this, logMessage, ex);
                }
            }
         }

        /// <summary>
        /// 실제 로그 메시지 출력
        /// </summary>
        /// <remarks>
        /// 사실상 abstract 성격의 메소드이지만, 기존 개발 된 Logger들과의 호환성을 위해 abstract 지시지를 붙이지 않았습니다.
        /// </remarks>
        /// <param name="logMacrosList">Log 정보 매크로</param>
        /// <param name="args">Log 파라미터</param>
        protected virtual void LogOut(IEnumerable<ObjectDictionary> logMacrosList, params object[] args)
        {
        }
    }
}
