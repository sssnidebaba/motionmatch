using UnityEngine;

[CreateAssetMenu]
public class Configuration : ScriptableObject
{
    [Tooltip("ƽ�⶯�������ȺͶ�����Ӧ�ٶȣ�ֵԽ�߶�����Ӧ�ٶ��ٶ�Խ�죬����������Խ��")]
    [Range(0, 1)]
    public float PoseTrajectoryRatio;
    [Tooltip("ѭ��������֡��cost�˴�ֵ")]
    [Range(0, 1)]
    public float LoopAnimFavour;
    [Tooltip("��һ֡��cost�˴�ֵ")]
    [Range(0, 1)]
    public float NextPoseFavour;
    [Tooltip("���ٶȴﵽ����ٶ�ʱ����ǰ�����Ŀ�곯��ĽǶȲ�С�ڴ�ֵʱ������нǶ�����(��λ����)")]
    public float AngleThreshold;
    [Tooltip("�Ƕ������ٶ�(��λ����/ÿ��ƥ��)")]
    public float AngleRiviseSpeed;
    [Tooltip("���ٶ�Ȩ��")]
    public float BodyVelocityWeight;
    [Tooltip("Ԥ��켣����Ȩ��")]
    public float AngleMutiplier;
    [Tooltip("Ԥ��켣λ��Ȩ��")]
    public float PositionMultiplier;
    [Tooltip("����λ��Ȩ��")]
    public float[] JointPositionWeights;
    [Tooltip("�����ٶ�Ȩ��")]
    public float[] JointVelocityWeights;

    //Ԥ��������
    [Tooltip("����ʱ�䣬����0.05����1s�Ķ����������20������")]
    public float Interval;
    [Tooltip("�켣�㣨����ƥ�䵱ǰ״̬������ǰ/��Ĺ켣��")]
    public float[] TrajPoints;//�켣��
    [Tooltip("ѡȡ��Щ��������ƥ��")]
    public HumanBodyBones[] Bones;
}
