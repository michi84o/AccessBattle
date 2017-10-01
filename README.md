# AccessBattle

This is a free non-commercial implementation of the game 
"Rai-Net Access Battlers" which was originally released 
by the Japanese company '5pb.'. 
AccessBattle is a private fan project and not affiliated with
'5pb.' in any way.

Build v0.1 contains a singleplayer mode with a very stupid AI.
Build v0.3a is a complete rewrite and contains the alpha of multiplayer mode.

- Current status: Work in progress
  - Multiplayer is in alpha. There are some bugs and features are missing (WIP)
  - Singleplayer will be implemented later.
  
- Roadmap
  - User Interface + Multiplayer finished: January 2018
  - Singleplayer: April 2018
  - Documentation: August 2018 (only if I see someone actually using this code)
  
  - Known Issues:
  - Server has no user management yet. Any client is accepted, no passwords.
  - It is currently not possible to leave a game
  - Game might screw up if one client disconnects
  - Rematch does not work sometimes

The main game engine is inside a separate DLL so that people
can program their own user interface for it. The DLL should
be compatible with Mono and also run on Linux.

The user interface is implemented with WPF using .NET 4.5.
You need at least Windows Vista SP2, Windows 7 SP1 or newer 
to use it. I tested it on Windows 7.

The code is currently a complete mess. I am focusing on
making the game run. When it does I will clean it up
and add some documentation.

Third party notices:
This program uses the following external sources.
- Silkscreen font by Jason Kottke
- Newtonsoft.Json by James Newton-King

Warning! This program uses encryption for the connection to the server. Please check if this is legal in your country before using this program.

This software is GPL v3 licensed! If you use any of the code you must release your code under GPL v3 as well.
I'm NOT taking donations of any kind.
I do not accept pull requests until I have released version 1.0.

Special thanks:
5pb. and Nitroplus for creating Steins;Gate