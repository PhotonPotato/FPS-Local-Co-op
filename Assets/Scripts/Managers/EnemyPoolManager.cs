using System.Collections.Generic;
using UnityEngine;

public class EnemyPoolManager : MonoBehaviour
{
    public static EnemyPoolManager SharedInstance;

    [System.Serializable]
    public struct Pool
    {
        public string name;
        public int count;
        public GameObject ObjectToPool;
        public List<GameObject> PooledObjects;
        public Transform PoolParent;
    }

    public List<Pool> pools;

    private void Start()
    {
        if (SharedInstance == null) SharedInstance = this;
    }

    private void Update()
    {
        if (Time.frameCount == EventsManager.Instance.frameWhenGameSceneLoaded + 1)
        {
            SharedInstance = this;

            GeneratePools();
        }
    }

    /// <summary>
    /// Generate and initate all the pools
    /// </summary>
    public void GeneratePools()
    {
        for (int i = 0; i < pools.Count; i++)
        {
            Pool pool = pools[i];

            for (int j = 0; j < pool.count; j++)
            {
                //Spawn and hide the object
                GameObject obj = Instantiate(pool.ObjectToPool, pool.PoolParent);

                obj.SetActive(false);

                //if its a rust monster, tell the obj that its in a pool
                RustMonsterBehavior rustMonsterBehavior;
                if (obj.TryGetComponent<RustMonsterBehavior>(out rustMonsterBehavior))
                {
                    rustMonsterBehavior.pooledObj = true;
                }

                pool.PooledObjects.Add(obj);
            }
        }
    }

    public bool GetPooledObject(int poolIndex, out GameObject obj)
    {
        Pool pool = pools[poolIndex];

        for (int i = 0; i < pool.count; i++)
        {
            if (!pool.PooledObjects[i].activeInHierarchy)
            {
                obj = pool.PooledObjects[i];
                obj.SetActive(true);
                return true;
            }
        }
        
        obj = null;
        return false;
    }
}
