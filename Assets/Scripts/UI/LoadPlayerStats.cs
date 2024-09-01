using TMPro;
using UnityEngine;

namespace UI
{
    public class LoadPlayerStats : MonoBehaviour
    {
        public TMP_Text correctNotes;
        public TMP_Text percentage;
        public TMP_Text totalNotes;
        public TMP_Text songTitle;

        private void Start()
        {
            if (PlayerPrefs.GetString("SelectedSong") != null)
            {
                string songName = PlayerPrefs.GetString("SelectedSong");
                songTitle.text = songName;
                int correctNotesNumber = 0;
                int totalNotesNumber = 0;
                float correctPercentage = 0;
                if (PlayerPrefs.GetInt("totalNotes_" + songName) > 0)
                {
                    correctNotesNumber = PlayerPrefs.GetInt("correctNotes_" + songName);
                    totalNotesNumber = PlayerPrefs.GetInt("totalNotes_" + songName);
                    correctPercentage = PlayerPrefs.GetFloat("correctNotesPercentage_" + songName);
                
                }
                correctNotes.text = "Nombre de notes correctes : " + correctNotesNumber;
                totalNotes.text = "Nombre de notes total : " + totalNotesNumber;
                percentage.text = "Pourcentage de réussite : " + correctPercentage;
           
            }
        }
    }
}
