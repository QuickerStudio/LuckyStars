using System;
using System.Runtime.Versioning;
using LuckyStars.Utils;

namespace LuckyStars.Players.Settings
{
    /// <summary>
    /// 音乐播放器设置类
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class MusicPlayerSettings : IPlayerSettings
    {
        // 注册表键名常量
        private const string SURROUND_ENABLED_KEY = "MusicSurroundEnabled";
        private const string LOOP_ENABLED_KEY = "MusicLoopEnabled";
        private const string SURROUND_DEPTH_KEY = "MusicSurroundDepth";

        // 默认值
        private const bool DEFAULT_SURROUND_ENABLED = true;
        private const bool DEFAULT_LOOP_ENABLED = true;
        private const float DEFAULT_SURROUND_DEPTH = 0.5f;

        // 属性
        private bool _surroundEnabled;
        private bool _loopEnabled;
        private float _surroundDepth;

        /// <summary>
        /// 是否启用环绕音效果
        /// </summary>
        public bool SurroundEnabled
        {
            get => _surroundEnabled;
            set
            {
                if (_surroundEnabled != value)
                {
                    _surroundEnabled = value;
                    Save();
                }
            }
        }

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
        /// 环绕音效果深度（0.0-1.0）
        /// </summary>
        public float SurroundDepth
        {
            get => _surroundDepth;
            set
            {
                // 确保值在有效范围内
                float clampedValue = Math.Clamp(value, 0.0f, 1.0f);
                if (_surroundDepth != clampedValue)
                {
                    _surroundDepth = clampedValue;
                    Save();
                }
            }
        }

        /// <summary>
        /// 获取设置的唯一标识符
        /// </summary>
        public string SettingsId => "MusicPlayer";

        /// <summary>
        /// 构造函数
        /// </summary>
        public MusicPlayerSettings()
        {
            // 设置默认值
            _surroundEnabled = DEFAULT_SURROUND_ENABLED;
            _loopEnabled = DEFAULT_LOOP_ENABLED;
            _surroundDepth = DEFAULT_SURROUND_DEPTH;

            // 加载设置
            Load();
        }

        /// <summary>
        /// 保存设置到注册表
        /// </summary>
        public void Save()
        {
            // 保存环绕音设置
            RegistryManager.SaveValue(SURROUND_ENABLED_KEY, _surroundEnabled ? 1 : 0);
            
            // 保存循环播放设置
            RegistryManager.SaveValue(LOOP_ENABLED_KEY, _loopEnabled ? 1 : 0);
            
            // 保存环绕音深度设置（转换为整数百分比）
            int depthPercent = (int)(_surroundDepth * 100);
            RegistryManager.SaveValue(SURROUND_DEPTH_KEY, depthPercent);
        }

        /// <summary>
        /// 从注册表加载设置
        /// </summary>
        public void Load()
        {
            // 加载环绕音设置
            int surroundEnabledValue = RegistryManager.LoadValue(SURROUND_ENABLED_KEY, DEFAULT_SURROUND_ENABLED ? 1 : 0);
            _surroundEnabled = surroundEnabledValue == 1;

            // 加载循环播放设置
            int loopEnabledValue = RegistryManager.LoadValue(LOOP_ENABLED_KEY, DEFAULT_LOOP_ENABLED ? 1 : 0);
            _loopEnabled = loopEnabledValue == 1;

            // 加载环绕音深度设置
            int depthPercent = RegistryManager.LoadValue(SURROUND_DEPTH_KEY, (int)(DEFAULT_SURROUND_DEPTH * 100));
            _surroundDepth = Math.Clamp(depthPercent / 100.0f, 0.0f, 1.0f);
        }
    }
}
