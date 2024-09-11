namespace TEST
{
    
using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;

public class NoteDetection
{
    private const int MIDI_OFFSET = 21;
    private const int SONIFY_FS = 3000;
    private const int N_PITCH_BEND_TICKS = 8192;
    private const int MAX_FREQ_IDX = 87;

    public static Matrix<double> GetInferedOnsets(Matrix<double> onsets, Matrix<double> frames, int nDiff = 2)
    {
        var diffs = new List<Matrix<double>>();
        for (int n = 1; n <= nDiff; n++)
        {
            var zerosMatrix = Matrix<double>.Build.Dense(n, frames.ColumnCount);
            var framesAppended = zerosMatrix.Stack(frames);
            diffs.Add(framesAppended.SubMatrix(n, frames.RowCount, 0, frames.ColumnCount) - framesAppended.SubMatrix(0, frames.RowCount, 0, frames.ColumnCount));
        }

        var frameDiff = Matrix<double>.Build.DenseOfMatrix(diffs.Aggregate((a, b) => a.PointwiseMinimum(b)));
        frameDiff = frameDiff.PointwiseMaximum(0);
        frameDiff.SetSubMatrix(0, 0, Matrix<double>.Build.Dense(nDiff, frameDiff.ColumnCount));

        double maxOnsets = onsets.Enumerate().Max();
        frameDiff = frameDiff.Multiply(maxOnsets / frameDiff.Enumerate().Max());

        return onsets.PointwiseMaximum(frameDiff);
    }

    public static Tuple<Matrix<double>, Matrix<double>> ConstrainFrequency(
        Matrix<double> onsets, Matrix<double> frames, double? maxFreq, double? minFreq)
    {
        if (maxFreq.HasValue)
        {
            int maxFreqIdx = (int)Math.Round(FrequencyToMidi(maxFreq.Value) - MIDI_OFFSET);
            onsets.ClearSubMatrix(0, onsets.RowCount, maxFreqIdx, onsets.ColumnCount - maxFreqIdx);
            frames.ClearSubMatrix(0, frames.RowCount, maxFreqIdx, frames.ColumnCount - maxFreqIdx);
        }

        if (minFreq.HasValue)
        {
            int minFreqIdx = (int)Math.Round(FrequencyToMidi(minFreq.Value) - MIDI_OFFSET);
            onsets.ClearSubMatrix(0, onsets.RowCount, 0, minFreqIdx);
            frames.ClearSubMatrix(0, frames.RowCount, 0, minFreqIdx);
        }

        return Tuple.Create(onsets, frames);
    }

    public static List<Tuple<int, int, int, double>> OutputToNotesPolyphonic(
        Matrix<double> frames, Matrix<double> onsets, double onsetThresh, double frameThresh,
        int minNoteLen, bool inferOnsets, double? maxFreq, double? minFreq,
        bool melodiaTrick = true, int energyTol = 11)
    {
        int nFrames = frames.RowCount;

        var constrainedFreq = ConstrainFrequency(onsets, frames, maxFreq, minFreq);
        onsets = constrainedFreq.Item1;
        frames = constrainedFreq.Item2;

        if (inferOnsets)
        {
            onsets = GetInferedOnsets(onsets, frames);
        }

        var peakThreshMat = Matrix<double>.Build.Dense(onsets.RowCount, onsets.ColumnCount);
        var peaks = FindPeaks(onsets);
        foreach (var peak in peaks)
        {
            peakThreshMat[peak.Item1, peak.Item2] = onsets[peak.Item1, peak.Item2];
        }

        var onsetIndices = peakThreshMat.EnumerateIndexed()
            .Where(x => x.Item3 >= onsetThresh)
            .OrderByDescending(x => x.Item1)
            .ToList();

        var remainingEnergy = frames.Clone();

        var noteEvents = new List<Tuple<int, int, int, double>>();

        foreach (var onset in onsetIndices)
        {
            int noteStartIdx = onset.Item1;
            int freqIdx = onset.Item2;

            if (noteStartIdx >= nFrames - 1)
                continue;

            int i = noteStartIdx + 1;
            int k = 0;

            while (i < nFrames - 1 && k < energyTol)
            {
                if (remainingEnergy[i, freqIdx] < frameThresh)
                    k++;
                else
                    k = 0;
                i++;
            }

            i -= k;

            if (i - noteStartIdx <= minNoteLen)
                continue;

            for (int j = noteStartIdx; j < i; j++)
            {
                remainingEnergy[j, freqIdx] = 0;
                if (freqIdx < MAX_FREQ_IDX)
                    remainingEnergy[j, freqIdx + 1] = 0;
                if (freqIdx > 0)
                    remainingEnergy[j, freqIdx - 1] = 0;
            }

            double amplitude = frames.SubMatrix(noteStartIdx, i - noteStartIdx, freqIdx, 1).Enumerate().Average();
            noteEvents.Add(Tuple.Create(noteStartIdx, i, freqIdx + MIDI_OFFSET, amplitude));
        }

        if (melodiaTrick)
        {
            while (remainingEnergy.Enumerate().Max() > frameThresh)
            {
                var maxIndex = remainingEnergy.EnumerateIndexed().OrderByDescending(x => x.Item3).First();
                int iMid = maxIndex.Item1;
                int freqIdx = maxIndex.Item2;

                remainingEnergy[iMid, freqIdx] = 0;

                // Forward pass
                int i = iMid + 1;
                int k = 0;
                while (i < nFrames - 1 && k < energyTol)
                {
                    if (remainingEnergy[i, freqIdx] < frameThresh)
                        k++;
                    else
                        k = 0;

                    remainingEnergy[i, freqIdx] = 0;
                    if (freqIdx < MAX_FREQ_IDX)
                        remainingEnergy[i, freqIdx + 1] = 0;
                    if (freqIdx > 0)
                        remainingEnergy[i, freqIdx - 1] = 0;

                    i++;
                }

                int iEnd = i - 1 - k;

                // Backward pass
                i = iMid - 1;
                k = 0;
                while (i > 0 && k < energyTol)
                {
                    if (remainingEnergy[i, freqIdx] < frameThresh)
                        k++;
                    else
                        k = 0;

                    remainingEnergy[i, freqIdx] = 0;
                    if (freqIdx < MAX_FREQ_IDX)
                        remainingEnergy[i, freqIdx + 1] = 0;
                    if (freqIdx > 0)
                        remainingEnergy[i, freqIdx - 1] = 0;

                    i--;
                }

                int iStart = i + 1 + k;

                if (iEnd - iStart <= minNoteLen)
                    continue;

                double amplitude = frames.SubMatrix(iStart, iEnd - iStart, freqIdx, 1).Enumerate().Average();
                noteEvents.Add(Tuple.Create(iStart, iEnd, freqIdx + MIDI_OFFSET, amplitude));
            }
        }

        return noteEvents;
    }

    private static double FrequencyToMidi(double frequency)
    {
        return 69 + 12 * Math.Log(frequency / 440, 2);
    }

    private static List<Tuple<int, int>> FindPeaks(Matrix<double> matrix)
    {
        var peaks = new List<Tuple<int, int>>();
        for (int i = 1; i < matrix.RowCount - 1; i++)
        {
            for (int j = 0; j < matrix.ColumnCount; j++)
            {
                if (matrix[i, j] > matrix[i - 1, j] && matrix[i, j] > matrix[i + 1, j])
                {
                    peaks.Add(Tuple.Create(i, j));
                }
            }
        }
        return peaks;
    }
}




}