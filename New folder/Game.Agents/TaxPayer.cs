using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Agents;

public struct TaxPayer : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_UntaxedIncome;

	public int m_AverageTaxRate;

	public int m_AverageTaxPaid;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int untaxedIncome = m_UntaxedIncome;
		writer.Write(untaxedIncome);
		int averageTaxRate = m_AverageTaxRate;
		writer.Write(averageTaxRate);
		int averageTaxPaid = m_AverageTaxPaid;
		writer.Write(averageTaxPaid);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int untaxedIncome = ref m_UntaxedIncome;
		reader.Read(out untaxedIncome);
		if (reader.context.version >= Version.averageTaxRate)
		{
			ref int averageTaxRate = ref m_AverageTaxRate;
			reader.Read(out averageTaxRate);
		}
		else
		{
			m_AverageTaxRate = 10;
		}
		if (reader.context.format.Has(FormatTags.TrackCompanyCustomersAndTaxes))
		{
			ref int averageTaxPaid = ref m_AverageTaxPaid;
			reader.Read(out averageTaxPaid);
		}
	}
}
