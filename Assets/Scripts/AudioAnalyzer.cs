using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using TMPro;

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
 
public class AudioAnalyzer : MonoBehaviour
{
 
    public float rmsValue;
    public float dbValue;
    public float pitchValue;
 
    public int qSamples = 1024;
    public int binSize = 8192; // you can change this up, I originally used 8192 for better resolution, but I stuck with 1024 because it was slow-performing on the phone
    public float refValue = 0.1f;
    public float threshold = 0.01f;
    public AudioClip test;
 
 
    private List<Peak> peaks = new List<Peak>();
    float[] samples;
    float[] spectrum;
    int samplerate;
 
    public TMP_Text display; // drag a Text object here to display values
    public bool mute = true;
    public AudioMixer masterMixer; // drag an Audio Mixer here in the inspector
 
 
    void Start()
    {
        samples = new float[qSamples];
        spectrum = new float[binSize];
        samplerate = AudioSettings.outputSampleRate;
 
        // starts the Microphone and attaches it to the AudioSource
        //GetComponent<AudioSource>().clip = Microphone.Start(null, true, 10, samplerate);
        GetComponent<AudioSource>().loop = true; // Set the AudioClip to loop
        //while (!(Microphone.GetPosition(null) > 0)) { } // Wait until the recording has started
        GetComponent<AudioSource>().Play();
        GetComponent<AudioSource>().PlayOneShot(test);
 
        // Mutes the mixer. You have to expose the Volume element of your mixer for this to work. I named mine "masterVolume".
        masterMixer.SetFloat("masterVolume", -80f);
    }
 
    void Update()
    {
        AnalyzeSound();
        /*if (display != null)
        {
            display.text = "RMS: " + rmsValue.ToString("F2") +
                " (" + dbValue.ToString("F1") + " dB)\n" +
                "Pitch: " + pitchValue.ToString("F0") + " Hz";
        }*/
    }
 
    void AnalyzeSound()
    {
        float[] samples = new float[qSamples];
        GetComponent<AudioSource>().GetOutputData(samples, 0); // fill array with samples
        int i = 0;
        float sum = 0f;
        for (i = 0; i < qSamples; i++)
        {
            sum += samples[i] * samples[i]; // sum squared samples
        }
        rmsValue = Mathf.Sqrt(sum / qSamples); // rms = square root of average
        dbValue = 20 * Mathf.Log10(rmsValue / refValue); // calculate dB
        if (dbValue < -160) dbValue = -160; // clamp it to -160dB min
 
        // get sound spectrum
        GetComponent<AudioSource>().GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        float maxV = 0f;
        for (i = 0; i < binSize; i++)
        { // find max
            if (spectrum[i] > maxV && spectrum[i] > threshold)
            {
                peaks.Add(new Peak(spectrum[i], i));
                if (peaks.Count > 5)
                { // get the 5 peaks in the sample with the highest amplitudes
                    peaks.Sort(new AmpComparer()); // sort peak amplitudes from highest to lowest
                    //peaks.Remove (peaks [5]); // remove peak with the lowest amplitude
                }
            }
        }
        float freqN = 0f;
        string detectedNote = "Unknown";

        if (peaks.Count > 0)
        {
            // Trouver la fréquence dominante
            maxV = peaks[0].amplitude;
            int maxN = peaks[0].index;
            freqN = maxN; // Passer l'indice à une variable flottante

            if (maxN > 0 && maxN < binSize - 1)
            { // Interpoler l'indice en utilisant les voisins
                var dL = spectrum[maxN - 1] / spectrum[maxN];
                var dR = spectrum[maxN + 1] / spectrum[maxN];
                freqN += 0.5f * (dR * dR - dL * dL);
            }
            pitchValue = freqN * (samplerate / 2f) / binSize; // Convertir l'indice en fréquence

            // Trouver la note correspondante en fonction de la fréquence
            foreach (var kvp in noteFrequencies)
            {
                float minFrequency = kvp.Value - 5.0f; // Marge d'incertitude inférieure
                float maxFrequency = kvp.Value + 5.0f; // Marge d'incertitude supérieure

                if (pitchValue >= minFrequency && pitchValue <= maxFrequency)
                {
                    detectedNote = kvp.Key;
                    break; // Sortez de la boucle dès que la note est trouvée
                }
            }
        }

        peaks.Clear();

        // Utilisez la variable detectedNote pour afficher la note détectée
        if (display != null)
        {
            display.text = "RMS: " + rmsValue.ToString("F2") +
                           " (" + dbValue.ToString("F1") + " dB)\n" +
                           "Pitch: " + pitchValue.ToString("F0") + " Hz\n" +
                           "Detected Note: " + detectedNote;
        }
    }
    
    Dictionary<string, float> noteFrequencies = new Dictionary<string, float>
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