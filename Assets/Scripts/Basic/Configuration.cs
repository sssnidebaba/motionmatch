using UnityEngine;

[CreateAssetMenu]
public class Configuration : ScriptableObject
{
    [Tooltip("平衡动画流畅度和动画响应速度，值越高动画响应速度速度越快，动画流畅度越低")]
    [Range(0, 1)]
    public float PoseTrajectoryRatio;
    [Tooltip("循环动画的帧的cost乘此值")]
    [Range(0, 1)]
    public float LoopAnimFavour;
    [Tooltip("下一帧的cost乘此值")]
    [Range(0, 1)]
    public float NextPoseFavour;
    [Tooltip("当速度达到最大速度时，当前朝向和目标朝向的角度差小于此值时，会进行角度修正(单位：度)")]
    public float AngleThreshold;
    [Tooltip("角度修正速度(单位：度/每次匹配)")]
    public float AngleRiviseSpeed;
    [Tooltip("根速度权重")]
    public float BodyVelocityWeight;
    [Tooltip("预测轨迹朝向权重")]
    public float AngleMutiplier;
    [Tooltip("预测轨迹位置权重")]
    public float PositionMultiplier;
    [Tooltip("骨骼位置权重")]
    public float[] JointPositionWeights;
    [Tooltip("骨骼速度权重")]
    public float[] JointVelocityWeights;

    //预处理配置
    [Tooltip("采样时间，例如0.05代表1s的动画会采样出20条数据")]
    public float Interval;
    [Tooltip("轨迹点（决定匹配当前状态多少秒前/后的轨迹）")]
    public float[] TrajPoints;//轨迹点
    [Tooltip("选取哪些骨骼进行匹配")]
    public HumanBodyBones[] Bones;
}
