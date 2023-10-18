using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PianoDetector : MonoBehaviour
{

    private ARTrackedImageManager _trackedPiano;
    // Start is called before the first frame update

    private void OnEnable()
    {
        _trackedPiano.trackedImagesChanged += OnImageChanged;
    }

    private void OnDisable()
    {
        _trackedPiano.trackedImagesChanged -= OnImageChanged;
    }
    

    private void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {
        foreach (var trackedImage in args.added)
        {
            Debug.Log(trackedImage.name);
        }
    }
}
