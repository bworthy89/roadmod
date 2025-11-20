using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Creatures;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using UnityEngine.Scripting;

namespace Game.Serialization;

[CompilerGenerated]
public class LaneObjectSystem : GameSystemBase
{
	[BurstCompile]
	private struct LaneObjectJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CarCurrentLane> m_CarCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> m_CarTrailerLaneType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> m_ParkedCarType;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> m_ParkedTrainType;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> m_WatercraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> m_AircraftCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> m_TrainCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> m_AnimalCurrentLaneType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> m_BlockedLaneType;

		public ComponentTypeHandle<HumanCurrentLane> m_HumanCurrentLaneType;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_ObjectGeometryData;

		public BufferLookup<LaneObject> m_LaneObjects;

		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<CarCurrentLane> nativeArray4 = chunk.GetNativeArray(ref m_CarCurrentLaneType);
			NativeArray<CarTrailerLane> nativeArray5 = chunk.GetNativeArray(ref m_CarTrailerLaneType);
			NativeArray<ParkedCar> nativeArray6 = chunk.GetNativeArray(ref m_ParkedCarType);
			NativeArray<ParkedTrain> nativeArray7 = chunk.GetNativeArray(ref m_ParkedTrainType);
			NativeArray<WatercraftCurrentLane> nativeArray8 = chunk.GetNativeArray(ref m_WatercraftCurrentLaneType);
			NativeArray<AircraftCurrentLane> nativeArray9 = chunk.GetNativeArray(ref m_AircraftCurrentLaneType);
			NativeArray<TrainCurrentLane> nativeArray10 = chunk.GetNativeArray(ref m_TrainCurrentLaneType);
			NativeArray<HumanCurrentLane> nativeArray11 = chunk.GetNativeArray(ref m_HumanCurrentLaneType);
			NativeArray<AnimalCurrentLane> nativeArray12 = chunk.GetNativeArray(ref m_AnimalCurrentLaneType);
			BufferAccessor<BlockedLane> bufferAccessor = chunk.GetBufferAccessor(ref m_BlockedLaneType);
			for (int i = 0; i < nativeArray4.Length; i++)
			{
				Entity entity = nativeArray[i];
				CarCurrentLane carCurrentLane = nativeArray4[i];
				if (m_LaneObjects.HasBuffer(carCurrentLane.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[carCurrentLane.m_Lane], entity, carCurrentLane.m_CurvePosition.xy);
				}
				else
				{
					Transform transform = nativeArray2[i];
					PrefabRef prefabRef = nativeArray3[i];
					ObjectGeometryData geometryData = m_ObjectGeometryData[prefabRef.m_Prefab];
					Bounds3 bounds = ObjectUtils.CalculateBounds(transform.m_Position, transform.m_Rotation, geometryData);
					m_SearchTree.Add(entity, new QuadTreeBoundsXZ(bounds));
				}
				if (m_LaneObjects.HasBuffer(carCurrentLane.m_ChangeLane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[carCurrentLane.m_ChangeLane], entity, carCurrentLane.m_CurvePosition.xy);
				}
			}
			for (int j = 0; j < nativeArray5.Length; j++)
			{
				Entity entity2 = nativeArray[j];
				CarTrailerLane carTrailerLane = nativeArray5[j];
				if (m_LaneObjects.HasBuffer(carTrailerLane.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[carTrailerLane.m_Lane], entity2, carTrailerLane.m_CurvePosition.xy);
				}
				else
				{
					Transform transform2 = nativeArray2[j];
					PrefabRef prefabRef2 = nativeArray3[j];
					ObjectGeometryData geometryData2 = m_ObjectGeometryData[prefabRef2.m_Prefab];
					Bounds3 bounds2 = ObjectUtils.CalculateBounds(transform2.m_Position, transform2.m_Rotation, geometryData2);
					m_SearchTree.Add(entity2, new QuadTreeBoundsXZ(bounds2));
				}
				if (m_LaneObjects.HasBuffer(carTrailerLane.m_NextLane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[carTrailerLane.m_NextLane], entity2, carTrailerLane.m_NextPosition.xy);
				}
			}
			for (int k = 0; k < nativeArray6.Length; k++)
			{
				Entity entity3 = nativeArray[k];
				ParkedCar parkedCar = nativeArray6[k];
				if (m_LaneObjects.HasBuffer(parkedCar.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[parkedCar.m_Lane], entity3, parkedCar.m_CurvePosition);
					continue;
				}
				Transform transform3 = nativeArray2[k];
				PrefabRef prefabRef3 = nativeArray3[k];
				ObjectGeometryData geometryData3 = m_ObjectGeometryData[prefabRef3.m_Prefab];
				Bounds3 bounds3 = ObjectUtils.CalculateBounds(transform3.m_Position, transform3.m_Rotation, geometryData3);
				m_SearchTree.Add(entity3, new QuadTreeBoundsXZ(bounds3));
			}
			for (int l = 0; l < nativeArray8.Length; l++)
			{
				Entity entity4 = nativeArray[l];
				WatercraftCurrentLane watercraftCurrentLane = nativeArray8[l];
				if (m_LaneObjects.HasBuffer(watercraftCurrentLane.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[watercraftCurrentLane.m_Lane], entity4, watercraftCurrentLane.m_CurvePosition.xy);
				}
				else
				{
					Transform transform4 = nativeArray2[l];
					PrefabRef prefabRef4 = nativeArray3[l];
					ObjectGeometryData geometryData4 = m_ObjectGeometryData[prefabRef4.m_Prefab];
					Bounds3 bounds4 = ObjectUtils.CalculateBounds(transform4.m_Position, transform4.m_Rotation, geometryData4);
					m_SearchTree.Add(entity4, new QuadTreeBoundsXZ(bounds4));
				}
				if (m_LaneObjects.HasBuffer(watercraftCurrentLane.m_ChangeLane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[watercraftCurrentLane.m_ChangeLane], entity4, watercraftCurrentLane.m_CurvePosition.xy);
				}
			}
			for (int m = 0; m < nativeArray9.Length; m++)
			{
				Entity entity5 = nativeArray[m];
				AircraftCurrentLane aircraftCurrentLane = nativeArray9[m];
				if (m_LaneObjects.HasBuffer(aircraftCurrentLane.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[aircraftCurrentLane.m_Lane], entity5, aircraftCurrentLane.m_CurvePosition.xy);
				}
				if (!m_LaneObjects.HasBuffer(aircraftCurrentLane.m_Lane) || (aircraftCurrentLane.m_LaneFlags & AircraftLaneFlags.Flying) != 0)
				{
					Transform transform5 = nativeArray2[m];
					PrefabRef prefabRef5 = nativeArray3[m];
					ObjectGeometryData geometryData5 = m_ObjectGeometryData[prefabRef5.m_Prefab];
					Bounds3 bounds5 = ObjectUtils.CalculateBounds(transform5.m_Position, transform5.m_Rotation, geometryData5);
					m_SearchTree.Add(entity5, new QuadTreeBoundsXZ(bounds5));
				}
			}
			for (int n = 0; n < nativeArray10.Length; n++)
			{
				Entity laneObject = nativeArray[n];
				TrainCurrentLane currentLane = nativeArray10[n];
				TrainNavigationHelpers.GetCurvePositions(ref currentLane, out var pos, out var pos2);
				if (m_LaneObjects.TryGetBuffer(currentLane.m_Front.m_Lane, out var bufferData))
				{
					NetUtils.AddLaneObject(bufferData, laneObject, pos);
				}
				if (currentLane.m_Rear.m_Lane != currentLane.m_Front.m_Lane && m_LaneObjects.TryGetBuffer(currentLane.m_Rear.m_Lane, out bufferData))
				{
					NetUtils.AddLaneObject(bufferData, laneObject, pos2);
				}
			}
			for (int num = 0; num < nativeArray7.Length; num++)
			{
				Entity laneObject2 = nativeArray[num];
				ParkedTrain parkedTrain = nativeArray7[num];
				TrainNavigationHelpers.GetCurvePositions(ref parkedTrain, out var pos3, out var pos4);
				if (m_LaneObjects.TryGetBuffer(parkedTrain.m_FrontLane, out var bufferData2))
				{
					NetUtils.AddLaneObject(bufferData2, laneObject2, pos3);
				}
				if (parkedTrain.m_RearLane != parkedTrain.m_FrontLane && m_LaneObjects.TryGetBuffer(parkedTrain.m_RearLane, out bufferData2))
				{
					NetUtils.AddLaneObject(bufferData2, laneObject2, pos4);
				}
			}
			for (int num2 = 0; num2 < nativeArray11.Length; num2++)
			{
				Entity entity6 = nativeArray[num2];
				HumanCurrentLane value = nativeArray11[num2];
				if (m_LaneObjects.HasBuffer(value.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[value.m_Lane], entity6, value.m_CurvePosition.xx);
					continue;
				}
				if ((value.m_Flags & CreatureLaneFlags.TransformTarget) == 0 && m_TransformData.HasComponent(value.m_Lane))
				{
					value.m_Flags |= CreatureLaneFlags.TransformTarget;
					nativeArray11[num2] = value;
				}
				Transform transform6 = nativeArray2[num2];
				PrefabRef prefabRef6 = nativeArray3[num2];
				ObjectGeometryData geometryData6 = m_ObjectGeometryData[prefabRef6.m_Prefab];
				Bounds3 bounds6 = ObjectUtils.CalculateBounds(transform6.m_Position, transform6.m_Rotation, geometryData6);
				m_SearchTree.Add(entity6, new QuadTreeBoundsXZ(bounds6));
			}
			for (int num3 = 0; num3 < nativeArray12.Length; num3++)
			{
				Entity entity7 = nativeArray[num3];
				AnimalCurrentLane animalCurrentLane = nativeArray12[num3];
				if (m_LaneObjects.HasBuffer(animalCurrentLane.m_Lane))
				{
					NetUtils.AddLaneObject(m_LaneObjects[animalCurrentLane.m_Lane], entity7, animalCurrentLane.m_CurvePosition.xx);
					continue;
				}
				Transform transform7 = nativeArray2[num3];
				PrefabRef prefabRef7 = nativeArray3[num3];
				ObjectGeometryData geometryData7 = m_ObjectGeometryData[prefabRef7.m_Prefab];
				Bounds3 bounds7 = ObjectUtils.CalculateBounds(transform7.m_Position, transform7.m_Rotation, geometryData7);
				m_SearchTree.Add(entity7, new QuadTreeBoundsXZ(bounds7));
			}
			for (int num4 = 0; num4 < bufferAccessor.Length; num4++)
			{
				Entity laneObject3 = nativeArray[num4];
				DynamicBuffer<BlockedLane> dynamicBuffer = bufferAccessor[num4];
				for (int num5 = 0; num5 < dynamicBuffer.Length; num5++)
				{
					BlockedLane blockedLane = dynamicBuffer[num5];
					if (m_LaneObjects.HasBuffer(blockedLane.m_Lane))
					{
						NetUtils.AddLaneObject(m_LaneObjects[blockedLane.m_Lane], laneObject3, blockedLane.m_CurvePosition);
					}
				}
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
		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerLane> __Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkedTrain> __Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WatercraftCurrentLane> __Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TrainCurrentLane> __Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AnimalCurrentLane> __Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<BlockedLane> __Game_Objects_BlockedLane_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<HumanCurrentLane> __Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		public BufferLookup<LaneObject> __Game_Net_LaneObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>(isReadOnly: true);
			__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CarTrailerLane>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkedTrain>(isReadOnly: true);
			__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WatercraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>(isReadOnly: true);
			__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TrainCurrentLane>(isReadOnly: true);
			__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AnimalCurrentLane>(isReadOnly: true);
			__Game_Objects_BlockedLane_RO_BufferTypeHandle = state.GetBufferTypeHandle<BlockedLane>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<HumanCurrentLane>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Net_LaneObject_RW_BufferLookup = state.GetBufferLookup<LaneObject>();
		}
	}

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private EntityQuery m_Query;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_Query = GetEntityQuery(new EntityQueryDesc
		{
			Any = new ComponentType[10]
			{
				ComponentType.ReadOnly<CarCurrentLane>(),
				ComponentType.ReadOnly<CarTrailerLane>(),
				ComponentType.ReadOnly<ParkedCar>(),
				ComponentType.ReadOnly<ParkedTrain>(),
				ComponentType.ReadOnly<WatercraftCurrentLane>(),
				ComponentType.ReadOnly<AircraftCurrentLane>(),
				ComponentType.ReadOnly<TrainCurrentLane>(),
				ComponentType.ReadOnly<HumanCurrentLane>(),
				ComponentType.ReadOnly<AnimalCurrentLane>(),
				ComponentType.ReadOnly<BlockedLane>()
			}
		});
		RequireForUpdate(m_Query);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.Schedule(new LaneObjectJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CarCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarTrailerLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarTrailerLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkedCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkedTrainType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_ParkedTrain_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WatercraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_WatercraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TrainCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_TrainCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimalCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_AnimalCurrentLane_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BlockedLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_BlockedLane_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HumanCurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Creatures_HumanCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_SearchTree = m_ObjectSearchSystem.GetMovingSearchTree(readOnly: false, out dependencies)
		}, m_Query, JobHandle.CombineDependencies(base.Dependency, dependencies));
		m_ObjectSearchSystem.AddMovingSearchTreeWriter(jobHandle);
		base.Dependency = jobHandle;
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
	public LaneObjectSystem()
	{
	}
}
