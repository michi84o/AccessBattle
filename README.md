# AccessBattle

This is a free non-commercial implementation of the game 
"Rai-Net Access Battlers" from the Japanese company  "5pb.".

It is still in development. Right now you can play against
a very stupid AI. The final game will be playable over network.

The main game engine is inside a separate DLL so that people
can program their own user interface for it. The DLL should
be compatible with Mono and also run on Linux.

The User interface is implemented in WPF using .NET 4.5.
You need at least Windows Vista SP2, Windows 7 SP1 or newer 
to use it. I tested it on Windows 7.

The code is currently a complete mess. I am focusing on
making the game run. When it does I will clean it up
and add some documentation.