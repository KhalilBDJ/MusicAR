using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPianoPlacement : MonoBehaviour
{
    [SerializeField]
    private ARTrackedImageManager m_TrackedImageManager;

    [SerializeField]
    private ARAnchorManager m_AnchorManager;

    [SerializeField]
    private GameObject piano;

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
        StartCoroutine(PlaceObjectWithDelay(image, 2f)); // Lance une coroutine avec un délai de 2 secondes
    }

    IEnumerator PlaceObjectWithDelay(ARTrackedImage image, float delay)
    {
        yield return new WaitForSeconds(delay); // Attend pendant le délai spécifié

        ARAnchor anchor = m_AnchorManager.AddAnchor(new Pose(image.transform.position, Quaternion.Euler(0, 0, 0)));
        if (anchor != null)
        {
            currentPiano = Instantiate(piano, new Vector3(anchor.transform.position.x, anchor.transform.position.y, anchor.transform.position.z), Quaternion.Euler(0, 0, 0), anchor.transform);
        }
    }

    public void MovePiano(string direction)
    {
        switch (direction)
        {
            case "UP":
                currentPiano.transform.position += new Vector3(0, 0.05f, 0); 
                break;
            case "DOWN":
                currentPiano.transform.position += new Vector3(0, -0.05f, 0);
                break;
            case "LEFT":
                currentPiano.transform.position += new Vector3(-0.05f, 0, 0);
                break;
            case "RIGHT":
                currentPiano.transform.position += new Vector3(0.05f, 0, 0); 
                break;
            case "FORWARD":
                currentPiano.transform.position += new Vector3(0, 0, 0.05f); 
                break;
            case "BACKWARD":
                currentPiano.transform.position += new Vector3(0, 0, -0.05f); 
                break;
            case "BIGGER":
                currentPiano.transform.localScale += new Vector3(0.05f, 0.05f, 0.05f);
                break;
            case "SMALLER":
                currentPiano.transform.localScale += new Vector3(-0.05f, -0.05f, -0.05f);
                break;
        }
    }
}
