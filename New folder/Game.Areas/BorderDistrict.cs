using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

public struct BorderDistrict : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Left;

	public Entity m_Right;

	public BorderDistrict(Entity left, Entity right)
	{
		m_Left = left;
		m_Right = right;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity left = m_Left;
		writer.Write(left);
		Entity right = m_Right;
		writer.Write(right);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity left = ref m_Left;
		reader.Read(out left);
		ref Entity right = ref m_Right;
		reader.Read(out right);
	}
}
