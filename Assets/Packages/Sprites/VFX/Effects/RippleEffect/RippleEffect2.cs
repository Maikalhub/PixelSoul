using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class RippleEffect2 : MonoBehaviour
{
    public AnimationCurve waveform = new AnimationCurve(
        new Keyframe(0.00f, 0.50f),
        new Keyframe(0.05f, 1.00f),
        new Keyframe(0.15f, 0.10f),
        new Keyframe(0.25f, 0.80f),
        new Keyframe(0.35f, 0.30f),
        new Keyframe(0.45f, 0.60f),
        new Keyframe(0.55f, 0.40f),
        new Keyframe(0.65f, 0.55f),
        new Keyframe(0.75f, 0.46f),
        new Keyframe(0.85f, 0.52f),
        new Keyframe(0.99f, 0.50f)
    );

    [Range(0.01f, 1.0f)] public float refractionStrength = 0.5f;
    [Range(0.01f, 1.0f)] public float reflectionStrength = 0.7f;
    [Range(1.0f, 5.0f)] public float waveSpeed = 1.25f;
    [Range(0.0f, 2.0f)] public float dropInterval = 0.5f;
    public Color reflectionColor = Color.gray;

    [SerializeField] Shader shader; // твой URP шейдер RippleEffect2
    private Material material;
    private Texture2D gradTexture;

    class Droplet
    {
        Vector2 position;
        float time;

        public Droplet() { time = 1000; }

        public void Reset(Vector2 pos)
        {
            position = pos;
            time = 0;
        }

        public void Update()
        {
            time += Time.deltaTime * 2;
        }

        public Vector4 MakeShaderParameter(float aspect)
        {
            return new Vector4(position.x * aspect, position.y, time, 0);
        }
    }

    private Droplet[] droplets;
    private float timer;
    private int dropCount;
    private CommandBuffer commandBuffer;

    void Awake()
    {
        droplets = new Droplet[3];
        for (int i = 0; i < droplets.Length; i++) droplets[i] = new Droplet();

        gradTexture = new Texture2D(2048, 1, TextureFormat.Alpha8, false);
        gradTexture.wrapMode = TextureWrapMode.Clamp;
        gradTexture.filterMode = FilterMode.Bilinear;
        for (int i = 0; i < gradTexture.width; i++)
        {
            float x = (float)i / gradTexture.width;
            float a = waveform.Evaluate(x);
            gradTexture.SetPixel(i, 0, new Color(a, a, a, a));
        }
        gradTexture.Apply();

        material = new Material(shader);
        material.hideFlags = HideFlags.DontSave;
        material.SetTexture("_GradTex", gradTexture);

        SetupCommandBuffer();
        UpdateShaderParameters();
    }

    void SetupCommandBuffer()
    {
        commandBuffer = new CommandBuffer { name = "RippleEffect" };
        int tempID = Shader.PropertyToID("_TempRT");
        commandBuffer.GetTemporaryRT(tempID, -1, -1, 0, FilterMode.Bilinear);
        commandBuffer.Blit(BuiltinRenderTextureType.CameraTarget, tempID);
        commandBuffer.Blit(tempID, BuiltinRenderTextureType.CameraTarget, material);
        commandBuffer.ReleaseTemporaryRT(tempID);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.AfterForwardOpaque, commandBuffer);
    }

    void Update()
    {
        if (dropInterval > 0)
        {
            timer += Time.deltaTime;
            while (timer > dropInterval)
            {
                timer -= dropInterval;
            }
        }

        foreach (var d in droplets) d.Update();
        UpdateShaderParameters();
    }

    void UpdateShaderParameters()
    {
        float aspect = GetComponent<Camera>().aspect;

        material.SetVector("_Drop1", droplets[0].MakeShaderParameter(aspect));
        material.SetVector("_Drop2", droplets[1].MakeShaderParameter(aspect));
        material.SetVector("_Drop3", droplets[2].MakeShaderParameter(aspect));

        material.SetColor("_Reflection", reflectionColor);
        material.SetVector("_Params1", new Vector4(aspect, 1, 1 / waveSpeed, 0));
        material.SetVector("_Params2", new Vector4(1, 1 / aspect, refractionStrength, reflectionStrength));
    }

    public void Emit(Vector2 pos)
    {
        droplets[dropCount++ % droplets.Length].Reset(pos);
    }
}
