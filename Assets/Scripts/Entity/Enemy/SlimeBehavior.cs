using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class SlimeBehavior : MonoBehaviour
{
    [Header("Refs")]
    private NavMeshAgent m_agent;
    public Health m_Health;
    public Transform m_target;
    public ParticleSystem deathExplosion;
    public GameObject obj;

    [Header("General Settings")]
    public AnimationCurve speedVsHealth;

    [Header("Trackers")]
    public bool active = false;
    public bool dead = false;

    private float distanceToTarget;
    private float distanceToNextWaypoint;
    public float distanceToActivateEnemy;


    [Header("Slime Settings")]
    public float bodyHeight = .5f;
    public float hopHeight = 2;
    public float maxHopDistance = 3;

    public float timeToHopOneUnit = .4f;
    public float timeBetweenHops = .3f;

    //Privates
    private float t = 0; //time (0 - 1) local to one hop
    float timeOfLastHop = Mathf.NegativeInfinity;
    private float timeToHop = 0;

    Vector3 thisHopOrigin;
    Vector3 thisHopDestination;


    [Header("Speed Settings")]
    public float speedMult = 1;

    public AnimationCurve speedOverJump;
    public AnimationCurve movementLerpRemap;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>(); //Initialize the john
        m_Health = GetComponent<Health>();

        //Snyc the tranform with the agent posistion (and vice-versa)
        m_agent.updatePosition = true;
    }


    // Update is called once per frame
    void Update() //KINDA JUST MASHED A BUNCH OF SHIT TOGETHER. FUTURE TY PLEASE FIX THIS ASAP (ITS RLY UGLY)
    {
        if (active) //Make sure it should be moving
        {
            if (m_target == null)
            {
                //If there isn't alread, just find the closest play to pathfind to.
                m_target = FindClosestPlayer(ref distanceToTarget);
                m_agent.SetDestination(m_target.position);
            }
            else if (Time.frameCount % 40 == 0) //Update destination every 40 frames
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

            //Activation by being hit
            if (!dead && m_Health.lastHitBy != -1) 
            {
                active = true;

                m_target = Generator.generator.activePlayers[m_Health.lastHitBy];
            }

            return;
        }

        if (dead) return; //Fuck it back out if u dead


        //If it has been long enough since the last hop + rest time, then set up another hop
        if (Time.time - timeOfLastHop > timeToHop + timeBetweenHops)
        {
            float distanceToWaypoint = Vector3.Distance(transform.position, m_agent.path.corners[1]);

            obj.transform.position = m_agent.path.corners[1];
            //Debug.Log($"distance {distanceToWaypoint}");
            
            //Cap the maximum hop distance
            if (distanceToWaypoint > maxHopDistance)
            {
                distanceToWaypoint = maxHopDistance;
            }

            distanceToNextWaypoint = distanceToWaypoint;

            timeToHop = distanceToNextWaypoint * timeToHopOneUnit; //Calculate the total time for this hop

            thisHopOrigin = transform.position;
            thisHopDestination = Vector3.MoveTowards(transform.position, m_agent.path.corners[1], distanceToWaypoint);

            timeOfLastHop = Time.time;

            Debug.Log("Hop");
        }
        else
        {
            //This tuns when we are mid hop.

            //t is set as a time value 0 - 1 local to the hop
            t = (Time.time - timeOfLastHop) / timeToHop;

            //Throughtout the following transform, t gets remapped individually for both the actual speed towards the player
            //and then also for the height of the slime over the course of a hop.

            transform.position = Vector3.Lerp(thisHopOrigin, thisHopDestination, movementLerpRemap.Evaluate(t)) //Basic lerp between positions (remap the flow of t to be less robotic)
                               + new Vector3(0, bodyHeight + Mathf.Sin(speedOverJump.Evaluate(t) * Mathf.PI) * hopHeight * distanceToNextWaypoint, 0); //Add body Height offset and y position based on a sin wave.
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

    public void OnThisEnemyDeath()
    {
        active = false;
        dead = true;

        //Animation?
        //Sound?

        //deathExplosion?.Play();

        //Kill this object
        Destroy(this.gameObject, 1.5f);
    }
}
