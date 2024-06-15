using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class PlayerSpawnManager : MonoBehaviour
{
    public bool DEBUGMODE = false;

    [Header("Refs")]
    public static PlayerSpawnManager SceneSpawnManager;

    public Transform PlayerSpawnPoint;

    [Header("Trackers")]
    int numPlayers = 0; //Local variacle to keep the count of players
    public List<Transform> activePlayers { get; private set; } //Saves all joined player transforms

    [Header("Settings")]
    public Transform[] spawnPodiumPositions; //All locations to spawn a player upon joining
    public float spawnPodiumPlayerYOffset = 1.93f;

    public void Start()
    {
        activePlayers = new List<Transform>();

        if (SceneSpawnManager == null) SceneSpawnManager = this;
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        //Get a new spawn point for the player
        SetPlayerPositionAndRotation(playerInput, DEBUGMODE ? PlayerSpawnPoint.position : GetNewPlayerSpawnPoint(), DEBUGMODE ? Quaternion.identity : GetNewPlayerRotation());

        playerInput.GetComponent<PlayerManager>().playerIndex = numPlayers;

        //playerInput.uiInputModule = FindAnyObjectByType<InputSystemUIInputModule>();

        //This works to catch any null refs for active players
        if (activePlayers == null) activePlayers = new List<Transform>();

        activePlayers.Add(playerInput.transform);

        //The following is from when players joined directly into the scene:
        //Generator.generator.activePlayers.Add(playerInput.transform)
        //Generator.generator.AllCurrentActiveObjects.Add(new System.Collections.Generic.List<GameObject>());

        //StartCoroutine(Generator.generator.ShowRoomsCloseToPlayer(numPlayers)); // Throw in a room update as well

        playerInput.GetComponent<Health>().SetPlayerID(numPlayers);

        if (DEBUGMODE)
        {
            Generator.generator.activePlayers.Add(playerInput.transform);
            Generator.generator.AllCurrentActiveObjects.Add(new List<GameObject>());

            StartCoroutine(Generator.generator.ShowRoomsCloseToPlayer(numPlayers));
        }
        else
        {
            playerInput.GetComponent<PlayerCharacterController>().enabled = false;
            playerInput.GetComponent<PlayerInventoryManager>().enabled = false;
            playerInput.GetComponent<WeaponController>().enabled = false;
            playerInput.GetComponent<PlayerManager>().enabled = false;
            playerInput.GetComponentInChildren<Camera>().enabled = false;
        }

        numPlayers++;
    }

    private Vector3 GetNewPlayerSpawnPoint() => spawnPodiumPositions[numPlayers].position + new Vector3(0, spawnPodiumPlayerYOffset, 0);

    private Quaternion GetNewPlayerRotation() => spawnPodiumPositions[numPlayers].rotation;

    public int GetNumPlayers => numPlayers;

    private void SetPlayerPositionAndRotation(PlayerInput input, Vector3 position, Quaternion rot)
    {
        CharacterController playerController = input.GetComponent<CharacterController>();

        //Player movement can only happen when the character controller is disabled
        //(Because the character controller overrides the position)
        playerController.enabled = false;
        input.transform.position = position;
        input.transform.rotation = rot;
        playerController.enabled = true;
    }

    public void MovePlayersToGameSpawnLocation()
    {
        //Find a spot in the main level to drop players
        foreach (Transform player in Generator.generator.activePlayers)
        {
            SetPlayerPositionAndRotation(player.GetComponent<PlayerInput>(), PlayerSpawnPoint.position + (Vector3.one * Random.Range(0, 3f)), Quaternion.identity);
        }
    }
}
