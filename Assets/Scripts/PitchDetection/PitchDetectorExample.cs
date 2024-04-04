using System.Collections.Generic;
using TMPro;

namespace PitchDetection
{
    using UnityEngine;

    public class PitchDetectorExample : MonoBehaviour
    {
        public AudioClip audioClip;
        public float threshold = 0.01f;
        public int bufferSize = 1024;
        public TMP_Text currentNote;

        private AudioSource audioSource;
        private PitchDetector pitchDetector;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Configurez l'AudioSource pour lire l'audio du microphone
            audioSource.loop = true; // Boucle l'audio pour une capture continue
            audioSource.mute = false; // Mettez à true si vous ne voulez pas que l'audio du micro soit audible

            // Commencez à capturer l'audio du microphone
            StartMicrophone();

            pitchDetector = new PitchDetector(bufferSize, AudioSettings.outputSampleRate, threshold);
        }
        
        void StartMicrophone()
        {
            audioSource.clip = Microphone.Start(null, true, 10, AudioSettings.outputSampleRate);
            // Attendre que le microphone commence à capturer
            while (!(Microphone.GetPosition(null) > 0)) { }
            audioSource.Play();
        }
        
        void OnDisable()
        {
            // Arrêter le microphone lors de la désactivation du script
            Microphone.End(null);
        }
        

        void Update()
        {
            //audioSource.Play();

            if (Microphone.IsRecording(null))
            {
                float[] audioData = new float[bufferSize];
                audioSource.GetOutputData(audioData, 0);

                if (audioData.Length > 0)
                {
                    PitchResult pitchResult = pitchDetector.DetectPitch(audioData);
                    float pitch = pitchResult.Pitch;
                    float confidence = pitchResult.Confidence;

                    // Map pitch to note name
                    string noteName = "";
                    foreach (KeyValuePair<string, float> kvp in noteFrequencies)
                    {
                        if (kvp.Value > pitch * 0.95 && kvp.Value < pitch * 1.05)
                        {
                            noteName = kvp.Key;
                            break;
                        }
                    }

                    // Display note name
                    Debug.Log("Note: " + noteName + " Confidence: " + confidence);
                    currentNote.text = "Note: " + noteName + " Confidence: " + confidence;
                }
            }
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
}