using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Common;
using Game.Creatures;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class DummyHumanSection : InfoSectionBase
{
	protected override string group => "DummyHumanSection";

	private Entity originEntity { get; set; }

	private Entity destinationEntity { get; set; }

	protected override void Reset()
	{
		originEntity = Entity.Null;
		destinationEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (base.EntityManager.TryGetComponent<Resident>(selectedEntity, out var component))
		{
			return component.m_Citizen == Entity.Null;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		if (base.EntityManager.TryGetComponent<CurrentVehicle>(selectedEntity, out var component))
		{
			originEntity = base.EntityManager.GetComponentData<Owner>(component.m_Vehicle).m_Owner;
			destinationEntity = VehicleUIUtils.GetDestination(base.EntityManager, component.m_Vehicle);
		}
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("origin");
		if (originEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, originEntity);
		}
		writer.PropertyName("originEntity");
		if (originEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(originEntity);
		}
		writer.PropertyName("destination");
		if (destinationEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, destinationEntity);
		}
		writer.PropertyName("destinationEntity");
		if (destinationEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(destinationEntity);
		}
	}

	[Preserve]
	public DummyHumanSection()
	{
	}
}
