using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public float growthRate = 1f;
    public float moveRate = 1f;
    public string noteName;  // Ajouté pour stocker le nom de la note

    private bool isPlaying;
    private PianoKeyPool pianoKeyPool;
    private bool shouldReturnToPool;

    void Awake()
    {
        pianoKeyPool = FindObjectOfType<PianoKeyPool>();
    }

    void Update()
    {
        if (isPlaying)
        {
            transform.position += new Vector3(0, moveRate/2, 0) * Time.deltaTime;
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y + growthRate * Time.deltaTime, transform.localScale.z);
        }
        else
        {
            // Maintenant, la note montera toujours, que isPlaying soit true ou false
            transform.position += new Vector3(0, moveRate, 0) * Time.deltaTime;
        }
        

        // Cependant, si shouldReturnToPool est true et la position y est >= 100, retournez l'objet à la pool
        if (shouldReturnToPool && transform.position.y >= (10 + transform.position.y))
        {
            shouldReturnToPool = false;  // Réinitialiser le flag
            pianoKeyPool.ReturnNoteObject(gameObject, noteName);  // Modifié pour passer le nom de la note
        }
    }

    public void PlayNote(string newNoteName)  // Modifié pour accepter le nom de la note
    {
        noteName = newNoteName;  // Stockez le nom de la note pour une utilisation ultérieure
        isPlaying = true;
        shouldReturnToPool = false;  // Réinitialiser le flag lorsqu'une note commence à jouer
    }

    public void StopNote()
    {
        isPlaying = false;
        shouldReturnToPool = true;  // Mettre le flag à true lorsque la note cesse de jouer
    }
}