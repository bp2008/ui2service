# ui2service

A Windows Service that provides additional functionality for UI2, a custom web interface for Blue Iris.

The homepage for UI2 is here: https://www.ipcamtalk.com/showthread.php/93-I-made-a-better-remote-live-view-page

## Features

* Reduces bandwidth usage of UI2 by exploiting temporal redundancy in the encoding of frames.  Decoding is done all in JavaScript on the clientside, so no browser plugins or special apps are required to play this compressed format.

## Note

This application was experimental, and never served a useful purpose.
