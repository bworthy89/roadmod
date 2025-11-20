using System;

namespace Game.Simulation;

[Flags]
public enum MaintenanceType : byte
{
	Park = 1,
	Road = 2,
	Snow = 4,
	Vehicle = 8,
	None = 0
}
