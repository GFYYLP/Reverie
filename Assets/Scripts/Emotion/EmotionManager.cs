using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class EmotionManager : MonoBehaviour
{
    // [SerializeField] private RenderTexture sourceRT;
    [SerializeField] private ComputeShader grammarShader;
    [SerializeField] private float grammarInterval = 0.5f; // seconds between grammar evaluations

    [StructLayout(LayoutKind.Sequential)]
    private struct Grammar
    {
        private float symmetry;
        private float colorVariance;
        private float brightness;
        private float edgeDensity;
        private float isolation;
        private float warmth;
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

    public void ParseGrammar(RenderTexture sourceRT)
    {
        int kernel = grammarShader.FindKernel("CSMain");
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
        float[] scores = new float[6];
        grammarBuffer.GetData(scores);

        float symmetry      = scores[0];
        float colorVariance = scores[1];
        float brightness    = scores[2];
        float edgeDensity   = scores[3];
        float isolation     = scores[4];
        //float warmth        = scores[5];

        // map to emotion weights
        // float content  = symmetry * colorVariance * warmth;           // needs all three
        // float unease   = isolation * (1f - colorVariance) * (1f - warmth);
        // float awe      = edgeDensity * (1f - isolation);              // complex but not empty
        // float intensity = Mathf.Max(content, unease, awe);
        //
        // Emotion.Instance.UpdateState(content, unease, awe, intensity);
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
}
