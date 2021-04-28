using System;
using System.Collections.Generic;

namespace DevPlatform.Base
{
    /// <summary>
    /// IModule 인터페이스.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        /// Module의 식별 이름
        /// </summary>
        string ModuleSignature { get; }

        /// <summary>
        /// 외부에서 호출하기 위한 인스턴스 초기화 함수.
        /// 외부에서 Module Property들을 설정한 이후에 호출됩니다.
        /// 주로 ModuleFactory를 통해 Module이 생성 될 때 Factory에 의해 호출됩니다.
        /// </summary>
        /// <param name="moduleTypeName">ModuleFactory에 등록 된 Module Type 이름</param>
        /// <param name="factoryParam">ModuleFactory에 등록 된 Module 파라미터</param>
        void InitializeInstance(string moduleTypeName, object factoryParam = null);
    }
}
