using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct StackData : IComponentData, IQueryTypeParameter, ISerializable
{
	public Bounds1 m_FirstBounds;

	public Bounds1 m_MiddleBounds;

	public Bounds1 m_LastBounds;

	public StackDirection m_Direction;

	public bool3 m_DontScale;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((byte)m_Direction);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		m_MiddleBounds = new Bounds1(-1f, 1f);
		m_Direction = (StackDirection)value;
	}
}
