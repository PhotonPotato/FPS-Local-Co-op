using System;
using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;

    private DamageType damageType;

    public bool timedDetonation = false;
    public float timeToDetonate = 4f;
    private float timeOfRelease = float.NegativeInfinity;
    private bool released = false;

    public bool usingRigidBody = false;

    public float gravityAcceleration = 0f;
    
    public float explosionRadius = 1f;

    public ParticleSystem impactEffect;

    private LayerMask hitLayers;
    private Vector3 direction;
    private Vector3 spawnPos;

    private float timeOfSpawn;

    private int senderID = -1; //Player index of sender

    public int bulletImpactPoolID = 2;

    Rigidbody rb;

    public void Initialize(int damage, LayerMask layers, Vector3 direction, int senderID = -1, DamageType damageType = DamageType.RegularBullet)
    {
        this.damage = damage;
        
        hitLayers = layers;
        this.direction = direction;
        timeOfSpawn = Time.time; // Destroy the bullet after 3 seconds if it hasn't hit anything

        spawnPos = transform.position;

        this.senderID = senderID; //Stores the player index of the personwho fired
        this.damageType = damageType;

        timeOfRelease = Time.time;

        released = true;

        if (usingRigidBody)
        {
            rb = GetComponent<Rigidbody>();

            rb.AddForce(direction * speed + Vector3.up * 100);
        }
    }

    private void Update()
    {
        //Check for destroy bullet
        if (Time.time - timeOfSpawn >= 3f && !timedDetonation) gameObject.SetActive(false);

        //Check if theres something inbetween
        Vector3 StartOfFramePos = transform.position;

        if (usingRigidBody)
        {
            //Use the rigidbody's velocity system
            //rb.ad
        }
        else
        {
            //Calc in acceleration due to gravity
            direction.y -= gravityAcceleration * Time.deltaTime;

            transform.position += direction * speed * Time.deltaTime;
        }

        if (timedDetonation) //Keep track of a detonation timer
        {
            if (released && Time.time - timeOfRelease >= timeToDetonate) //Then detonate the damn grenade
            {
                DealExplosionDamageInSphereFromPoint(transform.position, explosionRadius, damage);

                //Get the explosion effects from the pool
                GameObject obj = BulletObjectPoolManager.SharedInstance.GetPooledObject(bulletImpactPoolID);

                if (obj != null)
                {
                    obj.SetActive(true);
                    obj.transform.position = transform.position;
                    obj.GetComponent<ParticleSystem>().Play();
                }

                //Put this object back into the pool
                gameObject.SetActive(false);

                released = false;
            }
        }
        else //Run regular trigger detection
        {
            if (Physics.Linecast(StartOfFramePos, transform.position, out RaycastHit hit, hitLayers, QueryTriggerInteraction.Ignore))
            {
                OnTriggerEnter(hit.collider);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Only run the following if its not a grenade
        if (timedDetonation) return;


        // Check if the bullet has hit an object on the hitLayers
        if (hitLayers == (hitLayers | (1 << other.gameObject.layer)))
        {
            //If explosice, now look in an explosion radius
            if (damageType == DamageType.Explosive)
            {
                DealExplosionDamageInSphereFromPoint(Vector3.MoveTowards(transform.position, spawnPos, .3f), explosionRadius, damage);
            }
            else if (damageType == DamageType.RegularBullet)
            {
                //Try to find if the object is damagable (remember the health must be in the same object as the collider, not a parent)
                if (other.TryGetComponent<Health>(out Health health))
                {
                    health.DealDamage(damage, senderID, damageType);
                }
            }

            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject obj = BulletObjectPoolManager.SharedInstance.GetPooledObject(bulletImpactPoolID);

                if (obj != null)
                {
                    obj.SetActive(true);
                    obj.transform.position = Vector3.MoveTowards(transform.position, spawnPos, .3f);
                    obj.GetComponent<ParticleSystem>().Play();
                }
            }

            gameObject.SetActive(false); // Destroy the bullet on impact (put it back into the pool)
        }
    }

    private void DealExplosionDamageInSphereFromPoint(Vector3 origin, float radius, int damage)
    {
        //Sphere cast to find colliders in the explosion raidius with the orgiin being the position of the particle effects.
        Collider[] cols = Physics.OverlapSphere(origin, radius, hitLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider col in cols)
        {
            //Try to find if the object is damagable (remember the health must be in the same object as the collider, not a parent)
            if (col.TryGetComponent<Health>(out Health health))
            {
                health.DealDamage(damage, senderID, damageType);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (released)
        {
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}

public enum DamageType
{
    Melee,
    RegularBullet,
    Explosive,
    Arrow
}