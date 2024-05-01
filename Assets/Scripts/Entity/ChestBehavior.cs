using UnityEngine;

public class ChestBehavior : MonoBehaviour
{
    public GameObject[] possibleChestDrops;

    public int numberDrops = 1;

    public GameObject chestOpportunityParticles;

    public bool opened = false;
    
    public void OnChestOpen()
    {
        if (opened) return;

        //Destroy the particles and show chest open animation
        Destroy(chestOpportunityParticles);
        //Inset animation call


        //Spawn drops
        for (int i = 0; i < numberDrops; i++)
        {
            Instantiate(GetRandomDrop(), transform.position + (transform.forward * 2), Quaternion.identity);
        }

        opened = true;
    }

    public GameObject GetRandomDrop()
    {
        return possibleChestDrops[Random.Range(0, possibleChestDrops.Length)];
    }
}
