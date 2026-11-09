using System.Collections;
using UnityEngine;

// Continuous looping ambience (birds by day, owls/crickets by night) that
// crossfades smoothly on each day/night transition instead of hard-cutting.
// Detects the transition itself by polling IsDay, rather than depending on
// any particular event shape on DayNightController.
public class DayNightAmbience : MonoBehaviour
{
    [SerializeField] private DayNightController dayNightController;

    [Header("Loops")]
    [SerializeField] private AudioClip dayAmbience;
    [SerializeField] private AudioClip nightAmbience;
    [SerializeField, Range(0f, 1f)] private float dayVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float nightVolume = 0.5f;
    [SerializeField] private float fadeDuration = 2f;

    private AudioSource dayAudioSource;
    private AudioSource nightAudioSource;
    private Coroutine fadeRoutine;
    private bool? lastIsDay;

    private void Awake()
    {
        if (dayNightController == null)
            dayNightController = FindFirstObjectByType<DayNightController>();

        dayAudioSource = CreateLoopingSource(dayAmbience);
        nightAudioSource = CreateLoopingSource(nightAmbience);
    }

    private void Update()
    {
        if (dayNightController == null)
            return;

        bool isDay = dayNightController.IsDay;

        if (lastIsDay == null)
        {
            SetImmediate(isDay);
            lastIsDay = isDay;
            return;
        }

        if (isDay != lastIsDay.Value)
        {
            HandlePhaseChanged(isDay);
            lastIsDay = isDay;
        }
    }

    private AudioSource CreateLoopingSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // ambient/global, not tied to a world position
        source.volume = 0f;
        return source;
    }

    private void HandlePhaseChanged(bool isDay)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(isDay));
    }

    private void SetImmediate(bool isDay)
    {
        AudioSource activeSource = isDay ? dayAudioSource : nightAudioSource;
        AudioSource inactiveSource = isDay ? nightAudioSource : dayAudioSource;

        activeSource.volume = isDay ? dayVolume : nightVolume;
        if (activeSource.clip != null)
            activeSource.Play();

        inactiveSource.volume = 0f;
        inactiveSource.Stop();
    }

    private IEnumerator FadeTo(bool isDay)
    {
        AudioSource fadeInSource = isDay ? dayAudioSource : nightAudioSource;
        AudioSource fadeOutSource = isDay ? nightAudioSource : dayAudioSource;
        float targetVolume = isDay ? dayVolume : nightVolume;

        if (fadeInSource.clip != null && !fadeInSource.isPlaying)
            fadeInSource.Play();

        float startInVolume = fadeInSource.volume;
        float startOutVolume = fadeOutSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeInSource.volume = Mathf.Lerp(startInVolume, targetVolume, t);
            fadeOutSource.volume = Mathf.Lerp(startOutVolume, 0f, t);
            yield return null;
        }

        fadeInSource.volume = targetVolume;
        fadeOutSource.volume = 0f;
        fadeOutSource.Stop();
    }
}
