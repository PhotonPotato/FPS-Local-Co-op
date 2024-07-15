using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    public float maxIntensity = .5f;
    public float minIntensity = .2f;

    private float flickerFrequency = 40;
    public float maxFlickerFrequency = 40;
    public float minFlickerFrequency = 40;

    public Light light;
    //public Light[] lights;

    private float timeOfLastFreqChange = -3;

    public void Start()
    {
        light = GetComponent<Light>();
    }

    public void Update()
    {
        if (Time.time - timeOfLastFreqChange > Random.Range(.5f, 2f)) 
        { 
            flickerFrequency = Random.Range(minFlickerFrequency, maxFlickerFrequency);
            timeOfLastFreqChange = Time.time;
        }

        light.intensity = Mathf.Lerp(minIntensity, maxIntensity,
            (Mathf.Sin(Time.time * flickerFrequency) *.5f + .5f) * Random.Range(.85f, 1f));
    }
}
