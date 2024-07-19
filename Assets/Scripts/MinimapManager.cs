using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class MinimapManager : MonoBehaviour
{
    [Header("Refs")]
    public Camera minimapCaptureCam;

    public Image minimapImage;

    public Transform[] playerTransforms;
    public Transform[] playerMinimapMarkerTransforms;

    public Sprite minimapSprite;

    public int RESWIDTH = 1920;
    public int RESHEIGHT = 1080;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount == 11) Generator.generator.ShowAllRooms(true);
        if (Time.frameCount == 12)
        { 
            GenerateMinimapImage();

            minimapImage.sprite = minimapSprite;

            Generator.generator.ShowAllRooms(true);

            //playerTransforms = Generator.generator.activePlayers.ToArray();
        }


        for (int i = 0; i < playerTransforms.Length; i++)
        {
            Debug.Log(WorldToMinimapCoord(playerTransforms[i].position));
            playerMinimapMarkerTransforms[i].position = WorldToMinimapCoord(playerTransforms[i].position);
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
        return minimapCaptureCam.WorldToScreenPoint(pos);
    }
}
