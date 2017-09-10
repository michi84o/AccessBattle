# AccessBattle

This is a free non-commercial implementation of the game 
"Rai-Net Access Battlers" which was originally released 
by the Japanese company '5pb.'. 
AccessBattle is a private fan project and not affiliated with
'5pb.' in any way.

It is still in development. 
Build v0.1 contains a singleplayer mode with a very stupid AI.
Build v0.2a is a complete rewrite and contains the alpha of multiplayer mode.

- Current status: Work in progress (taking a break until mid September 2017)
  - Multiplayer is in alpha. There are some bugs and features are mssing (WIP)
  - Singleplayer will be implemented later.
  
- Known Issues:
  - Network game menu won't update game list after game finished (fixed in code)
  - Server has no user management yet. Any client is accepted, no passwords.
  - If you abort during join the menus will be gone
  - It is currently not possible to leave a game
  - Game might screw up if one client disconnects
  - No indication which cards can be seen by opponent (fixed in code)
  - Created game is not joined if Non-ASCII characters were used for its name (WIP)

I do not accept pull requests until I have released the first fully working version.

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

Special thanks:
5pb. and Nitroplus for creating Steins;Gate