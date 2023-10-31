using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Tutorials.Core.Editor;

public class PianoKeyPool : MonoBehaviour
{
    public GameObject naturalNotePrefab;
    public GameObject sharpNotePrefab;
    public GameObject pianoGameObject; 
    public int initialPoolSize = 200;
    public float keyWidth;  
    


    private string[] noteNames = {
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
    
    private Vector3[] keyPositions = new Vector3[88];
    private Queue<GameObject> _sharpNotesPool = new Queue<GameObject>();
    private Queue<GameObject> _naturalNotesPool = new Queue<GameObject>();

    private void Awake()
    {
        keyWidth = pianoGameObject.GetComponent<RectTransform>().rect.width / 88;
        _noteNames = new List<string>(noteNames);
        InitializePool(_sharpNotesPool, sharpNotePrefab, initialPoolSize);
        InitializePool(_naturalNotesPool, naturalNotePrefab, initialPoolSize);
        InitializeKeyPositions();
    }
    
    private void InitializePool(Queue<GameObject> pool, GameObject prefab, int size)
    {
        for (int i = 0; i < size; i++)
        {
            GameObject noteObject = Instantiate(prefab);
            noteObject.transform.SetParent(pianoGameObject.transform);  // Set PianoGameObject as the parent
            noteObject.SetActive(false);
            pool.Enqueue(noteObject);
        }
    }

    
    private void InitializeKeyPositions()
    {
        for (int i = 0; i < 88; i++)
        {
            float xPosition = pianoGameObject.transform.position.x + (i * keyWidth);
            keyPositions[i] = new Vector3(xPosition, pianoGameObject.transform.position.y, pianoGameObject.transform.position.z);
        }
    }
    
    private int GetKeyIndex(string noteName)
    {
        int index = _noteNames.IndexOf(noteName) - 1; 
        return index;
    }

    public GameObject GetNoteObject(string noteName)
    {
        GameObject noteObject;
        if (!noteName.IsNullOrEmpty())
        {
            if (noteName.Contains("#"))
            {
                noteObject = GetObjectFromPool(_sharpNotesPool, sharpNotePrefab);
                noteObject.transform.localScale = new Vector3(keyWidth, 1, 1);
            }
            else
            {
                noteObject = GetObjectFromPool(_naturalNotesPool, naturalNotePrefab);
                noteObject.transform.localScale = new Vector3(keyWidth, 1, 1);

            }
            int keyIndex = GetKeyIndex(noteName);
            noteObject.transform.localPosition = keyPositions[keyIndex];
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
            GameObject noteObject = Instantiate(prefab);
            noteObject.transform.SetParent(pianoGameObject.transform);  // Set PianoGameObject as the parent
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