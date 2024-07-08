using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Sentis;

public class ONNXTesting : MonoBehaviour
{
    [SerializeField] private ModelAsset _modelAsset;
    [SerializeField] private AudioClip _clip;

    private IWorker _worker;
    private Model _runtimeModel;

    private const int sampleRate = 16000;
    private const int FFT_HOP = 512;
    private const int nOverlappingFrames = 30;
    private const int overlapLen = nOverlappingFrames * FFT_HOP;
    private const int windowSize = 43844;
    private const int hopSize = windowSize - overlapLen;
    private const int MIDI_OFFSET = 21;
    private const int MAX_FREQ_IDX = 87;
    private const int ANNOTATIONS_FPS = 100;

    private void OnEnable()
    {
        _runtimeModel = ModelLoader.Load(_modelAsset);
        _worker = WorkerFactory.CreateWorker(BackendType.CPU, _runtimeModel);
    }

    private void OnDisable()
    {
        _worker.Dispose();
    }

    void Start()
    {
        CaptureAudio();
    }

    private void CaptureAudio()
    {
        float[] samples = new float[_clip.samples * _clip.channels];
        _clip.GetData(samples, 0);

        List<float[]> segments = ProcessSamples(samples);
        Dictionary<string, List<float[]>> outputs = new Dictionary<string, List<float[]>>()
        {
            {"note", new List<float[]>()},
            {"onset", new List<float[]>()}
        };

        foreach (float[] segment in segments)
        {
            TensorFloat tensor = CreateTensor(segment);
            _worker.Execute(tensor);

            TensorFloat notesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            notesTensor.CompleteOperationsAndDownload();
            onsetsTensor.CompleteOperationsAndDownload();
            var test = notesTensor.ToReadOnlyArray();
            var test2 = onsetsTensor.ToReadOnlyArray();
            if (notesTensor != null && onsetsTensor != null)
            {
                outputs["note"].Add(notesTensor.ToReadOnlyArray());
                outputs["onset"].Add(onsetsTensor.ToReadOnlyArray());
            }
        }
    }

    private List<float[]> ProcessSamples(float[] samples)
    {
        List<float[]> segments = new List<float[]>();
        int totalSamples = samples.Length;
        for (int i = 0; i < totalSamples; i += hopSize)
        {
            float[] segment = new float[windowSize];
            Array.Copy(samples, i, segment, 0, Math.Min(windowSize, totalSamples - i));
            if (totalSamples - i < windowSize)
            {
                for (int j = totalSamples - i; j < windowSize; j++)
                {
                    segment[j] = 0;
                }
            }

            segments.Add(segment);
        }

        return segments;
    }

    private TensorFloat CreateTensor(float[] data)
    {
        TensorShape shape = new TensorShape(1, windowSize, 1);
        return new TensorFloat(shape, data);
    }
    

}
