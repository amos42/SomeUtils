using System;
using System.Text;
using DevPlatform.Bootstrap.BootstrapDataTypes;
using System.Collections.Generic;
#if NETSTANDARD2_1
using System.Text.Json.Serialization;
#elif NETSTANDARD2_0
using Newtonsoft.Json;
#endif
using System.Xml.Serialization;

namespace DevPlatform.Bootstrap.BootstrapDataV1
{
#pragma warning disable IDE1006 // 명명 스타일
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.

    //[Serializable()]
    public class ProcessData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute()]
        public string type { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute()]
        public string order { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute()]
        public string path { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlText()]
        public string data { get; set; }
    }

    //[Serializable()]
    public class CodeData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlArray(), XmlArrayItem("import", typeof(string))]
        public string[] imports { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlArray(), XmlArrayItem("process", typeof(ProcessData))]
        public ProcessData[] processes { get; set; }
    }

    //[Serializable()]
    public class TypeData : ITypeData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute()]
        public string instance { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string assembly { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string type { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<ITypeData> generics { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("generics")]
#elif NETSTANDARD2_0
        [JsonProperty("generics")]
#endif
        [XmlArray("generics"), XmlArrayItem("generic", typeof(TypeData))]
        public TypeData[] __generics { set => generics = value; get => null; }
    }

    //[Serializable()]
    public class InstanceData : IInstanceData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute("name")]
        public string name { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute("instance")]
        public string instance { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string parentObject { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string assembly { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string type { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<ITypeData> generics { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("generics")]
#elif NETSTANDARD2_0
        [JsonProperty("generics")]
#endif
        [XmlArray("generics"), XmlArrayItem("generic", typeof(TypeData))]
        public TypeData[] __generics { set => generics = value; get => null; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> createParams { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("createParams")]
#elif NETSTANDARD2_0
        [JsonProperty("createParams")]
#endif
        [XmlArray("createParams"), XmlArrayItem("param", typeof(ObjectData))]
        public ObjectData[] __createParams { set => createParams = value; get => null; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> properties { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("properties")]
#elif NETSTANDARD2_0
        [JsonProperty("properties")]
#endif
        [XmlArray("properties"), XmlArrayItem("property", typeof(ObjectData))]
        public ObjectData[] __properties { set => properties = value; get => null; }
    }

    //[Serializable()]
    public class ObjectData : InstanceData, IObjectData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute("encryptedValue")]
        public string encryptedValue { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlAttribute("value")]
        public string value { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<string> values { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("values")]
#elif NETSTANDARD2_0
        [JsonProperty("values")]
#endif
        [XmlArray("values"), XmlArrayItem("value", typeof(string))]
        public string[] __values { set => values = value; get => null; }

        [XmlIgnore, JsonIgnore]
        public IObjectData objectValue { get; set; }

        [XmlIgnore, JsonIgnore]
        public ICallerData customGetter { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("objectValue")]
#elif NETSTANDARD2_0
        [JsonProperty("objectValue")]
#endif
        [XmlElement("objectValue")]
        public ObjectData __objectValue { set => objectValue = value; get => null; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> objectValues { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("objectValues")]
#elif NETSTANDARD2_0
        [JsonProperty("objectValues")]
#endif
        [XmlArray("objectValues"), XmlArrayItem("object", typeof(ObjectData))]
        public ObjectData[] __objectValues { set => objectValues = value; get => null; }

#if NETSTANDARD2_1
        [JsonPropertyName("customGetter")]
#elif NETSTANDARD2_0
        [JsonProperty("customGetter")]
#endif
        [XmlElement("customGetter")]
        public CallerData __customGetter { set => customGetter = value; get => null; }
    }

    //[Serializable()]
    public class CallerData : InstanceData, ICallerData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string property { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string method { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> callParams { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("createParams")]
#elif NETSTANDARD2_0
        [JsonProperty("callParams")]
#endif
        [XmlArray("callParams"), XmlArrayItem("param", typeof(ObjectData))]
        public ObjectData[] __callParams { set => callParams = value; get => null; }
    }

    //[Serializable()]
    public class ObjectDefineData : ObjectData, IObjectDefineData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string property { get; set; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public string method { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> callParams { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("createParams")]
#elif NETSTANDARD2_0
        [JsonProperty("callParams")]
#endif
        [XmlArray("callParams"), XmlArrayItem("param", typeof(ObjectData))]
        public ObjectData[] __callParams { set => callParams = value; get => null; }
    }

    //[Serializable()]
    public class ModuleData : ObjectData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public CodeData initializer { get; set; }
    }

    //[Serializable()]
    [XmlRoot("bootstrap")]
    public class BootstrapData
    {
#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty("version")]
#endif
        [XmlElement("version")]
        public string version { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectDefineData> objectDefs { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("object.defines")]
#elif NETSTANDARD2_0
        [JsonProperty("object.defines")]
#endif
        [XmlArray("object.defines"), XmlArrayItem("object", typeof(ObjectDefineData))]
        public ObjectDefineData[] __objectDefs { set => objectDefs = value; get => null; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlArray("modules"), XmlArrayItem("module", typeof(ModuleData))]
        public ModuleData[] modules { get; set; }

        [XmlIgnore, JsonIgnore]
        public IEnumerable<IObjectData> systemProperties { get; set; }

#if NETSTANDARD2_1
        [JsonPropertyName("system.properties")]
#elif NETSTANDARD2_0
        [JsonProperty("system.properties")]
#endif
        [XmlArray("system.properties"), XmlArrayItem("property", typeof(ObjectData))]
        public ObjectData[] __systemProperties { set => systemProperties = value; get => null; }

#if NETSTANDARD2_1
#elif NETSTANDARD2_0
        [JsonProperty()]
#endif
        [XmlElement()]
        public CodeData startup { get; set; }
    }

#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
#pragma warning restore IDE1006 // 명명 스타일
}
