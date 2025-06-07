using System;
using System.Runtime.Versioning;
using LuckyStars.Utils;

namespace LuckyStars.Players.Settings
{
    /// <summary>
    /// 视频播放器设置类
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class VideoPlayerSettings : IPlayerSettings
    {
        // 注册表键名常量
        private const string LOOP_ENABLED_KEY = "VideoLoopEnabled";
        private const string VOLUME_KEY = "VideoVolume";

        // 默认值
        private const bool DEFAULT_LOOP_ENABLED = true;
        private const float DEFAULT_VOLUME = 0.2f;

        // 属性
        private bool _loopEnabled;
        private float _volume;

        /// <summary>
        /// 是否启用循环播放
        /// </summary>
        public bool LoopEnabled
        {
            get => _loopEnabled;
            set
            {
                if (_loopEnabled != value)
                {
                    _loopEnabled = value;
                    Save();
                }
            }
        }

        /// <summary>
        /// 音量（0.0-1.0）
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                // 确保值在有效范围内
                float clampedValue = Math.Clamp(value, 0.0f, 1.0f);
                if (_volume != clampedValue)
                {
                    _volume = clampedValue;
                    Save();
                }
            }
        }

        /// <summary>
        /// 获取设置的唯一标识符
        /// </summary>
        public string SettingsId => "VideoPlayer";

        /// <summary>
        /// 构造函数
        /// </summary>
        public VideoPlayerSettings()
        {
            // 设置默认值
            _loopEnabled = DEFAULT_LOOP_ENABLED;
            _volume = DEFAULT_VOLUME;

            // 加载设置
            Load();
        }

        /// <summary>
        /// 保存设置到注册表
        /// </summary>
        public void Save()
        {
            // 保存循环播放设置
            RegistryManager.SaveValue(LOOP_ENABLED_KEY, _loopEnabled ? 1 : 0);
            
            // 保存音量设置（转换为整数百分比）
            int volumePercent = (int)(_volume * 100);
            RegistryManager.SaveValue(VOLUME_KEY, volumePercent);
        }

        /// <summary>
        /// 从注册表加载设置
        /// </summary>
        public void Load()
        {
            // 加载循环播放设置
            int loopEnabledValue = RegistryManager.LoadValue(LOOP_ENABLED_KEY, DEFAULT_LOOP_ENABLED ? 1 : 0);
            _loopEnabled = loopEnabledValue == 1;

            // 加载音量设置
            int volumePercent = RegistryManager.LoadValue(VOLUME_KEY, (int)(DEFAULT_VOLUME * 100));
            _volume = Math.Clamp(volumePercent / 100.0f, 0.0f, 1.0f);
        }
    }
}
