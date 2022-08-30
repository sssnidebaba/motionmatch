using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

class MotionManager : MonoBehaviour
{
    // GameObject
    public UnityEngine.Camera mCamera;// 跟随镜头
    Transform root; // 角色的root

    // Script
    public AnimationData animationData; // 预处理数据脚本
    TrajectoryGenerate trajectoryGenerate; // 轨迹预测脚本
    MotionMatcher motionMatcher; // 匹配脚本   
    public MMAnimator mmAnimator; // 播放动画脚本


    // Data
    [HideInInspector]
    public AnimationClip[] clips; // 动画
    private PoseData[] Data; // 动画帧数据
    private Configuration config; // 权重
    [HideInInspector]
    private HumanBodyBones[] Bones; // 匹配的骨骼点
    [HideInInspector]
    public float[] TrajPoints; // 匹配的轨迹时间点

    // Variable
    private bool isRun; // 当前是否在全速直线跑（角度可以有些许差距，用于修正角度）
    private float matchInterval; // 匹配时间间隔
    private float timeSinceLastMatch; // 距离上一次匹配已过去的时间
    PoseData Predict; // 最终要匹配的帧的数据
    PoseData requireFrame; // getRequireFrame()函数处理出的数据存放位置
    int currentAnimID = 0; // 当前播放的帧的ID
    float TransitionTime; // 记录一般情况过度时间，用于动态调整后恢复
    // 以下用于测试动画播放效果
    public bool loopPlay = false;
    public int beginIndex;
    public int endIndex;
    List<int> order;
    int orderP = 0;

    private void Start()
    {
        trajectoryGenerate = (TrajectoryGenerate)GetComponent("TrajectoryGenerate");
        mmAnimator = new MMAnimator();
        config = animationData.config;
        Bones = config.Bones;
        clips = animationData.clips;
        mmAnimator.clips = clips;
        mmAnimator.init(GetComponent<Animator>());
        TrajPoints = config.TrajPoints;
        trajectoryGenerate.Initialized(TrajPoints);
        int count = Bones.Length;
        Predict = new PoseData(new Vector3(), count, TrajPoints.Length, -1, -1, -1, AnimationType.Locomotion);
        requireFrame = new PoseData(new Vector3(), count, TrajPoints.Length, -1, -1, -1, AnimationType.Locomotion);
        root = gameObject.transform;
        Data = animationData.Poses;
        motionMatcher = new MotionMatcher(Predict, Data, clips, config);
        timeSinceLastMatch = 0;
        matchInterval = config.Interval;
        transform.position = new Vector3(0, 0, 0);
        order = new List<int>();
        for (int i = beginIndex; i <= endIndex; i++)
        {
            order.Add(i);
        }
        TransitionTime = mmAnimator.DefaultTransitionTime;
        ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
    }
    private void Update()
    {

        timeSinceLastMatch += Time.deltaTime;
        if (timeSinceLastMatch > matchInterval)// 超过一帧时间后进行匹配
        {
            UpdatePredict(); // 更新预测
            if (isRun)
            {// 当全速跑时自动修正角度
                float delta = Mathf.DeltaAngle(MoveData.nowDir, transform.rotation.eulerAngles.y - 180);
                transform.Rotate(0, Mathf.Clamp(delta, -config.AngleRiviseSpeed, config.AngleRiviseSpeed), 0);
            }

            if (loopPlay) // 循环播放部分动画
            {
                orderP = (orderP + 1) % order.Count;
                currentAnimID = order[orderP];
            }
            else // 正常匹配
            {
                currentAnimID = motionMatcher.MotionMatch();
            }
            if (AdvancedMove.instance.matchOn)
            {
                mmAnimator.PlayAnimationClip(Data[currentAnimID].ClipID, Data[currentAnimID].Time); // 播放动画
            }
            trajectoryGenerate.nowAnimData = Data[currentAnimID]; //绘画轨迹
            timeSinceLastMatch = 0;
        }
    }

