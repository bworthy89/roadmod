using Unity.Entities;

namespace Game.Prefabs;

public struct PlaceableNetPieceData : IComponentData, IQueryTypeParameter
{
	public uint m_ConstructionCost;

	public uint m_ElevationCost;

	public float m_UpkeepCost;
}
