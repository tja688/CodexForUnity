我们游戏打算制作一个皮影戏的效果，

这里是一套落地方案，旨在还原皮影戏那种“投影”的光影质感

这个是重中之重，我们美术只给我们具体影偶的sprite2d图像

这种方案的核心逻辑是：把影偶渲染成一张贴图，再把这张贴图“印”在带有布料纹理的幕布上。

以下是完整的五步落地步骤：

第一步：构建“幕后舞台” (Render Texture 架构)
我们需要一个玩家看不见的平行空间，专门用来“演”皮影，然后把画面转录下来。

新建图层 (Layer)： 创建一个名为 PuppetLayer 的图层。所有的影偶Sprite都要设置在这个Layer上。

创建投影摄像机 (Shadow Camera)：

创建一个新的 Camera，命名为 ShadowCam。

Culling Mask： 只勾选 PuppetLayer。

Projection： 设置为 Orthographic (正交)。这能保证影偶移动时透视不会变形，更像贴在幕布上。

Background Type： 设置为 Solid Color。

Background Color： 设置为 纯白色 (255, 255, 255)。

原理解释： 白色代表光线穿透最强的地方（灯泡直射），影偶遮挡的地方会有颜色，这样我们在后面做“正片叠底”时，白色背景会自动消失，只留下影偶的色彩。

创建渲染纹理 (Render Texture)：

在Project里新建一个 Render Texture，命名为 RT_ShadowProjection。

分辨率建议设高一点（如 2048x2048），保证边缘清晰。

将 ShadowCam 的 Target Texture 设为这个 RT_ShadowProjection。

完成后的状态： 你现在拥有了一张实时更新的、白底彩色的皮影动态贴图。

第二步：制作“幕布材质” (Shader Graph 核心)
这是实现“质感”的最关键一步。我们需要一个 Shader Graph，挂在主摄像机面前的那个 Quad（幕布）上。

Shader Graph 逻辑流程：

基础纹理输入：

Main Texture (幕布底图)： 找一张米黄色、稍微有点旧的纸或者棉布纹理。

Normal Map (法线贴图)： 极度重要！ 必须是一张粗糙的织物法线。这决定了投影上去的光是否有“渗入布料纹理”的感觉。

投影纹理输入 (RT采样)：

创建一个 Texture2D 属性，把第一步做的 RT_ShadowProjection 传进来。

Screen Position 节点： 如果你想模拟更真实的“投影仪”效果，可以用 Screen Position 对 RT 进行采样；如果幕布是固定的，直接用 UV 采样即可。

核心混合算法 (Multiply)：

将 幕布底图 的颜色 与 RT投影图 的颜色相乘 (Multiply)。

效果： RT 中白色的背景乘上去后不变（透出幕布原色），RT 中有颜色的影偶乘上去后，会像染料一样染在幕布上。

光影细节处理 (Emission & Transmission)：

虽然是乘法，但为了模拟“背后有强光”，你需要把混合后的结果连接到 Emission (自发光) 节点，而不仅仅是 Base Color。

给 Emission 乘上一个强度系数（Intensity），让画面看起来是“亮”的，而不是一块暗布。

第三步：实现“距离感”与“虚焦” (Blur Processing)
你提到一定要有投影的感觉，投影的核心物理特性是：离幕布越远，影子越虚。

由于我们用的是 RT，这非常好实现：

引入模糊算法：

在 Unity 6 URP 中，你可以利用 Renderer Feature 或者在 Shader Graph 内部做模糊。

推荐方案 (Shader Graph内)： 在幕布 Shader 中，对 RT_ShadowProjection 进行采样时，使用 Custom Function 写入一个简单的“高斯模糊”或“Box Blur”算法。

关联距离参数：

在 Shader Graph 中新建一个 Float 属性，命名为 BlurAmount。

写一个简单的 C# 脚本挂在幕布上。

逻辑： 脚本计算 影偶Z轴 与 幕布Z轴 的距离。

实时更新： Material.SetFloat("BlurAmount", calculatedDistance * factor);

效果呈现：

当影偶贴紧幕布（Z轴距离近），BlurAmount 为 0，影子边缘锐利。

当影偶向后退（Z轴距离远），BlurAmount 变大，影子在幕布上自动散开、变模糊。

第三步半：解决“黑影”与“透光”的区别 (Color Correction)
如果美术给的 Sprite 是实心的（完全不透明），投射出来就是黑乎乎的一坨。皮影需要透光。

修改影偶 Sprite 的材质：

不需要改 Shader，只需要改 Sprite 图片本身。

告诉美术：影偶的 Alpha 通道不要只用 0 和 1。

影偶的中间部分（兽皮部分）Alpha 值设为 0.8 或 0.9（半透明），轮廓线和关节连接处设为 1.0（不透明）。

或者，在 Shader Graph 处理 RT 时，提高 Saturation (饱和度)，让投射到幕布上的颜色比原始 Sprite 更鲜艳，模拟光线穿透兽皮的高光感。

第四步：环境氛围润色 (Post-Processing & Lighting)
幕布本身做好了，现在要布置舞台灯光，消除“数码味”。

Vignette (暗角)：

在 Global Volume 中开启 Vignette。

这能模拟旧式光源（如油灯、老灯泡）中间亮、四周暗的物理特性，聚焦视线。

Bloom (辉光)：

开启 Bloom。设置 Threshold 阈值，只让幕布上最亮（纯白背景）的地方产生微微的晕光。

这会让幕布看起来像是被强光打透的，而不是一张发光的图片。

Film Grain (胶片噪点)：

加上一点点噪点。这能有效掩盖 2D Sprite 拉伸后的像素感，增加一种“老电影”或“旧时光”的粗糙质感。

动态光源闪烁 (Script)：

不要让幕布的 Emission 强度保持恒定。

写一个脚本，利用 Mathf.PerlinNoise 让 Emission 的 Intensity 在 1.0 到 1.2 之间随机微弱浮动。

质感来源： 这就是你要的“呼吸感”，模拟背后烛火或电压不稳的效果。

第五步：物理交互 (可选的顶级细节)
如果想做到极致，可以加上这一步：

幕布微动： 在幕布 Shader 的 Vertex Position (顶点位置) 上，通过 Time 节点加一个极其微小的 Sine 波或者 Noise 扰动。

效果： 幕布会看起来像是有微风吹过，或者因为后面有人走动而产生的轻微起伏。投影在上面的影子也会随之微微扭曲。

总结
你的方案核心不在于 Unity 的 Light 组件，而在于 Render Texture 的合成。

ShadowCam 负责把影偶拍成一张图。

Screen Shader 负责把这张图以“正片叠底”的方式印在布料纹理上。

Blur 算法 负责根据距离控制影子的虚实。

这套方案在 Unity 6 URP 中性能极佳（只是多了一个 RT 渲染），且完全可控，能完美还原你想要的“幕布+投影”质感。