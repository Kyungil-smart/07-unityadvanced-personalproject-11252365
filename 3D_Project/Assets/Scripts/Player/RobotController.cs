using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(RobotAnimationController))]
public class RobotController : MonoBehaviour
{
    #region Action Names
    private const string ACTION_MAP_NAME = "RobotActions";
    private const string ACTION_MOVE = "Move";
    private const string ACTION_JUMP = "Jump";
    private const string ACTION_DASH = "Dash";
    private const string ACTION_AIM_TOGGLE = "AimToggle";
    
    #endregion
    
    private const float GROUND_STICK_VELOCITY = -2f;    // 로봇을 바닥에 확실하게 밀착시킴
    
    private PlayerInput _playerInput;
    private CharacterController _characterController;
    private RobotAnimationController _animationController;
    private RobotCombatController _combatController;
    
    [Header("카메라 & 조준")]
    [SerializeField, Tooltip("메인카메라")] private Transform _cameraTransform;
    [SerializeField, Tooltip("Aim용 카메라")] private CinemachineCamera _aimCamera;
    [SerializeField, Tooltip("크로스헤어")] private GameObject _crosshairUI;
    public bool IsAiming {get; private set;}
    
    [Header("이동 세팅")]
    [SerializeField, Tooltip("이동 속도")] private float _moveSpeed;
    [SerializeField, Tooltip("중력 값")] private float _gravity;
    [SerializeField, Tooltip("회전 속도") ,Range(0f, 1000f)] private float _rotationSpeed;
    
    [Header("Dash 세팅")]
    [SerializeField, Tooltip("Dash 속도")] private float _dashSpeed = 15f;
    [SerializeField, Tooltip("Dash 지속 시간")] private float _dashDuration = 0.3f;
    [SerializeField, Tooltip("Dash 쿨타임")] private float _dashCooldown = 1f;
    private bool _isDashing;
    private float _dashTimer;
    private float _lastDashTime;
    private Vector3 _dashDirection;

    [Header("점프 세팅")] 
    [SerializeField, Tooltip("점프 높이")] private float _jumpHeight;
    [Tooltip("가변 점프 : 0에 가까울수록 더 급격히 낮아짐 / 1이면 가변 점프 효과 없음")]
    [SerializeField, Range(0f, 1f)] private float _shortJumpMultiplier;
    
    private Vector2 _moveInput; // 현재 입력된 방향을 저장할 변수 
    private Vector3 _velocity;  // 캐릭터의 수직 낙하 속도
    
    
    #region Unity Lifecycle
    private void Awake() => Init();
    private void OnEnable() => _playerInput.onActionTriggered += HandleInput;
    private void OnDisable() => _playerInput.onActionTriggered -= HandleInput;
    
    private void Update()
    {
        if (_combatController != null && _combatController.IsDead) return;
        
        bool isGrounded = _characterController.isGrounded;
        
        // 대시 타이머 처리
        if (_isDashing)
        {
            _dashTimer -= Time.deltaTime;
            if (_dashTimer <= 0f) _isDashing = false;
        }
        
        CalculateGravity(isGrounded); // 중력 및 낙하 속도 계산
        
        Vector3 moveDirection = _isDashing ? _dashDirection : GetCameraRelativeDirection();
        MoveCharacter(moveDirection);    // 이동 방향 계산 및 실제 이동 적용
        RotateCharacter(moveDirection);  // 회전 (바라보는 방향 변경)
        
        UpdateAnimation(isGrounded);  // 애니메이션 상태 전달
        
    }
    
    #endregion

    private void Init()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        _animationController = GetComponent<RobotAnimationController>();
        _combatController = GetComponent<RobotCombatController>();
        
        if (_cameraTransform == null)
        {
            Debug.LogError("RobotController: 인스펙터에서 메인 카메라를 꼭 넣어주세요.");
        }
        
        _gravity = -Mathf.Abs(_gravity);    // 중력이 무조건 음수가 되도록 강제 보정
        
