using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class ButtonClickHandler : MonoBehaviour
    {
   
        public void OnButtonClicked()
        {
            string fileName = gameObject.name; // Utiliser le nom de l'objet comme fileName

            // Sauvegarder le nom du fichier dans les PlayerPrefs
            PlayerPrefs.SetString("SelectedFile", "Assets/XML/" + fileName + ".xml");
            Debug.Log(PlayerPrefs.GetString("SelectedFile"));

            // Charger la scène "Player (not AR)"
            SceneManager.LoadScene("Player (not AR)");
        }
    }
}