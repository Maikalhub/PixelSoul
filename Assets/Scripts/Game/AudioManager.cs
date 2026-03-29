using System.Collections;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Master Volume")]
    [Range(0f, 1f)] public float musicMasterVolume = 0.8f;
    [Range(0f, 1f)] public float ambientMasterVolume = 0.8f;

    [Header("Fade Settings")]
    [SerializeField] private float musicFadeDuration = 1.25f;
    [SerializeField] private float ambientFadeDuration = 0.75f;

    [Header("Ambient Pitch Randomize")]
    [SerializeField] private float ambientPitchMin = 0.95f;
    [SerializeField] private float ambientPitchMax = 1.05f;

    [Header("Optional Preassigned Sources")]
    [SerializeField] private AudioSource musicSourceA;
    [SerializeField] private AudioSource musicSourceB;
    [SerializeField] private AudioSource ambientSource;

    private AudioSource activeMusicSource;
    private AudioSource inactiveMusicSource;

    private Coroutine musicTransitionCoroutine;
    private Coroutine musicPlaylistCoroutine;
    private Coroutine ambientRoutineCoroutine;
    private Coroutine ambientFadeCoroutine;

    private AudioClip[] currentMusicClips;
    private AudioClip[] currentAmbientClips;

    private int lastMusicIndex = -1;

    private Vector2 currentAmbientDelayRange = new Vector2(8f, 18f);
    private Vector2 currentAmbientVolumeRange = new Vector2(0.25f, 0.6f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupSources();

        activeMusicSource = musicSourceA;
        inactiveMusicSource = musicSourceB;
    }

    private void SetupSources()
    {
        if (musicSourceA == null)
            musicSourceA = CreateChildSource("Music Source A");

        if (musicSourceB == null)
            musicSourceB = CreateChildSource("Music Source B");

        if (ambientSource == null)
            ambientSource = CreateChildSource("Ambient Source");

        ConfigureMusicSource(musicSourceA);
        ConfigureMusicSource(musicSourceB);

        ambientSource.playOnAwake = false;
        ambientSource.loop = false;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 1f;
    }

    private AudioSource CreateChildSource(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        return child.AddComponent<AudioSource>();
    }

    private void ConfigureMusicSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    public void PlayLevelAudio(
        AudioClip[] musicClips,
        AudioClip[] randomAmbientClips,
        Vector2 randomAmbientDelayRange,
        Vector2 randomAmbientVolumeRange)
    {
        currentMusicClips = musicClips;
        currentAmbientClips = randomAmbientClips;
        currentAmbientDelayRange = NormalizeRange(randomAmbientDelayRange, 8f, 18f);
        currentAmbientVolumeRange = NormalizeRange(randomAmbientVolumeRange, 0.25f, 0.6f);

        RestartMusicPlaylist();
        RestartAmbientRoutine();
    }

    public void StopAllAudio(float fadeDuration = 0.75f)
    {
        if (musicTransitionCoroutine != null)
        {
            StopCoroutine(musicTransitionCoroutine);
            musicTransitionCoroutine = null;
        }

        if (musicPlaylistCoroutine != null)
        {
            StopCoroutine(musicPlaylistCoroutine);
            musicPlaylistCoroutine = null;
        }

        if (ambientRoutineCoroutine != null)
        {
            StopCoroutine(ambientRoutineCoroutine);
            ambientRoutineCoroutine = null;
        }

        if (ambientFadeCoroutine != null)
        {
            StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = null;
        }

        StartCoroutine(FadeOutAndStopRoutine(fadeDuration));
    }

    public void SetMusicVolume(float value)
    {
        musicMasterVolume = Mathf.Clamp01(value);
        activeMusicSource.volume = Mathf.Min(activeMusicSource.volume, musicMasterVolume);
    }

    public void SetAmbientVolume(float value)
    {
        ambientMasterVolume = Mathf.Clamp01(value);
    }

    private void RestartMusicPlaylist()
    {
        if (musicPlaylistCoroutine != null)
        {
            StopCoroutine(musicPlaylistCoroutine);
            musicPlaylistCoroutine = null;
        }

        if (musicTransitionCoroutine != null)
        {
            StopCoroutine(musicTransitionCoroutine);
            musicTransitionCoroutine = null;
        }

        if (currentMusicClips == null || currentMusicClips.Length == 0)
        {
            StartCoroutine(FadeOutCurrentMusicRoutine());
            return;
        }

        musicPlaylistCoroutine = StartCoroutine(MusicPlaylistRoutine());
    }

    private IEnumerator MusicPlaylistRoutine()
    {
        while (true)
        {
            AudioClip nextClip = GetNextMusicClip();
            if (nextClip == null)
            {
                yield return null;
                continue;
            }

            yield return StartCoroutine(CrossfadeMusicRoutine(nextClip));

            float additionalWait = Mathf.Max(0f, nextClip.length - (musicFadeDuration * 2f));

            if (additionalWait > 0f)
                yield return new WaitForSecondsRealtime(additionalWait);
            else
                yield return null;
        }
    }

    private AudioClip GetNextMusicClip()
    {
        if (currentMusicClips == null || currentMusicClips.Length == 0)
            return null;

        if (currentMusicClips.Length == 1)
        {
            lastMusicIndex = 0;
            return currentMusicClips[0];
        }

        int nextIndex = lastMusicIndex;

        int safety = 0;
        while (nextIndex == lastMusicIndex && safety < 20)
        {
            nextIndex = Random.Range(0, currentMusicClips.Length);
            safety++;
        }

        lastMusicIndex = nextIndex;
        return currentMusicClips[nextIndex];
    }

    private IEnumerator FadeOutCurrentMusicRoutine()
    {
        float startActive = activeMusicSource.volume;
        float startInactive = inactiveMusicSource.volume;
        float t = 0f;

        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / musicFadeDuration);

            activeMusicSource.volume = Mathf.Lerp(startActive, 0f, k);
            inactiveMusicSource.volume = Mathf.Lerp(startInactive, 0f, k);

            yield return null;
        }

        activeMusicSource.volume = 0f;
        inactiveMusicSource.volume = 0f;

        activeMusicSource.Stop();
        inactiveMusicSource.Stop();
    }

    private IEnumerator FadeOutAndStopRoutine(float duration)
    {
        float startA = musicSourceA.volume;
        float startB = musicSourceB.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            musicSourceA.volume = Mathf.Lerp(startA, 0f, k);
            musicSourceB.volume = Mathf.Lerp(startB, 0f, k);

            yield return null;
        }

        musicSourceA.volume = 0f;
        musicSourceB.volume = 0f;

        musicSourceA.Stop();
        musicSourceB.Stop();
        ambientSource.Stop();
    }

    private IEnumerator CrossfadeMusicRoutine(AudioClip newClip)
    {
        if (newClip == null)
        {
            yield return StartCoroutine(FadeOutCurrentMusicRoutine());
            yield break;
        }

        inactiveMusicSource.Stop();
        inactiveMusicSource.clip = newClip;
        inactiveMusicSource.time = 0f;
        inactiveMusicSource.volume = 0f;
        inactiveMusicSource.Play();

        float startOldVolume = activeMusicSource.volume;
        float t = 0f;

        while (t < musicFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / musicFadeDuration);

            inactiveMusicSource.volume = Mathf.Lerp(0f, musicMasterVolume, k);
            activeMusicSource.volume = Mathf.Lerp(startOldVolume, 0f, k);

            yield return null;
        }

        inactiveMusicSource.volume = musicMasterVolume;
        activeMusicSource.volume = 0f;
        activeMusicSource.Stop();

        AudioSource temp = activeMusicSource;
        activeMusicSource = inactiveMusicSource;
        inactiveMusicSource = temp;
    }

    private void RestartAmbientRoutine()
    {
        if (ambientRoutineCoroutine != null)
        {
            StopCoroutine(ambientRoutineCoroutine);
            ambientRoutineCoroutine = null;
        }

        if (ambientFadeCoroutine != null)
        {
            StopCoroutine(ambientFadeCoroutine);
            ambientFadeCoroutine = null;
        }

        if (currentAmbientClips == null || currentAmbientClips.Length == 0)
        {
            ambientFadeCoroutine = StartCoroutine(FadeOutAmbientRoutine());
            return;
        }

        ambientRoutineCoroutine = StartCoroutine(RandomAmbientRoutine());
    }

    private IEnumerator FadeOutAmbientRoutine()
    {
        float startVolume = ambientSource.volume;
        float t = 0f;

        while (t < ambientFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / ambientFadeDuration);
            ambientSource.volume = Mathf.Lerp(startVolume, 0f, k);
            yield return null;
        }

        ambientSource.volume = 1f;
        ambientSource.Stop();
    }

    private IEnumerator RandomAmbientRoutine()
    {
        while (true)
        {
            float waitTime = Random.Range(currentAmbientDelayRange.x, currentAmbientDelayRange.y);
            yield return new WaitForSeconds(waitTime);

            if (currentAmbientClips == null || currentAmbientClips.Length == 0)
                continue;

            AudioClip clip = GetRandomAmbientClip();
            if (clip == null)
                continue;

            ambientSource.pitch = Random.Range(ambientPitchMin, ambientPitchMax);

            float randomVolume = Random.Range(
                currentAmbientVolumeRange.x,
                currentAmbientVolumeRange.y
            ) * ambientMasterVolume;

            ambientSource.PlayOneShot(clip, randomVolume);
        }
    }

    private AudioClip GetRandomAmbientClip()
    {
        if (currentAmbientClips == null || currentAmbientClips.Length == 0)
            return null;

        int index = Random.Range(0, currentAmbientClips.Length);
        return currentAmbientClips[index];
    }

    private Vector2 NormalizeRange(Vector2 value, float fallbackMin, float fallbackMax)
    {
        float min = value.x <= 0f ? fallbackMin : value.x;
        float max = value.y <= 0f ? fallbackMax : value.y;

        if (max < min)
            max = min;

        return new Vector2(min, max);
    }
}