using UnityEngine;
using UnityEngine.UI;

public class LootItem : MonoBehaviour
{
    /// <summary>
    /// Holds data for items littered throughout the dungeon.
    /// </summary>
    ///

    public int itemID = -1;
    public string itemName;
    public float itemSellPrice;

    public GameObject itemModel;

    public Sprite displayImage;

    public int GetItemID => itemID;
    public string GetItemName => itemName;
    public float GetItemSellPrice => itemSellPrice;
}
