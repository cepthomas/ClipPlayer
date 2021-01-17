# ClipPlayer
A tool for playing audio and midi files. This is intended to be used from the command line or Windows Explorer "Open With".
It displays a small UI so that playing can be stopped.

Midi playing is considered just "good enough" due to the inherent limitations of the windows multimedia timer.

# Usage
It is designed to run without arguments but these options are available:
```
ClipPlayer.exe [-vol val] [-wdev val] [-lat val] [-mdev val] [-drch val] [-tmp val] wav|mpr3|wav file
    vol: volume from 0 to 1
    wdev: wav device name
    lat: latency
    mdev: midi device name
    drch: map this channel to drum channel
    tmp: tempo/bpm if not in file
```

# Third Party
This application uses these FOSS components:
- NAudio DLL including modified controls and midi file utilities: [NAudio](https://github.com/naudio/NAudio) (Microsoft Public License).
- Main icon: [Charlotte Schmidt](http://pattedemouche.free.fr/) (Copyright © 2009 of Charlotte Schmidt).

