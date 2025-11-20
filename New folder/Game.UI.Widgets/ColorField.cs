using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class ColorField : Field<Color>
{
	public bool hdr { get; set; }

	public bool showAlpha { get; set; }

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("hdr");
		writer.Write(hdr);
		writer.PropertyName("showAlpha");
		writer.Write(showAlpha);
	}
}
