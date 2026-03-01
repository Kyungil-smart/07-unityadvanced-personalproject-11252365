using UnityEngine;
using UnityEngine.InputSystem;

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
    
    #endregion
    
    private const float GROUND_STICK_VELOCITY = -2f;    // 로봇을 바닥에 확실하게 밀착시킴
    
    private PlayerInput _playerInput;
    private CharacterController _characterController;
    private RobotAnimationController _animationController;
    
    [Header("카메라 세팅")]
    [Tooltip("메인 카메라")]
    [SerializeField] private Transform _cameraTransform;
    
    [Header("로 봇(Player)")]
    [Tooltip("로봇의 이동 속도")]
    [SerializeField] private float _moveSpeed;
    private Vector2 _moveInput; // 현재 입력된 방향을 저장할 변수 

    [Tooltip("로봇의 회전 속도 (숫자가 클수록 빠르게 회전)")]
    [SerializeField, Range(0f, 1000f)] private float _rotationSpeed;
    
    [Tooltip("로봇의 중력 값")]
    [SerializeField] private float _gravity;
    private Vector3 _velocity;  // 캐릭터의 수직 낙하 속도
    
    [Tooltip("점프 높이")]
    [SerializeField] private float _jumpHeight;

    [Tooltip("가변 점프 : 0에 가까울수록 더 급격히 낮아짐 / 1이면 가변 점프 효과 없음")]
    [SerializeField, Range(0f, 1f)] private float _shortJumpMultiplier;
    
    
    #region Unity Lifecycle
    private void Awake()
    {
        Init();
    }

    private void OnEnable()
    {
        _playerInput.onActionTriggered += HandleInput;
    }

    private void OnDisable()
    {
        _playerInput.onActionTriggered -= HandleInput;
    }

    private void Update()
    {
        bool isGrounded = _characterController.isGrounded;
        CalculateGravity(isGrounded); // 중력 및 낙하 속도 계산
        
        Vector3 targetDirection = GetCameraRelativeDirection();
        MoveCharacter(targetDirection);    // 이동 방향 계산 및 실제 이동 적용
        RotateCharacter(targetDirection);  // 회전 (바라보는 방향 변경)
        
        UpdateAnimation(isGrounded);  // 애니메이션 상태 전달
        
    }
    
    #endregion

    private void Init()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        _animationController = GetComponent<RobotAnimationController>();
        
        if (_cameraTransform == null)
        {
            Debug.LogError("RobotController: 인스펙터에서 메인 카메라를 꼭 넣어주세요.");
        }
        
        _gravity = -Mathf.Abs(_gravity);    // 중력이 무조건 음수가 되도록 강제 보정
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

    private void MoveCharacter(Vector3 targetDirection)
    {
        Vector3 finalMovement= (targetDirection * _moveSpeed) + _velocity;
        _characterController.Move(finalMovement * Time.deltaTime);
    }

    private void RotateCharacter(Vector3 targetDirection)
    {
        if (targetDirection == Vector3.zero) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.RotateTowards
            (transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);


    }

    private void UpdateAnimation(bool isGrounded)
    {
        // 현재 캐릭터의 실제 속도
        Vector3 currentVelocity = _characterController.velocity;
        
        // 수평 속도 측정
        float horizontalSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        
        // 현재 실제 속도를 최대 이동속도(_moveSpeed)로 나누어 0~1 사이의 비율로 만듬
        float normalizedSpeed = (_moveSpeed > 0f) ? Mathf.Clamp01(horizontalSpeed / _moveSpeed) : 0f;
        
        // 매 프레임마다 애니메이션 스크립트에게 현재 속도와 바닥 상태를 넘겨줌
        _animationController.UpdateLocomotion(normalizedSpeed, isGrounded);
    }
    
    
}
