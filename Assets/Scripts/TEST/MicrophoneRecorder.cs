using UnityEngine;
using System.Collections;
using Unity.Sentis;
using System.Collections.Generic;
using System.Linq;
using System;

public class MicrophoneRecorder : MonoBehaviour
{
    [SerializeField] private ModelAsset _modelAsset;
    [SerializeField] private AudioClip _clip;

    private IWorker _worker;
    private Model _runtimeModel;

    private const int SampleRate = 22050;
    private const float RecordingLength = 0.2f; // Durée des segments en secondes
    private const int TargetSampleSize = 43844; // Correspond à AUDIO_N_SAMPLES dans le code Python
    private const int MinFramesForActivation = 3; // Minimum frames to consider une note as played
    private const int EnergyTol = 11; // Tolérance de l'énergie pour maintenir une note active

    private AudioSource audioSource;
    private string microphone;

    private HashSet<string> activeNotes = new HashSet<string>();
    private Dictionary<string, int> noteEnergyCount = new Dictionary<string, int>();

    public event EventHandler<NotePlayedEventArgs> NoteChanged;
    public PianoKeyPool pianoKeyPool;

    private List<string> previousNotes = new List<string>();
    private Dictionary<string, GameObject> activeKeys = new Dictionary<string, GameObject>();

    private bool tutorial;

    private float[] audioData;
    private float[] paddedData;

    private void OnEnable()
    {
        _runtimeModel = ModelLoader.Load(_modelAsset);
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _runtimeModel);

        // Initialiser les tableaux pour réutilisation
        audioData = new float[TargetSampleSize];
        paddedData = new float[TargetSampleSize];
    }

    void Start()
    {
        // Vérifie si des microphones sont disponibles
        if (Microphone.devices.Length > 0)
        {
            // Sélectionne le premier microphone disponible
            microphone = Microphone.devices[0];
            audioSource = gameObject.GetComponent<AudioSource>();
            StartCoroutine(RecordMicrophone());
        }
        else
        {
            Debug.LogError("Aucun microphone n'est connecté.");
        }

        // Initialiser la variable tutorial
        tutorial = false; //GameManager.Instance.isTutorialMode;
    }

    private IEnumerator RecordMicrophone()
    {
        // Démarre la capture audio depuis le microphone
        audioSource.clip = Microphone.Start(microphone, true, 1, SampleRate);

        // Attendre que la capture commence
        yield return new WaitUntil(() => Microphone.GetPosition(microphone) > 0);

        while (true)
        {
            // Récupère les données audio de l'AudioSource
            int micPosition = Microphone.GetPosition(microphone);
            audioSource.clip.GetData(audioData, micPosition);

            // Crée un tableau de la taille cible avec padding
            System.Array.Copy(audioData, paddedData, Mathf.Min(audioData.Length, TargetSampleSize));

            // Préparer les données comme une seule fenêtre d'entrée
            TensorFloat tensor = CreateTensor(paddedData);
            _worker.Execute(tensor);

            TensorFloat notesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            notesTensor.CompleteOperationsAndDownload();
            onsetsTensor.CompleteOperationsAndDownload();
            var note = notesTensor.ToReadOnlyArray();
            var onsets = onsetsTensor.ToReadOnlyArray();

            // Restructure notes and onsets to 2D arrays
            float[,] notes2D = ReshapeTo2D(note, 172, 88);
            float[,] onsets2D = ReshapeTo2D(onsets, 172, 88);

            // Mise à jour des notes jouées
            List<string> detectedNotes = UpdateActiveNotes(onsets2D, notes2D, 0.6f, 0.3f);
            foreach (var notes in detectedNotes)
            {
                Debug.Log(notes);
            }

            // Gérer les notes détectées et arrêtées
            HandleDetectedNotes(detectedNotes);
            
            // Attendre la durée de l'enregistrement
            yield return new WaitForSeconds(RecordingLength);
        }
    }

    private IEnumerator Testing()
    {
        yield return new WaitForSeconds(1f);

        // Démarre la capture audio depuis l'audio clip
        audioSource.clip = _clip;
        audioSource.Play();

        // Récupère les données audio de l'AudioSource
        float[] audioData = new float[audioSource.clip.samples];
        audioSource.clip.GetData(audioData, 0);

        // Crée un tableau de la taille cible avec padding
        float[] paddedData = new float[TargetSampleSize];
        int copyLength = Mathf.Min(audioData.Length, TargetSampleSize);
        System.Array.Copy(audioData, paddedData, copyLength);

        // Préparer les données comme une seule fenêtre d'entrée
        TensorFloat tensor = CreateTensor(paddedData);
        _worker.Execute(tensor);

        TensorFloat notesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
        TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
        notesTensor.CompleteOperationsAndDownload();
        onsetsTensor.CompleteOperationsAndDownload();
        var note = notesTensor.ToReadOnlyArray();
        var onsets = onsetsTensor.ToReadOnlyArray();

        // Restructure notes and onsets to 2D arrays
        float[,] notes2D = ReshapeTo2D(note, 172, 88);
        float[,] onsets2D = ReshapeTo2D(onsets, 172, 88);

        // Mise à jour des notes jouées
        List<string> detectedNotes = UpdateActiveNotes(onsets2D, notes2D, 0.5f, 0.3f);

        // Gérer les notes détectées et arrêtées
        HandleDetectedNotes(detectedNotes);
    }

    private void OnDisable()
    {
        _worker.Dispose();
        Microphone.End(microphone);
    }

    private TensorFloat CreateTensor(float[] data)
    {
        TensorShape shape = new TensorShape(1, TargetSampleSize, 1);
        return new TensorFloat(shape, data);
    }

    private float[,] ReshapeTo2D(float[] array, int rows, int cols)
    {
        float[,] result = new float[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = array[i * cols + j];
            }
        }
        return result;
    }

    private List<string> UpdateActiveNotes(float[,] onsets, float[,] notes, float onsetThreshold, float frameThreshold)
    {
        HashSet<string> newActiveNotes = new HashSet<string>();
        for (int note = 0; note < onsets.GetLength(1); note++)
        {
            int consecutiveFrames = 0;
            for (int frame = 0; frame < onsets.GetLength(0); frame++)
            {
                if (onsets[frame, note] > onsetThreshold)
                {
                    consecutiveFrames++;
                    if (consecutiveFrames >= MinFramesForActivation)
                    {
                        string noteName = GetNoteName(note + 21); // Ajoute l'offset MIDI pour obtenir la note correcte
                        newActiveNotes.Add(noteName);
                        noteEnergyCount[noteName] = EnergyTol;
                        break;
                    }
                }
                else if (notes[frame, note] > frameThreshold)
                {
                    string noteName = GetNoteName(note + 21);
                    if (activeNotes.Contains(noteName))
                    {
                        newActiveNotes.Add(noteName);
                        noteEnergyCount[noteName]--;
                        if (noteEnergyCount[noteName] <= 0)
                        {
                            activeNotes.Remove(noteName);
                            noteEnergyCount.Remove(noteName);
                        }
                    }
                }
                else
                {
                    consecutiveFrames = 0;
                }
            }
        }

        var detectedNotes = newActiveNotes.ToList();
        return detectedNotes;
    }

    private void HandleDetectedNotes(List<string> detectedNotes)
    {
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
    }

    private string GetNoteName(int midiNumber)
    {
        // Convertir le numéro MIDI en nom de note (par exemple, 60 -> C4)
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (midiNumber / 12) - 1;
        int noteIndex = midiNumber % 12;
        return noteNames[noteIndex] + octave;
    }
}
