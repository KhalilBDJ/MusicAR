namespace Classes
{
   using System;

public class YinPitchTracker
{
    private float threshold;
    private int sampleRate;
    private float[] buffer;

    public YinPitchTracker(int sampleRate, float threshold = 0.15f)
    {
        this.sampleRate = sampleRate;
        this.threshold = threshold;
    }

    public float ProcessBuffer(float[] audioBuffer)
    {
        buffer = new float[audioBuffer.Length / 2];
        DifferenceFunction(audioBuffer);
        CumulativeMeanNormalizedDifference();
        int tau = AbsoluteThreshold();
        if (tau != -1)
        {
            float betterTau = ParabolicInterpolation(tau);
            return sampleRate / betterTau;
        }
        return -1;
    }

    private void DifferenceFunction(float[] audioBuffer)
    {
        int length = buffer.Length;
        for (int tau = 0; tau < length; tau++)
        {
            for (int i = 0; i < length; i++)
            {
                float delta = audioBuffer[i] - audioBuffer[i + tau];
                buffer[tau] += delta * delta;
            }
        }
    }

    private void CumulativeMeanNormalizedDifference()
    {
        buffer[0] = 1;
        float runningSum = 0;
        for (int tau = 1; tau < buffer.Length; tau++)
        {
            runningSum += buffer[tau];
            buffer[tau] *= tau / runningSum;
        }
    }

    private int AbsoluteThreshold()
    {
        for (int tau = 2; tau < buffer.Length; tau++)
        {
            if (buffer[tau] < threshold)
            {
                while (tau + 1 < buffer.Length && buffer[tau + 1] < buffer[tau])
                    tau++;
                return tau;
            }
        }
        return -1;
    }

    private float ParabolicInterpolation(int tau)
    {
        float betterTau = tau;
        if (tau > 1 && tau < buffer.Length - 1)
        {
            float s0, s1, s2;
            s0 = buffer[tau - 1];
            s1 = buffer[tau];
            s2 = buffer[tau + 1];
            betterTau = tau + (s2 - s0) / (2 * (2 * s1 - s2 - s0));
        }
        return betterTau;
    }
}

}