using UnityEngine;
using UnityEngine.InputSystem;

public class RobotController : MonoBehaviour
{
    private PlayerInput _playerInput;
    private CharacterController _characterController;   // 캐릭터 움직일 컴포넌트

    [Header("로 봇")]
    [Tooltip("캐릭터의 이동 속도")]
    [SerializeField] private float _moveSpeed;
    private Vector2 _moveInput; // 현재 입력된 방향을 저장할 변수 

    [Tooltip("캐릭터의 중력 값")]
    [SerializeField] private float _gravity;
    private Vector3 _velocity;  // 캐릭터의 수직 낙하 속도
    
    [Tooltip("점프 높이")]
    [SerializeField] private float _jumpHeight; // 
    
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
        // // 바닥 판정 및 낙하 속도 초기화
        // if (_characterController.isGrounded && _velocity.y < 0) _velocity.y = -2f;
        //
        // // 수평 이동 WASD
        // Vector3 moveDirection = new Vector3(_moveInput.x, 0f, _moveInput.y);
        // _characterController.Move(moveDirection * _moveSpeed * Time.deltaTime);
        //
        // // 수직 이동(중력 사용)
        // _velocity.y += _gravity * Time.deltaTime;
        //
        // _characterController.Move(_velocity * Time.deltaTime);
        
        
    }

    #endregion

    private void Init()
    {
        _playerInput = GetComponent<PlayerInput>();
        _characterController = GetComponent<CharacterController>();
    }
    
    
    
    private void HandleInput(InputAction.CallbackContext context)
    {
        switch (context.action.name)
        {
            case "Move":
                _moveInput = context.ReadValue<Vector2>();
                break;
            
            case "Jump":
                // 버튼을 누른 순간(started) + 바닥에 있을 때만 점프
                if (context.started && _characterController.isGrounded) 
                {
                    // 물리 공식: V = sqrt(h * -2 * g)
                    _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                }
                if (context.canceled && _velocity.y > 0)
                {
                    // 올라가던 속도를 갑자기 줄여서 낮게 점프하게 만듦
                    // 0.5f는 예시이며, 취향에 따라 0으로 만들거나 더 줄일 수 있습니다.
                    _velocity.y *= 0.5f; 
                }
                break;
            // context.performed
        }
    }
}
