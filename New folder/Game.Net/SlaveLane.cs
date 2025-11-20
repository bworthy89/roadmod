using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct SlaveLane : IComponentData, IQueryTypeParameter, ISerializable
{
	public SlaveLaneFlags m_Flags;

	public uint m_Group;

	public ushort m_MinIndex;

	public ushort m_MaxIndex;

	public ushort m_SubIndex;

	public ushort m_MasterIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		SlaveLaneFlags flags = m_Flags;
		writer.Write((uint)flags);
		uint value = m_Group;
		writer.Write(value);
		ushort subIndex = m_SubIndex;
		writer.Write(subIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		ref uint value2 = ref m_Group;
		reader.Read(out value2);
		if (reader.context.version >= Version.laneCountOverflowFix)
		{
			ref ushort subIndex = ref m_SubIndex;
			reader.Read(out subIndex);
		}
		else
		{
			reader.Read(out byte value3);
			reader.Read(out byte _);
			m_SubIndex = value3;
		}
		m_Flags = (SlaveLaneFlags)value;
	}
}
