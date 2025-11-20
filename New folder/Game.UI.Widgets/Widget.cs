using System;
using System.Collections.Generic;
using System.Diagnostics;
using Colossal.Annotations;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

[DebuggerDisplay("{m_Path}, hidden={m_Hidden}, disabled={m_Disabled}")]
public abstract class Widget : IWidget, IJsonWritable, IVisibleWidget, IDisableCallback
{
	protected const string kProperties = "props";

	protected const string kChildren = "children";

	protected const string kTutorialTag = "tutorialTag";

	private WidgetChanges m_Changes;

	private PathSegment m_Path = PathSegment.Empty;

	private string m_TutorialTag;

	protected bool m_Disabled;

	protected bool m_Hidden;

	private bool m_IsInitialUpdate = true;

	public PathSegment path
	{
		get
		{
			return m_Path;
		}
		set
		{
			if (value != m_Path)
			{
				m_Path = value;
				m_Changes |= WidgetChanges.Path;
			}
		}
	}

	public string tutorialTag
	{
		get
		{
			return m_TutorialTag;
		}
		set
		{
			if (value != m_TutorialTag)
			{
				m_TutorialTag = value;
				SetPropertiesChanged();
			}
		}
	}

	public virtual IList<IWidget> visibleChildren => Array.Empty<IWidget>();

	[CanBeNull]
	public Func<bool> disabled { get; set; }

	[CanBeNull]
	public Func<bool> hidden { get; set; }

	public virtual bool isVisible => !m_Hidden;

	public virtual bool isActive => !m_Disabled;

	public virtual string propertiesTypeName => GetType().FullName;

	public void SetPropertiesChanged()
	{
		m_Changes |= WidgetChanges.Properties;
	}

	public void SetChildrenChanged()
	{
		m_Changes |= WidgetChanges.Children;
	}

	WidgetChanges IWidget.Update()
	{
		return UpdateBase();
	}

	private WidgetChanges UpdateBase()
	{
		UpdateVisibility();
		WidgetChanges widgetChanges = m_Changes;
		m_Changes = WidgetChanges.None;
		if (m_Hidden && !m_IsInitialUpdate)
		{
			return widgetChanges;
		}
		m_IsInitialUpdate = false;
		bool flag = disabled != null && disabled();
		if (flag != m_Disabled)
		{
			widgetChanges |= WidgetChanges.Activity;
			m_Disabled = flag;
		}
		return widgetChanges | Update();
	}

	protected virtual WidgetChanges Update()
	{
		return WidgetChanges.None;
	}

	public virtual WidgetChanges UpdateVisibility()
	{
		bool flag = hidden != null && hidden();
		if (flag != m_Hidden)
		{
			m_Hidden = flag;
			m_Changes |= WidgetChanges.Visibility;
			return WidgetChanges.Visibility;
		}
		return WidgetChanges.None;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(Widget).FullName);
		writer.PropertyName("path");
		writer.Write(path);
		writer.PropertyName("props");
		writer.TypeBegin(propertiesTypeName);
		WriteBaseProperties(writer);
		writer.TypeEnd();
		writer.PropertyName("children");
		writer.Write(visibleChildren);
		writer.TypeEnd();
	}

	void IWidget.WriteProperties(IJsonWriter writer)
	{
		WriteBaseProperties(writer);
	}

	private void WriteBaseProperties(IJsonWriter writer)
	{
		writer.PropertyName("disabled");
		writer.Write(m_Disabled);
		writer.PropertyName("hidden");
		writer.Write(m_Hidden);
		WriteProperties(writer);
	}

	protected virtual void WriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("tutorialTag");
		writer.Write(tutorialTag);
	}

	public static void PatchWidget(RawValueBinding binding, IList<int> path, IWidget widget, WidgetChanges changes)
	{
		IJsonWriter jsonWriter = binding.PatchBegin();
		if (changes == WidgetChanges.Path)
		{
			WritePatchPath(jsonWriter, path, "path");
			jsonWriter.Write(widget.path);
		}
		else if ((changes & WidgetChanges.TotalProperties) != WidgetChanges.None && (changes & ~WidgetChanges.TotalProperties) == 0)
		{
			WritePatchPath(jsonWriter, path, "props");
			jsonWriter.TypeBegin(widget.propertiesTypeName);
			widget.WriteProperties(jsonWriter);
			jsonWriter.TypeEnd();
		}
		else if (changes == WidgetChanges.Children)
		{
			WritePatchPath(jsonWriter, path, "children");
			jsonWriter.Write(widget.visibleChildren);
		}
		else
		{
			WritePatchPath(jsonWriter, path);
			jsonWriter.Write(widget);
		}
		binding.PatchEnd();
	}

	public static void WritePatchPath(IJsonWriter writer, IList<int> path)
	{
		writer.ArrayBegin(2 * path.Count - 1);
		for (int i = 0; i < path.Count - 1; i++)
		{
			writer.Write(path[i]);
			writer.Write("children");
		}
		writer.Write(path[path.Count - 1]);
		writer.ArrayEnd();
	}

	public static void WritePatchPath(IJsonWriter writer, IList<int> path, string propertyName)
	{
		writer.ArrayBegin(2 * path.Count);
		for (int i = 0; i < path.Count - 1; i++)
		{
			writer.Write(path[i]);
			writer.Write("children");
		}
		writer.Write(path[path.Count - 1]);
		writer.Write(propertyName);
		writer.ArrayEnd();
	}

	public static void WritePatchPath(IJsonWriter writer, IList<int> path, string propertyName1, string propertyName2, string propertyName3, int propertyName4)
	{
		writer.ArrayBegin(2 * path.Count + 3);
		for (int i = 0; i < path.Count - 1; i++)
		{
			writer.Write(path[i]);
			writer.Write("children");
		}
		writer.Write(path[path.Count - 1]);
		writer.Write(propertyName1);
		writer.Write(propertyName2);
		writer.Write(propertyName3);
		writer.Write(propertyName4);
		writer.ArrayEnd();
	}
}
