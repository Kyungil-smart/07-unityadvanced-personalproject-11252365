using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
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

    [Header("로 봇(Player)")]
    [Tooltip("로봇의 이동 속도")]
    [SerializeField] private float _moveSpeed;
    private Vector2 _moveInput; // 현재 입력된 방향을 저장할 변수 

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
        CalculateGravity(); // 중력 및 낙하 속도 계산
        MoveCharacter();    // 이동 방향 계산 및 실제 이동 적용
        
    }
    
    #endregion

    private void Init()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
        
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
    
    private void CalculateGravity()
    {
        // 바닥에 붙어있고 y속도가 음수라면,
        // 아주 약한 음수로 고정해 바닥에 계속 붙게 만듦.
        if (_characterController.isGrounded && _velocity.y < 0)
        {
            _velocity.y = GROUND_STICK_VELOCITY;
        }
        
        // 중력은 매 프레임 속도에 누적된다
        _velocity.y += _gravity * Time.deltaTime;
    }

    private void MoveCharacter()
    {
        // 수평 이동 벡터 계산
        Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
        Vector3 finalMovement= (moveDirection * _moveSpeed) + _velocity;
        
        _characterController.Move(finalMovement * Time.deltaTime);
    }
}
