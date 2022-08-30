#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;

public enum TagType
{
    Required,
    DoNotUse,
    End
}
[System.Serializable]
public class Tags
{
    public string animationName;
    public List<Vector2> intervals;

    public Tags(string name)
    {
        animationName = name;
        intervals = new();
    }
}

[System.Serializable]
public class BlendAnimationClips
{
    public bool isLooping;
    public int leftClipID;
    public int rightClipID;

    [Range(0f, 1f)]
    public float weight;
}

[CreateAssetMenu]
public class PreProcessor : ScriptableObject

{
    [HideInInspector]
    public List<Tags> myTags = new();

    private List<PoseData> Poses;

    [SerializeField]
    [Tooltip("混合动画集")]
    private BlendAnimationClips[] BlendClips;

    private HumanBodyBones[] Bones;

    private int Count;

    [SerializeField]
    [Tooltip("静立动画集")]
    private AnimationClip[] IdleClips;

    private float Interval;

    private Transform[] Joints;//关节

    [SerializeField]
    [Tooltip("移动动画集")]
    private AnimationClip[] LocomotionClips;//移动动画集

    private GameObject Model;

    private Animator ModelAnimator;


    private PlayableGraph playableGraph;

    [SerializeField]
    private GameObject Prefab;//模型

    private List<Vector3> RootPositions;//记录模型位置信息的辅助列表

    private List<Vector3> RootRotations;//记录模型角度信息的辅助列表

    private float[] TrajPoints;//轨迹点

    [SerializeField]
    [Tooltip("权重")]
    private Configuration config;//权重

    public void ClearTags()
    {
        myTags.Clear();
    }


    public void DestoryModel()
    {
        DestroyImmediate(Model);
    }

    /// <summary>
    /// 根据动画名找到对应Tag信息
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Tags FindTagsByName(string name)
    {
        if (!myTags.Any())
            return null;
        foreach (var s in myTags)
        {
            if (s.animationName == name)
            {
                return s;
            }
        }
        return null;
    }

    public List<PoseData> GetPoses()
    {
        return Poses;
    }
    /// <summary>
    /// 生成动画数据库的Asset文件
    /// </summary>
    /// <param name="path">路劲</param>
    public void GenerateAnimationData(string path)
    {
        AnimationData data = CreateInstance<AnimationData>();
        data.Poses = new PoseData[Poses.Count];
        data.config = config;Poses.CopyTo(data.Poses);
        data.clips = new AnimationClip[LocomotionClips.Length];
        LocomotionClips.ToList().CopyTo(data.clips);
        AssetDatabase.CreateAsset(data,path);
    }

    public HumanBodyBones[] GetBones()
    {
        return Bones;
    }

    public float GetInterval()
    {
        return Interval;
    }

    public AnimationClip[] GetClips()
    {
        return LocomotionClips;
    }

    public List<PoseData> GetPose()
    {
        return Poses;
    }

    public float[] GetTrajPoints()
    {
        return TrajPoints;
    }

    public Configuration GetWeights()
    {
        return config;
    }

    public bool HasIdleClips()
    {
        return IdleClips.Length != 0;
    }
    /// <summary>
    /// 初始化（替代构造函数）
    /// </summary>
    public void Initialize()
    {
        Model = GameObject.Instantiate(Prefab);
        ModelAnimator = Model.GetComponent<Animator>();
        ModelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Bones = config.Bones;
        TrajPoints = config.TrajPoints;
        Interval = config.Interval;
        Joints = new Transform[Bones.Length];
        for (int i = 0; i < Bones.Length; ++i)
        {
            Joints[i] = ModelAnimator.GetBoneTransform(Bones[i]);
        }
        Poses = new List<PoseData>();
        RootPositions = new List<Vector3>();
        RootRotations = new List<Vector3>();
        Count = 0;
    }

    /// <summary>
    /// 处理额外动画集前初始化（可优化）
    /// </summary>
    public void InitializeBeforeProcessingAdditionalAnimationSets()
    {
        Model = GameObject.Instantiate(Prefab);
        ModelAnimator = Model.GetComponent<Animator>();
        ModelAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Joints = new Transform[Bones.Length];
        for (int i = 0; i < Bones.Length; ++i)
        {
            Joints[i] = ModelAnimator.GetBoneTransform(Bones[i]);
        }
    }

