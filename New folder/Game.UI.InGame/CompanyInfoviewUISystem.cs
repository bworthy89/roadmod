using System;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Companies;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

[CompilerGenerated]
public class CompanyInfoviewUISystem : InfoviewUISystemBase
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<IndustrialProcessData> __Game_Prefabs_IndustrialProcessData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup = state.GetComponentLookup<IndustrialProcessData>(isReadOnly: true);
		}
	}

	private const string kGroup = "companyInfoview";

	private ResourceSystem m_ResourceSystem;

	private EntityQuery m_CommercialQuery;

	private EntityQuery m_IndustrialQuery;

	private GetterValueBinding<IndicatorValue> m_CommercialProfitability;

	private GetterValueBinding<IndicatorValue> m_IndustrialProfitability;

	private GetterValueBinding<IndicatorValue> m_OfficeProfitability;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1723701004_0;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_CommercialProfitability.active && !m_IndustrialProfitability.active)
			{
				return m_OfficeProfitability.active;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CommercialQuery = GetEntityQuery(ComponentType.ReadOnly<Profitability>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<CommercialCompany>());
		m_IndustrialQuery = GetEntityQuery(ComponentType.ReadOnly<Profitability>(), ComponentType.ReadOnly<PropertyRenter>(), ComponentType.ReadOnly<IndustrialCompany>());
		AddBinding(m_CommercialProfitability = new GetterValueBinding<IndicatorValue>("companyInfoview", "commercialProfitability", GetCommercialProfitability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_IndustrialProfitability = new GetterValueBinding<IndicatorValue>("companyInfoview", "industrialProfitability", GetIndustrialProfitability, new ValueWriter<IndicatorValue>()));
		AddBinding(m_OfficeProfitability = new GetterValueBinding<IndicatorValue>("companyInfoview", "officeProfitability", GetOfficeProfitability, new ValueWriter<IndicatorValue>()));
	}

	protected override void PerformUpdate()
	{
		m_CommercialProfitability.Update();
		m_IndustrialProfitability.Update();
		m_OfficeProfitability.Update();
	}

	private IndicatorValue GetCommercialProfitability()
	{
		NativeArray<Entity> nativeArray = m_CommercialQuery.ToEntityArray(Allocator.TempJob);
		float current = 0f;
		try
		{
			int num = 0;
			int num2 = 0;
			EconomyParameterData singleton = __query_1723701004_0.GetSingleton<EconomyParameterData>();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<CompanyStatisticData>(nativeArray[i], out var component))
				{
					num++;
					num2 += CompanyUtils.GetCompanyProfitability(component.m_Profit, singleton);
				}
			}
			current = ((num == 0) ? 0f : ((float)num2 / (float)num));
		}
		finally
		{
			nativeArray.Dispose();
		}
		return new IndicatorValue(0f, 255f, current);
	}

	private IndicatorValue GetOfficeProfitability()
	{
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		NativeArray<Entity> nativeArray = m_IndustrialQuery.ToEntityArray(Allocator.TempJob);
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ResourceData> datas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<IndustrialProcessData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef);
		float current = 0f;
		try
		{
			int num = 0;
			int num2 = 0;
			EconomyParameterData singleton = __query_1723701004_0.GetSingleton<EconomyParameterData>();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (base.EntityManager.TryGetComponent<CompanyStatisticData>(nativeArray[i], out var component) && componentLookup.HasComponent(nativeArray[i]))
				{
					PrefabRef prefabRef = componentLookup[nativeArray[i]];
					if (componentLookup2.HasComponent(prefabRef.m_Prefab) && !(Math.Abs(EconomyUtils.GetWeight(componentLookup2[prefabRef.m_Prefab].m_Output.m_Resource, prefabs, ref datas)) > float.Epsilon))
					{
						num++;
						num2 += CompanyUtils.GetCompanyProfitability(component.m_Profit, singleton);
					}
				}
			}
			current = ((num == 0) ? 0f : ((float)num2 / (float)num));
		}
		finally
		{
			nativeArray.Dispose();
		}
		return new IndicatorValue(0f, 255f, current);
	}

	private IndicatorValue GetIndustrialProfitability()
	{
		NativeArray<Entity> nativeArray = m_IndustrialQuery.ToEntityArray(Allocator.TempJob);
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ResourceData> datas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<IndustrialProcessData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_IndustrialProcessData_RO_ComponentLookup, ref base.CheckedStateRef);
		float current = 0f;
		try
		{
			int num = 0;
			int num2 = 0;
			EconomyParameterData singleton = __query_1723701004_0.GetSingleton<EconomyParameterData>();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				if (!base.EntityManager.TryGetComponent<CompanyStatisticData>(nativeArray[i], out var component) || !componentLookup.HasComponent(nativeArray[i]))
				{
					continue;
				}
				PrefabRef prefabRef = componentLookup[nativeArray[i]];
				if (componentLookup2.HasComponent(prefabRef.m_Prefab))
				{
					IndustrialProcessData industrialProcessData = componentLookup2[prefabRef.m_Prefab];
					if (!(Math.Abs(EconomyUtils.GetWeight(prefabs: m_ResourceSystem.GetPrefabs(), r: industrialProcessData.m_Output.m_Resource, datas: ref datas)) < float.Epsilon))
					{
						num++;
						num2 += CompanyUtils.GetCompanyProfitability(component.m_Profit, singleton);
					}
				}
			}
			current = ((num == 0) ? 0f : ((float)num2 / (float)num));
		}
		finally
		{
			nativeArray.Dispose();
		}
		return new IndicatorValue(0f, 255f, current);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<EconomyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1723701004_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public CompanyInfoviewUISystem()
	{
	}
}
