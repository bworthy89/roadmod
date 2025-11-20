using UnityEngine;

namespace Game.UI.Widgets;

public class ListElementLabelAttribute : PropertyAttribute
{
	public string format { get; private set; }

	public bool localized { get; private set; }

	public ListElementLabelAttribute(bool localized = false)
	{
		format = null;
		this.localized = localized;
	}

	public ListElementLabelAttribute(string format, bool localized = false)
	{
		this.format = format;
		this.localized = localized;
	}
}
