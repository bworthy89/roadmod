using System.Runtime.CompilerServices;
using Colossal.UI.Binding;
using Game.Common;
using Game.Vehicles;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class DeathcareVehicleSection : VehicleSection
{
	protected override string group => "DeathcareVehicleSection";

	private Entity deadEntity { get; set; }

	protected override void Reset()
	{
		base.Reset();
		deadEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (base.EntityManager.HasComponent<Vehicle>(selectedEntity) && base.EntityManager.HasComponent<Hearse>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Owner>(selectedEntity);
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
		Hearse componentData = base.EntityManager.GetComponentData<Hearse>(selectedEntity);
		base.stateKey = VehicleUIUtils.GetStateKey(selectedEntity, componentData, base.EntityManager);
		deadEntity = ((base.stateKey == VehicleStateLocaleKey.Conveying) ? componentData.m_TargetCorpse : Entity.Null);
		base.OnProcess();
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		base.OnWriteProperties(writer);
		writer.PropertyName("dead");
		if (deadEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, deadEntity);
		}
		writer.PropertyName("deadEntity");
		if (deadEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(deadEntity);
		}
	}

	[Preserve]
	public DeathcareVehicleSection()
	{
	}
}
