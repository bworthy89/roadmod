using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct TaxiStand : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TaxiRequest;

	public TaxiStandFlags m_Flags;

	public ushort m_StartingFee;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity taxiRequest = m_TaxiRequest;
		writer.Write(taxiRequest);
		TaxiStandFlags flags = m_Flags;
		writer.Write((uint)flags);
		ushort startingFee = m_StartingFee;
		writer.Write(startingFee);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity taxiRequest = ref m_TaxiRequest;
		reader.Read(out taxiRequest);
		if (reader.context.version >= Version.taxiStandFlags)
		{
			reader.Read(out uint value);
			m_Flags = (TaxiStandFlags)value;
		}
		if (reader.context.version >= Version.taxiFee)
		{
			ref ushort startingFee = ref m_StartingFee;
			reader.Read(out startingFee);
		}
	}
}
