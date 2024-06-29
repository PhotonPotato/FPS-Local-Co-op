using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    public string weaponName;
    public WeaponType type;
    public DamageType damageType;
    public GameObject model;
    public int damage;
    public float range;

    public int bulletPoolIndex;
    public Transform gunMuzzle;
    public float fireRate = 0.1f;
    public int magazineSize = 30;
    public float reloadTime = 1f;
    public LayerMask hitLayers;

    private int currentAmmo;
    private float nextFireTime;

    public bool fireDissabled = false;
    public bool reloading = false;
    public float reloadTimer = 0; //Only updates when script is active!

    public float muzzleFlashIntensity = 1f;

    public float rumbleFireDuration = .8f;
    public float rumbleIntensity = 1;

    public float kickbackAmount = .01f;
    public float kickbackTime = .05f;
    public float kickbackRotation = 10f;

    public float bulletSpreadHip = .5f;
    public float bulletSpreadADS = .5f;

    public WeaponController operatingController;

    private int senderID = -1; //Stores player index of owner

    private void Start()
    {
        currentAmmo = magazineSize;
    }

    private void Update()
    {
        if (reloading)
        {
            //Change ammo Bar to reload timer??
            reloadTimer -= Time.deltaTime;

            Reload();
        }
    }

    public Vector3 GetBulletFireVector()
    {
        Ray ray = new Ray(operatingController.camPos.position, operatingController.camPos.forward);

        //Raycast from the camera forward and then find the position that the player is aiming at
        if (Physics.Raycast(ray, out RaycastHit hit, 100, operatingController.bulletLayers, QueryTriggerInteraction.Ignore))
        {
            //Makrer.transform.position = hit.point;

            if (hit.collider == null)
            {
                //If nothing is in front of the player, just fire straight out of the gun
                return gunMuzzle.forward;
            }
            else
            {
                return (hit.point - gunMuzzle.position).normalized;
            }
        }

        return gunMuzzle.forward;
    }

    public Vector3 GetRandomSpreadVector(bool isADS)
    {
        Vector3 output = Vector3.one;

        output *= (Mathf.PerlinNoise(Random.value * 1000 + 100, Random.value * 1000) - .5f) * (isADS ? bulletSpreadADS : bulletSpreadHip);

        return output;
    }

    public bool HandleFireCall(bool isADS = false)
    {
        //Make sure its not dissabled
        if (fireDissabled || reloading) return false;


        if (Time.time > nextFireTime)
        {
            //Can't shoot if the gun doesn't have ammo
            if (currentAmmo <= 0)
            {
                HandleReloadCall();
                return false;
            }

            Fire(isADS);
            return true;
        }

        return false;
    }

    private void Fire(bool isADS)
    {
        nextFireTime = Time.time + fireRate;
        currentAmmo--;

        //Just a catch for the null ref error that happens when the game scene is joined
        if (BulletObjectPoolManager.SharedInstance == null) return;

        GameObject bullet = BulletObjectPoolManager.SharedInstance.GetPooledObject(bulletPoolIndex);

        if (bullet == null)
        {
            Debug.Log("No Objects Left In Pool");
            return;
        }

        bullet.transform.SetPositionAndRotation(gunMuzzle.position, gunMuzzle.rotation);
        bullet.SetActive(true);

        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            Debug.Log($"operating controller: {operatingController}");
            bulletScript.Initialize(damage, operatingController.bulletLayers, type == WeaponType.Grenade ? operatingController.camPos.forward : GetBulletFireVector() + GetRandomSpreadVector(isADS), senderID, damageType);
        }

        TrailRenderer rend = bullet.GetComponentInChildren<TrailRenderer>();
        if (rend != null)
        {
            rend.Clear();
            rend.emitting = true;
        }
    }

    public bool HandleReloadCall()
    {
        if (reloading || currentAmmo == magazineSize) return false;

        reloading = true;

        reloadTimer = reloadTime; //Set up timer (will update until less than 0)

        return true;
    }

    private void Reload()
    {
        // Implement reload logic here

        //If it has been the whole reload duration
        if (reloadTimer <= 0)
        {
            currentAmmo = magazineSize;
            reloading = false;

            //Reset the Ammo bar max and min to be for bullet capacity?
        }
    }

    public int GetCurrentAmmo()
    {
        return currentAmmo;
    }

    public void SetSenderID(int id) => senderID = id;
}

public enum WeaponType
{
    Melee,
    Ranged,
    Grenade,
    Sniper
}