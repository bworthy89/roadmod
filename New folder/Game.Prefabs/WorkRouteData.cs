using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Net;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

public struct WorkRouteData : IComponentData, IQueryTypeParameter, ISerializable
{
	public RoadTypes m_RoadType;

	public SizeClass m_SizeClass;

	public MapFeature m_MapFeature;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_SizeClass);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_RoadType = RoadTypes.None;
		m_SizeClass = (SizeClass)value;
		m_MapFeature = MapFeature.None;
	}
}
