using Colossal.Mathematics;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Bounds2InputField : Field<Bounds2>
{
	public bool allowMinGreaterMax { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("allowMinGreaterMax");
		writer.Write(allowMinGreaterMax);
	}
}
