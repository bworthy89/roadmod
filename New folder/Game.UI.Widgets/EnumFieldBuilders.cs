using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Reflection;
using Game.UI.Editor;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class EnumFieldBuilders : IFieldBuilderFactory
{
	public static readonly Dictionary<Type, EnumMember[]> kMemberCache = new Dictionary<Type, EnumMember[]>();

	public FieldBuilder TryCreate(Type memberType, object[] attributes)
	{
		return Create(memberType, attributes);
	}

	public static FieldBuilder Create(Type memberType, object[] attributes)
	{
		if (memberType.IsEnum && GetConverters(memberType, out var fromObject, out var toObject))
		{
			if (!kMemberCache.TryGetValue(memberType, out var enumMembers))
			{
				enumMembers = BuildMembers(memberType, fromObject);
				kMemberCache[memberType] = enumMembers;
			}
			if (memberType.GetCustomAttribute(typeof(FlagsAttribute)) != null)
			{
				return (IValueAccessor accessor) => new FlagsField
				{
					enumMembers = enumMembers,
					accessor = new CastAccessor<ulong>(accessor, fromObject, toObject)
				};
			}
			return (IValueAccessor accessor) => new EnumField
			{
				enumMembers = enumMembers,
				accessor = new CastAccessor<ulong>(accessor, fromObject, toObject)
			};
		}
		return null;
	}

	private static EnumMember[] BuildMembers(Type memberType, Converter<object, ulong> fromObject)
	{
		bool flag = memberType.GetCustomAttribute(typeof(FlagsAttribute)) != null;
		FieldInfo[] fields = memberType.GetFields(BindingFlags.Static | BindingFlags.Public);
		List<EnumMember> list = new List<EnumMember>(fields.Length);
		foreach (FieldInfo fieldInfo in fields)
		{
			if (fieldInfo.GetCustomAttribute<HideInEditorAttribute>() == null)
			{
				object value = fieldInfo.GetValue(null);
				ulong num = fromObject(value);
				if (!flag || num != 0L)
				{
					list.Add(new EnumMember(fromObject(value), LocalizedString.Value(fieldInfo.Name)));
				}
			}
		}
		return list.ToArray();
	}

	public static bool GetConverters(Type memberType, out Converter<object, ulong> fromObject, out Converter<ulong, object> toObject)
	{
		Type underlyingType = Enum.GetUnderlyingType(memberType);
		if (underlyingType == typeof(sbyte))
		{
			fromObject = (object value) => (ulong)(sbyte)value;
			toObject = (ulong value) => (sbyte)value;
		}
		else if (underlyingType == typeof(byte))
		{
			fromObject = (object value) => (byte)value;
			toObject = (ulong value) => (byte)value;
		}
		else if (underlyingType == typeof(short))
		{
			fromObject = (object value) => (ulong)(short)value;
			toObject = (ulong value) => (short)value;
		}
		else if (underlyingType == typeof(ushort))
		{
			fromObject = (object value) => (ushort)value;
			toObject = (ulong value) => (ushort)value;
		}
		else if (underlyingType == typeof(int))
		{
			fromObject = (object value) => (ulong)(int)value;
			toObject = (ulong value) => (int)value;
		}
		else if (underlyingType == typeof(uint))
		{
			fromObject = (object value) => (uint)value;
			toObject = (ulong value) => (uint)value;
		}
		else if (underlyingType == typeof(long))
		{
			fromObject = (object value) => (ulong)(long)value;
			toObject = (ulong value) => (long)value;
		}
		else
		{
			if (!(underlyingType == typeof(ulong)))
			{
				fromObject = null;
				toObject = null;
				return false;
			}
			fromObject = (object value) => (ulong)value;
			toObject = (ulong value) => value;
		}
		return true;
	}
}
