using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.InputSystem.XInput;
using Unity.VisualScripting;
using UnityEngine.UIElements;

public class WeaponController : MonoBehaviour
{
    public Transform handPosition;
    public Transform offhand;

    public WeaponInventory inventory;
    public PlayerCharacterController m_PlayerController;
    private InputSystemFirstPersonControls inputActions;
    private WeaponBobbing m_WeaponBobbingBehavior;
    public PlayerManager m_PlayerManager;

    private PlayerInput m_PlayerInput;
    private InputAction m_ChangeWeaponInput;
    private InputAction m_FireWeaponPrimaryInput;
    private InputAction m_ReloadInput;

    private int currentWeaponIndex = 0;
    private GameObject currentWeaponGameObject;

    public WeaponBehavior currentWeaponBehavior;

    public Transform camPos;

    public Transform ADSWeaponPosTransform;

    public LayerMask bulletLayers;

    public float minTimeBetweenWeaponChange = .1f;
    private float timeOfLastWeaponChange = Mathf.NegativeInfinity;

    public Light m_MuzzleFlash;

    public Coroutine muzzleFlashCoroutine;

    //Haptics
    public XInputController m_Controller;
    private float timeOfLastRumble = Mathf.NegativeInfinity;

    //Other
    public bool FiringDisabled = false;

    private bool lastChangeWeaponState = false;

    private void Awake()
    {
        m_PlayerInput = GetComponent<PlayerInput>();
        m_ChangeWeaponInput = m_PlayerInput.actions["Change Weapon"];
        m_FireWeaponPrimaryInput = m_PlayerInput.actions["Fire Primary"];
        m_ReloadInput = m_PlayerInput.actions["Reload"];

        m_PlayerController = GetComponent<PlayerCharacterController>();
        inputActions = m_PlayerController.inputActions;

        m_WeaponBobbingBehavior = GetComponent<WeaponBobbing>();

        m_PlayerManager = GetComponent<PlayerManager>();

        if (inventory.weapons.Count > 0)
        {
            EquipWeapon(inventory.weapons[currentWeaponIndex]);
        }

        camPos = m_PlayerController.cam.gameObject.transform;
    }

    private void Update()
    {
        // Scroll through the inventory using the mouse scroll wheel
        float scrollWheelInput = m_ChangeWeaponInput.ReadValue<float>();

        //WAS THROWING A NULL REF, MIGHT WANT TO UNCOMMENT LATER
        //inputActions.FPSController.ChangeWeapon.Reset();

        //Disable firing if the player is in a menu
        FiringDisabled = m_PlayerController.isInMenu;

        //Don't allow for weapon changes if a menu is open
        if (!m_PlayerController.isInMenu && scrollWheelInput != 0f && !lastChangeWeaponState)
        {
            ChangeWeapon(Mathf.RoundToInt(scrollWheelInput));

            //Update the weapon bobbing so that it can reset the weapons rotation
            m_WeaponBobbingBehavior.OnWeaponChange();
        }


        //Read fire input (make sure shooting aint dissabled
        if (m_FireWeaponPrimaryInput.ReadValue<float>() == 1 && !FiringDisabled)
        {
            if (currentWeaponBehavior.HandleFireCall(m_PlayerController.isADS))
            {
                //Fire was initiated
                m_MuzzleFlash.enabled = true;

                if (muzzleFlashCoroutine == null) muzzleFlashCoroutine = StartCoroutine(InitiateMuzzleFlash(currentWeaponBehavior.muzzleFlashIntensity, .05f, Time.time));

                //Send out a pulse
                InitiateFireRumblePulse();

                m_WeaponBobbingBehavior.OnWeaponFire(currentWeaponBehavior.kickbackAmount, currentWeaponBehavior.kickbackRotation, currentWeaponBehavior.kickbackTime);
            }
        }

        //Read reload logic
        if (m_ReloadInput.ReadValue<float>() == 1)
        {
            currentWeaponBehavior.HandleReloadCall();
        }

        UpdateWeaponAmmoUI();

        if (m_Controller != null) HandleControllerRumble();

        if (muzzleFlashCoroutine == null) m_MuzzleFlash.enabled = false;

        //Update the view of the sniper crosshair overlay so the alpha always fades towards a desired value
        if (currentWeaponBehavior.type == WeaponType.Sniper)
        {
            //Fades the scope in or out
            m_PlayerManager.FadeSniperCrosshairImagesAlphaToColor(m_PlayerController.isADS ? 1 : 0, 5);

            //Hide the sniper model if the scope is in
            currentWeaponBehavior.model.SetActive(!m_PlayerController.isADS);
        }
        else m_PlayerManager.SetSniperCrosshairImagesAlpha(0);

        lastChangeWeaponState = scrollWheelInput != 0;
    }

