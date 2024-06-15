using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class EventsManager : MonoBehaviour
{
    /// <summary>
    /// Runs when all players have joined.
    /// Loads the level scene and unloads the player joining screen.
    /// </summary>

    Scene ActiveGameScene;
    public Camera JoinSceneCamera;
    public GameObject JoinCanvas;
    PlayerSpawnManager SpawnManager;

    //Janky way to run a function after it ran
    float frameWhenGameSceneLoaded = -100;

    //Used to pass the active players list between the playerSpawnManager and the 
    List<Transform> tempSaveOfActivePlayers;

    public void Start()
    {
        SpawnManager = FindAnyObjectByType<PlayerSpawnManager>();
    }

    private void Update()
    {
        ///<summary>
        /// The following will run once and ONLY 1 frame after the game scene is loaded.
        /// This is because:
        /// 1. Leaving a one frame buffer allows for the Scene Manager to update,
        ///    meaning that SetActiveScene will NOT throw a NullPointer error.
        ///
        /// 2. The first frame leaves time for the Start function to run for the
        ///    Generator.
        /// </summary>
        if (Time.frameCount == frameWhenGameSceneLoaded + 1)
        {
            //Update the spawn manager refernece
            SpawnManager = FindAnyObjectByType<PlayerSpawnManager>();
            
            //Set the game scene to active
            SceneManager.SetActiveScene(ActiveGameScene);

            //Move the players
            MovePlayersToLevel();

            Generator.generator.activePlayers = FindAnyObjectByType<PlayerSpawnManager>().activePlayers;

            for (int i = 0; i < Generator.generator.activePlayers.Count; i++)
            {
                Generator.generator.AllCurrentActiveObjects.Add(new List<GameObject>());
            }

            //Move the players using the spawn manager's function
            SpawnManager.MovePlayersToGameSpawnLocation();

            //Show all rooms near players
            Generator.generator.ShowRoomsCloseToAllPlayerss();
        }
    }

    public void OnGameInitiated()
    {
        var parameters = new LoadSceneParameters(LoadSceneMode.Additive);

        ActiveGameScene = SceneManager.LoadScene("GameScene", parameters);

        Destroy(FindObjectsByType<EventSystem>(FindObjectsSortMode.None)[0].gameObject);

        //Hide the ready button
        JoinCanvas?.SetActive(false);

        //update the frame tracker
        frameWhenGameSceneLoaded = Time.frameCount;
    }

    //Move the root player objects to the new scene
    public void MovePlayersToLevel()
    {
        //Move the player input manager object as well
        SceneManager.MoveGameObjectToScene(SpawnManager.gameObject, ActiveGameScene);
        SceneManager.MoveGameObjectToScene(SpawnManager.PlayerSpawnPoint.gameObject, ActiveGameScene);

        PlayerSpawnManager spawnManager = PlayerSpawnManager.SceneSpawnManager;

        foreach(Transform player in spawnManager.activePlayers)
        {
            SceneManager.MoveGameObjectToScene(player.gameObject, ActiveGameScene);

            //Makes the player movement active again
            player.GetComponent<PlayerCharacterController>().enabled = true;
            player.GetComponent<PlayerInventoryManager>().enabled = true;

            player.GetComponent<WeaponController>().enabled = true;
            player.GetComponent<PlayerManager>().enabled = true;
            player.GetComponentInChildren<Camera>().enabled = true;

            JoinSceneCamera.gameObject.SetActive(false);
        }
    }
}
