using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class LevelExtractManager : MonoBehaviour
{
    public ExtractionType extractionType;

    public PlayerSpawnManager SpawnManager;
    public EventSystem MarketEventSystem;
    public Material mat;

    public int playersWithinZone;

    public Color NoPlayersColor;
    public Color AllPlayersInZoneColor;
    public Color ExtractColor;

    public Scene GameScene;
    public Scene ExtractDestinationScene;

    private bool DestinationSceneloaded = false;
    private int DestinationSceneLoadedFrameStamp = int.MaxValue;

    private bool countdownToExtractInitiated = false;
    private float timeOfExtractCountdownInitiation = Mathf.Infinity;
    public float TimeToExtract = 5f;

    void Start()
    {
        SpawnManager = FindAnyObjectByType<PlayerSpawnManager>();
        MarketEventSystem = FindAnyObjectByType<EventSystem>();
    }

    void Update()
    {
        // The follwing gets called 1 frame after the new market scene is loaded
        // bc the scene gets loaded a frame after it is called to be loaded
        if (Time.frameCount - DestinationSceneLoadedFrameStamp == 1)
        {
            //Move spawn manager and player spawn point (bc it holds the splitscreen player input tech)

            SceneManager.MoveGameObjectToScene(SpawnManager.gameObject, ExtractDestinationScene);
            SceneManager.MoveGameObjectToScene(SpawnManager.PlayerSpawnPoint.gameObject, ExtractDestinationScene);

            //Move players
            for (int i = 0; i < SpawnManager.activePlayers.Count; i++)
            {
                Transform player = SpawnManager.activePlayers[i];
                PlayerManager playerManager = player.GetComponent<PlayerManager>();

                //Wake up player if they are dead
                if (!playerManager.isAlive)
                {
                    //Re-enable the key scripts
                    playerManager.EnableAllMajorPlayerComponents();

                    playerManager.m_DisplayObject.SetActive(true);
                    playerManager.m_BlackDeathScreen.gameObject.SetActive(false);

                    playerManager.isAlive = true;
                }

                if (extractionType == ExtractionType.GameScene)
                {
                    //Move player to where the market is in world space
                    SpawnManager.MovePlayerToMarketSceneSpawnLocation(player);
                }
                else
                {
                    //Disable the player's scripts and camera
                    player.GetComponent<PlayerManager>().EnableAllMajorPlayerComponents(false);

                    //Move the player to the correct podium position
                    SpawnManager.MovePlayerToPodiumPosition(i);

                    FindAnyObjectByType<EventsManager>().gameStarted = false;
                }

                //Send the player to the new scene
                SceneManager.MoveGameObjectToScene(player.gameObject, ExtractDestinationScene);
            }

            //Destroy(MarketEventSystem.gameObject);

            //Enable the podium scene camera
            if (extractionType == ExtractionType.MarketScene) FindAnyObjectByType<EventsManager>().OnPlayersReenteringPodiumScene();

            //Unload game scene
            SceneManager.SetActiveScene(ExtractDestinationScene);
            SceneManager.UnloadSceneAsync(GameScene);
        }

        //If the countdown has elapsed, initiate the market scene loading
        if (!DestinationSceneloaded && countdownToExtractInitiated && Time.time - timeOfExtractCountdownInitiation > TimeToExtract)
        {
            LoadDestinationScene();

            DestinationSceneloaded = true;
        }


        if (countdownToExtractInitiated)
        {
            mat.color = Color.Lerp(AllPlayersInZoneColor, ExtractColor, (Time.time - timeOfExtractCountdownInitiation) / TimeToExtract);
        }
        else
        {
            mat.color = Color.Lerp(NoPlayersColor, AllPlayersInZoneColor, playersWithinZone / SpawnManager.activePlayers.Count);
        }

        //Check for if the # players inside the extract = the # players alive
        if (!countdownToExtractInitiated && playersWithinZone > 0 && playersWithinZone == SpawnManager.GetNumAlivePlayers())
        {
            //Initiate the extract countdown
            countdownToExtractInitiated = true;
            timeOfExtractCountdownInitiation = Time.time;
        }

        //Check if everyones dead
        if (SpawnManager.GetNumAlivePlayers() == 0)
        {
            AllPlayersDead();
        }
    }

    public void LoadDestinationScene()
    {
        //Load the scene
        GameScene = SceneManager.GetActiveScene();

        MarketEventSystem = FindAnyObjectByType<EventSystem>();

        var parameters = new LoadSceneParameters(LoadSceneMode.Additive);

        if (extractionType == ExtractionType.GameScene)
        {
            //then we are extracting to the market
            ExtractDestinationScene = SceneManager.LoadScene("MarketScene", parameters);
        }
        else
        {
            //Then we are extractng to the podium scene
            ExtractDestinationScene = SceneManager.GetSceneAt(0);
        }

        Debug.Log("extract scene loading");

        DestinationSceneLoadedFrameStamp = Time.frameCount;
    }

    public void OnTriggerEnter(Collider other)
    {
        //Catch when a player enters this zone
        if (other.gameObject.tag == "Player")
        {
            playersWithinZone++;

            //Tell the player that its in the extract zone
            other.GetComponent<PlayerManager>().isInExtractZone = true;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        //Catch when a player exits this zone
        if (other.gameObject.tag == "Player")
        {
            playersWithinZone--;

            other.GetComponent<PlayerManager>().isInExtractZone = false;

            //Reset the extract countdown
            countdownToExtractInitiated = false;
        }
    }

    public void PlayerDiedWithinExtractZone()
    {
        playersWithinZone--;

        countdownToExtractInitiated = false;
    }

    public void AllPlayersDead()
    {
        //Send people straight to the menu
        extractionType = ExtractionType.MarketScene;

        LoadDestinationScene();

        DestinationSceneloaded = true;
    }

    public enum ExtractionType
    {
        GameScene,
        MarketScene
    }
}
