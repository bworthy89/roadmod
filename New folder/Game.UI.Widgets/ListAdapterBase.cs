using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Colossal.OdinSerializer.Utilities;
using Game.Prefabs;
using Game.Reflection;
using Game.UI.Editor;
using Game.UI.Localization;
using UnityEngine;

namespace Game.UI.Widgets;

public abstract class ListAdapterBase<T> : IListAdapter where T : class, IList
{
	protected int m_StartIndex;

	protected int m_EndIndex;

	public ITypedValueAccessor<T> accessor { get; set; }

	public Type elementType { get; set; }

	public IEditorGenerator generator { get; set; }

	public int level { get; set; }

	public string path { get; set; }

	public object[] attributes { get; set; } = Array.Empty<object>();

	public MemberInfo labelMember { get; set; }

	public int length => accessor.GetTypedValue()?.Count ?? 0;

	public bool resizable { get; set; } = true;

	public bool sortable => true;

	public bool UpdateRange(int startIndex, int endIndex)
	{
		if (startIndex != m_StartIndex || endIndex != m_EndIndex)
		{
			m_StartIndex = startIndex;
			m_EndIndex = endIndex;
			return true;
		}
		return false;
	}

	public IEnumerable<IWidget> BuildElementsInRange()
	{
		int rangeLength = m_EndIndex - m_StartIndex;
		for (int i = 0; i < rangeLength; i++)
		{
			int index = m_StartIndex + i;
			ListElementAccessor<T> elementAccessor = new ListElementAccessor<T>(accessor, elementType, index);
			IWidget widget = generator.Build(elementAccessor, attributes ?? Array.Empty<object>(), level + 1, $"{path}[{index}]");
			if (widget is NamedWidget namedWidget && labelMember != null)
			{
				namedWidget.displayNameAction = () => GetElementLabel(elementAccessor.GetValue(), index);
			}
			else if (widget is INamed named)
			{
				named.displayName = GetElementLabel(elementAccessor.GetValue(), index);
			}
			yield return widget;
		}
	}

	private LocalizedString GetElementLabel(object obj, int index)
	{
		string text = null;
		bool flag = false;
		if (labelMember != null && obj != null)
		{
			ListElementLabelAttribute customAttribute = labelMember.GetCustomAttribute<ListElementLabelAttribute>();
			string format = customAttribute.format;
			string text2 = labelMember.GetMemberValue(obj)?.ToString();
			text = ((format != null) ? string.Format(format, text2, index) : text2);
			flag = customAttribute.localized;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = $"Element {index}";
		}
		if (flag)
		{
			return LocalizedString.Id(text);
		}
		return LocalizedString.Value(text);
	}

	public int AddElement()
	{
		int num = length;
		InsertElement(num);
		if (num > 0)
		{
			CopyData(num - 1, num);
		}
		return num;
	}

	public virtual int DuplicateElement(int index)
	{
		int num = index + 1;
		InsertElement(num);
		CopyData(index, num);
		return num;
	}

	protected virtual void CopyData(int fromIndex, int toIndex)
	{
		T typedValue = accessor.GetTypedValue();
		if (typedValue != null)
		{
			if (elementType.IsPrimitive || elementType == typeof(string) || typeof(PrefabBase).IsAssignableFrom(elementType))
			{
				typedValue[toIndex] = typedValue[fromIndex];
				return;
			}
			string json = JsonUtility.ToJson(typedValue[fromIndex]);
			object obj = typedValue[toIndex];
			JsonUtility.FromJsonOverwrite(json, obj);
			typedValue[toIndex] = obj;
		}
	}

	public virtual void MoveElement(int fromIndex, int toIndex)
	{
		T typedValue = accessor.GetTypedValue();
		object value = typedValue[fromIndex];
		if (toIndex < fromIndex)
		{
			for (int i = toIndex; i < fromIndex; i++)
			{
				typedValue[i + 1] = typedValue[i];
			}
		}
		else if (toIndex > fromIndex)
		{
			for (int j = fromIndex; j < toIndex; j++)
			{
				typedValue[j] = typedValue[j + 1];
			}
		}
		typedValue[toIndex] = value;
	}

	public abstract void InsertElement(int index);

	public abstract void DeleteElement(int index);

	public abstract void Clear();

	public static object CreateInstance(Type type)
	{
		if (type == typeof(string))
		{
			return string.Empty;
		}
		if (typeof(PrefabBase).IsAssignableFrom(type))
		{
			return null;
		}
		if (type.IsEnum)
		{
			foreach (object value in Enum.GetValues(type))
			{
				string name = Enum.GetName(type, value);
				if (type.GetMember(name)[0].GetCustomAttribute<HideInEditorAttribute>() == null)
				{
					return value;
				}
			}
		}
		return Activator.CreateInstance(type);
	}
}
