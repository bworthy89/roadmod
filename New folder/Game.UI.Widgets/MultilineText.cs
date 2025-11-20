using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class MultilineText : NamedWidget
{
	public string icon { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("icon");
		writer.Write(icon);
	}
}
