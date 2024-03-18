using UnityEngine;
using System;

public class Health : MonoBehaviour
{
    [SerializeField] private int health;
    [SerializeField] private int maxHealth;

    public bool invincible = false;

    public bool DealDamage(int amount)
    {
        if (invincible) return false;

        health -= amount;

        return true;
    }

    public int GetHealth() { return health; }
    public int GetMaxHealth() { return maxHealth; }
}
