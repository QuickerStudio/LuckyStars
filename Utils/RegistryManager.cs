using Microsoft.Win32;
using System;
using System.Runtime.Versioning;

namespace LuckyStars.Utils
{
    [SupportedOSPlatform("windows")]
    public class RegistryManager
    {
        private const string RegistryKey = "SOFTWARE\\LuckyStars";
        private const string TimerIntervalValueName = "TimerInterval";
        private const string LastWallpaperPathValueName = "LastWallpaperPath";
        private const string LastWallpaperTypeValueName = "LastWallpaperType";
        private const string MusicPlayingStateName = "MusicPlayingState";
        private const string MusicVolumeName = "MusicVolume";
        private const string MusicPausedName = "MusicPaused";

        public enum TimerState
        {
            FiveMinutes = 300000,
            TenMinutes = 600000,
            TwentyMinutes = 1200000,
            Disabled = 0
        }

        [SupportedOSPlatform("windows")]
        public static int LoadTimerIntervalFromRegistry()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(TimerIntervalValueName);
                if (value != null && int.TryParse(value.ToString(), out int interval))
                {
                    return interval;
                }
            }
            return (int)TimerState.FiveMinutes;
        }

        [SupportedOSPlatform("windows")]
        public static void SaveTimerIntervalToRegistry(int interval)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key?.SetValue(TimerIntervalValueName, interval);
        }

        [SupportedOSPlatform("windows")]
        public static TimerState GetCurrentTimerState()
        {
            int interval = LoadTimerIntervalFromRegistry();
            return interval switch
            {
                (int)TimerState.FiveMinutes => TimerState.FiveMinutes,
                (int)TimerState.TenMinutes => TimerState.TenMinutes,
                (int)TimerState.TwentyMinutes => TimerState.TwentyMinutes,
                (int)TimerState.Disabled => TimerState.Disabled,
                _ => TimerState.FiveMinutes
            };
        }

        [SupportedOSPlatform("windows")]
        public static TimerState CycleTimerState()
        {
            var currentState = GetCurrentTimerState();
            var nextState = currentState switch
            {
                TimerState.FiveMinutes => TimerState.TenMinutes,
                TimerState.TenMinutes => TimerState.TwentyMinutes,
                TimerState.TwentyMinutes => TimerState.Disabled,
                TimerState.Disabled => TimerState.FiveMinutes,
                _ => TimerState.FiveMinutes
            };

            SaveTimerIntervalToRegistry((int)nextState);
            return nextState;
        }

        public enum WallpaperType
        {
            Image,
            Video
        }

        [SupportedOSPlatform("windows")]
        public static void SaveLastWallpaperState(string path, WallpaperType type)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            if (key != null)
            {
                key.SetValue(LastWallpaperPathValueName, path);
                key.SetValue(LastWallpaperTypeValueName, (int)type);
            }
        }

        [SupportedOSPlatform("windows")]
        public static (string path, WallpaperType type) LoadLastWallpaperState()
        {
            string path = string.Empty;
            WallpaperType type = WallpaperType.Image;

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var pathValue = key.GetValue(LastWallpaperPathValueName);
                var typeValue = key.GetValue(LastWallpaperTypeValueName);

                if (pathValue != null)
                {
                    path = pathValue.ToString() ?? string.Empty;
                }

                if (typeValue != null && int.TryParse(typeValue.ToString(), out int typeInt))
                {
                    type = (WallpaperType)typeInt;
                }
            }

            return (path, type);
        }

        /// <summary>
        /// 保存音乐播放状态到注册表
        /// </summary>
        /// <param name="isPlaying">是否正在播放</param>
        [SupportedOSPlatform("windows")]
        public static void SaveMusicPlayingState(bool isPlaying)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key?.SetValue(MusicPlayingStateName, isPlaying ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 从注册表加载音乐播放状态
        /// </summary>
        /// <returns>是否应该播放音乐</returns>
        [SupportedOSPlatform("windows")]
        public static bool LoadMusicPlayingState()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(MusicPlayingStateName);
                if (value != null && int.TryParse(value.ToString(), out int state))
                {
                    return state == 1;
                }
            }
            return false; // 默认不播放
        }

        /// <summary>
        /// 保存音乐音量到注册表
        /// </summary>
        /// <param name="volume">音量值（0.0-1.0）</param>
        [SupportedOSPlatform("windows")]
        public static void SaveMusicVolume(float volume)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            // 将浮点数转换为整数存储（0-100）
            int volumeInt = (int)(volume * 100);
            key?.SetValue(MusicVolumeName, volumeInt, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 从注册表加载音乐音量
        /// </summary>
        /// <returns>音量值（0.0-1.0）</returns>
        [SupportedOSPlatform("windows")]
        public static float LoadMusicVolume()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(MusicVolumeName);
                if (value != null && int.TryParse(value.ToString(), out int volumeInt))
                {
                    return volumeInt / 100.0f;
                }
            }
            return 0.2f; // 默认音量20%
        }

        /// <summary>
        /// 保存音乐暂停状态到注册表
        /// </summary>
        /// <param name="isPaused">是否暂停</param>
        [SupportedOSPlatform("windows")]
        public static void SaveMusicPausedState(bool isPaused)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key?.SetValue(MusicPausedName, isPaused ? 1 : 0, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 从注册表加载音乐暂停状态
        /// </summary>
        /// <returns>是否暂停</returns>
        [SupportedOSPlatform("windows")]
        public static bool LoadMusicPausedState()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(MusicPausedName);
                if (value != null && int.TryParse(value.ToString(), out int state))
                {
                    return state == 1;
                }
            }
            return false; // 默认不暂停
        }

        /// <summary>
        /// 通用方法：保存整数值到注册表
        /// </summary>
        /// <param name="valueName">值名称</param>
        /// <param name="value">整数值</param>
        [SupportedOSPlatform("windows")]
        public static void SaveValue(string valueName, int value)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key?.SetValue(valueName, value, RegistryValueKind.DWord);
        }

        /// <summary>
        /// 通用方法：从注册表加载整数值
        /// </summary>
        /// <param name="valueName">值名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的整数值，如果不存在则返回默认值</returns>
        [SupportedOSPlatform("windows")]
        public static int LoadValue(string valueName, int defaultValue)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(valueName);
                if (value != null && int.TryParse(value.ToString(), out int result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 通用方法：保存字符串值到注册表
        /// </summary>
        /// <param name="valueName">值名称</param>
        /// <param name="value">字符串值</param>
        [SupportedOSPlatform("windows")]
        public static void SaveStringValue(string valueName, string value)
        {
            using var key = Registry.CurrentUser.CreateSubKey(RegistryKey);
            key?.SetValue(valueName, value, RegistryValueKind.String);
        }

        /// <summary>
        /// 通用方法：从注册表加载字符串值
        /// </summary>
        /// <param name="valueName">值名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>加载的字符串值，如果不存在则返回默认值</returns>
        [SupportedOSPlatform("windows")]
        public static string LoadStringValue(string valueName, string defaultValue)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key != null)
            {
                var value = key.GetValue(valueName);
                if (value != null)
                {
                    return value.ToString() ?? defaultValue;
                }
            }
            return defaultValue;
        }
    }
}
