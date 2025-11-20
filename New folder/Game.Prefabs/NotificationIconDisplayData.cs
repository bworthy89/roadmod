using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct NotificationIconDisplayData : IComponentData, IQueryTypeParameter, IEnableableComponent, ISerializable, ISerializeAsEnabled
{
	public float2 m_MinParams;

	public float2 m_MaxParams;

	public int m_IconIndex;

	public uint m_CategoryMask;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write((m_MinParams + m_MaxParams) * 0.5f);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out m_MinParams);
		m_MaxParams = m_MinParams;
		m_IconIndex = 0;
		m_CategoryMask = 2147483648u;
	}
}
