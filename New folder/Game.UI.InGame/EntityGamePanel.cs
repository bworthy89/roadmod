using System;
using Colossal.UI.Binding;
using Unity.Entities;

namespace Game.UI.InGame;

public abstract class EntityGamePanel : GamePanel, IEquatable<EntityGamePanel>
{
	public virtual Entity selectedEntity { get; set; } = Entity.Null;

	protected override void BindProperties(IJsonWriter writer)
	{
		base.BindProperties(writer);
		writer.PropertyName("selectedEntity");
		writer.Write(selectedEntity);
	}

	public bool Equals(EntityGamePanel other)
	{
		if (other == null)
		{
			return false;
		}
		if (this != other)
		{
			return selectedEntity.Equals(other.selectedEntity);
		}
		return true;
	}
}
