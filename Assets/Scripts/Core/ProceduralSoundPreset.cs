using UnityEngine;

using System.Collections.Generic;

/// <summary>
/// Defines the basic shape of the generated sound wave.
/// </summary>
public enum WaveformType { Sine, Square, Sawtooth, Noise, Additive }
public enum FilterType { None, LowPass, HighPass, BandPass }

/// <summary>
/// A single harmonic component for additive synthesis.
/// </summary>
[System.Serializable]
public class Harmonic
{
    [Tooltip("Frequency multiplier relative to the base frequency.")]
    public float frequencyMultiplier = 1.0f;
    [Tooltip("Amplitude of this harmonic (0-1).")]
    [Range(0f, 1f)]
    public float amplitude = 1.0f;
}

/// <summary>
/// A ScriptableObject that holds the definition for a procedurally generated sound.
/// This acts as a recipe for the SoundSynthesizer.
/// </summary>
[CreateAssetMenu(fileName = "New ProceduralSoundPreset", menuName = "Alien Civilizations/Procedural Sound Preset")]
public class ProceduralSoundPreset : ScriptableObject
{
    [Header("Core Shape")]
    [Tooltip("The fundamental waveform of the sound. If 'Additive' is chosen, the harmonics list will be used.")]
    public WaveformType waveform = WaveformType.Sine;

    [Tooltip("The base frequency of the sound in Hz. For additive synthesis, this is the fundamental frequency.")]
    [Range(20f, 20000f)]
    public float frequency = 440f;

    [Header("Additive Synthesis")]
    [Tooltip("A list of harmonics to be combined for the 'Additive' waveform type.")]
    public List<Harmonic> harmonics = new List<Harmonic>();

    [Header("Envelope (ADSR)")]
    [Tooltip("Attack Time: How long it takes for the sound to reach peak volume (in seconds).")]
    [Range(0.001f, 2f)]
    public float attackTime = 0.01f;

    [Tooltip("Decay Time: How long it takes for the sound to drop from peak to sustain level (in seconds).")]
    [Range(0.001f, 2f)]
    public float decayTime = 0.1f;

    [Tooltip("Sustain Amplitude: The volume level the sound maintains after the decay phase (0-1).")]
    [Range(0f, 1f)]
    public float sustainAmplitude = 0.5f;

    [Tooltip("Release Time: How long it takes for the sound to fade out after it has finished playing (in seconds).")]
    [Range(0.001f, 2f)]
    public float releaseTime = 0.2f;

    /// <summary>
    /// The total duration of the sound's main body (Attack + Decay).
    /// The release phase happens after this.
    /// </summary>
    public float BodyDuration => attackTime + decayTime;

    /// <summary>
    /// The total duration of the entire sound envelope.
    /// </summary>
    public float TotalDuration => attackTime + decayTime + releaseTime;

    [Header("Frequency Modulation (FM Synthesis)")]
    [Tooltip("The frequency of the modulating wave. Creates complex overtones.")]
    [Range(0f, 2000f)]
    public float fmFrequency = 0f;

    [Tooltip("The intensity of the frequency modulation. Higher values create more distorted, metallic sounds.")]
    [Range(0f, 1000f)]
    public float fmAmount = 0f;

    [Header("Filter (Subtractive Synthesis)")]
    [Tooltip("The type of filter to apply to the sound.")]
    public FilterType filterType = FilterType.None;

    [Tooltip("The center/cutoff frequency of the filter.")]
    [Range(20f, 20000f)]
    public float filterFrequency = 20000f;

    [Tooltip("The resonance or 'Q' of the filter, which creates a peak at the filter frequency.")]
    [Range(1f, 10f)]
    public float filterResonance = 1f;

    [Header("Effects")]
    [Tooltip("Delay Time: The time in seconds between echoes. Set to 0 for no delay.")]
    [Range(0f, 2f)]
    public float delayTime = 0f;

    [Tooltip("Delay Feedback: Amount of the delayed signal fed back into the delay line (0-1). Controls the number of echoes.")]
    [Range(0f, 0.95f)]
    public float delayFeedback = 0.5f;

    [Tooltip("Delay Mix: The blend between the original (dry) and delayed (wet) signal (0-1).")]
    [Range(0f, 1f)]
    public float delayMix = 0.5f;

    [Tooltip("Reverb Time: The decay time of the reverb tail in seconds. Set to 0 for no reverb.")]
    [Range(0f, 4f)]
    public float reverbTime = 0f;

    [Tooltip("Reverb Mix: The blend between the original (dry) and reverberated (wet) signal (0-1).")]
    [Range(0f, 1f)]
    public float reverbMix = 0f;
}
