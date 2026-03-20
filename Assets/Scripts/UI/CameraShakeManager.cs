using UnityEngine;
using Unity.Cinemachine;

public class CameraShakeManager : MonoBehaviour
{
    public static CameraShakeManager Instance;

    [SerializeField] private float globalShakeForce = 1f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void Shake(CinemachineImpulseSource impulseSource)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(globalShakeForce);
    }

    public void ShakeForce(CinemachineImpulseSource impulseSource, float force)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(force);
    }
}
