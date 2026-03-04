using System;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Bullet : MonoBehaviour
{
    [Header("총알 세팅")]
    [SerializeField, Min(0.1f)] private float _moveSpeed;
    [SerializeField, Min(0.1f)] private float _lifeTime;
    [SerializeField, Min(1)] private int _damage;
    
    [Header("목표 레이어")]
    [SerializeField, Tooltip("총알이 반응할 레이어")]
    private LayerMask _targetLayer;
    
    private Rigidbody _rigidbody;
    private IObjectPool<Bullet> _objectPool;
    private bool _isReleased;

    #region Unity Lifecycle
    private void Awake() => Init();

    public void SetPool(IObjectPool<Bullet> objectPool)
    {
        _objectPool = objectPool;
    }

    private void OnEnable()
    {
        _isReleased = false;
        
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.linearVelocity = transform.forward * _moveSpeed;
        
        Invoke(nameof(DestroySelf), _lifeTime);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DestroySelf));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isReleased) return;
        
        if ((_targetLayer.value & (1 << other.gameObject.layer)) == 0) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null) damageable.TakeDamage(_damage);

        DestroySelf();
    }
    
    #endregion

    private void Init() =>  _rigidbody = GetComponent<Rigidbody>();
    private void DestroySelf()
    {
        if (_isReleased) return;
        _isReleased = true;
        
        if (_objectPool != null) _objectPool.Release(this);
        else
        {
            Destroy(gameObject);
        }
        
    }
}