    public void PrintInfo()//log函数
    {
        for (int i = 0; i < Count; ++i)
        {
            Debug.Log("Info " + i);
            Debug.Log("Pose ID " + Poses[i].PoseID);
            Debug.Log("Pose Time " + Poses[i].Time);
            Debug.Log("Clip ID " + Poses[i].ClipID);
            Debug.Log("Pose Velocity " + Poses[i].LocalVelocity);
            Debug.Log("Joints Info ");
            foreach (JointData m in Poses[i].JointsData)
            {
                Debug.Log("Joint Position " + m.Position);
                Debug.Log("Joint Velocity " + m.Velocity);
            }
            Debug.Log("Trajectory Info ");
            foreach (TrajectoryPoint m in Poses[i].Trajectory)
            {
                Debug.Log("Trajectory Position " + m.Position);
                Debug.Log("Facing Angle " + m.FacingAngle);
            }
        }
        Debug.Log(Count + "poses in total");
    }

    /// <summary>
    /// 处理移动以及混合动画
    /// </summary>
    public void ProcessAnimationClips()
    {
        for (int i = 0; i < LocomotionClips.Length; ++i)
        {
            Model.transform.rotation = Quaternion.Euler(0, 0, 0);//每次处理前模型的角度信息恢复原位
            ProcessAnim(LocomotionClips[i], i, AnimationType.Locomotion);
        }
        if (BlendClips.Any())
        {
            for (int i = 0; i < BlendClips.Length; ++i)
            {
                Model.transform.rotation = Quaternion.Euler(0, 0, 0);
                ProcessBlendAnim(BlendClips[i]);
            }
        }
        Debug.Log("Done");

    }
    /// <summary>
    /// 处理Idle动画
    /// </summary>
    public void ProcessIdleAnimation()
    {
        if (IdleClips.Length > 0)
        {
            InitializeBeforeProcessingAdditionalAnimationSets();
            int IdleIndex = LocomotionClips.Length;
            LocomotionClips = LocomotionClips.Concat(IdleClips).ToArray();//将Idle动画复制到LocomotionClips数组末尾
            Model.transform.rotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            for (int i = IdleIndex; i < LocomotionClips.Length; ++i)
            {
                ProcessAnim(LocomotionClips[i], i, AnimationType.Idle);
            }
        }
    }
    /// <summary>
    /// 移除Locomotion动画集中重复元素
    /// </summary>
    public void RemoveDuplicateElements()//移除重复元素
    {
        if (!HasIdleClips())
        {
            return;
        }
        foreach (var clip in IdleClips)//从Locomotion集中移除Idle集中有的动画
        {
            for (int i = LocomotionClips.Length - 1; i >= 0; --i)
            {
                if (LocomotionClips[i].name.Equals(clip.name))
                {
                    var clipList = LocomotionClips.ToList();
                    clipList.RemoveAt(i);
                    LocomotionClips = clipList.ToArray();
                }
            }
        }
        for (int i = 0; i < LocomotionClips.Length; ++i)//移除Locomotion集自身的重复元素
        {
            for (int j = LocomotionClips.Length - 1; j > i; --j)
            {
                if (LocomotionClips[i].name.Equals(LocomotionClips[j].name))
                {
                    var clipList = LocomotionClips.ToList();
                    clipList.RemoveAt(j);
                    LocomotionClips = clipList.ToArray();
                }
            }
        }
    }
    /// <summary>
    /// 切分Tag传递过来的一维数组
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public List<List<Vector2>> SplitList(List<Vector2> list)//Tag模块传过来的Tag是一维数组形式的，此处将一维数组按Tag种类分割成二维数组
    {
        List<List<Vector2>> res = new();
        for (int i = 0; i < (int)TagType.End; ++i)
        {
            res.Add(new());
        }
        for (int i = 0; i < list.Count; ++i)//Tag数组中第一个元素的y记录第一个标签所打的tag的个数
        {
            for (int j = i + 1; j < list[i].y + i + 1; ++j)//往后y个元素即y个tag具体的值
            {
                res[(int)list[i].x].Add(list[j]);
            }
            i += (int)list[i].y;
        }
        return res;
    }


    private void ProcessAnim(AnimationClip clip, int id, AnimationType type)
    {
        playableGraph = PlayableGraph.Create();

        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual); //将playableGraph更新模式设为手动更新

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", ModelAnimator); //将模型的Animator连接至PlayableGraph的输出

