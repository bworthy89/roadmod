using System;
using Colossal.UI.Binding;

namespace Game.UI.InGame;

public abstract class TabbedGamePanel : GamePanel, IEquatable<TabbedGamePanel>
{
	public virtual int selectedTab { get; set; }

	protected override void BindProperties(IJsonWriter writer)
	{
		base.BindProperties(writer);
		writer.PropertyName("selectedTab");
		writer.Write(selectedTab);
	}

	public bool Equals(TabbedGamePanel other)
	{
		if (other == null)
		{
			return false;
		}
		if (this != other)
		{
			return selectedTab.Equals(other.selectedTab);
		}
		return true;
	}
}
