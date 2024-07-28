using UnityEngine;
using UnityEngine.InputSystem.UI;
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

    public Transform InventoryBuyPanel;

    //Settings
    public Slider m_LookSensitivitySlider;
    private TMP_Text m_LookSensitivityLabel;
    public Slider m_ADSSensitivityMultiplierSlider;
    private TMP_Text m_ADSSensitivityLabel;

    public PlayerCharacterController m_CharacterController;

    //The multiplayer eventsystem that the player uses
    public MultiplayerEventSystem m_PlayerEventSystem;

    [Tooltip("First UI Element to be selected when opening the sell panel")]
    public GameObject m_SellPanelFirstSelectedButton;
    [Tooltip("First UI Element to be selected when opening the buy panel")]
    public GameObject m_BuyPanelFirstSelectedButton;
    [Tooltip("First UI Element to be selected when opening the inventory panel")]
    public GameObject m_InventoryPanelFirstSelectedButton;

    PlayerInput m_PlayerInput;
    InputAction openInventoryAction;
    InputAction backButtonAction;

    [Header("Trackers")]
    [Tooltip("True if open, false if not")]
    public bool inventoryPanelOpen = false;

    public bool sellPanelOpen = false;
    public bool buyPanelOpen = false;

    [Header("Settings")]
    private bool lastInventoryButtonState = false;

    private void Start()
    {
        m_PlayerManager = GetComponent<PlayerManager>();
        m_PlayerInput = GetComponent<PlayerInput>();
        m_CharacterController = GetComponent<PlayerCharacterController>();

        m_LookSensitivityLabel = m_LookSensitivitySlider.GetComponentInChildren<TMP_Text>();
        m_ADSSensitivityLabel = m_ADSSensitivityMultiplierSlider.GetComponentInChildren<TMP_Text>();

        openInventoryAction = m_PlayerInput.actions.FindAction("PauseMenu");
        backButtonAction = m_PlayerInput.actions.FindAction("MenuBack");
    }

    public void Update()
    {
        //Look for the inventory button down (processors are applied when using ReadValue).
        if (openInventoryAction.IsPressed() && !lastInventoryButtonState)
        {
            inventoryPanelOpen = !inventoryPanelOpen;

            if (inventoryPanelOpen) OnOpenInventoryPanel();

            //Close the other panel if we're trying to open this one
            if (sellPanelOpen) OnCloseItemSellPanel();
            else if (buyPanelOpen) OnCloseBuyPanel();
        }

        //If in a UI menu
        if (inventoryPanelOpen || sellPanelOpen || buyPanelOpen)
        {
            //if back button pressed
            if (backButtonAction.ReadValue<float>() > 0)
            {
                //Close all menus
                if (inventoryPanelOpen) OnCloseInventoryPanel();
                if (buyPanelOpen) OnCloseBuyPanel();
                if (sellPanelOpen) OnCloseItemSellPanel();
            }
        }

        if (inventoryPanelOpen)
        {
            //Update the settings text
            m_LookSensitivityLabel.text = "Look Sensistivity: " + m_LookSensitivitySlider.value;
            m_ADSSensitivityLabel.text = "ADS Sensitivity Multiplier: " + m_ADSSensitivityMultiplierSlider.value;
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
        Debug.Log("Open sell panel");

        InventorySellPanel.gameObject.SetActive(true);

        //Update the first ui selection for the event system
        m_PlayerEventSystem.SetSelectedGameObject(m_SellPanelFirstSelectedButton);

        sellPanelOpen = true;

        if (inventoryPanelOpen) inventoryPanelOpen = false;
        else if (buyPanelOpen) OnCloseBuyPanel();
        //Add sounds or triggers below
    }

    /// <summary>
    /// Called to close sell panel when "x" button on panel
    /// is pressed.
    /// </summary>
    public void OnCloseItemSellPanel()
    {
        Debug.Log("Close sell panel");

        InventorySellPanel.gameObject.SetActive(false);

        sellPanelOpen = false;

        //Add sounds effects or triggers below
    }

    public void OnOpenInventoryPanel()
    {
        //Update the first ui selection for the event system
        m_PlayerEventSystem.SetSelectedGameObject(m_InventoryPanelFirstSelectedButton);

        m_LookSensitivitySlider.value = m_CharacterController.lookSensitivity;
        m_ADSSensitivityMultiplierSlider.value = m_CharacterController.adsSensitivityMultiplier;
    }

    /// <summary>
    /// Called by close button in top right of inventory panel
    /// </summary>
    public void OnCloseInventoryPanel()
    {
        Debug.Log("Close inventory panel");

        inventoryPanelOpen = false;

        //Update the settings
        m_CharacterController.lookSensitivity = m_LookSensitivitySlider.value;
        m_CharacterController.adsSensitivityMultiplier = m_ADSSensitivityMultiplierSlider.value;

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
        Debug.Log("Sell pressed");

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

    /// <summary>
    /// Called by the player inventory manager when a buy panel is interacted with
    /// </summary>
    public void OnOpenBuyPanel()
    {
        Debug.Log("Open buy panel");

        InventoryBuyPanel.gameObject.SetActive(true);

        //Update the first ui selection for the event system
        m_PlayerEventSystem.SetSelectedGameObject(m_BuyPanelFirstSelectedButton);

        buyPanelOpen = true;

        if (inventoryPanelOpen) inventoryPanelOpen = false;
        else if (sellPanelOpen) OnCloseItemSellPanel();

        //Add sounds or triggers below
    }

    /// <summary>
    /// Called by close button on the buy panel
    /// </summary>
    public void OnCloseBuyPanel()
    {
        Debug.Log("Close buy panel");
        InventoryBuyPanel.gameObject.SetActive(false);

        buyPanelOpen = false;

        //Add sound effects here
    }

    public void DropItem(int index, bool randomDropPosOffset = true, bool removeItemFromItemsList = true, bool updateDisplaySlots = true)
    {
        Vector3 dropPosition = transform.position - new Vector3(0, 1f, 0);

        if (randomDropPosOffset) dropPosition += new Vector3(Random.Range(-.5f, .5f), 0, Random.Range(-.5f, .5f));

        //Actually move the item out of the content bin
        items[index].transform.SetParent(null);
        items[index].transform.position = dropPosition;
        items[index].transform.rotation = Quaternion.identity;

        //Remove item from list if requested
        //(if this argument is set to false, chances are the list will get cleared
        //after shits done by whatevers calling this)
        if (removeItemFromItemsList) items.RemoveAt(index);

        //Sync the display slots with the items.
        if (updateDisplaySlots) ForceUpdateInventoryDisplaySlots();
    }

    /// <summary>
    /// Not only clears the item inventory/diplay slots
    /// but drops the items around the player.
    /// </summary>
    public void DropAllItems()
    {
        for (int i = 0; i < items.Count; i++)
        {
            //Move drops to a randomly determined mosition
            DropItem(i, true, false,false);
        }

        items.Clear();

        //Sync the display slots with the items.
        ForceUpdateInventoryDisplaySlots();
    }
}
