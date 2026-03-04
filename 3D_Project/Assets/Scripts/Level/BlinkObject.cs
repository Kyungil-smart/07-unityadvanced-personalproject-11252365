using System.Collections;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(Collider))]
public class BlinkObject : MonoBehaviour
{
    [Header("오브젝트 타이머 세팅")] 
    [SerializeField, Tooltip("게임 시작 후 대기 시간)"), Min(0f)] private float _startDelay;
    [SerializeField, Tooltip("Collider 활성화 시간"), Min(0.1f)] private float _colliderActiveTime;
    [SerializeField, Tooltip("Collider 비활성화 시간"), Min(0.1f)] private float _colliderInactiveTime;
    [SerializeField, Tooltip("Renderer 활성화 시간"), Min(0.1f)] private float _rendererActiveTime;
    [SerializeField, Tooltip("Renderer 비활성화 시간"), Min(0.1f)] private float _rendererInactiveTime;
    
    private MeshRenderer _renderer;
    private Collider _collider;

    
    private WaitForSeconds _startDelayWait;
    private WaitForSeconds _colliderActiveWait;
    private WaitForSeconds _colliderInactiveWait;
    private WaitForSeconds _rendererActiveWait;
    private WaitForSeconds _rendererInactiveWait;
    
    private void Awake() => Init();

    private void Start()
    {
        StartCoroutine(ColliderRoutine());
        StartCoroutine(RendererRoutine());
    }
    
    private void Init()
    {
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        
        if (_startDelay > 0f) _startDelayWait = new WaitForSeconds(_startDelay); 
        
        _colliderActiveWait = new WaitForSeconds(_colliderActiveTime);
        _colliderInactiveWait = new WaitForSeconds(_colliderInactiveTime);
        _rendererActiveWait = new WaitForSeconds(_rendererActiveTime);
        _rendererInactiveWait = new WaitForSeconds(_rendererInactiveTime);
    }

    private IEnumerator ColliderRoutine()
    {
        if (_startDelayWait != null) yield return _startDelayWait;
        
        while (true)
        {
            SetColliderState(true);
            yield return _colliderActiveWait;
            
            SetColliderState(false);
            yield return _colliderInactiveWait;
        }
    }
    
    private IEnumerator RendererRoutine()
    {
        if (_startDelayWait != null) yield return _startDelayWait;
        
        while (true)
        {
            SetRendererState(true);
            yield return _rendererActiveWait;
            
            SetRendererState(false);
            yield return _rendererInactiveWait; 
        }
    }
    
    private void SetColliderState(bool isActive) => _collider.enabled = isActive;
    private void SetRendererState(bool isActive) => _renderer.enabled = isActive;
}
