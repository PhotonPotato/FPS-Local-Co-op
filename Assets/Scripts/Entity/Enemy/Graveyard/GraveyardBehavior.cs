using UnityEngine;
using UnityEngine.AI;

public class GraveyardBehavior : MonoBehaviour
{
    public Transform SpawnPoint;

    public int EnemyPoolID = 1;

    public float maxTimeBetweenSpawns = 4;
    public float minTimeBetweenSpawns = 3;
    private float timeOfNextSpawn = 3;

    [Tooltip("Zombies will be agro'd at the player immeiately after spawning")]
    public bool agroOffSpawn = true;

    private void Start()
    {
        transform.SetParent(null);

        if (SpawnPoint == null) SpawnPoint = transform;
    }

    private void Update()
    {
        if (Time.time >= timeOfNextSpawn)
        {
            SpawnNewGraveyardZombie();

            //Reset the timers
            timeOfNextSpawn = Time.time + Random.Range(minTimeBetweenSpawns, maxTimeBetweenSpawns);
        }
    }

    private void SpawnNewGraveyardZombie()
    {
        GameObject newZombieObj;

        if (EnemyPoolManager.SharedInstance.GetPooledObject(EnemyPoolID, out newZombieObj))
        {
            newZombieObj.SetActive(false);

            //Move the enemy
            newZombieObj.transform.position = SpawnPoint.position + new Vector3(Random.Range(-4, 4), 0, Random.Range(-4, 4));//SetPositionAndRotation(SpawnPoint.position + new Vector3(Random.Range(-4, 4), 0, Random.Range(-4, 4)), Quaternion.identity);
            newZombieObj.transform.rotation = Quaternion.identity;

            //Configure the rust monster behavior
            RustMonsterBehavior rustMonsterBehavior = newZombieObj.GetComponent<RustMonsterBehavior>();

            rustMonsterBehavior.active = agroOffSpawn;

            newZombieObj.SetActive(true);
        }
    }
}
