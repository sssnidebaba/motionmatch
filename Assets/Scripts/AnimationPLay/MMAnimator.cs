using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public class MMAnimator
{
    public Animator animator;
    //播放动画
    public AnimationClip []clips;
    
    //必要组件
    PlayableGraph graph;
    public ScriptPlayable<MyPlayableBehaviour> myplayable;
    public MyPlayableBehaviour behaviour;
    AnimationPlayableOutput playableOutput;
    //过渡时间
    public float DefaultTransitionTime = 0.2f;
    public float TransitionTime = 0.2f;

    public void init(Animator animator)
    {
        if (this.animator == null)
            this.animator = animator;
        graph = PlayableGraph.Create("MxMAnimation");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        myplayable = ScriptPlayable<MyPlayableBehaviour>.Create(graph);       
        behaviour = myplayable.GetBehaviour();
        playableOutput = AnimationPlayableOutput.Create(graph, "Animation", animator);
        playableOutput.SetSourcePlayable(myplayable);
        behaviour.init(graph, myplayable, clips[clips.Length - 1]);
        graph.Play();
    }
    public void Destroy()
    {
        graph.Destroy();
    }
    //带过渡时间的播放
    public void PlayAnimationClip(int id,double time,float ttime)
    {
        TransitionTime = ttime;
        behaviour.PlayAnimation(graph, clips[id], time, TransitionTime);
    }
    //传入动画播放
    public void PlayAnimationClipByClip(AnimationClip clip, double time)
    {
        TransitionTime = DefaultTransitionTime;
        behaviour.PlayAnimation(graph, clip, time, TransitionTime);
    }
    public void PlayAnimationClipByClip(AnimationClip clip, double time,float transitionTime)
    {
        TransitionTime = transitionTime;
        behaviour.PlayAnimation(graph, clip, time, TransitionTime);
    }
    //不带过渡时间的播放（调用默认过渡时间
    public void PlayAnimationClip(int id,double time)
    {
        TransitionTime = DefaultTransitionTime;
        behaviour.PlayAnimation(graph, clips[id], time, TransitionTime);
    }
    //播放混合动画
    public void PlayBlendAnimationClips(int id0, int id1, double time,float weight)
    {
        TransitionTime = DefaultTransitionTime;
        behaviour.PlayBlendingAnimation(graph, clips[id0], clips[id1], time,weight, TransitionTime);
    }
    //获取当前动画播放位置
    public double Timeschedule()
    {
        AnimationClipPlayable t = (AnimationClipPlayable)behaviour.mixerplayable[0].GetInput(0);
        return t.GetTime() / t.GetAnimationClip().length;
    }
    //获取当前动画名字
    public string Nowclipname()
    {
        AnimationClipPlayable t = (AnimationClipPlayable)behaviour.mixerplayable[0].GetInput(0);
        return t.GetAnimationClip().name;
    }
    
    
    //动画由playable控制
    public void Connect()
    {
        playableOutput.SetSourcePlayable(myplayable);

    }
    //动画断开与playable的连接
    public void Disconnect()
    {
        playableOutput.SetSourcePlayable(Playable.Null);
    }
}
