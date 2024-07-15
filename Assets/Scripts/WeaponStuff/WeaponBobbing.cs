using System.Collections;
using UnityEngine;

public class WeaponBobbing : MonoBehaviour
{
    public Transform weaponTransform;
    public Transform weaponKickbackParentTransform;
    public PlayerCharacterController m_CharacterController;

    public float frequency = 1.0f;
    public float bobbingAmount = 0.1f;
    public float returnSpeed = 2.0f; // Adjust this to control the speed of returning to the original position
    public float bobbingBasedOnMovementMagnitude = .2f;

    [Tooltip("Deadzone for movement before weapon bobs")]
    public float magnitudeThreshhold;

    private float timeElapsed = 0.0f;
    private Vector3 originalPosition;

    private Vector3 originalKickbackParentLocalPosition;
    private Quaternion originalKickbackParentLocalRotation;

    private Coroutine kickbackCoroutine;

    void Start()
    {
        // Store the original position of the weapon
        originalPosition = weaponTransform.localPosition;

        originalKickbackParentLocalPosition = weaponKickbackParentTransform.localPosition;
        originalKickbackParentLocalRotation = weaponKickbackParentTransform.localRotation;

        m_CharacterController = GetComponent<PlayerCharacterController>();
    }

    void Update()
    {
        // Get input (you can replace this with your own input logic)
        float inputMagnitude = m_CharacterController.GetPlayerMovement().magnitude;
        bool inputActive = inputMagnitude >= magnitudeThreshhold;

        // Update time elapsed
        if (inputActive)
        {
            timeElapsed += Time.deltaTime * frequency * (bobbingBasedOnMovementMagnitude * inputMagnitude);
        }
        else
        {
            // If no input, smoothly return to the original position
            if (weaponTransform.localPosition != originalPosition)
            {
                weaponTransform.localPosition = Vector3.Lerp(weaponTransform.localPosition, originalPosition, returnSpeed * Time.deltaTime);
            }
            timeElapsed = 0.0f;
            return;
        }

        // Calculate weapon bobbing offset using a sine wave
        float bobbingOffset = Mathf.Sin(timeElapsed) * bobbingAmount;

        // Apply bobbing offset to weapon transform
        Vector3 newPosition = originalPosition;
        newPosition.y += bobbingOffset;
        weaponTransform.localPosition = newPosition;
    }

    public void OnWeaponFire(float kickbackAmount, float rotAmount, float kickbackTime)
    {
        if (kickbackCoroutine != null)
        {
            StopCoroutine(kickbackCoroutine);
        }
        kickbackCoroutine = StartCoroutine(KickbackCoroutine(kickbackAmount, rotAmount, kickbackTime));
    }

    /// <summary>
    /// Called when weapon is changed
    /// Resets the weapons position and rotation
    /// </summary>
    public void OnWeaponChange()
    {
        weaponKickbackParentTransform.localPosition = originalKickbackParentLocalPosition;
        //Reset the kickback rotation of the weapon
        weaponKickbackParentTransform.localRotation = originalKickbackParentLocalRotation;
    }

    IEnumerator KickbackCoroutine(float amount, float rotAmount, float kickbackTime)
    {
        float elapsedTime = 0.0f;
        Vector3 initialPosition = weaponKickbackParentTransform.localPosition;
        Vector3 kickbackPosition = initialPosition + weaponKickbackParentTransform.up * amount;

        Quaternion initialRot = weaponKickbackParentTransform.localRotation;

        Quaternion kickbackRot = Quaternion.Euler(weaponKickbackParentTransform.localRotation.eulerAngles - new Vector3(30 * rotAmount, 0, 0));//weaponModelTransform.rotation.x + 30 * amount, weaponModelTransform.rotation.y + 0, weaponModelTransform.rotation.z + 0, weaponModelTransform.rotation.w);

        while (elapsedTime < kickbackTime)
        {
            weaponKickbackParentTransform.localPosition = Vector3.Lerp(initialPosition, kickbackPosition, elapsedTime / kickbackTime);
            weaponKickbackParentTransform.localRotation = Quaternion.Slerp(initialRot, kickbackRot, 1 - Mathf.Pow(1 - elapsedTime / kickbackTime, 3));

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the weapon returns to its original position
        weaponKickbackParentTransform.localPosition = initialPosition;
        weaponKickbackParentTransform.localRotation = initialRot;
    }

    public void SetOriginalPosition(Vector3 pos) => originalPosition = pos;
}

