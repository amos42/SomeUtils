using System;
using System.Text;
using System.Collections.Generic;

namespace DevPlatform.Bootstrap.BootstrapDataTypes
{
#pragma warning disable IDE1006 // 명명 스타일
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.

    /// <summary>
    /// Type 기술
    /// </summary>
    public interface ITypeData
    {
        string assembly { get; set; }

        string type { get; set; }

        IEnumerable<ITypeData> generics { get; set; }
    }

    /// <summary>
    /// Type을 기반으로 생성할 Instance 기술
    /// </summary>
    public interface IInstanceData : ITypeData
    {
        string instance { get; set; }

        IEnumerable<IObjectData> createParams { get; set; }

        IEnumerable<IObjectData> properties { get; set; }
    }

    /// <summary>
    /// 개체 내의 Caller와 호출 파라미터를 기술
    /// </summary>
    public interface ICallerData : IInstanceData
    {
        string parentObject { get; set; }

        string property { get; set; }

        string method { get; set; }

        IEnumerable<IObjectData> callParams { get; set; }
    }

    /// <summary>
    /// Value 기술
    /// </summary>
    public interface IValueData
    {
        string value { get; set; }

        IEnumerable<string> values { get; set; }

        IObjectData objectValue { get; set; }

        IEnumerable<IObjectData> objectValues { get; set; }

        string encryptedValue { get; set; }

        ICallerData customGetter { get; set; }
    }

    /// <summary>
    /// Instance과 Value를 포함한 개체 기술
    /// </summary>
    public interface IObjectData : IInstanceData, IValueData
    {
        string name { get; set; }

        string parentObject { get; set; }
    }

    /// <summary>
    /// object와 caller를 포함한 개체 정의 기술
    /// </summary>
    public interface IObjectDefineData : IObjectData, ICallerData
    {
    }

#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
#pragma warning restore IDE1006 // 명명 스타일
}
