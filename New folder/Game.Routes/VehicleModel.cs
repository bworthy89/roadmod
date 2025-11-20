using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

[InternalBufferCapacity(4)]
public struct VehicleModel : IBufferElementData, ISerializable
{
	public Entity m_PrimaryPrefab;

	public Entity m_SecondaryPrefab;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity primaryPrefab = m_PrimaryPrefab;
		writer.Write(primaryPrefab);
		Entity secondaryPrefab = m_SecondaryPrefab;
		writer.Write(secondaryPrefab);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity primaryPrefab = ref m_PrimaryPrefab;
		reader.Read(out primaryPrefab);
		ref Entity secondaryPrefab = ref m_SecondaryPrefab;
		reader.Read(out secondaryPrefab);
	}
}
