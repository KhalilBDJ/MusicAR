using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonBehaviour : MonoBehaviour
{
   public void GoBack()
   {
      SceneManager.LoadScene("Welcome scene");
   }
}
