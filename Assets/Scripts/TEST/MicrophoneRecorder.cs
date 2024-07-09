using UnityEngine;
using System.Collections;
using Unity.Sentis;

public class MicrophoneRecorder : MonoBehaviour
{
    [SerializeField] private ModelAsset _modelAsset;

    private IWorker _worker;
    private Model _runtimeModel;

    private const int SampleRate = 22050;
    private const float RecordingLength = 0.1f; // Durée des segments en secondes
    private const int FFT_HOP = 256;
    private const int TargetSampleSize = 43844; // Correspond à AUDIO_N_SAMPLES dans le code Python
    private const int MinFramesForActivation = 11; // Minimum frames to consider a note as played

    private AudioSource audioSource;
    private string microphone;

    private void OnEnable()
    {
        _runtimeModel = ModelLoader.Load(_modelAsset);
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _runtimeModel);
    }

    void Start()
    {
        // Vérifie si des microphones sont disponibles
        if (Microphone.devices.Length > 0)
        {
            Debug.Log("Microphones disponibles :");
            foreach (var device in Microphone.devices)
            {
                Debug.Log(device);
            }

            // Sélectionne le premier microphone disponible
            microphone = Microphone.devices[0];
            audioSource = gameObject.AddComponent<AudioSource>();
            StartCoroutine(RecordMicrophone());
        }
        else
        {
            Debug.LogError("Aucun microphone n'est connecté.");
        }
    }

    private IEnumerator RecordMicrophone()
    {
        while (true)
        {
            // Démarre la capture audio depuis le microphone
            audioSource.clip = Microphone.Start(microphone, true, 1, SampleRate);

            // Attendre que la capture commence
            yield return new WaitUntil(() => Microphone.GetPosition(microphone) > 0);

            // Attendre la durée de l'enregistrement
            yield return new WaitForSeconds(RecordingLength);

            // Récupère les données audio de l'AudioSource
            float[] audioData = new float[audioSource.clip.samples];
            audioSource.clip.GetData(audioData, 0);

            // Crée un tableau de la taille cible avec padding
            float[] paddedData = new float[TargetSampleSize];
            int copyLength = Mathf.Min(audioData.Length, TargetSampleSize);
            System.Array.Copy(audioData, paddedData, copyLength);

            // Affiche le tableau dans la console
            //Debug.Log("Captured audio data: " + string.Join(", ", paddedData));

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

            // Vérifie les activations prolongées des notes
            CheckProlongedNoteActivations(notes2D);

            // Arrêter la capture audio
            Microphone.End(microphone);

            // Pause avant le prochain enregistrement
            //yield return new WaitForSeconds(RecordingLength);
        }
    }

    private void OnDisable()
    {
        _worker.Dispose();
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

    private void CheckProlongedNoteActivations(float[,] onsets2D)
    {
        for (int note = 0; note < onsets2D.GetLength(1); note++)
        {
            int consecutiveFrames = 0;

            for (int frame = 0; frame < onsets2D.GetLength(0); frame++)
            {
                if (onsets2D[frame, note] > 0.5f)
                {
                    consecutiveFrames++;
                    if (consecutiveFrames >= MinFramesForActivation)
                    {
                        Debug.Log($"Note {note} is being played with probability {onsets2D[frame, note]} at frame {frame}");
                        break; // Optionally, break if you only need to detect once per note
                    }
                }
                else
                {
                    consecutiveFrames = 0;
                }
            }
        }
    }
}
