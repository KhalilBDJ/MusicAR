using System.Collections;
using System.Collections.Generic;
using Animations;
using UnityEngine;
using xmlParser;
using symbol;
using TMPro;
using UnityEditor.Build.Content;

public class XMLPlayer : MonoBehaviour
{
    public int BPM = 60; // Le BPM de la pièce musicale
    public string xmlFilePath; // Chemin vers le fichier MusicXML
    public PianoKeyPool pianoKeyPool; // Référence au pool de touches de piano

    private XmlFacade xmlFacade; // Facade pour accéder aux données XML
    private List<Measure> measures = new List<Measure>();
    
    
    
    void Start()
    {
        if (!GameManager.Instance.isTutorialMode)
        {
            this.enabled = false;
            return;
        }
        
        xmlFacade = new XmlFacade(PlayerPrefs.GetString("SelectedFile"));
        
        measures = xmlFacade.GetMeasureList(); // Obtenir la liste des mesures à partir du XML
        //StartCoroutine(WaitForARObjectInitialization());
        if (pianoKeyPool != null)
        {
            // Initialiser XmlFacade et commencer à jouer la musique
            StartCoroutine(PlayMusic());
        }
    }

    IEnumerator WaitForARObjectInitialization()
    {
        // Attendre que l'objet avec le tag "ARObject" soit instancié
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("ARObject") != null);
        
        // Une fois l'objet trouvé, obtenir le script PianoKeyPool attaché à cet objet
        GameObject arObject = GameObject.FindGameObjectWithTag("ARObject");
        //pianoKeyPool = arObject.GetComponent<PianoKeyPool>();
        
        // Assurez-vous que PianoKeyPool a bien été obtenu
        if (pianoKeyPool != null)
        {
            // Initialiser XmlFacade et commencer à jouer la musique
            StartCoroutine(PlayMusic());
        }
        else
        {
            Debug.LogError("PianoKeyPool component not found on the ARObject.");
        }
    }


    IEnumerator PlayMusic()
    {
        yield return new WaitForSeconds(2f); 
        float secondsPerBeat = 60f / BPM; // Calculer le temps par battement en secondes
        HashSet<Note> playedNotes = new HashSet<Note>(); // Utiliser un HashSet pour stocker les notes jouées

        foreach (Measure measure in measures)
        {
            foreach (List<List<Symbol>> symbolSet in measure.GetMeasureSymbolList()) // Parcourir chaque groupe de symboles dans la mesure
            {
                foreach (List<Symbol> symbolList in symbolSet) // Chaque main, droite et gauche
                {
                    foreach (Symbol symbol in symbolList) // Chaque symbole/note dans la main
                    {
                        if (symbol is Note)
                        {
                            Note note = (Note)symbol;
                        
                            // Jouer la note principale si elle n'a pas déjà été jouée
                            if (!playedNotes.Contains(note))
                            {
                                PlayNoteInAnimation(note);
                                playedNotes.Add(note);
                            }

                            // Gérer les autres notes de l'accord, si présentes
                            if (note.HasChord())
                            {
                                foreach (Note chordNote in note.GetChordList())
                                {
                                    // Jouer chaque note de l'accord si elle n'a pas déjà été jouée
                                    if (!playedNotes.Contains(chordNote))
                                    {
                                        PlayNoteInAnimation(chordNote);
                                        playedNotes.Add(chordNote);
                                    }
                                }
                            }

                            yield return new WaitForSeconds(secondsPerBeat); // Attendre un battement avant de jouer la note suivante
                        }
                    }
                }
            }
        }
    }


    void PlayNoteInAnimation(Note note)
    {
        // Construire le nom de la note en tenant compte des accords et des altérations
        string noteName = note.GetStep();
        if (!string.IsNullOrEmpty(note.GetAccidental()))
        {
            if (note.GetAccidental() == "sharp") noteName += "#";
            else if (note.GetAccidental() == "flat") noteName += "b";
            // Gérer d'autres altérations si nécessaire
        }
        noteName += note.GetOctave();

        GameObject noteObject = pianoKeyPool.GetNoteObject(noteName); // Obtenir un objet de note du pool
        if (noteObject != null)
        {
            PianoKeyAnimation keyAnimation = noteObject.GetComponent<PianoKeyAnimation>();

            float duration = 60f / BPM * (4f / note.GetType()); // Calculer la durée de la note en secondes
            keyAnimation.PlayNote(noteName, duration - 0.1f); // Jouer la note
        }
    }


    IEnumerator StopNoteAfterDelay(PianoKeyAnimation keyAnimation, float delay)
    {
        yield return new WaitForSeconds(delay); // Attendre la durée de la note
        keyAnimation.StopNote(); // Arrêter la note
    }
}