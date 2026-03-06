using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Pool;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(RobotAnimationController))]
public class RobotCombatController : MonoBehaviour, IDamageable
{
    [Serializable]
    private class AimRaycastConfig
    {
        [Tooltip("화면 중앙 기준점을 제공할 메인 카메라")] public Camera AimCamera;
        [Tooltip("사격 판정에 포함될 물리 대상들")] public LayerMask HitLayerMask = ~0;
        [Tooltip("카메라가 목표물을 탐지할 수잇는 최대 조준 사거리")] public float MaxDistance = 100f;
    }

    [Serializable]
    private class AimDebugConfig
    {
        [Tooltip("Scene 뷰에서 조준 관련 디버그 선과 타격점을 표시할지 여부")] public bool ShowGizmos = true;
        [Tooltip("체크 시, 플레이어가 실제 조준 중일 때만 기즈모를 그립니다.")] public bool ShowOnlyWhileAiming = true;
        [Tooltip("히트 지점 구 크기")] public float HitPointRadius = 0.2f;
        [Tooltip("총구가 바라보는 방향 길이")] public float ForwardLineLength = 3f;
    }
    
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
    
    [Header("전투 세팅")]
    //[SerializeField] private GameObject _healthBar;
    [SerializeField, Tooltip("연사 쿨타임 (초)")] private float _autoFireCooldown = 0.5f;
    [SerializeField, Tooltip("발사할 총알 프리팹")] private GameObject _bulletPrefab;
    [SerializeField, Tooltip("총구 위치")] private Transform _firePoint;
    private bool _isAutoFire;
    private float _autoFireTimer;
    
    private IObjectPool<Bullet> _bulletPool;
    private Vector3 _pendingBulletPosition;
    private Quaternion _pendingBulletRotation;
    
    [Header("Aim Raycast (Runtime)")]
    [SerializeField] private AimRaycastConfig _aimRaycastConfig = new AimRaycastConfig();

    [Header("Aim Debug (Optional)")]
    [SerializeField] private AimDebugConfig _aimDebugConfig = new AimDebugConfig();

    
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
        
        _pendingBulletPosition = _firePoint.position;
        _pendingBulletRotation = _firePoint.rotation;

        if (_robotController != null && _robotController.IsAiming)
        {
            if (TryGetAimPoint(out Vector3 aimPoint))
            {
                Vector3 shootDirection = aimPoint - _firePoint.position;

                if (shootDirection.sqrMagnitude > 0.0001f)
                    _pendingBulletRotation = Quaternion.LookRotation(shootDirection.normalized, Vector3.up);
            }
        }
        
        _bulletPool.Get();
    }

    private bool TryGetAimPoint(out Vector3 aimPoint)
    {
        aimPoint = default;
        
        if (_aimRaycastConfig.AimCamera == null) return false;
        
        Ray ray = _aimRaycastConfig.AimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        bool didHit = Physics.Raycast(
            ray,
            out RaycastHit hit,
            _aimRaycastConfig.MaxDistance,
            _aimRaycastConfig.HitLayerMask,
            QueryTriggerInteraction.Ignore);
        
        aimPoint = didHit ? hit.point : ray.origin + ray.direction * _aimRaycastConfig.MaxDistance;
        return true;
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
        bullet.transform.SetPositionAndRotation(_pendingBulletPosition, _pendingBulletRotation);
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
    
    private void OnDrawGizmosSelected()
    {
        if (_aimDebugConfig == null || !_aimDebugConfig.ShowGizmos) return;
        if (_aimRaycastConfig == null || _aimRaycastConfig.AimCamera == null) return;

        // "조준 중일 때만" 옵션은 플레이 중일 때만 정확히 판단 가능
        if (_aimDebugConfig.ShowOnlyWhileAiming && Application.isPlaying)
        {
            if (_robotController == null || !_robotController.IsAiming) return;
        }

        Ray cameraRay = _aimRaycastConfig.AimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        bool didHit = Physics.Raycast(
            cameraRay,
            out RaycastHit hit,
            _aimRaycastConfig.MaxDistance,
            _aimRaycastConfig.HitLayerMask,
            QueryTriggerInteraction.Ignore);

        Vector3 rayEndPoint = didHit
            ? hit.point
            : cameraRay.origin + cameraRay.direction * _aimRaycastConfig.MaxDistance;

        // 카메라 Ray 노란색
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(cameraRay.origin, rayEndPoint);

        // 히트 지점 초록색
        if (didHit)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(hit.point, _aimDebugConfig.HitPointRadius);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.3f);
        }
        // 총구 앞 하늘색
        if (_firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                _firePoint.position,
                _firePoint.position + _firePoint.forward * _aimDebugConfig.ForwardLineLength);
        }
    }
}
