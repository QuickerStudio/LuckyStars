using System;
using System.Runtime.Versioning;
using LuckyStars.Utils;

namespace LuckyStars.Players.Settings
{
    /// <summary>
    /// 图片播放器设置类
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class PicturePlayerSettings : IPlayerSettings
    {
        // 注册表键名常量
        private const string TRANSITION_ENABLED_KEY = "PictureTransitionEnabled";
        private const string TRANSITION_DURATION_KEY = "PictureTransitionDuration";

        // 默认值
        private const bool DEFAULT_TRANSITION_ENABLED = true;
        private const float DEFAULT_TRANSITION_DURATION = 0.3f; // 秒

        // 属性
        private bool _transitionEnabled;
        private float _transitionDuration;

        /// <summary>
        /// 是否启用过渡动画
        /// </summary>
        public bool TransitionEnabled
        {
            get => _transitionEnabled;
            set
            {
                if (_transitionEnabled != value)
                {
                    _transitionEnabled = value;
                    Save();
                }
            }
        }

        /// <summary>
        /// 过渡动画持续时间（秒）
        /// </summary>
        public float TransitionDuration
        {
            get => _transitionDuration;
            set
            {
                // 确保值在有效范围内（最小0.1秒，最大2秒）
                float clampedValue = Math.Clamp(value, 0.1f, 2.0f);
                if (_transitionDuration != clampedValue)
                {
                    _transitionDuration = clampedValue;
                    Save();
                }
            }
        }

        /// <summary>
        /// 获取设置的唯一标识符
        /// </summary>
        public string SettingsId => "PicturePlayer";

        /// <summary>
        /// 构造函数
        /// </summary>
        public PicturePlayerSettings()
        {
            // 设置默认值
            _transitionEnabled = DEFAULT_TRANSITION_ENABLED;
            _transitionDuration = DEFAULT_TRANSITION_DURATION;

            // 加载设置
            Load();
        }

        /// <summary>
        /// 保存设置到注册表
        /// </summary>
        public void Save()
        {
            // 保存过渡动画设置
            RegistryManager.SaveValue(TRANSITION_ENABLED_KEY, _transitionEnabled ? 1 : 0);
            
            // 保存过渡动画持续时间设置（转换为整数毫秒）
            int durationMs = (int)(_transitionDuration * 1000);
            RegistryManager.SaveValue(TRANSITION_DURATION_KEY, durationMs);
        }

        /// <summary>
        /// 从注册表加载设置
        /// </summary>
        public void Load()
        {
            // 加载过渡动画设置
            int transitionEnabledValue = RegistryManager.LoadValue(TRANSITION_ENABLED_KEY, DEFAULT_TRANSITION_ENABLED ? 1 : 0);
            _transitionEnabled = transitionEnabledValue == 1;

            // 加载过渡动画持续时间设置
            int durationMs = RegistryManager.LoadValue(TRANSITION_DURATION_KEY, (int)(DEFAULT_TRANSITION_DURATION * 1000));
            _transitionDuration = Math.Clamp(durationMs / 1000.0f, 0.1f, 2.0f);
        }
    }
}
