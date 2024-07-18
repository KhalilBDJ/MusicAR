using UnityEngine;
using System.Collections;
using Unity.Sentis;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using UnityEngine.Serialization;

public class MicrophoneRecorder : MonoBehaviour
{
    [FormerlySerializedAs("_modelAsset")] [SerializeField] private ModelAsset modelAsset;
    [FormerlySerializedAs("_clip")] [SerializeField] private AudioClip clip;

    private IWorker _worker;
    private Model _runtimeModel;

    private const int SampleRate = 22050;
    private const float RecordingLength = 0.2f; // Durée des segments en secondes
    private const int TargetSampleSize = 43844; // Correspond à AUDIO_N_SAMPLES dans le code Python
    private const int MinFramesForActivation = 3; // Minimum frames to consider une note as played
    private const int EnergyTol = 11; // Tolérance de l'énergie pour maintenir une note active

    private AudioSource _audioSource;
    private string _microphone;

    private readonly Dictionary<string, int> _noteEnergyCount = new Dictionary<string, int>();

    public event EventHandler<NotePlayedEventArgs> NoteChanged;
    public PianoKeyPool pianoKeyPool;

    private List<string> _previousNotes = new List<string>();
    private Dictionary<string, GameObject> activeKeys = new Dictionary<string, GameObject>();

    private bool _tutorial;

    private float[] _audioData;
    private float[] _paddedData;
    private List<string> _activeNotes = new List<string>();
    private Dictionary<int, int> _potentialNotes = new Dictionary<int, int>();

    private void OnEnable()
    {
        _runtimeModel = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _runtimeModel);

