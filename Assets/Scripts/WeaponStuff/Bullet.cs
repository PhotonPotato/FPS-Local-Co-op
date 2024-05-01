using System;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;

    public float gravityAcceleration = 0f;

    public ParticleSystem impactEffect;

    private LayerMask hitLayers;
    private Vector3 direction;
    private Vector3 spawnPos;

    private float timeOfSpawn;

    public void Initialize(int damage, LayerMask layers, Vector3 direction)
    {
        this.damage = damage;

        hitLayers = layers;
        this.direction = direction;
        timeOfSpawn = Time.time; // Destroy the bullet after 3 seconds if it hasn't hit anything

        spawnPos = transform.position;
    }

    private void Update()
    {
        //Check for destroy bullet
        if (Time.time - timeOfSpawn >= 3f) gameObject.SetActive(false);

        //Check if theres something inbetween
        Vector3 StartOfFramePos = transform.position;

        //Calc in acceleration due to gravity
        direction.y -= gravityAcceleration * Time.deltaTime;

        transform.position += direction * speed * Time.deltaTime;

        if (Physics.Linecast(StartOfFramePos, transform.position, out RaycastHit hit, hitLayers, QueryTriggerInteraction.Ignore))
        {
            OnTriggerEnter(hit.collider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the bullet has hit an object on the hitLayers
        if (hitLayers == (hitLayers | (1 << other.gameObject.layer)))
        {
            //Try to find if the object is damagable
            if (other.TryGetComponent<Health>(out Health health))
            {
                health.DealDamage(damage);
            }

            //Health health = other.GetComponent<Health>();
            //if (health != null)
            //{
                //health.TakeDamage(damage);
            //}

            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject obj = BulletObjectPoolManager.SharedInstance.GetPooledObject(2);

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

    private void AltTriggerEnter(RaycastHit other)
    {
        if (hitLayers == (hitLayers | (1 << other.collider.gameObject.layer)))
        {
            //Try to find if the object is damagable
            if (other.collider.TryGetComponent(out Health health))
            {
                health.DealDamage(damage);
            }

            //Health health = other.GetComponent<Health>();
            //if (health != null)
            //{
            //health.TakeDamage(damage);
            //}

            // Spawn impact effect
            if (impactEffect != null)
            {
                GameObject a = Instantiate(impactEffect.gameObject, transform.position, Quaternion.identity).gameObject;
                Debug.Log("Normal " + other.normal);
                a.GetComponent<ParticleSystem>().Play();
            }

            gameObject.SetActive(false); // Destroy the bullet on impact (put it back into the pool)
        }

    }
}