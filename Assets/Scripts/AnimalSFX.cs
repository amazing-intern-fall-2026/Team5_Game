using System.Collections;
using UnityEngine;

// Attach to any animal prefab (Pig, Chicken, Cow, Sheep...). Each instance
// gets its own AudioClip array, so the same component works for every
// animal - just drag in different clips per prefab.
[RequireComponent(typeof(AudioSource))]
public class AnimalSFX : MonoBehaviour
{
    [SerializeField] private AudioClip[] sounds;
    [SerializeField] private float minInterval = 4f;
    [SerializeField] private float maxInterval = 10f;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // positional - quieter the farther the player is
    }

    private void Start()
    {
        StartCoroutine(SoundRoutine());
    }

    private IEnumerator SoundRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            PlayRandomSound();
        }
    }

    private void PlayRandomSound()
    {
        if (sounds == null || sounds.Length == 0)
            return;

        AudioClip clip = sounds[Random.Range(0, sounds.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
