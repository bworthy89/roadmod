using UnityEngine;

namespace Game.UI.Widgets;

public class EditorNameAttribute : PropertyAttribute
{
	public string displayName { get; private set; }

	public EditorNameAttribute(string displayName)
	{
		this.displayName = displayName;
	}
}
