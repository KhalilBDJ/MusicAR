# Welcome to MusicAR

The goal of this application is to create an Augmented Reality experience for music learners.  
This is just a prototype and a proof of concept.

The user will be able to import a PDF version of a music sheet, and the application will show what keys to press in a UI similar to this:

![Capture d’écran 2024-09-11 155113](https://github.com/user-attachments/assets/3765fa28-7eb6-4d78-a4aa-3a95a67cbd5b)

In order to import the sheets, Audiveris is used to perform OMR manually. Later on, the architecture will look something like this:

![Capture d’écran 2024-09-11 155145](https://github.com/user-attachments/assets/ba3e884c-e45f-487a-ab04-cf11fda04c6d)

Then, the [DrawSheetMusic](https://github.com/qy-zhang/DrawSheetMusic) project is used in order to parse the MusicXML file and translate it so the user can play the correct notes on the music sheet.

Finally, to detect the correct notes and wrong notes, Spotify’s [Basic Pitch](https://github.com/spotify/basic-pitch) model in ONNX is used (see script `MicrophoneRecorder` for more details) and adapted for real-time detection.

## Application Needs

The application still needs a lot of fixes. Here is a list of some of them:

- UI/UX creation and implementation.
- Bug concerning Sentis package where workers don't dispose due to them not being called on the main thread.
- Looking for a better way to store the user’s progress.
- Many unused scripts need to be removed. The code needs to be refactored and cleaned up.
- MusicXML parser is not perfect; either a new parser should be developed or a better one should be found.

## Future Development

I am planning on continuing the development of this application and implementing new functionality for other instruments (and use the contours of the Basic Pitch output).

Here are some features I want to implement:

- Show the music sheet in AR and indicate the current position on the sheet.
- Correct the rhythm.
- Add a metronome.
- Implement correction for instruments that use vibrato (explore the contours output).
- Add ear training.
- Daily exercises.
- Chord tracking for jazz standards.

## How to Try

If you want to try it, start the Welcome Scene and play with it.

Thanks! Don’t hesitate to comment on the repo !
