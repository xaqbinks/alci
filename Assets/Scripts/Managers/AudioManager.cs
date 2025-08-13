using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A central manager for handling all audio playback.
/// This version uses a procedural sound synthesizer to generate audio on the fly.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Procedural Sound Library")]
    [Tooltip("A list of procedural sound presets that can be triggered by name.")]
    public List<ProceduralSoundPreset> soundPresets;

    // The AudioSource used for playing one-shot sound effects.
    private AudioSource sfxSource;
    // A reference to the main camera or listener in the scene.
    private Transform listenerTransform;

    // A dictionary for fast lookups of presets by name.
    private Dictionary<string, ProceduralSoundPreset> presetDict;
    // A cache for recently generated audio clips to prevent redundant generation.
    private Dictionary<string, AudioClip> clipCache;
    private const float CACHE_CLEAR_INTERVAL = 10.0f; // Clear cache every 10 seconds.

    /// <summary>
    /// Initializes the AudioManager, gets its AudioSource component, and builds the preset library.
    /// </summary>
    public void Initialize()
    {
        sfxSource = GetComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        if (Camera.main != null)
        {
            listenerTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("AudioManager: No main camera found for positional audio. Sound will be mono.");
        }

        presetDict = new Dictionary<string, ProceduralSoundPreset>();
        foreach (var preset in soundPresets)
        {
            if (preset != null && !presetDict.ContainsKey(preset.name))
            {
                presetDict[preset.name] = preset;
            }
        }

        clipCache = new Dictionary<string, AudioClip>();
        StartCoroutine(ClearCacheRoutine());

        Debug.Log("AudioManager Initialized with procedural sound presets and caching.");
    }

    private System.Collections.IEnumerator ClearCacheRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(CACHE_CLEAR_INTERVAL);
            // In a real project, you might check if clips are still playing.
            // For this simple case, we just clear them.
            foreach (var clip in clipCache.Values)
            {
                Destroy(clip);
            }
            clipCache.Clear();
            Debug.Log("Audio cache cleared.");
        }
    }

    /// <summary>
    /// Generates and plays a positional procedural sound effect.
    /// </summary>
    /// <param name="presetName">The name of the ProceduralSoundPreset asset.</param>
    /// <param name="worldPosition">The world-space position of the sound event.</param>
    public void PlaySound(string presetName, Vector3 worldPosition)
    {
        if (listenerTransform == null) return; // Cannot play positional audio without a listener.

        if (presetDict.TryGetValue(presetName, out ProceduralSoundPreset preset))
        {
            AudioClip clip = SoundSynthesizer.GenerateStereoClip(preset, worldPosition, listenerTransform);
            sfxSource.PlayOneShot(clip);
            Destroy(clip, preset.TotalDuration);
        }
        else
        {
            Debug.LogWarning($"AudioManager: Sound preset '{presetName}' not found in the library.");
        }
    }

    /// <summary>
    /// Generates and plays a dynamic, positional, procedural sound effect.
    /// </summary>
    public void PlayDynamicSound(string presetName, Vector3 worldPosition, float? frequencyOverride = null, float? fmAmountOverride = null)
    {
        if (listenerTransform == null) return;

        // Create a unique key for the cache based on the sound's parameters.
        // Rounding parameters to reduce unique keys for similar sounds.
        string cacheKey = $"{presetName}_{Mathf.RoundToInt(frequencyOverride ?? 0)}_{Mathf.RoundToInt(fmAmountOverride ?? 0)}";

        AudioClip clipToPlay;

        if (clipCache.TryGetValue(cacheKey, out clipToPlay))
        {
            // Play the cached clip
            sfxSource.PlayOneShot(clipToPlay);
        }
        else
        {
            // --- Generate New Clip ---
            if (presetDict.TryGetValue(presetName, out ProceduralSoundPreset basePreset))
            {
                ProceduralSoundPreset tempPreset = Instantiate(basePreset);

                if (frequencyOverride.HasValue) tempPreset.frequency = frequencyOverride.Value;
                if (fmAmountOverride.HasValue) tempPreset.fmAmount = fmAmountOverride.Value;

                clipToPlay = SoundSynthesizer.GenerateStereoClip(tempPreset, worldPosition, listenerTransform);
                sfxSource.PlayOneShot(clipToPlay);

                // Add to cache, but don't destroy it here anymore. The coroutine will handle it.
                clipCache[cacheKey] = clipToPlay;

                Destroy(tempPreset, tempPreset.TotalDuration);
            }
            else
            {
                Debug.LogWarning($"AudioManager: Sound preset '{presetName}' not found for dynamic playback.");
            }
        }
    }
}
