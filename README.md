# ClipPlayer

A tool for playing audio and midi files.

- Midi playing is considered just "good enough" due to the inherent limitations of the windows multimedia timer.
- Best way to use is to associate with audio files (.mid, .wav, .mp3, .m4a, .flac) so it can be started with a simple click.
- It displays a small UI so that playing can be stopped/started/looped.
- UI is also used for editing your settings: output devices etc. Note that not all presented audio/midi options pertain to this application.
- It is built as a single instance app. Second instance sends the filename to the primary via IPC.
- Requires VS2022 and .NET6.


# External Components

- [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Application icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).
- Button icons: [Glyphicons Free](http://glyphicons.com/) (CC BY 3.0).
