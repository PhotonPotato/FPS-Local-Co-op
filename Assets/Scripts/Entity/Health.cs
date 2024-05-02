using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] private int health;
    [SerializeField] private int maxHealth;

    public bool invincible = false;

    public bool canDie = true;
    public bool broadcastDeathMessage = false;

    public int lastHitBy { get; private set; } = -1;

    public bool DealDamage(int amount, int senderID = -1)
    {
        if (invincible) return false;

        health -= amount;

        if (canDie && health <= 0)
        {
            //Initiate death.

            //Send a message to other scripts on THIS object
            if (broadcastDeathMessage) this.gameObject.BroadcastMessage("OnThisEnemyDeath", null, SendMessageOptions.DontRequireReceiver);

            canDie = false;
        }

        //Store who hit this enemy
        lastHitBy = senderID;

        return true;
    }

    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
}
