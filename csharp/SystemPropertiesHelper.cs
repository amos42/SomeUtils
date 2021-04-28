using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DevPlatform.Base
{
    /// <summary>
    /// 시스템 프로퍼티 관리
    /// </summary>
    public static class SystemPropertiesHelper
    {
        /// <summary>
        /// 복수개의 프로퍼티들을 일괄 추가합니다.
        /// </summary>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="properties">프로퍼티들 집합</param>
        public static void SetProperties(this SystemProperties self, IDictionary<string, object> properties)
        {
            if (properties == null) return;
            foreach (var prop in properties)
            {
                self.SetProperty(prop.Key, prop.Value);
            }
        }

        /// <summary>
        /// 타입을 지정하여 프로퍼티 값을 얻는다.
        /// </summary>
        /// <typeparam name="T">캐스팅 할 타입</typeparam>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="name">프로퍼티 이름</param>
        /// <returns>프로퍼티 값</returns>
        public static T GetProperty<T>(this SystemProperties self, string name)
        {
            if (self.TryGetProperty(name, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch (InvalidCastException)
                {
                    try
                    {
                        return (T)Convert.ChangeType(value, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        return default(T);
                    }
                    catch (ArgumentNullException)
                    {
                        return default(T);
                    }
                    catch
                    {
                        throw;
                    }
                }
            }
            else
            {
                return default(T);
            }
        }

        /// <summary>
        /// 타입을 지정하여 프로퍼티 값을 얻는다.
        /// </summary>
        /// <typeparam name="T">캐스팅 할 타입</typeparam>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="name">프로퍼티 이름</param>
        /// <param name="value">프로퍼티 값 저장할 변수</param>
        /// <returns>성공시 true</returns>
        public static bool TryGetProperty<T>(this SystemProperties self, string name, out T value)
        {
            bool r;
            try
            {
                r = self.TryGetProperty (name, out var value0);
                if (r)
                {
                    value = (T)value0;
                }
                else
                {
                    value = default(T);
                }
            }
            catch (InvalidCastException) 
            {
                value = default(T);
                r = false;
            }

            return r;
        }

        /// <summary>
        /// 타입을 지정하여 프로퍼티를 추가합니다.
        /// 만약 해당 프로퍼티 값이 존재한다면 갱신합니다.
        /// </summary>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="name">프로퍼티 이름</param>
        /// <param name="value">프로퍼티 값</param>
        public static void SetProperty<T>(this SystemProperties self, string name, object value)
        {
            if (value == null)
            {
                self.SetProperty(name, default(T));
            }
            else if (typeof(T).IsAssignableFrom(value.GetType()))
            {
                self.SetProperty(name, value);
            }
        }

        /// <summary>
        /// Namespace와 Caller 이름을 이용한 축약 표현 가능한 SetProperty 함수
        /// </summary>
        /// <typeparam name="T">변수 타입</typeparam>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="value">값</param>
        /// <param name="name">프로퍼티 이름</param>
        public static void SetValue<T>(this SystemProperties self, T value, [CallerMemberName] string name = null)
        {
            self.SetProperty<T>(name, value);
        }

        /// <summary>
        /// Namespace와 Caller 이름을 이용한 축약 표현 가능한 GetProperty 함수
        /// </summary>
        /// <typeparam name="T">변수 타입</typeparam>
        /// <param name="self">SystemProperties 개체 인스턴스</param>
        /// <param name="name">프로퍼티 이름</param>
        /// <returns>프로퍼티 값</returns>
        public static T GetValue<T>(this SystemProperties self, [CallerMemberName] string name = null)
        {
            return self.GetProperty<T>(name);
        }
    }
}
