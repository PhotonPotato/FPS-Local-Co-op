using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MinimapManager : MonoBehaviour
{
    public static MinimapManager SharedInstance;

    [Header("Refs")]
    public Camera minimapCaptureCam;

    private RectTransform minimapTransform;
    public Image minimapImage;

    public GameObject minimapTrackedObjMarkerPrefab;

    [Header("Settings")]

    public List<Transform> trackedTransforms;
    public List<RectTransform> trackedMinimapMarkerTransforms;

    public Sprite minimapSprite;

    public int RESWIDTH = 1920;
    public int RESHEIGHT = 1080;

    public float RefreshTime = .3f;
    private float timeOfLastRefresh = float.NegativeInfinity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SharedInstance = this;

        minimapTransform = minimapImage.GetComponent<RectTransform>();

        trackedTransforms = new List<Transform>();
        trackedMinimapMarkerTransforms = new List<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount == EventsManager.SharedInstance.frameWhenGameSceneLoaded + 4) Generator.generator.ShowAllRooms(true);
        if (Time.frameCount == EventsManager.SharedInstance.frameWhenGameSceneLoaded + 5)
        {
            minimapCaptureCam.enabled = true;
            GenerateMinimapImage();
            minimapCaptureCam.enabled = false;

            minimapImage.sprite = minimapSprite;

            Generator.generator.ShowRoomsCloseToAllPlayerss();

            //playerTransforms = Generator.generator.activePlayers.ToArray();
        }

        //Update the minimap trackers on a refresh timer
        if (Time.time - timeOfLastRefresh > RefreshTime)
        {
            for (int i = 0; i < trackedTransforms.Count; i++)
            {
                Debug.Log(WorldToMinimapCoord(trackedTransforms[i].position));
                trackedMinimapMarkerTransforms[i].localPosition = WorldToMinimapCoord(trackedTransforms[i].position);
            }

            timeOfLastRefresh = Time.time;
        }
    }

    public void GenerateMinimapImage()
    {
        RenderTexture rt = new RenderTexture(RESWIDTH, RESHEIGHT, 24);
        rt.filterMode = FilterMode.Point;

        Texture2D minimapTexture = new Texture2D(RESWIDTH, RESHEIGHT);

        minimapTexture.filterMode = FilterMode.Point;

        //Set up the cameras target render texture
        minimapCaptureCam.targetTexture = rt;

        //Actually call the camera's render function
        minimapCaptureCam.Render();

        //Ensure the current active render texture is rt
        RenderTexture.active = rt;

        minimapTexture.ReadPixels(new Rect(0, 0, RESWIDTH, RESHEIGHT), 0, 0);

        //Clear all the textures
        minimapCaptureCam.targetTexture = null;
        RenderTexture.active = null;

        //Destroy the render texture to save memory
        Destroy(rt);

        //Now set the image to the screenshot
        minimapSprite = Sprite.Create(minimapTexture, new Rect(0,0, RESWIDTH, RESHEIGHT), new Vector2(.5f, .5f), 1000);
    }

    public Vector3 WorldToMinimapCoord(Vector3 pos)
    {
        Vector2 viewportPoint = minimapCaptureCam.WorldToViewportPoint(pos);

        //Remap Screenpoint to be within the confines of the rect transform
        
        return new Vector2(minimapTransform.rect.xMin + (minimapTransform.rect.width * viewportPoint.x), minimapTransform.rect.yMin + (minimapTransform.rect.height * viewportPoint.y)); ;
    }

    public bool AddTrackedObject(Transform trackedTransform, Color markerColor)
    {
        if (minimapTrackedObjMarkerPrefab == null) return false;

        Image trackedObjImage = Instantiate(minimapTrackedObjMarkerPrefab, minimapImage.transform).GetComponent<Image>();

        trackedObjImage.color = markerColor;

        //Add this obj and the new tracker to the lists
        trackedTransforms.Add(trackedTransform);
        trackedMinimapMarkerTransforms.Add(trackedObjImage.rectTransform);

        return true;
    }
}
