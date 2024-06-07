using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerInventoryManager : MonoBehaviour
{
    public List<LootItem> items;
    public List<GameObject> inventoryDisplaySlots;

    [Header("Refs")]
    public Transform InventoryItemParent;
    public Transform InventoryDiplaySlotsParent;

    public GameObject DefaultInventoryDisplaySlotObject;

    public void Update()
    {

    }

    public void ForceUpdateInventoryDisplaySlots()
    {
        //Resize the inventory display slot list
        while (inventoryDisplaySlots.Count != items.Count)
        {
            if (inventoryDisplaySlots.Count > items.Count)
            {
                //There are too many display items, remove one.
                RemoveDisplaySlot(0);
            }
            else
            {
                //There are not enough display items, so add more (by instantiating the default one).
                AddEmptyDisplaySlot();
            }
        }

        //Reset all slots
        for (int i = 0; i < inventoryDisplaySlots.Count; i++)
        {
            //Set the inventory
            inventoryDisplaySlots[i].GetComponent<Image>().sprite = items[i].displayImage;
        }
    }

    public void AddItem(LootItem item)
    {
        items.Add(item);

        //Move the item into the inventory
        item.transform.SetParent(InventoryItemParent);

        //Update the inventory display slots to keep the display and item list consistent.
        ForceUpdateInventoryDisplaySlots();
    }

    public void RemoveItem(LootItem item, bool deleteItemUponRemoval = true)
    {
        
        items.Remove(item);

        if (deleteItemUponRemoval) Destroy(item.gameObject);

        //Update the inventory display slots to keep the display and item list consistent.
        ForceUpdateInventoryDisplaySlots();
    }

    public void AddEmptyDisplaySlot()
    {
        //Add another item by instantiating the default one
        inventoryDisplaySlots.Add(Instantiate(DefaultInventoryDisplaySlotObject, InventoryDiplaySlotsParent));
    }

    public void RemoveDisplaySlot(int index)
    {
        // remove and also destroy the object from the world.
        inventoryDisplaySlots.RemoveAt(0);
        Destroy(inventoryDisplaySlots[0]);
    }
}
