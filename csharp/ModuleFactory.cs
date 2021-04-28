using System;
using System.Collections.Generic;
using System.Linq;

namespace DevPlatform.Base
{
    using ObjectDictionary = IDictionary<string, object>;

    /// <summary>
    /// 모듈 초기화 함수를 위한 인터페이스
    /// </summary>
    public interface IModuleInitializer
    {
        /// <summary>
        /// 모듈 초기화 함수
        /// </summary>
        /// <param name="moduleInstance">모듈 인스턴스</param>
        /// <param name="moduleTypeName">등록 모듈 타입 이름</param>
        /// <param name="factoryParam">등록자 파라미터</param>
        void Initializer(IModule moduleInstance, string moduleTypeName, object factoryParam = null);
    }

    /// <summary>
    /// 모듈의 프로퍼티 생성 정보
    /// </summary>
    public class ModuleObjectCreateInfo
    {
        /// <summary>
        /// 타입 정보 (타입 or 타입명) 
        /// </summary>
        public object TypeInfo { get; }

        /// <summary>
        /// 생성자 파라미터
        /// </summary>
        public IEnumerable<object> CreateParams { get; }

        /// <summary>
        /// 프로퍼티 값 집합
        /// </summary>
        public ObjectDictionary Properties { get; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="typeInfo">타입정보</param>
        /// <param name="createParams">생성자 파라미터</param>
        /// <param name="properties">프로퍼티 값 집합</param>
        public ModuleObjectCreateInfo(object typeInfo, IEnumerable<object> createParams = null, ObjectDictionary properties = null)
        {
            TypeInfo = typeInfo;
            CreateParams = createParams;
            Properties = properties;
        }
    }

    /// <summary>
    /// Module 생성 Factory
    /// </summary>
    public partial class ModuleFactory
    {
        /// <summary>
        /// 개체의 걊을 채웁니다.
        /// </summary>
        /// <param name="targetInstance">타겟 인스턴스</param>
        /// <param name="targetType">타겟 타입</param>
        /// <param name="value">모듈 property 값 혹은 집합</param>
        /// <param name="fillValues">변수 값 채우기 delegator</param>
        /// <returns>교정 된 값</returns>
        public delegate object FillValues(object targetInstance, Type targetType, object value, FillValues fillValues);

        /// <summary>
        /// 모듈 생성을 위한 정보
        /// </summary>
        public class ModuleFactoryItem
        {
            /// <summary>
            /// 모듈 Type 이름
            /// </summary>
            public string ModuleTypeName { get; }

            /// <summary>
            /// 모듈 Type
            /// </summary>
            public Type ModuleType { get; }

            /// <summary>
            /// 생성자 파라미터
            /// </summary>
            public IEnumerable<object> CreateParams { get; }

            /// <summary>
            /// 초기 프로퍼티
            /// </summary>
            public ObjectDictionary Properties { get; }

            /// <summary>
            /// 추가적인 초기화 함수
            /// </summary>
            public IModuleInitializer AdditionalInitializer { get; set; }

            /// <summary>
            /// 프로퍼티 값 채우는 함수
            /// </summary>
            public FillValues ValueFiller = null;

            /// <summary>
            /// 등록자 파라미터
            /// </summary>
            public object FactoryParam { get; }

            /// <summary>
            /// 생성자
            /// </summary>
            /// <param name="moduleTypeName">모듈의 type 이름</param>
            /// <param name="moduleType">모듈의 type</param>
            /// <param name="createParams">생성자 파라미터</param>
            /// <param name="properties">모듈 프로퍼티</param>
            /// <param name="initializer">모듈 사용자 초기화 인터페이스</param>
            /// <param name="valueFiller">프로퍼티 값 채우기 함수</param>
            /// <param name="factoryParam">등록자 파라미터</param>
            public ModuleFactoryItem(string moduleTypeName, Type moduleType, IEnumerable<object> createParams, ObjectDictionary properties, IModuleInitializer initializer, FillValues valueFiller, object factoryParam = null)
            {
                ModuleTypeName = moduleTypeName;
                ModuleType = moduleType;
                CreateParams = createParams;
                Properties = properties;
                AdditionalInitializer = initializer;
                ValueFiller = valueFiller;
                FactoryParam = factoryParam;
            }
        }

