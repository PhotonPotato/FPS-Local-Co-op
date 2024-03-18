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

    public void Initialize(int damage, LayerMask layers, Vector3 direction)
    {
        this.damage = damage;

        hitLayers = layers;
        this.direction = direction;
        Destroy(gameObject, 3f); // Destroy the bullet after 3 seconds if it hasn't hit anything
    }

    private void Update()
    {
        //Check if theres something inbetween
        Vector3 StartOfFramePos = transform.position;

        //Calc in acceleration due to gravity
        direction.y -= gravityAcceleration * Time.deltaTime;

        transform.position += direction * speed * Time.deltaTime;

        if (Physics.Linecast(StartOfFramePos, transform.position, out RaycastHit hit, hitLayers))
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
                Instantiate(impactEffect, transform.position, transform.rotation);
            }

            Destroy(this.gameObject); // Destroy the bullet on impact
        }
    }
}