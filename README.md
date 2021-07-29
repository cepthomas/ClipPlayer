# ClipPlayer
A tool for playing audio and midi files.
- Midi playing is considered just "good enough" due to the inherent limitations of the windows multimedia timer.
- Best way to use is to associate with audio files (.mid, .wav, .mp3, .m4a, .flac) so it can be started with a simple click.
- It displays a small UI so that playing can be stopped/started.
- UI is also used for editing your settings: output devices etc.
- It is built as a single instance app. A simple IPC mechanism is used to send subsequent instance args to the primary instance.
- To support development of the IPC there is a rudimentary cross-process logger.


# Third Party
This application uses these FOSS components:
- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Json processor: [Newtonsoft](https://github.com/JamesNK/Newtonsoft.Json) (MIT).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).

