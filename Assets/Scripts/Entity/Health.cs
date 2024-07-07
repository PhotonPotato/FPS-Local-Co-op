using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] private int health;
    [SerializeField] private int maxHealth;

    [Tooltip("This is -1 if its not attached to a player")]
    public int thisPlayerID { get; private set; } = -1;

    public bool invincible = false;

    public bool canDie = true;

    [SerializeField] private bool broadcastDamageMessage = false;
    [SerializeField] private bool broadcastDeathMessage = false;

    public int lastHitBy { get; private set; } = -1;

    public virtual bool DealDamage(int amount, int senderID = -1, DamageType damageType = DamageType.Melee)
    {
        if (invincible) return false;

        //Check if its self damage
        if (thisPlayerID != -1)
        {
            //If friendlyfire even enabled
            //if (!GameConstants.FriendlyFire) return false;

            if (senderID == thisPlayerID) return false;

            Debug.Log("hit");
        }

        health -= amount;

        if (broadcastDamageMessage) this.gameObject.BroadcastMessage("OnThisTakeDamage", damageType, SendMessageOptions.DontRequireReceiver);

        if (canDie && health <= 0)
        {
            //Initiate death.

            //Send a message to other scripts on THIS object
            if (broadcastDeathMessage) this.gameObject.BroadcastMessage("OnThisEntityDeath", null, SendMessageOptions.DontRequireReceiver);

            canDie = false;
        }

        //Store who hit this enemy
        lastHitBy = senderID;

        return true;
    }

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;

    public void SetPlayerID(int id) { thisPlayerID = id; }
}
