using UnityEngine;
using UnityEngine.UI;

public class LootItem : MonoBehaviour
{
    /// <summary>
    /// Holds data for items littered throughout the dungeon.
    /// </summary>
    ///

    [SerializeField] private int itemID = -1;
    [SerializeField] private string itemName;
    [SerializeField] private float itemSellPrice;

    public GameObject itemModel;

    public Texture2D displayTexture;

    public int GetItemID() => itemID;
    public string GetItemName() => itemName;
    public float GetItemSellPrice() => itemSellPrice;
}
