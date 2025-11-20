using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Game.UI.Localization;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.Tooltip;

[CompilerGenerated]
public class TempExtractorTooltipSystem : TooltipSystemBase
{
	private struct TreeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds2 m_Bounds;

		public Circle2 m_Circle;

		public ComponentLookup<Overridden> m_OverriddenData;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<TreeData> m_PrefabTreeData;

		public float m_Result;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if (m_OverriddenData.HasComponent(entity))
			{
				return;
			}
			Transform transform = m_TransformData[entity];
			if (!MathUtils.Intersect(m_Circle, transform.m_Position.xz))
			{
				return;
			}
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (m_PrefabTreeData.HasComponent(prefabRef.m_Prefab))
			{
				TreeData treeData = m_PrefabTreeData[prefabRef.m_Prefab];
				if (treeData.m_WoodAmount >= 1f)
				{
					m_Result += treeData.m_WoodAmount;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Overridden> __Game_Common_Overridden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LotData> __Game_Prefabs_LotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Overridden_RO_ComponentLookup = state.GetComponentLookup<Overridden>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup = state.GetComponentLookup<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_LotData_RO_ComponentLookup = state.GetComponentLookup<LotData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
		}
	}

	private NaturalResourceSystem m_NaturalResourceSystem;

	private ResourceSystem m_ResourceSystem;

	private CitySystem m_CitySystem;

	private ClimateSystem m_ClimateSystem;

	private PrefabSystem m_PrefabSystem;

	private Game.Objects.SearchSystem m_SearchSystem;

	private CityProductionStatisticSystem m_CityProductionStatisticSystem;

	private EntityQuery m_ErrorQuery;

	private EntityQuery m_TempQuery;

	private StringTooltip m_ResourceAvailable;

	private StringTooltip m_ResourceUnavailable;

	private IntTooltip m_Surplus;

	private IntTooltip m_Deficit;

	private StringTooltip m_ClimateAvailable;

