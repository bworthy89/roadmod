using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class EnumMember : IJsonWritable
{
	public ulong value { get; set; }

	public LocalizedString displayName { get; set; }

	public bool disabled { get; set; }

	public EnumMember(ulong value, LocalizedString displayName, bool disabled = false)
	{
		this.value = value;
		this.displayName = displayName;
		this.disabled = disabled;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("value");
		ULongWriter.WriteAsArray(writer, value);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("disabled");
		writer.Write(disabled);
		writer.TypeEnd();
	}
}
