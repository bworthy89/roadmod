using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Objects;

public struct Damaged : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Damage;

	public Damaged(float3 damage)
	{
		m_Damage = damage;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(m_Damage);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.damageTypes)
		{
			ref float3 damage = ref m_Damage;
			reader.Read(out damage);
		}
		else
		{
			ref float y = ref m_Damage.y;
			reader.Read(out y);
		}
	}
}
