using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct PlaceableNetData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Bounds1 m_ElevationRange;

	public Entity m_UndergroundPrefab;

	public PlacementFlags m_PlacementFlags;

	public CompositionFlags m_SetUpgradeFlags;

	public CompositionFlags m_UnsetUpgradeFlags;

	public uint m_DefaultConstructionCost;

	public float m_DefaultUpkeepCost;

	public float m_SnapDistance;

	public float m_MinWaterElevation;

	public int m_XPReward;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((uint)m_PlacementFlags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_PlacementFlags = (PlacementFlags)value;
		m_SnapDistance = 8f;
	}
}
