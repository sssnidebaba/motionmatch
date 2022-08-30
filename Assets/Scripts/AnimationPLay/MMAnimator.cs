using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[RequireComponent(typeof(Animator))]
public class MMAnimator
{
    public Animator animator;
    //���Ŷ���
    public AnimationClip []clips;
    
    //��Ҫ���
    PlayableGraph graph;
    public ScriptPlayable<MyPlayableBehaviour> myplayable;
    public MyPlayableBehaviour behaviour;
    AnimationPlayableOutput playableOutput;
    //����ʱ��
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
    //������ʱ��Ĳ���
    public void PlayAnimationClip(int id,double time,float ttime)
    {
        TransitionTime = ttime;
        behaviour.PlayAnimation(graph, clips[id], time, TransitionTime);
    }
    //���붯������
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
    //��������ʱ��Ĳ��ţ�����Ĭ�Ϲ���ʱ��
    public void PlayAnimationClip(int id,double time)
    {
        TransitionTime = DefaultTransitionTime;
        behaviour.PlayAnimation(graph, clips[id], time, TransitionTime);
    }
    //���Ż�϶���
    public void PlayBlendAnimationClips(int id0, int id1, double time,float weight)
    {
        TransitionTime = DefaultTransitionTime;
        behaviour.PlayBlendingAnimation(graph, clips[id0], clips[id1], time,weight, TransitionTime);
    }
    //��ȡ��ǰ��������λ��
    public double Timeschedule()
    {
        AnimationClipPlayable t = (AnimationClipPlayable)behaviour.mixerplayable[0].GetInput(0);
        return t.GetTime() / t.GetAnimationClip().length;
    }
    //��ȡ��ǰ��������
    public string Nowclipname()
    {
        AnimationClipPlayable t = (AnimationClipPlayable)behaviour.mixerplayable[0].GetInput(0);
        return t.GetAnimationClip().name;
    }
    
    
    //������playable����
    public void Connect()
    {
        playableOutput.SetSourcePlayable(myplayable);

    }
    //�����Ͽ���playable������
    public void Disconnect()
    {
        playableOutput.SetSourcePlayable(Playable.Null);
    }
}