        private readonly IDictionary<string, ModuleFactoryItem> moduleMap = new Dictionary<string, ModuleFactoryItem>();

        /// <summary>
        /// Module Class가 등록되었는가 여부를 체크합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <returns>class 등록 여부</returns>
        public bool ExistsModule(string moduleTypeName)
        {
            return moduleMap.ContainsKey(moduleTypeName);
        }

        /// <summary>
        /// 등록 된 Module Class 정보를 얻습니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <returns>class 등록 여부</returns>
        public ModuleFactoryItem FindModuleFactoryItem(string moduleTypeName)
        {
            if(String.IsNullOrEmpty(moduleTypeName))
            {
                return null;
            }

            if(moduleMap.TryGetValue(moduleTypeName, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 등록 된 Module Class 정보를 얻습니다.
        /// </summary>
        /// <param name="moduleType">Module의 class Type 이름</param>
        /// <returns>class 등록 여부</returns>
        public ICollection<ModuleFactoryItem> FindModuleFactoryItems(Type moduleType)
        {
            if(moduleType == null)
            {
                return null;
            }

            var lst = new List<ModuleFactoryItem>();

            foreach(var module in moduleMap)
            {
                if(moduleType.IsAssignableFrom(module.Value.ModuleType))
                {
                    lst.Add(module.Value);
                }
            }

            return lst;
        }

        /// <summary>
        /// Module을 등록합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <param name="moduleType">Module의 class Type</param>
        /// <param name="createParams">생성자 파라미터들</param>
        /// <param name="properties">초기 프로퍼티 값들</param>
        /// <param name="initializer">추가적인 인스턴스 초기화 함수</param>
        /// <param name="valueFiller">프로퍼티 값 세팅 대행자</param>
        /// <param name="factoryParam">등록자 파라미터자</param>
        /// <returns>등록된 class 이름</returns>
        public string RegistModule(string moduleTypeName, Type moduleType,
            IEnumerable<object> createParams = null,
            ObjectDictionary properties = null, IModuleInitializer initializer = null,
            FillValues valueFiller = null, object factoryParam = null)
        {
            if (moduleType == null)
            {
                return null;
            }

            if(String.IsNullOrEmpty(moduleTypeName))
            {
                moduleTypeName = moduleType.FullName;
            }

            if (typeof(IModule).IsAssignableFrom(moduleType))
            {
                if (!moduleMap.ContainsKey(moduleTypeName))
                {
                    moduleMap.Add(moduleTypeName, new ModuleFactoryItem(moduleTypeName, moduleType, createParams, properties, initializer, valueFiller, factoryParam));
                    return moduleTypeName;
                }
            }

            return null;
        }

        /// <summary>
        /// 등록된 모든 Module들을 삭제합니다.
        /// </summary>
        public void ClearModules()
        {
            moduleMap.Clear();
        }

        /// <summary>
        /// 등록된 Module을 삭제합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        public void RemoveModule(string moduleTypeName)
        {
            moduleMap.Remove(moduleTypeName);
        }

        /// <summary>
        /// 모듈 초기화 함수를 설정합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <param name="initializer">추가적인 인스턴스 초기화 함수</param>
        public void SetModuleInitializer(string moduleTypeName, IModuleInitializer initializer)
        {
            var moduleItem = FindModuleFactoryItem(moduleTypeName);
            if (moduleItem != null)
            {
                moduleItem.AdditionalInitializer = initializer;
            }
        }

        /// <summary>
        /// 모듈 초기화 Property 값을 추가합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <param name="propertyName">Property 이름</param>
        /// <param name="propertyValue">Property 값</param>
        public void SetModuleProperty(string moduleTypeName, string propertyName, object propertyValue)
        {
            var moduleItem = FindModuleFactoryItem(moduleTypeName);
            if (moduleItem != null) { 
                if (propertyValue != null)
                {
                    if (moduleItem.Properties.ContainsKey(propertyName))
                    {
                        moduleItem.Properties[propertyName] = propertyValue;
                    }
                    else
                    {
                        moduleItem.Properties.Add(propertyName, propertyValue);
                    }
                }
                else
                {
                    if (moduleItem.Properties.ContainsKey(propertyName))
                    {
                        moduleItem.Properties.Remove(propertyName);
                    }
                }
            }
        }

        /// <summary>
        /// 모듈 초기화 Property 값들을 일괄 추가합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        /// <param name="properties">Property 목록</param>
        public void SetModuleProperties(string moduleTypeName, IEnumerable<KeyValuePair<string, object>> properties)
        {
            if (properties == null) return;
            var moduleItem = FindModuleFactoryItem(moduleTypeName);
            if (moduleItem != null)
            {
                foreach (var property in properties)
                {
                    if (moduleItem.Properties.ContainsKey(property.Key))
                    {
                        moduleItem.Properties[property.Key] = property.Value;
                    }
                    else
                    {
                        moduleItem.Properties.Add(property.Key, property.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 모듈 초기화 Property를 일괄 삭제한다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class Type 이름</param>
        public void ClearModuleProperties(string moduleTypeName)
        {
            var moduleItem = FindModuleFactoryItem(moduleTypeName);
            if (moduleItem != null)
            {
                moduleItem.Properties.Clear();
            }
        }

        /// <summary>
        /// Module 인스턴스를 생성하고, Properties를 초기화 합니다.
        /// </summary>
        /// <param name="moduleTypeItem">Module의 class</param>
        /// <param name="args">생성자 파라미터</param>
        /// <param name="properties">Module의 초기화 프로퍼티들</param>
        /// <returns>생성된 Module의 인스턴스</returns>
        public IModule CreateModuleInstance(ModuleFactoryItem moduleTypeItem, ObjectDictionary properties = null, params object[] args)
        {
            if (moduleTypeItem == null) return null;

            IModule mod;
            try
            {
                object[] allArgs;
                if (moduleTypeItem.CreateParams?.Any() == true)
                {
                    if (args?.Any() == true)
                    {
                        var lst = new List<object>();
                        foreach (var a in moduleTypeItem.CreateParams)
                        {
                            lst.Add(a);
                        }
                        foreach (var a in args)
                        {
                            lst.Add(a);
                        }
                        allArgs = lst.ToArray();
                    }
                    else
                    {
                        allArgs = moduleTypeItem.CreateParams.ToArray();
                    }
                }
                else
                {
                    allArgs = args;
                }
                mod = Activator.CreateInstance(moduleTypeItem.ModuleType, allArgs) as IModule;
            }
            catch(MissingMethodException ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            catch
            {
                throw;
            }
            if (mod == null) return null;

            mod.FillProperties(moduleTypeItem.Properties, moduleTypeItem.ValueFiller);
            if (properties?.Any() == true)
            {
                mod.FillProperties(properties);
            }

            mod.InitializeInstance(moduleTypeItem.ModuleTypeName, moduleTypeItem.FactoryParam);

            if (moduleTypeItem.AdditionalInitializer != null)
            {
                moduleTypeItem.AdditionalInitializer.Initializer(mod, moduleTypeItem.ModuleTypeName, moduleTypeItem.FactoryParam);
            }

            return mod;
        }

        /// <summary>
        /// Module 인스턴스를 생성하고, Properties를 초기화 합니다.
        /// </summary>
        /// <param name="moduleTypeName">Module의 class 이름</param>
        /// <param name="args">생성자 파라미터</param>
        /// <param name="properties">Module의 초기화 프로퍼티들</param>
        /// <returns>생성된 Module의 인스턴스</returns>
        public IModule CreateModuleInstance(string moduleTypeName, ObjectDictionary properties = null, params object[] args)
        {
            if (moduleMap.TryGetValue(moduleTypeName, out var moduleTypeItem))
            {
                return CreateModuleInstance(moduleTypeItem, properties, args);
            }

            return null;
        }
    }
}
