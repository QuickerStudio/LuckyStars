using LuckyStars.Utils;

namespace LuckyStars.Managers
{
    /// <summary>
    /// 星空动画提供者，提供星空粒子效果的HTML内容
    /// </summary>
    public class StarfieldAnimationProvider : IAnimationProvider
    {
        /// <summary>
        /// 获取动画名称
        /// </summary>
        /// <returns>动画名称</returns>
        public string GetName() => "星空";

        /// <summary>
        /// 获取动画描述
        /// </summary>
        /// <returns>动画描述</returns>
        public string GetDescription() => "一个美丽的星空粒子效果，星星会随着鼠标移动而产生互动";

        /// <summary>
        /// 获取HTML内容
        /// </summary>
        /// <returns>完整的HTML内容字符串</returns>
        public string GetHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""zh"">
<head>
    <meta charset=""UTF-8"">
    <title>LuckyStars - 互动壁纸</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            overflow: hidden;
            background-color: transparent;
            width: 100vw;
            height: 100vh;
            /* 移除不透明背景，确保壁纸可见 */
        }
        /* 移除调试相关样式 */
        /* 画布样式设置 */
        canvas {
            position: fixed;
            top: 0;
            left: 0;
            z-index: -1;
        }
    </style>
</head>
<body>
    <!-- 移除调试元素 -->

    <canvas id=""particleCanvas""></canvas>

    <script>
        // 接收从C#发送的鼠标坐标的函数
        function updateMousePosition(x, y) {
            // 在JavaScript端进行精确的坐标调整
            // 鼠标指针的热点在左上角，但视觉上我们看到的是箭头的尖端
            // 根据Windows标准鼠标指针，需要将跟踪点向右下偏移一点
            // 直接使用原始坐标，不进行任何偏移调整
            mouse.x = x;
            mouse.y = y;

            // 记录当前鼠标位置，用于调试
            console.log(`鼠标坐标更新: 原始X=${x}, Y=${y}, 调整后X=${mouse.x}, Y=${mouse.y}`);
        }

        // 节能模式控制
        let isLowPowerMode = false;
        let normalFrameRate = true; // 正常模式下使用requestAnimationFrame
        let lowPowerInterval = null; // 节能模式下的定时器

        // 设置节能模式
        function setLowPowerMode(enabled) {
            if (enabled === isLowPowerMode) return; // 如果状态没变，不做处理

            isLowPowerMode = enabled;
            console.log(`节能模式: ${isLowPowerMode ? '开启' : '关闭'}`);

            if (isLowPowerMode) {
                // 进入节能模式
                normalFrameRate = false;

                // 使用低帧率的setInterval替代requestAnimationFrame
                if (!lowPowerInterval) {
                    lowPowerInterval = setInterval(() => {
                        animate();
                    }, 100); // 降低到大约10FPS
                }

                // 减少粒子数量
                const reduceRatio = 0.3; // 保留原本30%的粒子

                // 减少彩色粒子
                while (colorParticles.length > config.colorParticleCount * reduceRatio) {
                    colorParticles.pop();
                }

                // 减少点状粒子
                while (dotParticles.length > config.dotParticleCount * reduceRatio) {
                    dotParticles.pop();
                }
            } else {
                // 退出节能模式
                normalFrameRate = true;

                // 清除低帧率定时器
                if (lowPowerInterval) {
                    clearInterval(lowPowerInterval);
                    lowPowerInterval = null;
                }

                // 恢复粒子数量
                // 恢复彩色粒子
                while (colorParticles.length < config.colorParticleCount) {
                    colorParticles.push(new ColorParticle(Math.random() * canvas.width, Math.random() * canvas.height));
                }

                // 恢复点状粒子
                while (dotParticles.length < config.dotParticleCount) {
                    dotParticles.push(new DotParticle());
                }

                // 重新启动动画循环
                requestAnimationFrame(animate);
            }
        }

        // 简化初始化
        document.addEventListener('DOMContentLoaded', () => {});

