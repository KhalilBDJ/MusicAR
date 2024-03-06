using System.Collections;
using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    private float growthRate = 0.05f;
    private float moveRate = 0.05f;
    public string noteName; // Ajouté pour stocker le nom de la note

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
            // Fait grandir la note uniquement vers le haut (en augmentant sa taille et en ajustant sa position pour qu'elle grandisse vers le haut)
            float growthAmount = growthRate * Time.deltaTime;
            transform.localScale += new Vector3(0, growthAmount, 0);
            transform.position += new Vector3(0, growthAmount / 2, 0); // Ajuste la position pour que la croissance semble se faire vers le haut
        }
        else
        {
            // Une fois la note arrêtée, elle se détache et monte indéfiniment
            transform.position += new Vector3(0, moveRate, 0) * Time.deltaTime;
        }

        // Si la note doit être retournée au pool et s'éloigne suffisamment, la remettre au pool
        if (shouldReturnToPool && transform.position.y > 50) // Condition modifiée pour utiliser la position en y
        {
            shouldReturnToPool = false;
            pianoKeyPool.ReturnNoteObject(gameObject, noteName);
        }
    }

    public void PlayNote(string newNoteName, float duration)
    {
        noteName = newNoteName;
        isPlaying = true;
        shouldReturnToPool = false;
        StartCoroutine(StopNoteAfterDuration(duration)); // Démarrer la coroutine avec la durée de la note
    }
    
    public void PlayNote(string newNoteName)  
    {
        noteName = newNoteName;  
        isPlaying = true;
        shouldReturnToPool = false;  
    }

    IEnumerator StopNoteAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration); // Attendre la durée spécifiée
        StopNote(); // Arrêter la note
    }

    public void StopNote()
    {
        isPlaying = false;
        shouldReturnToPool = true;
    }
}