    // 以currentAnimID帧为基准，获取timeSinceLastMatch + offsetTime时间后的PoseData信息
    private void GetRequireFrame(float offsetTime)
    {
        float chosenClipLength = Mathf.FloorToInt(clips[Data[currentAnimID].ClipID].length / matchInterval) * matchInterval - Mathf.Epsilon;
        bool chosenClipLooping = clips[Data[currentAnimID].ClipID].isLooping;

        float timePassed = timeSinceLastMatch + offsetTime;

        float newChosenTime = Data[currentAnimID].Time + timePassed;
        if (newChosenTime >= chosenClipLength)
        {
            if (chosenClipLooping)
            {
                newChosenTime %= chosenClipLength; //Loop
            }
            else
            {
                newChosenTime = chosenClipLength - matchInterval; //Clamp
            }

            timePassed = newChosenTime - Data[currentAnimID].Time;
        }
        int numPosesPassed;
        if (timePassed < Mathf.Epsilon)
        {
            numPosesPassed = Mathf.CeilToInt(timePassed / matchInterval);
        }
        else
        {
            numPosesPassed = Mathf.FloorToInt(timePassed / matchInterval);
        }

        float interpolationValue;
        PoseData beforePose, afterPose;
        interpolationValue = timePassed % matchInterval / matchInterval;
        // 第一帧特殊处理
        if (numPosesPassed == 0)
        {
            beforePose = Data[currentAnimID];
            afterPose = Data[currentAnimID + 1];
        }
        else
        {

            beforePose = Data[currentAnimID + numPosesPassed - 1];
            afterPose = Data[currentAnimID + numPosesPassed];
        }

        requireFrame.LocalVelocity = Vector3.Lerp(beforePose.LocalVelocity,
               afterPose.LocalVelocity, interpolationValue);

        for (int i = 0; i < beforePose.JointsData.Length; ++i)
        {
            requireFrame.JointsData[i].Velocity = Vector3.Lerp(beforePose.JointsData[i].Velocity,
                afterPose.JointsData[i].Velocity, interpolationValue);
            requireFrame.JointsData[i].Position = Vector3.Lerp(beforePose.JointsData[i].Position,
               afterPose.JointsData[i].Position, interpolationValue);
        }

        for (int i = 0; i < beforePose.Trajectory.Length; ++i)
        {
            requireFrame.Trajectory[i].Position = Vector3.Lerp(beforePose.Trajectory[i].Position,
                afterPose.Trajectory[i].Position, interpolationValue);
            requireFrame.Trajectory[i].FacingAngle = Mathf.LerpAngle(beforePose.Trajectory[i].FacingAngle,
                afterPose.Trajectory[i].FacingAngle, interpolationValue);
        }
        if (interpolationValue < 0.5)
        {
            for (int i = 0; i < requireFrame.tags.Length; i++)
            {
                requireFrame.tags[i] = beforePose.tags[i];
            }
        }
        else
        {
            for (int i = 0; i < requireFrame.tags.Length; i++)
            {
                requireFrame.tags[i] = afterPose.tags[i];
            }
        }
    }
    // 从轨迹预测中获取数据后进行转换处理
    private void UpdateTra()
    {
        float deltaAngle = -root.transform.eulerAngles.y;
        TrajectoryData tra = trajectoryGenerate.Gettraject();

        for (int i = 0; i < tra.pastDir.Count; i++)
        {
            Predict.Trajectory[i] = new TrajectoryPoint(tra.pastpos[i],
                (tra.pastDir[i] + deltaAngle) % 360);
        }
        for (int i = 0; i < tra.futuredir.Count; i++)
        {
            Predict.Trajectory[tra.pastpos.Count + i] = new TrajectoryPoint(Quaternion.Euler(0, deltaAngle, 0) * tra.futurepos[i],
                (tra.futuredir[i] + deltaAngle) % 360);
        }
    }
    private void UpdatePredict()
    {
        // 获取当前骨骼信息及根速度
        GetRequireFrame(0);
        Predict.LocalVelocity = requireFrame.LocalVelocity;
        for (int i = 0; i < requireFrame.JointsData.Length; ++i)
        {
            Predict.JointsData[i].Velocity = requireFrame.JointsData[i].Velocity;
            Predict.JointsData[i].Position = requireFrame.JointsData[i].Position;
            Predict.JointsData[i].Position += Predict.JointsData[i].Velocity * matchInterval;
        }

        // 处理轨迹
        UpdateTra();

        // 判断是否静止
        Predict.AnimationType = AnimationType.Idle;
        for (int i = 0; i < Predict.Trajectory.Length; i++)
        {
            if (TrajPoints[i] > 0)
            {
                if (Mathf.Abs(Predict.Trajectory[i].Position.x) > 1e-6) Predict.AnimationType = AnimationType.Locomotion;
                if (Mathf.Abs(Predict.Trajectory[i].Position.y) > 1e-6) Predict.AnimationType = AnimationType.Locomotion;
                if (Mathf.Abs(Predict.Trajectory[i].Position.z) > 1e-6) Predict.AnimationType = AnimationType.Locomotion;
            }
        }

        // 进入Idle时过渡时间延长
        if (Predict.AnimationType == AnimationType.Idle)
        {
            mmAnimator.DefaultTransitionTime = 0.1f;
        }
        else
        {
            mmAnimator.DefaultTransitionTime = TransitionTime;
        }

        // 判断是否在全速跑且角度差距不大
        isRun = Mathf.Abs(Mathf.DeltaAngle(MoveData.nowDir, MoveData.aimDir)) < config.AngleThreshold
                && Mathf.Abs(MoveData.velocity.magnitude - MoveData.maxSpeed) < 0.1f;
    }
    private void OnDestroy()
    {
        mmAnimator.Destroy();
    }
}
