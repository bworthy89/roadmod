using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

public struct InfoviewHeatmapData : IComponentData, IQueryTypeParameter
{
	public HeatmapData m_Type;
}
