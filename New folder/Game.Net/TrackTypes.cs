using System;

namespace Game.Net;

[Flags]
public enum TrackTypes : byte
{
	None = 0,
	Train = 1,
	Tram = 2,
	Subway = 4
}
