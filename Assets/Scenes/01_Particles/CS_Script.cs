using UnityEngine;

public class CS_Script : MonoBehaviour
{
    Vector2 cursorPos;  // posicaoo do mouse na tela

    // estrutura para armazenar os dados da particula
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float lifetime;
    }
    // separa 7 floats para cada particula (3 para posicao, 3 para velocidade e 1 para tempo de vida)
    const int SIZE_PARTICLE = 7 * sizeof(float); 

    public int particleCount = 1000000; // sim, e 1 milhao de particulas
    public Material particleMaterial;   // material associado ao shader
    public ComputeShader cs;            // associado ao arquivo compute shader

    int kernelID;  // id do kernel
    ComputeBuffer particleBuffer;  // buffer para armazenar as particulas

    int groupSizeX; // numero de grupos em X

    RenderParams rp;  // parametros de renderizacao

    // inicializa cada particula
    void Start()
    {
        Particle[] particles = new Particle[particleCount];

        for(int i = 0; i < particleCount; i++)
        {
            // cria uma posicao aleatoria para cada particula
            Vector3 p = new Vector3();
            p.x = Random.value * 2 - 1; // valor entre -1 e 1
            p.y = Random.value * 2 - 1;
            p.z = Random.value * 2 - 1;
            p.Normalize(); // normaliza o vetor
            p *= Random.value * 5; // multiplica por um valor entre 0 e 5

            // seta a posicao da particula
            particles[i].position = p;

            // zera a velocidade da particula
            particles[i].velocity = Vector3.zero;

            // seta o tempo de vida da particula
            particles[i].lifetime = Random.value * 5.0f + 1.0f; // valor entre 1 e 6
        }

        // cria o buffer para armazenar as particulas
        // o buffer e criado com o tamanho da estrutura Particle e o numero de particulas
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);
        // copia os dados para o buffer
        particleBuffer.SetData(particles);
        // localiza o id do kernel
        kernelID = cs.FindKernel("CSParticle");

        uint threadsX;
        // localiza o numero de threads em X
        cs.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        // numero de grupos em X
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // vincula o compute buffer ao shader e ao compute shader
        cs.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        particleMaterial.SetBuffer("particleBuffer", particleBuffer);

        // cria os parametros de renderizacao
        rp = new RenderParams(particleMaterial);
        // cria um bounds de 10km em cada direcao
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
        float[] mousePosition2D = { cursorPos.x, cursorPos.y };       
        // Envia os dados ao compute shader
        cs.SetFloat("deltaTime", Time.deltaTime);
        cs.SetFloats("mousePosition", mousePosition2D);
        // Atualiza as particulas
        cs.Dispatch(kernelID, groupSizeX, 1, 1);
        // renderiza as particulas
        Graphics.RenderPrimitives(rp, MeshTopology.Points, 1, particleCount);
    }

    void OnGUI()
    {
        Vector3 point = new Vector3();
        Camera cam = Camera.main; // captura a camera principal
        Event ev = Event.current; // captura o evento atual
        Vector2 mousePos = new Vector2();  
        mousePos.x = ev.mousePosition.x; // captura a posicao do mouse em x   
        // inverte a posicao do mouse em y, o sistema de coordenadas do Unity e diferente do OpenGL
        // OpenGL tem a origem no canto inferior esquerdo, enquanto na Unity é no canto superior esquerdo 
        mousePos.y = cam.pixelHeight - ev.mousePosition.y;
        // converte a posicao do mouse para o espaco do mundo
        point = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, cam.nearClipPlane));
        // atualiza a posicao do mouse em x e y
        cursorPos.x = point.x;
        cursorPos.y = point.y;
    }
}
