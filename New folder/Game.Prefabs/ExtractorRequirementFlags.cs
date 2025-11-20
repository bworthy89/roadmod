using System;

namespace Game.Prefabs;

[Flags]
public enum ExtractorRequirementFlags
{
	None = 0,
	RouteConnect = 1,
	NetConnect = 2
}
