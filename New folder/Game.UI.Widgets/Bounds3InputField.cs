using Colossal.Mathematics;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Bounds3InputField : Field<Bounds3>
{
	public bool allowMinGreaterMax { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("allowMinGreaterMax");
		writer.Write(allowMinGreaterMax);
	}
}
