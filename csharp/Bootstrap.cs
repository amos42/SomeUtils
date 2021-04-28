using DevPlatform.Base;
using DevPlatform.CommonUtil;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using DevPlatform.Bootstrap.BootstrapDataV1;
using DevPlatform.Bootstrap.BootstrapDataTypes;

namespace DevPlatform.Bootstrap
{
    /// <summary>
    /// IBootstrap 구현체
    /// </summary>
    public static class Bootstrap
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(SystemConstants.SystemLogger);

        /// <summary>
        /// C# 스크립트 호출 파라미터
        /// </summary>
        public class SourceParam
        {
            /// <summary>
            /// 스크립트 호출한 모듈의 인스턴스
            /// </summary>
            public IModule module;

            /// <summary>
            /// 사용자 정의 파라미터
            /// </summary>
            public object param;
        }

        /// <summary>
        /// 모듈의 프로퍼티 생성 정보
        /// </summary>
        public class ExtModuleObjectCreateInfo : ModuleObjectCreateInfo
        {
            /// <summary>
            /// 오브젝트 타입
            /// </summary>
            public enum ObjectValueType
            {
                /// <summary>
                /// 개체 타입
                /// </summary>
                NormalObject,
                /// <summary>
                /// 값 타입
                /// </summary>
                Value,
                /// <summary>
                /// Turple 타입
                /// </summary>
                Turple
            }

            /// <summary>
            /// 개체 이름
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 개체 값 타입
            /// </summary>
            public ObjectValueType ValueType { get; }

            /// <summary>
            /// 개체의 값
            /// </summary>
            public object Value { get; }

            /// <summary>
            /// 생성자
            /// </summary>
            /// <param name="name">개체 이름</param>
            /// <param name="typeInfo">타입정보</param>
            /// <param name="createParams">생성자 파라미터</param>
            /// <param name="properties">프로퍼티 값 집합</param>
            /// <param name="valueType">값 타입</param>
            /// <param name="objectValue">개체 혹은 값</param>
            public ExtModuleObjectCreateInfo(string name, object typeInfo, IEnumerable<object> createParams = null, IDictionary<string, object> properties = null, ObjectValueType valueType = ObjectValueType.NormalObject, object objectValue = null) :
                base(typeInfo, createParams, properties)
            {
                Name = name;
                ValueType = valueType;
                Value = objectValue;
            }
        }

        /// <summary>
        /// 스크립트를 모듈 초기화 함수로 변환
        /// </summary>
        class ScriptModuleInitializer : IModuleInitializer
        {
            private readonly IEnumerable<ScriptRunner<object>> initializers;
            private readonly object param;

            /// <summary>
            /// 생성자
            /// </summary>
            /// <param name="initializers">스크립트 실행 정보</param>
            /// <param name="param">스크립트 호출 파라미터</param>
            public ScriptModuleInitializer(IEnumerable<ScriptRunner<object>> initializers, object param = null)
            {
                this.initializers = initializers;
                this.param = param;
            }

            /// <inheritdoc/>
            public void Initializer(IModule module, string moduleTypeName, object factoryParam = null)
            {
                if (initializers == null) return;

                foreach (var initializer in initializers)
                {
                    initializer?.Invoke(globals: new SourceParam() { module = module, param = this.param }).Wait();
                }
            }
        }

