using System;

namespace Game.Simulation;

[Flags]
public enum GarbageTransferRequestFlags : ushort
{
	Deliver = 1,
	Receive = 2,
	RequireTransport = 4
}
