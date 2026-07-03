using UnityEngine;

public class SkyManager : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Composite composite;
    [SerializeField] private int sliceCount = 8;
    [SerializeField] private float scrollSpeedMin = 0.02f;
    [SerializeField] private float scrollSpeedMax = 0.08f;
    [SerializeField] private float fadeInDuration = 2f;

    private Texture2DArray sliceArray;
    private RenderTexture  stagingRT;

    private float[] scrollSpeeds;
    private float[] scrollOffsets;
    private float[] opacity;
    private float[] targetOpacity;
    private float[] effectiveScrollOffsets;

    void Awake()
    {
        scrollSpeeds  = new float[sliceCount];
        scrollOffsets = new float[sliceCount];
        opacity       = new float[sliceCount];
        targetOpacity = new float[sliceCount];
        effectiveScrollOffsets = new float[sliceCount];

        for (int i = 0; i < sliceCount; i++)
        {
            float speed      = Random.Range(scrollSpeedMin, scrollSpeedMax);
            scrollSpeeds[i]  = (i % 2 == 0) ? speed : -speed;
            scrollOffsets[i] = Random.Range(0f, 1f);
            opacity[i]       = 1f;
            targetOpacity[i] = 1f;
        }

        RenderSettings.skybox = skyboxMaterial;
        skyboxMaterial.SetInt("_SliceCount", sliceCount);
        skyboxMaterial.SetFloatArray("_ScrollSpeeds",  scrollSpeeds);
        skyboxMaterial.SetFloatArray("_ScrollOffsets", scrollOffsets);
        skyboxMaterial.SetFloatArray("_Opacity",       opacity);
    }

    void Update()
    {
        bool dirty = false;
        for (int i = 0; i < sliceCount; i++)
        {
            if (!Mathf.Approximately(opacity[i], targetOpacity[i]))
            {
                opacity[i] = Mathf.MoveTowards(opacity[i], targetOpacity[i], Time.deltaTime / fadeInDuration);
                dirty = true;
            }
        }
        if (dirty) skyboxMaterial.SetFloatArray("_Opacity", opacity);

        UpdateEmotionParameters();
    }

    void UpdateEmotionParameters()
    {
        if (Emotion.Instance == null) return;

        float content = Emotion.Instance.content * 0.01f;
        float unease  = Emotion.Instance.unease  * 0.01f;
        float awe     = Emotion.Instance.awe     * 0.01f;

        //awe: exaggerates the dome into a vast, swirling vault
        skyboxMaterial.SetFloat("_ElevScale", Mathf.Lerp(1f, 2.4f, awe));
        skyboxMaterial.SetFloat("_Swirl", awe * 1.5f);

        //content: warms the horizon fade
        Color fadeTint = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.65f), content);
        skyboxMaterial.SetColor("_FadeTint", fadeTint);
        skyboxMaterial.SetFloat("_FadeSpread", Mathf.Lerp(0.15f, 0.4f, content));

        //unease: jitters the slices
        for (int i = 0; i < sliceCount; i++)
        {
            float wobble = (Mathf.PerlinNoise(i * 13.7f, Time.time * 1.5f) - 0.5f) * unease * 0.6f;
            effectiveScrollOffsets[i] = scrollOffsets[i] + wobble;
        }
        skyboxMaterial.SetFloatArray("_ScrollOffsets", effectiveScrollOffsets);
    }

    public void OnRoomAdvance()
    {
        var available = composite.GetCapturedTextures();
        if (available == null || available.Length == 0) return;

        //derive format and size from the actual snapshot RT on first call
        if (sliceArray == null)
        {
            RenderTexture sample = available[0];
            int res = Mathf.Max(sample.width, sample.height);
            sliceArray = new Texture2DArray(res, res, sliceCount, TextureFormat.ARGB32, false);
            sliceArray.wrapMode = TextureWrapMode.Repeat;
            stagingRT = new RenderTexture(res, res, 0, sample.format);
            stagingRT.wrapMode = TextureWrapMode.Repeat;
            skyboxMaterial.SetTexture("_Slices", sliceArray);
        }

        for (int i = 0; i < sliceCount; i++)
        {
            RenderTexture src = available[Random.Range(0, available.Length)];
            Graphics.Blit(src, stagingRT);  
            Graphics.CopyTexture(stagingRT, 0, 0, sliceArray, i, 0);
            
            //alternating scroll
            float speed      = Random.Range(scrollSpeedMin, scrollSpeedMax);
            scrollSpeeds[i]  = (i % 2 == 0) ? speed : -speed;
            scrollOffsets[i] = Random.Range(0f, 1f);
            targetOpacity[i] = 1f;
        }

        skyboxMaterial.SetFloatArray("_ScrollSpeeds",  scrollSpeeds);
        skyboxMaterial.SetFloatArray("_ScrollOffsets", scrollOffsets);
    }

    void OnDestroy()
    {
        stagingRT?.Release();
        if (sliceArray != null) Destroy(sliceArray);
    }
}
