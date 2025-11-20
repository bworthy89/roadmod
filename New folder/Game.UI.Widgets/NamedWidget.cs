using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public abstract class NamedWidget : Widget, INamed
{
	private LocalizedString m_displayName;

	private LocalizedString m_description;

	[CanBeNull]
	public Func<LocalizedString> displayNameAction { get; set; }

	[CanBeNull]
	public Func<LocalizedString> descriptionAction { get; set; }

	public LocalizedString displayName
	{
		get
		{
			return m_displayName;
		}
		set
		{
			displayNameAction = null;
			m_displayName = value;
		}
	}

	public LocalizedString description
	{
		get
		{
			return m_description;
		}
		set
		{
			descriptionAction = null;
			m_description = value;
		}
	}

	protected override WidgetChanges Update()
	{
		return base.Update() | UpdateNameAndDescription(setChanged: false);
	}

	public WidgetChanges UpdateNameAndDescription(bool setChanged = true)
	{
		WidgetChanges widgetChanges = WidgetChanges.None;
		if (displayNameAction != null)
		{
			LocalizedString localizedString = displayNameAction();
			if (!localizedString.Equals(m_displayName))
			{
				m_displayName = localizedString;
				widgetChanges |= WidgetChanges.Properties;
				if (setChanged)
				{
					SetPropertiesChanged();
				}
			}
		}
		if (descriptionAction != null)
		{
			LocalizedString localizedString2 = descriptionAction();
			if (!localizedString2.Equals(m_description))
			{
				m_description = localizedString2;
				widgetChanges |= WidgetChanges.Properties;
				if (setChanged)
				{
					SetPropertiesChanged();
				}
			}
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.PropertyName("description");
		writer.Write(description);
	}
}
