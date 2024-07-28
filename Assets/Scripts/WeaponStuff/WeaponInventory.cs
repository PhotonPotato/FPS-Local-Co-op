using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponInventory : MonoBehaviour
{

    public List<WeaponBehavior> weapons = new List<WeaponBehavior>();

    public WeaponController Controller;

    public List<WeaponBehavior> startingWeapons;

    public Slider AmmoSlider;
    public Image StatusImage;

    public Sprite reloadingStatusIcon;

    public GameObject AmmoSliderPrefab;

    // Add a new weapon to the inventory
    public void AddWeapon(WeaponBehavior newWeapon)
    {
        weapons.Add(newWeapon);
    }

    // Remove a weapon from the inventory
    public void RemoveWeapon(WeaponBehavior weaponToRemove)
    {
        //Destroy the visualization of the object as well
        int index = weapons.IndexOf(weaponToRemove);

        weapons.Remove(weaponToRemove);
        Destroy(weaponToRemove.gameObject);
    }

    // Check if the inventory contains a specific weapon
    public bool HasWeapon(WeaponBehavior weaponToCheck)
    {
        return weapons.Contains(weaponToCheck);
    }

    void AddStartingWeapons()
    {
        int i = 0;
        foreach (WeaponBehavior weapon in startingWeapons)
        {
            //Work around way to make sure that it works with weapons that are in the prefab folder
            GameObject weaponSpawn = Instantiate(weapon.gameObject);

            WeaponBehavior tempWeapon = weaponSpawn.GetComponent<WeaponBehavior>();

            AddWeapon(tempWeapon);
            Controller.SetCurrentWeaponIndex(i);
            
            Controller.EquipWeapon(tempWeapon);
            i++;
        }
    }

    // Example usage
    public void Awake()
    {
        Controller = GetComponent<WeaponController>();

        AddStartingWeapons();

        // Create some weapons
        /*GameObject swordModel = Resources.Load<GameObject>("SwordModel");
        Weapon sword = new Weapon("Sword", swordModel, 10);

        GameObject gunModel = Resources.Load<GameObject>("GunModel");
        Weapon gun = new Weapon("Gun", gunModel, 20, 50f);*/

        // Add weapons to the inventor

        // Example of checking if the inventory contains a specific weapon
        /*if (HasWeapon(sword))
        {
            Debug.Log("Inventory contains Sword!");
        }

        // Example of removing a weapon from the inventory
        RemoveWeapon(gun);

        // Example of checking if the inventory contains a specific weapon after removal
        if (!HasWeapon(gun))
        {
            Debug.Log("Inventory does not contain Gun anymore!");
        }*/
    }

    public void OnWeaponPickup(GameObject pickupParent)
    {
        //Get and equip the weapon
        WeaponBehavior pickupWeapon = pickupParent.GetComponentInChildren<WeaponBehavior>();

        AddWeapon(pickupWeapon);
        Controller.SetCurrentWeaponIndex(weapons.Count - 1);
        Controller.EquipWeapon(pickupWeapon);

        //Get rid of the pickup container
        Destroy(pickupParent);
    }

    public void OnWeaponBuy(WeaponBehavior pickupWeapon)
    {
        //Pull the actual prefab object and spawn it in
        GameObject weaponSpawn = Instantiate(pickupWeapon.gameObject);

        WeaponBehavior tempWeapon = weaponSpawn.GetComponent<WeaponBehavior>();

        AddWeapon(tempWeapon);

        Controller.SetCurrentWeaponIndex(weapons.Count - 1);
        Controller.EquipWeapon(tempWeapon);
    }
}
