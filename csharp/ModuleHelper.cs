using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Linq;
using System.Net.NetworkInformation;

namespace DevPlatform.Base
{
    using ObjectDictionary = IDictionary<string, object>;

    /// <summary>
    /// IModule의 Helper class
    /// </summary>
    public static class ModuleHelper
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(SystemConstants.SystemLogger);

        /// <summary>
        /// IModule의 property를 채웁니다.
        /// </summary>
        /// <param name="module">모듈 인스턴스</param>
        /// <param name="properties">모듈 property 집합</param>
        /// <param name="fillValues">object의 값 세팅 함수</param>
        public static void FillProperties(this IModule module, IDictionary<string, object> properties, ModuleFactory.FillValues fillValues = null)
        {
            if (module == null) return;
            if (fillValues == null) fillValues = DefaultFillValues;
            fillValues(module, module.GetType(), properties, fillValues);
        }

        /// <summary>
        /// 개체의 걊을 채웁니다.
        /// </summary>
        /// <param name="targetInstance">타겟 인스턴스</param>
        /// <param name="targetType">타겟 타입</param>
        /// <param name="value">모듈 property 값 혹은 집합</param>
        /// <param name="fillValues">변수 값 채우기 delegator</param>
        /// <returns>교정 된 값</returns>
        public static object DefaultFillValues(object targetInstance, Type targetType, object value, ModuleFactory.FillValues fillValues)
        {
            if (targetType == null || value == null) return null;

            var sourceType = value.GetType();

            // Target과 Value의 타입이 호환될 때
            if (targetType.IsAssignableFrom(sourceType))
            {
                if (typeof(ICloneable).IsAssignableFrom(sourceType))
                {
                    return ((ICloneable)value).Clone();
                }
                else
                {
                    return value;
                }
            }
            else if (targetType.IsAnsiClass && value != null && (value is ObjectDictionary || value is ModuleObjectCreateInfo))
            {
                object typeInfo = null;
                ObjectDictionary properties;
                object[] args;
                if (value is ModuleObjectCreateInfo)
                {
                    var value2 = value as ModuleObjectCreateInfo;
                    typeInfo = value2.TypeInfo;
                    args = value2.CreateParams?.ToArray<object>();
                    properties = value2.Properties;
                }
                else if(value is ObjectDictionary)
                {
                    args = null;
                    properties = value as ObjectDictionary;
                }
                else
                {
                    logger?.Debug($"지원하지 않는 Value 타입 : {value.GetType().FullName}");
                    return null;
                }

                Type type;
                if (typeInfo == null)
                {
                    type = targetType;
                }
                else
                {
                    type = typeInfo as Type;
                }
                if (type == null || type.IsAbstract)
                {
                    return null;
                }

                try
                {
                    var newTargetClass = targetInstance ?? ((args != null) ? Activator.CreateInstance(type, args) :
                                                                             Activator.CreateInstance(type));
                    if (properties != null)
                    {
                        var instProp = type.GetProperties();
                        foreach (var prop in instProp)
                        {
                            if (prop.CanWrite)
                            {
                                var attr = prop.GetCustomAttribute<ModulePropertyAttribute>(true);
                                string storedName = (attr != null && attr.StoredName != null) ? attr.StoredName : prop.Name;
                                if (properties.TryGetValue(storedName, out object propValue))
                                {
                                    var propOrgValue = (targetInstance != null && prop.CanRead) ? prop.GetValue(targetInstance) : null;
                                    var newValue = fillValues(propOrgValue, prop.PropertyType, propValue, fillValues);
                                    if (newValue != null)
                                    {
                                        prop.SetValue(newTargetClass, newValue);
                                    }
                                }
                            }
                        }
                    }

                    return (targetInstance == null) ? newTargetClass : null;
                }
                catch (Exception ex)
                {
                    logger?.Error(ex.Message);
                }
            }
            else if (value is IConvertible)
            {
                try
                {
                    return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
                }
                catch (InvalidCastException ex)
                {
                    logger?.Trace(ex.Message);
                }
            }

            return null;
        }

        /// <summary>
        /// 지정된 이름의 Assembly를 얻습니다.
        /// </summary>
        /// <param name="assemblyName">Assembly 이름</param>
        /// <returns>Assembly 인스턴스</returns>
        public static Assembly FindAssemblyByName(string assemblyName)
        {
            Assembly assembly = null;

            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                logger?.Warn(ex.Message);
            }
            catch (System.IO.FileLoadException ex)
            {
                logger?.Warn(ex.Message);
            }
            catch (System.BadImageFormatException ex)
            {
                logger?.Warn(ex.Message);
            }
            catch (Exception ex)
            {
                logger?.Warn(ex.Message);
            }

            return assembly;
        }

        /// <summary>
        /// Module Type을 얻습니다.
        /// </summary>
        /// <param name="assembly">Assembly 인스턴스</param>
        /// <param name="moduleName">Module의 Type 이름</param>
        /// <returns>Module의 Type</returns>
        public static Type FindModuleTypeByName(Assembly assembly, string moduleName)
        {
            if (assembly == null) return null;

            Type moduleType = null;
            try
            {
                moduleType = assembly.GetType(moduleName);
            }
            catch (Exception ex)
            {
                logger?.Warn(ex.Message);
            }

            return (typeof(IModule).IsAssignableFrom(moduleType)) ? moduleType : null;
        }

        /// <summary>
        /// Module Type을 얻습니다.
        /// </summary>
        /// <param name="assemblyName">Assembly 이름</param>
        /// <param name="moduleName">Module의 Type 이름</param>
        /// <returns>Module의 Type</returns>
        public static Type FindModuleTypeByName(string assemblyName, string moduleName)
        {
            return FindModuleTypeByName(FindAssemblyByName(assemblyName), moduleName);
        }
    }
}
