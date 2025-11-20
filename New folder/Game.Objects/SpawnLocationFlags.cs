using System;

namespace Game.Objects;

[Flags]
public enum SpawnLocationFlags
{
	AllowEnter = 1,
	ParkedVehicle = 2,
	AllowExit = 4
}
