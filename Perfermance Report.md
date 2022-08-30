## 性能分析

### 1、总体帧数
![](https://github.com/WindUpRadio/Images/blob/main/average%20fps.jpg?raw=true)
![](https://github.com/WindUpRadio/Images/blob/main/average%20fps_.jpg?raw=true)
测量总体帧数使用了两种办法，结论是在Unity Editor中全屏稳定运行时的平均帧率大约为420帧。
### 2、GC次数
在我们的Motion Matching方案中，我们设计的是每隔interval时间进行一次匹配，本次测试中使用的值是0.03333。下图中每个固定时间出现的小尖峰也可以反映这一点。
![](https://github.com/WindUpRadio/Images/blob/main/pinnacle.jpg?raw=true)
每次匹配时大约进行30次GC.Alloc，不匹配时为0
![](https://github.com/WindUpRadio/Images/blob/main/gc%20times.jpg?raw=true)
### 3、CPU均值
排除掉Editor本身的CPU占用的话，每次匹配时大约花费5msCPU时间，不匹配时大约花费0.75msCPU时间。
![](https://github.com/WindUpRadio/Images/blob/main/cpu%20time.jpg?raw=true)

### 4、内存占用
内存占用稳定在0.78GB左右，除去Profiler消耗的大约325MB内存，占用大约为463MB。
其中堆内存共约315MB，运行过程中使用的约228MB。
![](https://github.com/WindUpRadio/Images/blob/main/memory.jpg?raw=true)
### 5、影响性能的主要函数
经分析，性能瓶颈主要出现在MotionManager.Update函数上，计算Cost并进行匹配，以及调用动画播放接口的功能在这个函数中执行。
![](https://raw.githubusercontent.com/WindUpRadio/images/main/image1.png)
可以看到轨迹生成相关脚本CPU占用时间均为0。而Update函数中，主要是计算Cost与匹配影响性能。
### 6、与单线程版本的对比
我们方案的最终版本使用了C#原生多线程，根据CPU核心数共创建了6个子线程。
单线程版本的CPU占用时间大约为9ms，大约是多线程版本的两倍，帧率大约是340帧，比多线程版本慢80帧。
![](https://github.com/WindUpRadio/Images/blob/main/image.png?raw=true)
### 7、配置
Unity Editor 版本：2021.3.5f1c1
操作系统版本：Windows 10 企业版 LTSC
CPU：Intel I5-8400 2.8GHZ
RAM：16GB
显卡：GTX 1650

