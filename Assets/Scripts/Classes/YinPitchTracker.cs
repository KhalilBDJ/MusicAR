using System;

namespace Classes
{
    public class YinPitchTracker
{
    private float[] buffer;
    private int bufferSize;
    private float threshold;
    private int sampleRate; // Ajout de la variable sampleRate


    public YinPitchTracker(int sampleRate, float threshold = 0.15f)
    {
        this.sampleRate = sampleRate; // Initialisation de sampleRate
        this.bufferSize = sampleRate / 2;
        this.buffer = new float[this.bufferSize];
        this.threshold = threshold;
    }

    public float GetPitch(float[] audioBuffer)
    {
        int tauEstimate;
        float pitchInHertz;

        // Step 1: Difference function
        for (int tau = 0; tau < this.bufferSize; tau++)
        {
            this.buffer[tau] = 0;
            for (int i = 0; i < this.bufferSize - tau; i++) // Modification ici
            {
                float delta = audioBuffer[i] - audioBuffer[i + tau];
                this.buffer[tau] += delta * delta;
            }
        }


        // Step 2: Cumulative mean normalized difference function
        float runningSum = 0;
        this.buffer[0] = 1;
        for (int tau = 1; tau < this.bufferSize; tau++)
        {
            runningSum += this.buffer[tau];
            this.buffer[tau] *= tau / runningSum;
        }

        // Step 3: Absolute threshold
        for (tauEstimate = 2; tauEstimate < this.bufferSize; tauEstimate++)
        {
            if (this.buffer[tauEstimate] < this.threshold)
            {
                while (tauEstimate + 1 < this.bufferSize && this.buffer[tauEstimate + 1] < this.buffer[tauEstimate])
                    tauEstimate++;
                break;
            }
        }

        // Step 4: Check if no pitch found
        if (tauEstimate == this.bufferSize || this.buffer[tauEstimate] >= this.threshold)
        {
            pitchInHertz = -1;
        }
        else
        {
            // Step 5: Interpolated pitch
            pitchInHertz = sampleRate / ParabolicInterpolation(tauEstimate);
        }

        return pitchInHertz;
    }

    private float ParabolicInterpolation(int tauEstimate)
    {
        float betterTau;
        int x0, x2;

        if (tauEstimate < 1)
        {
            x0 = tauEstimate;
        }
        else
        {
            x0 = tauEstimate - 1;
        }

        if (tauEstimate + 1 < this.bufferSize)
        {
            x2 = tauEstimate + 1;
        }
        else
        {
            x2 = tauEstimate;
        }

        if (x0 == tauEstimate)
        {
            if (this.buffer[tauEstimate] <= this.buffer[x2])
            {
                betterTau = tauEstimate;
            }
            else
            {
                betterTau = x2;
            }
        }
        else if (x2 == tauEstimate)
        {
            if (this.buffer[tauEstimate] <= this.buffer[x0])
            {
                betterTau = tauEstimate;
            }
            else
            {
                betterTau = x0;
            }
        }
        else
        {
            float s0, s1, s2;
            s0 = this.buffer[x0];
            s1 = this.buffer[tauEstimate];
            s2 = this.buffer[x2];
            // Parabolic interpolation formula
            betterTau = tauEstimate + (s2 - s0) / (2 * (2 * s1 - s2 - s0));
        }

        return betterTau;
    }
}

}
