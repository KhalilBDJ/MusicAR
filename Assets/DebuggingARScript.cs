using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class DebuggingARScript : MonoBehaviour
{
    [SerializeField]
    ARTrackedImageManager m_TrackedImageManager;

    [SerializeField] private GameObject prefab;

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
        // Assurez-vous que l'objet est placé au même niveau Y que les images
        positionBetweenImages.y = image1.transform.position.y; // ou une valeur spécifique si nécessaire

        float distanceBetweenImages = Vector3.Distance(image1.transform.position, image2.transform.position);
        Vector3 direction = (image2.transform.position - image1.transform.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(direction);

        InstantiateOrUpdatePrefab(positionBetweenImages, rotation, distanceBetweenImages);
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
            existingInstance = Instantiate(prefab, position, rotation);
            AdjustScale(existingInstance, distance);
        }
    }

    void AdjustScale(GameObject obj, float distance)
    {
        // Ajustez cette méthode pour modifier la taille de l'objet en fonction de la distance entre les images
        Vector3 newScale = obj.transform.localScale;
        newScale.z = distance; // Ajustez cet axe selon l'orientation de votre objet
        obj.transform.localScale = newScale;
    }
}
