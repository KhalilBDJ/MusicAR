using System;

public class NotePlayedEventArgs : EventArgs
{
    public string Note { get; set; }
    public bool ChangeColor { get; set; }

    public NotePlayedEventArgs(string note, bool changeColor)
    {
        Note = note;
        ChangeColor = changeColor;
    }
}