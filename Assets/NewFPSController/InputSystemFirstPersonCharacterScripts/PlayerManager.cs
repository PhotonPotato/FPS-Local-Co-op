using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerManager : MonoBehaviour
{
    // Add health bs here

    //[Header("Refs")]
    WeaponInventory m_Inventory;
    PlayerInput m_Input;
    WeaponController m_WeaponController;

    InputAction interactAction;

    [Header("Settings")]
    public LayerMask interactionQueryLayers;

    public float interactionDistance = 3f;
    public float interactionSpherecastRadius = 1f;

    [System.NonSerialized] public int playerIndex;

    public void Start()
    {
        m_Inventory = GetComponent<WeaponInventory>();
        m_Input = GetComponent<PlayerInput>();

        m_WeaponController = GetComponent<WeaponController>();

        interactAction = m_Input.actions["Interact"];
    }

    public void Update()
    {
        //Check for the interact input
        if (interactAction.ReadValue<float>() == 1)
        {
            HandleInteractionKeyPressed();//Debug.Log("Interact " + HandleInteractionKeyPressed());
        }
    }

    public bool HandleInteractionKeyPressed() //Handles raycasting for pickups
    {
        RaycastHit ineractionQueryHit;

        //Spherecast to ground in front of player
        if (Physics.SphereCast(m_WeaponController.camPos.position, interactionSpherecastRadius, m_WeaponController.camPos.forward, out ineractionQueryHit, interactionDistance, interactionQueryLayers, QueryTriggerInteraction.Collide))
        {
            //Initiate the pickup
            switch (ineractionQueryHit.collider.tag)
            {
                case "WeaponPickup":
                    m_Inventory.OnWeaponPickup(ineractionQueryHit.collider.gameObject);
                    break;

                case "Chest":
                    //Access and run "on open" type shi
                    ineractionQueryHit.collider.gameObject.GetComponent<ChestBehavior>().OnChestOpen();
                    break;

            }

            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("faggot fagoot fagoot");

        //Check for corridoor triggers
        switch (other.tag)
        {

            //If its a corridoor trigger, send an optimization message
            case "CorridoorTrigger":
                StartCoroutine(Generator.generator.ShowRoomsCloseToPlayer(playerIndex));
                break;
        }
    }
}
