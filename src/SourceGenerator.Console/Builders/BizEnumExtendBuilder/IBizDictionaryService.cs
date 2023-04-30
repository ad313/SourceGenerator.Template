using System.Collections.Generic;
using System.Threading.Tasks;

namespace SourceGenerator.Consoles.Builders.BizEnumExtendBuilder
{
    /// <summary>
    /// 业务字典服务
    /// </summary>
    public interface IBizDictionaryService
    {
        #region 字典

        /// <summary>
        /// 获取单个字典数据
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<List<DictionaryItem>> GetBizDictionary(string code);

        /// <summary>
        /// 批量获取字典
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        Task<Dictionary<string, List<DictionaryItem>>> GetBizDictionary(List<string> codes);

        #endregion

        #region 部门

        /// <summary>
        /// 获取单个部门数据
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<BizDataItem> GetBizDepartment(string code);

        /// <summary>
        /// 批量获取部门
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        Task<List<BizDataItem>> GetBizDepartment(List<string> codes);

        #endregion

        #region 用户

        /// <summary>
        /// 获取单个用户数据
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<BizDataItem> GetBizUser(string userId);

        /// <summary>
        /// 批量获取用户
        /// </summary>
        /// <param name="userIdList"></param>
        /// <returns></returns>
        Task<List<BizDataItem>> GetBizUser(List<string> userIdList);

        #endregion

        #region 行政区划

        /// <summary>
        /// 获取单个行政区划
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<BizDataItem> GetBizRegion(string code);

        /// <summary>
        /// 批量获取行政区划
        /// </summary>
        /// <param name="codes"></param>
        /// <returns></returns>
        Task<List<BizDataItem>> GetBizRegion(List<string> codes);

        #endregion
    }

    public class BizDataItem
    {
        public string Text { get; set; }

        public string Value { get; set; }
    }

    public class DictionaryItem
    {
        /// <summary>
        /// Code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Level
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// ParentId
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Path
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// SortId
        /// </summary>
        public int SortId { get; set; }

        /// <summary>
        /// Remark
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public string Value { get; set; }

    }

}