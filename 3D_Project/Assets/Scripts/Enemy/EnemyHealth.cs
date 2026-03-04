using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [SerializeField, Tooltip("최대 체력")] private int _maxHealth = 10;
    private int _currentHealth;

    private void Awake() => Init();
    
    private void Init()
    {
        _currentHealth = _maxHealth;
    }
    
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        
        Debug.Log($"<color=yellow> 적 피격!</color> 남은 체력 : {_currentHealth}");

        if (_currentHealth <= 0)
        {
            Die();
        }
            
    }

    private void Die()
    {
        Debug.Log("<color=red>적 처치됨!</color>");
        // TODO: 나중에 사망 애니메이션, 파티클 효과 등을 추가
        Destroy(gameObject);
    }
}
