using UnityEngine;
using System.Collections.Generic;

public class PianoKeyMapper : MonoBehaviour
{
    public List<Transform> pianoKeys = new List<Transform>();

    public Transform GetKeyTransform(string note)
    {
        int index = GetNoteIndex(note);
        if (index >= 0 && index < pianoKeys.Count)
        {
            return pianoKeys[index];
        }
        return null;
    }

    private int GetNoteIndex(string note)
    {
        Dictionary<string, int> noteToIndex = new Dictionary<string, int>
        {
            {"C1", 0},
            {"C#1", 1},
            {"D1", 2},
            {"D#1", 3},
            {"E1", 4},
            {"F1", 5},
            {"F#1", 6},
            {"G1", 7},
            {"G#1", 8},
            {"A1", 9},
            {"A#1", 10},
            {"B1", 11},
            {"C2", 12},
            {"C#2", 13},
            {"D2", 14},
            {"D#2", 15},
            {"E2", 16},
            {"F2", 17},
            {"F#2", 18},
            {"G2", 19},
            {"G#2", 20},
            {"A2", 21},
            {"A#2", 22},
            {"B2", 23},
            {"C3", 24},
            {"C#3", 25},
            {"D3", 26},
            {"D#3", 27},
            {"E3", 28},
            {"F3", 29},
            {"F#3", 30},
            {"G3", 31},
            {"G#3", 32},
            {"A3", 33},
            {"A#3", 34},
            {"B3", 35},
            {"C4", 36}, // Middle C
            {"C#4", 37},
            {"D4", 38},
            {"D#4", 39},
            {"E4", 40},
            {"F4", 41},
            {"F#4", 42},
            {"G4", 43},
            {"G#4", 44},
            {"A4", 45},
            {"A#4", 46},
            {"B4", 47},
            {"C5", 48},
            {"C#5", 49},
            {"D5", 50},
            {"D#5", 51},
            {"E5", 52},
            {"F5", 53},
            {"F#5", 54},
            {"G5", 55},
            {"G#5", 56},
            {"A5", 57},
            {"A#5", 58},
            {"B5", 59},
            {"C6", 60},
            {"C#6", 61},
            {"D6", 62},
            {"D#6", 63},
            {"E6", 64},
            {"F6", 65},
            {"F#6", 66},
            {"G6", 67},
            {"G#6", 68},
            {"A6", 69},
            {"A#6", 70},
            {"B6", 71},
            {"C7", 72},
            {"C#7", 73},
            {"D7", 74},
            {"D#7", 75},
            {"E7", 76},
            {"F7", 77},
            {"F#7", 78},
            {"G7", 79},
            {"G#7", 80},
            {"A7", 81},
            {"A#7", 82},
            {"B7", 83},
            {"C8", 84} // Highest C
        };


        if (noteToIndex.ContainsKey(note))
        {
            return noteToIndex[note];
        }
        return -1;
    }
}