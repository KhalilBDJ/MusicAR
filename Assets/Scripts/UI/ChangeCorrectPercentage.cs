using System;
using System.Collections;
using System.Collections.Generic;
using SO;
using TMPro;
using UnityEngine;

public class ChangeCorrectPercentage : MonoBehaviour
{

    public TMP_Text playerStat;
    public GlobalVariables globalVariables;

    // Update is called once per frame
    void Update()
    {
        if (globalVariables.totalNotes>0)
        {
            globalVariables.playerCorrectNotesPercentage =
                (globalVariables.playerCorrectNotes / globalVariables.totalNotes) * 100;
        }
        playerStat.text = "Réussite: " + globalVariables.playerCorrectNotes + "/" + globalVariables.totalNotes + "\n" + globalVariables.playerCorrectNotesPercentage + "%";
    }

    private void OnDisable()
    {
        globalVariables.playerCorrectNotes = 0;
        globalVariables.totalNotes = 0;
    }
}
