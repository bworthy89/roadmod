using System;
using Game.Reflection;
using UnityEngine;

namespace Game.UI.Widgets;

public class ColorFieldBuilders : IFieldBuilderFactory
{
	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(Color))
		{
			return CreateColorFieldBuilder(attributes, ToColor, FromColor);
		}
		if (memberType == typeof(Color32))
		{
			return CreateColorFieldBuilder(attributes, ToColor32, FromColor32);
		}
		return null;
		static object FromColor(Color value)
		{
			return value;
		}
		static object FromColor32(Color value)
		{
			return (Color32)value;
		}
		static Color ToColor(object value)
		{
			return (Color)value;
		}
		static Color ToColor32(object value)
		{
			return (Color32)value;
		}
	}

	private static FieldBuilder CreateColorFieldBuilder(object[] attributes, Converter<object, Color> fromObject, Converter<Color, object> toObject)
	{
		bool hdr = false;
		bool showAlpha = false;
		WidgetAttributeUtils.GetColorUsage(attributes, ref hdr, ref showAlpha);
		return (IValueAccessor accessor) => new ColorField
		{
			hdr = hdr,
			showAlpha = showAlpha,
			accessor = new CastAccessor<Color>(accessor, fromObject, toObject)
		};
	}
}
