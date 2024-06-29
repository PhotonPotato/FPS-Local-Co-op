using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerInventoryManager : MonoBehaviour
{
    public List<LootItem> items;
    public List<GameObject> inventoryDisplaySlots;

    [Header("Refs")]
    public PlayerManager m_PlayerManager;

    public Transform InventoryPanel;
    public Transform InventoryItemParent;
    public Transform InventoryDiplaySlotsParent;

    public GameObject DefaultInventoryDisplaySlotObject;

    public Transform InventorySellPanel;
    public TMP_Text SellPanelReceiptText;

    PlayerInput m_PlayerInput;
    InputAction openInventoryAction;

    [Header("Trackers")]
    [Tooltip("True if open, false if not")]
    public bool inventoryPanelOpen = false;

    public bool sellPanelOpen = false;

    [Header("Settings")]
    private bool lastInventoryButtonState = false;

    private void Start()
    {
        m_PlayerManager = GetComponent<PlayerManager>();
        m_PlayerInput = GetComponent<PlayerInput>();

        openInventoryAction = m_PlayerInput.actions.FindAction("OpenInventory");
    }

    public void Update()
    {
        //Look for the inventory button down (processors are applied when using ReadValue).
        if (openInventoryAction.IsPressed() && !lastInventoryButtonState)
        {
            inventoryPanelOpen = !inventoryPanelOpen;

            //Close the other panel if we're trying to open this one
            if (sellPanelOpen) OnCloseItemSellPanel();
        }

        //Update the sate of the inventory panel
        InventoryPanel.gameObject.SetActive(inventoryPanelOpen);

        //Keep this updating
        lastInventoryButtonState = openInventoryAction.ReadValue<float>() > 0;
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
            //Set the inventory slot icon by creating a new sprite from the png
            inventoryDisplaySlots[i].GetComponent<Image>().sprite =
            Sprite.Create(items[i].displayTexture, new Rect(0, 0, ItemIconGenerator.RESWIDTH, ItemIconGenerator.RESHEIGHT), Vector2.zero);
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

    public void ClearAllInventoryItems()
    {
        //Go through and remove all items in the items list
        for (int i = items.Count - 1; i >= 0; i--)
        {
            RemoveItem(items[i]);
        }
    }

    public void AddEmptyDisplaySlot()
    {
        //Add another item by instantiating the default one
        inventoryDisplaySlots.Add(Instantiate(DefaultInventoryDisplaySlotObject, InventoryDiplaySlotsParent));
    }

    public void RemoveDisplaySlot(int index)
    {
        // remove and also destroy the object from the world.
        Destroy(inventoryDisplaySlots[index]);
        inventoryDisplaySlots.RemoveAt(index);
    }

    ///<summary>
    /// Gets called when interacting with the sell station
    /// Opens sell panel in player canvas
    /// Offers option to "Sell all items"
    /// </summary>
    public void OnOpenItemSellPanel()
    {
        InventorySellPanel.gameObject.SetActive(true);

        sellPanelOpen = true;

        if (inventoryPanelOpen == true) inventoryPanelOpen = false;

        //Add sounds or triggers below
    }

    /// <summary>
    /// Called to close sell panel when "x" button on panel
    /// is pressed.
    /// </summary>
    public void OnCloseItemSellPanel()
    {
        InventorySellPanel.gameObject.SetActive(false);

        sellPanelOpen = false;

        //Add sounds effects or triggers below
    }

    /// <summary>
    /// Called by close button in top right of inventory panel
    /// </summary>
    public void OnCloseInventoryPanel()
    {
        inventoryPanelOpen = false;

        //Play a sound or something
    }

    /// <summary>
    /// Called when "Sell All Items" pressed
    /// Add money to balance in player manager account balance
    /// Generates string receipt to be shown in text field
    /// Removes all items from the inventory list
    /// </summary>
    public void HandleItemSell()
    {
        string receipt = "Receipt Will Print Below:\n";

        if (items.Count == 0)
        {
            receipt += "No items to sell.";
            SellPanelReceiptText.text = receipt;

            return;
        }
        else receipt+= "    ------------------------\n";

        float total = 0;

        //Save the state of items.count before iterating through the list
        int originalItemCount = items.Count;

        //Loop through all items
        for (int i = 0; i < originalItemCount; i++)
        {
            //Always look at and delete the bottom of the stack
            LootItem item = items[0];

            float itemSellPrice = item.GetItemSellPrice();

            //Update total
            total += itemSellPrice;

            //Add to account balance
            m_PlayerManager.AddToAccountBalance(itemSellPrice);

            //Add to the receipt
            receipt += string.Format("  {0}   {1}    ${2}\n",
                                     Random.Range(100000, 999999),
                                     item.GetItemName(),
                                     itemSellPrice);

            //Delete the item before moving on
            RemoveItem(items[0]);
        }

        //Print a total
        receipt += "    ------------------------\n" +
                   $"   Total:          ${total}";

        //Set the text field to display the receipt
        SellPanelReceiptText.text = receipt;
    }
}
