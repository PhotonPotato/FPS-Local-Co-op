using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    // Add health bs here

    [Header("Refs")]
    WeaponInventory m_WeaponInventory;
    PlayerInventoryManager m_LootInventorymanager;
    PlayerInput m_Input;
    public WeaponController m_WeaponController;
    Health m_Health;
    public Slider m_HealthBar;

    InputAction interactAction;

    public TMP_Text accountBalanceText;

    public Image SniperCrosshairImage;

    [Header("Settings")]
    public LayerMask interactionQueryLayers;

    public float interactionDistance = 3f;
    public float interactionSpherecastRadius = 1f;
    float timeOfLastInteraction = Mathf.NegativeInfinity;
    [Tooltip("Limits how fast one can interact consecutively")]
    public float minTimeBetweenInteractions = .1f;

    [System.NonSerialized] public int playerIndex;

    [Header("Trackers")]
    public float playerAccountBalance {get; private set;}  = 0f;

    public void Start()
    {
        m_WeaponInventory = GetComponent<WeaponInventory>();
        m_LootInventorymanager = GetComponent<PlayerInventoryManager>();

        m_Input = GetComponent<PlayerInput>();

        m_WeaponController = GetComponent<WeaponController>();

        m_Health = GetComponent<Health>();

        interactAction = m_Input.actions["Interact"];

        m_HealthBar.maxValue = m_Health.GetMaxHealth();
    }

    public void Update()
    {
        //Check for the interact input
        if (interactAction.ReadValue<float>() == 1)
        {
            HandleInteractionKeyPressed();//Debug.Log("Interact " + HandleInteractionKeyPressed());
        }

        //Only update the account balance text if the inventory is open
        if (m_LootInventorymanager.inventoryPanelOpen) accountBalanceText.text = $"Account Balance: ${playerAccountBalance}";

        m_HealthBar.value = m_Health.GetHealth();

        if (m_Health.GetHealth() <= 0) Destroy(this.gameObject);
    }

    public bool HandleInteractionKeyPressed() //Handles raycasting for pickups
    {
        //Make sure that it has been long enough between interactions
        if (Time.time - timeOfLastInteraction < minTimeBetweenInteractions) return false;

        RaycastHit interactionQueryHit;

        //Spherecast to ground in front of player
        if (Physics.SphereCast(m_WeaponController.camPos.position, interactionSpherecastRadius, m_WeaponController.camPos.forward, out interactionQueryHit, interactionDistance, interactionQueryLayers, QueryTriggerInteraction.Collide))
        {
            //Initiate the pickup
            switch (interactionQueryHit.collider.tag)
            {
                case "WeaponPickup":
                    m_WeaponInventory.OnWeaponPickup(interactionQueryHit.collider.gameObject);
                    break;

                case "Chest":
                    //Access and run "on open" type shi
                    interactionQueryHit.collider.gameObject.GetComponent<ChestBehavior>().OnChestOpen();
                    break;

                case "ItemPickup":
                    //Get the actual item
                    Debug.Log("item detected, picking up");

                    LootItem item = interactionQueryHit.transform.GetComponent<LootItem>();

                    m_LootInventorymanager.AddItem(item);
                    break;

                case "SellStation":
                    Debug.Log("Sell Station Interacted With");

                    //Just call the function that opens the sell panel
                    GetComponent<PlayerInventoryManager>().OnOpenItemSellPanel();
                    break;

            }

            timeOfLastInteraction = Time.time;

            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        //Check for corridoor triggers
        switch (other.tag)
        {

            //If its a corridoor trigger, send an optimization message
            case "CorridoorTrigger":
                StartCoroutine(Generator.generator.ShowRoomsCloseToPlayer(playerIndex));
                break;
        }
    }

    public void AddToAccountBalance(float amount) => playerAccountBalance += amount;
}
