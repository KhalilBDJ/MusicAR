using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARPianoPlacement : MonoBehaviour
{
    
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;

    [SerializeField] private GameObject piano;

    private GameObject currentPiano;
    private List<ARTrackedImage> trackedImageInstances = new List<ARTrackedImage>();
    
    
    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;

    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            trackedImageInstances.Add(newImage);
            TryPlacingObject(newImage);
        }

        foreach (var updatedImage in eventArgs.updated)
        {
        }
    
        foreach (var removedImage in eventArgs.removed)
        {
            trackedImageInstances.Remove(removedImage);
        }
    }

    void TryPlacingObject(ARTrackedImage image)
    {
        currentPiano = Instantiate(piano, image.transform);
    }

    public void MovePiano(string direction)
    {
        switch (direction)
        {
            case "UP":
                currentPiano.transform.position += new Vector3(0, 0.1f, 0); 
                break;
            case "DOWN":
                currentPiano.transform.position += new Vector3(0, -0.1f, 0);
                break;
            case "LEFT":
                currentPiano.transform.position += new Vector3(-0.1f, 0, 0);
                break;
            case "RIGHT":
                currentPiano.transform.position += new Vector3(0.1f, 0, 0); 
                break;
        }
    }
}
