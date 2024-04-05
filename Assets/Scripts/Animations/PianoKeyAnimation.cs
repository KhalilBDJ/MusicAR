using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PianoKeyAnimation : MonoBehaviour
{
    public bool tutorial;

    public bool Tutorial
    {
        get => tutorial;
        set => tutorial = value;
    }

    private float growthRate = 0.05f;
    private float moveRate = 0.05f;
    public string noteName; // Ajouté pour stocker le nom de la note
    private AudioAnalyzer analyzer;
    private GameObject scripts;

    private bool isPlaying;
    private PianoKeyPool pianoKeyPool;
    private bool shouldReturnToPool;
    private Vector3 initialScale; // Échelle initiale de la note
    private GameObject contactObject;
    private float _duration;

    private List<string> _currentNotes;


    void Awake()
    {
        scripts = GameObject.FindGameObjectWithTag("Script");
        analyzer = scripts.GetComponent<AudioAnalyzer>();
        pianoKeyPool = FindObjectOfType<PianoKeyPool>();
        initialScale = transform.localScale; // Sauvegarde de l'échelle initiale
    }
    
    private void OnEnable()
    {
        if (tutorial)
        {
            analyzer.NoteChanged += OnNotesChanged;

        }
    }

    private void OnDisable()
    {
        if (tutorial)
        {
            analyzer.NoteChanged -= OnNotesChanged;

        }
    }
    

    private void Start()
    {
        if (tutorial)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
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
                transform.localScale += new Vector3(0, growthAmount/2, 0);
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
        else
        {
            
            // Calculer la vitesse pour que le point P traverse la note en '_duration' secondes.
            float requiredSpeed = 0.15f;

            transform.localScale = new Vector3(transform.localScale.x, (requiredSpeed * _duration)/2,
                transform.localScale.z);

            // Appliquer la vitesse ajustée
            transform.position += new Vector3(0, -requiredSpeed, 0) * Time.deltaTime;

            if (!isPlaying)
            {
                transform.localScale = new Vector3(transform.localScale.x, _duration/10, transform.localScale.z);
            }

            if (transform.position.y >= contactObject.transform.position.y - transform.localScale.y && transform.position.y <= contactObject.transform.position.y + transform.localScale.y/2)
            {
                if (!_currentNotes.Contains(noteName))
                {
                    GetComponent<Renderer>().material.color = Color.red;
                }
                else
                {
                    GetComponent<Renderer>().material.color = Color.blue;

                }
            }

            if (transform.position.y <= contactObject.transform.position.y - transform.localScale.y)
            {
                isPlaying = true;
                transform.localScale = initialScale; 
                pianoKeyPool.ReturnNoteObject(gameObject, noteName);
                StopNote();
            }
        }
    }

    private void OnNotesChanged(object sender, NotePlayedEventArgs e)
    {
        _currentNotes = e.Notes;
    }

    public void PlayNote(string newNoteName, float duration)
    {
        if (!tutorial)
        {
            noteName = newNoteName;
            isPlaying = true;
            shouldReturnToPool = false;
            StartCoroutine(StopNoteAfterDuration(duration)); // Démarrer la coroutine avec la durée de la note
        }
        else
        {
            noteName = newNoteName;
            _duration = duration;
            isPlaying = false;
            shouldReturnToPool = false;
        }
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