    private void ChangeWeapon(int direction)
    {
        currentWeaponIndex += direction;
        if (currentWeaponIndex < 0)
        {
            currentWeaponIndex = inventory.weapons.Count - 1;
        }
        else if (currentWeaponIndex >= inventory.weapons.Count)
        {
            currentWeaponIndex = 0;
        }

        //Update the timer so that weapons don't jsut fly by
        timeOfLastWeaponChange = Time.time;

        //Actually handle the weapon change inventory
        EquipWeapon(inventory.weapons[currentWeaponIndex]);
    }

    public void EquipWeapon(WeaponBehavior weapon)
    {
        // Destroy the current weapon GameObject if exists
        if (currentWeaponGameObject != null)
        {
            //Destroy(currentWeaponGameObject);

            //Hide the unused weapons
            currentWeaponGameObject.transform.SetParent(offhand);
        }

        // Instantiate the new weapon GameObject
        //currentWeaponGameObject = Instantiate(weapon.model, handPosition.position, handPosition.rotation);

        currentWeaponGameObject = inventory.weapons[currentWeaponIndex].gameObject;

        currentWeaponGameObject.transform.parent = handPosition;

        // Ensure the weapon is positioned correctly in the hand
        currentWeaponGameObject.transform.localPosition = Vector3.zero;
        currentWeaponGameObject.transform.localRotation = Quaternion.identity;

        //Set the new behavior
        currentWeaponBehavior = weapon;

        //Update the sender ID of the weapon
        currentWeaponBehavior.SetSenderID(m_PlayerManager.playerIndex);
        Debug.Log($"weapon controller player instanceID {m_PlayerManager.playerIndex}");

        currentWeaponBehavior.operatingController = this;

        currentWeaponBehavior.SetSniperADSMuzzleTransform(ADSWeaponPosTransform);
    }

    public bool SetCurrentWeaponIndex(int index)
    {
        currentWeaponIndex = index;

        return true;
    }

    private void UpdateWeaponAmmoUI()
    {
        //DEPRICATED, used to be for multiple ammo bars
        //Run through and update each slider for each weapon
        /*Slider slider = inventory.AmmoSliderDisplayGroup.GetChild(currentWeaponIndex).GetComponentInChildren<Slider>();

        if (slider != null)
        {
            if (currentWeaponBehavior.reloading)
            {
                //Make the "ammo" slider into a display of the reload time
                slider.maxValue = currentWeaponBehavior.reloadTime;

                slider.value = currentWeaponBehavior.reloadTimer;
            }
            else
            {
                //Just use it as a normal ammo slider
                slider.maxValue = currentWeaponBehavior.magazineSize;

                slider.value = currentWeaponBehavior.GetCurrentAmmo();
            }
        }*/

        if (inventory.AmmoSlider != null)
        {
            if (currentWeaponBehavior.reloading)
            {
                //Update the status icon
                inventory.StatusImage.sprite = inventory.reloadingStatusIcon;

                //Make the "ammo" slider into a display of the reload time
                inventory.AmmoSlider.maxValue = currentWeaponBehavior.reloadTime;

                inventory.AmmoSlider.value = currentWeaponBehavior.reloadTimer;
            }
            else
            {
                //Update the status icon to the icon of the current weapon
                inventory.StatusImage.sprite = currentWeaponBehavior.Icon;

                //Just use it as a normal ammo slider
                inventory.AmmoSlider.maxValue = currentWeaponBehavior.magazineSize;

                inventory.AmmoSlider.value = currentWeaponBehavior.GetCurrentAmmo();
            }
        }
    }

    private void InitiateFireRumblePulse()
    {
        timeOfLastRumble = Time.time;
    }

    private void HandleControllerRumble()
    {
        if (Time.time - timeOfLastRumble > currentWeaponBehavior.rumbleFireDuration)
        {
            m_Controller.SetMotorSpeeds(0f, 0f);
        }
        else
        {
            m_Controller.SetMotorSpeeds(currentWeaponBehavior.rumbleIntensity / 4, currentWeaponBehavior.rumbleIntensity);
        }
    }

    public IEnumerator InitiateMuzzleFlash(float intensity, float duration, float timeStampStart)
    {
        float period = (Mathf.PI * 2) / duration;

        while (Time.time - timeStampStart <= duration)
        {
            float currentIntensity = intensity * Mathf.Sin(period * Time.time - timeStampStart);

            m_MuzzleFlash.intensity = currentIntensity;

            yield return null;
        }

        muzzleFlashCoroutine = null;
    }
}
