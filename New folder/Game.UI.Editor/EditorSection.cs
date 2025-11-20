using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.Reflection;
using Game.UI.Widgets;
using UnityEngine;

namespace Game.UI.Editor;

public class EditorSection : ExpandableGroup, IDisableCallback
{
	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget>(group, "deleteEditorSection", delegate(IWidget widget)
			{
				if (widget is EditorSection editorSection)
				{
					editorSection.onDelete?.Invoke();
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, bool>(group, "setEditorSectionActive", delegate(IWidget widget, bool active)
			{
				if (widget is EditorSection editorSection)
				{
					editorSection.active?.SetTypedValue(active);
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	public static readonly Color kPrefabColor = new Color(27f / 85f, 0.2509804f, 0.20784314f);

	private bool m_Active;

	private Color? m_Color;

	[CanBeNull]
	public Action onDelete { get; set; }

	[CanBeNull]
	public ITypedValueAccessor<bool> active { get; set; }

	public bool primary { get; set; }

	[CanBeNull]
	public Color? color
	{
		get
		{
			return m_Color;
		}
		set
		{
			if (!(value == m_Color))
			{
				m_Color = value;
				SetPropertiesChanged();
			}
		}
	}

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		bool flag = active?.GetTypedValue() ?? true;
		if (flag != m_Active)
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Active = flag;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("expandable");
		writer.Write(base.children.Count != 0);
		writer.PropertyName("deletable");
		writer.Write(onDelete != null);
		writer.PropertyName("activatable");
		writer.Write(active != null);
		writer.PropertyName("active");
		writer.Write(m_Active);
		writer.PropertyName("primary");
		writer.Write(primary);
		writer.PropertyName("color");
		if (color.HasValue)
		{
			writer.Write(color.Value);
		}
		else
		{
			writer.WriteNull();
		}
	}
}
