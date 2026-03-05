using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalZone : MonoBehaviour
{
    private bool _triggerOnce = true;
    private bool _hasTriggered;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        
        if (other.GetComponentInParent<RobotController>() == null
            && other.GetComponentInParent<RobotCombatController>() == null)
        {return;}
        
        _hasTriggered = true;
        
        if (_triggerOnce) GetComponent<Collider>().enabled = false;
        
        GameManager.Instance.Clear();
    }
}
