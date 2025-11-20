using System;

namespace Game.Simulation;

[Flags]
public enum ElectricityFlowEdgeFlags : byte
{
	None = 0,
	Forward = 1,
	Backward = 2,
	Bottleneck = 4,
	BeyondBottleneck = 8,
	Disconnected = 0x10,
	ForwardBackward = 3
}