        var clipPlayable = AnimationClipPlayable.Create(playableGraph, clip);//创建要处理的动画的playable

        playableOutput.SetSourcePlayable(clipPlayable);//输出

        int start = Count; //start是每个处理的动画的Pose数据的第一帧在整个Pose数组中的下标
        //第一趟循环
        {
            for (float timef = 0; timef + Interval < clip.length; timef += Interval)
            {
                playableGraph.Evaluate(Interval);
                RootPositions.Add(Model.transform.position);//记录每次更新时模型的位置
                RootRotations.Add(Model.transform.rotation.eulerAngles);//记录每次更新时模型的角度信息
                Poses.Add(new PoseData(new Vector3(), Joints.Length, TrajPoints.Length, Count, timef, id, type));
                //关节位置
                for (int i = 0; i < Joints.Length; ++i)
                {
                    Poses[Count].JointsData[i] = new JointData(Model.transform.InverseTransformPoint(Joints[i].position), new Vector3());//记录关节位置相对于模型根节点的坐标
                }
                Count += 1;
            }
        }

        //根节点速度，关节速度
        {
            Poses[start].LocalVelocity = (RootPositions[start + 1] - RootPositions[start]) / Interval;//以当前帧和前一帧位置算平均速度
            for (int j = 0; j < Joints.Length; ++j)//每个动画第一帧特殊处理
            {
                if (!clip.isLooping) Poses[start].JointsData[j].Velocity = (Poses[start + 1].JointsData[j].Position
                        - Poses[start].JointsData[j].Position) / Interval;//非循环动画
                else Poses[start].JointsData[j].Velocity = (Poses[start].JointsData[j].Position
                        - Poses[Count - 1].JointsData[j].Position) / Interval;//循环动画
            }
            for (int i = start + 1; i < Count; ++i)//处理第二到最后一帧
            {
                Poses[i].LocalVelocity = (RootPositions[i] - RootPositions[i - 1]) / (Interval);
                for (int j = 0; j < Joints.Length; ++j)
                {
                    Poses[i].JointsData[j].Velocity = (Poses[i].JointsData[j].Position - Poses[i - 1].JointsData[j].Position) / Interval;
                }
            }
        }
        //计算轨迹
        for (int i = start; i < Count; ++i)
        {
            for (int j = 0; j < TrajPoints.Length; ++j)
            {
                float trajTime = Poses[i].Time + TrajPoints[j];//轨迹的时间点（可能小于0或大于clip.length）
                int poseID = Mathf.FloorToInt(trajTime / Interval);//轨迹在当前帧中的位置
                int length = Count - start;//当前动画的帧数
                if (poseID < 0)//如果轨迹在第0帧之前
                {
                    if (!clip.isLooping)//非循环动画以时间和速度做线性估计（可考虑加上角速度估计）
                    {
                        Vector3 trajVelocity = Poses[start].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * trajTime + RootPositions[start] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[start].y - RootRotations[i].y;
                    }
                    else//循环动画原方案是用playableGraph探测，发现有bug，于是弃用，也改为线性估计
                    {
                        Vector3 trajVelocity = Poses[start].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * trajTime + RootPositions[start] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[start].y - RootRotations[i].y;
                    }
                }
                else //如果轨迹点在最后一帧之后
                if (poseID >= length)
                {
                    if (!clip.isLooping)//估计
                    {
                        Vector3 trajVelocity = Poses[Count - 1].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * (trajTime - Poses[Count - 1].Time) + RootPositions[Count - 1] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[Count - 1].y - RootRotations[i].y;
                    }
                    else//循环动画直接用playableGraph模拟动画播放，记录完信息后再回退到当前状态
                    {
                        playableGraph.Evaluate(trajTime - (length - 1) * Interval);
                        Poses[i].Trajectory[j].Position = Model.transform.position - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = Model.transform.rotation.eulerAngles.y - RootRotations[i].y;
                        playableGraph.Evaluate((length - 1) * Interval - trajTime);
                    }
                }
                else //轨迹点落在中间帧
                {
                    float lerp = (trajTime / Interval) - (float)poseID;
                    Vector3 position;
                    float angle;
                    if (poseID + start + 1 < Count)//可插值
                    {
                        position = Vector3.Lerp(RootPositions[poseID + start], RootPositions[poseID + start + 1], lerp);
                        angle = Mathf.LerpAngle(RootRotations[poseID + start].y, RootRotations[poseID + start + 1].y, lerp);
                    }
                    else//如果是最后一帧插值会越界
                    {
                        position = RootPositions[poseID + start];
                        angle = RootRotations[poseID + start].y;
                    }
                    Poses[i].Trajectory[j].Position = position - RootPositions[i];
                    Poses[i].Trajectory[j].FacingAngle = angle - RootRotations[i].y;
                }
                Poses[i].Trajectory[j].Position = Quaternion.Euler(0f, -RootRotations[i].y, 0f) * Poses[i].Trajectory[j].Position;//坐标系转换成以人物朝向为参考
            }
        }

