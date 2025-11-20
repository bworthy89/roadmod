using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using UnityEngine.Scripting;

namespace Game.Net;

[CompilerGenerated]
public class CostSystem : GameSystemBase
{
	[BurstCompile]
	private struct CalculateCostJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<Edge> m_EdgeType;

		[ReadOnly]
		public ComponentTypeHandle<Composition> m_CompositionType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Recent> m_RecentData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> m_PlacableNetData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PlaceableObjectData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public uint m_SimulationFrame;

		[ReadOnly]
		public EconomyParameterData m_EconomyParameterData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Curve> nativeArray2 = chunk.GetNativeArray(ref m_CurveType);
			NativeArray<Edge> nativeArray3 = chunk.GetNativeArray(ref m_EdgeType);
			NativeArray<Composition> nativeArray4 = chunk.GetNativeArray(ref m_CompositionType);
			NativeArray<Temp> nativeArray5 = chunk.GetNativeArray(ref m_TempType);
			BufferAccessor<Game.Objects.SubObject> bufferAccessor = chunk.GetBufferAccessor(ref m_SubObjectType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Temp value = nativeArray5[i];
				value.m_Cost = 0;
				value.m_Value = 0;
				if ((value.m_Flags & TempFlags.Essential) != 0)
				{
					int num = 0;
					int num2 = 0;
					if (nativeArray4.Length != 0)
					{
						Curve curve = nativeArray2[i];
						Edge edge = nativeArray3[i];
						Composition composition = nativeArray4[i];
						if ((value.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade | TempFlags.RemoveCost)) != 0)
						{
							PlaceableNetComposition componentData8;
							if (m_CompositionData.TryGetComponent(value.m_Original, out var componentData))
							{
								Curve curve2 = m_CurveData[value.m_Original];
								Edge edge2 = m_EdgeData[value.m_Original];
								if (m_PlacableNetData.TryGetComponent(composition.m_Edge, out var componentData2))
								{
									m_ElevationData.TryGetComponent(edge.m_Start, out var componentData3);
									m_ElevationData.TryGetComponent(edge.m_End, out var componentData4);
									num2 += NetUtils.GetConstructionCost(curve, componentData3, componentData4, componentData2);
								}
								if (m_PlacableNetData.TryGetComponent(componentData.m_Edge, out var componentData5))
								{
									m_ElevationData.TryGetComponent(edge2.m_Start, out var componentData6);
									m_ElevationData.TryGetComponent(edge2.m_End, out var componentData7);
									num += NetUtils.GetConstructionCost(curve2, componentData6, componentData7, componentData5);
								}
							}
							else if (m_PlacableNetData.TryGetComponent(composition.m_Edge, out componentData8))
							{
								m_ElevationData.TryGetComponent(edge.m_Start, out var componentData9);
								m_ElevationData.TryGetComponent(edge.m_End, out var componentData10);
								num2 += NetUtils.GetConstructionCost(curve, componentData9, componentData10, componentData8);
							}
						}
					}
					if ((value.m_Flags & (TempFlags.Create | TempFlags.Modify | TempFlags.Replace | TempFlags.Upgrade | TempFlags.RemoveCost)) != 0)
					{
						if (CollectionUtils.TryGet(bufferAccessor, i, out var value2))
						{
							for (int j = 0; j < value2.Length; j++)
							{
								Entity subObject = value2[j].m_SubObject;
								if (!m_OwnerData.TryGetComponent(subObject, out var componentData11) || !(componentData11.m_Owner == entity))
								{
									continue;
								}
								PrefabRef prefabRef = m_PrefabRefData[subObject];
								if (m_PlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData12))
								{
									int num3 = (int)componentData12.m_ConstructionCost;
									if (m_TreeData.TryGetComponent(subObject, out var componentData13))
									{
										num3 = ObjectUtils.GetContructionCost(num3, componentData13, in m_EconomyParameterData);
									}
									num2 += num3;
								}
							}
						}
						if (m_SubObjects.TryGetBuffer(value.m_Original, out var bufferData))
						{
							for (int k = 0; k < bufferData.Length; k++)
							{
								Entity subObject2 = bufferData[k].m_SubObject;
								if (!m_OwnerData.TryGetComponent(subObject2, out var componentData14) || !(componentData14.m_Owner == value.m_Original))
								{
									continue;
								}
								PrefabRef prefabRef2 = m_PrefabRefData[subObject2];
								if (m_PlaceableObjectData.TryGetComponent(prefabRef2.m_Prefab, out var componentData15))
								{
									int num4 = (int)componentData15.m_ConstructionCost;
									if (m_TreeData.TryGetComponent(subObject2, out var componentData16))
									{
										num4 = ObjectUtils.GetContructionCost(num4, componentData16, in m_EconomyParameterData);
									}
									num += num4;
								}
							}
						}
					}
					value.m_Value = num2;
					Recent componentData18;
					if ((value.m_Flags & TempFlags.RemoveCost) != 0)
					{
						value.m_Cost = -num;
						if ((value.m_Flags & TempFlags.Delete) == 0)
						{
							value.m_Cost += num2;
						}
					}
					else if ((value.m_Flags & TempFlags.Delete) != 0)
					{
						if ((value.m_Flags & TempFlags.Hidden) == 0 && m_RecentData.TryGetComponent(value.m_Original, out var componentData17))
						{
							value.m_Cost = -NetUtils.GetRefundAmount(componentData17, m_SimulationFrame, m_EconomyParameterData);
						}
					}
					else if (m_RecentData.TryGetComponent(value.m_Original, out componentData18))
					{
						value.m_Cost = NetUtils.GetUpgradeCost(num2, num, componentData18, m_SimulationFrame, m_EconomyParameterData);
					}
					else
					{
						value.m_Cost = NetUtils.GetUpgradeCost(num2, num);
					}
				}
				nativeArray5[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Edge> __Game_Net_Edge_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Composition> __Game_Net_Composition_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Recent> __Game_Tools_Recent_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableNetComposition> __Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Edge>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Composition>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Tools_Temp_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>();
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Elevation>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Tools_Recent_RO_ComponentLookup = state.GetComponentLookup<Recent>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup = state.GetComponentLookup<PlaceableNetComposition>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	private ToolSystem m_ToolSystem;

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_UpdatedTempNetQuery;

	private EntityQuery m_EconomyParameterQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_UpdatedTempNetQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Temp>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Node>()
			}
		});
		m_EconomyParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EconomyParameterData>());
		RequireForUpdate(m_EconomyParameterQuery);
		RequireForUpdate(m_UpdatedTempNetQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_ToolSystem.actionMode.IsEditor())
		{
			CalculateCostJob jobData = new CalculateCostJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_EdgeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Edge_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CompositionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Composition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
				m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
				m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
				m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
				m_RecentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Recent_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlacableNetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableNetComposition_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_EconomyParameterData = m_EconomyParameterQuery.GetSingleton<EconomyParameterData>(),
				m_SimulationFrame = m_SimulationSystem.frameIndex
			};
			base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_UpdatedTempNetQuery, base.Dependency);
		}
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
	public CostSystem()
	{
	}
}
