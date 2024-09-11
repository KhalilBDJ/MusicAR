using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class ButtonClickHandler : MonoBehaviour
    {
   
        public void OnButtonClicked()
        {
            if (GameManager.Instance.isTutorialMode)
            {
                string fileName = gameObject.name; // Utiliser le nom de l'objet comme fileName

                // Sauvegarder le nom du fichier dans les PlayerPrefs
                PlayerPrefs.SetString("SelectedFile", "Assets/XML/" + fileName + ".xml");
                PlayerPrefs.SetString("SelectedSong", gameObject.name); 
                Debug.Log(PlayerPrefs.GetString("SelectedFile"));

                // Charger la scène "Player (not AR)"
                SceneManager.LoadScene("Player (not AR)");
            }
            else
            {
                PlayerPrefs.SetString("SelectedSong", gameObject.name); 
                SceneManager.LoadScene("PlayerStats");
            }
           
        }
    }
}