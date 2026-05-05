using System;
using UnityEngine;

namespace KaleidoscopeEngine.Audio
{
    public enum AudioMusicalState
    {
        Calm,
        Build,
        Drop,
        Break,
        Silence
    }

    public enum AudioReactiveEventType
    {
        Beat,
        StrongBeat,
        Snare,
        Drop,
        Break,
        Build,
        Silence
    }

    public readonly struct AudioReactiveEvent
    {
        public readonly AudioReactiveEventType Type;
        public readonly float Time;
        public readonly float Confidence;
        public readonly float BassEnergy;
        public readonly float OverallEnergy;

        public AudioReactiveEvent(AudioReactiveEventType type, float time, float confidence, float bassEnergy, float overallEnergy)
        {
            Type = type;
            Time = time;
            Confidence = confidence;
            BassEnergy = bassEnergy;
            OverallEnergy = overallEnergy;
        }
    }

    public readonly struct AudioAnalysisSnapshot
    {
        public readonly float BassEnergy;
        public readonly float MidEnergy;
        public readonly float HighEnergy;
        public readonly float OverallEnergy;
        public readonly float EnergyDelta;
        public readonly float BeatConfidence;
        public readonly float BpmEstimate;
        public readonly bool BeatDetected;
        public readonly AudioMusicalState MusicalState;

        public AudioAnalysisSnapshot(
            float bassEnergy,
            float midEnergy,
            float highEnergy,
            float overallEnergy,
            float energyDelta,
            float beatConfidence,
            float bpmEstimate,
            bool beatDetected,
            AudioMusicalState musicalState)
        {
            BassEnergy = bassEnergy;
            MidEnergy = midEnergy;
            HighEnergy = highEnergy;
            OverallEnergy = overallEnergy;
            EnergyDelta = energyDelta;
            BeatConfidence = beatConfidence;
            BpmEstimate = bpmEstimate;
            BeatDetected = beatDetected;
            MusicalState = musicalState;
        }
    }

    [Serializable]
    public sealed class BeatDetector
    {
        [SerializeField, Range(8, 128)] private int historySize = 48;
        [SerializeField, Range(1.05f, 3f)] private float threshold = 1.48f;
        [SerializeField, Range(0.08f, 0.4f)] private float cooldownSeconds = 0.18f;
        [SerializeField, Range(0f, 1f)] private float minimumEnergy = 0.018f;

        private float[] history;
        private int historyIndex;
        private int historyCount;
        private float lastBeatTime = -10f;
        private float lastInterval;
        private float smoothedBpm;

        public float Confidence { get; private set; }
        public float AverageEnergy { get; private set; }
        public float BpmEstimate => smoothedBpm;

        public bool Process(float energy, float time)
        {
            EnsureHistory();
            AverageEnergy = CalculateAverage();
            float safeAverage = Mathf.Max(0.0001f, AverageEnergy);
            Confidence = Mathf.Clamp01((energy / safeAverage - threshold) / Mathf.Max(0.001f, threshold));
            bool cooledDown = time - lastBeatTime >= cooldownSeconds;
            bool detected = historyCount >= historySize / 3 &&
                            cooledDown &&
                            energy >= minimumEnergy &&
                            energy > safeAverage * threshold;

            Record(energy);
            if (detected)
            {
                if (lastBeatTime > 0f)
                {
                    lastInterval = Mathf.Clamp(time - lastBeatTime, 0.2f, 2f);
                    float bpm = 60f / Mathf.Max(0.001f, lastInterval);
                    smoothedBpm = smoothedBpm <= 0.1f ? bpm : Mathf.Lerp(smoothedBpm, bpm, 0.18f);
                }

                lastBeatTime = time;
            }

            return detected;
        }

        public void Reset()
        {
            history = null;
            historyIndex = 0;
            historyCount = 0;
            lastBeatTime = -10f;
            lastInterval = 0f;
            smoothedBpm = 0f;
            Confidence = 0f;
            AverageEnergy = 0f;
        }

        private void EnsureHistory()
        {
            int safeSize = Mathf.Clamp(historySize, 8, 128);
            if (history == null || history.Length != safeSize)
            {
                history = new float[safeSize];
                historyIndex = 0;
                historyCount = 0;
            }
        }

        private void Record(float energy)
        {
            history[historyIndex] = Mathf.Max(0f, energy);
            historyIndex = (historyIndex + 1) % history.Length;
            historyCount = Mathf.Min(historyCount + 1, history.Length);
        }

