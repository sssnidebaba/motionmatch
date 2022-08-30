using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

[SerializeField]
public class MyPlayableBehaviour : PlayableBehaviour
{
    // Start is called before the first frame update
    AnimationMixerPlayable mixerPlayable;
    public float[] weights = new float[5] { 0, 0, 0, 0, 0 };
    public AnimationMixerPlayable[] mixerplayable = new AnimationMixerPlayable[5];
    public float[] weightratios = new float[5] { 0, 0, 0, 0, 0 };
    AnimationClipPlayable clipPlayable0;
    float TransitionTime = 0.2f;
    public float restweight;
    public int nextout = 1;
    double lasttime = -1;
    bool ok = true;
    public void init(PlayableGraph graph,Playable playable,AnimationClip clip)
    {
        playable.SetInputCount(1);
        mixerPlayable = AnimationMixerPlayable.Create(graph, 5);
        graph.Connect(mixerPlayable, 0, playable, 0);
        playable.SetInputWeight(0, 1);
        mixerplayable[0] = AnimationMixerPlayable.Create(graph, 2);
        AnimationClipPlayable temp = AnimationClipPlayable.Create(graph, clip);
        mixerplayable[0].ConnectInput(0, temp, 0);
        mixerplayable[0].SetInputWeight(0, 1);
        mixerPlayable.ConnectInput(0, mixerplayable[0], 0);
        weights[0] = 1;
    }
    public void PlayAnimation(PlayableGraph graph,AnimationClip clip,double time,float transitiontime)
    {
        restweight = weights[nextout];
        if (restweight > 0.05f&&(clip.name!= "RunJump_ToRight_2"&&clip.name!="JumpDown")) return;
        if (time == lasttime)
            return;
        else
        {
            lasttime = time;
        }
        weights[nextout] = weights[0];
        if (!mixerplayable[nextout].IsNull())
        mixerplayable[nextout].Destroy();
        mixerplayable[nextout] = mixerplayable[0];
        weights[0] = 0;
        
        float sum = weights[1] + weights[2] + weights[3] + weights[4];
        if(sum == 0)
        {
            weightratios[1] = 0;
            weightratios[2] = 0;
            weightratios[3] = 0;
            weightratios[4] = 0;

        }
        else
        {
            weightratios[1] = weights[1] / sum;
            weightratios[2] = weights[2] / sum;
            weightratios[3] = weights[3] / sum;
            weightratios[4] = weights[4] / sum;
        }
        
        if (restweight > 0f)
        {

            weights[1] += weightratios[1] * restweight;
            weights[2] += weightratios[2] * restweight;
            weights[3] += weightratios[3] * restweight;
            weights[4] += weightratios[4] * restweight;

        }
        TransitionTime = transitiontime;

        mixerplayable[0] = AnimationMixerPlayable.Create(graph, 2);
        AnimationClipPlayable temp = AnimationClipPlayable.Create(graph, clip);
        temp.SetTime(time);
        mixerplayable[0].ConnectInput(0, temp, 0);
        mixerplayable[0].SetInputWeight(0, 1);
        mixerPlayable.DisconnectInput(0);
        mixerPlayable.ConnectInput(0, mixerplayable[0], 0);
        mixerPlayable.DisconnectInput(nextout);
        mixerPlayable.ConnectInput(nextout, mixerplayable[nextout], 0);
        nextout++;
        nextout = nextout == 5 ? 1 : nextout;
        ok = true;
    }
    public void PlayBlendingAnimation(PlayableGraph graph, AnimationClip clip0, AnimationClip clip1, double time, float weight,float transitiontime)
    {
        restweight = weights[nextout];
        if (restweight > 0.05f) return;
        if (time == lasttime)
            return;
        else
        {
            lasttime = time;
        }
        weights[nextout] = weights[0];
        if (!mixerplayable[nextout].IsNull())
            mixerplayable[nextout].Destroy();
        mixerplayable[nextout] = mixerplayable[0];
        weights[0] = 0;

        float sum = weights[1] + weights[2] + weights[3] + weights[4];
        if (sum == 0)
        {
            weightratios[1] = 0;
            weightratios[2] = 0;
            weightratios[3] = 0;
            weightratios[4] = 0;

        }
        else
        {
            weightratios[1] = weights[1] / sum;
            weightratios[2] = weights[2] / sum;
            weightratios[3] = weights[3] / sum;
            weightratios[4] = weights[4] / sum;
        }
        if (restweight > 0f)
        {

            weights[1] += weightratios[1] * restweight;
            weights[2] += weightratios[2] * restweight;
            weights[3] += weightratios[3] * restweight;
            weights[4] += weightratios[4] * restweight;

        }
        TransitionTime = transitiontime;

        mixerplayable[0] = AnimationMixerPlayable.Create(graph, 2);
        AnimationClipPlayable temp0 = AnimationClipPlayable.Create(graph, clip0);
        AnimationClipPlayable temp1 = AnimationClipPlayable.Create(graph, clip1);
        temp0.SetTime(time);
        temp1.SetTime(time);
        mixerplayable[0].ConnectInput(0, temp0, 0);
        mixerplayable[0].ConnectInput(1, temp1, 0);
        
        mixerplayable[0].SetInputWeight(0, weight);
        mixerplayable[0].SetInputWeight(1, 1-weight);
        mixerPlayable.DisconnectInput(0);
        mixerPlayable.ConnectInput(0, mixerplayable[0], 0);
        mixerPlayable.DisconnectInput(nextout);
        mixerPlayable.ConnectInput(nextout, mixerplayable[nextout], 0);
        nextout++;
        nextout = nextout == 5 ? 1 : nextout;
        ok = true;
    }
    public override void PrepareFrame(Playable playable, FrameData info)
    {
        if (ok)
        {
            setweights();
            ok = false;
        }
        else if(TransitionTime!=0)
        {
            weights[1] -= weightratios[1] * info.deltaTime / TransitionTime;
            weights[2] -= weightratios[2] * info.deltaTime / TransitionTime;
            weights[3] -= weightratios[3] * info.deltaTime / TransitionTime;
            weights[4] -= weightratios[4] * info.deltaTime / TransitionTime;
            weights[1] = weights[1] < 0 ? 0 : weights[1];
            weights[2] = weights[2] < 0 ? 0 : weights[2];
            weights[3] = weights[3] < 0 ? 0 : weights[3];
            weights[4] = weights[4] < 0 ? 0 : weights[4];
            weights[0] = 1 - weights[1] - weights[2] - weights[3] - weights[4];
            setweights();
        }else if(TransitionTime==0)
        {
            weights[1] = 0;
            weights[2] = 0;
            weights[3] = 0;
            weights[4] = 0;
            weights[0] = 1;
            setweights();
        }
        base.PrepareFrame(playable, info);    
    }
    private void setweights()
    {
        mixerPlayable.SetInputWeight(0, weights[0]);
        mixerPlayable.SetInputWeight(1, weights[1]);
        mixerPlayable.SetInputWeight(2, weights[2]);
        mixerPlayable.SetInputWeight(3, weights[3]);
        mixerPlayable.SetInputWeight(4, weights[4]);
    }
    

}
