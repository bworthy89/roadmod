using System;

namespace Game.Net;

[Flags]
public enum FlowDirection : byte
{
	None = 0,
	Forward = 1,
	Backward = 2,
	Both = 3
}
