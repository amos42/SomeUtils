#if NETSTANDARD2_1
using System.Text.Json;
using System.Text.Json.Serialization;
#elif NETSTANDARD2_0
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif
using DevPlatform.Base;
using DevPlatform.CommonUtil;
using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using System.Reflection;
using System.Collections.Generic;
using DevPlatform.Bootstrap.BootstrapDataV1;
using NJsonSchema;
using System.Threading.Tasks;

namespace DevPlatform.Bootstrap
{
    /// <summary>
    /// Bootstrap Loader 구현체
    /// </summary>
    public static class BootstrapLoader
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(SystemConstants.SystemLogger);

        private class LoggerReporter : IReporter
        {
            private readonly ILogger logger;
            public LoggerReporter(ILogger logger)
            {
                this.logger = logger;
            }
            public void Report(int type, string message)
            {
                logger?.Debug(message);
            }
        }

        /// <summary>
        /// 스크립트 소스의 데이터 타입
        /// </summary>
        public enum DataType
        {
            /// <summary>
            /// 자동 선택
            /// </summary>
            AUTO, 

            /// <summary>
            /// Json 데이터
            /// </summary>
            JSON, 

            /// <summary>
            /// Xml 데이터
            /// </summary>
            XML
        }

        private static BootstrapData LoadFromJson(string data)
        {
#if NETSTANDARD2_1
            BootstrapData result = null;
            try
            {
                result = JsonSerializer.Deserialize<BootstrapData>(data);
            } 
            catch (JsonException ex)
            {
                logger?.Error(ex.Message);
                result = null;
            }

            return result;
#elif NETSTANDARD2_0
            BootstrapData result = null;
            try
            {
                result = JsonConvert.DeserializeObject<BootstrapData>(data, new JsonSerializerSettings() {
                    TypeNameHandling = TypeNameHandling.None
                });
            } 
            catch (Newtonsoft.Json.JsonSerializationException ex)
            {
                logger?.Error(ex.Message);
                result = null;
            }

            return result;
#endif
        }

        private static BootstrapData LoadFromJsonStream(Stream stream)
        {
            string data = null;
            StreamReader reader = null;
            try
            {
                reader = new StreamReader(stream);
                data = reader.ReadToEnd();
            } catch(IOException ex)
            {
                logger?.Error(ex.Message);
            }
            finally
            {
                reader?.Close();
            }
            var bootstrapData = LoadFromJson(data);
            return bootstrapData;
        }

