using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Routes;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public abstract class VehicleWithLineSection : VehicleSection
{
	protected Entity lineEntity { get; set; }

	protected override void Reset()
	{
		base.Reset();
		lineEntity = Entity.Null;
	}

	protected override void OnProcess()
	{
		lineEntity = (base.EntityManager.TryGetComponent<CurrentRoute>(selectedEntity, out var component) ? component.m_Route : Entity.Null);
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("line");
		if (lineEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, lineEntity);
		}
		writer.PropertyName("lineEntity");
		writer.Write(lineEntity);
	}

	[Preserve]
	protected VehicleWithLineSection()
	{
	}
}
