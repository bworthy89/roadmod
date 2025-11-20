using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Buildings;

public struct ResourceNeeding : IBufferElementData, ISerializable
{
	public Resource m_Resource;

	public int m_Amount;

	public ResourceNeedingFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
		ResourceNeedingFlags flags = m_Flags;
		writer.Write((uint)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out sbyte value);
		m_Resource = EconomyUtils.GetResource(value);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		if (!reader.context.format.Has(FormatTags.LevelingFixReset))
		{
			reader.Read(out uint _);
			m_Flags = ResourceNeedingFlags.None;
		}
		else
		{
			reader.Read(out uint value3);
			m_Flags = (ResourceNeedingFlags)value3;
		}
	}
}
