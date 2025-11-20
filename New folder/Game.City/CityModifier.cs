using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.City;

[InternalBufferCapacity(0)]
public struct CityModifier : IBufferElementData, ISerializable
{
	public float2 m_Delta;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Delta);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.modifierRefactoring)
		{
			ref float2 delta = ref m_Delta;
			reader.Read(out delta);
		}
		else
		{
			ref float y = ref m_Delta.y;
			reader.Read(out y);
		}
	}
}
