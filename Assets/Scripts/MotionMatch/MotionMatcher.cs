using System;
using System.Threading;
using UnityEngine;

/// <summary>
/// ����MotionMatch��ȡƥ���֡ID
/// </summary>
class MotionMatcher
{

    public PoseData[] Data;
    public PreProcessor processor;
    Configuration config;

    ManualResetEvent[] doneEvents;// �����߳�ͬ��

    public PoseData Predict; // Ԥ���֡
    int bestAnimID = 0; // cost��С��ƥ��֡ID
    float bestCost = 0; // bestAnimID֡��costֵ
    int lastBestAnimId = 0; // ��һ��cost��С��֡ID
    // ��weight�л�ȡ��Ȩ��
    public float BodyVelocityWeight; // ���ٶ�Ȩ��
    public float AngleMutiplier; // Ԥ��켣����Ȩ��
    public float PositionMultiplier; // Ԥ��켣λ��Ȩ��
    public float[] JointPositionWeights; // ����λ��Ȩ��
    public float[] JointVelocityWeights; // �����ٶ�Ȩ��

    private bool[] isLooping;
    private float[] cost;
    public MotionMatcher(PoseData Predict,
        PoseData[] Data,
        AnimationClip[] clips,
        Configuration config)
    {
        this.Predict = Predict;
        this.Data = Data;
        this.config = config;

        isLooping = new bool[Data.Length];
        for (int i = 0; i < Data.Length; i++)
        {
            isLooping[i] = clips[Data[i].ClipID].isLooping;
        }

        AngleMutiplier = config.AngleMutiplier * config.PoseTrajectoryRatio;
        PositionMultiplier = config.PositionMultiplier * config.PoseTrajectoryRatio;

        BodyVelocityWeight = config.BodyVelocityWeight * (1 - config.PoseTrajectoryRatio);
        JointPositionWeights = new float[config.JointPositionWeights.Length];
        for (int i = 0; i < JointPositionWeights.Length; i++)
        {
            JointPositionWeights[i] = config.JointPositionWeights[i] * (1 - config.PoseTrajectoryRatio);
        }
        JointVelocityWeights = new float[config.JointVelocityWeights.Length];
        for (int i = 0; i < JointVelocityWeights.Length; i++)
        {
            JointVelocityWeights[i] = config.JointVelocityWeights[i] * (1 - config.PoseTrajectoryRatio);
        }

        cost = new float[Data.Length];
        doneEvents = new ManualResetEvent[Environment.ProcessorCount];
        for (int i = 0; i < doneEvents.Length; i++)
        {
            doneEvents[i] = new ManualResetEvent(false);
        }
    }
    private float GetCost(int id)
    {
        if (id == lastBestAnimId ||
            Data[id].AnimationType != Predict.AnimationType ||
            Data[id].tags[(int)(TagType.DoNotUse)] != 0)
            return cost[id] = float.MaxValue;

        cost[id] = 0;
        // ���ٶ�
        cost[id] += BodyVelocityWeight * (Predict.LocalVelocity - Data[id].LocalVelocity).magnitude;

        // Ԥ��켣
        float deltaAngle = 0, deltaPosition = 0;
        for (int i = 0; i < Predict.Trajectory.Length; i++)
        {
            deltaAngle += Mathf.Abs(Mathf.DeltaAngle(Predict.Trajectory[i].FacingAngle, Data[id].Trajectory[i].FacingAngle));
            deltaPosition += (Predict.Trajectory[i].Position - Data[id].Trajectory[i].Position).magnitude;
        }

        cost[id] += PositionMultiplier * deltaPosition;
        cost[id] += AngleMutiplier * deltaAngle;

        // ����
        float deltaVelocity = 0; deltaPosition = 0;
        for (int i = 0; i < Predict.JointsData.Length; i++)
        {
            deltaVelocity += JointVelocityWeights[i] * (Predict.JointsData[i].Velocity - Data[id].JointsData[i].Velocity).magnitude;
            deltaPosition += JointPositionWeights[i] * (Predict.JointsData[i].Position - Data[id].JointsData[i].Position).magnitude;
        }
        cost[id] += deltaPosition;
        cost[id] += deltaVelocity;

        // ����
        if (isLooping[id]) cost[id] *= config.LoopAnimFavour;
        if (Data[lastBestAnimId].NextPoseID == id) cost[id] *= config.NextPoseFavour;

        return cost[id];
    }

    public void ThreadFor(int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            GetCost(i);
        }
    }

    public int MotionMatch()
    {
        lastBestAnimId = bestAnimID;

        int lenPerThread = (Data.Length + Environment.ProcessorCount - 1) / Environment.ProcessorCount;
        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            doneEvents[i].Reset();
            ThreadPool.QueueUserWorkItem(i =>
            {
                ThreadFor(lenPerThread * (int)i, Math.Min(Data.Length, lenPerThread * ((int)(i) + 1)));
                doneEvents[(int)i].Set();
            },i);
        }

        WaitHandle.WaitAll(doneEvents);
        bestCost = float.MaxValue;
        for (int i = 0; i < Data.Length; i++)
        {
            if (cost[i] < bestCost)
            {
                bestCost = cost[i];
                bestAnimID = i;
            }
        }
        return bestAnimID;
    }
}