        // 参数变量集中管理
        const config = {
            particleSize: { min: 1, max: 4 },
            particleSpeed: { min: -0.5, max: 0.5 },
            mouseMaxDist: 200,
            maxLineDistColor: 10000,
            maxLineDistDot: 6000,
            collisionDuration: 10,
            glowRadiusFactor: 4.8,
            glowRadiusOffset: 1.2,
            glowHueSpeed: 0.3,
            mouseLineWidthFactor: 2,
            mouseLineOpacityFactor: 0.5,
            mouseLineLengthFactor: 8,
            randomColorChangeSpeed: 0.05,
            colorParticleCount: 250,  // 增加彩色粒子数量
            dotParticleCount: 150     // 增加点状粒子数量
        };

        // 初始化画布
        const canvas = document.getElementById('particleCanvas');
        const ctx = canvas.getContext('2d');
        canvas.width = window.innerWidth;
        canvas.height = window.innerHeight;

        // 鼠标交互参数
        const mouse = {
            x: null,
            y: null,
            maxDist: config.mouseMaxDist
        };

        // ================= 彩色粒子系统 =================
        class ColorParticle {
            constructor(x, y) {
                // 粒子参数
                this.x = x;
                this.y = y;
                this.size = Math.random() * (config.particleSize.max - config.particleSize.min) + config.particleSize.min;
                this.speedX = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min;
                this.speedY = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min;
                this.color = `hsl(${Math.random() * 360}, 70%, 50%)`;
                this.collisionColor = null;
                this.collisionTimer = 0;
                this.isConnectedToMouse = false;
                this.originalColor = this.color;
                this.brightness = 50 + Math.random() * 30; // 随机亮度
                this.twinkleSpeed = 0.01 + Math.random() * 0.03; // 随机闪烁速度
                this.twinkleOffset = Math.random() * Math.PI * 2; // 随机闪烁偏移
            }

            draw() {
                // 计算闪烁效果的亮度变化
                const brightnessFactor = Math.sin(Date.now() * this.twinkleSpeed + this.twinkleOffset);
                const currentBrightness = this.brightness + brightnessFactor * 20;

                // 更新颜色
                const hue = parseInt(this.color.split('hsl(')[1].split(',')[0]);
                this.color = `hsl(${hue}, 70%, ${currentBrightness}%)`;

                ctx.fillStyle = this.color;
                ctx.beginPath();
                this.drawStar(ctx, this.x, this.y, this.size, this.size * 2, 5);
                ctx.fill();
            }

            drawStar(ctx, x, y, radius1, radius2, points) {
                let angle = Math.PI / points;
                ctx.beginPath();
                for (let i = 0; i < 2 * points; i++) {
                    let radius = i % 2 === 0 ? radius2 : radius1;
                    ctx.lineTo(x + Math.cos(i * angle) * radius, y + Math.sin(i * angle) * radius);
                }
                ctx.closePath();
            }

            update() {
                // 鼠标引力影响
                if (mouse.x && mouse.y) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);
                    if (distance < mouse.maxDist) {
                        this.speedX += dx * 0.0005;
                        this.speedY += dy * 0.0005;
                    }
                }

                // 位置更新
                this.x += this.speedX;
                this.y += this.speedY;

                // 边界反弹处理
                if (this.x > canvas.width - this.size) {
                    this.speedX *= -0.8;
                    this.x = canvas.width - this.size;
                } else if (this.x < this.size) {
                    this.speedX *= -0.8;
                    this.x = this.size;
                }

                if (this.y > canvas.height - this.size) {
                    this.speedY *= -0.8;
                    this.y = canvas.height - this.size;
                } else if (this.y < this.size) {
                    this.speedY *= -0.8;
                    this.y = this.size;
                }

                // 简化碰撞状态更新
                if (this.collisionTimer > 0 && --this.collisionTimer === 0) {
                    this.collisionColor = null;
                }