        var tags = FindTagsByName(clip.name);//找到当前动画对应的Tag
        //处理Tag
        if (tags != null)
        {
            var intervals = SplitList(tags.intervals);//intervals[i]即为第i个tag对应的tag区间
            for (int i = 0; i < intervals.Count; ++i)
            {
                List<Vector2> interval = intervals[i];
                switch (i)//处理第i个tag
                {
                    default:
                        {
                            foreach (var s in interval)//对于第i个tag的每个区间，给对应pose数据中tags数组的第i个元素赋值为10000f
                            {
                                int begin = Math.Max((int)(s.x / Interval), 0);
                                int end = Math.Min((int)(s.y / Interval), Poses.Count - start);
                                for (int j = begin + start; j < start + end; ++j)
                                {
                                    Poses[j].tags[i] = 10000f;
                                }
                            }
                        }
                        break;
                }
            }
        }
        for (int i = start; i < Poses.Count; ++i)//处理每帧的下一帧ID（非循环动画的最后一帧以及下一帧标记的不使用的帧NextPoseID的值设为-1
        {
            Poses[i].LocalVelocity = Quaternion.Euler(0f, -RootRotations[i].y, 0f) * Poses[i].LocalVelocity;
            if (i == Poses.Count - 1)
            {
                if (clip.isLooping)
                {
                    Poses[i].NextPoseID = start;
                }
                else
                {
                    Poses[i].NextPoseID = -1;
                }
            }
            else
            {
                if (Poses[i + 1].tags[((int)TagType.End) - 1] != 10000f)
                    Poses[i].NextPoseID = i + 1;
                else Poses[i].NextPoseID = -1;
            }
        }
        playableGraph.Destroy();//销毁playableGraph
    }
    /// <summary>
    /// 处理需要混合的动画
    /// </summary>
    /// <param name="clip"></param>
    private void ProcessBlendAnim(BlendAnimationClips clip)
    {
        playableGraph = PlayableGraph.Create();
        playableGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var mixer = AnimationMixerPlayable.Create(playableGraph);//混合动画playable
        mixer.SetInputCount(2);

        var clipPlayable1 = AnimationClipPlayable.Create(playableGraph, LocomotionClips[clip.leftClipID]);
        var clipPlayable2 = AnimationClipPlayable.Create(playableGraph, LocomotionClips[clip.rightClipID]);//将要混合的两个动画数据

        playableGraph.Connect(clipPlayable1, 0, mixer, 0);
        playableGraph.Connect(clipPlayable2, 0, mixer, 1);

        mixer.SetInputWeight(0, clip.weight);
        mixer.SetInputWeight(1, 1 - clip.weight);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", ModelAnimator);
        playableOutput.SetSourcePlayable(mixer);
        int start = Count;
        //其余处理流程类似普通动画
        clip.isLooping = LocomotionClips[clip.leftClipID].isLooping && LocomotionClips[clip.rightClipID].isLooping;
        for (float timef = 0; timef + Interval < Math.Max(LocomotionClips[clip.leftClipID].length, LocomotionClips[clip.rightClipID].length); timef += Interval)
        {
            playableGraph.Evaluate(Interval); //
            RootPositions.Add(Model.transform.position);
            RootRotations.Add(Model.transform.rotation.eulerAngles);
            Poses.Add(new PoseData(new Vector3(), Joints.Length, TrajPoints.Length, Count, timef, -1, AnimationType.Locomotion));
            Poses[Count].IsBlendClip = true; Poses[Count].SourceClipID = new Vector2(clip.leftClipID, clip.rightClipID); Poses[Count].Weight = clip.weight;
            for (int i = 0; i < Joints.Length; ++i)
            {
                Poses[Count].JointsData[i] = new JointData(Model.transform.InverseTransformPoint(Joints[i].position), new Vector3());
            }
            Count += 1;
        }
        //速度
        Poses[start].LocalVelocity = (RootPositions[start + 1] - RootPositions[start]) / Interval;
        for (int j = 0; j < Joints.Length; ++j)
        {
            if (!clip.isLooping) Poses[start].JointsData[j].Velocity = (Poses[start + 1].JointsData[j].Position - Poses[start].JointsData[j].Position) / (Interval);
            else Poses[start].JointsData[j].Velocity = (Poses[start].JointsData[j].Position - Poses[Count - 1].JointsData[j].Position) / (Interval);
        }
        for (int i = start + 1; i < Count; ++i)
        {
            Poses[i].LocalVelocity = (RootPositions[i] - RootPositions[i - 1]) / (Interval);
            for (int j = 0; j < Joints.Length; ++j)
            {
                Poses[i].JointsData[j].Velocity = (Poses[i].JointsData[j].Position - Poses[i - 1].JointsData[j].Position) / (Interval);
            }
        }
        //轨迹
        for (int i = start; i < Count; ++i)
        {
            for (int j = 0; j < TrajPoints.Length; ++j)
            {
                float trajTime = Poses[i].Time + TrajPoints[j];
                int poseID = Mathf.FloorToInt(trajTime / Interval);
                int length = Count - start;
                Vector3 distanceThroughoutWholeClip = RootPositions[Count - 1] - RootPositions[start];
                float angleThroughoutWholeClip = RootRotations[Count - 1].y - RootRotations[start].y;
                if (poseID < 0)
                {
                    if (!clip.isLooping)
                    {
                        Vector3 trajVelocity = Poses[start].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * trajTime + RootPositions[start] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[start].y - RootRotations[i].y;
                    }
                    else
                    {
                        Vector3 trajVelocity = Poses[start].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * trajTime + RootPositions[start] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[start].y - RootRotations[i].y;
                    }
                }
                else
                if (poseID >= length)
                {
                    if (!clip.isLooping)
                    {
                        Vector3 trajVelocity = Poses[Count - 1].LocalVelocity;
                        Poses[i].Trajectory[j].Position = trajVelocity * (trajTime - Poses[Count - 1].Time) + RootPositions[Count - 1] - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = RootRotations[Count - 1].y - RootRotations[i].y;
                    }
                    else
                    {
                        playableGraph.Evaluate(trajTime - (length - 1) * Interval);
                        Poses[i].Trajectory[j].Position = Model.transform.position - RootPositions[i];
                        Poses[i].Trajectory[j].FacingAngle = Model.transform.rotation.eulerAngles.y - RootRotations[i].y;
                        playableGraph.Evaluate((length - 1) * Interval - trajTime);
                    }
                }
                else
                {
                    float lerp = (trajTime / Interval) - (float)poseID;
                    Vector3 position;
                    float angle;
                    if (poseID + start + 1 < Count)
                    {
                        position = Vector3.Lerp(RootPositions[poseID + start], RootPositions[poseID + start + 1], lerp);
                        angle = Mathf.LerpAngle(RootRotations[poseID + start].y, RootRotations[poseID + start + 1].y, lerp);
                    }
                    else
                    {
                        position = RootPositions[poseID + start];
                        angle = RootRotations[poseID + start].y;
                    }
                    Poses[i].Trajectory[j].Position = position - RootPositions[i];
                    Poses[i].Trajectory[j].FacingAngle = angle - RootRotations[i].y;
                }
                Poses[i].Trajectory[j].Position = Quaternion.Euler(0f, -RootRotations[i].y, 0f) * Poses[i].Trajectory[j].Position;
            }
        }
        //nextID
        for (int i = start; i < Poses.Count; ++i)
        {
            Poses[i].LocalVelocity = Quaternion.Euler(0f, -RootRotations[i].y, 0f) * Poses[i].LocalVelocity;
            if (i == Poses.Count - 1)
            {
                if (clip.isLooping)
                {
                    Poses[i].NextPoseID = start;
                }
                else
                {
                    Poses[i].NextPoseID = -1;
                }
            }
            else Poses[i].NextPoseID = -1;
        }

    }
}
#endif