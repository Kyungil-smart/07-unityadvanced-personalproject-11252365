using UnityEngine;

[DisallowMultipleComponent]
public class RobotAnimationController : MonoBehaviour
{
    private Animator _animator;

    #region Animation Parameters Hashes
    private static readonly int ANIM_MOVE_SPEED = Animator.StringToHash("MoveSpeed");
    private static readonly int ANim_MOVE_X =  Animator.StringToHash("MoveX");
    private static readonly int ANim_MOVE_Y =  Animator.StringToHash("MoveY");
    private static readonly int ANIM_IS_GROUNDED = Animator.StringToHash("IsGrounded");
    private static readonly int ANIM_IS_AIMING = Animator.StringToHash("IsAiming");
    private static readonly int ANIM_JUMP_TRIGGER = Animator.StringToHash("JumpTrigger");
    private static readonly int ANIM_FIRE_TRIGGER = Animator.StringToHash("FireTrigger");
    private static readonly int ANIM_DASH_TRIGGER = Animator.StringToHash("DashTrigger");
    private static readonly int ANIM_AUTO_FIRE_TRIGGER = Animator.StringToHash("AutoFireTrigger");
    private static readonly int ANIM_RELOAD_TRIGGER = Animator.StringToHash("ReloadTrigger");
    private static readonly int ANIM_DIE_TRIGGER = Animator.StringToHash("DieTrigger");

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
    public void UpdateLocomotion(float moveSpeed, Vector2 moveInput, bool isGrounded, bool isAiming)
    {
        if (_animator == null) return;
        
        _animator.SetBool(ANIM_IS_GROUNDED, isGrounded);
        _animator.SetBool(ANIM_IS_AIMING, isAiming);

        if (isAiming)
        {
            // 조준 중일 때는 8방향 블렌드 트리 사용을 위한 전달 x, y
            _animator.SetFloat(ANim_MOVE_X, moveInput.x, _moveSpeedDampTime, Time.deltaTime);
            _animator.SetFloat(ANim_MOVE_Y, moveInput.y, _moveSpeedDampTime, Time.deltaTime);
        }
        else
        {
            _animator.SetFloat(ANIM_MOVE_SPEED, moveSpeed, _moveSpeedDampTime, Time.deltaTime);
        }
    }

    #region Animator Triggers
    public void TriggerJump() => _animator.SetTrigger(ANIM_JUMP_TRIGGER);
    public void TriggerDash() => _animator.SetTrigger(ANIM_DASH_TRIGGER);
    public void TriggerFire() => _animator.SetTrigger(ANIM_FIRE_TRIGGER);
    public void TriggerAutoFire() => _animator.SetTrigger(ANIM_AUTO_FIRE_TRIGGER);
    public void TriggerReload() => _animator.SetTrigger(ANIM_RELOAD_TRIGGER);
    public void TriggerDie() => _animator.SetTrigger(ANIM_DIE_TRIGGER);

    #endregion
}
