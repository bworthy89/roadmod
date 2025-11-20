using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class CarTrailerMoveSystem : GameSystemBase
{
	[BurstCompile]
	private struct CarTrailerMoveJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> m_LayoutElementType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<CarTractorData> m_PrefabTractorData;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> m_PrefabTrailerData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Transform> m_TransformData;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Moving> m_MovingData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<TransformFrame> m_TransformFrames;

		[ReadOnly]
		public uint m_TransformFrameIndex;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			BufferAccessor<LayoutElement> bufferAccessor = chunk.GetBufferAccessor(ref m_LayoutElementType);
			float num = 4f / 15f;
			bool flag = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				DynamicBuffer<LayoutElement> dynamicBuffer = bufferAccessor[i];
				if (dynamicBuffer.Length <= 1)
				{
					continue;
				}
				Transform transform = m_TransformData[entity];
				Moving moving = m_MovingData[entity];
				DynamicBuffer<TransformFrame> dynamicBuffer2 = m_TransformFrames[entity];
				CarTractorData carTractorData = m_PrefabTractorData[prefabRef.m_Prefab];
				for (int j = 1; j < dynamicBuffer.Length; j++)
				{
					Entity vehicle = dynamicBuffer[j].m_Vehicle;
					PrefabRef prefabRef2 = m_PrefabRefData[vehicle];
					Transform transform2 = m_TransformData[vehicle];
					Moving moving2 = m_MovingData[vehicle];
					CarData carData = m_PrefabCarData[prefabRef2.m_Prefab];
					CarTrailerData carTrailerData = m_PrefabTrailerData[prefabRef2.m_Prefab];
					quaternion quaternion;
					float3 @float;
					if (flag)
					{
						quaternion = transform.m_Rotation;
						@float = transform.m_Position;
						@float += math.rotate(transform.m_Rotation, carTractorData.m_AttachPosition);
						@float -= math.rotate(transform.m_Rotation, carTrailerData.m_AttachPosition);
					}
					else
					{
						switch (carTrailerData.m_MovementType)
						{
						case TrailerMovementType.Free:
						{
							float3 float2 = transform.m_Position + math.rotate(transform.m_Rotation, carTractorData.m_AttachPosition);
							float3 float3 = transform2.m_Position + math.rotate(transform2.m_Rotation, new float3(carTrailerData.m_AttachPosition.xy, carData.m_PivotOffset));
							quaternion = transform.m_Rotation;
							float3 value = float2 - float3;
							value += moving.m_Velocity * (num * 0.25f);
							value -= moving2.m_Velocity * (num * 0.5f);
							if (MathUtils.TryNormalize(ref value))
							{
								quaternion = quaternion.LookRotationSafe(value, math.up());
							}
							@float = float2 - math.rotate(quaternion, carTrailerData.m_AttachPosition);
							break;
						}
						case TrailerMovementType.Locked:
							quaternion = transform.m_Rotation;
							@float = transform.m_Position;
							@float += math.rotate(transform.m_Rotation, carTractorData.m_AttachPosition);
							@float -= math.rotate(transform2.m_Rotation, carTrailerData.m_AttachPosition);
							break;
						default:
							@float = transform2.m_Position;
							quaternion = transform2.m_Rotation;
							break;
						}
					}
					moving2.m_Velocity = (@float - transform2.m_Position) / num;
					TransformFrame value2 = new TransformFrame
					{
						m_Position = (transform2.m_Position + @float) * 0.5f,
						m_Velocity = moving2.m_Velocity,
						m_Rotation = math.slerp(transform2.m_Rotation, quaternion, 0.5f),
						m_Flags = dynamicBuffer2[(int)m_TransformFrameIndex].m_Flags
					};
					transform2.m_Position = @float;
					transform2.m_Rotation = quaternion;
					DynamicBuffer<TransformFrame> dynamicBuffer3 = m_TransformFrames[vehicle];
					dynamicBuffer3[(int)m_TransformFrameIndex] = value2;
					m_MovingData[vehicle] = moving2;
					m_TransformData[vehicle] = transform2;
					if (j + 1 < dynamicBuffer.Length)
					{
						transform = transform2;
						moving = moving2;
						carTractorData = m_PrefabTractorData[prefabRef2.m_Prefab];
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
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTractorData> __Game_Prefabs_CarTractorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarTrailerData> __Game_Prefabs_CarTrailerData_RO_ComponentLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<Moving> __Game_Objects_Moving_RW_ComponentLookup;

		public BufferLookup<TransformFrame> __Game_Objects_TransformFrame_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<LayoutElement>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_CarTractorData_RO_ComponentLookup = state.GetComponentLookup<CarTractorData>(isReadOnly: true);
			__Game_Prefabs_CarTrailerData_RO_ComponentLookup = state.GetComponentLookup<CarTrailerData>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Objects_Moving_RW_ComponentLookup = state.GetComponentLookup<Moving>();
			__Game_Objects_TransformFrame_RW_BufferLookup = state.GetBufferLookup<TransformFrame>();
		}
	}

	private SimulationSystem m_SimulationSystem;

	private EntityQuery m_VehicleQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<LayoutElement>(), ComponentType.ReadOnly<UpdateFrame>(), ComponentType.Exclude<Stopped>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint index = m_SimulationSystem.frameIndex & 0xF;
		m_VehicleQuery.ResetFilter();
		m_VehicleQuery.SetSharedComponentFilter(new UpdateFrame(index));
		JobHandle dependency = JobChunkExtensions.ScheduleParallel(new CarTrailerMoveJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LayoutElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTractorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTrailerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarTrailerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MovingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Moving_RW_ComponentLookup, ref base.CheckedStateRef),
			m_TransformFrames = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_TransformFrame_RW_BufferLookup, ref base.CheckedStateRef),
			m_TransformFrameIndex = m_SimulationSystem.frameIndex / 16 % 4
		}, m_VehicleQuery, base.Dependency);
		base.Dependency = dependency;
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
	public CarTrailerMoveSystem()
	{
	}
}
