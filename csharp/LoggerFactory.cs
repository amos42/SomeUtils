using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DevPlatform.Base
{
    /// <summary>
    /// LoggerFactory 클래스
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Caller별 Logger 테이블
        /// </summary>
        private static readonly Dictionary<string, string> loggerByCallerTable = new Dictionary<string, string>();

        /// <summary>
        /// 기본 Logger 타입 이름
        /// </summary>
        private static string defaultLoggerType = null;//"DevPlatform.Logger";

        /// <summary>
        /// Logger 사용시 발생한 문제를 리포팅하기 위한 비상용 Logger
        /// </summary>
        public static ILogger LogExceptionLogger = null;

        /// <summary>
        /// 현재 호출된 위치의 class type을 얻습니다.
        /// </summary>
        /// <param name="depth">Stack Frame의 depth. 기본은 0</param>
        /// <returns>현재 호출된 위치의 class type</returns>
        public static Type GetCurrentCallerType(int depth = 0)
        {
            var frame = new StackFrame(depth + 1);
            return frame.GetMethod().DeclaringType;
        }

        /// <summary>
        /// Logger Module을 등록합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <param name="moduleType">Module의 class Type</param>
        /// <param name="properties">생성 파라미터</param>
        /// <param name="initializer">추가적인 인스턴스 초기화 함수</param>
        /// <param name="isDefaultLogger">기본 Logger로 지정 여부</param>
        /// <returns>등록된 class 이름</returns>
        public static string RegistLoggerModule(string moduleTypeName, Type moduleType,
            IDictionary<string, object> properties = null, IModuleInitializer initializer = null, bool isDefaultLogger = false)
        {
            // ILogger 타입인가 확인
            if (!typeof(ILogger).IsAssignableFrom(moduleType))
            {
                return null;
            }

            // 빈 생성자나 혹은 callerName을 파라미터로 하는 생성자가 포함되어 있는가 확인
            if( moduleType.GetConstructor(new Type[] { typeof(string) }) == null &&
                moduleType.GetConstructor(new Type[] { }) == null)
            {
                return null;
            }

            // Logger를 ModuleFactory에 등록
            var logger = ModuleFactory.Instance.RegistModule(moduleTypeName, moduleType, null, properties, initializer);
            if(isDefaultLogger && String.IsNullOrEmpty(logger))
            {
                SetDefaultLoggerType(logger);
            }

            return logger;
        }

        /// <summary>
        /// Logger Module을 등록합니다.
        /// </summary>
        /// <param name="moduleType">Module의 class Type</param>
        /// <param name="properties">생성 파라미터</param>
        /// <param name="initializer">추가적인 인스턴스 초기화 함수</param>
        /// <param name="isDefaultLogger">기본 Logger로 지정 여부</param>
        /// <returns>등록된 class 이름</returns>
        public static string RegistLoggerModule(Type moduleType,
            IDictionary<string, object> properties = null, IModuleInitializer initializer = null, bool isDefaultLogger = false)
        {
            return RegistLoggerModule(moduleType.FullName, moduleType, properties, initializer, isDefaultLogger);
        }

        /// <summary>
        /// 기본 Logger 타입을 지정합니다.
        /// </summary>
        /// <param name="loggerTypeName">기본으로 설정할 Logger 타입명</param>
        public static void SetDefaultLoggerType(string loggerTypeName)
        {
            defaultLoggerType = loggerTypeName;
        }

        /// <summary>
        /// Caller별 Logger 타입을 지정합니다.
        /// </summary>
        /// <param name="callerName">caller의 이름</param>
        /// <param name="loggerTypeName">caller에 기본 지정할 logger 이름. null 지정하면 삭제</param>
        public static void SetLoggerByCaller(string callerName, string loggerTypeName)
        {
            if (loggerTypeName != null)
                loggerByCallerTable[callerName] = loggerTypeName;
            else
                loggerByCallerTable.Remove(callerName);
        }

        /// <summary>
        /// Caller별 Logger 테이블을 초기화 합니다.
        /// </summary>
        public static void ClearLoggerByCallerTable()
        {
            loggerByCallerTable.Clear();
        }

        /// <summary>
        /// 기본 Logger 타입을 지정합니다.
        /// </summary>
        /// <param name="loggerType">기본으로 설정할 Logger 타입</param>
        public static void SetDefaultLoggerType(Type loggerType)
        {
            SetDefaultLoggerType(loggerType?.FullName);
        }

        /// <summary>
        /// 기본 Logger 타입을 얻습니다.
        /// </summary>
        /// <returns>기본 Logger 타입의 이름</returns>
        public static string GetDefaultLoggerTypeName()
        {
            return defaultLoggerType;
        }

        /// <summary>
        /// Logger의 초기화 함수를 지정합니다.
        /// </summary>
        /// <param name="loggerTypeName">설정할 Logger 타입명</param>
        /// <param name="initializer">초기화 함수</param>
        public static void SetLoggerInitializer(string loggerTypeName, IModuleInitializer initializer = null)
        {
            ModuleFactory.Instance.SetModuleInitializer(loggerTypeName, initializer);
        }

        /// <summary>
        /// Logger의 초기화 함수를 지정합니다.
        /// (Deprecated)
        /// </summary>
        /// <param name="loggerTypeName">설정할 Logger 타입명</param>
        /// <param name="initializer">초기화 함수</param>
        /// <param name="param">초기화 함수 파라미터</param>
        [Obsolete("이 함수는 더이상 지원하지 않습니다. IModuleInitializer를 이용한 함수로 교체하시기 바랍니다.", false)]
        public static void SetLoggerInitializer(string loggerTypeName, ModuleFactory.ModuleInitializer initializer, object param = null)
        {
            ModuleFactory.Instance.SetModuleInitializer(loggerTypeName, initializer, param);
        }

        /// <summary>
        /// 지정된 호출자 이름과 타입명의 Logger 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="loggerTypeName">Logger 타입 이름. 생략시 "DevPlatform.Logger" 타입으로 생성</param>
        /// <param name="callerName">Logger를 호출한 class의 이름</param>
        /// <param name="properties">Logger의 초기화 파라미터들</param>
        /// <returns>현재 class 대상 ILogger 인스턴스</returns>
        public static ILogger GetLogger(string loggerTypeName, string callerName, IDictionary<string, object> properties = null)
        {
            if (loggerByCallerTable.TryGetValue(callerName, out var loggerName))
            {
                loggerTypeName = loggerName;
            }

            ILogger logger = null;
            if (!String.IsNullOrEmpty(loggerTypeName))
            {
                // 단일 포맷의 생성자를 가질 경우 인스턴스 생성
                //logger = ModuleFactory.Instance.CreateModuleInstance(loggerTypeName, properties, callerName) as ILogger;

                // caller를 받는 생성자와, 빈 생성자를 포함한 인스턴스 생성
                var moduleTypeItem = ModuleFactory.Instance.FindModuleFactoryItem(loggerTypeName);
                if (moduleTypeItem != null)
                {
                    // caller를 받는 생성자
                    if (moduleTypeItem.ModuleType.GetConstructor(new Type[] { typeof(string) }) != null)
                    {
                        logger = ModuleFactory.Instance.CreateModuleInstance(moduleTypeItem, properties, callerName) as ILogger;
                    }
                    // 빈 생성자
                    else if (moduleTypeItem.ModuleType.GetConstructor(new Type[] { }) != null)
                    {
                        logger = ModuleFactory.Instance.CreateModuleInstance(moduleTypeItem, properties) as ILogger;
                    }
                }
                else
                {
                    if (loggerTypeName.Equals(SystemConstants.DefaultLogger, StringComparison.OrdinalIgnoreCase))
                    {
                        logger = new SimpleLogger(callerName);
                    }
                }

                var baseLogger = logger as LoggerBase;
                if (baseLogger != null)
                {
                    if (baseLogger.LogExceptionLogger == null)
                    {
                        baseLogger.LogExceptionLogger = LogExceptionLogger;
                    }
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(defaultLoggerType))
                {
                    logger = GetLogger(defaultLoggerType, callerName);
                }
                else
                {
                    logger = GetLogger(SystemConstants.DefaultLogger, callerName);
                }
            }

            return logger;
        }

        /// <summary>
        /// 현재 위치의 class에 해당하는 지정된 타입명의 Logger 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="loggerTypeName">Logger 타입 이름. 생략시 "DevPlatform.Logger" 타입으로 생성</param>
        /// <param name="stackDepth">Caller의 Stack 깊이. 기본은 1</param>
        /// <returns>현재 class 대상 ILogger 인스턴스</returns>
        public static ILogger GetLogger(string loggerTypeName, int stackDepth = 1)
        {
            string currentClass = GetCurrentCallerType(stackDepth).FullName;
            return GetLogger(loggerTypeName, currentClass);
        }

        /// <summary>
        /// 현재 위치의 class에 해당하는 default 타입의 Logger 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="stackDepth">Caller의 Stack 깊이. 기본은 1</param>
        /// <returns>현재 class 대상 ILogger 인스턴스</returns>
        public static ILogger GetLogger(int stackDepth = 1)
        {
            string currentClass = GetCurrentCallerType(stackDepth).FullName;
            return GetLogger(defaultLoggerType, currentClass);
        }
    }
}
