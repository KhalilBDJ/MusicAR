using System;
using System.Collections.Generic;

public class NotePlayedEventArgs : EventArgs
{
    public List<string> Notes { get; set; }
    public bool ChangeColor { get; set; }

    public NotePlayedEventArgs(List<string> notes, bool changeColor)
    {
        Notes = notes;
        ChangeColor = changeColor;
    }
}