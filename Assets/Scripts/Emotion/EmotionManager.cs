using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class EmotionManager : MonoBehaviour
{
    // [SerializeField] private RenderTexture sourceRT;
    [SerializeField] private ComputeShader grammarShader;
    [SerializeField] private float grammarInterval = 0.5f; // seconds between grammar evaluations
    [SerializeField] private float surrealThreshold = 0.5f; // threshold for surrealism detection
    [SerializeField] private SurrealSpace surrealSpace;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct Grammar
    {
        public float symmetry;
        public float colorVariance;
        public float brightness;
        public float edgeDensity;
        public float isolation;
        public Vector3 padding;//surrealFrag;
    }
    private Grammar currGrammar;
    private GraphicsBuffer grammarBuffer;
    
    // Start is called before the first frame update
    void Start()
    {
        grammarBuffer = new GraphicsBuffer(
            GraphicsBuffer.Target.Structured,
            1, 
            Marshal.SizeOf<Grammar>()
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // returns raw grammar floats without updating emotion state
    public float[] ParseGrammarRaw(RenderTexture sourceRT)
    {
        int kernel = grammarShader.FindKernel("Analyze");
        RenderTexture small = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sourceRT, small);
        grammarShader.SetTexture(kernel, "inputTex", small);
        grammarShader.SetBuffer(kernel, "results", grammarBuffer);
        grammarShader.Dispatch(kernel, 1, 1, 1);
        RenderTexture.ReleaseTemporary(small);

        Grammar[] scores = new Grammar[1];
        grammarBuffer.GetData(scores);

        return new float[] {
            scores[0].symmetry,
            scores[0].colorVariance,
            scores[0].brightness,
            scores[0].edgeDensity,
            scores[0].isolation
        };
    }

    public void ParseGrammar(RenderTexture sourceRT)
    {
        int kernel = grammarShader.FindKernel("Analyze");
        RenderTexture small = RenderTexture.GetTemporary(64, 64, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(sourceRT, small);

        grammarShader.SetTexture(kernel, "inputTex", small);
        grammarShader.SetBuffer(kernel, "results", grammarBuffer); // 6 floats
        grammarShader.Dispatch(kernel, 1, 1, 1); // single threadgroup, 64x64 fits in 8x8 threads

        RenderTexture.ReleaseTemporary(small);
        
        Readback();
    }

    private void Readback()
    {
        Grammar[] scores = new Grammar[1];
        grammarBuffer.GetData(scores);

        //float warmth        = scores.warmth;

        //map to emotion weights
        float content = scores[0].symmetry * scores[0].colorVariance;// * warmth;           // needs all three
        float unease = scores[0].isolation * (1f - scores[0].colorVariance);// * (1f - warmth);
        float awe = scores[0].edgeDensity;// * (1f - scores[0].isolation);              // complex but not empty
        float intensity = Mathf.Max(content, unease, awe);
        
        //surreal space detection
        if (scores[0].edgeDensity > surrealThreshold)
        {
            surrealSpace.AdvanceSpace();
        }
        
        Emotion.Instance.UpdateState(content*0.1f, unease*0.1f, awe*0.1f, intensity*0.1f);
    }
    
    // IEnumerator GrammarLoop() {
    //     while (true) {
    //         yield return new WaitForSeconds(grammarInterval);
    //     
    //         DispatchGrammarPass();
    //     
    //         // only readback, not per-frame
    //         float[] scores = new float[6];
    //         grammarBuffer.GetData(scores);
    //         UpdateVolumeParameters(scores);
    //     }
    // }
    
    void OnDestroy()
    {
        grammarBuffer.Release();
        grammarBuffer.Dispose();
    }
}
