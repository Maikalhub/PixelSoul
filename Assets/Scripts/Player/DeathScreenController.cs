using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [Header("Fade Image")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float startDelay = 0.4f;
    [SerializeField] private float screenFadeDuration = 1f;
    [SerializeField] private float screenTargetAlpha = 1f;

    [Header("UI Fade")]
    [SerializeField] private CanvasGroup deathTextGroup;
    [SerializeField] private CanvasGroup restartButtonGroup;
    [SerializeField] private CanvasGroup mainMenuButtonGroup;
    [SerializeField] private float textFadeDuration = 0.4f;
    [SerializeField] private float buttonFadeDuration = 0.35f;
    [SerializeField] private float betweenElementsDelay = 0.12f;

    [Header("Scene Names")]
    [SerializeField] private string menuSceneName = "Menu";

    private bool isShowing;

    // Ключи для сохранения состояния
    private const string RESTART_LEVEL_INDEX_KEY = "TempRestartLevelIndex";
    private const string PLAYER_STATE_SAVED_KEY = "PlayerStateSaved";

    // Статы персонажа
    private const string KEY_MAX_HEALTH = "PlayerMaxHealth";
    private const string KEY_CURRENT_HEALTH = "PlayerCurrentHealth";
    private const string KEY_MAX_STAMINA = "PlayerMaxStamina";
    private const string KEY_CURRENT_STAMINA = "PlayerCurrentStamina";
    private const string KEY_ATTACK_DAMAGE = "PlayerAttackDamage";
    private const string KEY_DEFENSE = "PlayerDefense";
    private const string KEY_MOVE_SPEED = "PlayerMoveSpeed";
    private const string KEY_JUMP_POWER = "PlayerJumpPower";
    private const string KEY_SPRINT_MULT = "PlayerSprintMultiplier";
    private const string KEY_DASH_SPEED = "PlayerDashSpeed";
    private const string KEY_THROW_FORCE = "PlayerThrowForce";
    private const string KEY_THROW_COOLDOWN = "PlayerThrowCooldown";

    // Эффекты навыков
    private const string KEY_CAN_SHOOT = "SkillCanShoot";
    private const string KEY_HAS_FLASHLIGHT = "SkillHasFlashlight";
    private const string KEY_REVIVE_CHARGES = "SkillReviveCharges";
    private const string KEY_CURRENT_SHIELD = "SkillCurrentShield";
    private const string KEY_MAX_SHIELD = "SkillMaxShield";
    private const string KEY_EXTRA_PROJECTILES = "SkillExtraProjectiles";
    private const string KEY_PROJECTILE_SPREAD = "SkillProjectileSpread";
    private const string KEY_BULLET_SPLASH = "SkillBulletSplash";
    private const string KEY_BULLET_SPLASH_DMG = "SkillBulletSplashDmg";
    private const string KEY_BULLET_SPLASH_RADIUS = "SkillBulletSplashRadius";

    // Монеты
    private const string KEY_COINS = "PlayerCoins";

    private void Awake()
    {
        Initialize();
        RestoreLevelAfterRestart();
        RestorePlayerState();
    }

    private void Initialize()
    {
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.raycastTarget = false;
            fadeImage.gameObject.SetActive(true);
        }

        PrepareCanvasGroup(deathTextGroup);
        PrepareCanvasGroup(restartButtonGroup);
        PrepareCanvasGroup(mainMenuButtonGroup);
    }

    private void PrepareCanvasGroup(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        group.gameObject.SetActive(true);
    }

    public void ShowDeathScreen()
    {
        if (isShowing) return;
        StartCoroutine(ShowDeathScreenRoutine());
    }

    private IEnumerator ShowDeathScreenRoutine()
    {
        isShowing = true;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        yield return StartCoroutine(FadeImageToAlpha(screenTargetAlpha, screenFadeDuration));

        if (fadeImage != null)
            fadeImage.raycastTarget = false;

        if (deathTextGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(deathTextGroup, 1f, textFadeDuration, false));

        if (betweenElementsDelay > 0f)
            yield return new WaitForSeconds(betweenElementsDelay);

        if (restartButtonGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(restartButtonGroup, 1f, buttonFadeDuration, true));

        if (betweenElementsDelay > 0f)
            yield return new WaitForSeconds(betweenElementsDelay);

        if (mainMenuButtonGroup != null)
            yield return StartCoroutine(FadeCanvasGroup(mainMenuButtonGroup, 1f, buttonFadeDuration, true));
    }

    private IEnumerator FadeImageToAlpha(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color color = fadeImage.color;
        float startAlpha = color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = targetAlpha;
        fadeImage.color = color;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration, bool enableInteractionOnComplete)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        float startAlpha = group.alpha;

        group.gameObject.SetActive(true);
        group.interactable = false;
        group.blocksRaycasts = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        group.alpha = targetAlpha;

        if (enableInteractionOnComplete)
        {
            group.interactable = true;
            group.blocksRaycasts = true;
        }
    }

    // ================= КНОПКИ =================

    public void OnRestartButton()
    {
        SavePlayerState();
        StoreCurrentLevelIndexForRestart();
        StartCoroutine(FadeAndLoad(SceneManager.GetActiveScene().name));
    }

    public void OnMainMenuButton()
    {
        ClearAllTempData();
        StartCoroutine(FadeAndLoad(menuSceneName));
    }

    private IEnumerator FadeAndLoad(string sceneName)
    {
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        yield return StartCoroutine(FadeImageToAlpha(1f, 0.25f));
        SceneManager.LoadScene(sceneName);
    }

    // ================= СОХРАНЕНИЕ =================

    private void SavePlayerState()
    {
        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        SkillEffectApplier skillEffects = FindObjectOfType<SkillEffectApplier>();

        if (player != null)
        {
            PlayerPrefs.SetInt(KEY_MAX_HEALTH, player.maxHealth);
            PlayerPrefs.SetInt(KEY_CURRENT_HEALTH, player.currentHealth);
            PlayerPrefs.SetFloat(KEY_MAX_STAMINA, player.maxStamina);
            PlayerPrefs.SetFloat(KEY_CURRENT_STAMINA, player.currentStamina);
            PlayerPrefs.SetInt(KEY_ATTACK_DAMAGE, player.attackDamage);
            PlayerPrefs.SetInt(KEY_DEFENSE, player.defenseStat);
            PlayerPrefs.SetFloat(KEY_MOVE_SPEED, player.moveSpeed);
            PlayerPrefs.SetFloat(KEY_JUMP_POWER, player.jumpPower);
            PlayerPrefs.SetFloat(KEY_SPRINT_MULT, player.sprintMultiplier);
            PlayerPrefs.SetFloat(KEY_DASH_SPEED, player.dashSpeed);
            PlayerPrefs.SetFloat(KEY_THROW_FORCE, player.throwForce);
            PlayerPrefs.SetFloat(KEY_THROW_COOLDOWN, player.cooldown);
        }

        if (skillEffects != null)
        {
            PlayerPrefs.SetInt(KEY_CAN_SHOOT, skillEffects.CanShoot ? 1 : 0);
            PlayerPrefs.SetInt(KEY_HAS_FLASHLIGHT, skillEffects.HasFlashlight ? 1 : 0);
            PlayerPrefs.SetInt(KEY_REVIVE_CHARGES, skillEffects.ReviveCharges);
            PlayerPrefs.SetFloat(KEY_CURRENT_SHIELD, skillEffects.CurrentShield);
            PlayerPrefs.SetFloat(KEY_MAX_SHIELD, skillEffects.MaxShield);
            PlayerPrefs.SetInt(KEY_EXTRA_PROJECTILES, skillEffects.ExtraProjectiles);
            PlayerPrefs.SetFloat(KEY_PROJECTILE_SPREAD, skillEffects.ProjectileSpreadAngle);
            PlayerPrefs.SetInt(KEY_BULLET_SPLASH, skillEffects.BulletSplashEnabled ? 1 : 0);
            PlayerPrefs.SetFloat(KEY_BULLET_SPLASH_DMG, skillEffects.BulletSplashDamage);
            PlayerPrefs.SetFloat(KEY_BULLET_SPLASH_RADIUS, skillEffects.BulletSplashRadius);
        }

        // Сохраняем монеты
        PlayerPrefs.SetInt(KEY_COINS, CoinSystem.Instance != null ? CoinSystem.Instance.CurrentCoins : 0);

        PlayerPrefs.SetInt(PLAYER_STATE_SAVED_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log("DeathScreenController: Состояние сохранено.");
    }

    private void RestorePlayerState()
    {
        if (PlayerPrefs.GetInt(PLAYER_STATE_SAVED_KEY, 0) == 0)
            return;

        PlayerMovement player = FindObjectOfType<PlayerMovement>();
        SkillEffectApplier skillEffects = FindObjectOfType<SkillEffectApplier>();

        if (player != null)
        {
            player.maxHealth = PlayerPrefs.GetInt(KEY_MAX_HEALTH, player.maxHealth);
            player.currentHealth = PlayerPrefs.GetInt(KEY_CURRENT_HEALTH, player.currentHealth);
            player.maxStamina = PlayerPrefs.GetFloat(KEY_MAX_STAMINA, player.maxStamina);
            player.currentStamina = PlayerPrefs.GetFloat(KEY_CURRENT_STAMINA, player.currentStamina);
            player.attackDamage = PlayerPrefs.GetInt(KEY_ATTACK_DAMAGE, player.attackDamage);
            player.defenseStat = PlayerPrefs.GetInt(KEY_DEFENSE, player.defenseStat);
            player.moveSpeed = PlayerPrefs.GetFloat(KEY_MOVE_SPEED, player.moveSpeed);
            player.jumpPower = PlayerPrefs.GetFloat(KEY_JUMP_POWER, player.jumpPower);
            player.sprintMultiplier = PlayerPrefs.GetFloat(KEY_SPRINT_MULT, player.sprintMultiplier);
            player.dashSpeed = PlayerPrefs.GetFloat(KEY_DASH_SPEED, player.dashSpeed);
            player.throwForce = PlayerPrefs.GetFloat(KEY_THROW_FORCE, player.throwForce);
            player.cooldown = PlayerPrefs.GetFloat(KEY_THROW_COOLDOWN, player.cooldown);
        }

        if (skillEffects != null)
        {
            skillEffects.RestoreEffects(
                PlayerPrefs.GetInt(KEY_CAN_SHOOT, 0) == 1,
                PlayerPrefs.GetInt(KEY_HAS_FLASHLIGHT, 0) == 1,
                PlayerPrefs.GetInt(KEY_REVIVE_CHARGES, 0),
                PlayerPrefs.GetFloat(KEY_CURRENT_SHIELD, 0f),
                PlayerPrefs.GetFloat(KEY_MAX_SHIELD, 0f),
                PlayerPrefs.GetInt(KEY_EXTRA_PROJECTILES, 0),
                PlayerPrefs.GetFloat(KEY_PROJECTILE_SPREAD, 8f),
                PlayerPrefs.GetInt(KEY_BULLET_SPLASH, 0) == 1,
                PlayerPrefs.GetFloat(KEY_BULLET_SPLASH_DMG, 0f),
                PlayerPrefs.GetFloat(KEY_BULLET_SPLASH_RADIUS, 0f)
            );
        }

        // Восстанавливаем монеты
        if (CoinSystem.Instance != null)
        {
            int savedCoins = PlayerPrefs.GetInt(KEY_COINS, 0);
            CoinSystem.Instance.SetCoins(savedCoins);
        }

        Debug.Log("DeathScreenController: Состояние восстановлено.");
    }

    private void StoreCurrentLevelIndexForRestart()
    {
        if (LevelManager.Instance != null)
        {
            PlayerPrefs.SetInt(RESTART_LEVEL_INDEX_KEY, LevelManager.Instance.CurrentLevelIndex);
            PlayerPrefs.Save();
        }
    }

    private void RestoreLevelAfterRestart()
    {
        if (PlayerPrefs.HasKey(RESTART_LEVEL_INDEX_KEY))
        {
            int savedIndex = PlayerPrefs.GetInt(RESTART_LEVEL_INDEX_KEY);
            PlayerPrefs.DeleteKey(RESTART_LEVEL_INDEX_KEY);

            if (LevelManager.Instance != null)
                LevelManager.Instance.SetCurrentLevelFromCode(savedIndex, true);
        }
    }

    private void ClearAllTempData()
    {
        PlayerPrefs.DeleteKey(RESTART_LEVEL_INDEX_KEY);
        PlayerPrefs.DeleteKey(PLAYER_STATE_SAVED_KEY);
        PlayerPrefs.DeleteKey(KEY_MAX_HEALTH);
        PlayerPrefs.DeleteKey(KEY_CURRENT_HEALTH);
        PlayerPrefs.DeleteKey(KEY_MAX_STAMINA);
        PlayerPrefs.DeleteKey(KEY_CURRENT_STAMINA);
        PlayerPrefs.DeleteKey(KEY_ATTACK_DAMAGE);
        PlayerPrefs.DeleteKey(KEY_DEFENSE);
        PlayerPrefs.DeleteKey(KEY_MOVE_SPEED);
        PlayerPrefs.DeleteKey(KEY_JUMP_POWER);
        PlayerPrefs.DeleteKey(KEY_SPRINT_MULT);
        PlayerPrefs.DeleteKey(KEY_DASH_SPEED);
        PlayerPrefs.DeleteKey(KEY_THROW_FORCE);
        PlayerPrefs.DeleteKey(KEY_THROW_COOLDOWN);
        PlayerPrefs.DeleteKey(KEY_CAN_SHOOT);
        PlayerPrefs.DeleteKey(KEY_HAS_FLASHLIGHT);
        PlayerPrefs.DeleteKey(KEY_REVIVE_CHARGES);
        PlayerPrefs.DeleteKey(KEY_CURRENT_SHIELD);
        PlayerPrefs.DeleteKey(KEY_MAX_SHIELD);
        PlayerPrefs.DeleteKey(KEY_EXTRA_PROJECTILES);
        PlayerPrefs.DeleteKey(KEY_PROJECTILE_SPREAD);
        PlayerPrefs.DeleteKey(KEY_BULLET_SPLASH);
        PlayerPrefs.DeleteKey(KEY_BULLET_SPLASH_DMG);
        PlayerPrefs.DeleteKey(KEY_BULLET_SPLASH_RADIUS);
        PlayerPrefs.DeleteKey(KEY_COINS);
        PlayerPrefs.Save();
    }
}