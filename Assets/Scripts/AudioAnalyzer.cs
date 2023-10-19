using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

[RequireComponent(typeof(AudioSource))]
class Peak
{
    public float amplitude;
    public int index;
 
    public Peak()
    {
        amplitude = 0f;
        index = -1;
    }
 
    public Peak(float _frequency, int _index)
    {
        amplitude = _frequency;
        index = _index;
    }
}
 
class AmpComparer : IComparer<Peak>
{
    public int Compare(Peak a, Peak b)
    {
        return 0 - a.amplitude.CompareTo(b.amplitude);
    }
}
 
class IndexComparer : IComparer<Peak>
{
    public int Compare(Peak a, Peak b)
    {
        return a.index.CompareTo(b.index);
    }
}
 
[RequireComponent(typeof(AudioSource))]
public class AudioAnalyzer : MonoBehaviour
{
    public float rmsValue;
    public float dbValue;
    public float pitchValue;

    public int qSamples = 1024;
    public int binSize = 8192;
    public float refValue = 0.1f;
    public float threshold = 0.01f;
    public AudioClip test;

    private List<Peak> peaks = new List<Peak>();
    float[] samples;
    float[] spectrum;
    int samplerate;

    public TMP_Text display;
    public bool mute = true;
    public AudioMixer masterMixer;

    private string _previousNote;
    private bool _isPlaying;

    public GameObject _naturalNotes;
    public GameObject _sharpNotesFlatNotes;
    private Dictionary<string, GameObject> activeKeys = new Dictionary<string, GameObject>();


    // Déclaration du délégué et de l'événement pour le changement de note
    public delegate void NoteChangedEventHandler(string newNote);
    public event NoteChangedEventHandler NoteChanged;

    void Start()
    {
        _isPlaying = false;
        samples = new float[qSamples];
        spectrum = new float[binSize];
        samplerate = AudioSettings.outputSampleRate;

        GetComponent<AudioSource>().loop = true;
        GetComponent<AudioSource>().Play();
        GetComponent<AudioSource>().PlayOneShot(test);

        masterMixer.SetFloat("masterVolume", -80f);
    }

    private void Awake()
    {
        NoteChanged += HandleNoteChanged;
    }

    private void OnDestroy()
    {
        NoteChanged -= HandleNoteChanged;    }

    void Update()
    {
        AnalyzeSound();
    }

    void AnalyzeSound()
    {
        float[] samples = new float[qSamples];
        GetComponent<AudioSource>().GetOutputData(samples, 0);
        int i = 0;
        float sum = 0f;
        for (i = 0; i < qSamples; i++)
        {
            sum += samples[i] * samples[i];
        }
        rmsValue = Mathf.Sqrt(sum / qSamples);
        dbValue = 20 * Mathf.Log10(rmsValue / refValue);
        if (dbValue < -160) dbValue = -160;

        GetComponent<AudioSource>().GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        float maxV = 0f;
        for (i = 0; i < binSize; i++)
        {
            if (spectrum[i] > maxV && spectrum[i] > threshold)
            {
                peaks.Add(new Peak(spectrum[i], i));
                if (peaks.Count > 5)
                {
                    peaks.Sort(new AmpComparer());
                }
            }
        }
        float freqN = 0f;
        string detectedNote = "Unknown";

        if (peaks.Count > 0)
        {
            maxV = peaks[0].amplitude;
            int maxN = peaks[0].index;
            freqN = maxN;

            if (maxN > 0 && maxN < binSize - 1)
            {
                var dL = spectrum[maxN - 1] / spectrum[maxN];
                var dR = spectrum[maxN + 1] / spectrum[maxN];
                freqN += 0.5f * (dR * dR - dL * dL);
                if (freqN == 0)
                {
                    _isPlaying = false;
                    NoteChanged?.Invoke("");

                }
                else
                {
                    _isPlaying = true;
                }
            }
            pitchValue = freqN * (samplerate / 2f) / binSize;

            foreach (var kvp in noteFrequencies)
            {
                float minFrequency = kvp.Value - 5.0f;
                float maxFrequency = kvp.Value + 5.0f;

                if (pitchValue >= minFrequency && pitchValue <= maxFrequency)
                {
                    detectedNote = kvp.Key;

                    // Vérification du changement de note et déclenchement de l'événement
                    if (detectedNote != _previousNote)
                    {
                        _previousNote = detectedNote;
                        NoteChanged?.Invoke(detectedNote);
                    }
                    break;
                }
            }
        }
       

        peaks.Clear();
        if (detectedNote != "Unknown")
        {
            GameObject pianoKey;
            if (!activeKeys.ContainsKey(detectedNote))
            {
                // Instancier un nouveau GameObject pour la touche de piano
                pianoKey = Instantiate(_naturalNotes, new Vector3(0, 0, 0), Quaternion.identity);
                activeKeys.Add(detectedNote, pianoKey);
            }
            else
            {
                pianoKey = activeKeys[detectedNote];
            }

            var pianoKeyAnimation = pianoKey.GetComponent<PianoKeyAnimation>();
            if (_isPlaying)
            {
                pianoKeyAnimation.PlayNote();
            }
            else
            {
                pianoKeyAnimation.StopNote();
                activeKeys.Remove(detectedNote);
            }
        }

        // Arrêtez de jouer les notes qui ne sont plus détectées
        var notesToRemove = new List<string>();
        foreach (var kvp in activeKeys)
        {
            if (kvp.Key != detectedNote || !_isPlaying)
            {
                var pianoKeyAnimation = kvp.Value.GetComponent<PianoKeyAnimation>();
                pianoKeyAnimation.StopNote();
                notesToRemove.Add(kvp.Key);
            }
        }

        foreach (var note in notesToRemove)
        {
            activeKeys.Remove(note);
        }

        if (display != null)
        {
            display.text = "RMS: " + rmsValue.ToString("F2") +
                           " (" + dbValue.ToString("F1") + " dB)\n" +
                           "Pitch: " + pitchValue.ToString("F0") + " Hz\n" +
                           "Detected Note: " + detectedNote;
            Debug.Log("note: " + detectedNote);

        }
    }
    
    
    private void HandleNoteChanged(string newNote)
    {
       // Debug.Log("nouvelle note: " + newNote);
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
        {"C4", 261.63f}, // Middle C
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
        {"C8", 4186.01f} // Highest C
    };

}