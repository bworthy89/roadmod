using Colossal.UI.Binding;

namespace Game.UI.InGame;

public struct TaxResource : IJsonReadable, IJsonWritable
{
	public int m_Resource;

	public int m_AreaType;

	public void Read(IJsonReader reader)
	{
		reader.ReadMapBegin();
		reader.ReadProperty("resource");
		reader.Read(out m_Resource);
		reader.ReadProperty("area");
		reader.Read(out m_AreaType);
		reader.ReadMapEnd();
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("resource");
		writer.Write(m_Resource);
		writer.PropertyName("area");
		writer.Write(m_AreaType);
		writer.TypeEnd();
	}
}
