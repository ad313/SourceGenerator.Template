//using Newtonsoft.Json;
//using Newtonsoft.Json.Serialization;
//using SourceGenerator.Analyzers.MetaData;
//using System;
//using System.IO;

//namespace SourceGenerator.Template
//{
//    public static class MetaDataHelper
//    {
//        /// <summary>
//        /// 读取json，转换元数据
//        /// </summary>
//        /// <param name="path"></param>
//        /// <returns></returns>
//        /// <exception cref="FileNotFoundException"></exception>
//        /// <exception cref="Exception"></exception>
//        public static AssemblyMetaData LoadJson(string path)
//        {
//            if (!System.IO.File.Exists(path))
//                throw new FileNotFoundException(nameof(path));

//            var json = File.ReadAllText(path).TrimStart('/');
//            if (string.IsNullOrWhiteSpace(json))
//                throw new Exception("读取json内容为空");

//            return JsonConvert.DeserializeObject<AssemblyMetaData>(json, new JsonSerializerSettings()
//            {
//                ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() }
//            });
//        }
//    }
//}