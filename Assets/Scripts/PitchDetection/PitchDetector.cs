namespace PitchDetection
{
    using System;
    using System.Collections.Generic;
    using System;

    public class PitchDetector
    {
        private readonly float[] buffer;
        private readonly int sampleRate;
        private readonly float threshold;

        public PitchDetector(int bufferSize, int sampleRate, float threshold)
        {
            this.buffer = new float[bufferSize];
            this.sampleRate = sampleRate;
            this.threshold = threshold;
        }

        public PitchResult DetectPitch(float[] audioData)
        {
            if (audioData.Length < buffer.Length)
            {
                throw new ArgumentException("Input audio data must be at least as long as the buffer size.");
            }

            Array.Copy(audioData, buffer, buffer.Length);

            // Step 1: Calculate the squared difference for each tau value.
            float[] difference = new float[buffer.Length / 2];
            for (int tau = 0; tau < difference.Length; tau++)
            {
                for (int j = 0; j < difference.Length; j++)
                {
                    float delta = buffer[j] - buffer[j + tau];
                    difference[tau] += delta * delta;
                }
            }

            // Step 2: Calculate the cumulative mean normalized difference function
            float[] cumulativeMean = new float[difference.Length];
            cumulativeMean[0] = 1;
            for (int tau = 1; tau < difference.Length; tau++)
            {
                cumulativeMean[tau] = difference[tau] * tau / (float)CumulativeSum(difference, tau);
                if (cumulativeMean[tau] < threshold)
                {
                    // Found a pitch, return the result
                    float pitch = sampleRate / tau;
                    return new PitchResult { Pitch = pitch, Confidence = 1 - cumulativeMean[tau] };
                }
            }

            // No pitch found
            return new PitchResult { Pitch = 0, Confidence = 0 };
        }

        private static float CumulativeSum(float[] array, int upToIndex)
        {
            float sum = 0;
            for (int i = 0; i <= upToIndex; i++)
            {
                sum += array[i];
            }
            return sum;
        }
    }

}