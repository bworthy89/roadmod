using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Serialization;

public class HouseholdSystem : GameSystemBase, IPostDeserialize
{
	private EntityQuery m_MovingInHouseholdQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_MovingInHouseholdQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Household>() },
			None = new ComponentType[5]
			{
				ComponentType.ReadOnly<TouristHousehold>(),
				ComponentType.ReadOnly<CommuterHousehold>(),
				ComponentType.ReadOnly<MovingAway>(),
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		RequireForUpdate(m_MovingInHouseholdQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
	}

	public void PostDeserialize(Context context)
	{
		if (!(context.version < Version.clearMovingInHousehold))
		{
			return;
		}
		NativeArray<Entity> nativeArray = m_MovingInHouseholdQuery.ToEntityArray(Allocator.Temp);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			if (base.EntityManager.TryGetComponent<Household>(nativeArray[i], out var component) && (component.m_Flags & HouseholdFlags.MovedIn) == 0 && (!base.EntityManager.TryGetComponent<PropertyRenter>(nativeArray[i], out var component2) || component2.m_Property == Entity.Null))
			{
				base.EntityManager.AddComponent<Deleted>(nativeArray[i]);
			}
		}
		nativeArray.Dispose();
	}

	[Preserve]
	public HouseholdSystem()
	{
	}
}
