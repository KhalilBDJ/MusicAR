using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public float growthRate = 1f;
    public float moveRate = 1f;

    private bool isPlaying;

    void Update()
    {
        if (isPlaying)
        {
            transform.localScale += new Vector3(growthRate, growthRate, growthRate) * Time.deltaTime;
        }

        transform.position += new Vector3(0, moveRate, 0) * Time.deltaTime;
    }

    public void PlayNote()
    {
        isPlaying = true;
    }

    public void StopNote()
    {
        isPlaying = false;
    }
}