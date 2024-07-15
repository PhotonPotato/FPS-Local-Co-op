using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;
using NUnit.Framework;
using System.Collections.Generic;
using System;

public class RustMonsterBehavior : MonoBehaviour
{
    [Header("Refs")]
    private NavMeshAgent m_agent;
    public Health m_Health;

    public Transform m_target;
    public Transform m_childModel;

    public Animator m_animator;

    public ParticleSystem hitParticles;
    public ParticleSystem deathExplosion;

    [Header("State")]
    public bool active = false;
    public bool dead = false;

    [Header("Settings")]
    public bool pooledObj = false;
    //[NonSerialized] public EnemyPoolManager enemyPoolManager;

    [Space]

    public float speedMult = 1;
    public AnimationCurve speedVsHealth;

    [Space]

    public float distanceToInitiateAttack = 2f;

    public LayerMask AttackableLayers;

    public float AttackSphereOriginDist = 1.5f;
    public float AttackSphereRadius = 1.5f;
    public bool isDealingAttackDamage = false;
    public bool isAttacking = false;

    public int AttackDamage = 35;

    [Space]

    private float distanceToTarget;
    public float distanceToActivateEnemy;

    private List<GameObject> alreadyAttackedObjects;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>(); //Initialize the john
        m_Health = GetComponent<Health>();

        alreadyAttackedObjects = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        m_agent.speed = active ? speedVsHealth.Evaluate(m_Health.GetHealth() / (float)m_Health.GetMaxHealth()) * speedMult : 0; //Don't move if inactive

        if (active) //Make sure it should be moving
        {
            if (m_target == null)
            {
                m_target = FindClosestPlayer(ref distanceToTarget);
            }
            else
            {
                m_agent.SetDestination(m_target.position);

                //If it is close to its target position, then try to attack
                if (Vector3.Distance(m_target.position, transform.position) <= distanceToInitiateAttack)
                {
                    //Sound?
                    m_animator.SetTrigger("Attack");
                    isAttacking = true;
                }

                if (isAttacking) m_agent.speed = 0;
                
                if (isDealingAttackDamage) DetectPlayerWithinAttackRange();
                
            }
        }
        else
        {
            //Sit dormant until the player gets close (check every few frames)
            if (Time.frameCount % 40 == 0)
            {
                if (FindClosestPlayer(ref distanceToTarget) != null)
                {
                    if (distanceToTarget <= distanceToActivateEnemy)
                    {
                        active = true;
                    }
                }
            }
        }

        //Reset the zombies y value every once in a while
        if (!dead && Time.frameCount % 80 == 0)
        {
            m_childModel.localPosition = new Vector3(m_childModel.localPosition.x, 0, m_childModel.localPosition.z);
        }

        m_childModel.localRotation = new Quaternion(m_childModel.localRotation.x, (0 - m_childModel.localRotation.y) / 5, m_childModel.localRotation.z, m_childModel.localRotation.w);

        if (!active && !dead && m_Health.lastHitBy != -1) //Activation by being hit
        {
            active = true;

            m_target = Generator.generator.activePlayers[m_Health.lastHitBy];
        }

        m_animator.SetFloat("Speed", m_agent.velocity.magnitude / m_agent.speed);
    }

    public void DetectPlayerWithinAttackRange()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position + transform.forward * AttackSphereOriginDist, AttackSphereRadius, AttackableLayers, QueryTriggerInteraction.Ignore);

        //Make sure it even hit anything
        if (cols.Length > 0)
        {
            foreach (Collider col in cols)
            {
                if (col.gameObject.tag == "Player" && !alreadyAttackedObjects.Contains(col.gameObject))
                {
                    Debug.Log("Damage");

                    col.GetComponent<Health>()?.DealDamage(AttackDamage);

                    alreadyAttackedObjects.Add(col.gameObject);
                }
            }
        }
    }

    private Transform FindClosestPlayer(ref float distance)
    {
        if (Generator.generator.activePlayers.Count == 0) return null; //Don't even try if theres nothing there


        Transform closestPlayer = Generator.generator.activePlayers[0];
        float distanceToClosestPlayer = Vector3.Distance(transform.position, closestPlayer.position);

        for (int i = 1; i < Generator.generator.activePlayers.Count; i++)
        {
            Transform currentPlayer = Generator.generator.activePlayers[i];

            float currentDist = Vector3.Distance(transform.position, currentPlayer.position);

            if (currentDist < distanceToClosestPlayer)
            {
                closestPlayer = currentPlayer;
                distanceToClosestPlayer = currentDist;
            }
        }

        distance = distanceToClosestPlayer;

        return closestPlayer;
    }

    //Broadcasted by health component on death
    public void OnThisEntityDeath()
    {
        active = false;
        dead = true;

        //Sound?

        deathExplosion?.Play();

        m_animator.SetTrigger("Dead");

        GetComponent<CapsuleCollider>().enabled = false;
    }

    //Broadcasted by health component when taking damage
    public void OnThisTakeDamage()
    {
        //Sounds??

        hitParticles?.Play();
    }

    public void SetAttacking(bool state)
    {
        isDealingAttackDamage = state;

        if (!state)
        {
            //reset the alreaedy attacked list
            alreadyAttackedObjects.Clear();
            Debug.Log("clear");
        }
    }

    private void OnDrawGizmos()
    {
        if (isDealingAttackDamage) 
        {
            Gizmos.DrawWireSphere(transform.position + transform.forward * AttackSphereOriginDist, AttackSphereRadius);
        }
    }

    public void KillAndDestroy()
    {
        if (pooledObj)
        {
            this.gameObject.SetActive(false);

            //Reset all the animation states of the zombie
            m_animator.SetFloat("Speed", 0);
            m_animator.ResetTrigger("Attack");
            m_animator.ResetTrigger("Dead");

            m_childModel.localPosition = Vector3.zero;

            //Clean up ceratin variables so that if its pulled from the pol again, it will be good as new
            isAttacking = false;
            dead = false;

            m_Health.ResetToMaxHealth();
            m_Health.canDie = true;

            GetComponent<CapsuleCollider>().enabled = true;
        }
        else
        {
            //Kill this object
            Destroy(this.gameObject);
        }
    }
}
