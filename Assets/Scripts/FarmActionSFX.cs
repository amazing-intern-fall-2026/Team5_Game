using UnityEngine;

// Attach to the Player, alongside PlayerFarmToolController. One-shot
// sounds for farming actions, each with its own clip pool for variation.
[RequireComponent(typeof(AudioSource))]
public class FarmActionSFX : MonoBehaviour
{
    [SerializeField] private AudioClip[] tillClips;
    [SerializeField] private AudioClip[] waterClips;
    [SerializeField] private AudioClip[] plantClips;
    [SerializeField] private AudioClip[] harvestClips;
    [SerializeField] private AudioClip[] fertilizeClips;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // positional, plays from wherever the player is
    }

    public void PlayTill() => PlayRandom(tillClips);
    public void PlayWater() => PlayRandom(waterClips);
    public void PlayPlant() => PlayRandom(plantClips);
    public void PlayHarvest() => PlayRandom(harvestClips);
    public void PlayFertilize() => PlayRandom(fertilizeClips);

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null)
            audioSource.PlayOneShot(clip, volume);
    }
}
