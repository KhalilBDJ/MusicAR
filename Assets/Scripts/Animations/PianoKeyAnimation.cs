using System;
using System.Collections;
using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public bool tutorial;

    private float growthRate = 0.05f;
    private float moveRate = 0.05f;
    public string noteName; // Ajouté pour stocker le nom de la note

    private bool isPlaying;
    private PianoKeyPool pianoKeyPool;
    private bool shouldReturnToPool;
    private Vector3 initialScale; // Échelle initiale de la note
    private GameObject contactObject;


    void Awake()
    {
        pianoKeyPool = FindObjectOfType<PianoKeyPool>();
        initialScale = transform.localScale; // Sauvegarde de l'échelle initiale
    }

    private void Start()
    {
        if (!tutorial)
        {
            transform.position = new Vector3(transform.position.x, 100, transform.position.z);
            contactObject = GameObject.FindGameObjectWithTag("ARObject");

        }
    }

    void Update()
    {
        if (!tutorial)
        {
            if (isPlaying)
            {
                // Fait grandir la note uniquement vers le haut (en augmentant sa taille et en ajustant sa position pour qu'elle grandisse vers le haut)
                float growthAmount = growthRate * Time.deltaTime;
                transform.localScale += new Vector3(0, growthAmount, 0);
                transform.position +=
                    new Vector3(0, growthAmount / 2,
                        0); // Ajuste la position pour que la croissance semble se faire vers le haut
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
        else
        {
            if (!isPlaying)
            {
                // Fait descendre la note
                transform.position += new Vector3(0, -growthRate, 0) * Time.deltaTime;
            }
            else
            {
                // Réduit la taille de la note
                float shrinkAmount = growthRate * Time.deltaTime;
                transform.localScale -= new Vector3(0, shrinkAmount, 0);
                transform.position -= new Vector3(0, shrinkAmount / 2, 0); // Ajuste la position pour que la réduction semble se faire vers le bas

                // Vérifie si la taille de la note est suffisamment petite pour être retournée au pool
                if (transform.localScale.y <= 0)
                {
                    transform.localScale = initialScale; // Réinitialise l'échelle pour une utilisation future
                    pianoKeyPool.ReturnNoteObject(gameObject, noteName);
                    isPlaying = false; // Réinitialise isPlaying
                }
            }

            // Détection de contact pour déclencher la réduction de la taille
            if (!isPlaying && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.1f))
            {
                if (hit.transform == contactObject.transform)
                {
                    isPlaying = true;
                }
            }
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