using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARPianoPlacement : MonoBehaviour
{
    
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;
    
    private List<ARTrackedImage> trackedImageInstances = new List<ARTrackedImage>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;

    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            trackedImageInstances.Add(newImage);
        }

        foreach (var updatedImage in eventArgs.updated)
        {
        }
    
        foreach (var removedImage in eventArgs.removed)
        {
            trackedImageInstances.Remove(removedImage);
        }
    }
}
