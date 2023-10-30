using UnityEngine;
using System.Collections.Generic;

public class PianoKeyPool : MonoBehaviour
{
    public GameObject naturalNotePrefab;
    public GameObject sharpNotePrefab;
    public int initialPoolSize = 10;

    private Queue<GameObject> naturalNotesPool;
    private Queue<GameObject> sharpNotesPool;

    void Awake()
    {
        naturalNotesPool = new Queue<GameObject>();
        sharpNotesPool = new Queue<GameObject>();

        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject naturalNote = Instantiate(naturalNotePrefab);
            naturalNote.SetActive(false);
            naturalNotesPool.Enqueue(naturalNote);

            GameObject sharpNote = Instantiate(sharpNotePrefab);
            sharpNote.SetActive(false);
            sharpNotesPool.Enqueue(sharpNote);
        }
    }

    public GameObject GetNaturalNote()
    {
        if (naturalNotesPool.Count == 0)
        {
            GameObject naturalNote = Instantiate(naturalNotePrefab);
            naturalNote.SetActive(false);
            naturalNotesPool.Enqueue(naturalNote);
        }
        GameObject note = naturalNotesPool.Dequeue();
        note.SetActive(true);
        return note;
    }

    public GameObject GetSharpNote()
    {
        if (sharpNotesPool.Count == 0)
        {
            GameObject sharpNote = Instantiate(sharpNotePrefab);
            sharpNote.SetActive(false);
            sharpNotesPool.Enqueue(sharpNote);
        }
        GameObject note = sharpNotesPool.Dequeue();
        note.SetActive(true);
        return note;
    }

    public void ReturnNoteToPool(GameObject note)
    {
        note.SetActive(false);
        if (note.name.Contains("#"))
        {
            sharpNotesPool.Enqueue(note);
        }
        else
        {
            naturalNotesPool.Enqueue(note);
        }
    }
}