using System;

namespace Game.Net;

[Flags]
public enum ConnectionLaneFlags
{
	Start = 1,
	Distance = 2,
	Outside = 4,
	SecondaryStart = 8,
	SecondaryEnd = 0x10,
	Road = 0x20,
	Track = 0x40,
	Pedestrian = 0x80,
	Parking = 0x100,
	AllowMiddle = 0x200,
	AllowCargo = 0x400,
	Airway = 0x800,
	Inside = 0x1000,
	Area = 0x2000,
	Disabled = 0x4000,
	AllowEnter = 0x8000,
	AllowExit = 0x10000,
	NoRestriction = 0x20000
}