        IsAiming = false;
        _isDashing = false;
        _dashTimer = 0f;
        _lastDashTime = -10f;
    }
    
    // NOTE : Input System의 3가지 작동 단계
    //  context.started   : 버튼을 "누른 찰나의 순간" (점프 발동, 단발성 공격 등)
    //  context.performed : 입력이 "유지되거나 갱신될 때" (조이스틱/WASD 연속 이동, 꾹 누르는 차지 샷 완료 등)
    //  context.canceled  : 버튼에서 "손을 뗀 순간" (가변 점프 도약 끊기, 조작 멈춤 등)
    private void HandleInput(InputAction.CallbackContext context)
    {
        if (context.action.actionMap.name != ACTION_MAP_NAME) return;
        
        // TODO : 액션이 늘어나면 함수로 분리
        switch (context.action.name)
        {
            case ACTION_MOVE:
                _moveInput = context.ReadValue<Vector2>();
                break;
            
            case ACTION_JUMP:
                if (context.started && _characterController.isGrounded) 
                {
                    // 물리 공식: V = sqrt(h * -2 * g)
                    _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                    _animationController.TriggerJump();
                }
                if (context.canceled && _velocity.y > 0)
                {
                    // 올라가던 속도를 갑자기 줄여서 낮게 점프하게 만듦
                    // 0에 가까울수록 더 급격히 낮아짐 / 1이면 가변 점프 효과 없음
                    _velocity.y *= _shortJumpMultiplier; 
                }
                break;
            case ACTION_DASH:
                if (context.started && !_isDashing && _characterController.isGrounded)
                    StartDash();
                break;
            case ACTION_AIM_TOGGLE:
                if (context.started) ToggleAim();
                break;
            
        }
    }

    private void CalculateGravity(bool isGrounded)
    {
        // 바닥에 붙어있고 y속도가 음수라면,
        // 아주 약한 음수로 고정해 바닥에 계속 붙게 만듦.
        if (isGrounded && _velocity.y < 0)
        {
            _velocity.y = GROUND_STICK_VELOCITY;
        }
        
        // 중력은 매 프레임 속도에 누적된다
        _velocity.y += _gravity * Time.deltaTime;
    }
    
    private Vector3 GetCameraRelativeDirection()
    {
        if (_moveInput == Vector2.zero) return Vector3.zero;
        if (_cameraTransform == null) return Vector3.zero;
        
        // 카메라가 바라보는 방향을 기준으로 이동 벡터를 계산
        Vector3 forward = _cameraTransform.forward;
        Vector3 right = _cameraTransform.right;
        
        // Y축은 무시하고 바닥에서만 평면적으로 움직이게 함
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
         Vector3 direction = (forward * _moveInput.y) + (right * _moveInput.x);
         
         // 대각선 이동 과속 방지
         return Vector3.ClampMagnitude(direction, 1f);
    }

    private void MoveCharacter(Vector3 moveDirection)
    {
        float speed = _isDashing ? _dashSpeed : _moveSpeed;
        Vector3 finalMove = (moveDirection * speed) + _velocity;
        _characterController.Move(finalMove * Time.deltaTime);
    }

    private void RotateCharacter(Vector3 moveDirection)
    {
        // 조준 중(IsAiming)일 때는 무조건 카메라가 바라보는 앞방향(forward)
        Vector3 lookDirection = IsAiming ? _cameraTransform.forward : moveDirection;
        lookDirection.y = 0f;
        
        if (lookDirection == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.RotateTowards
            (transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }
    
    private void StartDash()
    {
        if (Time.time < _lastDashTime + _dashCooldown) return;
        
        _lastDashTime = Time.time;
        
        _isDashing = true;
        _dashTimer = _dashDuration;
        
        Vector3 moveDirection = GetCameraRelativeDirection();
        _dashDirection = (moveDirection != Vector3.zero) ? moveDirection : transform.forward;
        
        _animationController.TriggerDash();
    }
    
    private void ToggleAim()
    {
        IsAiming = !IsAiming;
        if (_crosshairUI != null) _crosshairUI.SetActive(IsAiming);
        if (_aimCamera != null) _aimCamera.Priority = IsAiming ? 10 : 0;
    }

    private void UpdateAnimation(bool isGrounded)
    {
        float speed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
        
        // 현재 실제 속도를 최대 이동속도(_moveSpeed)로 나누어 0~1 사이의 비율로 만듬
        float normalizedSpeed = (_moveSpeed > 0f) ? Mathf.Clamp01(speed / _moveSpeed) : 0f;
        
        // 매 프레임마다 애니메이션 스크립트에게 인자값을 넘겨줌
        _animationController.UpdateLocomotion(normalizedSpeed, _moveInput, isGrounded, IsAiming);
    }
    
}
