# AccessBattle

This is a free non-commercial implementation of the game 
"Rai-Net Access Battlers" which was originally released 
by the Japanese company '5pb.'. 
AccessBattle is a private fan project and not affiliated with
'5pb.' in any way.

![Screenshot of client](Images/screenshot1.png)

## Status

- Development discontinued

I started this project in Christmas 2016 after finishing the Steins;Gate visual novel. It took me nearly  two years to bring this to version 1.0. The basics are working but the functionality lacks polish. This project was used by me as a playground to try new stuff like client-server encryption, database access and AI. I don't have the motivation to continue this project anymore and want to move on. Feel free to fork this repository and improve it as you like. I hope my source code makes enough sense to other people.

- Known Issues:
  - Same user can login multiple times. Not sure if bug or feature.
  - Game might screw up if one client disconnects
  - Exit Game does not work in single player
  - Even though the UI uses upper case letters only, login still differentiates between upper and lower case.
  
- TODO
  - Reconnects
  - Prevent Login Spamming and Account Hacking
  - Matchmaking for random matches
  - Replace font to support lower case and at least Hiragana and Katakana.

## Program Structure
  
The main game engine is inside a separate DLL so that people
can program their own user interface for it. The DLL should
be compatible with Mono and also run on Linux.

The main user interface is implemented with WPF using .NET 4.5.
You need at least Windows Vista SP2, Windows 7 SP1 or newer 
to use it. I tested it on Windows 7.

Here is an example of a console based UI:

![Screenshot of client](Images/console.png)

## Third Party Notices

This program uses the following external sources.
- Silkscreen font by Jason Kottke
- Newtonsoft.Json by James Newton-King
- MySqlConnector by Bradley Grainger (I also contributed some lines of code to this)

Warning! This program uses encryption for the connection to the server. Please check if this is legal in your country before using this program.

## Other Notices

**I'm NOT taking donations of any kind.**


## Special thanks
5pb. and Nitroplus for creating Steins;Gate

## License
**This software is GPL v3 licensed! If you use any of the code you must release your code under GPL v3 as well.**