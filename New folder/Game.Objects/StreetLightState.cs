using System;

namespace Game.Objects;

[Flags]
public enum StreetLightState : byte
{
	None = 0,
	TurnedOff = 1
}
