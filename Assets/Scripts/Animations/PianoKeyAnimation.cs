using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public float growthRate = 1f;
    private float moveRate = 0.3f;
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
            transform.position += new Vector3(0, moveRate, 0) * Time.deltaTime;
        }
        

        if (shouldReturnToPool && transform.position.z <= -50 )
        {
            shouldReturnToPool = false; 
            pianoKeyPool.ReturnNoteObject(gameObject, noteName);  
        }
    }

    public void PlayNote(string newNoteName)  
    {
        noteName = newNoteName;  
        isPlaying = true;
        shouldReturnToPool = false;  
    }

    public void StopNote()
    {
        isPlaying = false;
        shouldReturnToPool = true;  
    }
}