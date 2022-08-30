using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

class MotionManager : MonoBehaviour
{
    // GameObject
    public UnityEngine.Camera mCamera;// ���澵ͷ
    Transform root; // ��ɫ��root

    // Script
    public AnimationData animationData; // Ԥ�������ݽű�
    TrajectoryGenerate trajectoryGenerate; // �켣Ԥ��ű�
    MotionMatcher motionMatcher; // ƥ��ű�   
    public MMAnimator mmAnimator; // ���Ŷ����ű�


    // Data
    [HideInInspector]
    public AnimationClip[] clips; // ����
    private PoseData[] Data; // ����֡����
    private Configuration config; // Ȩ��
    [HideInInspector]
    private HumanBodyBones[] Bones; // ƥ��Ĺ�����
    [HideInInspector]
    public float[] TrajPoints; // ƥ��Ĺ켣ʱ���

    // Variable
    private bool isRun; // ��ǰ�Ƿ���ȫ��ֱ���ܣ��Ƕȿ�����Щ���࣬���������Ƕȣ�
    private float matchInterval; // ƥ��ʱ����
    private float timeSinceLastMatch; // ������һ��ƥ���ѹ�ȥ��ʱ��
    PoseData Predict; // ����Ҫƥ���֡������
    PoseData requireFrame; // getRequireFrame()��������������ݴ��λ��
    int currentAnimID = 0; // ��ǰ���ŵ�֡��ID
    float TransitionTime; // ��¼һ���������ʱ�䣬���ڶ�̬������ָ�
    // �������ڲ��Զ�������Ч��
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
        if (timeSinceLastMatch > matchInterval)// ����һ֡ʱ������ƥ��
        {
            UpdatePredict(); // ����Ԥ��
            if (isRun)
            {// ��ȫ����ʱ�Զ������Ƕ�
                float delta = Mathf.DeltaAngle(MoveData.nowDir, transform.rotation.eulerAngles.y - 180);
                transform.Rotate(0, Mathf.Clamp(delta, -config.AngleRiviseSpeed, config.AngleRiviseSpeed), 0);
            }

            if (loopPlay) // ѭ�����Ų��ֶ���
            {
                orderP = (orderP + 1) % order.Count;
                currentAnimID = order[orderP];
            }
            else // ����ƥ��
            {
                currentAnimID = motionMatcher.MotionMatch();
            }
            if (AdvancedMove.instance.matchOn)
            {
                mmAnimator.PlayAnimationClip(Data[currentAnimID].ClipID, Data[currentAnimID].Time); // ���Ŷ���
            }
            trajectoryGenerate.nowAnimData = Data[currentAnimID]; //�滭�켣
            timeSinceLastMatch = 0;
        }
    }

    // ��currentAnimID֡Ϊ��׼����ȡtimeSinceLastMatch + offsetTimeʱ����PoseData��Ϣ
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
        // ��һ֡���⴦��
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
    // �ӹ켣Ԥ���л�ȡ���ݺ����ת������
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
        // ��ȡ��ǰ������Ϣ�����ٶ�
        GetRequireFrame(0);
        Predict.LocalVelocity = requireFrame.LocalVelocity;
        for (int i = 0; i < requireFrame.JointsData.Length; ++i)
        {
            Predict.JointsData[i].Velocity = requireFrame.JointsData[i].Velocity;
            Predict.JointsData[i].Position = requireFrame.JointsData[i].Position;
            Predict.JointsData[i].Position += Predict.JointsData[i].Velocity * matchInterval;
        }

        // ����켣
        UpdateTra();

        // �ж��Ƿ�ֹ
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

        // ����Idleʱ����ʱ���ӳ�
        if (Predict.AnimationType == AnimationType.Idle)
        {
            mmAnimator.DefaultTransitionTime = 0.1f;
        }
        else
        {
            mmAnimator.DefaultTransitionTime = TransitionTime;
        }

        // �ж��Ƿ���ȫ�����ҽǶȲ�಻��
        isRun = Mathf.Abs(Mathf.DeltaAngle(MoveData.nowDir, MoveData.aimDir)) < config.AngleThreshold
                && Mathf.Abs(MoveData.velocity.magnitude - MoveData.maxSpeed) < 0.1f;
    }
    private void OnDestroy()
    {
        mmAnimator.Destroy();
    }
}
