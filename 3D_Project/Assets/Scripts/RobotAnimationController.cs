using UnityEngine;

[DisallowMultipleComponent]
public class RobotAnimationController : MonoBehaviour
{
    private Animator _animator;

    #region Animation Parameters Hashes
    private static readonly int ANIM_MOVE_SPEED = Animator.StringToHash("MoveSpeed");
    private static readonly int ANIM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
    private static readonly int ANIM_JUMP_TRIGGER = Animator.StringToHash("JumpTrigger");

    #endregion

    [Header("애니메이션 세팅")] [Tooltip("걷기/뛰기 전환의 부드러움 정도 (0에 가까울수록 즉각적으로 바낌)")] 
    [SerializeField, Range(0f, 0.2f)] private float _moveSpeedDampTime = 0.03f;
    
    #region Unity Lifecycle
    private void Awake()
    {
        Init();
    }
    
    #endregion

    private void Init()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("Animator를 찾을 수 없습니다.");
            return;
        }
        
        // 스크립트가 이동을 통제하도록 루트 모션 강제 비활성화
        _animator.applyRootMotion = false;
        
    }
    
    // RobotController에서 이동 상태를 갱신할 때 사용하는 함수 
    public void UpdateLocomotion(float moveSpeed, bool isGrounded)
    {
        if (_animator == null) return;
        
        // 댐핑(Damping) 적용: 값이 0에서 1로 튀지 않고 지정된 시간에 걸쳐 부드럽게 바뀜
        _animator.SetFloat(ANIM_MOVE_SPEED, moveSpeed, _moveSpeedDampTime, Time.deltaTime);
        _animator.SetBool(ANIM_IS_GROUNDED, isGrounded);
    }

    public void TriggerJump()
    {
        if (_animator == null) return;
        _animator.SetTrigger(ANIM_JUMP_TRIGGER);
    }
}
