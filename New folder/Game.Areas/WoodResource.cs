using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Areas;

[InternalBufferCapacity(0)]
public struct WoodResource : IBufferElementData, ISerializable
{
	public Entity m_Tree;

	public WoodResource(Entity tree)
	{
		m_Tree = tree;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Tree);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_Tree);
	}
}
