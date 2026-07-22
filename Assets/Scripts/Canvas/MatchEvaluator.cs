using System;
using System.Collections;
using UnityEngine;

public class MatchEvaluator : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private int gridDivisions  = 3;
    [SerializeField] private int flatResolution = 512;

    [Header("Metric Weights")]
    [SerializeField] private float symmetryWeight      = 0.5f;
    [SerializeField] private float colorVarianceWeight = 0.5f;
    [SerializeField] private float brightnessWeight    = 0.3f;
    [SerializeField] private float edgeDensityWeight   = 1.0f;
    [SerializeField] private float isolationWeight     = 1.0f;
    [SerializeField] private float histogramWeight     = 0.5f;
    

    public float LastScore { get; private set; }
    public bool  LastResult { get; private set; }

    [SerializeField] private EmotionManager emotionManager;
    private float[][] referenceGrammars;
    private float[]   referenceHistogram;

    [SerializeField] private RectTransform debugAlbumRect;
    private Rect debugRect;

    // void OnGUI()
    // {
    //     if (debugAlbumRect == null) return;
    //     Vector3[] c = new Vector3[4];
    //     debugAlbumRect.GetWorldCorners(c);
    //     // GUI y is flipped
    //     float x = c[0].x;
    //     float y = Screen.height - c[2].y;
    //     float w = c[2].x - c[0].x;
    //     float h = c[2].y - c[0].y;
    //     GUI.color = Color.red;
    //     GUI.Box(new Rect(x, y, w, h), "album");
    // }

    // call when a new level loads, reference can be any Texture (Texture2D or RT)
    public void UpdateReference(Texture reference)
    {
        RenderTexture rt = RenderTexture.GetTemporary(flatResolution, flatResolution, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(reference, rt);
        referenceGrammars  = ParseGrid(rt);
        referenceHistogram = ComputeHistogram(rt);
        RenderTexture.ReleaseTemporary(rt);
    }

    // call this when the player wants to submit their album for evaluation
    public IEnumerator Evaluate(RectTransform albumRect, Action<float, bool> onDone)
    {
        yield return new WaitForEndOfFrame(); //ensures ReadPixels() from FlattenAlbum() is called after
                                              //the frame has finished rendering

        RenderTexture albumRT = FlattenAlbum(albumRect);
        if (albumRT == null) { onDone(0f, false); yield break; }

        float[][] albumGrammars  = ParseGrid(albumRT);
        float[]   albumHistogram = ComputeHistogram(albumRT);
        albumRT.Release();

        // grammar distance averaged across grid cells
        int cells = gridDivisions * gridDivisions;
        float grammarDist = 0f;
        for (int i = 0; i < cells; i++)
            grammarDist += GrammarDistance(referenceGrammars[i], albumGrammars[i]);
        grammarDist /= cells;

        float histDist = HistogramDistance(referenceHistogram, albumHistogram);

        float totalDist = (grammarDist + histogramWeight * histDist) / (1f + histogramWeight);
        LastScore  = 1f - Mathf.Clamp01(totalDist);
        LastResult = LastScore >= 0.75f;

        onDone(LastScore, LastResult);
    }

    private RenderTexture FlattenAlbum(RectTransform albumRect)
    {
        Vector3[] corners = new Vector3[4];
        albumRect.GetWorldCorners(corners);


        // corners: 0=BL 1=TL 2=TR 3=BR
        float x = corners[0].x;
        float y = corners[0].y;
        float w = corners[2].x - corners[0].x;
        float h = corners[2].y - corners[0].y;

        // clamp to screen bounds
        x = Mathf.Clamp(x, 0, Screen.width);
        y = Mathf.Clamp(y, 0, Screen.height);
        w = Mathf.Clamp(w, 1, Screen.width  - x);
        h = Mathf.Clamp(h, 1, Screen.height - y);

        Debug.Log($"FlattenAlbum: screen={Screen.width}x{Screen.height} corners BL={corners[0]} TR={corners[2]} rect=({x},{y},{w},{h})");
        Texture2D snap = new Texture2D((int)w, (int)h, TextureFormat.RGB24, false);
        snap.ReadPixels(new Rect(x, y, w, h), 0, 0);
        snap.Apply();

        RenderTexture rt = new RenderTexture(flatResolution, flatResolution, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(snap, rt);
        Destroy(snap);
        return rt;
    }
    

    private float[][] ParseGrid(RenderTexture rt)
    {
        //divides album's composite into multiple subdivisions for more precise grammar parsing/image matching
        int cells = gridDivisions * gridDivisions;
        float[][] grammars = new float[cells][];
        float step = 1f / gridDivisions;

        for (int i = 0; i < cells; i++)
        {
            int cx = i % gridDivisions;
            int cy = i / gridDivisions;

            int cellW = rt.width  / gridDivisions;
            int cellH = rt.height / gridDivisions;
            RenderTexture cell = RenderTexture.GetTemporary(cellW, cellH, 0, RenderTextureFormat.ARGB32);

            Graphics.Blit(rt, cell,
                new Vector2(step, step),
                new Vector2(cx * step, cy * step));

            grammars[i] = emotionManager.ParseGrammarRaw(cell);
            RenderTexture.ReleaseTemporary(cell);
        }
        return grammars;
    }
    
    //computes a color histogram of a RenderTexture
    //Instead of comparing images pixel-by-pixel, it summarizes the image by asking:
    //"How much of the image is made up of dark reds? Bright reds? Dark greens? Bright blues?..."
    private float[] ComputeHistogram(RenderTexture rt)
    {
        const int buckets = 8;
        float[] hist = new float[buckets * 3];

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        Color[] pixels = tex.GetPixels();
        Destroy(tex);

        foreach (Color c in pixels)
        {
            hist[Mathf.Min((int)(c.r * buckets), buckets - 1)]++;
            hist[buckets     + Mathf.Min((int)(c.g * buckets), buckets - 1)]++;
            hist[buckets * 2 + Mathf.Min((int)(c.b * buckets), buckets - 1)]++;
        }

        float total = pixels.Length;
        for (int i = 0; i < hist.Length; i++) hist[i] /= total;
        return hist;
    }
    
    //grammar-weighted histogram
    private float GrammarDistance(float[] a, float[] b)
    {
        float[] weights = { symmetryWeight, colorVarianceWeight, brightnessWeight, edgeDensityWeight, isolationWeight };
        float weightSum = 0f, dist = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dist      += weights[i] * Mathf.Abs(a[i] - b[i]);
            weightSum += weights[i];
        }
        return weightSum > 0 ? dist / weightSum : 0f;
    }

    private float HistogramDistance(float[] a, float[] b)
    {
        float dist = 0f;
        for (int i = 0; i < a.Length; i++)
            dist += Mathf.Abs(a[i] - b[i]);
        return dist / a.Length;
    }
}
