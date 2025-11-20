using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct ContentData : IComponentData, IQueryTypeParameter, ISerializable
{
	public ContentFlags m_Flags;

	public int m_DlcID;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		ContentFlags flags = m_Flags;
		writer.Write((uint)flags);
		int dlcID = m_DlcID;
		writer.Write(dlcID);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref int dlcID = ref m_DlcID;
		reader.Read(out dlcID);
		m_Flags = (ContentFlags)value;
	}
}
