using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedMove : MonoBehaviour
{
    public static AdvancedMove instance;
    public bool matchOn;
    public float maxJumpHeight;


    [SerializeField]
    private double jumpAnimStartOffset, riseStartMoment, fallStartMoment, fallSpeedRiseEndMoment;
    [SerializeField]
    private AnimationClip jumpAnim;
    public AnimationClip jumpdownclip;
    private Animator anim;
    private MMAnimator mxmAnimator;
    private CharacterController charaterControl;
    private Vector3 jumpAimPoint, jumpStartPoint, jumpMidPoint;
    private float fallSpeed;
    private bool isJump,isClimb;
    private RaycastHit hit;
    float targetX = 0;
    float targetY = 0;
    float targetZ = 0;
    private void Awake()
    {

        if (instance == null)
        {
            instance = this;
        }
    }

    private void Start()
    {
        anim = GetComponent<Animator>();
        charaterControl = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if(mxmAnimator==null) mxmAnimator = GetComponent<MotionManager>().mmAnimator;
        if (anim.GetCurrentAnimatorClipInfo(0).Length == 0) return;
        AutoJump();
        MatchTarget();
        ClimbStop();
        JumpDown();
        CCcontrol();
        JumpControl();
    }
    private void JumpControl()//跳跃控制
    {
        if (!isClimb)
        {
            if (!isJump)
            {
                JumpGroundDetect();

                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (jumpAimPoint.y - transform.position.y < maxJumpHeight)
                    {
                        
                        isJump = true;
                        matchOn = false;

                        mxmAnimator.PlayAnimationClipByClip(jumpAnim, jumpAnimStartOffset,0.2f);
                        
                    }
                }
            }
            if (mxmAnimator.Timeschedule() > 0.8)
            {
                if (isJump)
                {
                    anim.applyRootMotion = true;
                    isJump = false;
                    matchOn = true;
                }
            }

            if (isJump)
            {
                 var momentJump = mxmAnimator.Timeschedule();

                if (momentJump > riseStartMoment && momentJump <= fallStartMoment)
                {
                    jumpStartPoint = transform.position;
                    jumpMidPoint = new Vector3(0.5f * (jumpStartPoint.x + jumpAimPoint.x), jumpStartPoint.y + 2f, 0.5f * (jumpAimPoint.z + jumpStartPoint.z));
                    anim.applyRootMotion = false;
                    transform.position = Vector3.Lerp(transform.position, jumpMidPoint, 0.01f);

                }
                else if (momentJump > fallStartMoment)
                {
                    fallSpeed = Mathf.Lerp(0, 0.05f, (float)((momentJump - fallStartMoment) /(fallSpeedRiseEndMoment - fallStartMoment)));
                    transform.position = Vector3.Lerp(transform.position, jumpAimPoint, fallSpeed);
                }
            }
        }
    }


    private void JumpGroundDetect()
    {
        Ray ray = new Ray(transform.position + new Vector3(0, 10, 0) + transform.forward * 5f, -1 * transform.up);
        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo))
        {
            jumpAimPoint = hitInfo.point;
        }
        else
        {
            jumpAimPoint = transform.position;
        }
    }//跳跃落点检测
    //匹配翻越点
    private void MatchTarget()
    {
        if (!anim.IsInTransition(0))
        {
            //扑
            if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpMedium")
            {
                anim.MatchTarget(new Vector3(targetX, targetY, targetZ) + transform.TransformDirection(1, 0, -1) * 0.3f, Quaternion.identity, AvatarTarget.RightHand, new MatchTargetWeightMask(Vector3.one, 0), 0.30f, 0.36f, true);
            }
            //爬
            else if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpHigh")
            {
                anim.MatchTarget(new Vector3(targetX, targetY - 0.1f, targetZ) + transform.TransformDirection(1, 0, -1) * 0.3f, Quaternion.identity, AvatarTarget.RightHand, new MatchTargetWeightMask(Vector3.one, 0), 0.20f, 0.24f);
                //anim.MatchTarget(new Vector3(targetX, targetY + 0.1f, targetZ) - transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.LeftHand, new MatchTargetWeightMask(Vector3.one, 0), 0.24f, 0.50f);
                //anim.MatchTarget(new Vector3(targetx, 0, targetz)+transform.TransformDirection(Vector3.forward)*2, Quaternion.identity, AvatarTarget.RightFoot, new MatchTargetWeightMask(Vector3.one, 0), 0.90f, 1f);
            }
            //单手
            if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpShort")
            {
                anim.MatchTarget(new Vector3(targetX, targetY, targetZ) - transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.LeftHand, new MatchTargetWeightMask(Vector3.one, 0), 0.28f, 0.45f);
            }
        }
    }
    //角色控制器的开关
    private void CCcontrol()
    {
        AnimatorStateInfo animatorClipInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpMedium")
        {
            //anim.MatchTarget(new Vector3(targetX, targetY + 0.1f, targetZ) + transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.RightHand, new MatchTargetWeightMask(Vector3.one, 0), 0.30f, 0.36f, true);
            if (animatorClipInfo.normalizedTime > 0.30f && animatorClipInfo.normalizedTime < 0.55f)
                charaterControl.enabled = false;
            else
                charaterControl.enabled = true;
        }
        //爬
        else if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpHigh")
        {
            //anim.MatchTarget(new Vector3(targetX, targetY, targetZ) + transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.RightHand, new MatchTargetWeightMask(Vector3.one, 0), 0.20f, 0.24f);
            //anim.MatchTarget(new Vector3(targetX, targetY + 0.1f, targetZ) - transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.LeftHand, new MatchTargetWeightMask(Vector3.one, 0), 0.24f, 0.50f);
            //anim.MatchTarget(new Vector3(targetx, 0, targetz)+transform.TransformDirection(Vector3.forward)*2, Quaternion.identity, AvatarTarget.RightFoot, new MatchTargetWeightMask(Vector3.one, 0), 0.90f, 1f);
            if (animatorClipInfo.normalizedTime > 0.20f && animatorClipInfo.normalizedTime < 0.50f)
                charaterControl.enabled = false;
            else
                charaterControl.enabled = true;
        }
        //单手
        if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpShort")
        {
            //anim.MatchTarget(new Vector3(targetX, targetY + 0.1f, targetZ) - transform.TransformDirection(1, 0, 0) * 0.3f, Quaternion.identity, AvatarTarget.LeftHand, new MatchTargetWeightMask(Vector3.one, 0), 0.28f, 0.45f);
            if (animatorClipInfo.normalizedTime > 0.28f && animatorClipInfo.normalizedTime < 0.55f)
                charaterControl.enabled = false;
            else
                charaterControl.enabled = true;
        }
    }
    //浮空情况的处理
    private void ClimbStop()
    {
        
            if (!Physics.Raycast(transform.position + Vector3.up * 1, transform.TransformDirection(0, -1, 1), 1.6f))
            {
                if (!isJump)
                {
                    if (charaterControl.enabled)
                        charaterControl.SimpleMove(Vector3.zero);
                }


                AnimatorStateInfo animatorClipInfo = anim.GetCurrentAnimatorStateInfo(0);
                //扑基本动作结束时浮空，不播放后续动作
                if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpMedium" && animatorClipInfo.normalizedTime > 0.7f)
                {
                    //mxmAnimator.graph.Play();
                    mxmAnimator.Connect();
                    matchOn = true;
                    isClimb = false;
                    anim.CrossFade("01_IdleLoop", 0.1f, 0, 0.1f);
                }
                //print("is ground");

                //爬基本动作结束时浮空，直接播放跳下动作
                else if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpHigh" && animatorClipInfo.normalizedTime > 0.67f && animatorClipInfo.normalizedTime < 0.75f)
                {
                    anim.CrossFade("JumpHigh", 0.1f, 0, 0.85f);
                }

            }
            else
            {
                //爬基本动作结束时不浮空，阻止后续动作
                AnimatorStateInfo animatorClipInfo = anim.GetCurrentAnimatorStateInfo(0);
                if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpHigh" && animatorClipInfo.normalizedTime > 0.70f)
                {
                    mxmAnimator.Connect();
                    //mAnimator.graph.Play();
                    matchOn = true;
                    isClimb = false;
                    anim.CrossFade("01_IdleLoop", 0.1f, 0, 0.1f);
                }


            }

            if (anim.GetCurrentAnimatorStateInfo(0).normalizedTime > 1 && (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpMedium" || anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpShort" || anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpHigh" || anim.GetCurrentAnimatorClipInfo(0)[0].clip.name == "JumpDown"))
            {
                //mAnimator.graph.Play();
                mxmAnimator.Connect();
                matchOn = true;
                isClimb = false;
                anim.CrossFade("01_IdleLoop", 0.1f, 0, 0.1f);
            }
        
    }
    //自动攀爬动画的实现
    public void AutoJump()
    {
        if (!isJump)
        {
            //过渡时间没有长度
            if (anim.GetCurrentAnimatorClipInfo(0).Length == 0) return;
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 3f) && !isClimb/*&&!Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward),  1f)*/ )
            {
                targetX = hit.point.x;
                targetZ = hit.point.z;
                if (Vector3.Angle(transform.position - hit.point, hit.normal) > 30) return;
                if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), 0.2f))
                {
                    //攀爬物体距离角色的高度
                    float boundsy = hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.y * hit.transform.lossyScale.y / 2 + hit.transform.position.y - transform.position.y;

                    if (anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpMedium" && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpShort" && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpHigh")
                    {

                        if (boundsy > 0 && boundsy < 2)
                        {
                            float length = Mathf.Abs(hit.normal.x) == 1 ? hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.x * hit.transform.lossyScale.x / 2 : hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.z * hit.transform.lossyScale.z / 2;
                            //Vector3[] vertices = hit.transform.GetComponent<MeshFilter>().mesh.vertices;
                            /*foreach (Vector3 vertex in vertices)
                            {
                                Debug.Log(vertex);
                            }*/
                            //mAnimator.graph.Stop();
                            mxmAnimator.Disconnect();
                            matchOn = false;
                            isClimb = true;
                            if (length > 1)
                            {
                                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-hit.normal), 10 * Time.deltaTime);
                                //转向
                                transform.rotation = Quaternion.LookRotation(-hit.normal);
                                //物体高度
                                targetY = hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.y * hit.transform.lossyScale.y / 2 + hit.transform.position.y;
                                //过渡
                                anim.CrossFade("JumpMedium", 0f, 0, 0.25f);
                            }
                            else
                            {
                                //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-hit.normal), 10 * Time.deltaTime);
                                //转向
                                transform.rotation = Quaternion.LookRotation(-hit.normal);
                                //物体高度
                                targetY = hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.y * hit.transform.lossyScale.y / 2 + hit.transform.position.y;
                                anim.CrossFade("JumpShort", 0f, 0, 0.18f);
                            }

                        }
                        else if (boundsy > 2 && boundsy < 4)
                        {
                            mxmAnimator.Disconnect();
                            //mAnimator.graph.Stop();
                            matchOn = false;
                            isClimb = true;
                            transform.rotation = Quaternion.LookRotation(-hit.normal);
                            targetY = hit.transform.GetComponent<MeshFilter>().mesh.bounds.size.y * hit.transform.lossyScale.y / 2 + hit.transform.position.y;
                            anim.CrossFade("JumpHigh", 0f, 0, 0.1f);
                        }
                    }
                    else
                    {
                        anim.CrossFade("01_IdleLoop", 0.1f, 0, 0.1f);
                    }
                }

            }
        }
    }
    //跳下的处理
    private void JumpDown()
    {
        if (!Physics.Raycast(transform.position + Vector3.up * 1, transform.TransformDirection(0, -1, 1), 1.7f) && matchOn && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpHigh" && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpShort" && anim.GetCurrentAnimatorClipInfo(0)[0].clip.name != "JumpMedium" && mxmAnimator.Nowclipname() != "JumpDown")
        {
            matchOn = false;
            mxmAnimator.PlayAnimationClipByClip(jumpdownclip, 0);
        }
        if (mxmAnimator.Nowclipname() == "JumpDown" && Physics.Raycast(transform.position + Vector3.up * 1, transform.TransformDirection(0, -1, 1), 1.7f))
        {
            matchOn = true;
        }
    }
    private void OnDrawGizmos()//跳跃点 攀爬点打点
    {
        if (isJump)
        {
            Gizmos.DrawSphere(jumpAimPoint, 0.2f);

        }
        if (isClimb)
        {
            Gizmos.DrawSphere(new Vector3(targetX, targetY, targetZ), 0.1f);
        }

    }
}
