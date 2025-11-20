using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct MasterLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public MasterLaneFlags m_Flags;

	public uint m_Group;

	public ushort m_MinIndex;

	public ushort m_MaxIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Group);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref uint value = ref m_Group;
		reader.Read(out value);
		if (reader.context.version < Version.laneCountOverflowFix)
		{
			reader.Read(out byte value2);
			reader.Read(out value2);
		}
	}
}
