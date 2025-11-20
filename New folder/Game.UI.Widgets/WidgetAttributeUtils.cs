using System;
using System.Linq;
using Colossal.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.Widgets;

public static class WidgetAttributeUtils
{
	public static bool RequiresInputField(object[] attributes)
	{
		return attributes.OfType<InputFieldAttribute>().Any();
	}

	public static bool IsTimeField(object[] attributes)
	{
		return attributes.OfType<TimeFieldAttribute>().Any();
	}

	public static bool AllowsMinGreaterMax(object[] attributes)
	{
		return attributes.OfType<AllowMinGreaterMaxAttribute>().Any();
	}

	public static void GetColorUsage(object[] attributes, ref bool hdr, ref bool showAlpha)
	{
		ColorUsageAttribute colorUsageAttribute = attributes.OfType<ColorUsageAttribute>().FirstOrDefault();
		if (colorUsageAttribute != null)
		{
			hdr = colorUsageAttribute.hdr;
			showAlpha = colorUsageAttribute.showAlpha;
		}
	}

	public static bool GetNumberRange(object[] attributes, ref int min, ref int max)
	{
		RangeAttribute rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
		if (rangeAttribute != null)
		{
			min = (int)rangeAttribute.min;
			max = (int)rangeAttribute.max;
			return true;
		}
		RangeNAttribute rangeNAttribute = attributes.OfType<RangeNAttribute>().FirstOrDefault();
		if (rangeNAttribute != null)
		{
			min = (int)rangeNAttribute.min.x;
			max = (int)rangeNAttribute.max.x;
			return true;
		}
		return false;
	}

	public static bool GetNumberRange(object[] attributes, ref uint min, ref uint max)
	{
		RangeAttribute rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
		if (rangeAttribute != null)
		{
			min = (uint)rangeAttribute.min;
			max = (uint)rangeAttribute.max;
			return true;
		}
		RangeNAttribute rangeNAttribute = attributes.OfType<RangeNAttribute>().FirstOrDefault();
		if (rangeNAttribute != null)
		{
			min = (uint)rangeNAttribute.min.x;
			max = (uint)rangeNAttribute.max.x;
			return true;
		}
		return false;
	}

	public static bool GetNumberRange(object[] attributes, ref float min, ref float max)
	{
		RangeAttribute rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
		if (rangeAttribute != null)
		{
			min = rangeAttribute.min;
			max = rangeAttribute.max;
			return true;
		}
		RangeNAttribute rangeNAttribute = attributes.OfType<RangeNAttribute>().FirstOrDefault();
		if (rangeNAttribute != null)
		{
			min = rangeNAttribute.min.x;
			max = rangeNAttribute.max.x;
			return true;
		}
		return false;
	}

	public static bool GetNumberRange(object[] attributes, ref float4 min, ref float4 max)
	{
		RangeAttribute rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
		if (rangeAttribute != null)
		{
			min = rangeAttribute.min;
			max = rangeAttribute.max;
			return true;
		}
		RangeNAttribute rangeNAttribute = attributes.OfType<RangeNAttribute>().FirstOrDefault();
		if (rangeNAttribute != null)
		{
			min = rangeNAttribute.min;
			max = rangeNAttribute.max;
			return true;
		}
		return false;
	}

	public static bool GetNumberRange(object[] attributes, ref double min, ref double max)
	{
		RangeAttribute rangeAttribute = attributes.OfType<RangeAttribute>().FirstOrDefault();
		if (rangeAttribute != null)
		{
			min = rangeAttribute.min;
			max = rangeAttribute.max;
			return true;
		}
		RangeNAttribute rangeNAttribute = attributes.OfType<RangeNAttribute>().FirstOrDefault();
		if (rangeNAttribute != null)
		{
			min = rangeNAttribute.min.x;
			max = rangeNAttribute.max.x;
			return true;
		}
		return false;
	}

	public static int GetNumberStep(object[] attributes, int defaultStep = 1)
	{
		NumberStepAttribute numberStepAttribute = attributes.OfType<NumberStepAttribute>().FirstOrDefault();
		if (numberStepAttribute != null)
		{
			int num = (int)numberStepAttribute.Step;
			if (num > 0)
			{
				return num;
			}
		}
		return defaultStep;
	}

	public static uint GetNumberStep(object[] attributes, uint defaultStep = 1u)
	{
		NumberStepAttribute numberStepAttribute = attributes.OfType<NumberStepAttribute>().FirstOrDefault();
		if (numberStepAttribute != null)
		{
			uint num = (uint)numberStepAttribute.Step;
			if (num != 0)
			{
				return num;
			}
		}
		return defaultStep;
	}

	public static float GetNumberStep(object[] attributes, float defaultStep = 0.01f)
	{
		NumberStepAttribute numberStepAttribute = attributes.OfType<NumberStepAttribute>().FirstOrDefault();
		if (numberStepAttribute == null || !(numberStepAttribute.Step > 0f))
		{
			return defaultStep;
		}
		return numberStepAttribute.Step;
	}

	public static double GetNumberStep(object[] attributes, double defaultStep = 0.01)
	{
		NumberStepAttribute numberStepAttribute = attributes.OfType<NumberStepAttribute>().FirstOrDefault();
		if (numberStepAttribute == null || !(numberStepAttribute.Step > 0f))
		{
			return defaultStep;
		}
		return numberStepAttribute.Step;
	}

	[CanBeNull]
	public static string GetNumberUnit(object[] attributes)
	{
		return attributes.OfType<NumberUnitAttribute>().FirstOrDefault()?.Unit;
	}

	public static Type GetCustomFieldFactory(object[] attributes, bool isContainer = false)
	{
		if (!isContainer)
		{
			ElementCustomFieldAttribute elementCustomFieldAttribute = attributes.OfType<ElementCustomFieldAttribute>().FirstOrDefault();
			if (elementCustomFieldAttribute != null)
			{
				return elementCustomFieldAttribute.Factory;
			}
		}
		return attributes.OfType<CustomFieldAttribute>().FirstOrDefault()?.Factory;
	}
}
