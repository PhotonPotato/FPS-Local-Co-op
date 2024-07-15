using UnityEngine;

public class RustMonsterAnimationEvenHandler : MonoBehaviour
{
    private RustMonsterBehavior parentBehavior;

    private void Start()
    {
        parentBehavior = GetComponentInParent<RustMonsterBehavior>();
    }

    /// <summary>
    /// Called by attack animation event to tell the monster to disable attacking
    /// </summary>
    public void OnActivateDealingDamage()
    {
        parentBehavior.SetAttacking(true);
    } 
    
    /// <summary>
    /// Called by attack animation event to tell the monster to enable attacking
    /// </summary>
    public void OnDeactivateDealingDamage()
    {
        parentBehavior.SetAttacking(false);
    }

    public void OnEnemyFullDead()
    {
        parentBehavior.KillAndDestroy();
    }

    public void OnAttackComplete()
    {
        parentBehavior.m_animator.ResetTrigger("Attack");
        parentBehavior.isAttacking = false;
    }
}
