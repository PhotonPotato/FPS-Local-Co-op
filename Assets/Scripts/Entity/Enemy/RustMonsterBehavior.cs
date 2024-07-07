using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class RustMonsterBehavior : MonoBehaviour
{
    private NavMeshAgent m_agent;
    public Health m_Health;

    public Transform m_target;
    public bool active = false;
    public bool dead = false;

    public float speedMult = 1;
    public AnimationCurve speedVsHealth;

    private float distanceToTarget;
    public float distanceToActivateEnemy;

    public ParticleSystem deathExplosion;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>(); //Initialize the john
        m_Health = GetComponent<Health>();
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

        if (!active && !dead && m_Health.lastHitBy != -1) //Activation by being hit
        {
            active = true;

            m_target = Generator.generator.activePlayers[m_Health.lastHitBy];
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

        //Animation?
        //Sound?

        deathExplosion?.Play();

        //Kill this object
        Destroy(this.gameObject, 1.5f);
    }
}
