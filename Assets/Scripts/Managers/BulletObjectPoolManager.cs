using UnityEngine;
using System.Collections.Generic;

public class BulletObjectPoolManager : MonoBehaviour
{
    public static BulletObjectPoolManager SharedInstance;

    public Transform PoolParent;

    [System.Serializable]
    public struct Pool
    {
        public string name;
        public int poolAmount;
        public List<PoolObject> pooledObjects;
        public PoolObject objectToPool;

        public bool UseMinimumTimeRecyclying;
        public float minimumTimeBeforeRecycle;
    }

    [System.Serializable]
    public class PoolObject
    {
        public float timeInit;
        public GameObject obj;

        public PoolObject(GameObject obj, float timeInit = -1000)
        {
            this.obj = obj;
            this.timeInit = timeInit;
        }
    }

    public List<Pool> ObjectPools;

    void Start()
    {
        SharedInstance = this;

        Debug.Log($"shaerd manager: {BulletObjectPoolManager.SharedInstance}");

        if (ObjectPools.Count == 0) return;

        Pool pool;
        for (int i = 0; i < ObjectPools.Count; i++)
        {
            pool = ObjectPools[i];

            pool.pooledObjects = new List<PoolObject>();
            PoolObject tmp;

            for (int j = 0; j < pool.poolAmount; j++)
            {
                tmp = new PoolObject(Instantiate(pool.objectToPool.obj, PoolParent));
                
                tmp.obj.SetActive(false);

                ObjectPools[i].pooledObjects.Add(tmp);
            }
        }

        
    }

    //Make it name based later mby use a dictionary
    public GameObject GetPooledObject(int poolIndex)
    {
        Pool pool = ObjectPools[poolIndex];

        for (int i = 0; i < pool.poolAmount; i++)
        {
            if (!pool.pooledObjects[i].obj.activeInHierarchy)
            {
                pool.pooledObjects[i].timeInit = Time.time;

                return pool.pooledObjects[i].obj;
            }

            if (pool.UseMinimumTimeRecyclying)
            {
                //Try to recycle
                if (Time.time - pool.pooledObjects[i].timeInit > pool.minimumTimeBeforeRecycle)
                {
                    pool.pooledObjects[i].timeInit = Time.time;

                    return pool.pooledObjects[i].obj;
                }
            }
        }
        return null;
    }

    public GameObject GetAndInitializePooledObject(int poolIndex, Vector3? pos = null)
    {
        GameObject tmp = GetPooledObject(poolIndex);

        tmp.SetActive(true);

        tmp.transform.position = pos == null ? Vector3.zero : pos.Value;

        return tmp;
    }
}
