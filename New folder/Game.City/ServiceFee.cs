using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.City;

public struct ServiceFee : IBufferElementData, ISerializable
{
	public PlayerResource m_Resource;

	public float m_Fee;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		PlayerResource resource = m_Resource;
		writer.Write((int)resource);
		float fee = m_Fee;
		writer.Write(fee);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.purpose == Purpose.NewGame)
		{
			reader.Read(out int value);
			m_Resource = (PlayerResource)value;
			reader.Read(out float _);
			m_Fee = GetDefaultFee(m_Resource);
			return;
		}
		reader.Read(out int value3);
		m_Resource = (PlayerResource)value3;
		ref float fee = ref m_Fee;
		reader.Read(out fee);
		if (reader.context.version < Version.waterFeeReset && m_Resource == PlayerResource.Water)
		{
			m_Fee = 0.3f;
		}
	}

	public float GetDefaultFee(PlayerResource resource)
	{
		return resource switch
		{
			PlayerResource.BasicEducation => 100f, 
			PlayerResource.SecondaryEducation => 200f, 
			PlayerResource.HigherEducation => 300f, 
			PlayerResource.Healthcare => 100f, 
			PlayerResource.Garbage => 0.1f, 
			PlayerResource.Electricity => 0.2f, 
			PlayerResource.Water => 0.1f, 
			_ => 0f, 
		};
	}
}
