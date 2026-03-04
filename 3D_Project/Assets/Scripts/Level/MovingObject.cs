using System.Collections;
using UnityEngine;

public class MovingObject : MonoBehaviour
{
    [Header("이동 세팅")]
    [SerializeField, Tooltip("시작 위치 기준 왕복")] private Vector3 _moveOffset;
    [SerializeField, Tooltip("이동 속도"), Min(0f)] private float _moveSpeed;
    [SerializeField, Tooltip("도착 후 대기 시간"), Min(0f)] private float _waitTime;
    
    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private WaitForSeconds _wait;
    
    private void Awake() => Init();

    private void Start()
    {
        _endPosition = _startPosition + _moveOffset;
        StartCoroutine(MoveRoutine());
    }

    private void Init()
    {
        _startPosition = transform.position;
        _wait = new WaitForSeconds(_waitTime);
    }

    private IEnumerator MoveRoutine()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, _endPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position,
                    _endPosition, Time.deltaTime * _moveSpeed);
                yield return null;
            }
            
            transform.position = _endPosition;
            
            Vector3 temp = _endPosition;
            _endPosition = _startPosition;
            _startPosition = temp;
            
            yield return _wait;
        }
    }
    
}
