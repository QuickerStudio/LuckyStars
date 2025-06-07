using System;
using System.Runtime.Versioning;
using LuckyStars.Utils;

namespace LuckyStars.Players.Settings
{
    /// <summary>
    /// 交互式播放器设置类
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class InteractivePlayerSettings : IPlayerSettings
    {
        // 注册表键名常量
        private const string AUTO_REFRESH_ENABLED_KEY = "WebViewAutoRefreshEnabled";
        private const string REFRESH_INTERVAL_KEY = "WebViewRefreshInterval";

        // 默认值
        private const bool DEFAULT_AUTO_REFRESH_ENABLED = true;
        private const int DEFAULT_REFRESH_INTERVAL = 500; // 毫秒

        // 属性
        private bool _autoRefreshEnabled;
        private int _refreshInterval;

        /// <summary>
        /// 是否启用自动刷新
        /// </summary>
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (_autoRefreshEnabled != value)
                {
                    _autoRefreshEnabled = value;
                    Save();
                }
            }
        }

        /// <summary>
        /// 刷新间隔（毫秒）
        /// </summary>
        public int RefreshInterval
        {
            get => _refreshInterval;
            set
            {
                // 确保值在有效范围内（最小100毫秒，最大10秒）
                int clampedValue = Math.Clamp(value, 100, 10000);
                if (_refreshInterval != clampedValue)
                {
                    _refreshInterval = clampedValue;
                    Save();
                }
            }
        }

        /// <summary>
        /// 获取设置的唯一标识符
        /// </summary>
        public string SettingsId => "InteractivePlayer";

        /// <summary>
        /// 构造函数
        /// </summary>
        public InteractivePlayerSettings()
        {
            // 设置默认值
            _autoRefreshEnabled = DEFAULT_AUTO_REFRESH_ENABLED;
            _refreshInterval = DEFAULT_REFRESH_INTERVAL;

            // 加载设置
            Load();
        }

        /// <summary>
        /// 保存设置到注册表
        /// </summary>
        public void Save()
        {
            // 保存自动刷新设置
            RegistryManager.SaveValue(AUTO_REFRESH_ENABLED_KEY, _autoRefreshEnabled ? 1 : 0);
            
            // 保存刷新间隔设置
            RegistryManager.SaveValue(REFRESH_INTERVAL_KEY, _refreshInterval);
        }

        /// <summary>
        /// 从注册表加载设置
        /// </summary>
        public void Load()
        {
            // 加载自动刷新设置
            int autoRefreshEnabledValue = RegistryManager.LoadValue(AUTO_REFRESH_ENABLED_KEY, DEFAULT_AUTO_REFRESH_ENABLED ? 1 : 0);
            _autoRefreshEnabled = autoRefreshEnabledValue == 1;

            // 加载刷新间隔设置
            _refreshInterval = RegistryManager.LoadValue(REFRESH_INTERVAL_KEY, DEFAULT_REFRESH_INTERVAL);
            // 确保值在有效范围内
            _refreshInterval = Math.Clamp(_refreshInterval, 100, 10000);
        }
    }
}
