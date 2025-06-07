using System;

namespace LuckyStars.Players.Settings
{
    /// <summary>
    /// 播放器设置接口，定义所有播放器共有的设置属性
    /// </summary>
    public interface IPlayerSettings
    {
        /// <summary>
        /// 获取设置的唯一标识符
        /// </summary>
        string SettingsId { get; }

        /// <summary>
        /// 保存设置到持久化存储
        /// </summary>
        void Save();

        /// <summary>
        /// 从持久化存储加载设置
        /// </summary>
        void Load();
    }
}
