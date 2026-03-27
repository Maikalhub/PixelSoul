using System;
using System.Reflection;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerVisualEffects : MonoBehaviour
{
    [Header("Particles")]
    [SerializeField] private ParticleSystem smokeFX;

    [Header("Trails")]
    [SerializeField] private TrailRenderer dashTrail;
    [SerializeField] private GhostTrail ghostTrail;

    [Header("Camera / Ripple")]
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private RippleEffect rippleEffect;
    [SerializeField] private Material rippleMaterial;
    [SerializeField] private float rippleStrength = 1f;

    private void Awake()
    {
        if (dashTrail == null)
            dashTrail = GetComponent<TrailRenderer>();

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();

        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    public void PlaySpawnFX()
    {
        TriggerRipple();
    }

    public void PlayJumpFX()
    {
        PlaySmoke();
        GenerateImpulse();
    }

    public void PlayLandFX()
    {
        PlaySmoke();
        GenerateImpulse();
        TriggerRipple();
    }

    public void PlayDashFX()
    {
        GenerateImpulse();
        StartGhostTrail();
    }

    public void PlayAttackFX()
    {
        GenerateImpulse();
    }

    public void PlayThrowFX()
    {
        GenerateImpulse();
    }

    public void PlayFlipFX()
    {
        PlaySmoke();
    }

    public void PlayDeathFX()
    {
        PlaySmoke();
        GenerateImpulse();
        TriggerRipple();
        StartGhostTrail();
    }

    public void SetDashTrail(bool state)
    {
        if (dashTrail != null)
            dashTrail.emitting = state;
    }

    public void TriggerRipple()
    {
        if (rippleEffect == null)
            return;

        if (TryInvoke(rippleEffect, "TriggerRipple", rippleMaterial, rippleStrength)) return;
        if (TryInvoke(rippleEffect, "TriggerRipple", rippleMaterial)) return;
        if (TryInvoke(rippleEffect, "TriggerRipple")) return;

        if (TryInvoke(rippleEffect, "GenerateRipple", rippleMaterial, rippleStrength)) return;
        if (TryInvoke(rippleEffect, "GenerateRipple", rippleMaterial)) return;
        if (TryInvoke(rippleEffect, "GenerateRipple")) return;

        TryInvoke(rippleEffect, "PlayRipple");
    }

    private void StartGhostTrail()
    {
        if (ghostTrail == null)
            return;

        if (TryInvoke(ghostTrail, "ActivateTrail")) return;
        if (TryInvoke(ghostTrail, "StartTrail")) return;
        if (TryInvoke(ghostTrail, "PlayTrail")) return;
        TryInvoke(ghostTrail, "Play");
    }

    private void PlaySmoke()
    {
        if (smokeFX != null)
            smokeFX.Play();
    }

    private void GenerateImpulse()
    {
        if (impulseSource != null)
            impulseSource.GenerateImpulse();
    }

    private bool TryInvoke(object target, string methodName, params object[] args)
    {
        if (target == null)
            return false;

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = target.GetType().GetMethods(flags);

        foreach (MethodInfo method in methods)
        {
            if (method.Name != methodName)
                continue;

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != args.Length)
                continue;

            bool compatible = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (args[i] == null)
                    continue;

                Type expected = parameters[i].ParameterType;
                Type actual = args[i].GetType();

                if (!expected.IsAssignableFrom(actual))
                {
                    compatible = false;
                    break;
                }
            }

            if (!compatible)
                continue;

            try
            {
                method.Invoke(target, args);
                return true;
            }
            catch
            {
                // если сигнатура метода отличается, просто пробуем другой вариант
            }
        }

        return false;
    }
}