using UnityEngine;
[System.Serializable]
public class PoseData
{
    public int PoseID;
    public float Time;
    public int NextPoseID;
    public int ClipID;
    public Vector3 LocalVelocity;
    public JointData[] JointsData;
    public TrajectoryPoint[] Trajectory;
    public AnimationType AnimationType;
    public float[] tags = new float[(int)TagType.End];
    public bool IsBlendClip;
    public float Weight;
    public Vector2 SourceClipID;

    public PoseData(Vector3 localVelocity, int jointCount, int trajectoryCount, int poseID, float time, int clipID, AnimationType type)
    {
        LocalVelocity = localVelocity;
        JointsData = new JointData[jointCount];
        for (int i = 0; i < jointCount; i++)
        {
            JointsData[i] = new JointData(new Vector3(), new Vector3());
        }
        Trajectory = new TrajectoryPoint[trajectoryCount];
        for (int i = 0; i < trajectoryCount; i++)
        {
            Trajectory[i] = new TrajectoryPoint(new Vector3(), 0f);
        }
        PoseID = poseID;
        Time = time;
        ClipID = clipID;
        AnimationType = type;
    }
}

public enum AnimationType
{
    Idle,
    Locomotion
}