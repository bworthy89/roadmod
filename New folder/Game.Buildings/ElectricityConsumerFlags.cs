using System;

namespace Game.Buildings;

[Flags]
public enum ElectricityConsumerFlags : byte
{
	None = 0,
	Connected = 1,
	NoElectricityWarning = 2,
	BottleneckWarning = 4
}
