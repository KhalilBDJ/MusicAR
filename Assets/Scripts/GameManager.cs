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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isTutorialMode = isTutorial;
        }
        // Charger la nouvelle scène ici
        SceneManager.LoadScene("Player (not AR)");
    }

}
