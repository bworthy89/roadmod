using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct Loan : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Amount;

	public uint m_LastModified;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		if (reader.context.version < Version.noAutoPaybackLoans)
		{
			reader.Read(out float _);
			reader.Read(out int _);
		}
		if (reader.context.version > Version.loanLastModified)
		{
			ref uint lastModified = ref m_LastModified;
			reader.Read(out lastModified);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int amount = m_Amount;
		writer.Write(amount);
		uint lastModified = m_LastModified;
		writer.Write(lastModified);
	}
}
