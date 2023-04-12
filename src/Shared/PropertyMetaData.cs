using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SourceGenerator.Analyzers.MetaData
{
    public class PropertyMetaData: MetaDataBase
    {
        public PropertyMetaData(string name, List<AttributeMetaData> attributeMetaData, List<string> originalDescription, string accessModifier, string extModifier) : base(name, accessModifier, extModifier, attributeMetaData)
        {
            OriginalDescription = originalDescription;

            Description = GetStringParam(AttributeMetaData, "Display", "Name")?.Trim('"');

            if (string.IsNullOrWhiteSpace(Description))
            {
                Description = AttributeMetaData
                    .FirstOrDefault(d => d.Name == "DisplayName" || d.Name == "DisplayNameAttribute")?.ParamDictionary
                    .FirstOrDefault().Value;
            }

            if (string.IsNullOrWhiteSpace(Description) && OriginalDescription != null && OriginalDescription.Any())
            {
                var str = string.Join("", OriginalDescription).Replace("\r\n", "");
                var regex = new Regex($"{"<summary>"}(.*?){"</summary>"}");
                var match = regex.Match(str.Replace("\r\n", ""));
                if (match.Success)
                {
                    Description = match.Groups[match.Groups.Count - 1].Value.Trim().TrimStart('/').TrimEnd('/').Trim();
                }
            }
        }

        /// <summary>
        /// 属性描述
        /// </summary>
        public string Description { get; private set; }

        public List<string> OriginalDescription { get; private set; }

        public static string GetStringParam(List<AttributeMetaData> attributeMetaData, string attributeName, string key)
        {
            if (!attributeMetaData.Any()) return null;
            return attributeMetaData.FirstOrDefault(d => d.Name == attributeName)?.GetStringParam(key);
        }
    }
}
