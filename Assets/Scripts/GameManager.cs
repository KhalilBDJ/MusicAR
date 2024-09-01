using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool isTutorialMode;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        
    }
    
    public void SetTutorialMode(bool isTutorial)
    {
        if (GameManager.Instance != null && isTutorial)
        {
            GameManager.Instance.isTutorialMode = isTutorial;
            SceneManager.LoadScene("Song Selection");
        }
        else
        {
            SceneManager.LoadScene("Player (not AR)");
        }
        
    }

    public void OpenSongSelection()
    {
        SceneManager.LoadScene("Song Selection");
        GameManager.Instance.isTutorialMode = false;
    }

    public void OpenPlayerStats()
    {
        SceneManager.LoadScene("PlayerStats");
    }

}