        private float CalculateAverage()
        {
            if (history == null || historyCount == 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < historyCount; i++)
            {
                total += history[i];
            }

            return total / historyCount;
        }
    }

    [DisallowMultipleComponent]
    public sealed class AudioAnalyzer : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField, Range(128, 2048)] private int sampleCount = 512;

        [Header("Detection")]
        [SerializeField] private BeatDetector kickDetector = new BeatDetector();
        [SerializeField] private BeatDetector snareDetector = new BeatDetector();
        [SerializeField, Range(0.001f, 0.08f)] private float silenceThreshold = 0.008f;
        [SerializeField, Range(0.4f, 3f)] private float silenceHoldSeconds = 0.75f;
        [SerializeField, Range(0.01f, 0.2f)] private float dropEnergyDelta = 0.055f;
        [SerializeField, Range(0.4f, 4f)] private float buildHoldSeconds = 1.2f;
        [SerializeField, Range(0.2f, 5f)] private float majorEventCooldown = 2.2f;

        private float[] spectrum;
        private float[] waveform;
        private float previousOverallEnergy;
        private float smoothedOverallEnergy;
        private float silenceTimer;
        private float buildTimer;
        private float lastMajorEventTime = -10f;
        private AudioMusicalState musicalState = AudioMusicalState.Calm;

        public event Action<AudioReactiveEvent> AudioEventDetected;

        public AudioSource Source => audioSource;
        public float BassEnergy { get; private set; }
        public float MidEnergy { get; private set; }
        public float HighEnergy { get; private set; }
        public float OverallEnergy { get; private set; }
        public float EnergyDelta { get; private set; }
        public float BeatConfidence { get; private set; }
        public float BpmEstimate => kickDetector != null ? kickDetector.BpmEstimate : 0f;
        public bool BeatDetected { get; private set; }
        public AudioMusicalState MusicalState => musicalState;
        public AudioAnalysisSnapshot Snapshot => new AudioAnalysisSnapshot(
            BassEnergy,
            MidEnergy,
            HighEnergy,
            OverallEnergy,
            EnergyDelta,
            BeatConfidence,
            BpmEstimate,
            BeatDetected,
            musicalState);

        public void Configure(AudioSource source)
        {
            SetAudioSource(source);
        }

        public void SetAudioSource(AudioSource source)
        {
            if (audioSource == source)
            {
                return;
            }

            audioSource = source;
            Resync();
        }

        public void Resync()
        {
            kickDetector?.Reset();
            snareDetector?.Reset();
            previousOverallEnergy = 0f;
            smoothedOverallEnergy = 0f;
            silenceTimer = 0f;
            buildTimer = 0f;
            lastMajorEventTime = -10f;
            musicalState = AudioMusicalState.Calm;
        }

        private void Update()
        {
            EnsureBuffers();
            AnalyzeAudio();
        }

        private void EnsureBuffers()
        {
            int safeCount = Mathf.NextPowerOfTwo(Mathf.Clamp(sampleCount, 128, 2048));
            if (spectrum == null || spectrum.Length != safeCount)
            {
                spectrum = new float[safeCount];
                waveform = new float[safeCount];
            }
        }

        private void AnalyzeAudio()
        {
            BeatDetected = false;
            if (audioSource == null || !audioSource.isPlaying)
            {
                BassEnergy = 0f;
                MidEnergy = 0f;
                HighEnergy = 0f;
                EnergyDelta = 0f;
                OverallEnergy = Mathf.MoveTowards(OverallEnergy, 0f, Time.unscaledDeltaTime * 0.7f);
                UpdateMusicalState(false, false, false);
                return;
            }

            audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
            audioSource.GetOutputData(waveform, 0);

            CalculateBands();
            float rms = CalculateRms();
            OverallEnergy = Mathf.Clamp01(rms * 1.6f + (BassEnergy + MidEnergy + HighEnergy) * 0.32f);
            smoothedOverallEnergy = Mathf.Lerp(smoothedOverallEnergy, OverallEnergy, 0.08f);
            EnergyDelta = OverallEnergy - previousOverallEnergy;
            previousOverallEnergy = Mathf.Lerp(previousOverallEnergy, OverallEnergy, 0.35f);

            float now = Time.unscaledTime;
            bool kick = kickDetector != null && kickDetector.Process(BassEnergy, now);
            bool snare = snareDetector != null && snareDetector.Process(HighEnergy + MidEnergy * 0.45f, now);
            BeatConfidence = kickDetector != null ? kickDetector.Confidence : 0f;
            BeatDetected = kick;

            bool silence = OverallEnergy < silenceThreshold;
            bool build = !silence && EnergyDelta > 0.006f && OverallEnergy > smoothedOverallEnergy * 1.08f;
            bool drop = buildTimer >= buildHoldSeconds &&
                        EnergyDelta > dropEnergyDelta &&
                        kick &&
                        now - lastMajorEventTime > majorEventCooldown;

            if (kick)
            {
                Emit(BeatConfidence > 0.65f ? AudioReactiveEventType.StrongBeat : AudioReactiveEventType.Beat, BeatConfidence, now);
            }
            else if (snare)
            {
                Emit(AudioReactiveEventType.Snare, snareDetector.Confidence, now);
            }

            if (drop)
            {
                lastMajorEventTime = now;
                Emit(AudioReactiveEventType.Drop, Mathf.Clamp01(BeatConfidence + EnergyDelta * 4f), now);
            }

            UpdateMusicalState(silence, build, drop);
        }

        private void CalculateBands()
        {
            float bass = 0f;
            float mid = 0f;
            float high = 0f;
            int bassCount = 0;
            int midCount = 0;
            int highCount = 0;
            float nyquist = Mathf.Max(1f, AudioSettings.outputSampleRate * 0.5f);
            for (int i = 1; i < spectrum.Length; i++)
            {
                float frequency = i * nyquist / spectrum.Length;
                float value = Mathf.Sqrt(Mathf.Max(0f, spectrum[i]));
                if (frequency < 160f)
                {
                    bass += value;
                    bassCount++;
                }
                else if (frequency < 2200f)
                {
                    mid += value;
                    midCount++;
                }
                else
                {
                    high += value;
                    highCount++;
                }
            }

            BassEnergy = Mathf.Clamp01(bassCount > 0 ? bass / bassCount * 8f : 0f);
            MidEnergy = Mathf.Clamp01(midCount > 0 ? mid / midCount * 6f : 0f);
            HighEnergy = Mathf.Clamp01(highCount > 0 ? high / highCount * 5f : 0f);
        }

        private float CalculateRms()
        {
            if (waveform == null || waveform.Length == 0)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < waveform.Length; i++)
            {
                total += waveform[i] * waveform[i];
            }

            return Mathf.Sqrt(total / waveform.Length);
        }

        private void UpdateMusicalState(bool silence, bool build, bool drop)
        {
            float dt = Time.unscaledDeltaTime;
            if (silence)
            {
                silenceTimer += dt;
                buildTimer = 0f;
            }
            else
            {
                silenceTimer = 0f;
                buildTimer = build ? buildTimer + dt : Mathf.Max(0f, buildTimer - dt * 0.65f);
            }

            AudioMusicalState previous = musicalState;
            if (silenceTimer >= silenceHoldSeconds)
            {
                musicalState = AudioMusicalState.Silence;
            }
            else if (OverallEnergy < silenceThreshold * 2.8f && previous != AudioMusicalState.Silence)
            {
                musicalState = AudioMusicalState.Break;
            }
            else if (drop)
            {
                musicalState = AudioMusicalState.Drop;
            }
            else if (buildTimer >= buildHoldSeconds * 0.35f)
            {
                musicalState = AudioMusicalState.Build;
            }
            else
            {
                musicalState = AudioMusicalState.Calm;
            }

            float now = Time.unscaledTime;
            if (musicalState != previous)
            {
                if (musicalState == AudioMusicalState.Silence)
                {
                    Emit(AudioReactiveEventType.Silence, 1f, now);
                }
                else if (musicalState == AudioMusicalState.Break)
                {
                    Emit(AudioReactiveEventType.Break, 0.8f, now);
                }
                else if (musicalState == AudioMusicalState.Build)
                {
                    Emit(AudioReactiveEventType.Build, Mathf.Clamp01(buildTimer / Mathf.Max(0.1f, buildHoldSeconds)), now);
                }
            }
        }

        private void Emit(AudioReactiveEventType type, float confidence, float time)
        {
            AudioEventDetected?.Invoke(new AudioReactiveEvent(type, time, Mathf.Clamp01(confidence), BassEnergy, OverallEnergy));
        }
    }
}
