using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using Classes;
using TMPro;
using Unity.VisualScripting;

[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public bool tutorial;
    public event EventHandler<NotePlayedEventArgs> NoteChanged;


    public AudioSource source;

    public int qSamples = 1024;
    public int binSize = 8192;
    public float refValue = 0.1f;
    public AudioClip test;

    private List<Peak> peaks = new List<Peak>();
    float[] samples;
    float[] spectrum;
    int samplerate;

    //public TMP_Text display;
    public bool mute = true;
    public AudioMixer masterMixer;

    private string _previousNote;
    private bool _isPlaying;
    private float _threshold = 0.03f;
    private YinPitchTracker _yinPitchTracker;
    private AudioPitchEstimator _pitchEstimator;


    public PianoKeyPool pianoKeyPool;

    private Dictionary<string, GameObject> activeKeys = new Dictionary<string, GameObject>();
    
    private List<String> previousNotes = new List<string>();
    void Start()
    {
        _isPlaying = false;
        samples = new float[qSamples];
        spectrum = new float[binSize];
        samplerate = AudioSettings.outputSampleRate;
        //_yinPitchTracker = new YinPitchTracker(1024, 0.10f);
        GetComponent<AudioSource>().loop = true;
        GetComponent<AudioSource>().Play();
        GetComponent<AudioSource>().PlayOneShot(test);
        _pitchEstimator = new AudioPitchEstimator();
        

        masterMixer.SetFloat("masterVolume", -80f);
        
        /*_isPlaying = false;
        samples = new float[qSamples];
        spectrum = new float[binSize];
        samplerate = AudioSettings.outputSampleRate;

        // Initialisation de l'AudioSource avec le microphone
        if (Microphone.devices.Length > 0)
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.clip = Microphone.Start(Microphone.devices[0], true, 10, samplerate);
            while (!(Microphone.GetPosition(null) > 0)) { } // Attendre que le microphone commence
            source.Play();
        }
        else
        {
            Debug.LogError("Aucun microphone détecté");
        }

        _pitchEstimator = new AudioPitchEstimator();*/

        // Mute le son si nécessaire
        //masterMixer.SetFloat("masterVolume", mute ? -80f : 0f);
    }
    
    void Update()
    {
        if (Microphone.IsRecording(null))
        {
            AnalyzeSound();
        }
        else
        {
            AnalyzeSound();
        }
        //Debug.Log(GetDetectedNote(_pitchEstimator.Estimate(source)));
    }
    private void AnalyzeSound()
    {
        GetFrequencies();
    }
    
    
    void OnDestroy()
    {
        Microphone.End(Microphone.devices[0]);
    }

    private void GetRMSAndDBValues(out float rmsValue, out float dbValue)
    {
        GetComponent<AudioSource>().GetOutputData(samples, 0);
        float sum = 0f;
        for (int i = 0; i < qSamples; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / qSamples);

        // Vérification si rmsValue est très petit
        if (rmsValue > 0) // Ajoutez une tolérance si nécessaire
        {
            dbValue = 20 * Mathf.Log10(rmsValue / refValue);
            dbValue = Mathf.Max(dbValue, -160); // Plancher pour dbValue
        }
        else
        {
            dbValue = -160; // Valeur minimale pour éviter -infini
        }
    }


private List<float> GetFrequencies()
{
    float currentRMSValue, currentDBValue;
    GetRMSAndDBValues(out currentRMSValue, out currentDBValue);
    float dynamicThreshold = _threshold * ( 1+ Mathf.Abs(currentDBValue / 160));
    GetComponent<AudioSource>().GetSpectrumData(spectrum, 0, FFTWindow.Hanning);
    var peaks = new List<Peak>();

    for (int i = 0; i < binSize; i++)
    {
        if (spectrum[i] > _threshold)
        {
            peaks.Add(new Peak(spectrum[i], i));
        }
    }

    peaks.Sort((p1, p2) => p2.amplitude.CompareTo(p1.amplitude));

    var frequencies = new List<float>();
    List<String> detectedNotes = new List<string>();
    
    foreach (var peak in peaks)
    {
        float freqN = peak.index;
        if (freqN > 0 && freqN < binSize - 1)
        {
            var dL = spectrum[peak.index - 1] / spectrum[peak.index];
            var dR = spectrum[peak.index + 1] / spectrum[peak.index];
            freqN += 0.5f * (dR * dR - dL * dL);
            float pitchValue = freqN * (samplerate / 2f) / binSize;
            //float pitchValue = _pitchEstimator.Estimate(source);

            if (!IsFrequencySimilar(frequencies, pitchValue))
            {
                frequencies.Add(pitchValue);
                foreach (var frequency in frequencies)
                {
                    var detectedNote = GetDetectedNote(frequency);
                    if (!detectedNotes.Contains(detectedNote) && !detectedNote.Equals("Unknown"))
                    {
                        detectedNotes.Add(detectedNote);
                    }
                }
            }
        }
    }
    
    var stoppedKeys = previousNotes.Except(detectedNotes).ToList();
   
    foreach (var detectedNote in detectedNotes)
    {
        if (!previousNotes.Contains(detectedNote) && !stoppedKeys.Contains(detectedNote))
        {
            if (tutorial)
            {
                NoteChanged?.Invoke(this, new NotePlayedEventArgs(detectedNotes, true));
            }
            else
            {
                GameObject pianoKey = pianoKeyPool.GetNoteObject(detectedNote);
                activeKeys.Add(detectedNote, pianoKey);
                var pianoKeyAnimation = pianoKey.GetComponentInChildren<PianoKeyAnimation>();
                pianoKeyAnimation.PlayNote(detectedNote);
            }
        }
    }

    if (!tutorial)
    {
        foreach (var key in stoppedKeys)
        {
            GameObject stopNote = activeKeys[key];
            var pianoKeyAnimation = stopNote.GetComponentInChildren<PianoKeyAnimation>();
            pianoKeyAnimation.StopNote();
            activeKeys.Remove(key);
        }
        previousNotes = detectedNotes;
    }
   

    return frequencies;
}

private bool IsFrequencySimilar(List<float> frequencies, float newFrequency, float tolerance = 3.0f)
{
    foreach (var frequency in frequencies)
    {
        if (Mathf.Abs(frequency - newFrequency) < tolerance)
        {
            return true;
        }
    }
    return false;
}


private string GetDetectedNote(float frequency)
{
    string detectedNote = "Unknown";
    foreach (var kvp in noteFrequencies)
    {
        float minFrequency = kvp.Value - 1.0f;
        float maxFrequency = kvp.Value + 1.0f;

        if (frequency >= minFrequency && frequency <= maxFrequency)
        {
            detectedNote = kvp.Key;
            break;
        }
    }
    return detectedNote;
}

    
    private Dictionary<string, float> noteFrequencies = new Dictionary<string, float>
    {
        {"C1", 32.70f},
        {"C#1", 34.65f},
        {"D1", 36.71f},
        {"D#1", 38.89f},
        {"E1", 41.20f},
        {"F1", 43.65f},
        {"F#1", 46.25f},
        {"G1", 49.00f},
        {"G#1", 51.91f},
        {"A1", 55.00f},
        {"A#1", 58.27f},
        {"B1", 61.74f},
        {"C2", 65.41f},
        {"C#2", 69.30f},
        {"D2", 73.42f},
        {"D#2", 77.78f},
        {"E2", 82.41f},
        {"F2", 87.31f},
        {"F#2", 92.50f},
        {"G2", 98.00f},
        {"G#2", 103.83f},
        {"A2", 110.00f},
        {"A#2", 116.54f},
        {"B2", 123.47f},
        {"C3", 130.81f},
        {"C#3", 138.59f},
        {"D3", 146.83f},
        {"D#3", 155.56f},
        {"E3", 164.81f},
        {"F3", 174.61f},
        {"F#3", 185.00f},
        {"G3", 196.00f},
        {"G#3", 207.65f},
        {"A3", 220.00f},
        {"A#3", 233.08f},
        {"B3", 246.94f},
        {"C4", 261.63f}, 
        {"C#4", 277.18f},
        {"D4", 293.66f},
        {"D#4", 311.13f},
        {"E4", 329.63f},
        {"F4", 349.23f},
        {"F#4", 369.99f},
        {"G4", 392.00f},
        {"G#4", 415.30f},
        {"A4", 440.00f},
        {"A#4", 466.16f},
        {"B4", 493.88f},
        {"C5", 523.25f},
        {"C#5", 554.37f},
        {"D5", 587.33f},
        {"D#5", 622.25f},
        {"E5", 659.26f},
        {"F5", 698.46f},
        {"F#5", 739.99f},
        {"G5", 783.99f},
        {"G#5", 830.61f},
        {"A5", 880.00f},
        {"A#5", 932.33f},
        {"B5", 987.77f},
        {"C6", 1046.50f},
        {"C#6", 1108.73f},
        {"D6", 1174.66f},
        {"D#6", 1244.51f},
        {"E6", 1318.51f},
        {"F6", 1396.91f},
        {"F#6", 1479.98f},
        {"G6", 1567.98f},
        {"G#6", 1661.22f},
        {"A6", 1760.00f},
        {"A#6", 1864.66f},
        {"B6", 1975.53f},
        {"C7", 2093.00f},
        {"C#7", 2217.46f},
        {"D7", 2349.32f},
        {"D#7", 2489.02f},
        {"E7", 2637.02f},
        {"F7", 2793.83f},
        {"F#7", 2959.96f},
        {"G7", 3135.96f},
        {"G#7", 3322.44f},
        {"A7", 3520.00f},
        {"A#7", 3729.31f},
        {"B7", 3951.07f},
        {"C8", 4186.01f}
    };

}