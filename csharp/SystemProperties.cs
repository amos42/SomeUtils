using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DevPlatform.Base
{
    using ObjectDictionary = IDictionary<string, object>;

    /// <summary>
    /// 시스템 프로퍼티 관리
    /// </summary>
    public class SystemProperties
    {
        // singletone pattern
        private static readonly Lazy<SystemProperties> lazy = new Lazy<SystemProperties>(() => new SystemProperties());

        /// <summary>
        /// SystemProperties Singletone instance
        /// </summary>
        public static SystemProperties Instance { get { return lazy.Value; } }

        /// <summary>
        /// 프로퍼티명 기본 Namespace
        /// </summary>
        public string VariableNamespace { get; set; } = null;

        /// <summary>
        /// Namespace와 프로퍼티명의 구분문자
        /// </summary>
        public string NamespaceSeparator { get; set; } = ".";

        /// <summary>
        /// 전체 프로퍼티 얻기
        /// </summary>
        public virtual ObjectDictionary Properties { get; } = null;

        /// <summary>
        /// 프로퍼티 전체 갯수
        /// </summary>
        public int PropertiesCount { get => Properties?.Count ?? 0; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="sysProperties">대상 SystemProperties 개체</param>
        /// <param name="variableNamespace">네임스페이스</param>
        public SystemProperties(SystemProperties sysProperties, string variableNamespace = null) : this(sysProperties?.Properties, variableNamespace)
        {
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="properties">대상 사전 개체</param>
        /// <param name="variableNamespace">네임스페이스</param>
        public SystemProperties(ObjectDictionary properties, string variableNamespace = null)
        {
            Properties = properties ?? new Dictionary<string, object>();

            this.VariableNamespace = variableNamespace;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public SystemProperties() : this(null as ObjectDictionary, null)
        {
        }

        /// <summary>
        /// 시스템 프로퍼티 전체를 삭제합니다.
        /// </summary>
        public void ClearProperties()
        {
            Properties?.Clear();
        }

        /// <summary>
        /// Namespace를 포함한 완전한 Key 이름을 얻습니다.
        /// </summary>
        /// <param name="keyName">Key 이름</param>
        /// <returns>완전한 Key 이름</returns>
        public virtual string GetFullKeyName(string keyName)
        {
            if (String.IsNullOrEmpty(keyName))
            {
                return null;
            }

            if (String.IsNullOrEmpty(VariableNamespace))
            {
                return keyName;
            }

            return $"{VariableNamespace}{NamespaceSeparator}{keyName}";
        }

        /// <summary>
        /// 프로퍼티를 추가합니다.
        /// 만약 해당 프로퍼티 값이 존재한다면 갱신합니다.
        /// </summary>
        /// <param name="name">프로퍼티 이름</param>
        /// <param name="value">프로퍼티 값</param>
        public void SetProperty(string name, object value)
        {
            if (Properties == null)
            {
                return;
            }

            name = GetFullKeyName(name);
            if(name == null)
            {
                return;
            }

            lock (Properties)
            {
                if (Properties.ContainsKey(name))
                {
                    Properties[name] = value;
                }
                else
                {
                    Properties.Add(name, value);
                }
            }
        }

        /// <summary>
        /// 프로퍼티 값을 얻는다.
        /// </summary>
        /// <param name="name">프로퍼티 이름</param>
        /// <returns>프로퍼티 값</returns>
        public object GetProperty(string name)
        {
            if (Properties == null)
            {
                return null;
            }

            name = GetFullKeyName(name);
            if (name == null)
            {
                return null;
            }

            lock (Properties) 
            {
                if (Properties.TryGetValue(name, out var value))
                {
                    return value;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 프로퍼티 값을 얻는다.
        /// </summary>
        /// <param name="name">프로퍼티 이름</param>
        /// <param name="value">프로퍼티 값 저장할 변수</param>
        /// <returns>성공시 true</returns>
        public bool TryGetProperty(string name, out object value)
        {
            if (Properties == null)
            {
                value = null;
                return false;
            }

            name = GetFullKeyName(name);
            if (name == null)
            {
                value = null;
                return false;
            }

            lock (Properties)
            {
                return Properties.TryGetValue(name, out value);
            }
        }

        /// <summary>
        /// 프로퍼티를 삭제합니다.
        /// </summary>
        /// <param name="name">변수 이름</param>
        public void RemoveProperty(string name)
        {
            if (Properties == null)
            {
                return;
            }

            name = GetFullKeyName(name);
            if (name == null)
            {
                return;
            }

            lock (Properties)
            {
                Properties.Remove(name);
            }
        }

        /// <summary>
        /// 프로퍼티 리스트를 얻습니다.
        /// </summary>
        /// <param name="format">문자열 포맷</param>
        /// <param name="separator">구분자</param>
        /// <returns>프로퍼티 목록</returns>
        public virtual string ToString(string format, string separator = null)
        {
            if (Properties == null)
            {
                return null;
            }

            var sb = new StringBuilder();

            if (String.IsNullOrEmpty(separator)) 
            {
                foreach (var dic in Properties)
                {
                    var name = GetFullKeyName(dic.Key);
                    if (name == null)
                    {
                        continue;
                    }
                    sb.Append(String.Format(format, name, dic.Value));
                }
            }
            else
            {
                bool first = true;
                foreach (var dic in Properties)
                {
                    var name = GetFullKeyName(dic.Key);
                    if (name == null)
                    {
                        continue;
                    }
                    var str = String.Format(format, name, dic.Value);
                    if (!first)
                    {
                        str = separator + str;
                    }
                    sb.Append(str);
                    first = false;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 프로퍼티 리스트를 얻습니다.
        /// </summary>
        /// <returns>프로퍼티 목록</returns>
        public override string ToString()
        {
            return ToString("{0}: {1}", Environment.NewLine);
        }
    }
}
