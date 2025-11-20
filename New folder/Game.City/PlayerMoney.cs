using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.City;

public struct PlayerMoney : IComponentData, IQueryTypeParameter, ISerializable
{
	public const int kMaxMoney = 2000000000;

	private int m_Money;

	public bool m_Unlimited;

	public int money
	{
		get
		{
			if (!m_Unlimited)
			{
				return m_Money;
			}
			return 2000000000;
		}
	}

	public PlayerMoney(int amount)
	{
		m_Money = math.clamp(amount, -2000000000, 2000000000);
		m_Unlimited = false;
	}

	public void Add(int value)
	{
		m_Money = math.clamp(m_Money + value, -2000000000, 2000000000);
	}

	public void Subtract(int amount)
	{
		Add(-amount);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int value = m_Money;
		writer.Write(value);
		bool unlimited = m_Unlimited;
		writer.Write(unlimited);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int value = ref m_Money;
		reader.Read(out value);
		if (reader.context.version >= Version.unlimitedMoneyAndUnlockAllOptions)
		{
			ref bool unlimited = ref m_Unlimited;
			reader.Read(out unlimited);
		}
	}
}
