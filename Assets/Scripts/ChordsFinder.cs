using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class ChordsFinder : MonoBehaviour
{
    private AudioSource audioSource;
    public FFTWindow windowType;
    private float[] spectrumData;
    private const int sampleSize = 1024;
    public AudioClip test;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = test;//Microphone.Start(null, true, 10, 44100);
        audioSource.loop = true;
        audioSource.mute = true;  // Mute the sound, we don't want the player to hear it
        //while (!(Microphone.GetPosition(null) > 0)) { }  // Wait until the recording has started
        audioSource.Play();
        audioSource.PlayOneShot(test);// Play the audio source!
    }

    void Update()
    {
        spectrumData = new float[sampleSize];
        audioSource.GetSpectrumData(spectrumData, 0, windowType);
        List<float> peakFrequencies = FindPeakFrequencies(spectrumData);
        foreach (float freq in peakFrequencies)
        {
            UnityEngine.Debug.Log("Note Frequency: " + freq);
            
        }
    }

    List<float> FindPeakFrequencies(float[] spectrum)
    {
        List<float> peakFrequencies = new List<float>();
        for (int i = 6; i < spectrum.Length - 1; i++)  // Starting at 6 to avoid low-frequency noise
        {
            if (spectrum[i] > spectrum[i - 1] && spectrum[i] > spectrum[i + 1])  // A peak
            {
                float freq = i * AudioSettings.outputSampleRate / 2f / spectrum.Length;  // Convert index to frequency
                peakFrequencies.Add(freq);
            }
        }
        return peakFrequencies;
    }
}