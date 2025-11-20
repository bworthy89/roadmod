using System;

namespace Game.Net;

[Flags]
public enum UtilityLaneFlags
{
	SecondaryStartAnchor = 1,
	SecondaryEndAnchor = 2,
	PipelineConnection = 4,
	CutForTraffic = 8,
	VerticalConnection = 0x10
}
