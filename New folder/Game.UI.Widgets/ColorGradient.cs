using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public struct ColorGradient : IJsonWritable
{
	public GradientStop[] stops;

	public ColorGradient(GradientStop[] stops)
	{
		this.stops = stops;
	}

	public static explicit operator ColorGradient(Gradient gradient)
	{
		List<GradientStop> list = new List<GradientStop>();
		GradientColorKey[] colorKeys = gradient.colorKeys;
		for (int i = 0; i < colorKeys.Length; i++)
		{
			GradientColorKey gradientColorKey = colorKeys[i];
			list.Add(new GradientStop(gradientColorKey.time, gradientColorKey.color));
		}
		return new ColorGradient(list.ToArray());
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(GetType().FullName);
		writer.PropertyName("stops");
		int num = ((stops != null) ? stops.Length : 0);
		writer.ArrayBegin(num);
		for (int i = 0; i < num; i++)
		{
			writer.Write(stops[i]);
		}
		writer.ArrayEnd();
		writer.TypeEnd();
	}
}
