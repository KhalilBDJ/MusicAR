using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;

public class ONNXTesting : MonoBehaviour
{
    [SerializeField] private ModelAsset _modelAsset;
    [SerializeField] private AudioClip _clip;

    private IWorker _worker;
    private Model _runtimeModel;

    private const int sampleRate = 16000; // Example sample rate, change if needed
    private const int windowSize = 43844; // Fixed window size
    private const int hopSize = 43844 - 30 * 512; // Overlap size is 30 frames * FFT_HOP (e.g., 512)
    private const int MIDI_OFFSET = 21;
    private const int MAX_FREQ_IDX = 87;

    private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

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
        // Use the provided AudioClip
        float[] samples = new float[_clip.samples * _clip.channels];
        _clip.GetData(samples, 0);

        // Process and segment the samples
        List<float[]> segments = ProcessSamples(samples);

        // Run inference on each segment
        foreach (float[] segment in segments)
        {
            TensorFloat tensor = CreateTensor(segment);
            _worker.Execute(tensor);

            TensorFloat framesTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            framesTensor.CompleteOperationsAndDownload();
            onsetsTensor.CompleteOperationsAndDownload();

            if (framesTensor != null && onsetsTensor != null)
            {
                float[] framesData = framesTensor.ToReadOnlyArray();
                float[] onsetsData = onsetsTensor.ToReadOnlyArray();
                
                int numFrames = framesData.Length / (MAX_FREQ_IDX + 1);
                int numOnsets = onsetsData.Length / (MAX_FREQ_IDX + 1);
                
                float[,] framesArray = ConvertTo2DArray(framesData, numFrames, MAX_FREQ_IDX + 1);
                float[,] onsetsArray = ConvertTo2DArray(onsetsData, numOnsets, MAX_FREQ_IDX + 1);

                List<(float, float, int, float)> noteEvents = OutputToNotesPolyphonic(framesArray, onsetsArray, 0.5f, 0.5f, 10, true, null, null, true, 11);

                foreach (var note in noteEvents)
                {
                    Debug.Log($"Note: Start: {note.Item1}, End: {note.Item2}, Pitch: {note.Item3}, Amplitude: {note.Item4}");
                }
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
                // Zero pad the last segment
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
        // Ensure the data array fits the tensor shape
        TensorShape shape = new TensorShape(1, windowSize, 1);

        // Create and return the tensor
        return new TensorFloat(shape, data);
    }

    private float[,] ConvertTo2DArray(float[] data, int rows, int cols)
    {
        float[,] result = new float[rows, cols];
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = data[i * cols + j];
            }
        }
        return result;
    }

    private List<(float, float, int, float)> OutputToNotesPolyphonic(float[,] frames, float[,] onsets, float onsetThresh, float frameThresh, int minNoteLen, bool inferOnsets, float? maxFreq, float? minFreq, bool melodiaTrick, int energyTol)
    {
        int nFrames = frames.GetLength(0);
        int nFreqs = frames.GetLength(1);

        // Constrain frequencies
        ConstrainFrequency(ref onsets, ref frames, maxFreq, minFreq);

        // Infer onsets if needed
        if (inferOnsets)
        {
            onsets = GetInferredOnsets(onsets, frames);
        }

        // Detect peaks
        float[,] peakThreshMat = new float[nFrames, nFreqs];
        for (int j = 0; j < nFreqs; j++)
        {
            for (int i = 1; i < nFrames - 1; i++)
            {
                if (onsets[i, j] > onsets[i - 1, j] && onsets[i, j] > onsets[i + 1, j] && onsets[i, j] >= onsetThresh)
                {
                    peakThreshMat[i, j] = onsets[i, j];
                }
            }
        }

        // Find onsets
        List<(float, float, int, float)> noteEvents = new List<(float, float, int, float)>();
        float[,] remainingEnergy = (float[,])frames.Clone();
        for (int j = 0; j < nFreqs; j++)
        {
            for (int i = nFrames - 2; i >= 0; i--)
            {
                if (peakThreshMat[i, j] >= onsetThresh)
                {
                    int noteStartIdx = i;
                    int k = 0;
                    while (i < nFrames - 1 && k < energyTol)
                    {
                        if (remainingEnergy[i, j] < frameThresh)
                        {
                            k++;
                        }
                        else
                        {
                            k = 0;
                        }
                        i++;
                    }
                    int noteEndIdx = i - k;

                    if (noteEndIdx - noteStartIdx >= minNoteLen)
                    {
                        float amplitude = 0;
                        for (int m = noteStartIdx; m < noteEndIdx; m++)
                        {
                            amplitude += frames[m, j];
                        }
                        amplitude /= (noteEndIdx - noteStartIdx);

                        int pitch = j + MIDI_OFFSET;
                        Debug.Log($"Frequency Index: {j}, MIDI Pitch: {pitch}");

                        noteEvents.Add((noteStartIdx / (float)sampleRate, noteEndIdx / (float)sampleRate, pitch, amplitude));
                    }

                    i = noteStartIdx;
                }
            }
        }

        // Apply melodia trick
        if (melodiaTrick)
        {
            while (true)
            {
                float maxVal = 0;
                int maxIdx = -1, maxFreqIdx = -1;
                for (int j = 0; j < nFreqs; j++)
                {
                    for (int i = 0; i < nFrames; i++)
                    {
                        if (remainingEnergy[i, j] > maxVal)
                        {
                            maxVal = remainingEnergy[i, j];
                            maxIdx = i;
                            maxFreqIdx = j;
                        }
                    }
                }

                if (maxVal < frameThresh)
                    break;

                int startIdx = maxIdx;
                int k = 0;
                while (startIdx > 0 && k < energyTol)
                {
                    if (remainingEnergy[startIdx, maxFreqIdx] < frameThresh)
                    {
                        k++;
                    }
                    else
                    {
                        k = 0;
                    }
                    startIdx--;
                }
                startIdx += k;

                int endIdx = maxIdx;
                k = 0;
                while (endIdx < nFrames - 1 && k < energyTol)
                {
                    if (remainingEnergy[endIdx, maxFreqIdx] < frameThresh)
                    {
                        k++;
                    }
                    else
                    {
                        k = 0;
                    }
                    endIdx++;
                }
                endIdx -= k;

                if (endIdx - startIdx >= minNoteLen)
                {
                    float amplitude = 0;
                    for (int m = startIdx; m < endIdx; m++)
                    {
                        amplitude += frames[m, maxFreqIdx];
                    }
                    amplitude /= (endIdx - startIdx);

                    int pitch = maxFreqIdx + MIDI_OFFSET;
                    Debug.Log($"Melodia Trick - Frequency Index: {maxFreqIdx}, MIDI Pitch: {pitch}");

                    noteEvents.Add((startIdx / (float)sampleRate, endIdx / (float)sampleRate, pitch, amplitude));
                }

                for (int m = startIdx; m < endIdx; m++)
                {
                    remainingEnergy[m, maxFreqIdx] = 0;
                }
            }
        }

        return noteEvents;
    }

    private void ConstrainFrequency(ref float[,] onsets, ref float[,] frames, float? maxFreq, float? minFreq)
    {
        int maxFreqIdx = maxFreq.HasValue ? Mathf.RoundToInt(HZToMidi(maxFreq.Value) - MIDI_OFFSET) : MAX_FREQ_IDX;
        int minFreqIdx = minFreq.HasValue ? Mathf.RoundToInt(HZToMidi(minFreq.Value) - MIDI_OFFSET) : 0;

        for (int i = 0; i < onsets.GetLength(0); i++)
        {
            for (int j = 0; j < onsets.GetLength(1); j++)
            {
                if (j > maxFreqIdx || j < minFreqIdx)
                {
                    onsets[i, j] = 0;
                    frames[i, j] = 0;
                }
            }
        }
    }

    private float HZToMidi(float hz)
    {
        return 69 + 12 * Mathf.Log(hz / 440.0f, 2);
    }

    private float[,] GetInferredOnsets(float[,] onsets, float[,] frames)
    {
        int nFrames = frames.GetLength(0);
        int nFreqs = frames.GetLength(1);
        float[,] inferredOnsets = (float[,])onsets.Clone();

        for (int j = 0; j < nFreqs; j++)
        {
            float[] diffs = new float[nFrames];
            for (int i = 1; i < nFrames; i++)
            {
                diffs[i] = frames[i, j] - frames[i - 1, j];
                if (diffs[i] < 0) diffs[i] = 0;
            }
            float maxDiff = Mathf.Max(diffs);
            if (maxDiff > 0)
            {
                for (int i = 0; i < nFrames; i++)
                {
                    diffs[i] = onsets[i, j] * diffs[i] / maxDiff;
                    inferredOnsets[i, j] = Mathf.Max(inferredOnsets[i, j], diffs[i]);
                }
            }
        }

        return inferredOnsets;
    }
}
