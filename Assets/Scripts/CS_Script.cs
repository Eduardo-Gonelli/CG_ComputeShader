using UnityEngine;

public class CS_Script : MonoBehaviour
{
    Vector2 mousePos;  // posição do mouse na tela

    // estrutura para armazenar os dados da partícula
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float lifetime;
    }
    // separa 7 floats para cada partícula (3 para posição, 3 para velocidade e 1 para tempo de vida)
    const int SIZE_PARTICLE = 7 * sizeof(float); 

    public int particleCount = 1000000; // sim, são 1 milhão de partículas
    public Material particleMaterial;   // material associado ao shader
    public ComputeShader cs;            // associado ao arquivo compute shader

    int kernelID;  // id do kernel
    ComputeBuffer particleBuffer;  // buffer para armazenar as partículas

    int groupSizeX; // número de grupos em X

    RenderParams rp;  // parâmetros de renderização

    // inicializa cada partícula
    void Start()
    {
        Particle[] particles = new Particle[particleCount];

        for(int i = 0; i < particleCount; i++)
        {
            // cria uma posição aleatória para cada partícula
            Vector3 p = new Vector3();
            p.x = Random.value * 2 - 1; // valor entre -1 e 1
            p.y = Random.value * 2 - 1;
            p.z = Random.value * 2 - 1;
            p.Normalize(); // normaliza o vetor
            p *= Random.value * 5; // multiplica por um valor entre 0 e 5

            // seta a posição da partícula
            particles[i].position = p;

            // zera a velocidade da partícula
            particles[i].velocity = Vector3.zero;

            // seta o tempo de vida da partícula
            particles[i].lifetime = Random.value * 5 + 1.0f; // valor entre 1 e 6
        }

        // cria o buffer para armazenar as partículas
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);
        // copia os dados para o buffer
        particleBuffer.SetData(particles);
        // localiza o id do kernel
        kernelID = cs.FindKernel("CSParticle");

        uint threadsX;
        // localiza o número de threads em X
        cs.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        // número de grupos em X
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // vincula o compute buffer ao shader e o compute shader
        cs.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        particleMaterial.SetBuffer("particleBuffer", particleBuffer);

        // cria os parâmetros de renderização
        rp = new RenderParams(particleMaterial);
        // cria um bounds de 10km em cada direção
        rp.worldBounds = new Bounds(Vector3.zero, 10000* Vector3.one); 
    }

    void OnDestroy()
    {
        // libera o buffer
        if (particleBuffer != null)
        {
            particleBuffer.Release();            
        }
    }

    // Update is called once per frame
    void Update()
    {
        float[] mousePosition2D = { mousePos.x, mousePos.y };
        
        // Envia os dados ao compute shader
        cs.SetFloat("deltaTime", Time.deltaTime);
        cs.SetFloats("mousePosition", mousePosition2D);

        // Atualiza as partículas
        cs.Dispatch(kernelID, groupSizeX, 1, 1);
        // renderiza as partículas
        Graphics.RenderPrimitives(rp, MeshTopology.Points, 1, particleCount);
    }

    void OnGUI()
    {
        Vector3 point = new Vector3();
        Camera cam = Camera.main;
        Event ev = Event.current;
        Vector2 mPos = new Vector2();
        
        mPos.x = ev.mousePosition.x;        
        mPos.y = cam.pixelHeight - ev.mousePosition.y;
        // converte a posição do mouse para o espaço do mundo
        point = cam.ScreenToWorldPoint(new Vector3(mPos.x, mPos.y, cam.nearClipPlane));
        // atualiza a posição do mouse
        mousePos.x = point.x;
        mousePos.y = point.y;
    }
}
