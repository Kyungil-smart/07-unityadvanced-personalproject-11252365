using UnityEngine;
using UnityEngine.Pool;

public class TurretEnemyController : MonoBehaviour
{

    [Header("오브젝트 연결")]
    [SerializeField, Tooltip("총알 기준점")] private Transform _firePoint;
    [SerializeField, Tooltip("Enemy의 총알")] private GameObject _enemyBulletPrefab;
    [SerializeField, Tooltip("타겟 대상")] private Transform _target;
    
    [Header("탐지 대상")] 
    [SerializeField, Tooltip("타겟 범위")] private float _detectRange = 15f;
    [SerializeField, Tooltip("체크 시, 벽이 존재하면 사격X")] private bool _useLineOfSight = false;
    [SerializeField, Tooltip("사격을 차단할 레이어 선택")] private LayerMask _blockLayers;
    
    [Header("발사 설정")]
    [SerializeField, Tooltip("연사 간격 값이 연사가 빠름")] private float _fireCooldown = 0.5f;
    [SerializeField, Tooltip("Y축 회전 On/Off")] private bool _rotateYawOnly = true;

    [Header("오브젝트 풀")]
    [SerializeField, Tooltip("미리 만들 총알의 개수")] private int _defaultPoolSize = 10;
    [SerializeField, Tooltip("풀이 보관할 최대 총알 개수")] private int _maxPoolSize = 30;

    [Header("디버그")] 
    [SerializeField, Tooltip("체크 시 콘솔에 로그남음")] private bool _enableDebugLog = true;
    
    private IObjectPool<Bullet> _bulletPool;
    private float _fireTimer;

    private float _detectRangeSqr; // 거리 계산 제곱값

    #region Unity Lifecycle
    private void Awake()
    {
        _detectRangeSqr = _detectRange * _detectRange;
        
        // 풀 초기화 : 생성, 대여, 반환, 파괴 시의 규칙 설정
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: CreateBullet,
            actionOnGet: OnGetBullet,
            actionOnRelease: OnReleaseBullet,
            actionOnDestroy: OnDestroyBullet,
            collectionCheck: false,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize);
    }

    private void Update()
    {
        // 상태 체크 : Playing 일 때만 작동 (일시정지/클리어 시 X)
        if (GameManager.Instance == null 
            || GameManager.Instance.CurrentState != GameState.Playing) return;
        
        if (_target == null || _firePoint == null) return;
        
        // 거리 감지
        Vector3 toTarget = _target.position - transform.position;
        float distanceSqr = toTarget.sqrMagnitude;
        
        if (distanceSqr > _detectRangeSqr) return;

        // 시야 체크 : 벽 뒤면 발사 X
        if (_useLineOfSight && !HasLineOfSight(toTarget)) return;
        
        // 조준 : 타겟으로 회전
        AimAtTarget(toTarget);
        
        // 발사 : 쿨타임 끝나면 총알을 발사 및 초기화
        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f)
        {
            Fire();
            _fireTimer = _fireCooldown;
        }
        
    }

    #endregion

    private void AimAtTarget(Vector3 toTarget)
    {
        if (_rotateYawOnly) toTarget.y = 0;
        
        // 타겟과 위치가 겹쳤을 때 문제 발생 방지
        if (toTarget.sqrMagnitude <= 0.0001f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(
            toTarget.normalized, Vector3.up);
        transform.rotation = targetRotation;
    }

    private void Fire()
    {
        _bulletPool.Get();
        
        if (_enableDebugLog) Debug.Log("[터렛] 발사");
    }

    private bool HasLineOfSight(Vector3 toTarget)
    {
        if (_blockLayers.value == 0) return false;
        
        Vector3 origin = _firePoint.position;
        Vector3 direction = toTarget.normalized;
        float distance = Mathf.Sqrt(toTarget.sqrMagnitude);
        
        // 차단 레이어가 먼저 맞으면 시야 없음
        if (Physics.Raycast(origin, direction, distance, _blockLayers,
                QueryTriggerInteraction.Ignore)) return false;

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        // 탐지 범위
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectRange);
        
        // 발사 방향
        if (_firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_firePoint.position, _firePoint.position + _firePoint.forward * 2f);
        }
        
        // 타겟 있으면 타겟 까지 선 표시
        if (_target != null)
        {
            Vector3 origin = (_firePoint != null) 
                ? _firePoint.position : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, _target.position);
            Gizmos.DrawWireSphere(_target.position, 0.25f);
        }
    }

    #region Object Pool Callbacks

    // 풀에 여분없을 때 생성
    private Bullet CreateBullet()
    {
        GameObject bulletObject = Instantiate(_enemyBulletPrefab);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        if (bullet == null)
            Debug.LogError("[TurretEnemyController] 프리팹에 불렛 을 확인하세요");
        
        return bullet;
    }
    
    // 풀에서 꺼낼 때 실행
    private void OnGetBullet(Bullet bullet)
    {
        bullet.SetPool(_bulletPool);

        bullet.transform.position = _firePoint.position;
        bullet.transform.rotation = _firePoint.rotation;
        bullet.gameObject.SetActive(true);
    }

    // 풀로 반환될 때 실행
    private void OnReleaseBullet(Bullet bullet) => bullet.gameObject.SetActive(false);
    
    // 풀의 최대 한도를 넘으면 완전히 파괴 
    private void OnDestroyBullet(Bullet bullet) => Destroy(bullet.gameObject);

    #endregion

}
