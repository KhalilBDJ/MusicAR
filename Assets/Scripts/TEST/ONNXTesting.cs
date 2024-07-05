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
        _worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, _runtimeModel);
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
            { "note", new List<float[]>() },
            { "onset", new List<float[]>() }
        };

        foreach (float[] segment in segments)
        {
            TensorFloat tensor = CreateTensor(segment);
            _worker.Execute(tensor);

            TensorFloat notesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            notesTensor.CompleteOperationsAndDownload();
            onsetsTensor.CompleteOperationsAndDownload();

            if (notesTensor != null && onsetsTensor != null)
            {
                outputs["note"].Add(notesTensor.ToReadOnlyArray());
                outputs["onset"].Add(onsetsTensor.ToReadOnlyArray());
            }
        }

        float[][] frames = UnwrapOutput(outputs["note"].ToArray(), samples.Length, nOverlappingFrames);
        float[][] onsets = UnwrapOutput(outputs["onset"].ToArray(), samples.Length, nOverlappingFrames);

        List<Tuple<int, int, int, float>> estimatedNotes = OutputToNotesPolyphonic(
            frames,
            onsets,
            onset_thresh: 0.5f,
            frame_thresh: 0.3f,
            min_note_len: 128,
            infer_onsets: true,
            min_freq: null,
            max_freq: null,
            melodia_trick: true
        );
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

    private float[][] UnwrapOutput(float[][] output, int audioOriginalLength, int nOverlappingFrames)
    {
        int nOlap = nOverlappingFrames / 2;
        if (nOlap > 0)
        {
            output = output.Select(x => x.Skip(nOlap).Take(x.Length - 2 * nOlap).ToArray()).ToArray();
        }

        int nOutputFramesOriginal = (int)Math.Floor(audioOriginalLength * (ANNOTATIONS_FPS / (double)sampleRate));
        float[][] unwrappedOutput = output.SelectMany(x => x).Take(nOutputFramesOriginal).ToArray().Batch(output[0].Length).ToArray();
        return unwrappedOutput;
    }

    private List<Tuple<int, int, int, float>> OutputToNotesPolyphonic(
        float[][] frames,
        float[][] onsets,
        float onset_thresh,
        float frame_thresh,
        int min_note_len,
        bool infer_onsets,
        float? min_freq,
        float? max_freq,
        bool melodia_trick)
    {
        int n_frames = frames.Length;
        ConstrainFrequency(onsets, frames, max_freq, min_freq);

        if (infer_onsets)
        {
            onsets = GetInferredOnsets(onsets, frames);
        }

        List<Tuple<int, int, int, float>> noteEvents = new List<Tuple<int, int, int, float>>();

        // ... Ajout du reste du traitement basé sur votre fonction Python

        return noteEvents;
    }

    private void ConstrainFrequency(float[][] onsets, float[][] frames, float? maxFreq, float? minFreq)
    {
        if (maxFreq.HasValue)
        {
            int maxFreqIdx = (int)Math.Round(HertzToMidi(maxFreq.Value) - MIDI_OFFSET);
            for (int i = 0; i < onsets.Length; i++)
            {
                for (int j = maxFreqIdx; j < onsets[i].Length; j++)
                {
                    onsets[i][j] = 0;
                    frames[i][j] = 0;
                }
            }
        }
        if (minFreq.HasValue)
        {
            int minFreqIdx = (int)Math.Round(HertzToMidi(minFreq.Value) - MIDI_OFFSET);
            for (int i = 0; i < onsets.Length; i++)
            {
                for (int j = 0; j < minFreqIdx; j++)
                {
                    onsets[i][j] = 0;
                    frames[i][j] = 0;
                }
            }
        }
    }

    private float HertzToMidi(float hz)
    {
        return 69 + 12 * Mathf.Log(hz / 440f, 2);
    }

    private float[][] GetInferredOnsets(float[][] onsets, float[][] frames)
    {
        int n_diff = 2;
        List<float[][]> diffs = new List<float[][]>();

        for (int n = 1; n <= n_diff; n++)
        {
            float[][] framesAppended = new float[frames.Length + n][];
            for (int i = 0; i < n; i++)
            {
                framesAppended[i] = new float[frames[0].Length];
            }
            Array.Copy(frames, 0, framesAppended, n, frames.Length);

            float[][] diff = new float[frames.Length][];
            for (int i = n; i < framesAppended.Length; i++)
            {
                diff[i - n] = framesAppended[i].Select((x, j) => x - framesAppended[i - n][j]).ToArray();
            }
            diffs.Add(diff);
        }

        float[][] frameDiff = diffs.Select(x => x.Min().ToArray()).ToArray();
        for (int i = 0; i < frameDiff.Length; i++)
        {
            for (int j = 0; j < frameDiff[i].Length; j++)
            {
                if (frameDiff[i][j] < 0)
                {
                    frameDiff[i][j] = 0;
                }
            }
        }

        for (int i = 0; i < n_diff; i++)
        {
            for (int j = 0; j < frameDiff[i].Length; j++)
            {
                frameDiff[i][j] = 0;
            }
        }

        float maxOnsets = onsets.Max(x => x.Max());
        float maxFrameDiff = frameDiff.Max(x => x.Max());
        for (int i = 0; i < frameDiff.Length; i++)
        {
            for (int j = 0; j < frameDiff[i].Length; j++)
            {
                frameDiff[i][j] = maxOnsets * frameDiff[i][j] / maxFrameDiff;
            }
        }

        float[][] maxOnsetsDiff = onsets.Zip(frameDiff, (x, y) => x.Zip(y, Math.Max).ToArray()).ToArray();
        return maxOnsetsDiff;
    }
}

public static class Extensions
{
    public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int size)
    {
        T[] bucket = null;
        var count = 0;

        foreach (var item in source)
        {
            if (bucket == null)
            {
                bucket = new T[size];
            }

            bucket[count++] = item;

            if (count != size)
            {
                continue;
            }

            yield return bucket;

            bucket = null;
            count = 0;
        }

        if (bucket != null && count > 0)
        {
            yield return bucket.Take(count).ToArray();
        }
    }
}
