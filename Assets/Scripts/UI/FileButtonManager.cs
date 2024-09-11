using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ButtonCreatorEditor : MonoBehaviour
    {
        public GameObject buttonPrefab; // Le prefab de bouton à instancier
        public Transform parentTransform; // Le parent des boutons
        public string folderPath; // Le chemin du dossier à scanner

        private void Start()
        {
            GenerateButtons();
        }

        [ContextMenu("Generate Buttons")]
        void GenerateButtons()
        {
            // Nettoyer les anciens boutons avant d'en créer de nouveaux
            foreach (Transform child in parentTransform)
            {
                DestroyImmediate(child.gameObject);
            }

            // S'assurer que le dossier existe
            if (Directory.Exists(folderPath))
            {
                HashSet<string> processedFileNames = new HashSet<string>();
                string[] files = Directory.GetFiles(folderPath);

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                
                    // Vérifier si le fichier existe et qu'il n'est pas un fichier meta
                    if (File.Exists(file) && !file.EndsWith(".meta"))
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                        Debug.Log(fileNameWithoutExtension);

                        // Éviter les duplications de fichiers
                        if (!processedFileNames.Contains(fileNameWithoutExtension))
                        {
                            processedFileNames.Add(fileNameWithoutExtension);
                            CreateButton(fileNameWithoutExtension);
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Le dossier spécifié n'existe pas.");
            }
        }

        void CreateButton(string fileName)
        {
            GameObject newButton = Instantiate(buttonPrefab, parentTransform);

            // Changer le nom de l'objet
            newButton.name = fileName;

            // Modifier le texte du bouton pour refléter le nom de fichier
            TMP_Text buttonText = newButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = fileName;
            }
        }
    }
}
