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

    // Utilisez une liste pour stocker les instances de l'image détectée
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
            // Optionnellement, gérer les mises à jour spécifiques ici
        }

        foreach (var removedImage in eventArgs.removed)
        {
            trackedImageInstances.Remove(removedImage);
        }
    }

    void TryPlacePrefabBetweenImages()
    {
        // Assurez-vous qu'il y a exactement 2 instances de l'image détectée
        if (trackedImageInstances.Count == 2)
        {
            PlacePrefabBetweenImages(trackedImageInstances[0], trackedImageInstances[1]);
        }
    }

    void PlacePrefabBetweenImages(ARTrackedImage image1, ARTrackedImage image2)
    {
        Vector3 positionBetweenImages = (image1.transform.position + image2.transform.position) / 2;
        Quaternion rotationBetweenImages = Quaternion.Lerp(image1.transform.rotation, image2.transform.rotation, 0.5f);

        // Calcul de la largeur virtuelle nécessaire
        float distanceBetweenImages = Vector3.Distance(image1.transform.position, image2.transform.position);

        // Créer ou déplacer l'objet entre les images
        InstantiateOrUpdatePrefab(positionBetweenImages, rotationBetweenImages, distanceBetweenImages);
    }

    void InstantiateOrUpdatePrefab(Vector3 position, Quaternion rotation, float width)
    {
        audioSource.PlayOneShot(audioClip);

        GameObject existingInstance = GameObject.FindGameObjectWithTag("ARObject"); // Assurez-vous que votre prefab a ce tag
        if (existingInstance != null)
        {
            existingInstance.transform.position = position;
            existingInstance.transform.rotation = rotation;
            AdjustScale(existingInstance, width); // Ajuster l'échelle ici
        }
        else
        {
            existingInstance = Instantiate(prefab, position, rotation);
            AdjustScale(existingInstance, width); // Ajuster l'échelle ici aussi
        }
    }

    void AdjustScale(GameObject obj, float width)
    {
        // Ici, ajustez la largeur de l'objet pour qu'elle corresponde à la distance entre les images
        // Cette étape dépend de la façon dont votre objet est structuré et de quelle dimension représente sa "largeur"
        // Par exemple, si la largeur est sur l'axe X local de l'objet :
        Vector3 newScale = obj.transform.localScale;
        newScale.x = width; // Ajustez cette ligne selon la structure de votre objet
        obj.transform.localScale = newScale;
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
