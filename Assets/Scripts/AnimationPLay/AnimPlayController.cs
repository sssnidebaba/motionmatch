using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimPlayController : MonoBehaviour
{
    //动画状态机
    Animator Anim;
    //判断动画是否播放，用于动画结束的过渡
    bool Isplaying = true;
    //用户设置的循环动画
    public AnimationClip[] LoopClips;
    //用户设置的两动画过渡
    public NextAnimation[] NowAndNext;
    //过渡时间
    public float TransitionTime = 0.1f;
    //角色控制器
    CharacterController CC;
    //用于处理角色控制器偶尔浮空的bug
    float DownSpeed=0;
    //有下一动画的动画
    private List<AnimationClip> CurrentClips=new List<AnimationClip>() ;
    private void Awake()
    {
        //获取有下一动画的动画
        for (int i = 0; i < NowAndNext.Length; i++)
        {
            CurrentClips.Add(NowAndNext[i].now);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        Anim = GetComponent<Animator>();
        CC = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        //当前播放片段
        AnimationClip ac;
        AnimatorStateInfo animatorInfo;
        animatorInfo = Anim.GetCurrentAnimatorStateInfo(0);
        //有概率获取失败
        try
        {
            
            ac = Anim.GetCurrentAnimatorClipInfo(0)[0].clip;
            if(animatorInfo.normalizedTime > 1.0f && Isplaying)
            {
                //Debug.Log("到了");
                if (CurrentClips!=null&&CurrentClips.Contains(ac))
                {
                    PlayNextAnimation(ac);
                    //Debug.Log("Next");
                }
                else if( ((IList)LoopClips).Contains(ac))
                {
                    //Debug.Log("Loop");
                    LoopPlay(ac);
                    
                }
                else
                {
                    //Debug.Log("错了");
                }
            }
            else if (animatorInfo.normalizedTime < 1.0f)
            {
                //Debug.Log("没到");
                Isplaying = true;
            }
            AddGravity();
        }
        catch
        {

        }
       
        //Debug.Log(DownSpeed);
    }
    //播放函数（只需要调用这个函数）
    //动画名称 进度百分比位置
    public void PlayAnimation(string AnimationName,float Offtime)
    {
        //anim.CrossFade(AnimationName, TransitionTime, 0, Offtime);
        Anim.CrossFade(AnimationName,TransitionTime,0,Offtime);
    }

    //角色控制器碰撞接口（暂时用不到）
    /*private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        //Debug.Log(hit.gameObject.name);
    }*/
    //Ik优化（还没写）
    private void OnAnimatorIK(int layerIndex)
    {
        
    }
    //带过渡的循环播放
    private void LoopPlay(AnimationClip now)
    {
        
        //1.动画结束 2.动画的系统属性为不循环 3.是否正在播放 4.自定义循环是否包括
        if (( !now.isLooping  ))//normalizedTime: 范围0 -- 1, 0是动作开始，1是动作结束
        {
            Isplaying = false;
            Anim.CrossFade(now.name, TransitionTime, 0, 0);
        }
        
        
    }
    //对具有未来动画的动画进行控制
    private void PlayNextAnimation(AnimationClip now)
    {
        for(int i=0;i<NowAndNext.Length;i++)
        {
            if (NowAndNext[i].now==now)
            {
                Debug.Log(NowAndNext[i].next.name);
                Anim.CrossFade(NowAndNext[i].next.name, TransitionTime, 0, 0);
                break;
            }
        }
    }
    //对角色控制器的优化
    private void AddGravity()
    {
        if (!CC.isGrounded)
        {
            DownSpeed += -9.8f * Time.deltaTime;
            CC.Move(DownSpeed * Vector3.up);
        }
        else
        {
            DownSpeed = 0f;
        }
    }
    //调试函数
    public void play()
    {
        //anim.CrossFade("Idle2Run180L", 0.1f, 0, 0.5f);
        PlayAnimation("Idle2Run180L", 0.5f);
        //animat.Play(anim[1].name, -1, 0.5f);
    }
    public void play2()
    {
        //anim.CrossFade("WalkFWD", 0.1f, 0, 0.5f);
        PlayAnimation("WalkFWD", 0.5f);
        //animat.Play(anim[1].name, -1, 0.5f);
    }
    public void play3()
    {
        PlayAnimation("RunFwdStop", 0f);
    }
}
