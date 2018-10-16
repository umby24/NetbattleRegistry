# Netbattle Registry
This is a console based, C# reimplementation of the [Pokemon Netbattle]() registry program.

The registry program is responsible for the server listing you would see in game. Each server on startup would call out to the registry to be listed.

The registry also handles unique server registration, an attempt at making sure a servers name cannot be stolen from other users of the game.

# Why?
This is part of a project of mine to port the old game to a newer language. This is the smallest chunk of code to work on, and was within a day's work to complete. 

# Known Issues:
- If a server registers from localhost, the IP broadcast to clients will be 127.0.0.1, so if a client on another machine tries to connect, this presents issues. 