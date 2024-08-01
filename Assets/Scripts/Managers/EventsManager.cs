using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.IO;

public class EventsManager : MonoBehaviour
{
    public static EventsManager SharedInstance;
    /// <summary>
    /// Runs when all players have joined.
    /// Loads the level scene and unloads the player joining screen.
    /// </summary>

    [Header("Refs")]
    Scene ActiveGameScene;
    public Camera JoinSceneCamera;
    public GameObject JoinCanvas;
    public PlayerInputManager m_PlayerInputManager;
    public GameObject PodiumSceneParentObject;
    public Light PodiumDirectionalLight;
    PlayerSpawnManager SpawnManager;
    EventSystem PodiumEventSystem;

    public InputActionAsset playerActions;
    InputAction startGameAction;

    [Header("Settings")]
    public bool resetDifficultyOnStart = true;

    public AnimationCurve EnemyDifficultyVsEnemyType;

    [Header("Trackers")]
    public float currentRoomDifficulty;
    public float currentEnemyDifficulty;

    public bool gameStarted = false;

    //Janky way to run a function after it ran
    public float frameWhenGameSceneLoaded = -100;

    //Used to pass the active players list between the playerSpawnManager and the 
    List<Transform> tempSaveOfActivePlayers;

    public float roomDifficultyPunishmentFromLoss = .2f;
    public float roomDifficultyRewardFromSuccess = .3f;

    public void Start()
    {
        SharedInstance = this;

        SpawnManager = FindAnyObjectByType<PlayerSpawnManager>();
        PodiumEventSystem = FindAnyObjectByType<EventSystem>();

        startGameAction = playerActions.FindAction("StartGame");

        //pretty much if there are no players, then it will be the first lobby opened in the game.
        if (resetDifficultyOnStart && SpawnManager.GetNumPlayers == 0) ResetDifficultyToDefault();
    }

    private void Update()
    {
        if (!gameStarted && startGameAction.ReadValue<float>() > 0 && SpawnManager.GetNumPlayers > 0)
        {
            gameStarted = true;

            OnGameInitiated();
        }

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

                //Update the minimaps tracked players
                MinimapManager.SharedInstance.AddTrackedObject(Generator.generator.activePlayers[i], Color.green);
            }

            //Move the players using the spawn manager's function
            SpawnManager.MovePlayersToGameSpawnLocation();
        }
        else if (Time.frameCount == frameWhenGameSceneLoaded + 6)
        {
            //Show all rooms near players
            Generator.generator.ShowRoomsCloseToAllPlayerss();
        }
        else if (Time.frameCount == frameWhenGameSceneLoaded + 18) //18 is arbitrary but it just works
        {
            Debug.Log("finding graveyards...");
            //Open all graveyards
            foreach (GraveyardBehavior GraveyardBehavior in FindObjectsByType<GraveyardBehavior>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Debug.Log("found graveyard");

                GraveyardBehavior.gameObject.SetActive(true);
                GraveyardBehavior.transform.SetParent(null);
            }

            LevelExtractManager.SharedInstance.transform.SetParent(null);

            Generator.generator.ShowRoomsCloseToAllPlayerss();
        }
    }

    public void OnGameInitiated()
    {
        Debug.Log("Start Game Clicked");

        PodiumDirectionalLight.gameObject.SetActive(false);
        PodiumSceneParentObject.SetActive(false);

        SpawnManager.ResetNumAlivePlayers();

        var parameters = new LoadSceneParameters(LoadSceneMode.Additive);

        ActiveGameScene = SceneManager.LoadScene("GameScene", parameters);

        //Hide the ready button
        JoinCanvas?.SetActive(false);

        //update the frame tracker
        frameWhenGameSceneLoaded = Time.frameCount;

        //Disable player joining
        m_PlayerInputManager.DisableJoining();
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

            //Makes the player movement active again along with other functions
            player.GetComponent<PlayerManager>().EnableAllMajorPlayerComponents(true);

        }

        JoinSceneCamera.gameObject.SetActive(false);
    }

    //Called when players are being transported back from the market scene
    public void OnPlayersReenteringPodiumScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        JoinSceneCamera.gameObject.SetActive(true);
        JoinCanvas.SetActive(true);

        PodiumDirectionalLight.gameObject.SetActive(true);
        PodiumSceneParentObject.SetActive(true);

        //Enable player joining again
        m_PlayerInputManager.EnableJoining();
    }
    
    public void EnactDifficultyReward() => Generator.generator.UpdateDifficultySettingsForExtract(roomDifficultyRewardFromSuccess, .2f, -.2f);

    public void EnactDifficultyPunishment()
    {
        Generator.generator.UpdateDifficultySettingsForExtract(roomDifficultyPunishmentFromLoss * -1, -.1f, .1f);
    }

    public void ResetDifficultyToDefault()
    {
        //Read the default to a array
        var lines = File.ReadAllLines(GetFilePath("LevelDifficultyDefault.txt"));

        //Write the array to the actual save file
        File.WriteAllLines(GetFilePath("LevelDifficulty.txt"), lines);
    }

    string GetFilePath(string fileName)
    {
        return Path.Combine(Application.streamingAssetsPath, fileName);
    }
}
