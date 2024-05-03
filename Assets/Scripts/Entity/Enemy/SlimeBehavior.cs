using UnityEngine;
using UnityEngine.AI;

public class SlimeBehavior : MonoBehaviour
{
    private NavMeshAgent m_agent;
    public Health m_Health;

    public Transform m_target;
    public bool active = false;
    public bool dead = false;

    public float speedMult = 1;
    public AnimationCurve speedVsHealth;

    public AnimationCurve speedOverJump;
    private float t = 0; //time 
    float timeOfLastHop = Mathf.NegativeInfinity;
    public float timeToHopOneUnit = .4f;
    public float timeToHop = 0;
    public float maxHopDistance = 3;

    Vector3 lastHopOrigin;
    Vector3 nextHopDestination;

    private float distanceToTarget;
    public float distanceToActivateEnemy;

    public ParticleSystem deathExplosion;
    public GameObject obj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>(); //Initialize the john
        m_Health = GetComponent<Health>();
    }


    // Update is called once per frame
    void Update()
    {
        //Update the pathfinding every 40 frames
        if(m_target != null && Time.frameCount % 40 == 0) m_agent.SetDestination(m_target.position);

        if (Time.time - timeOfLastHop > timeToHop)
        {
            float distanceToWaypoint = Vector3.Distance(transform.position, m_agent.path.corners[1]);

            obj.transform.position = m_agent.path.corners[1];
            //Debug.Log($"distance {distanceToWaypoint}");
            
            //Cap the maximum hop distance
            if (distanceToWaypoint > maxHopDistance)
            {
                distanceToWaypoint = maxHopDistance;
            }

            timeToHop = distanceToWaypoint * timeToHopOneUnit; //Calculate the total time for this hop

            lastHopOrigin = transform.position;
            nextHopDestination = Vector3.MoveTowards(transform.position, m_agent.path.corners[1], distanceToWaypoint);

            timeOfLastHop = Time.time;

            Debug.Log("Hop");
        }
        else
        {
            //Then just hop
            t = (Time.time - timeOfLastHop) / timeToHop;

            t *= speedOverJump.Evaluate(t); //remap to smoother curve

            //Derivative of sin is -cos(t)
            m_agent.velocity += new Vector3(0, Mathf.Cos(t), 0);

            Debug.Log($"velocity {m_agent.velocity}");

            transform.position = Vector3.Lerp(lastHopOrigin, nextHopDestination, t);
        }
    }
}
