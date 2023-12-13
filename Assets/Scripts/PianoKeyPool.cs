using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;

public class PianoKeyPool : MonoBehaviour
{
    
    //PUBLIC GO
    public GameObject naturalNotePrefab;
    public GameObject sharpNotePrefab;
    public GameObject pianoGameObject; 
    
    
    //PUBLIC INT
    public int initialPoolSize = 200;
    
    //PRIVATE FLOAT
    private float _whiteKeyWidth;
    private float _blackKeyWidth;
    private float _currentXPosition;
    private float _parentHeight;

    private readonly Dictionary<string, float> _notePositions = new Dictionary<string, float>();
    private readonly string[] _names = {
        "A0", "A#0", "B0",
        "C1", "C#1", "D1", "D#1", "E1", "F1", "F#1", "G1", "G#1", "A1", "A#1", "B1",
        "C2", "C#2", "D2", "D#2", "E2", "F2", "F#2", "G2", "G#2", "A2", "A#2", "B2",
        "C3", "C#3", "D3", "D#3", "E3", "F3", "F#3", "G3", "G#3", "A3", "A#3", "B3",
        "C4", "C#4", "D4", "D#4", "E4", "F4", "F#4", "G4", "G#4", "A4", "A#4", "B4",
        "C5", "C#5", "D5", "D#5", "E5", "F5", "F#5", "G5", "G#5", "A5", "A#5", "B5",
        "C6", "C#6", "D6", "D#6", "E6", "F6", "F#6", "G6", "G#6", "A6", "A#6", "B6",
        "C7", "C#7", "D7", "D#7", "E7", "F7", "F#7", "G7", "G#7", "A7", "A#7", "B7",
        "C8"
    };


    private List<String> _noteNames = new List<string>();

    private Queue<GameObject> _sharpNotesPool = new Queue<GameObject>();
    private Queue<GameObject> _naturalNotesPool = new Queue<GameObject>();

    private void Awake()
    {
        Debug.Log(_names.Length);
        Debug.Log(pianoGameObject.GetComponent<MeshRenderer>().bounds.size.x);
        _whiteKeyWidth = pianoGameObject.GetComponent<Transform>().localScale.x / 52;
        _blackKeyWidth = _whiteKeyWidth / 2;
        _currentXPosition = pianoGameObject.GetComponent<Transform>().localScale.x  / 2 - _blackKeyWidth;
        _noteNames = new List<string>(_names);
        InitializePool(_sharpNotesPool, sharpNotePrefab, initialPoolSize);
        InitializePool(_naturalNotesPool, naturalNotePrefab, initialPoolSize);
        InitializeNotePositions();
        /*foreach (var note in _names)
        {
            GetNoteObject(note);
        }*/

        Debug.Log(pianoGameObject.GetComponentInParent<Transform>().name);
        Debug.Log(GetComponentInParent<Transform>().name);
        _parentHeight = pianoGameObject.GetComponentInParent<MeshRenderer>().bounds.size.y;
    }
    
    private void InitializePool(Queue<GameObject> pool, GameObject prefab, int size)
    {
        for (int i = 0; i < size; i++)
        {
            GameObject noteObject = Instantiate(prefab, pianoGameObject.transform, true);
            noteObject.SetActive(false);
            pool.Enqueue(noteObject);
        }
    }
    void InitializeNotePositions()
    {
        bool isPrevKeyWhite = true; // Pour s'assurer qu'on ne place pas deux touches noires successives trop près

        foreach (string note in _names)
        {
            // Déterminez si la note actuelle est une touche blanche ou noire
            bool isWhiteKey = !note.Contains("#");

            if (isWhiteKey)
            {
                _notePositions[note] = _currentXPosition;
                _currentXPosition -= _whiteKeyWidth;
                isPrevKeyWhite = true;
            }
            else
            {
                // Si la touche précédente était blanche, on place la touche noire à une demi-largeur de la touche blanche précédente
                float offset = isPrevKeyWhite ? -_blackKeyWidth : 0;
                _notePositions[note] = (_currentXPosition - offset);
                isPrevKeyWhite = false;
            }
        }
    }
    
    public GameObject GetNoteObject(string noteName)
    {
        GameObject noteObject;
        if (!noteName.IsNullOrEmpty())
        {
            if (noteName.Contains("#"))
            {
                noteObject = GetObjectFromPool(_sharpNotesPool, sharpNotePrefab);
                noteObject.transform.localScale = new Vector3(_blackKeyWidth, 1, 1);
                noteObject.transform.localPosition = new Vector3(_notePositions[noteName] ,0, - _parentHeight/2 );

            }
            else
            {
                noteObject = GetObjectFromPool(_naturalNotesPool, naturalNotePrefab);
                noteObject.transform.localScale = new Vector3(_whiteKeyWidth, 1, 1);
                noteObject.transform.localPosition = new Vector3(_notePositions[noteName] ,0, - _parentHeight/2 );
            }
            noteObject.name = noteName;
            noteObject.SetActive(true);
            return noteObject;
        }

        return null;

    }

    private GameObject GetObjectFromPool(Queue<GameObject> pool, GameObject prefab)
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            GameObject noteObject = Instantiate(prefab, pianoGameObject.transform, true);
            return noteObject;
        }
    }


    public void ReturnNoteObject(GameObject noteObject, string noteName)
    {
        noteObject.SetActive(false);

        if (!noteName.IsNullOrEmpty())
        {
            if (noteName.Contains("#"))
            {
                _sharpNotesPool.Enqueue(noteObject);
            }
            else
            {
                _naturalNotesPool.Enqueue(noteObject);
            }
        }

    }
}