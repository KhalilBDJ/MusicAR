using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using xmlParser;
using symbol;

public class XMLPlayer : MonoBehaviour
{
    public int BPM = 120; // Le BPM de la pièce musicale
    public string xmlFilePath; // Chemin vers le fichier MusicXML
    public PianoKeyPool pianoKeyPool; // Référence au pool de touches de piano

    private XmlFacade xmlFacade; // Facade pour accéder aux données XML
    
    
    void Start()
    {
        StartCoroutine(WaitForARObjectInitialization());
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
            xmlFacade = new XmlFacade(xmlFilePath);
            StartCoroutine(PlayMusic());
        }
        else
        {
            Debug.LogError("PianoKeyPool component not found on the ARObject.");
        }
    }


    IEnumerator PlayMusic()
    {
        List<Measure> measures = xmlFacade.GetMeasureList(); // Obtenir la liste des mesures à partir du XML
        float secondsPerBeat = 60f / BPM; // Calculer le temps par battement en secondes

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
                            string noteName = note.GetStep() + note.GetOctave(); // Construire le nom de la note

                            GameObject noteObject = pianoKeyPool.GetNoteObject(noteName); // Obtenir un objet de note du pool
                            if (noteObject != null)
                            {
                                PianoKeyAnimation keyAnimation = noteObject.GetComponent<PianoKeyAnimation>();
                                keyAnimation.PlayNote(noteName); // Jouer la note

                                float duration = secondsPerBeat * (4f / note.GetType()); // Calculer la durée de la note en secondes
                                StartCoroutine(StopNoteAfterDelay(keyAnimation, duration)); // Planifier l'arrêt de la note
                            }
                        }

                        yield return new WaitForSeconds(secondsPerBeat); // Attendre un battement avant de jouer la note suivante
                    }
                }
            }
        }
    }

    IEnumerator StopNoteAfterDelay(PianoKeyAnimation keyAnimation, float delay)
    {
        yield return new WaitForSeconds(delay); // Attendre la durée de la note
        keyAnimation.StopNote(); // Arrêter la note
    }
}
