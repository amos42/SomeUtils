using DevPlatform.Base;
using DevPlatform.CommonUtil;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace DevPlatform.Bootstrap
{
    /// <summary>
    /// IBootstrap 구현체
    /// </summary>
    public static class BootstrapHelper
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(SystemConstants.SystemLogger);

        /// <summary>
        /// 개체의 걊을 채웁니다.
        /// </summary>
        /// <param name="targetInstance">타겟 인스턴스</param>
        /// <param name="targetType">타겟 타입</param>
        /// <param name="value">모듈 property 값 혹은 집합</param>
        /// <param name="fillValues">변수 값 채우기 delegator</param>
        /// <returns>교정 된 값</returns>
        public static object DefaultBootstrapFillValues(object targetInstance, Type targetType, object value, ModuleFactory.FillValues fillValues)
        {
            if (targetType == null || value == null) return null;

            var outValue = ModuleHelper.DefaultFillValues(targetInstance, targetType, value, fillValues);
            if(outValue != null)
            {
                return outValue;
            }

            var sourceType = value.GetType();

            /*
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
            */

            // Target 타입이 Enum 일 경우
            if (targetType.IsEnum)
            {
                // 단일 문자열값
                if (value is string)
                {
                    try
                    {
                        return Enum.Parse(targetType, value as string, true);
                    }
                    catch (Exception ex)
                    {
                        logger?.Warn(ex.Message);
                        return null;
                    }
                }
                else
                {
                    // Enum이 Flags 속성을 갖고, 값이 배열값일 경우
                    var isFlags = targetType.GetCustomAttribute<FlagsAttribute>();
                    if ((isFlags != null) && typeof(IEnumerable).IsAssignableFrom(value.GetType()))
                    {
                        int flags = 0;
                        foreach (var v in value as IEnumerable)
                        {
                            Enum flag;
                            try
                            {
                                if (targetType.IsAssignableFrom(v.GetType()))
                                {
                                    flag = v as Enum;
                                }
                                else
                                {
                                    flag = Enum.Parse(targetType, v.ToString(), true) as Enum;
                                }

                                flags |= Convert.ToInt32(flag);
                            }
                            catch (Exception ex)
                            {
                                logger?.Warn(ex.Message);
                                continue;
                            }
                        }
                        return Enum.Parse(targetType, flags.ToString());
                    }
                }
            }
            // Target 타입과 Value의 타입이 모두 열거형일 때
            else if (typeof(IEnumerable).IsAssignableFrom(targetType) && value is IEnumerable)
            {
                if (targetType.IsArray)
                {
                    var valueCollection = (value as IEnumerable);
                    int len;
                    if (value is ICollection)
                    {
                        len = (valueCollection as ICollection).Count;
                    }
                    else
                    {
                        len = 0;
                        foreach (var v in valueCollection)
                        {
                            len++;
                        }
                    }
                    var desElmType = targetType.GetElementType();

                    var newValueArr = (targetInstance != null) ? targetInstance as Array :
                                       Array.CreateInstance(desElmType, len);
                    //creator(targetType, len, null, null) as Array;
                    int i = 0;
                    foreach (var v in valueCollection)
                    {
                        object newValue = fillValues(null, desElmType, v, fillValues);
                        if (newValue != null)
                        {
                            newValueArr.SetValue(newValue, i);
                        }
                        i++;
                    }
                    return newValueArr;
                }
                else if (typeof(IDictionary).IsAssignableFrom(targetType) && targetType.IsGenericType && value is IDictionary)
                {
                    var valueDic = value as IDictionary;
                    var newValue = (targetInstance ?? Activator.CreateInstance(targetType)) as IDictionary;
                    //creator(targetType, null, null, null) as IDictionary;

                    var desValueType = targetType.GetGenericArguments()[1];
                    foreach (var s in valueDic.Keys)
                    {
                        try
                        {
                            var newValue0 = fillValues(null, desValueType, valueDic[s], fillValues);
                            if (newValue0 != null)
                            {
                                newValue.Add(s, newValue0);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.Warn(ex.Message);
                            continue;
                        }
                    }
                    return newValue;
                }
                else if (typeof(NameValueCollection).IsAssignableFrom(targetType))
                {
                    if (!typeof(IEnumerable<object>).IsAssignableFrom(value.GetType())) return null;
                    if (!((targetInstance ?? Activator.CreateInstance(targetType)) is NameValueCollection newValue)) return null;

                    var valueDic = value as IEnumerable<object>;
                    foreach (var s in valueDic)
                    {
                        try
                        {
                            var itm = (KeyValuePair<string, object>)s;
                            var newKey0 = Convert.ToString(itm.Key, CultureInfo.CurrentCulture);
                            var newValue0 = Convert.ToString(itm.Value, CultureInfo.CurrentCulture);
                            if (newKey0 != null && newValue0 != null)
                            {
                                newValue.Add(newKey0, newValue0);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger?.Warn(ex.Message);
                            continue;
                        }
                    }
                    return newValue;
                }
                else if (targetType.IsGenericType)
                {
                    if (typeof(IDictionary).IsAssignableFrom(targetType) ||
                        targetType.FullName.Substring(0, targetType.FullName.IndexOf('`')).Equals("System.Collections.Generic.IDictionary", StringComparison.Ordinal))
                    {
                        IDictionary newValue = null;
                        var desValueType1 = targetType.GetGenericArguments()[0];
                        var desValueType2 = targetType.GetGenericArguments()[1];
                        if (targetInstance != null)
                        {
                            if (typeof(IDictionary).IsAssignableFrom(targetInstance.GetType()))
                            {
                                newValue = targetInstance as IDictionary;
                            }
                        }
                        else
                        {
                            var listType = typeof(Dictionary<,>).MakeGenericType(desValueType1, desValueType2);
                            newValue = Activator.CreateInstance(listType) as IDictionary;
                        }

                        if (newValue != null)
                        {
                            var valueCollection = (value as IEnumerable);
                            foreach (var s in valueCollection)
                            {
                                if (s is Bootstrap.ExtModuleObjectCreateInfo value2)
                                {
                                    var name = value2?.Name;

                                    try
                                    {
                                        var newValue0 = fillValues(null, desValueType2, s, fillValues);
                                        if (newValue0 != null)
                                        {
                                            newValue.Add(name, newValue0);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        logger?.Warn(ex.Message);
                                        continue;
                                    }
                                }
                                else if (s is KeyValuePair<string, object>)
                                {
                                    var vv = (KeyValuePair<string, object>)s;
                                    newValue.Add(vv.Key, vv.Value);
                                }
                            }
                        }
                        return newValue;
                    }
                    else
                    {
                        IList newValue = null;
                        var desValueType = targetType.GetGenericArguments()[0];
                        if (targetInstance != null)
                        {
                            if (typeof(IList).IsAssignableFrom(targetInstance.GetType()))
                            {
                                newValue = targetInstance as IList;
                            }
                        }
                        else
                        {
                            var listType = typeof(List<>).MakeGenericType(desValueType);
                            newValue = Activator.CreateInstance(listType) as IList;
                        }
                        if (newValue != null)
                        {
                            var valueCollection = (value as IEnumerable);
                            foreach (var s in valueCollection)
                            {
                                try
                                {
                                    var newValue0 = fillValues(null, desValueType, s, fillValues);
                                    if (newValue0 != null)
                                    {
                                        newValue.Add(newValue0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger?.Warn(ex.Message);
                                    continue;
                                }
                            }
                        }
                        return newValue;
                    }
                }
            }

            return null;
        }

    }
}
