using UnityEngine;

public class WeaponBehavior : MonoBehaviour
{
    public string weaponName;
    public WeaponType type;
    public GameObject model;
    public int damage;
    public float range;

    public GameObject bulletPrefab;
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

    [System.NonSerialized] public WeaponController operatingController;
    public GameObject Makrer;

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
        if (Physics.Raycast(ray, out RaycastHit hit, 100, operatingController.bulletLayers))
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

    public bool HandleFireCall()
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

            Fire();
            return true;
        }

        return false;
    }

    private void Fire()
    {
        nextFireTime = Time.time + fireRate;
        currentAmmo--;

        Debug.Log("FIRE");
        
        GameObject bullet = Instantiate(bulletPrefab, gunMuzzle.position, gunMuzzle.rotation);
        Bullet bulletScript = bullet.GetComponent<Bullet>();

        if (bulletScript != null)
        {
            bulletScript.Initialize(damage, operatingController.bulletLayers, GetBulletFireVector());
        }
    }

    public bool HandleReloadCall()
    {
        if (reloading || currentAmmo == magazineSize) return false;

        reloading = true;

        reloadTimer = reloadTime; //Set up timer (will update until less than 0)

        Debug.Log("REEE LOOAD");

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
}

public enum WeaponType
{
    Melee,
    Ranged
}