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

    // Stockage temporaire pour les images détectées
    private Dictionary<string, ARTrackedImage> trackedImages = new Dictionary<string, ARTrackedImage>();

    void OnEnable() => m_TrackedImageManager.trackedImagesChanged += OnChanged;

    void OnDisable() => m_TrackedImageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Ajouter ou mettre à jour les images détectées
        foreach (var newImage in eventArgs.added)
        {
            trackedImages[newImage.referenceImage.name] = newImage;
        }

        foreach (var updatedImage in eventArgs.updated)
        {
            trackedImages[updatedImage.referenceImage.name] = updatedImage;
        }

        // Retirer les images qui ne sont plus détectées
        foreach (var removedImage in eventArgs.removed)
        {
            if (trackedImages.ContainsKey(removedImage.referenceImage.name))
            {
                trackedImages.Remove(removedImage.referenceImage.name);
            }
        }

        // Vérifier si deux images spécifiques sont détectées
        if (trackedImages.Count == 2)
        {
            ARTrackedImage[] images = new ARTrackedImage[2];
            trackedImages.Values.CopyTo(images, 0);
            PlacePrefabBetweenImages(images[0], images[1]);
        }
    }

    void PlacePrefabBetweenImages(ARTrackedImage image1, ARTrackedImage image2)
    {
        // Calculer la position moyenne entre les deux images
        Vector3 positionBetweenImages = (image1.transform.position + image2.transform.position) / 2;
        
        // Optionnel : Calculer l'orientation moyenne (si nécessaire)
        Quaternion rotationBetweenImages = Quaternion.Lerp(image1.transform.rotation, image2.transform.rotation, 0.5f);

        // Créer une instance du prefab et la positionner
        GameObject prefabInstance = Instantiate(prefab, positionBetweenImages, rotationBetweenImages);

        // Ajuster la taille ou l'orientation de l'objet selon les besoins
        AdjustObjectSizeOrOrientation(prefabInstance, image1, image2);
    }

    void AdjustObjectSizeOrOrientation(GameObject prefabInstance, ARTrackedImage image1, ARTrackedImage image2)
    {
        // Exemple : ajuster la largeur de l'objet pour correspondre à la distance entre les deux images
        float distance = Vector3.Distance(image1.transform.position, image2.transform.position);
        // Ajustez cette partie selon la façon dont votre objet doit être redimensionné ou orienté
        prefabInstance.transform.localScale = new Vector3(distance, prefabInstance.transform.localScale.y, prefabInstance.transform.localScale.z);
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
