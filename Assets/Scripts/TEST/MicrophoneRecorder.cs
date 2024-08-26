using UnityEngine;
using System.Collections;
using Unity.Sentis;
using System.Collections.Generic;
using System.Linq;
using System;
using Animations;
using SO;
using Unity.VisualScripting;

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

    private List<string> _activeNotes = new List<string>();
    private Dictionary<string, int> noteEnergyCount = new Dictionary<string, int>();

    public event EventHandler<NotePlayedEventArgs> NoteChanged;
    public PianoKeyPool pianoKeyPool;

    private List<string> previousNotes = new List<string>();
    private Dictionary<string, GameObject> activeKeys = new Dictionary<string, GameObject>();

    private bool tutorial;

    private float[] audioData;
    private float[] paddedData;

    public GlobalVariables _globalVariables;

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
        tutorial = GameManager.Instance.isTutorialMode;
    }

    private void Update()
    {
        HandleDetectedNotes(_activeNotes);
    }

    private IEnumerator RecordMicrophone()
    {
        // Démarre la capture audio depuis le microphone
        audioSource.clip = Microphone.Start(microphone, true, 2, SampleRate);

        // Attendre que la capture commence
        yield return new WaitUntil(() => Microphone.GetPosition(microphone) > 0);

        while (true)
        {
            // Récupère les données audio de l'AudioSource
            int micPosition = Microphone.GetPosition(microphone);
            audioSource.clip.GetData(audioData, micPosition);
            
            // Préparer les données comme une seule fenêtre d'entrée
            TensorFloat tensor = CreateTensor(audioData);
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
            UpdateActiveNotes(onsets2D, notes2D, 0.6f, 0.3f);
            
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
        UpdateActiveNotes(onsets2D, notes2D, 0.5f, 0.3f);

    }

    private void OnDisable()
    {
        _worker.Dispose();
        Microphone.End(microphone);
        if (PlayerPrefs.GetString("SelectedSong")!= null)
        {
            string songName = PlayerPrefs.GetString("SelectedSong");
            PlayerPrefs.SetInt("totalNotes_" + songName, _globalVariables.totalNotes);
            PlayerPrefs.SetInt("correctNotes_" + songName, _globalVariables.playerCorrectNotes);
            PlayerPrefs.SetFloat("correctNotesPercentage_" + songName, _globalVariables.playerCorrectNotesPercentage);
        }
        
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

    private void UpdateActiveNotes(float[,] onsets, float[,] notes, float onsetThreshold, float frameThreshold)
    {
        for (int note = 0; note < onsets.GetLength(1); note++)
        {
            string noteName = GetNoteName(note + 21);
            int consecutiveFrames = 0;
            for (int frame = 151; frame < onsets.GetLength(0); frame++)
            {
                if (onsets[frame, note] > onsetThreshold && notes[frame,note] > frameThreshold)
                {
                    consecutiveFrames++;
                    if (consecutiveFrames >= MinFramesForActivation)
                    {
                        // Ajoute l'offset MIDI pour obtenir la note correcte
                        if (!_activeNotes.Contains(noteName))
                        {
                            _activeNotes.Add(noteName);
                        }
                        else
                        {
                            _activeNotes.Remove(noteName);
                            _activeNotes.Add(noteName);
                        }
                    }
                }
                else if(_activeNotes.Contains(noteName) && notes[frame,note] < frameThreshold)
                {
                    _activeNotes.Remove(noteName);
                    consecutiveFrames = 0;
                }
                else
                {
                    consecutiveFrames = 0;
                }
            }
        }
    }

    private void HandleDetectedNotes(List<string> currentNotes)
    {
        List<string> detectedNotes = new List<string>(currentNotes);
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