        private static BootstrapData LoadFromXMLStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(BootstrapData));

            var result = serializer.Deserialize(stream) as BootstrapData;

            return result;
        }

        private static BootstrapData LoadFromXML(string data)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(data)))
            {
                return LoadFromXMLStream(stream);
            }
        }

        /// <summary>
        /// Bootstrap 스크립트의 무결성을 체크합니다.
        /// </summary>
        /// <param name="data">Bootstrap configure 데이터</param>
        /// <param name="dataType">데이터 타입</param>
        /// <returns>Bootstrap 스크립트의 무결성 여부</returns>
        public static bool IsValidate(string data, DataType dataType = DataType.AUTO)
        {
            if (dataType == DataType.AUTO)
            {
                var data2 = data.TrimStart();
                if (data2.StartsWith("{", StringComparison.Ordinal) && data2.TrimEnd().EndsWith("}", StringComparison.Ordinal))
                {
                    dataType = DataType.JSON;
                }
                else if (data2.StartsWith("<", StringComparison.Ordinal) && data2.TrimEnd().EndsWith(">", StringComparison.Ordinal))
                {
                    dataType = DataType.XML;
                }
                else
                {
                    return false;
                }
            }

            if (dataType == DataType.JSON)
            {
                var schema = Task.Run(async() => await JsonSchema.FromJsonAsync(Properties.Resources.BootstrapJsonValidator)).Result;
                var result = schema?.Validate(data);
                return result?.Count == 0;
            } 
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Bootstrap을 로드하여 ModuleFactory, SystemProperites 등을 초기화 합니다.
        /// </summary>
        /// <param name="data">Bootstrap configure 데이터</param>
        /// <param name="dataType">데이터 타입</param>
        /// <param name="currentPath">Bootstrap Data의 기준 path</param>
        /// <param name="isOverriding">중복 된 항목을 겹쳐 쓰는가 여부</param>
        /// <param name="macros">macro 사전</param>
        /// <param name="securityKey">암호화 키 (AES256)</param>
        /// <param name="currentAssembly">현재 Bootstrap</param>
        /// <returns>Load 성공시 true, 실패시 false</returns>
        public static bool Load(string data, DataType dataType = DataType.AUTO, string currentPath = null, bool isOverriding = true,
                                Dictionary<string, object> macros = null, string securityKey = null, Assembly currentAssembly = null)
        {
            if (data == null) return false;

            if (dataType == DataType.AUTO)
            {
                var data2 = data.TrimStart();
                if (data2.StartsWith("{", StringComparison.Ordinal) && data2.TrimEnd().EndsWith("}", StringComparison.Ordinal))
                {
                    dataType = DataType.JSON;
                }
                else if (data2.StartsWith("<", StringComparison.Ordinal) && data2.TrimEnd().EndsWith(">", StringComparison.Ordinal))
                {
                    dataType = DataType.XML;
                }
                else
                {
                    return false;
                }
            }

            BootstrapData bootstrapData = null;
            if (dataType == DataType.JSON)
            {
                bootstrapData = LoadFromJson(data);
            }
            else if (dataType == DataType.XML)
            {
                bootstrapData = LoadFromXML(data);
            }

            if (bootstrapData != null)
            {
                string oldPath = null;

                if (!String.IsNullOrEmpty(currentPath))
                {
                    oldPath = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(currentPath);
                }

                var result = Bootstrap.ProcessBootstrap(bootstrapData, isOverriding, macros, securityKey, currentAssembly);

                if (oldPath != null)
                {
                    Directory.SetCurrentDirectory(oldPath);
                }

                return result;
            }

            return false;
        }

        /// <summary>
        /// Bootstrap을 로드하여 ModuleFactory, SystemProperites 등을 초기화 합니다.
        /// </summary>
        /// <param name="fileName">Bootstrap configure 파일명</param>
        /// <param name="dataType">데이터 타입</param>
        /// <param name="currentPath">Bootstrap Data의 기준 path</param>
        /// <param name="isOverriding">중복 된 항목을 겹쳐 쓰는가 여부</param>
        /// <param name="macros">macro 사전</param>
        /// <param name="securityKey">암호화 키 (AES256)</param>
        /// <param name="currentAssembly">현재 Bootstrap</param>
        /// <returns>Load 성공시 true, 실패시 false</returns>
        public static bool LoadFromFile(string fileName, DataType dataType = DataType.AUTO, string currentPath = null, bool isOverriding = true,
                                        Dictionary<string, object> macros = null, string securityKey = null, Assembly currentAssembly = null)
        {
            if (String.IsNullOrEmpty(fileName)) return false;

            if(dataType == DataType.AUTO)
            {
                if (fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    dataType = DataType.JSON;
                }
                else if (fileName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    dataType = DataType.XML;
                }
                else
                {
                    return false;
                }
            }

            BootstrapData bootstrapData = null;
            if (dataType == DataType.JSON)
            {
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bootstrapData = LoadFromJsonStream(stream);
                }
            }
            else if (dataType == DataType.XML)
            {
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    bootstrapData = LoadFromXMLStream(stream);
                }
            }

            if (bootstrapData != null)
            {
                string oldPath = null;
                if (!String.IsNullOrEmpty(currentPath))
                {
                    oldPath = Directory.GetCurrentDirectory();
                    Directory.SetCurrentDirectory(currentPath);
                }

                var result = Bootstrap.ProcessBootstrap(bootstrapData, isOverriding, macros, securityKey, currentAssembly);

                if (oldPath != null)
                {
                    Directory.SetCurrentDirectory(oldPath);
                }

                return result;
            }

            return false;
        }

        /// <summary>
        /// 암호화 된 bootstrap script 파일을 읽어서 bootsctrip 단계를 처리합니다.
        /// </summary>
        /// <param name="fileName">암호화 된 bootstrap 파일</param>
        /// <param name="securityKey">암호화 키 (AES256)</param>
        /// <param name="dataType">데이터 타입</param>
        /// <param name="currentPath">Bootstrap Data의 기준 path</param>
        /// <param name="isOverriding">중복 된 항목을 겹쳐 쓰는가 여부</param>
        /// <param name="macros">macro 사전</param>
        /// <param name="currentAssembly">현재 Bootstrap</param>
        /// <returns>Load 성공시 true, 실패시 false</returns>
        public static bool LoadFromEncryptFile(string fileName, string securityKey, DataType dataType = DataType.AUTO, string currentPath = null, bool isOverriding = true,
                                        Dictionary<string, object> macros = null, Assembly currentAssembly = null)
        {
            var data = FileUtil.ReadTextFile(fileName, new LoggerReporter(logger));
            if (data == null) return false;

            string decryptedText = Cryptor.DecryptString(data, securityKey);

            return Load(decryptedText, dataType, currentPath, isOverriding, macros, securityKey, currentAssembly);
        }
    }
}
