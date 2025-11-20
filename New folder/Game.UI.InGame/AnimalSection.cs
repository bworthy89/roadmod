using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class AnimalSection : InfoSectionBase
{
	private enum TypeKey
	{
		Pet,
		Livestock,
		Wildlife
	}

	protected override string group => "AnimalSection";

	private TypeKey typeKey { get; set; }

	private Entity ownerEntity { get; set; }

	private Entity destinationEntity { get; set; }

	private string GetTypeKeyString(TypeKey typeKey)
	{
		return typeKey switch
		{
			TypeKey.Pet => "Pet", 
			TypeKey.Livestock => "Livestock", 
			_ => "Wildlife", 
		};
	}

	protected override void Reset()
	{
		ownerEntity = Entity.Null;
		destinationEntity = Entity.Null;
	}

	private bool Visible()
	{
		if (!base.EntityManager.HasComponent<HouseholdPet>(selectedEntity))
		{
			return base.EntityManager.HasComponent<Wildlife>(selectedEntity);
		}
		return true;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.visible = Visible();
	}

	protected override void OnProcess()
	{
		typeKey = GetTypeKey();
		ownerEntity = (base.EntityManager.TryGetComponent<HouseholdPet>(selectedEntity, out var component) ? component.m_Household : Entity.Null);
		destinationEntity = GetDestination();
		base.tooltipKeys.Add(GetTypeKeyString(typeKey));
	}

	private Entity GetDestination()
	{
		if (base.EntityManager.TryGetComponent<CurrentTransport>(selectedEntity, out var component))
		{
			Entity entity = Entity.Null;
			if (base.EntityManager.TryGetComponent<Target>(component.m_CurrentTransport, out var component2))
			{
				entity = component2.m_Target;
			}
			if (base.EntityManager.HasComponent<OutsideConnection>(entity))
			{
				return entity;
			}
			if (base.EntityManager.TryGetComponent<Owner>(entity, out var component3))
			{
				return component3.m_Owner;
			}
			if (base.EntityManager.Exists(entity))
			{
				return entity;
			}
		}
		return Entity.Null;
	}

	private TypeKey GetTypeKey()
	{
		if (base.EntityManager.HasComponent<HouseholdPet>(selectedEntity))
		{
			return TypeKey.Pet;
		}
		if (base.EntityManager.HasComponent<Wildlife>(selectedEntity))
		{
			return TypeKey.Wildlife;
		}
		return TypeKey.Livestock;
	}

	public override void OnWriteProperties(IJsonWriter writer)
	{
		writer.PropertyName("typeKey");
		writer.Write(Enum.GetName(typeof(TypeKey), typeKey));
		writer.PropertyName("owner");
		if (ownerEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			m_NameSystem.BindName(writer, ownerEntity);
		}
		writer.PropertyName("ownerEntity");
		if (ownerEntity == Entity.Null)
		{
			writer.WriteNull();
		}
		else
		{
			writer.Write(ownerEntity);
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
	public AnimalSection()
	{
	}
}
