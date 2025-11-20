using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct GoodsDeliveryVehicle : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_PathElementTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_PathElementTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_PathElementTime);
	}
}
