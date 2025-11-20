using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct ElectricityConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_WantedConsumption;

	public int m_FulfilledConsumption;

	public short m_CooldownCounter;

	public ElectricityConsumerFlags m_Flags;

	public bool electricityConnected => (m_Flags & ElectricityConsumerFlags.Connected) != 0;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int wantedConsumption = m_WantedConsumption;
		writer.Write(wantedConsumption);
		int fulfilledConsumption = m_FulfilledConsumption;
		writer.Write(fulfilledConsumption);
		short cooldownCounter = m_CooldownCounter;
		writer.Write(cooldownCounter);
		ElectricityConsumerFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int wantedConsumption = ref m_WantedConsumption;
		reader.Read(out wantedConsumption);
		ref int fulfilledConsumption = ref m_FulfilledConsumption;
		reader.Read(out fulfilledConsumption);
		if (reader.context.version < Version.electricityFlashFix)
		{
			reader.Read(out int _);
		}
		else
		{
			ref short cooldownCounter = ref m_CooldownCounter;
			reader.Read(out cooldownCounter);
		}
		if (reader.context.version >= Version.notificationData)
		{
			if (reader.context.version >= Version.bottleneckNotification)
			{
				reader.Read(out byte value2);
				m_Flags = (ElectricityConsumerFlags)value2;
			}
			else
			{
				reader.Read(out bool value3);
				if (!value3)
				{
					m_Flags = ElectricityConsumerFlags.Connected;
				}
			}
		}
		if (reader.context.version < Version.buildingEfficiencyRework)
		{
			if (reader.context.version >= Version.utilityFeePrecision)
			{
				reader.Read(out float _);
			}
			else if (reader.context.version >= Version.electricityFeeEffect)
			{
				reader.Read(out int _);
			}
		}
	}
}
