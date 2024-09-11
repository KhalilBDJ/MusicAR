using System.Collections;
using System.Collections.Generic;
using SO;
using TMPro;
using UnityEngine;

namespace Animations
{
    public class PianoKeyAnimation : MonoBehaviour
    {

        private readonly float _growthRate = 0.05f;
        public string noteName; // Ajouté pour stocker le nom de la note
        public TMP_Text successPercentage;
        public GlobalVariables globalVariables;

    
        private MicrophoneRecorder _analyzer;
        private GameObject _scripts;
        private int _totalNotesCount;
        private int _notesPlayedAndCorrectCount;

        private bool _isPlaying;
        private PianoKeyPool _pianoKeyPool;
        private bool _shouldReturnToPool;
        private Vector3 _initialScale; // Échelle initiale de la note
        private GameObject _contactObject;
        private float _duration;
        private bool _tutorial;
        private List<string> _currentNotes;

        private bool _noteCounted;
        private bool _correctNoteCounted;


        void Awake()
        {
            _scripts = GameObject.FindGameObjectWithTag("Script");
            _analyzer = _scripts.GetComponent<MicrophoneRecorder>();
            _pianoKeyPool = FindObjectOfType<PianoKeyPool>();
            _initialScale = transform.localScale; // Sauvegarde de l'échelle initiale
            _currentNotes = new List<string>();
            _noteCounted = false;
            _correctNoteCounted = false;
        
            _tutorial = GameManager.Instance.isTutorialMode;
       
        }
    
        private void OnEnable()
        {
            if (_tutorial)
            {
                _analyzer.NoteChanged += OnNotesChanged;
            }
        }

        private void OnDisable()
        {
            if (_tutorial)
            {
                _analyzer.NoteChanged -= OnNotesChanged;

            }
        }
    

        private void Start()
        {
        
            if (_tutorial)
            {
                var transform1 = transform;
                var localPosition = transform1.localPosition;
                localPosition = new Vector3(localPosition.x, localPosition.y + 0.5f + _duration/20, localPosition.z);
                transform1.localPosition = localPosition;
                _contactObject = GameObject.FindGameObjectWithTag("Piano");
            }
        }

        void Update()
        {
            if (!_tutorial)
            {
                if (_isPlaying)
                {
                    // Fait grandir la note uniquement vers le haut (en augmentant sa taille et en ajustant sa position pour qu'elle grandisse vers le haut)
                    float growthAmount = _growthRate * Time.deltaTime;
                    transform.localScale += new Vector3(0, growthAmount, 0);
                    transform.localPosition += new Vector3(0, growthAmount / 2, 0); // Ajuste la position pour que la croissance semble se faire vers le haut
                }
                else
                {
                    // Une fois la note arrêtée, elle se détache et monte indéfiniment
                    transform.localPosition += new Vector3(0, _growthRate, 0) * Time.deltaTime;
                }

                // Si la note doit être retournée au pool et s'éloigne suffisamment, la remettre au pool
                if (_shouldReturnToPool && transform.localPosition.y > 50) // Condition modifiée pour utiliser la position en y
                {
                    _shouldReturnToPool = false;
                    _pianoKeyPool.ReturnNoteObject(gameObject, noteName);
                }
            }
            else
            {
                // Calculer la vitesse pour que le point P traverse la note en '_duration' secondes.
                float requiredSpeed = 0.1f;

                transform.localScale = new Vector3(transform.localScale.x, (requiredSpeed * _duration) / 2, transform.localScale.z);

                // Appliquer la vitesse ajustée
                transform.localPosition += new Vector3(0, -requiredSpeed, 0) * Time.deltaTime;

                if (!_isPlaying)
                {
                    transform.localScale = new Vector3(transform.localScale.x, _duration / 10, transform.localScale.z);
                }

                if (transform.localPosition.y >= _contactObject.transform.localPosition.y - transform.localScale.y && transform.localPosition.y <= _contactObject.transform.localPosition.y + transform.localScale.y / 2)
                {
                    if (!_noteCounted)
                    {
                        globalVariables.totalNotes += 1;
                        _noteCounted = true;
                    }
                    if (!_currentNotes.Contains(noteName))
                    {
                        GetComponent<Renderer>().material.color = Color.red;
                    }
                    else
                    {
                        GetComponent<Renderer>().material.color = Color.blue;
                        if (!_correctNoteCounted)
                        {
                            globalVariables.playerCorrectNotes += 1;
                            _correctNoteCounted = true;
                        }
                    }
                }

                if (transform.localPosition.y <= _contactObject.transform.localPosition.y - transform.localScale.y)
                {
                    _isPlaying = true;
                    transform.localScale = _initialScale;
                    _pianoKeyPool.ReturnNoteObject(gameObject, noteName);
                    StopNote();
                }
            }
        }


        private void OnNotesChanged(object sender, NotePlayedEventArgs e)
        {
            _currentNotes = e.Notes;
        }

        public void PlayNote(string newNoteName, float duration)
        {
            if (!_tutorial)
            {
                noteName = newNoteName;
                _isPlaying = true;
                _shouldReturnToPool = false;
                StartCoroutine(StopNoteAfterDuration(duration)); // Démarrer la coroutine avec la durée de la note
            }
            else
            {
                noteName = newNoteName;
                _duration = duration;
                _isPlaying = false;
                _shouldReturnToPool = false;
            }
        }
      
    
        public void PlayNote(string newNoteName)  
        {
            noteName = newNoteName;  
            _isPlaying = true;
            _shouldReturnToPool = false;  
        }

        IEnumerator StopNoteAfterDuration(float duration)
        {
            yield return new WaitForSeconds(duration); // Attendre la durée spécifiée
            StopNote(); // Arrêter la note
        }

        public void StopNote()
        {
            _isPlaying = false;
            _shouldReturnToPool = true;
        }
    }
}