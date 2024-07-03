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

            TensorFloat framesTensor = _worker.PeekOutput("StatefulPartitionedCall") as TensorFloat;
            TensorFloat onsetsTensor = _worker.PeekOutput("StatefulPartitionedCall:1") as TensorFloat;
            TensorFloat contoursTensor = _worker.PeekOutput("StatefulPartitionedCall:2") as TensorFloat;
            framesTensor.CompleteOperationsAndDownload();
            onsetsTensor.CompleteOperationsAndDownload();
            contoursTensor.CompleteOperationsAndDownload();

            if (framesTensor != null && onsetsTensor != null)
            {
                float[] framesData = framesTensor.ToReadOnlyArray();
                float[] onsetsData = onsetsTensor.ToReadOnlyArray();

                // Decode the model output to note events
                List<Tuple<int, int, int, float>> noteEvents = OutputToNotesPolyphonic(
                    framesData, onsetsData, 0.5f, 0.3f, 127, true, 2000f, 30f, true, 11);

                // Handle note events (e.g., print them or store them)
                foreach (var noteEvent in noteEvents)
                {
                    string noteName = ConvertMidiToNoteName(noteEvent.Item3);
                    Debug.Log($"Start: {noteEvent.Item1}, End: {noteEvent.Item2}, Pitch: {noteEvent.Item3} ({noteName}), Amplitude: {noteEvent.Item4}");
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

    private List<Tuple<int, int, int, float>> OutputToNotesPolyphonic(
        float[] frames, float[] onsets, float onsetThresh, float frameThresh, int minNoteLen, bool inferOnsets,
        float? maxFreq, float? minFreq, bool melodiaTrick, int energyTol)
    {
        // Calculate the dimensions based on the tensor shapes
        int nFrames = frames.Length / 264;
        int nOnsetFrames = onsets.Length / 88;

        float[,] frameMatrix = Reshape(frames, nFrames, 264);
        float[,] onsetMatrix = Reshape(onsets, nOnsetFrames, 88);

        (float[,] constrainedOnsets, float[,] constrainedFrames) = ConstrainFrequency(onsetMatrix, frameMatrix, maxFreq, minFreq);

        if (inferOnsets)
        {
            constrainedOnsets = GetInferredOnsets(constrainedOnsets, constrainedFrames);
        }

        List<Tuple<int, int, int, float>> noteEvents = new List<Tuple<int, int, int, float>>();

        float[,] peakThreshMat = new float[nOnsetFrames, 88];
        for (int i = 0; i < 88; i++)
        {
            List<int> peaks = FindPeaks(constrainedOnsets, i);
            foreach (int peak in peaks)
            {
                peakThreshMat[peak, i] = constrainedOnsets[peak, i];
            }
        }

        List<Tuple<int, int>> onsetIndices = new List<Tuple<int, int>>();
        for (int t = 0; t < nOnsetFrames; t++)
        {
            for (int f = 0; f < 88; f++)
            {
                if (peakThreshMat[t, f] >= onsetThresh)
                {
                    onsetIndices.Add(new Tuple<int, int>(t, f));
                }
            }
        }

        float[,] remainingEnergy = (float[,])constrainedFrames.Clone();
        foreach (var onsetIndex in onsetIndices)
        {
            int noteStartIdx = onsetIndex.Item1;
            int freqIdx = onsetIndex.Item2;

            if (noteStartIdx >= nOnsetFrames - 1)
            {
                continue;
            }

            int i = noteStartIdx + 1;
            int k = 0;
            while (i < nOnsetFrames - 1 && k < energyTol)
            {
                if (remainingEnergy[i, freqIdx] < frameThresh)
                {
                    k++;
                }
                else
                {
                    k = 0;
                }
                i++;
            }

            i -= k;
            if (i - noteStartIdx <= minNoteLen)
            {
                continue;
            }

            for (int j = noteStartIdx; j < i; j++)
            {
                remainingEnergy[j, freqIdx] = 0;
                if (freqIdx < MAX_FREQ_IDX)
                {
                    remainingEnergy[j, freqIdx + 1] = 0;
                }
                if (freqIdx > 0)
                {
                    remainingEnergy[j, freqIdx - 1] = 0;
                }
            }

            float amplitude = 0;
            for (int j = noteStartIdx; j < i; j++)
            {
                amplitude += constrainedFrames[j, freqIdx];
            }
            amplitude /= (i - noteStartIdx);

            noteEvents.Add(new Tuple<int, int, int, float>(noteStartIdx, i, freqIdx + MIDI_OFFSET, amplitude));
        }

        if (melodiaTrick)
        {
            while (MaxValue(remainingEnergy) > frameThresh)
            {
                int[] maxIdx = MaxIndex(remainingEnergy);
                int iMid = maxIdx[0];
                int freqIdx = maxIdx[1];
                remainingEnergy[iMid, freqIdx] = 0;

                int i = iMid + 1;
                int k = 0;
                while (i < nOnsetFrames - 1 && k < energyTol)
                {
                    if (remainingEnergy[i, freqIdx] < frameThresh)
                    {
                        k++;
                    }
                    else
                    {
                        k = 0;
                    }

                    remainingEnergy[i, freqIdx] = 0;
                    if (freqIdx < MAX_FREQ_IDX)
                    {
                        remainingEnergy[i, freqIdx + 1] = 0;
                    }
                    if (freqIdx > 0)
                    {
                        remainingEnergy[i, freqIdx - 1] = 0;
                    }

                    i++;
                }

                int iEnd = i - 1 - k;

                i = iMid - 1;
                k = 0;
                while (i > 0 && k < energyTol)
                {
                    if (remainingEnergy[i, freqIdx] < frameThresh)
                    {
                        k++;
                    }
                    else
                    {
                        k = 0;
                    }

                    remainingEnergy[i, freqIdx] = 0;
                    if (freqIdx < MAX_FREQ_IDX)
                    {
                        remainingEnergy[i, freqIdx + 1] = 0;
                    }
                    if (freqIdx > 0)
                    {
                        remainingEnergy[i, freqIdx - 1] = 0;
                    }

                    i--;
                }

                int iStart = i + 1 + k;

                if (iEnd - iStart <= minNoteLen)
                {
                    continue;
                }

                float amplitude = 0;
                for (int j = iStart; j < iEnd; j++)
                {
                    amplitude += constrainedFrames[j, freqIdx];
                }
                amplitude /= (iEnd - iStart);

                noteEvents.Add(new Tuple<int, int, int, float>(iStart, iEnd, freqIdx + MIDI_OFFSET, amplitude));
            }
        }

        return noteEvents;
    }

    private string ConvertMidiToNoteName(int midi)
    {
        int octave = (midi / 12) - 1;
        int noteIndex = midi % 12;
        string noteName = NoteNames[noteIndex];
        return $"{noteName}{octave}";
    }

    private List<int> FindPeaks(float[,] matrix, int col)
    {
        List<int> peaks = new List<int>();
        for (int i = 1; i < matrix.GetLength(0) - 1; i++)
        {
            if (matrix[i, col] > matrix[i - 1, col] && matrix[i, col] > matrix[i + 1, col])
            {
                peaks.Add(i);
            }
        }
        return peaks;
    }

    private float MaxValue(float[,] matrix)
    {
        float max = float.MinValue;
        foreach (float value in matrix)
        {
            if (value > max)
            {
                max = value;
            }
        }
        return max;
    }

    private int[] MaxIndex(float[,] matrix)
    {
        int[] index = new int[2];
        float max = float.MinValue;
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                if (matrix[i, j] > max)
                {
                    max = matrix[i, j];
                    index[0] = i;
                    index[1] = j;
                }
            }
        }
        return index;
    }

    private float[,] Reshape(float[] array, int rows, int cols)
    {
        if (array.Length != rows * cols)
        {
            throw new ArgumentException("Array length does not match specified dimensions.");
        }

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

    private (float[,], float[,]) ConstrainFrequency(float[,] onsets, float[,] frames, float? maxFreq, float? minFreq)
    {
        if (maxFreq.HasValue)
        {
            int maxFreqIdx = Mathf.RoundToInt(Mathf.Log(maxFreq.Value / 440.0f, 2) * 12 + 69 - MIDI_OFFSET);
            for (int i = 0; i < onsets.GetLength(0); i++)
            {
                for (int j = maxFreqIdx; j < onsets.GetLength(1); j++)
                {
                    onsets[i, j] = 0;
                    frames[i, j] = 0;
                }
            }
        }

        if (minFreq.HasValue)
        {
            int minFreqIdx = Mathf.RoundToInt(Mathf.Log(minFreq.Value / 440.0f, 2) * 12 + 69 - MIDI_OFFSET);
            for (int i = 0; i < onsets.GetLength(0); i++)
            {
                for (int j = 0; j < minFreqIdx; j++)
                {
                    onsets[i, j] = 0;
                    frames[i, j] = 0;
                }
            }
        }

        return (onsets, frames);
    }

    private float[,] GetInferredOnsets(float[,] onsets, float[,] frames, int nDiff = 2)
    {
        int nTimes = onsets.GetLength(0);
        int nFreqs = onsets.GetLength(1);

        float[,] diffs = new float[nTimes, nFreqs];
        for (int n = 1; n <= nDiff; n++)
        {
            for (int t = n; t < nTimes; t++)
            {
                for (int f = 0; f < nFreqs; f++)
                {
                    diffs[t, f] = Mathf.Max(diffs[t, f], frames[t, f] - frames[t - n, f]);
                }
            }
        }

        for (int t = 0; t < nTimes; t++)
        {
            for (int f = 0; f < nFreqs; f++)
            {
                diffs[t, f] = Mathf.Max(onsets[t, f], diffs[t, f]);
            }
        }

        return diffs;
    }
}
