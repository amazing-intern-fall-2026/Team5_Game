using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DayNightSFX : MonoBehaviour
{
    [SerializeField] private DayNightController dayNightController;
    [SerializeField] private AudioClip dayStartClip;
    [SerializeField] private AudioClip nightStartClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource audioSource;
    private bool? lastIsDay;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // ambient/global, not tied to a world position

        if (dayNightController == null)
            dayNightController = FindFirstObjectByType<DayNightController>();
    }

    private void Update()
    {
        if (dayNightController == null)
            return;

        bool isDay = dayNightController.IsDay;

        if (lastIsDay == null)
        {
            // First frame: just record the starting state, no stinger on load.
            lastIsDay = isDay;
            return;
        }

        if (isDay != lastIsDay.Value)
        {
            PlayTransition(isDay);
            lastIsDay = isDay;
        }
    }

    private void PlayTransition(bool isDay)
    {
        AudioClip clip = isDay ? dayStartClip : nightStartClip;
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
