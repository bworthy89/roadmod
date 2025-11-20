using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class UIntSliderField : UIntField
{
	[CanBeNull]
	public string unit { get; set; }

	public bool scaleDragVolume { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("unit");
		writer.Write(unit);
		writer.PropertyName("scaleDragVolume");
		writer.Write(scaleDragVolume);
	}
}
