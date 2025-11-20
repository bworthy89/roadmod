using System;

namespace Game.Prefabs;

[Flags]
public enum PublicTransportPurpose
{
	TransportLine = 1,
	Evacuation = 2,
	PrisonerTransport = 4,
	Other = 8
}
