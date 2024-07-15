using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    // Add health bs here

    [Header("Refs")]
    public WeaponInventory m_WeaponInventory;
    PlayerInventoryManager m_LootInventoryManager;
    PlayerInput m_Input;
    public WeaponController m_WeaponController;
    Health m_Health;
    public Slider m_HealthBar;

    public GameObject m_RightHandParent;
    public GameObject m_DisplayObject;
    public Image m_BlackDeathScreen;

    InputAction interactAction;

    public TMP_Text accountBalanceTextInventoryPanel;
    public TMP_Text accountBalanceTextBuyPanel;

    public Image SniperCrosshairImage;
    public Image SniperCrosshairBottomCover;
    public Image SniperCrosshairTopCover;

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

    public bool showDeathScreen;
    public bool isAlive = true;

    //Player needs to know this incase it dies inside the extract zone
    //It will need to tell the extract zone abt it
    [System.NonSerialized] public bool isInExtractZone = false;

    public void Start()
    {
        m_WeaponInventory = GetComponent<WeaponInventory>();
        m_LootInventoryManager = GetComponent<PlayerInventoryManager>();

        m_Input = GetComponent<PlayerInput>();

        m_WeaponController = GetComponent<WeaponController>();

        m_Health = GetComponent<Health>();

        interactAction = m_Input.actions["Interact"];

        m_HealthBar.maxValue = m_Health.GetMaxHealth();
    }

    public void Update()
    {
        if (!isAlive)
        {
            //Show the death screen if its not already
            if (!m_BlackDeathScreen.gameObject.activeSelf)
            {
                m_BlackDeathScreen.color = Color.clear;
                m_BlackDeathScreen.gameObject.SetActive(true);
            }

            //Fade to black
            m_BlackDeathScreen.color += (Color.black - m_BlackDeathScreen.color) / 10;

            //Don't bother with anything else if you are dead
            return;
        }

        //Check for the interact input
        if (interactAction.ReadValue<float>() == 1)
        {
            HandleInteractionKeyPressed();//Debug.Log("Interact " + HandleInteractionKeyPressed());
        }

        //Only update the account balance text if the inventory is open
        if (m_LootInventoryManager.inventoryPanelOpen) accountBalanceTextInventoryPanel.text = $"Account Balance: ${playerAccountBalance}";
        //Only update the buy panel's account balance if the buy panel is open
        if (m_LootInventoryManager.buyPanelOpen) accountBalanceTextBuyPanel.text = $"Account Balance: ${playerAccountBalance}";

        m_HealthBar.value = m_Health.GetHealth();

        if (transform.position.y < -20) OnThisEntityDeath();
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

                    m_LootInventoryManager.AddItem(item);
                    break;

                case "SellStation":
                    Debug.Log("Sell Station Interacted With");

                    //Just call the function that opens the sell panel
                    GetComponent<PlayerInventoryManager>().OnOpenItemSellPanel();
                    break;

                case "BuyStation":
                    Debug.Log("Buy Station Interacted With");

                    //Just call the function that opens the buy panel
                    GetComponent<PlayerInventoryManager>().OnOpenBuyPanel();
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

    /// <summary>
    /// Drops all inventory items.
    /// Close all inventory panels.
    /// Turns off major player scripts (movement, inventory, etc).
    /// Turns on black death screen.
    /// 
    /// Broadcasted by health component upon player death.
    /// </summary>
    public void OnThisEntityDeath()
    {
        Debug.Log("Player Death");
        //Drop items
        m_LootInventoryManager.DropAllItems();

        //Close all panels
        m_LootInventoryManager.OnCloseBuyPanel();
        m_LootInventoryManager.OnCloseItemSellPanel();
        m_LootInventoryManager.OnCloseInventoryPanel();

        //Disable major player scripts (excluding this one, that will happen later) and collider
        EnableAllMajorPlayerComponents(false, false, true, false);

        //Show death screen (happens when is alive becomes false)
        isAlive = false;

        //Update the extract zones player count if the players inside one upon death
        if (isInExtractZone) FindAnyObjectByType<LevelExtractManager>().PlayerDiedWithinExtractZone();

        PlayerSpawnManager.SceneSpawnManager?.OnPlayerDeath();

        //Hide the players
        m_DisplayObject.SetActive(false);


        //Particles?
        //Sounds??
    }


    /// <summary>
    /// Shuts down all movement and key scrips to effective turn the character into a vegetable.
    /// Can be used for podium scene and other menus.
    /// It is KEY to remember that this can be called even IF the script is dissabled.
    /// </summary>
    /// <param name="enable">
    /// Used to either enable or disable the components in question.
    /// </param>
    public void EnableAllMajorPlayerComponents(bool enable = true, bool allowDisablingOfCamera = true, bool enableModificationOfPlayerRightHand = true, bool allowDisableOfThisPlayerManager = true)
    {
        GetComponent<PlayerCharacterController>().enabled = enable;
        GetComponent<PlayerInventoryManager>().enabled = enable;
        GetComponent<WeaponController>().enabled = enable;

        if (enable || allowDisablingOfCamera) GetComponentInChildren<Camera>().enabled = enable;

        GetComponent<CapsuleCollider>().enabled = enable;

        if (enableModificationOfPlayerRightHand) m_RightHandParent.SetActive(enable);

        //Disable the player manager as well (if its requested)
        if (enable || allowDisableOfThisPlayerManager) this.enabled = enable;
    }

    /// <summary>
    /// Sets all 3 sniper crosshair images to a specified alpha while preserving color <param name="alpha"></param>
    /// </summary>
    public void SetSniperCrosshairImagesAlpha(float alpha)
    {
        SniperCrosshairImage.color =  new Color(SniperCrosshairImage.color.r, SniperCrosshairImage.color.g, SniperCrosshairImage.color.b, alpha);
        SniperCrosshairTopCover.color = new Color(SniperCrosshairTopCover.color.r, SniperCrosshairTopCover.color.g, SniperCrosshairTopCover.color.b, alpha);
        SniperCrosshairBottomCover.color = new Color(SniperCrosshairBottomCover.color.r, SniperCrosshairBottomCover.color.g, SniperCrosshairBottomCover.color.b, alpha);
    }

    /// <summary>
    /// Fades all 3 sniper crosshair image panels towards a specified alpha <paramref name="alpha"/>
    /// </summary>
    public void FadeSniperCrosshairImagesAlphaToColor(float alpha, float speed)
    {
        SniperCrosshairImage.color += new Color(0, 0, 0, (alpha - SniperCrosshairImage.color.a) / speed);
        SniperCrosshairTopCover.color += new Color(0, 0, 0, (alpha - SniperCrosshairTopCover.color.a) / speed);
        SniperCrosshairBottomCover.color += new Color(0, 0, 0, (alpha - SniperCrosshairBottomCover.color.a) / speed);
    }
}
