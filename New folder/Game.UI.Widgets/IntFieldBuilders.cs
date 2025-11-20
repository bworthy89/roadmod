using System;
using Game.Reflection;
using Unity.Mathematics;
using UnityEngine;

namespace Game.UI.Widgets;

public class IntFieldBuilders : IFieldBuilderFactory
{
	private static readonly int kGlobalValueRange = 10000000;

	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		if (memberType == typeof(byte))
		{
			return CreateIntFieldBuilder(attributes, 0, 255, ToByte, FromByte);
		}
		if (memberType == typeof(sbyte))
		{
			return CreateIntFieldBuilder(attributes, -128, 127, ToSByte, FromSByte);
		}
		if (memberType == typeof(short))
		{
			return CreateIntFieldBuilder(attributes, -32768, 32767, ToShort, FromShort);
		}
		if (memberType == typeof(ushort))
		{
			return CreateIntFieldBuilder(attributes, 0, 65535, ToUShort, FromUShort);
		}
		if (memberType == typeof(int))
		{
			return CreateIntFieldBuilder(attributes, int.MinValue, int.MaxValue, ToInt, FromInt);
		}
		if (memberType == typeof(int2))
		{
			return CreateIntFieldBuilder<Int2InputField, int2>(attributes, ToInt2, FromInt2);
		}
		if (memberType == typeof(Vector2Int))
		{
			return CreateIntFieldBuilder<Int2InputField, int2>(attributes, ToVector2Int, FromVector2Int);
		}
		if (memberType == typeof(int3))
		{
			return CreateIntFieldBuilder<Int3InputField, int3>(attributes, ToInt3, FromInt3);
		}
		if (memberType == typeof(Vector3Int))
		{
			return CreateIntFieldBuilder<Int3InputField, int3>(attributes, ToVector3Int, FromVector3Int);
		}
		if (memberType == typeof(int4))
		{
			return CreateIntFieldBuilder<Int4InputField, int4>(attributes, ToInt4, FromInt4);
		}
		return null;
		static object FromByte(int value)
		{
			return (byte)value;
		}
		static object FromInt(int value)
		{
			return value;
		}
		static object FromInt2(int2 value)
		{
			return value;
		}
		static object FromInt3(int3 value)
		{
			return value;
		}
		static object FromInt4(int4 value)
		{
			return value;
		}
		static object FromSByte(int value)
		{
			return (sbyte)value;
		}
		static object FromShort(int value)
		{
			return (short)value;
		}
		static object FromUShort(int value)
		{
			return (ushort)value;
		}
		static object FromVector2Int(int2 value)
		{
			return new Vector2Int(value.x, value.y);
		}
		static object FromVector3Int(int3 value)
		{
			return new Vector3Int(value.x, value.y, value.z);
		}
		static int ToByte(object value)
		{
			return (byte)value;
		}
		static int ToInt(object value)
		{
			return (int)value;
		}
		static int2 ToInt2(object value)
		{
			return (int2)value;
		}
		static int3 ToInt3(object value)
		{
			return (int3)value;
		}
		static int4 ToInt4(object value)
		{
			return (int4)value;
		}
		static int ToSByte(object value)
		{
			return (sbyte)value;
		}
		static int ToShort(object value)
		{
			return (short)value;
		}
		static int ToUShort(object value)
		{
			return (ushort)value;
		}
		static int2 ToVector2Int(object value)
		{
			Vector2Int vector2Int = (Vector2Int)value;
			return new int2(vector2Int.x, vector2Int.y);
		}
		static int3 ToVector3Int(object value)
		{
			Vector3Int vector3Int = (Vector3Int)value;
			return new int3(vector3Int.x, vector3Int.y, vector3Int.z);
		}
	}

	private static FieldBuilder CreateIntFieldBuilder(object[] attributes, int min, int max, Converter<object, int> fromObject, Converter<int, object> toObject)
	{
		if (!EditorGenerator.sBypassValueLimits)
		{
			min = math.max(min, -kGlobalValueRange);
			max = math.min(max, kGlobalValueRange);
		}
		int step = WidgetAttributeUtils.GetNumberStep(attributes, 1);
		if (!EditorGenerator.sBypassValueLimits && WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max) && !WidgetAttributeUtils.RequiresInputField(attributes))
		{
			string unit = WidgetAttributeUtils.GetNumberUnit(attributes);
			return (IValueAccessor accessor) => new IntSliderField
			{
				min = min,
				max = max,
				step = step,
				unit = unit,
				accessor = new CastAccessor<int>(accessor, fromObject, toObject)
			};
		}
		return (IValueAccessor accessor) => new IntInputField
		{
			min = min,
			max = max,
			step = step,
			accessor = new CastAccessor<int>(accessor, fromObject, toObject)
		};
	}

	private static FieldBuilder CreateIntFieldBuilder<TWidget, TValue>(object[] attributes, Converter<object, TValue> fromObject, Converter<TValue, object> toObject) where TWidget : IntField<TValue>, new()
	{
		int step = WidgetAttributeUtils.GetNumberStep(attributes, 1);
		int min = (EditorGenerator.sBypassValueLimits ? int.MinValue : (-kGlobalValueRange));
		int max = (EditorGenerator.sBypassValueLimits ? int.MaxValue : kGlobalValueRange);
		if (!EditorGenerator.sBypassValueLimits)
		{
			WidgetAttributeUtils.GetNumberRange(attributes, ref min, ref max);
		}
		return (IValueAccessor accessor) => new TWidget
		{
			min = min,
			max = max,
			step = step,
			accessor = ((fromObject != null && toObject != null) ? new CastAccessor<TValue>(accessor, fromObject, toObject) : new CastAccessor<TValue>(accessor))
		};
	}
}
