using System;

namespace Game.Tutorials;

[Serializable]
[Flags]
public enum ObjectPlacementTriggerFlags
{
	AllowSubObject = 1,
	RequireElevation = 2,
	RequireRoadConnection = 4,
	RequireTransformerConnection = 8,
	RequireSewageOutletConnection = 0x10,
	RequireElectricityProducerConnection = 0x20,
	RequireOutsideConnection = 0x40,
	RequireResourceConnection = 0x80,
	RequireResourceExtractorConnection = 0x100,
	RequireServiceUpgrade = 0x200
}
