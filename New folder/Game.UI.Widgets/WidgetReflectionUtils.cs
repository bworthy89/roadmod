using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Colossal.Annotations;
using Colossal.OdinSerializer;
using Game.Reflection;

namespace Game.UI.Widgets;

public static class WidgetReflectionUtils
{
	private static CustomSerializationPolicy kListElementLabelPolicy = new CustomSerializationPolicy("ListElementLabel", allowNonSerializableTypes: true, (MemberInfo info) => info.GetCustomAttribute<ListElementLabelAttribute>() != null);

	public static bool IsListType(Type memberType)
	{
		if (!memberType.IsArray)
		{
			if (typeof(IList).IsAssignableFrom(memberType))
			{
				return memberType.GetInterfaces().Any(IsGenericListInterface);
			}
			return false;
		}
		return true;
	}

	[CanBeNull]
	public static Type GetListElementType(Type memberType)
	{
		if (memberType.IsArray)
		{
			return memberType.GetElementType();
		}
		Type type = memberType.GetInterfaces().FirstOrDefault(IsGenericListInterface);
		if (type != null)
		{
			return type.GenericTypeArguments[0];
		}
		return null;
	}

	private static bool IsGenericListInterface(Type type)
	{
		if (type.IsGenericType)
		{
			return type.GetGenericTypeDefinition() == typeof(IList<>);
		}
		return false;
	}

	public static FieldBuilder CreateFieldBuilder<T, U>() where T : Field<U>, new()
	{
		return (IValueAccessor accessor) => new T
		{
			accessor = new CastAccessor<U>(accessor)
		};
	}

	public static FieldBuilder CreateFieldBuilder<T, U>(Converter<object, U> fromObject, Converter<U, object> toObject) where T : Field<U>, new()
	{
		return (IValueAccessor accessor) => new T
		{
			accessor = new CastAccessor<U>(accessor, fromObject, toObject)
		};
	}

	public static string NicifyVariableName(string name)
	{
		if (name == null)
		{
			return string.Empty;
		}
		if (name.StartsWith("m_"))
		{
			name = name.Substring(2);
		}
		else if (name.StartsWith("Get"))
		{
			name = name.Substring(3);
		}
		name = Regex.Replace(name, "\\B([A-Z][a-z])", " $1");
		name = Regex.Replace(name, "([^A-Z\\s])([A-Z])", "$1 $2");
		name = Regex.Replace(name, "(?<![\\d\\s]|\\dx|\\d-)(\\d)", " $1");
		name = Regex.Replace(name, "^([a-z])", (Match match) => match.Value.ToUpperInvariant());
		return name;
	}

	public static MemberInfo GetListElementLabelMember(Type type)
	{
		return FormatterUtilities.GetSerializableMembers(type, kListElementLabelPolicy).FirstOrDefault();
	}
}