                // 光晕颜色更新
                if (this.isConnectedToMouse) {
                    const hue = parseInt(this.color.split('hsl(')[1].split(',')[0]);
                    const newHue = (hue + 1) % 360;
                    this.color = `hsl(${newHue}, 70%, ${this.brightness}%)`;
                }
            }
        }

        // ================= 点状粒子系统 =================
        class DotParticle {
            constructor() {
                this.x = Math.random() * canvas.width;
                this.y = Math.random() * canvas.height;
                this.xa = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min;
                this.ya = Math.random() * (config.particleSpeed.max - config.particleSpeed.min) + config.particleSpeed.min;
                this.color = `hsl(${Math.random() * 360}, 80%, 60%)`;
                this.maxDist = config.maxLineDistDot;
                this.collisionColor = null;
                this.collisionTimer = 0;
                this.isConnectedToMouse = false;
                this.originalColor = this.color;
                this.size = 0.5 + Math.random() * 1.5; // 随机大小
                this.brightness = 60 + Math.random() * 20; // 随机亮度
                this.twinkleSpeed = 0.005 + Math.random() * 0.02; // 随机闪烁速度
                this.twinkleOffset = Math.random() * Math.PI * 2; // 随机闪烁偏移
            }

            update() {
                // 鼠标引力影响
                if (mouse.x && mouse.y) {
                    const dx = mouse.x - this.x;
                    const dy = mouse.y - this.y;
                    const distance = dx * dx + dy * dy;
                    if (distance < this.maxDist) {
                        this.xa += dx * 0.0002;
                        this.ya += dy * 0.0002;
                    }
                }

                // 位置更新
                this.x += this.xa;
                this.y += this.ya;

                // 边界反弹处理
                this.xa *= (this.x > canvas.width || this.x < 0) ? -0.8 : 1;
                this.ya *= (this.y > canvas.height || this.y < 0) ? -0.8 : 1;

                // 简化碰撞状态更新
                if (this.collisionTimer > 0 && --this.collisionTimer === 0) {
                    this.collisionColor = null;
                }

                // 计算闪烁效果的亮度变化
                const brightnessFactor = Math.sin(Date.now() * this.twinkleSpeed + this.twinkleOffset);
                const currentBrightness = this.brightness + brightnessFactor * 15;

                // 更新颜色
                const hue = parseInt(this.color.split('hsl(')[1].split(',')[0]);
                this.color = `hsl(${hue}, 80%, ${currentBrightness}%)`;

                // 光晕颜色更新
                if (this.isConnectedToMouse) {
                    const newHue = (hue + 1) % 360;
                    this.color = `hsl(${newHue}, 80%, ${currentBrightness}%)`;
                }
            }
        }

        // ================= 系统初始化 =================
        const colorParticles = Array.from({ length: config.colorParticleCount }, () =>
            new ColorParticle(Math.random() * canvas.width, Math.random() * canvas.height)
        );

        const dotParticles = Array.from({ length: config.dotParticleCount }, () =>
            new DotParticle()
        );

        // ================= 连线绘制系统 =================
        function drawLines(particles, maxDistance) {
            // 简化碰撞检测系统
            const checkDistance = Math.sqrt(maxDistance) * 0.8;

            // 优化的粒子间碰撞检测 - 仅对邻近粒子进行检测
            for (let i = 0; i < particles.length; i++) {
                const a = particles[i];
                // 只检测与附近粒子的碰撞
                for (let j = i + 1; j < Math.min(i + 10, particles.length); j++) {
                    const b = particles[j];
                    const dx = a.x - b.x;
                    const dy = a.y - b.y;
                    const distanceSq = dx * dx + dy * dy;
                    const minDist = (a.size || 1) + (b.size || 1);

                    if (distanceSq < minDist * minDist) {
                        // 直接应用碰撞效果
                        const collisionColor = `hsla(${Math.random() * 360}, 70%, 50%, 0.9)`;
                        a.collisionColor = collisionColor;
                        b.collisionColor = collisionColor;
                        a.collisionTimer = config.collisionDuration;
                        b.collisionTimer = config.collisionDuration;
                    }
                }
            }

            // 连线绘制逻辑
            particles.forEach(a => {
                particles.forEach(b => {
                    if (a === b) return;

                    const dx = a.x - b.x;
                    const dy = a.y - b.y;
                    const distance = dx * dx + dy * dy;

                    if (distance < maxDistance) {
                        ctx.beginPath();
                        const lineColor = a.collisionColor ||
                            `hsla(${a.color.split('hsl(')[1].split(')')[0]}, 0.2)`;

                        ctx.strokeStyle = lineColor;
                        ctx.lineWidth = 1 - (Math.sqrt(distance) / Math.sqrt(maxDistance));

                        ctx.moveTo(a.x, a.y);
                        ctx.lineTo(b.x, b.y);
                        ctx.stroke();
                    }
                });

                // 鼠标连线处理
                if (mouse.x && mouse.y) {
                    const dx = a.x - mouse.x;
                    const dy = a.y - mouse.y;
                    const distance = dx * dx + dy * dy;
                    if (distance < maxDistance * config.mouseLineLengthFactor) {
                        ctx.beginPath();
                        ctx.strokeStyle = `hsla(${a.color.split('hsl(')[1].split(')')[0]}, ${config.mouseLineOpacityFactor})`;
                        ctx.lineWidth = config.mouseLineWidthFactor - (Math.sqrt(distance) / Math.sqrt(maxDistance));
                        ctx.moveTo(a.x, a.y);
                        ctx.lineTo(mouse.x, mouse.y);
                        ctx.stroke();

                        // 更新粒子连接状态
                        a.isConnectedToMouse = true;
                    } else {
                        a.isConnectedToMouse = false;
                    }
                } else {
                    a.isConnectedToMouse = false;
                }
            });
        }

        // ================= 光效系统 =================
        let hue = 0;
        function applyGlow() {
            ctx.save();
            ctx.globalCompositeOperation = 'lighter';

            colorParticles.forEach(p => {
                const dynamicRadius = p.size * (config.glowRadiusFactor + Math.sin(Date.now() * 0.0008 + p.x) * config.glowRadiusOffset);

                const gradient = ctx.createRadialGradient(
                    p.x, p.y, 0,
                    p.x, p.y, dynamicRadius
                );
                gradient.addColorStop(0, `hsla(${(hue + p.x / 10) % 360}, 80%, 60%, 0.3)`);
                gradient.addColorStop(1, 'transparent');

                ctx.fillStyle = gradient;
                ctx.beginPath();
                ctx.arc(p.x, p.y, dynamicRadius, 0, Math.PI * 2);
                ctx.fill();
            });

            ctx.restore();
            hue = (hue + config.glowHueSpeed) % 360;
        }

        // ================= 动画循环系统 =================
        function animate() {
            ctx.clearRect(0, 0, canvas.width, canvas.height);

            // 更新彩色粒子系统
            colorParticles.forEach(p => {
                p.update();
                p.draw();
            });
            drawLines(colorParticles, config.maxLineDistColor);

            // 更新点状粒子系统
            dotParticles.forEach(p => {
                p.update();
                ctx.fillStyle = p.color;
                ctx.beginPath();
                ctx.arc(p.x, p.y, p.size, 0, Math.PI * 2);
                ctx.fill();
            });
            drawLines(dotParticles, config.maxLineDistDot);

            applyGlow();

            // 只在正常模式下使用requestAnimationFrame
            // 节能模式下由setInterval调用
            if (normalFrameRate) {
                requestAnimationFrame(animate);
            }
        }

        // ================= 事件监听系统 =================
        window.addEventListener('mousemove', e => {
            updateMousePosition(e.clientX, e.clientY);
        });

        window.addEventListener('mouseout', () => {
            mouse.x = null;
            mouse.y = null;
        });

        window.addEventListener('resize', () => {
            canvas.width = window.innerWidth;
            canvas.height = window.innerHeight;
            colorParticles.forEach(p => {
                p.x = Math.random() * canvas.width;
                p.y = Math.random() * canvas.height;
            });
            dotParticles.forEach(p => {
                p.x = Math.random() * canvas.width;
                p.y = Math.random() * canvas.height;
            });
        });

        animate();
    </script>
</body>
</html>";
        }
    }
}
