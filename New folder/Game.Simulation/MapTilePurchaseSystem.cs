using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.PSI.Common;
using Game.Achievements;
using Game.Areas;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MapTilePurchaseSystem : GameSystemBase, IMapTilePurchaseSystem
{
	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TilePurchaseCostFactor> __Game_Prefabs_TilePurchaseCostFactor_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Native> __Game_Common_Native_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MapTile> __Game_Areas_MapTile_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TilePurchaseCostFactor_RO_ComponentLookup = state.GetComponentLookup<TilePurchaseCostFactor>(isReadOnly: true);
			__Game_Common_Native_RO_ComponentLookup = state.GetComponentLookup<Native>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentLookup = state.GetComponentLookup<MapTile>(isReadOnly: true);
		}
	}

	private static readonly double kMapTileSizeModifier = 1.0 / Math.Pow(623.304347826087, 2.0);

	private static readonly double kResourceModifier = 8.0718994140625E-07;

	private static readonly int kAutoUnlockedTiles = 9;

	private static readonly double[] kMapFeatureBaselineModifiers = new double[8] { kMapTileSizeModifier, kMapTileSizeModifier, kResourceModifier, kResourceModifier, kResourceModifier, kResourceModifier, 1.0, kResourceModifier };

	private SelectionToolSystem m_SelectionToolSystem;

	private DefaultToolSystem m_DefaultToolSystem;

	private ToolSystem m_ToolSystem;

	private CitySystem m_CitySystem;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private MapTileSystem m_MapTileSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_SelectionQuery;

	private EntityQuery m_OwnedTileQuery;

	private EntityQuery m_LockedMapTilesQuery;

	private EntityQuery m_UnlockedMilestoneQuery;

	private EntityQuery m_LockedMilestoneQuery;

	private EntityQuery m_EconomyParameterQuery;

	private NativeArray<float> m_FeatureAmounts;

	private float m_Cost;

	private float m_Upkeep;

	private TypeHandle __TypeHandle;

	public TilePurchaseErrorFlags status { get; private set; }

	public bool selecting
	{
		get
		{
			if (m_ToolSystem.activeTool == m_SelectionToolSystem)
			{
				return m_SelectionToolSystem.selectionType == SelectionType.MapTiles;
			}
			return false;
		}
		set
		{
			if (value)
			{
				m_SelectionToolSystem.selectionType = SelectionType.MapTiles;
				m_SelectionToolSystem.selectionOwner = Entity.Null;
				m_ToolSystem.activeTool = m_SelectionToolSystem;
			}
			else if (m_ToolSystem.activeTool == m_SelectionToolSystem)
			{
				m_ToolSystem.activeTool = m_DefaultToolSystem;
			}
		}
	}

	public int cost => Mathf.RoundToInt(m_Cost);

	public int upkeep => Mathf.RoundToInt(m_Upkeep);

	public float GetMapTileUpkeepCostMultiplier(int tileCount)
	{
		if (tileCount <= kAutoUnlockedTiles)
		{
			return 0f;
		}
		return m_EconomyParameterQuery.GetSingleton<EconomyParameterData>().m_MapTileUpkeepCostMultiplier.Evaluate(tileCount);
	}

	public bool GetMapTileUpkeepEnabled()
	{
		if (m_CityConfigurationSystem.unlockMapTiles)
		{
			return false;
		}
		EconomyParameterData singleton = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>();
		float num = 0f;
		for (int i = 0; i <= 100; i += 10)
		{
			num = singleton.m_MapTileUpkeepCostMultiplier.Evaluate(i);
			if (num > 0f)
			{
				return true;
			}
		}
		return num != 0f;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SelectionToolSystem = base.World.GetOrCreateSystemManaged<SelectionToolSystem>();
		m_DefaultToolSystem = base.World.GetOrCreateSystemManaged<DefaultToolSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_MapTileSystem = base.World.GetOrCreateSystemManaged<MapTileSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_SelectionQuery = GetEntityQuery(ComponentType.ReadOnly<SelectionElement>());
		m_OwnedTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapTile>(), ComponentType.Exclude<Native>());
		m_LockedMapTilesQuery = GetEntityQuery(ComponentType.ReadWrite<MapTile>(), ComponentType.ReadOnly<Native>(), ComponentType.ReadOnly<Area>());
		m_UnlockedMilestoneQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneData>(), ComponentType.Exclude<Locked>());
		m_LockedMilestoneQuery = GetEntityQuery(ComponentType.ReadOnly<MilestoneData>(), ComponentType.ReadOnly<Locked>());
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		m_FeatureAmounts = new NativeArray<float>(9, Allocator.Persistent);
		RequireForUpdate(m_EconomyParameterQuery);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_FeatureAmounts.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UpdateStatus();
	}

	private void UpdateStatus()
	{
		m_FeatureAmounts.Fill(0f);
		m_Cost = 0f;
		m_Upkeep = 0f;
		int availableTiles = GetAvailableTiles();
		if (availableTiles == 0)
		{
			status = (IsMilestonesLeft() ? TilePurchaseErrorFlags.NoCurrentlyAvailable : TilePurchaseErrorFlags.NoAvailable);
			return;
		}
		if (!TryGetSelections(isReadOnly: true, out var selections) || selections.Length == 0)
		{
			status = TilePurchaseErrorFlags.NoSelection;
			return;
		}
		status = TilePurchaseErrorFlags.None;
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<TilePurchaseCostFactor> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TilePurchaseCostFactor_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Native> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Native_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<MapTile> componentLookup4 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentLookup, ref base.CheckedStateRef);
		NativeList<float> list = new NativeList<float>(Allocator.Temp);
		int num = 0;
		int num2 = CalculateOwnedTiles();
		float num3 = CalculateOwnedTilesCost(includeSelection: true);
		float num4 = num3 * GetMapTileUpkeepCostMultiplier(num2);
		float num5 = num3;
		for (int i = 0; i < selections.Length; i++)
		{
			Entity entity = selections[i].m_Entity;
			if (!componentLookup4.HasComponent(entity) || !componentLookup3.HasComponent(entity))
			{
				continue;
			}
			num++;
			if (!base.EntityManager.TryGetBuffer(entity, isReadOnly: true, out DynamicBuffer<MapFeatureElement> buffer))
			{
				continue;
			}
			Entity prefab = componentLookup[entity].m_Prefab;
			float amount = componentLookup2[prefab].m_Amount;
			if (base.EntityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<MapFeatureData> buffer2))
			{
				float value = 0f;
				for (int j = 0; j < buffer.Length; j++)
				{
					float amount2 = buffer[j].m_Amount;
					m_FeatureAmounts[j] += amount2;
					double baselineModifier = GetBaselineModifier(j);
					value += (float)((double)amount2 * baselineModifier * 10.0 * (double)buffer2[j].m_Cost * (double)amount);
					num5 += (float)((double)amount2 * baselineModifier * 10.0 * (double)buffer2[j].m_Cost * (double)amount);
				}
				list.Add(in value);
			}
		}
		list.Sort();
		for (int k = 0; k < list.Length; k++)
		{
			m_Cost += list[list.Length - k - 1] * (float)(num2 + k);
		}
		list.Dispose();
		m_Upkeep = num5 * GetMapTileUpkeepCostMultiplier(num2 + num) - num4;
		if (num > 0 && num > availableTiles)
		{
			status |= TilePurchaseErrorFlags.InsufficientPermits;
		}
		if (cost > m_CitySystem.moneyAmount)
		{
			status |= TilePurchaseErrorFlags.InsufficientFunds;
		}
	}

	public int GetAvailableTiles()
	{
		int num = CalculateOwnedTiles();
		NativeList<Entity> startTiles = m_MapTileSystem.GetStartTiles();
		int num2 = math.max(kAutoUnlockedTiles, startTiles.Length);
		NativeArray<MilestoneData> nativeArray = m_UnlockedMilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.Temp);
		try
		{
			foreach (MilestoneData item in nativeArray)
			{
				num2 += item.m_MapTiles;
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		return Mathf.Max(num2 - num, 0);
	}

	public bool IsMilestonesLeft()
	{
		NativeArray<MilestoneData> nativeArray = m_LockedMilestoneQuery.ToComponentDataArray<MilestoneData>(Allocator.Temp);
		try
		{
			return nativeArray.Length != 0;
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	private double GetBaselineModifier(int mapFeature)
	{
		if (mapFeature >= 0 && mapFeature < kMapFeatureBaselineModifiers.Length)
		{
			return kMapFeatureBaselineModifiers[mapFeature];
		}
		return 1.0;
	}

	public void UnlockMapTiles()
	{
		if (!m_LockedMapTilesQuery.IsEmptyIgnoreFilter)
		{
			NativeArray<Entity> nativeArray = m_LockedMapTilesQuery.ToEntityArray(Allocator.Temp);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity area = nativeArray[i];
				UnlockTile(base.EntityManager, area);
			}
			nativeArray.Dispose();
		}
	}

	public void PurchaseSelection()
	{
		UpdateStatus();
		if (status != TilePurchaseErrorFlags.None)
		{
			return;
		}
		PlayerMoney componentData = base.EntityManager.GetComponentData<PlayerMoney>(m_CitySystem.City);
		componentData.Subtract(cost);
		base.EntityManager.SetComponentData(m_CitySystem.City, componentData);
		if (!TryGetSelections(isReadOnly: false, out var selections))
		{
			return;
		}
		NativeArray<SelectionElement> nativeArray = selections.ToNativeArray(Allocator.Temp);
		try
		{
			selections.Clear();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i].m_Entity;
				UnlockTile(base.EntityManager, entity);
			}
			PlatformManager.instance.IndicateAchievementProgress(new AchievementId[2]
			{
				Game.Achievements.Achievements.TheExplorer,
				Game.Achievements.Achievements.EverythingTheLightTouches
			}, CalculateOwnedTiles());
		}
		finally
		{
			nativeArray.Dispose();
		}
	}

	public static void UnlockTile(EntityManager entityManager, Entity area)
	{
		if (entityManager.HasComponent<Native>(area))
		{
			entityManager.RemoveComponent<Native>(area);
			entityManager.AddComponentData(area, default(Updated));
		}
	}

	private bool TryGetSelections(bool isReadOnly, out DynamicBuffer<SelectionElement> selections)
	{
		if (selecting && !m_SelectionQuery.IsEmptyIgnoreFilter)
		{
			Entity singletonEntity = m_SelectionQuery.GetSingletonEntity();
			if (base.EntityManager.TryGetBuffer(singletonEntity, isReadOnly, out selections))
			{
				return true;
			}
		}
		selections = default(DynamicBuffer<SelectionElement>);
		return false;
	}

	public int GetSelectedTileCount()
	{
		if (TryGetSelections(isReadOnly: true, out var selections))
		{
			return selections.Length;
		}
		return 0;
	}

	private int CalculateOwnedTiles()
	{
		return m_OwnedTileQuery.CalculateEntityCountWithoutFiltering();
	}

	private float CalculateOwnedTilesCost(bool includeSelection)
	{
		ComponentLookup<PrefabRef> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<TilePurchaseCostFactor> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TilePurchaseCostFactor_RO_ComponentLookup, ref base.CheckedStateRef);
		NativeList<Entity> startTiles = m_MapTileSystem.GetStartTiles();
		NativeArray<Entity> nativeArray;
		if (includeSelection && TryGetSelections(isReadOnly: true, out var selections) && selections.Length != 0)
		{
			nativeArray = new NativeArray<Entity>(selections.Length, Allocator.TempJob);
			for (int i = 0; i < selections.Length; i++)
			{
				nativeArray[i] = selections[i].m_Entity;
			}
		}
		else
		{
			nativeArray = m_OwnedTileQuery.ToEntityArray(Allocator.TempJob);
		}
		float num = 0f;
		for (int j = 0; j < nativeArray.Length; j++)
		{
			if (startTiles.Contains(nativeArray[j]) || !base.EntityManager.TryGetBuffer(nativeArray[j], isReadOnly: true, out DynamicBuffer<MapFeatureElement> buffer))
			{
				continue;
			}
			Entity prefab = componentLookup[nativeArray[j]].m_Prefab;
			float amount = componentLookup2[prefab].m_Amount;
			if (base.EntityManager.TryGetBuffer(prefab, isReadOnly: true, out DynamicBuffer<MapFeatureData> buffer2))
			{
				for (int k = 0; k < buffer.Length; k++)
				{
					float amount2 = buffer[k].m_Amount;
					m_FeatureAmounts[k] += amount2;
					double baselineModifier = GetBaselineModifier(k);
					num += (float)((double)amount2 * baselineModifier * 10.0 * (double)buffer2[k].m_Cost * (double)amount);
				}
			}
		}
		nativeArray.Dispose();
		return num;
	}

	public int CalculateOwnedTilesUpkeep()
	{
		return Mathf.RoundToInt(CalculateOwnedTilesCost(includeSelection: false) * GetMapTileUpkeepCostMultiplier(CalculateOwnedTiles()));
	}

	public float GetFeatureAmount(MapFeature feature)
	{
		float num = m_FeatureAmounts[(int)feature];
		if (feature != MapFeature.FertileLand)
		{
			return num;
		}
		return m_NaturalResourceSystem.ResourceAmountToArea(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public MapTilePurchaseSystem()
	{
	}
}
