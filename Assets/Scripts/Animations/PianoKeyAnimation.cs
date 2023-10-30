using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public float growthRate = 1f;
    public float moveRate = 1f;

    private bool isPlaying;
    private PianoKeyPool pianoKeyPool;
    private bool shouldReturnToPool;

    void Awake()
    {
        pianoKeyPool = FindObjectOfType<PianoKeyPool>();
    }

    void Update()
    {
        // Maintenant, la note montera toujours, que isPlaying soit true ou false
        transform.localScale = new Vector3(1, transform.localScale.y + growthRate * Time.deltaTime, 1);
        transform.position += new Vector3(0, moveRate, 0) * Time.deltaTime;

        // Cependant, si shouldReturnToPool est true et la position y est >= 100, retournez l'objet à la pool
        if (shouldReturnToPool && transform.position.y >= 100)
        {
            shouldReturnToPool = false;  // Réinitialiser le flag
            pianoKeyPool.ReturnNoteToPool(gameObject);
        }
    }

    public void PlayNote()
    {
        isPlaying = true;
        shouldReturnToPool = false;  // Réinitialiser le flag lorsqu'une note commence à jouer
    }

    public void StopNote()
    {
        isPlaying = false;
        shouldReturnToPool = true;  // Mettre le flag à true lorsque la note cesse de jouer
    }
}