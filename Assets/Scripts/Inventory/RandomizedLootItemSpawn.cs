using System.Collections.Generic;
using UnityEngine;

public class RandomizedLootItemSpawn : MonoBehaviour
{
    [System.Serializable]
    public struct WeightedLootItemSpawn
    {
        public GameObject item;
        public float SpawnPercentChance;
    }
    
    public WeightedLootItemSpawn[] items;

    void Awake()
    {
        //Choose the item based on its probability
        float randSpawnChance = Random.value * 100;
        Debug.Log("rand chance " + randSpawnChance);

        //Stores the items that have a spawn chance above the random spawn percent
        List<GameObject> possibleSpawns = new List<GameObject>();

        foreach (WeightedLootItemSpawn item in items)
        {
            if (item.SpawnPercentChance >= randSpawnChance)
            {
                possibleSpawns.Add(item.item);
            }
        }

        //Choose a random one out of this spanw list and spawn it under this object
        GameObject obj = Instantiate(possibleSpawns[Random.Range(0, possibleSpawns.Count)], transform);
        obj.transform.localPosition = Vector3.zero;

        //Get rid of this monobehavior
        Destroy(this);
    }
}
