

# 一、课题名称

Motion Matching

# 二、课题背景或简介

unity原生的**Mecanim**动画系统，虽然能够以可视的、形如状态机的方式 *管理动画之间的复杂交互* ，但是随着动画片段的增加，动画机将会变成一张庞大而错综的网，不仅构建、排查十分麻烦，要增加新的状态更是一场灾难。

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/stateMachine.jpg?raw=true"> <br> <div style="display: inline-block;"><font size = 3>一个“复杂”的状态机</font></div> </center>

于是在02年，有人提出 **MotionGraphs** 的概念，预先在动画中生成过度点，运行时接受玩家输入并在最近的过度点进行过度。该方案算是MM的雏形，但是由于过度点是预先生成的、离散的时间节点，不能做到及时反馈；直到16年，育碧在《荣耀战魂》中成功实现实时搜索并反馈的 **MotionMatching** ，才让这项技术真正迈入实用领域。

# 三、技术方案简介

## 1、动画数据处理

### 数据格式

动画数据处理的过程，其实就是将动画中的信息转换为一个个PoseData的过程。

![](https://github.com/WindUpRadio/Images/blob/main/PoseData.jpg?raw=true)

PoseData各属性的说明如下：

- 每个Pose唯一的ID

- Pose在动画中对应的时刻

- Pose的下一个Pose的ID

- Pose对应的动画的ID

- 模型的根速度

- 关节数据

- 关节速度

- 关节坐标（相对于模型的根节点）

- 轨迹点

- 轨迹点朝向（相对于模型）

- 轨迹点坐标（相对于模型）

- 动画类型（有静立动画和移动动画两种）

- 标签（每一位对应Pose是否打上了对应那一位的标签）

- 是否为混合动画的Pose

- 混合动画的权重（仅当IsBlendClip为True时生效）

- 混合动画的ID（仅当IsBlendClip为True时生效）

### 模拟播放

根本方案：利用PlayableGraph提供的动画播放接口，在当前Unity Editor打开的场景中创建一个模型的实例并将其与PlayableGraph绑定，PlayableGraph的更新模式设为Manual（手动），每次调用PlayableGraph.Evaluate并记录所需要的信息，记录完成后销毁模型。

其中，根速度与关节速度是用两帧之间的坐标差除以时间得到的（与运行时预测统一），关节坐标可以直接通过记录到的信息作转换后拿到。

轨迹点处理：

- 当轨迹点落在动画区间内：直接从记录的数据中得到轨迹点的朝向和坐标。

- 如果轨迹点落在区间左侧：根据动画第一帧的速度，乘以轨迹点的时间预测得到坐标信息，朝向直接拿第一帧的朝向

- 如果轨迹点落在区间右侧：如果是循环动画则调用PlayableGraph.Evaluate向后窥探对应时刻的状态并获取信息，如果是非循环动画则方法和轨迹点落在区间左侧的一样

混合动画的处理除了使用的是AnimationMixerPlayable以外，其他和普通动画没有很大的差别。

  

  

## 2、轨迹模拟

虚拟移动数据结构：

- 加速度

- 角速度

- 最大速度限制

-  最大角速度限制

-  阻力

- 当前朝向

- 目标朝向

以上数据随输入而改变

 
轨迹点数据结构：

- 过去轨迹点位置

- 过去轨迹点方向

- 未来轨迹点位置

- 未来轨迹点方向

- 未来轨迹：根据虚拟移动数据生成未来轨迹点。

- 过去轨迹：储存过去十秒内每帧的位置点，从中采样生成过去轨迹

- 动画轨迹：由动画数据提供

  

## 3、动作匹配

涉及到的数据：

- 根速度

- 关节信息

- 轨迹信息

- tag信息

- 帧ID

除了轨迹预测信息，其余信息来自于最后一个被加入过渡的动画。

总体流程：

- 获取下一帧的数据

- 求出所有帧的cost（多线程优化）

- 找到cost最小的帧

- 播放对应帧的动画

- 其它特殊处理

### cost函数

涉及的参数：

- PoseTrajectoryRatio：平衡动画流畅度和动画响应速度。值越高动画响应速度速度越快，动画流畅度越低。

- LoopAnimFavour：优先匹配循环动画。

- NextPoseFavour：优先匹配下一帧。

- BodyVelocityWeight：根速度权重。

- AngleMutiplier：预测轨迹朝向权重。

- PositionMultiplier：预测轨迹位置权重。

- JointPositionWeights：关节位置权重。

- JointVelocityWeights：关节速度权重。

流程：

    若tag不满足要求或者帧为当前帧或者动画类型不匹配

        cost = infinity

    否则

        cost = 根速度的差距 + 关节信息的差距 + 预测轨迹信息的差距

        若这一帧为正在播放动画的下一帧

            cost减小

        若这一帧为循环动画的某一帧

            cost减小

### 其它处理

当人物移动速度达到最大速度时，会对人物的朝向进行调整。

涉及参数：

- AngleThreshold：修正角度的角度差阈值。当角色在全速前进且当前朝向和预期朝向的偏差小于此值时，会进行角度修正。

- AngleRiviseSpeed：角度修正的速度。

  

## 4、动画播放与过渡

以Playable为主，实现基本动画和部分高级动画的播放和过渡，以AnimatorController为辅，实现部分高级动画的播放和过渡。

  

### Playable

Playable是一个较底层的API，它能让开发者更随意地设计动画系统。定义了一个继承于PlayableBehaviour的类MyPlayableBehaviour，来控制PlayableGraph的帧处理和权重设置。PlayableGraph的结构如下：

![](https://s1.ax1x.com/2022/08/18/vDgZB6.png)

主要有一个主混合器AnimationMixerPlayable，去混合五个子混合器，每个子混合器又连接两个动画片段控制器AnimationClipPlayable(由于没有传入两个动画片段，故只显示一个动画片段)。

  

播放主要通过使用AnimationClip创建AnimationClipPlayable，用settime()函数设置时间，再与第一个子AnimationMixerPlayable连接，而第一个子混合器则顶替其他子混合器(按先后顺序)，之后重新设置各个子混合器的权重，第一个子AnimationMixerPlayable权重设为0，其他子混合器按自身比例获取被顶替的混合器的权重。当被顶替的混合器剩余权重过大时，撤销播放操作。对于特殊动画，如高级动画的跳跃等，则无撤销操作。

  

过渡则重载PrepareFrame(Playable playable, FrameData info)实现，此函数在动画准备帧时调用，故可以准确地设置权重。对于新动画，settime()函数会使动画从开始到指定时间偏移，从而产生位移，故过渡第一帧要单独处理。对于第一帧设置权重为零。对于其他帧，则先使其他子混合器的权重 -= 比例*帧长度/过渡时间，第一个混合器的权重设为1-其他子混合器权重之和。

  

按照以上思路，实现以下接口：

  

- PlayAnimationClip(int id,double time,float ttime) 带过渡时间的播放

- PlayAnimationClipByClip(AnimationClip clip, double time) 传入动画播放

- PlayAnimationClip(int id,double time) 不带过渡时间的播放（调用默认过渡时间

- PlayBlendAnimationClips(int id0, int id1, double time,float weight) 播放混合动画

- PlayAnimationClipByClip(AnimationClip clip, double time,float transitionTime) 带过渡时间的传入动画播放

  

PlayableGraph主要实现了走路动画，跳跃动画和跳下动画的播放。

  

### AnimatorController(动画控制器）

  

相比Playable的灵活性，动画控制器是Unity自带的动画控制器，拥有许多现成的函数库和较好的查看界面，可以通过使用CrossFade()函数实现动画的播放和过渡。而使用AnimatorController主要是因为MatchTarget()函数基于其实现，而翻越等高级运动，使用MatchTarget()函数可以更简便地实现，故依旧保留。

  

**AnimatorController**主要实现了翻越动画的播放。

  

### 高级运动

- 跳跃：触发跳跃时，在跳跃落点处地面打点，判断高度是否可以跳跃。若可即关闭动画片段匹配，播放跳跃动画，起始点和目标点之间做插值修改位置。

- 翻越：向前发射射线检测前方物体，若有，则关闭匹配，获取其相对于角色的高度、面向角色的宽度，根据宽度和高度，先进行转向面向墙壁，再播放不同的动作，期间调用MatchTarget()函数，设置对应人物关节的匹配位置和起始时间，同时关闭角色控制器CharacterController，使人物更好地模拟现实情况。再根据动作时间或者位置，开启角色控制器。期间仍要检测是否接地(通过射线检测)，施加重力，再根据动作的时间和名字，来控制匹配的开启或动作的跳转。

- 跳下：检测前下方是否有地面，若无，则关闭匹配，执行跳下动作，在接地时，开启匹配，在特殊状态时不执行。

  

由于翻越需要动画控制器的实现，故在进行时，调用Playable的断开连接函数，在结束时，重新连接Playable。

  

# 四、成果演示

### 同样的动捕数据，不同参数的影响

如下所示，左边的机器人动作更连贯（权重偏向关节），右边的机器人动作更敏捷（权重偏向轨迹）。

![](https://github.com/WWWWMMM/picture/blob/main/short1%20(1).gif?raw=true)

![](https://github.com/WWWWMMM/picture/blob/main/short1%20(2).gif?raw=true)


  行走跑步的表现

[![3gCXr.md.gif](https://s1.328888.xyz/2022/08/18/3gCXr.md.gif)](https://imgloc.com/i/3gCXr)


  对于矮的墙面向短边过的表现

[![vD5jT1.gif](https://s1.ax1x.com/2022/08/18/vD5jT1.gif)](https://imgtu.com/i/vD5jT1)

对于矮的墙面向长边过的表现

[![vD5zY6.gif](https://s1.ax1x.com/2022/08/18/vD5zY6.gif)](https://imgtu.com/i/vD5zY6)

对于高的墙面向长边过的表现

[![vD5xFx.gif](https://s1.ax1x.com/2022/08/18/vD5xFx.gif)](https://imgtu.com/i/vD5xFx)

对于高的墙面向短边过的表现

[![vD5XwR.gif](https://s1.ax1x.com/2022/08/18/vD5XwR.gif)](https://imgtu.com/i/vD5XwR)

自动下落的表现

[![vDISfK.gif](https://s1.ax1x.com/2022/08/18/vDISfK.gif)](https://imgtu.com/i/vDISfK)

点击跳跃键下落的表现

[![vDIClD.gif](https://s1.ax1x.com/2022/08/18/vDIClD.gif)](https://imgtu.com/i/vDIClD)

点击跳跃键跳上墙的表现

[![3goJJ.md.gif](https://s1.328888.xyz/2022/08/18/3goJJ.md.gif)](https://imgloc.com/i/3goJJ)

对于多个相连的墙的表现

[![vDI9SO.gif](https://s1.ax1x.com/2022/08/18/vDI9SO.gif)](https://imgtu.com/i/vDI9SO)



# 五、配置

unity编辑器版本为2021.3.5f1c1，在Windows10上运行；脚本语言使用C#

  

# 六、使用方法

## 1. 动捕数据的转换与模型中动画文件的提取

>工具栏点击 **getAnim**，再点击 **Window** 打开窗口

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/openWindow.jpg?raw=true">  </center>

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/getAnim_Win.jpg?raw=true"> <br><font size = 3>窗口默认状态</font> </center>

分为上下两个部分，实现两个主要功能：

- Get FBX区域：通过blender将bvh文件转换为fbx文件以供导入

- Get Anim区域：从fbx文件（已导入到项目中）批量提取动画

### Get FBX 区域：

>有时候没有合适的动画资产，只有bvh文件（动捕数据），可以通过此工具批量将bvh文件转换成fbx文件，以供后续使用

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/getAnim_getFBX.png?raw=true"> <br><font size = 3>使用时状态</font> </center>

*BlenderPath* 、*bvhFolder* 、*fbxFolder* 三项分别为：blender.exe的位置、需要转换的bvh文件所处文件夹、希望导出fbx文件的文件夹

点击行末按钮，通过文件浏览器选择文件/文件夹

*batchSize* ，指多少个bvh文件作为一组导出 **一个** fbx文件，bvh文件数不足 *batchSize* 的也视为一组进行转换

**注意：** 设置过大将会导致转换与后续提取动画效率降低；经过测试，10是比较合适的数值，一般无需调整

在参数设置完成之后，点击 *Convert* 按钮等待转换完成

结束时将自动以文件浏览器打开 *fbxFolder*

>在弹出fbx模型文件所在文件夹之后，需要手动将其拖入项目

>覆写了导入模型文件的方法（详情查看Importer.cs），若需要使用默认导入设置，将 Importer.cs 内容注释即可

### Get Anim 区域：

>虽然在project界面可以通过Ctrl+D的方式导出fbx文件中的**单个**anim文件，一旦数量提升，将成为一项重复机械的工作，故提供批量化提取的工具

本工具只会提取目录下直属fbx文件的动画，不对子文件夹进行搜索

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/getAnim_getAnim.jpg?raw=true"> <br> <div style="display: inline-block;"><font size = 3>使用时状态</font></div> </center>

*From* ：标签显示用户在project界面点击的文件/文件夹；使用时请点击fbx文件所在文件夹

*To* ：希望导出动画文件的位置，应当以Asset格式填写，**并以'\\'结尾**

若 To 文件夹不存在将会在提取时自动创建

设置完成点击 *Abstract* ，等待动画文件提取

## 2. 对动画打标签/打Tag

重绘AnimationClipInspector

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/inspector_default.jpg?raw=true"> <br> <div style="display: inline-block;"><font size = 3>默认状态</font></div> </center>

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/inspector_slct.jpg?raw=true"> <br> <div style="display: inline-block;"><font size = 3>使用前需要选择 PreProcessor</font></div> </center>

<center> <img src="https://github.com/Daedluscrew/imgBed/blob/main/inspector_pre.jpg?raw=true"> <br> <div style="display: inline-block;"><font size = 3>点击 TogglePreview 进入预览场景，同时Inspector改变</font></div> </center>

动画预览：

*Set Kyle to (0,0,0)* ：将预览模型设置到原点，并设置朝向为 Quaternion.identity

*Bake AnimClip for Preview* ：将动画应用到模型，以供预览及播放

*Time* ：**点击 *Bake* 后**，自动设置最大长度为动画时长；拖动滑条或输入数字，场景中模型实时更新为动画对应状态，包括位置、动作

*Play*、*Pause*：**点击 *Bake* 后使用**，根据 *interval* 在场景中倍速播放、暂停动画

打Tag：

*Required* 、*DoNotUse* ：自动获取对应结构类型设置的所有Tag，本项目预设为该两项

*+* ：在列表最后增加一项

*-* ：由下往上减少一项

*0* ：清空列表

在列表项中填入所需打Tag的时间，点击 *Save* 保存

点击 *TogglePreview* 返回原本场景

  

## 3、编辑配置文件

<center> <img style="border-radius: 0.3125em; box-shadow: 0 2px 4px 0 rgba(34,36,38,.12),0 2px 10px 0 rgba(34,36,38,.08);" src="https://github.com/WindUpRadio/Images/blob/main/Configuration.jpg?raw=true"> <br> <div style=" border-bottom: 1px solid #d9d9d9; display: inline-block; color: #999; padding: 2px;">配置文件编辑界面</div> </center>

<center> <img style="border-radius: 0.3125em; box-shadow: 0 2px 4px 0 rgba(34,36,38,.12),0 2px 10px 0 rgba(34,36,38,.08);" src="https://github.com/WindUpRadio/Images/blob/main/image.jpg?raw=true"> <br> <div style="border-bottom: 1px solid #d9d9d9; display: inline-block; color: #999; padding: 2px;">鼠标悬停可以看到各个属性的说明信息</div> </center>

例如：

1. Interval: 采样时间，如设置成0.05则代表1s的动画会采样出20条数据

2. Bones: 选取哪些骨骼进行匹配

3. TrajPoints: 决定匹配当前状态多少秒前/后的轨迹

截图中所示的配置信息可供参考，是经过多次迭代后较稳定的配置。

## 4、拖拽动画文件到预处理器

<center> <img style="border-radius: 0.3125em; box-shadow: 0 2px 4px 0 rgba(34,36,38,.12),0 2px 10px 0 rgba(34,36,38,.08);" src="https://github.com/WindUpRadio/Images/blob/main/image2.jpg?raw=true"> <br> <div style="border-bottom: 1px solid #d9d9d9; display: inline-block; color: #999; padding: 2px;">预处理器配置界面</div> </center>

BlendClips用来处理需要混合的动画。其中，IsLooping字段不需要手动设置，脚本会判断两个源动画是否都为循环动画来为该字段赋值。LeftClipID和RightClipID即源动画在LocomotionClips数组中的索引，Weight即两个动画的混合权重，LeftClip权重为Weight，RightClip权重为1-Weight。BlendClips一般用于应对动画数据不足或质量不高时的小角度旋转问题。若使用规范录制的动捕数据，则不需要配置BlendClips。

IdleClips存放所有静立动画，处理Idle动画时会将所有得到的数据的AniamtionType设为Idle，供MotionMatch模块处理。LocomotionClips存放其他所有移动动画，如跑，转向，急停等。Prefab存放用来进行MotionMatching的模型的预设，该预设必须要有Animator组件。Weights即配置文件。

## 5、生成AnimationData

点击PreProcess按钮后会将所有动画数据预处理，并生成一个AnimationData文件，供其他模块使用。该文件包含预处理所使用的配置文件，以及所有得到的Pose数据。

<center> <img style="border-radius: 0.3125em; box-shadow: 0 2px 4px 0 rgba(34,36,38,.12),0 2px 10px 0 rgba(34,36,38,.08);" src="https://github.com/WindUpRadio/Images/blob/main/image3.jpg?raw=true"></center>

  

## 6、轨迹生成


[![vDX3B8.jpg](https://s1.ax1x.com/2022/08/18/vDX3B8.jpg)](https://imgchr.com/i/vDX3B8)

Pos scale：对轨迹点位置大小进行等比放大缩小以适应动画轨迹

Show tra anim：显示动画轨迹

Show tra pre：显示预测轨迹

Now anim data：监控当前播放动画片段数据

Trajectory data：监控当前预测轨迹点数据

[![3f4Gw.jpg](https://s1.328888.xyz/2022/08/18/3f4Gw.jpg)](https://imgloc.com/i/3f4Gw)

设置walk和run状态的最大速度，加速度，角速度，阻力，最大角速度

设置walk到run的转换速度和run到walk的转换速度

监控虚拟运动movedata里的数据和当前运动状态。

  

  

## 7、匹配参数

![](https://github.com/WWWWMMM/picture/blob/main/screenshot-20220817-174230.png?raw=true)

- PoseTrajectoryRatio：平衡动画流畅度和动画响应速度。值越高角色响应速度快，但动画越不连贯；反之亦然。

- LoopAnimFavour：优先匹配循环动画。值越低越易匹配到循环动画，在可以匹配到循环动画的情况下值越大越好。

- NextPoseFavour：优先匹配下一帧。值越低越易匹配到下一帧，在动画足够连贯的的情况下值越大越好。

- BodyVelocityWeight：根速度权重。值越大根速度越重要。

- AngleMutiplier：预测轨迹朝向权重。值越大根轨迹朝向越重要。

- PositionMultiplier：预测轨迹位置权重。值越大根轨迹位置越重要。

- JointPositionWeights：关节位置权重。值越大根关节位置越重要。一般情况下脚的权重最大，脖子次之，手权重较小。

- JointVelocityWeights：关节速度权重。值越大根关节速度越重要。对匹配有辅助作用，一般权重设置较低。

  

  

  

##  8、高级运动

[![vDXJAg.jpg](https://s1.ax1x.com/2022/08/18/vDXJAg.jpg)](https://imgchr.com/i/vDXJAg)


Matchon：监控当前动画匹配是否开启

Max jump height：最大跳跃高度

Jump anim start offset：设置从跳跃动画的何处开始播放

Rise start moment：设置跳跃动画片段角色位置开始上升的时刻（百分比）

Fall start moment：设置跳跃动画片段角色位置开始下降的时刻（百分比）

Fall speed rise end moment：设置跳跃动画片段角色位置下降后下降速度上升到最高的时刻（百分比）【这部分算法有问题，不要深究】

由于翻越动画涉及参数(物体的长宽高，匹配时间点，落地检测时间点，动作衔接时间等）过多，故不设置可调节参数。

  

# 七、工程目录结构说明

![enter image description here](https://github.com/WindUpRadio/Images/blob/main/Catalogue.jpg?raw=true)

目录结构如上，其中：

- Data：预处理器文件，Configuration文件，以及动画数据库文件

- Locomotion：动画片段版本的Locomotion动画

- Materials：场景用到的材质

- MocapData：动捕动画文件

- Plugins：原插件中的模型，材质，动画等资源

- Scenes：场景

- Scripts：脚本

其中脚本文件夹目录结构如下：

![enter image description here](https://github.com/WindUpRadio/Images/blob/main/Scripts.jpg?raw=true)

- AnimationPlay：动画播放相关脚本

- Basic：基础数据的类定义脚本，如PoseData，AnimationData，TrajPoints等

- GetAnim：GetAnim插件所有脚本

- MotionMatch：动作匹配以及管理其他脚本的脚本

- PreProcessor： 预处理相关脚本

- Scene：场景相关脚本：镜头跟随，UI等

- Trajctory：轨迹生成相关脚本
