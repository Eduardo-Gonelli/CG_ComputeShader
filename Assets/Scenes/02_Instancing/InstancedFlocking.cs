using UnityEngine;

public class InstancedFlocking : MonoBehaviour
{
    public struct Boid
    {
        public Vector3 position;
        public Vector3 direction;
        public float noise_offset;

        public Boid(Vector3 pos, Vector3 dir, float offset)
        {
            position = pos;
            direction = dir;
            noise_offset = offset; 
        }
    }

    // Variaveis publicas
    public ComputeShader cs;
    public Mesh boidMesh;
    public Material boidMaterial;
    public Transform target;
    public int boidsCount;
    public float rotationSpeed = 1.0f;
    public float boidSpeed = 1.0f;
    public float nbDistance = 1.0f;
    public float boidSpeedVariation = 1.0f;
    public float spawnRadius;

    // Variaveis privadas
    Boid[] boids;
    ComputeBuffer boidsBuffer;
    RenderParams rp;
    GraphicsBuffer argsBuffer;
    int kernelHandle;
    int groupSizeX;
    int numOfBoids;

    void Start()
    {
        kernelHandle = cs.FindKernel("CSMain");

        uint x;
        cs.GetKernelThreadGroupSizes(kernelHandle, out x, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)boidsCount / (float)x);
        numOfBoids = groupSizeX * (int)x;

        InitBoids();
        InitShader();

        rp = new RenderParams(boidMaterial);
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000);
    }

    void InitBoids()
    {
        boids = new Boid[numOfBoids];
        for(int i = 0; i < numOfBoids; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            Quaternion rot = Quaternion.Slerp(transform.rotation, Random.rotation, 0.3f);
            float offset = Random.value * 1000.0f;
            boids[i] = new Boid(pos, rot.eulerAngles, offset);
        }
    }

    void InitShader()
    {
        boidsBuffer = new ComputeBuffer(numOfBoids, 7 * sizeof(float));
        boidsBuffer.SetData(boids);

        argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        GraphicsBuffer.IndirectDrawIndexedArgs[] data = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        data[0].indexCountPerInstance = boidMesh.GetIndexCount(0);
        data[0].instanceCount = (uint)numOfBoids;
        argsBuffer.SetData(data);

        cs.SetFloat("rotationSpeed", rotationSpeed);
        cs.SetFloat("boidSpeed", boidSpeed);
        cs.SetFloat("boidSpeedVariation", boidSpeedVariation);        
        cs.SetFloat("nbDistance", nbDistance);
        cs.SetInt("boidsCount", numOfBoids);        
        cs.SetVector("flockPosition", target.transform.position);

        cs.SetBuffer(kernelHandle, "boidsBuffer", boidsBuffer);
        boidMaterial.SetBuffer("boidsBuffer", boidsBuffer);
    }

    void Update()
    {
        cs.SetFloat("time", Time.time);
        cs.SetFloat("deltaTime", Time.deltaTime);
        cs.SetVector("flockPosition", target.transform.position);
        cs.Dispatch(kernelHandle, groupSizeX, 1, 1);

        Graphics.RenderMeshIndirect(rp, boidMesh, argsBuffer);
    }

    void OnDestroy()
    {
        if(boidsBuffer != null)
        {
            boidsBuffer.Dispose();
        }

        if(argsBuffer != null)
        {
            argsBuffer.Dispose();
        }
    }
}
