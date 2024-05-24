using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class SlimeBehavior : MonoBehaviour
{
    [Header("Refs")]
    private NavMeshAgent m_agent;
    public Health m_Health;
    public Transform m_target;
    public ParticleSystem m_deathExplosion;
    public GameObject obj;
    BoxCollider m_collider;

    [Header("General Settings")]
    public AnimationCurve speedVsHealth;
    public int damage = 20;

    [Header("Trackers")]
    public bool active = false;
    public bool dead = false;

    private float distanceToTarget;
    private float distanceToNextWaypoint;
    public float distanceToActivateEnemy;

    float timeOfLastAttack = Mathf.NegativeInfinity;


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

    public float cooldownBetweenAttacks = .3f;

    public Vector3 SqueezedSlimeScale;

    Vector3 thisHopOrigin;
    Vector3 thisHopDestination;

    [Header("Speed Settings")]
    public float speedMult = 1;

    public AnimationCurve jumpHeightLerpRemap;
    public AnimationCurve movementLerpRemap;

    public AnimationCurve SqueezeOverJump;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>(); //Initialize the john
        m_Health = GetComponent<Health>();

        //Snyc the tranform with the agent posistion (and vice-versa)
        m_agent.updatePosition = true;

        m_collider = GetComponent<BoxCollider>();
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

                //Unparent the object so it wont despawn with rooms
                transform.SetParent(null);
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

                //Unparent the object so it wont despawn with rooms
                transform.SetParent(null);
            }

            return;
        }

        if (dead) return; //Fuck it back out if u dead


        //If it has been long enough since the last hop + rest time, then set up another hop
        if (Time.time - timeOfLastHop > timeToHop + timeBetweenHops)
        {
            //make sure that there is a second waypoint before tryign to access it
            Vector3 nextWaypoint = m_agent.path.corners[m_agent.path.corners.Length == 1 ? 0 : 1];
            float distanceToWaypoint = Vector3.Distance(transform.position, nextWaypoint);

            //obj.transform.position = nextWaypoint;
            //Debug.Log($"distance {distanceToWaypoint}");
            
            //Cap the maximum hop distance
            if (distanceToWaypoint > maxHopDistance)
            {
                distanceToWaypoint = maxHopDistance;
            }

            distanceToNextWaypoint = distanceToWaypoint;

            timeToHop = distanceToNextWaypoint * timeToHopOneUnit; //Calculate the total time for this hop

            thisHopOrigin = transform.position;

            if (m_agent.path.corners.Length > 1)
            {
                thisHopDestination = Vector3.MoveTowards(transform.position, nextWaypoint, distanceToWaypoint);

                timeOfLastHop = Time.time;
                Debug.Log("Hop initiated");
            }
        }
        else
        {
            //This tuns when we are mid hop.

            //t is set as a time value 0 - 1 local to the hop
            t = (Time.time - timeOfLastHop) / timeToHop;

            //Throughtout the following transform, t gets remapped individually for both the actual speed towards the player
            //and then also for the height of the slime over the course of a hop.
            float currentJumpHeight = Mathf.Sin(jumpHeightLerpRemap.Evaluate(t) * Mathf.PI) * hopHeight * distanceToNextWaypoint;

            transform.position = Vector3.Lerp(thisHopOrigin, thisHopDestination, movementLerpRemap.Evaluate(t)) //Basic lerp between positions (remap the flow of t to be less robotic)
                               + new Vector3(0, bodyHeight + currentJumpHeight, 0); //Add body Height offset and y position based on a sin wave.

            //Scale the slime for a sort of squish effect throughout the jump
            transform.localScale = Vector3.Lerp(Vector3.one, SqueezedSlimeScale, SqueezeOverJump.Evaluate(t > .5 ? 1 - t : t));

            //Check if can attack
            if (Time.time - timeOfLastAttack >= cooldownBetweenAttacks)
            {
                //Check for collisions
                Collider[] colliders = Physics.OverlapSphere(transform.position + (transform.forward * 1), .4f);

                foreach (Collider col in colliders)
                {
                    if (col.gameObject.CompareTag("Player"))
                    {
                        //Damage
                        col.gameObject.GetComponentInParent<Health>()?.DealDamage(damage);

                        //Send the slime back
                        thisHopOrigin = m_agent.transform.position - new Vector3(0, bodyHeight + currentJumpHeight, 0);
                        thisHopDestination = transform.position - new Vector3(0, bodyHeight + currentJumpHeight, 0);
                        //Vector3.MoveTowards(thisHopDestination, thisHopOrigin, distanceToNextWaypoint / 2);

                        // timeOfLastHop = timeToHop / 2;

                        timeOfLastAttack = Time.time;

                        break; //Only damage one thing
                    }
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

    public void OnThisEnemyDeath()
    {
        active = false;
        dead = true;

        //Animation?
        //Sound?

        m_deathExplosion?.Play();

        //Kill this object
        Destroy(this.gameObject, 1.5f);
    }

    public void OnCollisionEnter(Collision collision)
    {
        
    }
   
    //public void OnCollisionEnter(Collision collision)
}
