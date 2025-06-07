namespace LuckyStars.Utils
{
    /// <summary>
    /// 动画提供者接口，用于提供不同类型的动画HTML内容
    /// </summary>
    public interface IAnimationProvider
    {
        /// <summary>
        /// 获取HTML内容
        /// </summary>
        /// <returns>完整的HTML内容字符串</returns>
        string GetHtml();

        /// <summary>
        /// 获取动画名称
        /// </summary>
        /// <returns>动画名称</returns>
        string GetName();

        /// <summary>
        /// 获取动画描述
        /// </summary>
        /// <returns>动画描述</returns>
        string GetDescription();
    }
}
