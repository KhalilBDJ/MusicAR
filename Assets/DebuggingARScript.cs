using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class DebuggingARScript : MonoBehaviour
{
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;

    [SerializeField] private GameObject prefab;
    [SerializeField] private XROrigin _xrOrigin;
    [SerializeField] private GameObject testObject;

    public AudioClip audioClip;
    public AudioSource audioSource;

    private List<ARTrackedImage> trackedImageInstances = new List<ARTrackedImage>();

    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;

    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var newImage in eventArgs.added)
        {
            trackedImageInstances.Add(newImage);
            TryPlacePrefabBetweenImages();
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            // Gérer les mises à jour spécifiques ici, si nécessaire
        }

        foreach (var removedImage in eventArgs.removed)
        {
            trackedImageInstances.Remove(removedImage);
        }
    }

    void TryPlacePrefabBetweenImages()
    {
        if (trackedImageInstances.Count == 2)
        {
            PlacePrefabBetweenImages(trackedImageInstances[0], trackedImageInstances[1]);
        }
    }

    void PlacePrefabBetweenImages(ARTrackedImage image1, ARTrackedImage image2)
    {
        Vector3 positionBetweenImages = (image1.transform.position + image2.transform.position) / 2;

        // Calcul de la "moyenne" des rotations
        Quaternion averageRotation = AverageQuaternion(image1.transform.rotation, image2.transform.rotation);

        float distanceBetweenImages = Vector3.Distance(image1.transform.position, image2.transform.position) - (image1.size.x / 2 + image2.size.x / 2);
        InstantiateOrUpdatePrefab(positionBetweenImages, averageRotation, distanceBetweenImages);
    }

    Quaternion AverageQuaternion(Quaternion q1, Quaternion q2)
    {
        return Quaternion.Slerp(q1, q2, 0.5f);
    }

    void InstantiateOrUpdatePrefab(Vector3 position, Quaternion rotation, float distance)
    {
        audioSource.PlayOneShot(audioClip);

        GameObject existingInstance = GameObject.FindGameObjectWithTag("ARObject");
        if (existingInstance != null)
        {
            existingInstance.transform.position = position;
            existingInstance.transform.rotation = rotation;
            AdjustScale(existingInstance, distance);
        }
        else
        {
            existingInstance = Instantiate(prefab, position, rotation, _xrOrigin.transform);
            AdjustScale(existingInstance, distance);
        }
    }

    void AdjustScale(GameObject obj, float distance)
    {
        Vector3 newScale = obj.transform.localScale;
        newScale.z = distance; // Adjust the scale based on the distance
        obj.transform.localScale = newScale;
    }

}