	private StringTooltip m_ClimateUnavailable;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_ClimateSystem = base.World.GetOrCreateSystemManaged<ClimateSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_SearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_CityProductionStatisticSystem = base.World.GetOrCreateSystemManaged<CityProductionStatisticSystem>();
		m_ErrorQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Error>());
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadOnly<Placeholder>(), ComponentType.Exclude<Hidden>(), ComponentType.Exclude<Deleted>());
		m_ResourceAvailable = new StringTooltip
		{
			path = "extractorMapFeatureAvailable"
		};
		m_ResourceUnavailable = new StringTooltip
		{
			path = "extractorMapFeatureUnavailable",
			color = TooltipColor.Warning
		};
		m_ClimateAvailable = new StringTooltip
		{
			path = "extractorClimateAvailable",
			value = LocalizedString.Id("Tools.EXTRACTOR_CLIMATE_REQUIRED_AVAILABLE")
		};
		m_ClimateUnavailable = new StringTooltip
		{
			path = "extractorClimateUnavailable",
			value = LocalizedString.Id("Tools.EXTRACTOR_CLIMATE_REQUIRED_UNAVAILABLE"),
			color = TooltipColor.Warning
		};
		m_Surplus = new IntTooltip
		{
			path = "extractorCityProductionSurplus",
			label = LocalizedString.Id("Tools.EXTRACTOR_PRODUCTION_SURPLUS"),
			unit = "weightPerMonth"
		};
		m_Deficit = new IntTooltip
		{
			path = "extractorCityProductionDeficit",
			label = LocalizedString.Id("Tools.EXTRACTOR_PRODUCTION_DEFICIT"),
			unit = "weightPerMonth"
		};
		RequireForUpdate(m_TempQuery);
	}

	private bool FindWoodResource(Circle2 circle)
	{
		TreeIterator iterator = new TreeIterator
		{
			m_OverriddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Overridden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_Bounds = new Bounds2(circle.position - circle.radius, circle.position + circle.radius),
			m_Circle = circle,
			m_Result = 0f
		};
		JobHandle dependencies;
		NativeQuadTree<Entity, QuadTreeBoundsXZ> staticSearchTree = m_SearchSystem.GetStaticSearchTree(readOnly: true, out dependencies);
		dependencies.Complete();
		staticSearchTree.Iterate(ref iterator);
		return iterator.m_Result > 0f;
	}

	private bool FindResource(Circle2 circle, MapFeature requiredFeature, CellMapData<NaturalResourceCell> resourceMap, DynamicBuffer<CityModifier> cityModifiers)
	{
		int2 cell = CellMapSystem<NaturalResourceCell>.GetCell(new float3(circle.position.x - circle.radius, 0f, circle.position.y - circle.radius), CellMapSystem<NaturalResourceCell>.kMapSize, resourceMap.m_TextureSize.x);
		int2 cell2 = CellMapSystem<NaturalResourceCell>.GetCell(new float3(circle.position.x + circle.radius, 0f, circle.position.y + circle.radius), CellMapSystem<NaturalResourceCell>.kMapSize, resourceMap.m_TextureSize.x);
		cell = math.max(new int2(0, 0), cell);
		cell2 = math.min(new int2(resourceMap.m_TextureSize.x - 1, resourceMap.m_TextureSize.y - 1), cell2);
		int2 cell3 = default(int2);
		cell3.x = cell.x;
		while (cell3.x <= cell2.x)
		{
			cell3.y = cell.y;
			while (cell3.y <= cell2.y)
			{
				if (MathUtils.Intersect(circle, CellMapSystem<NaturalResourceCell>.GetCellCenter(cell3, resourceMap.m_TextureSize.x).xz))
				{
					NaturalResourceCell naturalResourceCell = resourceMap.m_Buffer[cell3.x + cell3.y * resourceMap.m_TextureSize.x];
					float num = 0f;
					switch (requiredFeature)
					{
					case MapFeature.FertileLand:
						num = (int)naturalResourceCell.m_Fertility.m_Base;
						num -= (float)(int)naturalResourceCell.m_Fertility.m_Used;
						break;
					case MapFeature.Ore:
						num = (int)naturalResourceCell.m_Ore.m_Base;
						if (cityModifiers.IsCreated)
						{
							CityUtils.ApplyModifier(ref num, cityModifiers, CityModifierType.OreResourceAmount);
						}
						num -= (float)(int)naturalResourceCell.m_Ore.m_Used;
						break;
					case MapFeature.Oil:
						num = (int)naturalResourceCell.m_Oil.m_Base;
						if (cityModifiers.IsCreated)
						{
							CityUtils.ApplyModifier(ref num, cityModifiers, CityModifierType.OilResourceAmount);
						}
						num -= (float)(int)naturalResourceCell.m_Oil.m_Used;
						break;
					case MapFeature.Fish:
						num = (int)naturalResourceCell.m_Fish.m_Base;
						num -= (float)(int)naturalResourceCell.m_Fish.m_Used;
						break;
					default:
						num = 0f;
						break;
					}
					if (num > 0f)
					{
						return true;
					}
				}
				cell3.y++;
			}
			cell3.x++;
		}
		return false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ErrorQuery.IsEmptyIgnoreFilter || !base.EntityManager.TryGetBuffer(m_CitySystem.City, isReadOnly: true, out DynamicBuffer<CityModifier> buffer))
		{
			return;
		}
		CompleteDependency();
		JobHandle dependencies;
		CellMapData<NaturalResourceCell> data = m_NaturalResourceSystem.GetData(readOnly: true, out dependencies);
		dependencies.Complete();
		ComponentLookup<PlaceholderBuildingData> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<BuildingPropertyData> componentLookup2 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ExtractorAreaData> componentLookup3 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<LotData> componentLookup4 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<ResourceData> componentLookup5 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<Transform> componentLookup6 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef);
		ComponentLookup<PrefabRef> componentLookup7 = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef);
		BufferLookup<Game.Areas.SubArea> bufferLookup = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<SubAreaNode> bufferLookup2 = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef);
		BufferLookup<InstalledUpgrade> bufferLookup3 = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef);
		ResourcePrefabs prefabs = m_ResourceSystem.GetPrefabs();
		NativeArray<ArchetypeChunk> nativeArray = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
		MapFeature requiredFeature = MapFeature.None;
		bool foundResource = false;
		bool flag = false;
		Resource resource = Resource.NoResource;
		try
		{
			ComponentTypeHandle<Temp> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			dependencies.Complete();
			foreach (ArchetypeChunk item in nativeArray)
			{
				NativeArray<Entity> nativeArray2 = item.GetNativeArray(InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef));
				NativeArray<Temp> nativeArray3 = item.GetNativeArray(ref typeHandle);
				for (int i = 0; i < nativeArray2.Length; i++)
				{
					Entity entity = nativeArray2[i];
					if ((nativeArray3[i].m_Flags & (TempFlags.Create | TempFlags.Upgrade)) != 0)
					{
						Entity entity2 = componentLookup7[entity];
						if (componentLookup.HasComponent(entity2) && componentLookup2.HasComponent(entity2) && componentLookup[entity2].m_Type == BuildingType.ExtractorBuilding)
						{
							resource = componentLookup2[entity2].m_AllowedManufactured;
							DynamicBuffer<InstalledUpgrade> bufferData2;
							if (bufferLookup.TryGetBuffer(entity, out var bufferData) && ProcessAreas(bufferData, componentLookup3, componentLookup4, resource, prefabs, componentLookup5, componentLookup6[entity], data, buffer, ref requiredFeature, ref foundResource))
							{
								flag = true;
							}
							else if (bufferLookup3.TryGetBuffer(entity, out bufferData2))
							{
								for (int j = 0; j < bufferData2.Length; j++)
								{
									if (bufferLookup2.TryGetBuffer(componentLookup7[bufferData2[j].m_Upgrade], out var bufferData3) && bufferLookup.TryGetBuffer(bufferData2[j].m_Upgrade, out bufferData) && ProcessAreaNodes(bufferData, bufferData3, componentLookup3, componentLookup4, resource, prefabs, componentLookup5, componentLookup6[bufferData2[j].m_Upgrade], data, buffer, ref requiredFeature, ref foundResource))
									{
										flag = true;
									}
									else if (bufferLookup.TryGetBuffer(bufferData2[j].m_Upgrade, out bufferData) && ProcessAreas(bufferData, componentLookup3, componentLookup4, resource, prefabs, componentLookup5, componentLookup6[bufferData2[j].m_Upgrade], data, buffer, ref requiredFeature, ref foundResource))
									{
										flag = true;
									}
									if (flag)
									{
										break;
									}
								}
							}
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
		}
		if (!flag)
		{
			return;
		}
		NativeArray<int2> consumptionProductions = m_CityProductionStatisticSystem.GetConsumptionProductions();
		int resourceIndex = EconomyUtils.GetResourceIndex(resource);
		int num = consumptionProductions[resourceIndex].y - consumptionProductions[resourceIndex].x;
		Entity entity3 = prefabs[resource];
		ResourceData resourceData = componentLookup5[entity3];
		string icon = ImageSystem.GetIcon(m_PrefabSystem.GetPrefab<PrefabBase>(entity3));
		if (num > 0)
		{
			m_Surplus.value = num;
			m_Surplus.icon = icon;
			AddMouseTooltip(m_Surplus);
		}
		else
		{
			m_Deficit.value = -num;
			m_Deficit.icon = icon;
			AddMouseTooltip(m_Deficit);
		}
		bool flag2 = ShouldMapFeatureUseResourceIcon(resource);
		if (requiredFeature != MapFeature.None)
		{
			string mapFeatureIconName = AreaTools.GetMapFeatureIconName(requiredFeature);
			if (foundResource)
			{
				m_ResourceAvailable.icon = (flag2 ? icon : ("Media/Game/Icons/" + mapFeatureIconName + ".svg"));
				m_ResourceAvailable.value = LocalizedString.Id("Tools.EXTRACTOR_MAP_FEATURE_REQUIRED_AVAILABLE");
				AddMouseTooltip(m_ResourceAvailable);
			}
			else
			{
				m_ResourceUnavailable.icon = (flag2 ? icon : ("Media/Game/Icons/" + mapFeatureIconName + ".svg"));
				m_ResourceUnavailable.value = LocalizedString.Id("Tools.EXTRACTOR_MAP_FEATURE_REQUIRED_MISSING");
				AddMouseTooltip(m_ResourceUnavailable);
			}
		}
		if (resourceData.m_RequireTemperature)
		{
			if (m_ClimateSystem.averageTemperature >= resourceData.m_RequiredTemperature)
			{
				AddMouseTooltip(m_ClimateAvailable);
			}
			else
			{
				AddMouseTooltip(m_ClimateUnavailable);
			}
		}
	}

	private bool ShouldMapFeatureUseResourceIcon(Resource resource)
	{
		if (resource == Resource.Fish)
		{
			return true;
		}
		return false;
	}

	private bool ProcessAreaNodes(DynamicBuffer<Game.Areas.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodeBuf, ComponentLookup<ExtractorAreaData> extractorAreaDatas, ComponentLookup<LotData> lotDatas, Resource extractedResource, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas, Transform transform, CellMapData<NaturalResourceCell> resourceMap, DynamicBuffer<CityModifier> cityModifiers, ref MapFeature requiredFeature, ref bool foundResource)
	{
		bool result = false;
		float num = 0f;
		for (int i = 0; i < subAreaNodeBuf.Length; i++)
		{
			float num2 = ((math.abs(subAreaNodeBuf[i].m_Position.x) > math.abs(subAreaNodeBuf[i].m_Position.z)) ? math.abs(subAreaNodeBuf[i].m_Position.x) : math.abs(subAreaNodeBuf[i].m_Position.z));
			if (num2 > num)
			{
				num = num2;
			}
		}
		for (int j = 0; j < subAreas.Length; j++)
		{
			if (!base.EntityManager.TryGetComponent<PrefabRef>(subAreas[j].m_Area, out var component))
			{
				continue;
			}
			Entity prefab = component.m_Prefab;
			if (!extractorAreaDatas.HasComponent(prefab) || !lotDatas.HasComponent(prefab))
			{
				continue;
			}
			result = true;
			requiredFeature = ExtractorCompanySystem.GetRequiredMapFeature(extractedResource, prefab, resourcePrefabs, resourceDatas, extractorAreaDatas);
			if (requiredFeature != MapFeature.None)
			{
				float3 position = transform.m_Position;
				Circle2 circle = new Circle2(num, position.xz);
				if (requiredFeature == MapFeature.Forest)
				{
					foundResource = FindWoodResource(circle);
				}
				else
				{
					foundResource = FindResource(circle, requiredFeature, resourceMap, cityModifiers);
				}
			}
		}
		return result;
	}

	private bool ProcessAreas(DynamicBuffer<Game.Areas.SubArea> subAreas, ComponentLookup<ExtractorAreaData> extractorAreaDatas, ComponentLookup<LotData> lotDatas, Resource extractedResource, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas, Transform transform, CellMapData<NaturalResourceCell> resourceMap, DynamicBuffer<CityModifier> cityModifiers, ref MapFeature requiredFeature, ref bool foundResource)
	{
		bool result = false;
		for (int i = 0; i < subAreas.Length; i++)
		{
			if (!base.EntityManager.TryGetComponent<PrefabRef>(subAreas[i].m_Area, out var component))
			{
				continue;
			}
			Entity prefab = component.m_Prefab;
			if (!extractorAreaDatas.HasComponent(prefab) || !lotDatas.HasComponent(prefab))
			{
				continue;
			}
			result = true;
			float maxRadius = lotDatas[prefab].m_MaxRadius;
			requiredFeature = ExtractorCompanySystem.GetRequiredMapFeature(extractedResource, prefab, resourcePrefabs, resourceDatas, extractorAreaDatas);
			if (requiredFeature != MapFeature.None)
			{
				float3 position = transform.m_Position;
				Circle2 circle = new Circle2(maxRadius, position.xz);
				if (requiredFeature == MapFeature.Forest)
				{
					foundResource = FindWoodResource(circle);
				}
				else
				{
					foundResource = FindResource(circle, requiredFeature, resourceMap, cityModifiers);
				}
			}
		}
		return result;
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
	public TempExtractorTooltipSystem()
	{
	}
}
