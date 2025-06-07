using NAudio.Wave;
using System;
using System.Numerics;

namespace LuckyStars.Players
{
    /// <summary>
    /// 环绕音效果提供器
    /// </summary>
    public class SurroundSoundProvider : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly float _surroundDepth;
        private readonly float[] _delayBuffer;
        private int _delayBufferPosition;
        private const int DELAY_SAMPLES = 1024; // 延迟样本数，影响环绕音效果的空间感

        public SurroundSoundProvider(ISampleProvider source, float surroundDepth)
        {
            _source = source;
            _surroundDepth = Math.Clamp(surroundDepth, 0.0f, 1.0f);
            _delayBuffer = new float[DELAY_SAMPLES];
            _delayBufferPosition = 0;
        }

        public WaveFormat WaveFormat => _source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            // 读取原始音频数据
            int samplesRead = _source.Read(buffer, offset, count);

            // 如果是单声道，转换为立体声
            if (_source.WaveFormat.Channels == 1)
            {
                ApplyMonoSurroundEffect(buffer, offset, samplesRead);
            }
            else if (_source.WaveFormat.Channels == 2)
            {
                ApplyStereoSurroundEffect(buffer, offset, samplesRead);
            }

            return samplesRead;
        }

        /// <summary>
        /// 为单声道音频应用环绕音效果
        /// </summary>
        private void ApplyMonoSurroundEffect(float[] buffer, int offset, int sampleCount)
        {
            // 单声道音频转换为立体声环绕效果
            for (int n = 0; n < sampleCount; n++)
            {
                float sample = buffer[offset + n];

                // 存储当前样本到延迟缓冲区
                float delayedSample = _delayBuffer[_delayBufferPosition];
                _delayBuffer[_delayBufferPosition] = sample;
                _delayBufferPosition = (_delayBufferPosition + 1) % DELAY_SAMPLES;

                // 应用环绕音效果：左右声道使用不同的延迟和相位
                float surroundFactor = _surroundDepth * 0.5f;
                
                // 左声道：原始样本 + 延迟样本的反相
                float leftChannel = sample + delayedSample * surroundFactor;
                
                // 右声道：原始样本 - 延迟样本的反相
                float rightChannel = sample - delayedSample * surroundFactor;

                // 写回缓冲区
                buffer[offset + n] = (leftChannel + rightChannel) * 0.5f;
            }
        }

        /// <summary>
        /// 为立体声音频应用环绕音效果
        /// </summary>
        private void ApplyStereoSurroundEffect(float[] buffer, int offset, int sampleCount)
        {
            // 立体声音频的环绕效果处理
            for (int n = 0; n < sampleCount; n += 2)
            {
                float leftSample = buffer[offset + n];
                float rightSample = buffer[offset + n + 1];

                // 存储当前样本到延迟缓冲区
                float delayedLeft = _delayBuffer[_delayBufferPosition];
                float delayedRight = _delayBuffer[(_delayBufferPosition + 1) % DELAY_SAMPLES];
                _delayBuffer[_delayBufferPosition] = leftSample;
                _delayBuffer[(_delayBufferPosition + 1) % DELAY_SAMPLES] = rightSample;
                _delayBufferPosition = (_delayBufferPosition + 2) % DELAY_SAMPLES;

                // 应用环绕音效果：交叉混合左右声道
                float surroundFactor = _surroundDepth * 0.3f;
                
                // 增强立体声分离度
                float enhancedLeft = leftSample + (rightSample * -surroundFactor) + (delayedRight * surroundFactor);
                float enhancedRight = rightSample + (leftSample * -surroundFactor) + (delayedLeft * surroundFactor);

                // 写回缓冲区
                buffer[offset + n] = enhancedLeft;
                buffer[offset + n + 1] = enhancedRight;
            }
        }
    }
}
