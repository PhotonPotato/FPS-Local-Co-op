using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnManager : MonoBehaviour
{
    public static PlayerSpawnManager SceneSpawnManager;

    public Transform PlayerSpawnPoint;
    int numPlayers = 0; //Local variacle to keep the count of players

    public void Start()
    {
        if (SceneSpawnManager == null) SceneSpawnManager = this;
    }

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.GetComponent<CharacterController>().enabled = false;

        playerInput.gameObject.transform.position = PlayerSpawnPoint.position;

        playerInput.GetComponent<CharacterController>().enabled = true;

        playerInput.GetComponent<PlayerManager>().playerIndex = numPlayers;

        Generator.generator.activePlayers.Add(playerInput.transform);
        Generator.generator.AllCurrentActiveObjects.Add(new System.Collections.Generic.List<GameObject>());

        StartCoroutine(Generator.generator.ShowRoomsCloseToPlayer(numPlayers)); // Throw in a room update as well

        playerInput.GetComponent<Health>().SetPlayerID(numPlayers);

        numPlayers++;
    }


    public int GetNumPlayers => numPlayers;
}
