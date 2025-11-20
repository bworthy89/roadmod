using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Companies;

public struct StorageTransferRequest : IBufferElementData, ISerializable
{
	public StorageTransferFlags m_Flags;

	public Resource m_Resource;

	public int m_Amount;

	public Entity m_Target;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		StorageTransferFlags flags = m_Flags;
		writer.Write((byte)flags);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
		Entity target = m_Target;
		writer.Write(target);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		reader.Read(out sbyte value2);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		ref Entity target = ref m_Target;
		reader.Read(out target);
		m_Flags = (StorageTransferFlags)value;
		m_Resource = EconomyUtils.GetResource(value2);
	}
}
