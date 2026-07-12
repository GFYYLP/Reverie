using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RoomSpace : MonoBehaviour
{
    [Header("Compute")]
    public ComputeShader sdfShader;
    public ComputeShader marchingShader;

    const int GridSize = 32;
    const float IsoLevel = 0f;

    ComputeBuffer densityBuffer;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;

    struct Triangle {
        public Vector3 a, b, c;
    }

    void Awake() {
        int pointCount = GridSize * GridSize * GridSize;
        int maxTris    = pointCount * 5;

        densityBuffer  = new ComputeBuffer(pointCount, sizeof(float) * 4);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        triangleBuffer = new ComputeBuffer(maxTris, sizeof(float) * 9, ComputeBufferType.Append);
    }

    public void Generate(RoomConfig config) {
        RunSDF(config);
        RunMarchingCubes();
        BuildMesh();
    }

    public void Clear() {
        GetComponent<MeshFilter>().mesh = null;
    }

    void RunSDF(RoomConfig config) {
        int kernel = sdfShader.FindKernel("CSMain");

        sdfShader.SetBuffer(kernel, "densityValues", densityBuffer);
        sdfShader.SetInt("gridSize", GridSize);
        
        // //grammar parameters 
        sdfShader.SetFloat("spikeScore",     config.spikeScore);
        sdfShader.SetFloat("symmetryScore",  config.symmetryScore);
        sdfShader.SetFloat("verticalityScore", config.verticalityScore);
        sdfShader.SetFloat("seed",             config.seed);
        sdfShader.SetFloat("roomScale",      config.roomScale);
        sdfShader.SetFloat( "densityScore",      config.densityScore);
        sdfShader.SetFloat( "fragmentationScore",config.fragmentationScore);
        sdfShader.SetFloat( "radialScore",       config.radialScore);
        // sdfShader.SetFloat("cohesionScore", config.cohesionScore);
        // sdfShader.SetFloat("uneaseGate", config.uneaseGate);
        // sdfShader.SetFloat("dominanceScore", config.dominanceScore);
        // sdfShader.SetFloat("rhythmScore", config.rhythmScore);
        
        int threads = Mathf.CeilToInt(GridSize / 8f);
        sdfShader.Dispatch(kernel, threads, threads, threads);
    }

    void RunMarchingCubes() {
        int kernel = marchingShader.FindKernel("March");

        triangleBuffer.SetCounterValue(0);

        // marchingShader.SetBuffer(kernel, "densityValues", densityBuffer);
        // marchingShader.SetBuffer(kernel, "triangles",     triangleBuffer);
        // marchingShader.SetInt(   "gridSize",              GridSize);
        // marchingShader.SetFloat( "isoLevel",              IsoLevel);
        
        marchingShader.SetBuffer (kernel, "points", densityBuffer);
        marchingShader.SetBuffer (kernel, "triangles", triangleBuffer);
        marchingShader.SetInt ("numPointsPerAxis", GridSize);
        marchingShader.SetFloat ("isoLevel", IsoLevel);

        int threads = Mathf.CeilToInt(GridSize / 8f);
        marchingShader.Dispatch(kernel, threads, threads, threads);
    }

    void BuildMesh() {
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] countArr = new int[1];
        triCountBuffer.GetData(countArr);
        int count = countArr[0];

        Triangle[] tris = new Triangle[count];
        triangleBuffer.GetData(tris, 0, 0, count);

        Vector3[] vertices = new Vector3[count * 3];
        int[]     indices  = new int[count * 3];

        for (int i = 0; i < count; i++) {
            vertices[i * 3 + 0] = tris[i].a;
            vertices[i * 3 + 1] = tris[i].c; // swapped
            vertices[i * 3 + 2] = tris[i].b; // swapped
            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices    = vertices;
        mesh.triangles   = indices;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        
        var collider = GetComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }

    void OnDestroy() {
        densityBuffer ?.Release();
        triCountBuffer?.Release();
        triangleBuffer?.Release();
    }
}