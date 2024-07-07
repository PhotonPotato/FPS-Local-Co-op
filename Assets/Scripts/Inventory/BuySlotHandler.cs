using UnityEngine;

public class BuySlotHandler : MonoBehaviour
{
    public GameObject rootParent;

    public WeaponBehavior SlotWeapon;
    public int weaponPrice;

    public void OnBuyButtonPressed()
    {
        PlayerManager playerManager = rootParent.GetComponent<PlayerManager>();

        //Check if account balance has enough
        if (playerManager.playerAccountBalance >= weaponPrice)
        {
            //Deduct the price from the account value
            playerManager.AddToAccountBalance(weaponPrice * -1);
        }
        else
        {
            //Not enough in player account,
            //Maybe play a bad noise

            return;
        }

        //Add the weapon to the players inventory
        playerManager.m_WeaponInventory.OnWeaponBuy(SlotWeapon);
    }
}
