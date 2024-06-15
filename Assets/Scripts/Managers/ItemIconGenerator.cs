using UnityEngine;

public class ItemIconGenerator : MonoBehaviour
{
    [Header("Refs")]
    public GameObject[] ItemsToRender;
    public Camera captureCam;

    [Header("Settings")]
    public const int RESWIDTH = 256;
    public const int RESHEIGHT = 256;

    public string savePath;

    private void Start()
    {
        CaptureItems();
    }

    public void CaptureItems()
    {
        //For simplicity sake, I'm using this objects transform as the spawn point
        //and assuming that the camera is set up properly

        foreach (GameObject obj in ItemsToRender)
        {
            //Instantiate item
            GameObject currentInstance = Instantiate(obj, transform.position, transform.rotation);

            //Capture (pass in the item name)
            SaveCapture(currentInstance.GetComponent<LootItem>().GetItemName());

            //Delete item immediately so it won;t be in other renders
            DestroyImmediate(currentInstance);
        }

        Debug.Log("Capture completed with result SUCESS");
    }

    public void SaveCapture(string itemName)
    {

        RenderTexture rt = new RenderTexture(RESWIDTH, RESHEIGHT, 24);

        //Set up the camera to render to this rt
        captureCam.targetTexture = rt;

        Texture2D screenshot = new Texture2D(RESWIDTH, RESHEIGHT);

        //Run the actual render function to capture render in texture
        captureCam.Render();

        //Ensure the current active render texture is rt
        RenderTexture.active = rt;

        screenshot.ReadPixels(new Rect(0, 0, RESWIDTH, RESHEIGHT), 0, 0);

        captureCam.targetTexture = null;
        RenderTexture.active = null; //Apparently this avoids an error

        //Destroy the render texture to conserve memory
        Destroy(rt);

        //Now feed this into a byte array that will be stored in the files
        byte[] bytes = screenshot.EncodeToPNG();

        string filename = ScreenShotName(RESWIDTH, RESHEIGHT, itemName);//savePath + itemName + ".png";

        //Actually write to a file
        System.IO.File.WriteAllBytes(filename, bytes);

        Debug.Log(string.Format("Took screenshot to: {0}", filename));
    }

    public static string ScreenShotName(int width, int height, string itemName)
    {
        return string.Format("{0}/Art/ItemIcons/{1}_Item_Icon_{2}x{3}.png",
                             Application.dataPath,
                             itemName,
                             width, height);
    }
}
