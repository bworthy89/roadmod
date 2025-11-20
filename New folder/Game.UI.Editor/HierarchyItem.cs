using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Editor;

public struct HierarchyItem<T> : IJsonWritable
{
	public T m_Data;

	public LocalizedString m_DisplayName;

	public LocalizedString m_Tooltip;

	public int m_Level;

	public string m_Icon;

	public bool m_Selectable;

	public bool m_Selected;

	public bool m_Expandable;

	public bool m_Expanded;

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin("HierarchyItem");
		writer.PropertyName("displayName");
		writer.Write(m_DisplayName);
		writer.PropertyName("tooltip");
		if (m_Tooltip.isEmpty)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(m_Tooltip);
		}
		writer.PropertyName("level");
		writer.Write(m_Level);
		writer.PropertyName("icon");
		writer.Write(m_Icon);
		writer.PropertyName("expandable");
		writer.Write(m_Expandable);
		writer.PropertyName("expanded");
		writer.Write(m_Expanded);
		writer.PropertyName("selectable");
		writer.Write(m_Selectable);
		writer.PropertyName("selected");
		writer.Write(m_Selected);
		writer.TypeEnd();
	}
}
