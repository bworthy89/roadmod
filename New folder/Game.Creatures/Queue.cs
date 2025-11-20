using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Creatures;

[InternalBufferCapacity(0)]
public struct Queue : IBufferElementData, ISerializable
{
	public Entity m_TargetEntity;

	public Sphere3 m_TargetArea;

	public ushort m_ObsoleteTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetEntity = m_TargetEntity;
		writer.Write(targetEntity);
		ushort obsoleteTime = m_ObsoleteTime;
		writer.Write(obsoleteTime);
		float radius = m_TargetArea.radius;
		writer.Write(radius);
		if (m_TargetArea.radius > 0f)
		{
			float3 position = m_TargetArea.position;
			writer.Write(position);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity targetEntity = ref m_TargetEntity;
		reader.Read(out targetEntity);
		ref ushort obsoleteTime = ref m_ObsoleteTime;
		reader.Read(out obsoleteTime);
		ref float radius = ref m_TargetArea.radius;
		reader.Read(out radius);
		if (m_TargetArea.radius > 0f)
		{
			ref float3 position = ref m_TargetArea.position;
			reader.Read(out position);
		}
	}
}
