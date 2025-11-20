using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct TaxiwayData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_SpeedLimit;

	public TaxiwayFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float speedLimit = m_SpeedLimit;
		writer.Write(speedLimit);
		TaxiwayFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float speedLimit = ref m_SpeedLimit;
		reader.Read(out speedLimit);
		reader.Read(out uint value);
		m_Flags = (TaxiwayFlags)value;
	}
}
