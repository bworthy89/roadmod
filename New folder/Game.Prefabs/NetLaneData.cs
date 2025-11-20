using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct NetLaneData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_PathfindPrefab;

	public LaneFlags m_Flags;

	public float m_Width;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity pathfindPrefab = m_PathfindPrefab;
		writer.Write(pathfindPrefab);
		LaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		float width = m_Width;
		writer.Write(width);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity pathfindPrefab = ref m_PathfindPrefab;
		reader.Read(out pathfindPrefab);
		reader.Read(out uint value);
		ref float width = ref m_Width;
		reader.Read(out width);
		m_Flags = (LaneFlags)value;
	}
}
