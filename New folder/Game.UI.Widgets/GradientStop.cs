using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public struct GradientStop : IJsonWritable
{
	public float offset;

	public Color32 color;

	public GradientStop(float offset, Color32 color)
	{
		this.offset = offset;
		this.color = color;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("offset");
		writer.Write(offset);
		writer.PropertyName("color");
		writer.Write(color);
		writer.TypeEnd();
	}
}
