using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct Route : IComponentData, IQueryTypeParameter, ISerializable
{
	public RouteFlags m_Flags;

	public uint m_OptionMask;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		RouteFlags flags = m_Flags;
		writer.Write((uint)flags);
		uint optionMask = m_OptionMask;
		writer.Write(optionMask);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		m_Flags = (RouteFlags)value;
		if (reader.context.version >= Version.routePolicies)
		{
			ref uint optionMask = ref m_OptionMask;
			reader.Read(out optionMask);
		}
	}
}
