using Game.Areas;

namespace Game.Prefabs.Modes;

public interface IMapResourceMultiplier
{
	bool TryGetMultiplier(MapFeature feature, out float multiplier);
}
