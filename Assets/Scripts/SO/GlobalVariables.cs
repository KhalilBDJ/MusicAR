using Unity.VisualScripting;
using UnityEngine;

namespace SO
{
    [CreateAssetMenu(fileName = "GlobalVariables", menuName = "ScriptableObjects/GlobalVariables", order = 1)]
    public class GlobalVariables : ScriptableObject
    {
        public int playerCorrectNotes;
        public float playerCorrectNotesPercentage;
        public int totalNotes;

        
    }
}