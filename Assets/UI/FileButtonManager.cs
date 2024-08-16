using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Importer TextMeshPro namespace

public class FileButtonManager : MonoBehaviour
{
    public string folderPath; // Chemin du dossier à parcourir
    public GameObject buttonPrefab; // Préfabriqué pour les boutons
    public Transform buttonContainer; // Conteneur des boutons (Horizontal Layout Group)

    void Start()
    {
        CreateButtonsForFiles();
    }

    void CreateButtonsForFiles()
    {
        // Liste pour suivre les noms de fichiers déjà traités
        HashSet<string> processedFileNames = new HashSet<string>();

        // Vérifiez si le dossier existe
        if (Directory.Exists(folderPath))
        {
            // Obtenez tous les fichiers du dossier
            string[] files = Directory.GetFiles(folderPath);

            foreach (string file in files)
            {
                // Filtrer uniquement les fichiers valides
                if (File.Exists(file))
                {
                    // Ignorer les fichiers .meta
                    if (file.EndsWith(".meta"))
                    {
                        continue;
                    }

                    // Obtenir le nom du fichier sans l'extension
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                    // Vérifier si ce nom de fichier a déjà été traité
                    if (!processedFileNames.Contains(fileNameWithoutExtension))
                    {
                        // Ajouter le nom du fichier à la liste des noms déjà traités
                        processedFileNames.Add(fileNameWithoutExtension);

                        // Log pour vérifier les fichiers traités
                        Debug.Log("Traitement du fichier: " + fileNameWithoutExtension);

                        // Créer un nouveau bouton
                        GameObject newButton = Instantiate(buttonPrefab, buttonContainer);

                        // Modifier le texte du bouton avec TextMeshProUGUI
                        TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
                        if (buttonText != null)
                        {
                            buttonText.text = fileNameWithoutExtension;
                        }

                        // Ajouter un listener pour le bouton
                        Button buttonComponent = newButton.GetComponent<Button>();
                        if (buttonComponent != null)
                        {
                            string filePath = file; // Capture locale pour le chemin du fichier

                            buttonComponent.onClick.AddListener(() => OnButtonClicked(filePath));
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Le dossier spécifié n'existe pas: " + folderPath);
        }
    }

    void OnButtonClicked(string filePath)
    {
        // Enregistrer le chemin du fichier dans PlayerPrefs
        PlayerPrefs.SetString("SelectedFilePath", filePath);
        Debug.Log("Fichier sélectionné: " + filePath);
    }
}
