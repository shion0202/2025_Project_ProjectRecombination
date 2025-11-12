using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 기믹 오브젝트 등 단순한 애니메이션을 재생시키는 스크립트
// AnimCheck Animator Controller와 함께 사용
public class AnimCheck : MonoBehaviour
{
    [Header("Animator 컴포넌트와 동일한 오브젝트 또는 부모 오브젝트에 부착하세요.")]
    [SerializeField, Tooltip("애니메이션을 재생할 오브젝트에 부착된 애니메이터 컴포넌트 (자동 설정)")] private Animator _animator;

    [Header("Animations")]
    [SerializeField, Tooltip("재생하고자 하는 애니메이션 클립")] private AnimationClip animationClip;
    [SerializeField, Range(0.0f, 5.0f), Tooltip("애니메이션 재생 속도")] private float animSpeed = 1.0f;
    [SerializeField, Tooltip("애니메이션 재생 여부 (테스트 용도로 사용 가능)")] private bool isPlay = false;
    private AnimatorOverrideController _overrideController;

    [Header("Visual")]
    [SerializeField, Tooltip("애니메이션 재생 전 모습을 나타내는 오브젝트")] private GameObject defaultObject;
    [SerializeField, Tooltip("애니메이션 재생 후 모습을 나타내는 오브젝트")] private GameObject animObject;

    private void Awake()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
        }

        _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
        _animator.runtimeAnimatorController = _overrideController;
    }

    private void Start()
    {
        //Default로 사용 중인 애니메이션 이름이 변경될 경우 수정할 것
        _overrideController["Take 001"] = animationClip;
        _animator.SetFloat("animSpeed", animSpeed);
    }

    // 애니메이션 테스트를 위한 Update문
#if UNITY_EDITOR
    private void Update()
    {
        _overrideController["Take 001"] = animationClip;
        _animator.SetFloat("animSpeed", animSpeed);
        _animator.SetBool("isPlay", isPlay);

        if (defaultObject != null && animObject != null)
        {
            defaultObject.SetActive(!isPlay);
            animObject.SetActive(isPlay);
        }
    }
#endif

    public void Play()
    {
        isPlay = true;
        _animator.SetBool("isPlay", isPlay);

        if (defaultObject != null && animObject != null)
        {
            defaultObject.SetActive(!isPlay);
            animObject.SetActive(isPlay);
        }
    }

    public override string ToString()
    {
        string objectName = gameObject.name;
        string clipName = animationClip != null ? animationClip.name : "None";
        string defaultObjectState = defaultObject != null ? defaultObject.activeSelf.ToString() : "null";
        string animObjectState = animObject != null ? animObject.activeSelf.ToString() : "null";

        string log = $"[{objectName} ({GetType().Name})] Clip: {clipName}, Speed: {animSpeed:F2}, isPlay: {isPlay}, DefaultObjActive: {defaultObjectState}, AnimObjActive: {animObjectState}";
        return log;
    }
}
