using System;

namespace Game.Objects;

[Flags]
public enum NetObjectFlags : byte
{
	IsClear = 1,
	TrackPassThrough = 2,
	Backward = 4
}