        /// <summary>
        /// Bootstrap 프로퍼티 정보를 실제 프로퍼티로 변환
        /// </summary>
        /// <param name="objectList">개체 목록</param>
        /// <param name="objectSet">개체 사전 정의 목록</param>
        /// <param name="isModule">모듈인가 여부</param>
        /// <param name="assemblyList">참조되는 어셈블리 정보</param>
        /// <param name="macrosList">매크로 리스트</param>
        /// <param name="securityKey">암호키</param>
        /// <returns>변환 된 프로퍼티 셋</returns>
        private static IDictionary<string, object> ProcessBootstrapProperties(IEnumerable<IObjectData> objectList, IDictionary<string, IObjectDefineData> objectSet, bool isModule = false, IList<Assembly> assemblyList = null, IDictionary<string, object> macrosList = null, string securityKey = null)
        {
            if (isModule)
            {
                logger?.Debug("Process Module Properties");
            }

            IDictionary<string, object> propMap;
            if (objectList?.Any() == true)
            {
                propMap = new Dictionary<string, object>();
                foreach (var prop in objectList)
                {
                    var value = ProcessBootstrapObject(prop, objectSet, assemblyList, macrosList, securityKey);
                    if (value != null)
                    {
                        string name = null;
                        if(value is KeyValuePair<string, object> dicItem)
                        {
                            name = dicItem.Key;
                            value = dicItem.Value;
                        } 
                        else if(value is ExtModuleObjectCreateInfo)
                        {
                            name = (value as ExtModuleObjectCreateInfo).Name;
                        }

                        if (name != null) 
                        {
                            if (propMap.ContainsKey(name))
                            {
                                propMap[name] = value;
                            }
                            else
                            {
                                propMap.Add(name, value);
                            }
                        }
                    }
                }
            }
            else
            {
                propMap = null;
            }

            return propMap;
        }

        /// <summary>
        /// Bootstrap 스크립트 코드 정보로부터 실제 실행 가능한 스크립트 생성
        /// </summary>
        /// <param name="moduleType">모듈 타입</param>
        /// <param name="codeData">스크립트 코드</param>
        /// <returns>스크립트</returns>
        private static (IEnumerable<string>, IEnumerable<string>) GenProcessScript(Type moduleType, CodeData codeData)
        {
            if (codeData == null || codeData.processes == null)
            {
                return (null, null);
            }

            var codes = new List<string>();
            foreach (var process in codeData.processes)
            {
                if (process.type.Equals("code/CSharp", StringComparison.Ordinal))
                {
                    string code = null;
                    if (!String.IsNullOrEmpty(process.path))
                    {
                        code = FileUtil.ReadTextFile(process.path, new ConsoleReporter());
                    }
                    if (code == null)
                    {
                        code = process.data;
                    }

                    if (!String.IsNullOrEmpty(code))
                    {
                        if (moduleType != null)
                        {
                            var methodDics = new Dictionary<string, object>() {
                                { "moduleTypeName", moduleType?.FullName },
                                { "code", code }
                            };
                            var genCode = MacroUtil.ProcessMacro(Properties.Resources.CSharpModuleScriptTemplate, methodDics);
                            codes.Add(genCode);
                        }
                        else
                        {
                            codes.Add(code);
                        }
                    }
                }
            }

            if (codes.Count <= 0)
            {
                return (null, null);
            }

            var imports = new List<string>();
            if (codeData.imports != null)
            {
                imports.AddRange(codeData.imports);
            }

            var imps = new string[]
            {
                "System", 
                "DevPlatform.Base",
                moduleType?.FullName
            };

            foreach (var imp in imps) 
            {
                if (!String.IsNullOrEmpty(imp) && !imports.Contains(imp))
                {
                    imports.Add(imp);
                }
            }

            return (codes, imports);
        }

        #region GetCurrentScriptOptions()
        static ScriptOptions scriptOptions = null;

        private static ScriptOptions GetCurrentScriptOptions()
        {
            if (scriptOptions == null)
            {
                var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

                //var execAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
                //assms = execAssemblies
                //    .Select(name => loadedAssemblies.SingleOrDefault(a => !String.IsNullOrEmpty(a.Location))?.Location)
                //    .Where(l => l != null);

                var assms = loadedAssemblies.Where(a => !a.IsDynamic && !String.IsNullOrEmpty(a.Location));

                scriptOptions = ScriptOptions.Default.WithReferences(assms);
            }

            return scriptOptions;
        }
        #endregion

