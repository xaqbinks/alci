using UnityEngine;

/// <summary>
/// A class representing a single Biquad filter instance.
/// </summary>
internal class BiquadFilter
{
    // Coefficients
    private float a0, a1, a2, b1, b2;
    // State
    private float z1, z2;

    public BiquadFilter()
    {
        // Initialize to a pass-through state
        a0 = 1.0f;
        a1 = a2 = b1 = b2 = 0;
    }

    /// <summary>
    /// Processes a single sample through the filter.
    /// </summary>
    public float Process(float inSample)
    {
        float outSample = inSample * a0 + z1;
        z1 = inSample * a1 + z2 - b1 * outSample;
        z2 = inSample * a2 - b2 * outSample;
        return outSample;
    }

    /// <summary>
    /// Calculates the filter coefficients based on type and parameters.
    /// </summary>
    public void SetCoefficients(FilterType type, float frequency, float Q, int sampleRate)
    {
        if (type == FilterType.None) return;

        float w0 = 2 * Mathf.PI * frequency / sampleRate;
        float cosW0 = Mathf.Cos(w0);
        float sinW0 = Mathf.Sin(w0);
        float alpha = sinW0 / (2 * Q);

        float b0_temp = 0, a0_temp = 0;

        switch (type)
        {
            case FilterType.LowPass:
                b0_temp = (1 - cosW0) / 2;
                b1 = 1 - cosW0;
                b2 = (1 - cosW0) / 2;
                a0_temp = 1 + alpha;
                a1 = -2 * cosW0;
                a2 = 1 - alpha;
                break;
            case FilterType.HighPass:
                b0_temp = (1 + cosW0) / 2;
                b1 = -(1 + cosW0);
                b2 = (1 + cosW0) / 2;
                a0_temp = 1 + alpha;
                a1 = -2 * cosW0;
                a2 = 1 - alpha;
                break;
            case FilterType.BandPass:
                b0_temp = alpha;
                b1 = 0;
                b2 = -alpha;
                a0_temp = 1 + alpha;
                a1 = -2 * cosW0;
                a2 = 1 - alpha;
                break;
        }

        // Normalize coefficients
        a0 = b0_temp / a0_temp;
        a1 = b1 / a0_temp;
        a2 = b2 / a0_temp;
        b1 = a1 / a0_temp;
        b2 = a2 / a0_temp;
    }
}


