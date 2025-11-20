using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class DropdownItem<T>
{
	public T value { get; set; }

	public LocalizedString displayName { get; set; }

	public bool disabled { get; set; }

	public void Write(IWriter<T> valueWriter, IJsonWriter writer)
	{
		writer.TypeBegin(typeof(DropdownItem<T>).FullName);
		writer.PropertyName("value");
		valueWriter.Write(writer, value);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("disabled");
		writer.Write(disabled);
		writer.TypeEnd();
	}
}
