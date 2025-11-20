using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct VehicleSideEffectData : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Min;

	public float3 m_Max;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 min = m_Min;
		writer.Write(min);
		float3 max = m_Max;
		writer.Write(max);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 min = ref m_Min;
		reader.Read(out min);
		ref float3 max = ref m_Max;
		reader.Read(out max);
	}
}
