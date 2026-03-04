using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(RobotAnimationController))]
public class RobotCombatController : MonoBehaviour, IDamageable
{
    #region Action Names
    private const string ACTION_MAP_NAME = "RobotActions";
    private const string ACTION_FIRE = "Fire";
    private const string ACTION_AUTO_FIRE = "AutoFire";
    private const string ACTION_RELOAD = "Reload";
    
    #endregion
    
    private PlayerInput _playerInput;
    private RobotAnimationController _robotAnimationController;
    private RobotController _robotController;
    
    // TODO: 나중에 PlayerHeath.cs 로 분리
    [Header("상태 & 체력")]
    [SerializeField] private int _maxHealth = 10;
    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    
    // 
    [Header("전투 세팅")]
    //[SerializeField] private GameObject _healthBar;
    [SerializeField, Tooltip("연사 쿨타임 (초)")] private float _autoFireCooldown = 0.5f;
    [SerializeField, Tooltip("발사할 총알 프리팹")] private GameObject _bulletPrefab;
    [SerializeField, Tooltip("총구 위치")] private Transform _firePoint;
    private bool _isAutoFire;
    private float _autoFireTimer;
    
    private IObjectPool<Bullet> _bulletPool;
    
    #region Unity Lifecycle
    private void Awake() => Init();
    private void OnEnable() => _playerInput.onActionTriggered += HandleInput;
    private void OnDisable() => _playerInput.onActionTriggered -= HandleInput;

    private void Update()
    {
        if (IsDead) return;

        HandleAutoFire();
    }
    
    #endregion

    private void Init()
    {
        _playerInput = GetComponent<PlayerInput>();
        _robotAnimationController = GetComponent<RobotAnimationController>();
        _robotController = GetComponent<RobotController>();
        
        CurrentHealth = _maxHealth;
        IsDead = false;
        _isAutoFire = false;
        
        // 오브젝트 풀
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: false,
            defaultCapacity:  20,
            maxSize: 40);
    }
    
    // NOTE : Input System의 3가지 작동 단계
    //  context.started   : 버튼을 "누른 찰나의 순간" (점프 발동, 단발성 공격 등)
    //  context.performed : 입력이 "유지되거나 갱신될 때" (조이스틱/WASD 연속 이동, 꾹 누르는 차지 샷 완료 등)
    //  context.canceled  : 버튼에서 "손을 뗀 순간" (가변 점프 도약 끊기, 조작 멈춤 등)
    private void HandleInput(InputAction.CallbackContext context)
    {
        if (context.action.actionMap.name != ACTION_MAP_NAME || IsDead) return;

        switch (context.action.name)
        {
            case ACTION_FIRE:
                if (context.started)
                {
                    Shoot();
                }
                    
                break;
            case ACTION_AUTO_FIRE:
                if (context.started) _isAutoFire = true;
                if (context.canceled) _isAutoFire = false;
                break;
            case ACTION_RELOAD:
                if (context.started) _robotAnimationController.TriggerReload();
                break;
                
        }
    }

    private void Shoot()
    {
        if (_bulletPool == null || _firePoint == null) return;
        
        _robotAnimationController.TriggerFire();
        _bulletPool.Get();


    }
    
    private void HandleAutoFire()
    {
        if (!_isAutoFire) return;
        
        _autoFireTimer -= Time.deltaTime;
        if (_autoFireTimer <= 0f)
        {
            Shoot();
            _autoFireTimer = _autoFireCooldown;
        }
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;
        
        CurrentHealth -= damage;
        Debug.Log($"플레이어 피격1회 현재 체력 : {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        _isAutoFire = false;
        _robotAnimationController.TriggerDie();
        Debug.Log("플레이어 사망...");
    }

    #region Object Pool Methods
    private Bullet CreateBullet()
    {
        GameObject obj = Instantiate(_bulletPrefab);
        return obj.GetComponent<Bullet>();
    }

    private void OnGetBullet(Bullet bullet)
    {
        bullet.SetPool(_bulletPool);
        
        bullet.transform.position = _firePoint.position;
        bullet.transform.rotation = _firePoint.rotation;
        bullet.gameObject.SetActive(true);
    }

    private void OnReleaseBullet(Bullet bullet)
    {
        bullet.gameObject.SetActive(false);
    }

    private void OnDestroyBullet(Bullet bullet)
    {
        Destroy(bullet.gameObject);
    }
    
    #endregion
    
    
}