        //private class CreateInstanceData
        //{
        //    public string name;
        //    public Type type;
        //    public object[] createparams;
        //    public IDictionary<string, object> properties;
        //}


        private static bool ProcessBootstrapModule(ModuleData module, IDictionary<string, IObjectDefineData> objectSet, IList<Assembly> assemblyList, IDictionary<string, object> macros, string securityKey)
        {
            var result = ProcessBootstrapObject(module, objectSet, assemblyList, macros, securityKey);
            if (!(result is ExtModuleObjectCreateInfo))
            {
                return false;
            }

            var createInstanceType = result as ExtModuleObjectCreateInfo;

            IList<ScriptRunner<object>> initializers = null;
            if (module.initializer != null)
            {
                initializers = new List<ScriptRunner<object>>();
                var (codes, imports) = GenProcessScript(createInstanceType.TypeInfo as Type, module.initializer);
                if (codes != null)
                {
                    try
                    {
                        var opt = GetCurrentScriptOptions().WithImports(imports);

                        foreach (var code in codes)
                        {
                            var script = CSharpScript.Create(code, opt, typeof(SourceParam));
                            script.Compile();
                            initializers.Add(script.CreateDelegate());
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.Error(ex.Message + " : " + ex.InnerException.Message);
                        return false;
                    }
                }
            }

            var registName = ModuleFactory.Instance.RegistModule(createInstanceType.Name,
                createInstanceType.TypeInfo as Type, createInstanceType.CreateParams, createInstanceType.Properties,
                (initializers?.Any() == true) ? new ScriptModuleInitializer(initializers, null) : null,
                BootstrapHelper.DefaultBootstrapFillValues);

            if (String.IsNullOrEmpty(registName))
            {
                return false;
            }

            logger?.Trace($"Regist Modules : {registName}");

            if (!String.IsNullOrEmpty(module.instance))
            {
                if(module.instance.Equals("*", StringComparison.Ordinal))
                {
                    module.instance = $"Instance.{registName}";
                }

                if (!SystemProperties.Instance.TryGetProperty(module.instance, out var inst))
                {
                    inst = ModuleFactory.Instance.CreateModuleInstance(registName);
                    if (inst != null) 
                    {
                        SystemProperties.Instance.SetProperty(module.instance, inst);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Bootstrap Data를 이용해 ModuleFactory를 초기화시킵니다.
        /// </summary>
        /// <param name="bootstrapData">Bootstrap Data</param>
        /// <param name="isOverriding">중복 된 항목을 겹쳐 쓰는가 여부</param>
        /// <param name="macros">macro 사전</param>
        /// <param name="securityKey">암호 키</param>
        /// <param name="currentAssembly">현재 Bootstrap</param>
        /// <returns>성공시 true, 실패시 false</returns>
        public static bool ProcessBootstrap(BootstrapData bootstrapData, bool isOverriding = true,
            Dictionary<string, object> macros = null, string securityKey = null, Assembly currentAssembly = null)
        {
            if (bootstrapData == null)
            {
                return false;
            }

            logger?.Trace("Start Bootstrap process...");

            IDictionary<string, IObjectDefineData> objectSet = null;
            if (bootstrapData.objectDefs != null)
            {
                objectSet = new Dictionary<string, IObjectDefineData>();
                foreach (var obj in bootstrapData.objectDefs)
                {
                    objectSet.Add(obj.name, obj);
                }
            }

            var assemblyList = new List<Assembly>();

            var registModules = new List<(ModuleData, Type, object)>();
            if (bootstrapData.modules != null)
            {
                if (currentAssembly != null)
                {
                    if (macros == null)
                    {
                        macros = new Dictionary<string, object>();
                    }
                    macros.Add("CurrentAssemblyName", currentAssembly.GetName().Name);
                }

                foreach (var module in bootstrapData.modules)
                {
                    var mod = ModuleFactory.Instance.FindModuleFactoryItem(module.name);
                    if (mod != null && !isOverriding)
                    {
                        continue;
                    }

                    var result = ProcessBootstrapModule(module, objectSet, assemblyList, macros, securityKey);

                    if (!result)
                    {
                        logger?.Trace($"Regist Modules Fail : {module.name}");
                    }
                }
            }

            // Initialize System Properties
            if (bootstrapData.systemProperties != null)
            {
                foreach (var property in bootstrapData.systemProperties)
                {
                    if (property.name == null) continue;

                    string value;
                    if (!String.IsNullOrEmpty(property.encryptedValue))
                    {
                        if (String.IsNullOrEmpty(securityKey))
                        {
                            throw new ArgumentNullException(nameof(securityKey));
                        }
                        value = Cryptor.DecryptString(property.encryptedValue, securityKey);
                    }
                    else
                    {
                        value = property.value;
                    }
                    SystemProperties.Instance.SetProperty(property.name, value);
                    logger?.Trace($"Set System Property : {property.name} = {value}");
                }
            }

            // Execute Startup Codes
            if (bootstrapData.startup != null)
            {
                logger?.Trace("Execute Startup script...");

                var (codes, imports) = GenProcessScript(null, bootstrapData.startup);
                if (codes != null)
                {
                    try
                    {
                        var opt = GetCurrentScriptOptions().WithImports(imports);

                        foreach (var code in codes)
                        {
                            CSharpScript.EvaluateAsync(code, opt, null).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.Error(ex.BuildExceptionMessage());
                        return false;
                    }
                }
            }

            logger?.Trace("Bootstrap process all done.");

            return true;
        }

        private static Type GetObjectType(ITypeData typeData, IList<Assembly> assemblyList, IDictionary<string, object> macros)
        {
            if (String.IsNullOrEmpty(typeData.type))
            {
                return null;
            }

            Assembly assembly;
            if (!String.IsNullOrEmpty(typeData.assembly))
            {
                var assemblyName = typeData.assembly;

                if (macros != null)
                {
                    assemblyName = MacroUtil.ProcessMacro(assemblyName, macros);
                }

                assembly = ModuleHelper.FindAssemblyByName(assemblyName);
                if (assembly == null)
                {
                    return null;
                }
            }
            else
            {
                assembly = Assembly.GetAssembly(typeof(System.String));
            }

            var typeName = typeData.type;

            if (macros != null)
            {
                typeName = MacroUtil.ProcessMacro(typeName, macros);
            }

            if (typeData.generics?.Any() == true)
            {
                typeName = $"{typeName}`{typeData.generics.Count()}";
            }

            var type = assembly.GetType(typeName);
            if (type == null)
            {
                return null;
            }

            if (!assemblyList.Contains(assembly))
            {
                assemblyList.Add(assembly);
            }

            return type;
        }

        private static void InheritInstanceData(IInstanceData newObjectData, IInstanceData objectData, IInstanceData parentObj)
        {
            newObjectData.instance = (!String.IsNullOrEmpty(objectData.instance)) ? objectData.instance : parentObj.instance;

            newObjectData.assembly = (!String.IsNullOrEmpty(objectData.assembly)) ? objectData.assembly : parentObj.assembly;
            newObjectData.type = (!String.IsNullOrEmpty(objectData.type)) ? objectData.type : parentObj.type;
            newObjectData.generics = objectData.generics ?? parentObj.generics;

            newObjectData.createParams = objectData.createParams ?? parentObj.createParams;

            if (parentObj.properties?.Any() == true)
            {
                if (objectData.properties?.Any() == true)
                {
                    var lst = new List<IObjectData>();
                    lst.AddRange(parentObj.properties);
                    lst.AddRange(objectData.properties);
                    newObjectData.properties = lst;
                }
                else
                {
                    newObjectData.properties = parentObj.properties;
                }
            }
            else
            {
                newObjectData.properties = objectData.properties;
            }
        }

        private static (Type, object) ProcessBootstrapInstanceData(IInstanceData instData, bool isModule, IDictionary<string, IObjectDefineData> objectSet, IList<Assembly> assemblyList, IDictionary<string, object> macros, string securityKey, bool createInstance = false, string instanceName = null, string methodName = null)
        {
            object inst;
            Type type;
            if (!String.IsNullOrEmpty(instData.instance) && !isModule)
            {
                inst = SystemProperties.Instance.GetProperty(instData.instance);
                type = inst?.GetType();
            }
            else
            {
                type = GetObjectType(instData, assemblyList, macros);
                if (type != null)
                {
                    if (type.IsGenericType && !type.IsConstructedGenericType)
                    {
                        if (instData.generics?.Any() == true)
                        {
                            var lst = new List<Type>();
                            foreach (var gen in instData.generics)
                            {
                                var genType = GetObjectType(gen, assemblyList, macros);
                                if (genType == null)
                                {
                                    return (null, null);
                                }
                                lst.Add(genType);
                            }
                            type = type.MakeGenericType(lst.ToArray());
                        }
                        else
                        {
                            return (null, null);
                        }
                    }
                }

                if (!String.IsNullOrEmpty(methodName))
                {
                    var method = type.GetMethod(methodName);
                    if(method.IsStatic)
                    {
                        return (type, null);
                    }
                }

                IEnumerable<object> createparams = null;
                if (instData.createParams?.Any() == true)
                {
                    var lst = new List<object>();
                    foreach (var createParam in instData.createParams)
                    {
                        lst.Add(ProcessBootstrapObject(createParam, objectSet, assemblyList, macros, securityKey));
                    }
                    createparams = lst;
                }

                IDictionary<string, object> properties = null;
                if (instData.properties?.Any() == true)
                {
                    properties = ProcessBootstrapProperties(instData.properties, objectSet, true, assemblyList, macros, securityKey);
                }

                if (createInstance && type != null)
                {
                    if (!type.IsAbstract)
                    {
                        inst = ((createparams != null) ? Activator.CreateInstance(type, createparams.ToArray<object>()) :
                                                         Activator.CreateInstance(type));
                        if (properties?.Any() == true)
                        {
                            BootstrapHelper.DefaultBootstrapFillValues(inst, type, properties, BootstrapHelper.DefaultBootstrapFillValues);
                        }
                    }
                    else
                    {
                        inst = null;
                    }
                } 
                else
                {
                    inst = new ExtModuleObjectCreateInfo(instanceName, type, createparams, properties);
                }
            }

            return (type, inst);
        }

        private static object ProcessBootstrapObject(IObjectData objectData, IDictionary<string, IObjectDefineData> objectSet, IList<Assembly> assemblyList, IDictionary<string, object> macros, string securityKey)
        {
            if (objectSet != null && !String.IsNullOrEmpty(objectData.parentObject))
            {
                if (objectSet.TryGetValue(objectData.parentObject, out var parentObj))
                {
                    var newObjectData = new ObjectData() { name = objectData.name };
                    InheritInstanceData(newObjectData, objectData, parentObj);

                    newObjectData.encryptedValue = objectData.encryptedValue ?? parentObj.encryptedValue;
                    newObjectData.customGetter = objectData.customGetter ?? parentObj.customGetter;
                    newObjectData.value = objectData.value ?? parentObj.value;
                    newObjectData.values = objectData.values ?? parentObj.values;
                    newObjectData.objectValue = objectData.objectValue ?? parentObj.objectValue;
                    newObjectData.objectValues = objectData.objectValues ?? parentObj.objectValues;

                    objectData = newObjectData;
                }
                else
                {
                    logger?.Warn($"Not defined - {objectData.parentObject}");
                }
            }

            if (objectData.encryptedValue != null)
            {
                if(String.IsNullOrEmpty(securityKey))
                {
                    throw new ArgumentNullException(nameof(securityKey));
                }
                objectData.value = Cryptor.DecryptString(objectData.encryptedValue, securityKey);
                objectData.encryptedValue = null;
            }

            if (objectData.value != null)
            {
                if (!String.IsNullOrEmpty(objectData.name))
                {
                    return new KeyValuePair<string, object>(objectData.name, objectData.value);
                }
                else
                {
                    return objectData.value;
                }
            }
            else if (objectData.values != null)
            {
                if (!String.IsNullOrEmpty(objectData.name))
                {
                    return new KeyValuePair<string, object>(objectData.name, objectData.values);
                }
                else
                {
                    return objectData.values;
                }
            }
            else if (objectData.objectValue != null)
            {
                var objValue = ProcessBootstrapObject(objectData.objectValue, objectSet, assemblyList, macros, securityKey);
                if (!String.IsNullOrEmpty(objectData.name))
                {
                    return new KeyValuePair<string, object>(objectData.name, objValue);
                }
                else
                {
                    return objValue;
                }
            }
            else if (objectData.objectValues != null)
            {
                var lst = new List<object>();
                foreach (var obj in objectData.objectValues) 
                {
                    var result = ProcessBootstrapObject(obj, objectSet, assemblyList, macros, securityKey);
                    if(result != null)
                    {
                        lst.Add(result);
                    }
                }
                if (!String.IsNullOrEmpty(objectData.name))
                {
                    return new KeyValuePair<string, object>(objectData.name, lst);
                }
                else
                {
                    return lst;
                }
            } 
            else if (objectData.customGetter != null)
            {
                var objValue = ProcessBootstrapCaller(objectData.customGetter, objectSet, assemblyList, macros, securityKey);
                if (!String.IsNullOrEmpty(objectData.name))
                {
                    return new KeyValuePair<string, object>(objectData.name, objValue);
                }
                else
                {
                    return objValue;
                }
            }

            var (_, inst) = ProcessBootstrapInstanceData(objectData, true, objectSet, assemblyList, macros, securityKey, false, objectData.name);

            return inst;
        }

        private static object ProcessBootstrapCaller(ICallerData customGetter, IDictionary<string, IObjectDefineData> objectSet, IList<Assembly> assemblyList, IDictionary<string, object> macros, string securityKey)
        {
            if (objectSet != null && !String.IsNullOrEmpty(customGetter.parentObject))
            {
                if (objectSet.TryGetValue(customGetter.parentObject, out var parentObj))
                {
                    var newObjectData = new CallerData();
                    InheritInstanceData(newObjectData, customGetter, parentObj);

                    newObjectData.property = (!String.IsNullOrEmpty(customGetter.property)) ? customGetter.property : parentObj.property;
                    newObjectData.method = (!String.IsNullOrEmpty(customGetter.method)) ? customGetter.method : parentObj.method;
                    newObjectData.callParams = customGetter.callParams ?? parentObj.callParams;

                    customGetter = newObjectData;
                }
                else
                {
                    logger?.Warn($"Not defined - {customGetter.parentObject}");
                }
            }

            var (type, inst) = ProcessBootstrapInstanceData(customGetter, false, objectSet, assemblyList, macros, securityKey, true, null, customGetter.method);

            if (!String.IsNullOrEmpty(customGetter.property))
            {
                var property = type?.GetProperty(customGetter.property);
                if (property?.CanRead != true)
                {
                    return null;
                }

                return property.GetValue(inst);
            }
            else
            {
                var method = type?.GetMethod(customGetter.method);
                if (method == null)
                {
                    return null;
                }

                IEnumerable<object> callparams = null;
                if (customGetter.callParams?.Any() == true)
                {
                    var lst = new List<object>();
                    foreach (var callparam in customGetter.callParams)
                    {
                        lst.Add(ProcessBootstrapObject(callparam, objectSet, assemblyList, macros, securityKey));
                    }
                    callparams = lst;
                }

                return method.Invoke(inst, callparams?.ToArray());
            }
        }
    }
}