/// <summary>
/// A static class responsible for generating AudioClips from ProceduralSoundPreset definitions.
/// </summary>
public static class SoundSynthesizer
{
    public static AudioClip GenerateStereoClip(ProceduralSoundPreset preset, Vector3 soundPosition, Transform listener)
    {
        int sampleRate = AudioSettings.outputSampleRate;
        int sampleCount = (int)(preset.TotalDuration * sampleRate);
        int channelCount = 2; // Stereo
        float[] samples = new float[sampleCount * channelCount];

        // --- Calculate Panning ---
        Vector3 direction = (soundPosition - listener.position).normalized;
        Vector3 localDir = listener.InverseTransformDirection(direction);
        float pan = localDir.x; // -1 (left) to 1 (right)

        // Constant power panning law
        float leftGain = Mathf.Sqrt(0.5f * (1f - pan));
        float rightGain = Mathf.Sqrt(0.5f * (1f + pan));

        // Pre-calculate normalization factor for additive synthesis
        float totalAmplitude = 0;
        if (preset.waveform == WaveformType.Additive)
        {
            foreach (var harmonic in preset.harmonics) totalAmplitude += harmonic.amplitude;
        }
        if (totalAmplitude == 0) totalAmplitude = 1;

        for (int i = 0; i < sampleCount; i++)
        {
            float currentTime = (float)i / sampleRate;
            float envelope = GetAdsrEnvelope(currentTime, preset);

            float fmModulation = preset.fmAmount * Mathf.Sin(preset.fmFrequency * 2 * Mathf.PI * currentTime);
            float fundamentalFrequency = preset.frequency + fmModulation;

            float wave = 0;
            switch (preset.waveform)
            {
                // (Waveform generation logic remains the same)
                case WaveformType.Sine: wave = Mathf.Sin(fundamentalFrequency * 2 * Mathf.PI * currentTime); break;
                case WaveformType.Square: wave = Mathf.Sign(Mathf.Sin(fundamentalFrequency * 2 * Mathf.PI * currentTime)); break;
                case WaveformType.Sawtooth: wave = 2.0f * ((currentTime * fundamentalFrequency) % 1.0f) - 1.0f; break;
                case WaveformType.Noise: wave = Random.Range(-1f, 1f); break;
                case WaveformType.Additive:
                    foreach (var harmonic in preset.harmonics)
                    {
                        float hFreq = fundamentalFrequency * harmonic.frequencyMultiplier;
                        wave += harmonic.amplitude * Mathf.Sin(hFreq * 2 * Mathf.PI * currentTime);
                    }
                    wave /= totalAmplitude;
                    break;
            }
            // Write to stereo channels
            int frameIndex = i * channelCount;
            samples[frameIndex] = wave * envelope * leftGain;
            samples[frameIndex + 1] = wave * envelope * rightGain;
        }

        if (preset.filterType != FilterType.None)
        {
            BiquadFilter leftFilter = new BiquadFilter();
            BiquadFilter rightFilter = new BiquadFilter();
            leftFilter.SetCoefficients(preset.filterType, preset.filterFrequency, preset.filterResonance, sampleRate);
            rightFilter.SetCoefficients(preset.filterType, preset.filterFrequency, preset.filterResonance, sampleRate);

            for (int i = 0; i < sampleCount; i++)
            {
                int frameIndex = i * channelCount;
                samples[frameIndex] = leftFilter.Process(samples[frameIndex]);
                samples[frameIndex + 1] = rightFilter.Process(samples[frameIndex + 1]);
            }
        }

        // Apply Delay Effect
        if (preset.delayTime > 0.001f)
        {
            ApplyDelay(samples, channelCount, sampleRate, preset.delayTime, preset.delayFeedback, preset.delayMix);
        }

        // Apply Reverb Effect
        if (preset.reverbTime > 0.001f)
        {
            ApplyReverb(samples, channelCount, sampleRate, preset.reverbTime, preset.reverbMix);
        }

        AudioClip clip = AudioClip.Create(preset.name, sampleCount, channelCount, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void ApplyDelay(float[] samples, int channelCount, int sampleRate, float delayTime, float feedback, float mix)
    {
        int delaySamples = (int)(delayTime * sampleRate) * channelCount;
        float[] delayBuffer = new float[delaySamples];
        int delayWritePos = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            // Read from the buffer
            float delayedSample = delayBuffer[delayWritePos];

            // Mix the dry signal with the wet (delayed) signal
            float drySample = samples[i];
            samples[i] = Mathf.Lerp(drySample, delayedSample, mix);

            // Write the new mixed signal back to the buffer with feedback
            delayBuffer[delayWritePos] = drySample + delayedSample * feedback;

            // Advance the buffer position
            delayWritePos++;
            if (delayWritePos >= delaySamples)
            {
                delayWritePos = 0;
            }
        }
    }

    private static void ApplyReverb(float[] samples, int channelCount, int sampleRate, float reverbTime, float mix)
    {
        // A simple Schroeder-style reverb using multiple comb filters and all-pass filters.
        // These delay lengths are prime-like to avoid harmonic reinforcement.
        int[] combDelayLengths = { 1687, 1601, 2053, 2251 };
        int[] allPassDelayLengths = { 556, 441, 341, 225 };

        List<CombFilter> combFilters = new List<CombFilter>();
        foreach (int delay in combDelayLengths)
        {
            combFilters.Add(new CombFilter(delay, reverbTime, sampleRate));
        }

        List<AllPassFilter> allPassFilters = new List<AllPassFilter>();
        foreach (int delay in allPassDelayLengths)
        {
            allPassFilters.Add(new AllPassFilter(delay));
        }

        float[] output = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            float input = samples[i];
            float combOutput = 0;

            // Process through parallel comb filters
            foreach (var filter in combFilters)
            {
                combOutput += filter.Process(input);
            }

            // Process through serial all-pass filters
            float allPassOutput = combOutput;
            foreach (var filter in allPassFilters)
            {
                allPassOutput = filter.Process(allPassOutput);
            }

            output[i] = Mathf.Lerp(input, allPassOutput, mix);
        }

        // Copy the processed output back to the main sample array
        System.Array.Copy(output, samples, samples.Length);
    }

    private static float GetAdsrEnvelope(float time, ProceduralSoundPreset preset)
    {
        if (time <= preset.attackTime) return time / preset.attackTime;
        if (time <= preset.attackTime + preset.decayTime) return Mathf.Lerp(1f, preset.sustainAmplitude, (time - preset.attackTime) / preset.decayTime);
        if (time <= preset.BodyDuration) return preset.sustainAmplitude;
        if (time <= preset.TotalDuration) return Mathf.Lerp(preset.sustainAmplitude, 0f, (time - preset.BodyDuration) / preset.releaseTime);
        return 0f;
    }
}

// --- Reverb Helper Classes ---
internal class CombFilter
{
    private float[] buffer;
    private int pos = 0;
    private float gain;

    public CombFilter(int delay, float reverbTime, int sampleRate)
    {
        buffer = new float[delay];
        gain = Mathf.Pow(10, (-3 * delay) / (reverbTime * sampleRate));
    }

    public float Process(float input)
    {
        float output = buffer[pos];
        buffer[pos] = input + output * gain;
        pos = (pos + 1) % buffer.Length;
        return output;
    }
}

internal class AllPassFilter
{
    private float[] buffer;
    private int pos = 0;
    private const float gain = 0.5f;

    public AllPassFilter(int delay)
    {
        buffer = new float[delay];
    }

    public float Process(float input)
    {
        float output = -input + buffer[pos];
        buffer[pos] = input + output * gain;
        pos = (pos + 1) % buffer.Length;
        return output;
    }
}
