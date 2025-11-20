using System;

namespace Game.Net;

[Flags]
public enum UtilityTypes : byte
{
	None = 0,
	WaterPipe = 1,
	SewagePipe = 2,
	StormwaterPipe = 4,
	LowVoltageLine = 8,
	Fence = 0x10,
	Catenary = 0x20,
	HighVoltageLine = 0x40,
	Resource = 0x80
}
