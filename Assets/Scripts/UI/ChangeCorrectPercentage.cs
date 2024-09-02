using System;
using System.Collections;
using System.Collections.Generic;
using SO;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class ChangeCorrectPercentage : MonoBehaviour
{

    public TMP_Text playerStat;
    public GlobalVariables globalVariables;

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.isTutorialMode)
        {
            if (globalVariables.totalNotes>0)
            {
                try
                {
                    globalVariables.playerCorrectNotesPercentage = ((float)globalVariables.playerCorrectNotes / globalVariables.totalNotes) * 100;

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            playerStat.text = "Réussite: " + globalVariables.playerCorrectNotes + "/" + globalVariables.totalNotes + "\n" + globalVariables.playerCorrectNotesPercentage + "%";
        }
        else
        {
            playerStat.text = "";
        }
      
    }

    private void OnDisable()
    {
        globalVariables.playerCorrectNotes = 0;
        globalVariables.totalNotes = 0;
    }
}