        // Initialiser les tableaux pour réutilisation
        _audioData = new float[TargetSampleSize];
        _paddedData = new float[TargetSampleSize];
    }

    void Start()
    {
        // Vérifie si des microphones sont disponibles
        if (Microphone.devices.Length > 0)
        {
            // Sélectionne le premier microphone disponible
            _microphone = Microphone.devices[0];
            _audioSource = gameObject.GetComponent<AudioSource>();
            StartCoroutine(RecordMicrophone());
        }
        else
        {
            Debug.LogError("Aucun microphone n'est connecté.");
        }

        // Initialiser la variable tutorial
        _tutorial = false; //GameManager.Instance.isTutorialMode;
    }

    private IEnumerator RecordMicrophone()
    {
        // Démarre la capture audio depuis le microphone
        _audioSource.clip = Microphone.Start(_microphone, true, 2, SampleRate);

        // Attendre que la capture commence
        yield return new WaitUntil(() => Microphone.GetPosition(_microphone) > 0);

        while (true)
        {
            // Récupère les données audio de l'AudioSource
            int micPosition = Microphone.GetPosition(_microphone);
            int samplesToRead = Mathf.Min((int)(RecordingLength * SampleRate), TargetSampleSize);
            _audioSource.clip.GetData(_audioData, micPosition - samplesToRead);

            Debug.Log(_audioData.Length);

            // Crée un tableau de la taille cible avec padding
            System.Array.Copy(_audioData, _paddedData, Mathf.Min(_audioData.Length, TargetSampleSize));

            // Préparer les données comme une seule fenêtre d'entrée
            TensorFloat tensor = CreateTensor(_paddedData);
            _worker.Execute(tensor);

            TensorFloat notesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            if (notesTensor != null)
            {
                notesTensor.CompleteOperationsAndDownload();
                if (onsetsTensor != null)
                {
                    onsetsTensor.CompleteOperationsAndDownload();
                    var note = notesTensor.ToReadOnlyArray();
                    var onsets = onsetsTensor.ToReadOnlyArray();

                    // Restructure notes and onsets to 2D arrays
                    float[,] notes2D = ReshapeTo2D(note, 172, 88);
                    float[,] onsets2D = ReshapeTo2D(onsets, 172, 88);

                    // Mise à jour des notes jouées
                    UpdateActiveNotes(onsets2D, notes2D, 0.5f, 0.3f, 17, 3, 11);
                }
            }

            // Attendre la durée de l'enregistrement
            yield return new WaitForSeconds(RecordingLength);
        }
    }

    void Update()
    {
        HandleDetectedNotes(_activeNotes);
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

   private void UpdateActiveNotes(float[,] onsets, float[,] notes, float onsetThreshold, float frameThreshold, int nbFrames, int minFramesForActivation, int energyTol)
{
    int framesCount = onsets.GetLength(0); // Total number of frames
    int notesCount = onsets.GetLength(1); // Total number of notes (88 piano keys)
    Dictionary<int, int> newPotentialNotes = new Dictionary<int, int>();

    // Iterate through the last nbFrames of onsets
    for (int i = framesCount - nbFrames; i < framesCount; i++)
    {
        for (int j = 0; j < notesCount; j++)
        {
            if (onsets[i, j] > onsetThreshold)
            {
                if (_potentialNotes.ContainsKey(j))
                {
                    _potentialNotes[j]++;
                }
                else
                {
                    _potentialNotes.Add(j, 1);
                }

                // Check if a note has reached the minimum frames for activation
                if (_potentialNotes[j] >= minFramesForActivation)
                {
                    int startFrame = i - _potentialNotes[j] + 1;
                    int consecutiveNotes = 0;

                    // Check the corresponding frames in notes
                    for (int k = startFrame; k < Math.Min(framesCount, startFrame + energyTol); k++)
                    {
                        if (notes[k, j] > frameThreshold)
                        {
                            consecutiveNotes++;
                            if (consecutiveNotes >= energyTol)
                            {
                                string noteName = GetNoteName(j + 21);
                                if (!_activeNotes.Contains(noteName))
                                {
                                    _activeNotes.Add(noteName);
                                }
                                else
                                {
                                    // Note reactivation: ensure the note is kept active without being reset
                                    _activeNotes.Remove(noteName);
                                    _activeNotes.Add(noteName);
                                }
                                break;
                            }
                        }
                        else
                        {
                            consecutiveNotes = 0;
                        }
                    }
                }
            }
            else
            {
                _potentialNotes.Remove(j);
            }
        }
    }

    // Handle potential notes at the edge of current frame window
    foreach (var potentialNote in _potentialNotes)
    {
        if (potentialNote.Value < minFramesForActivation)
        {
            newPotentialNotes.Add(potentialNote.Key, potentialNote.Value);
        }
    }

    _potentialNotes = newPotentialNotes;

    // Remove notes that are no longer active
    List<string> currentActiveNotes = new List<string>();
    foreach (var note in _activeNotes)
    {
        int midiNumber = GetMidiNumber(note);
        if (notes[framesCount - 1, midiNumber - 21] <= frameThreshold)
        {
            // Note does not meet the threshold to stay active
            continue;
        }
        currentActiveNotes.Add(note);
    }

    _activeNotes = currentActiveNotes;
}


    private void HandleDetectedNotes(List<string> detectedNotes)
    {
        List<string> currentDetectedNotes = new List<string>(detectedNotes);
        var stoppedKeys = _previousNotes.Except(detectedNotes).ToList();

        foreach (var detectedNote in detectedNotes)
        {
            if (!_previousNotes.Contains(detectedNote) && !stoppedKeys.Contains(detectedNote))
            {
                if (_tutorial)
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

        if (!_tutorial)
        {
            foreach (var key in stoppedKeys)
            {
                GameObject stopNote = activeKeys[key];
                var pianoKeyAnimation = stopNote.GetComponentInChildren<PianoKeyAnimation>();
                pianoKeyAnimation.StopNote();
                activeKeys.Remove(key);
            }
            _previousNotes = currentDetectedNotes;
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
    
    private int GetMidiNumber(string noteName)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = int.Parse(noteName.Substring(noteName.Length - 1));
        string note = noteName.Substring(0, noteName.Length - 1);
        int noteIndex = Array.IndexOf(noteNames, note);
        return noteIndex + 12 * (octave + 1);
    }
}
