using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerEnemyDetectionRadius : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float baseDetectionRadius = 4f;
    [SerializeField] private float flashlightDetectionBonus = 4f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float scanInterval = 0.05f;
    [SerializeField] private bool hideEnemiesOutsideRadiusOnStart = true;

    [Header("References")]
    [SerializeField] private FlashlightController flashlight;

    private readonly HashSet<EnemyAI> visibleEnemies = new HashSet<EnemyAI>();
    private readonly HashSet<EnemyAI> detectedThisScan = new HashSet<EnemyAI>();
    private readonly List<EnemyAI> enemiesToHide = new List<EnemyAI>();

    private float scanTimer;

    public float BaseDetectionRadius => baseDetectionRadius;
    public float FlashlightDetectionBonus => flashlightDetectionBonus;

    public float CurrentDetectionRadius
    {
        get
        {
            bool flashlightAddsRadius =
                flashlight != null &&
                flashlight.IsUnlocked &&
                flashlight.IsOn;

            return flashlightAddsRadius
                ? baseDetectionRadius + flashlightDetectionBonus
                : baseDetectionRadius;
        }
    }

    private void Awake()
    {
        if (flashlight == null)
            flashlight = GetComponentInChildren<FlashlightController>(true);
    }

    private void Start()
    {
        if (hideEnemiesOutsideRadiusOnStart)
            HideAllEnemiesOnScene();

        ScanEnemies();
    }

    private void Update()
    {
        scanTimer -= Time.deltaTime;

        if (scanTimer > 0f)
            return;

        scanTimer = scanInterval;
        ScanEnemies();
    }

    public void SetFlashlight(FlashlightController newFlashlight)
    {
        flashlight = newFlashlight;
        ScanEnemies();
    }

    public void ModifyBaseRadius(float value, SkillValueMode mode)
    {
        baseDetectionRadius = ApplyValue(baseDetectionRadius, value, mode, 0.1f);
        ScanEnemies();
    }

    public void ModifyFlashlightDetectionBonus(float value, SkillValueMode mode)
    {
        flashlightDetectionBonus = ApplyValue(flashlightDetectionBonus, value, mode, 0f);
        ScanEnemies();
    }

    private void ScanEnemies()
    {
        detectedThisScan.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            CurrentDetectionRadius,
            enemyLayers
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            EnemyAI enemy = hit.GetComponentInParent<EnemyAI>();

            if (enemy == null)
                continue;

            detectedThisScan.Add(enemy);

            if (!visibleEnemies.Contains(enemy))
                visibleEnemies.Add(enemy);

            enemy.SetLightVisible(true);
        }

        enemiesToHide.Clear();

        foreach (EnemyAI enemy in visibleEnemies)
        {
            if (enemy == null || !detectedThisScan.Contains(enemy))
                enemiesToHide.Add(enemy);
        }

        foreach (EnemyAI enemy in enemiesToHide)
        {
            visibleEnemies.Remove(enemy);

            if (enemy != null)
                enemy.SetLightVisible(false);
        }
    }

    private void HideAllEnemiesOnScene()
    {
        EnemyAI[] enemies = FindObjectsOfType<EnemyAI>(true);

        foreach (EnemyAI enemy in enemies)
        {
            if (enemy != null)
                enemy.SetLightVisible(false);
        }

        visibleEnemies.Clear();
    }

    private float ApplyValue(float current, float value, SkillValueMode mode, float minValue)
    {
        float result = current;

        switch (mode)
        {
            case SkillValueMode.Add:
                result = current + value;
                break;

            case SkillValueMode.Multiply:
                result = current * value;
                break;

            case SkillValueMode.Set:
                result = value;
                break;
        }

        return Mathf.Max(result, minValue);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, baseDetectionRadius);

        float maxRadius = baseDetectionRadius + flashlightDetectionBonus;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }
}