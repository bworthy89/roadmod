using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Citizens;

public struct Household : IComponentData, IQueryTypeParameter, ISerializable
{
	public HouseholdFlags m_Flags;

	public int m_Resources;

	public short m_ConsumptionPerDay;

	public uint m_ShoppedValuePerDay;

	public uint m_ShoppedValueLastDay;

	public uint m_LastDayFrameIndex;

	public int m_SalaryLastDay;

	public int m_MoneySpendOnBuildingLevelingLastDay;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		HouseholdFlags flags = m_Flags;
		writer.Write((byte)flags);
		int resources = m_Resources;
		writer.Write(resources);
		short consumptionPerDay = m_ConsumptionPerDay;
		writer.Write(consumptionPerDay);
		uint shoppedValuePerDay = m_ShoppedValuePerDay;
		writer.Write(shoppedValuePerDay);
		uint shoppedValueLastDay = m_ShoppedValueLastDay;
		writer.Write(shoppedValueLastDay);
		uint lastDayFrameIndex = m_LastDayFrameIndex;
		writer.Write(lastDayFrameIndex);
		int salaryLastDay = m_SalaryLastDay;
		writer.Write(salaryLastDay);
		int moneySpendOnBuildingLevelingLastDay = m_MoneySpendOnBuildingLevelingLastDay;
		writer.Write(moneySpendOnBuildingLevelingLastDay);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		if (reader.context.version < Version.householdRandomSeedRemoved)
		{
			reader.Read(out uint _);
		}
		ref int resources = ref m_Resources;
		reader.Read(out resources);
		ref short consumptionPerDay = ref m_ConsumptionPerDay;
		reader.Read(out consumptionPerDay);
		m_Flags = (HouseholdFlags)value;
		if (m_Resources < 0)
		{
			m_Resources = 0;
		}
		if (reader.context.format.Has(FormatTags.HouseholdConsumptionFix))
		{
			ref uint shoppedValuePerDay = ref m_ShoppedValuePerDay;
			reader.Read(out shoppedValuePerDay);
			ref uint shoppedValueLastDay = ref m_ShoppedValueLastDay;
			reader.Read(out shoppedValueLastDay);
			ref uint lastDayFrameIndex = ref m_LastDayFrameIndex;
			reader.Read(out lastDayFrameIndex);
		}
		if (reader.context.format.Has(FormatTags.TrackCitizenEconomyStats))
		{
			ref int salaryLastDay = ref m_SalaryLastDay;
			reader.Read(out salaryLastDay);
			ref int moneySpendOnBuildingLevelingLastDay = ref m_MoneySpendOnBuildingLevelingLastDay;
			reader.Read(out moneySpendOnBuildingLevelingLastDay);
		}
	}
}
