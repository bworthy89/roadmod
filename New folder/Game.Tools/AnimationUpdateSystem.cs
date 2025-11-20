using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class AnimationUpdateSystem : GameSystemBase
{
	[BurstCompile]
	private struct AnimationMapJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<Animation> m_AnimationType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public NativeParallelMultiHashMap<Entity, Animation> m_AnimationMap;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Animation> nativeArray = chunk.GetNativeArray(ref m_AnimationType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Animation item = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				if (!item.m_Rotation.Equals(default(quaternion)))
				{
					m_AnimationMap.Add(prefabRef.m_Prefab, item);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AnimationUpdateJob : IJobChunk
	{
		[ReadOnly]
		public float m_DeltaTime;

		[ReadOnly]
		public NativeParallelMultiHashMap<Entity, Animation> m_AnimationMap;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		public ComponentTypeHandle<Animation> m_AnimationType;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Game.Objects.Transform> nativeArray = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Temp> nativeArray2 = chunk.GetNativeArray(ref m_TempType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Animation> nativeArray5 = chunk.GetNativeArray(ref m_AnimationType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Game.Objects.Transform transform = nativeArray[i];
				Temp temp = nativeArray2[i];
				PrefabRef prefabRef = nativeArray4[i];
				Animation value = nativeArray5[i];
				Owner owner = default(Owner);
				if (nativeArray3.Length != 0)
				{
					owner = nativeArray3[i];
				}
				if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					float3 pivot = componentData.m_Pivot;
					if (m_PrefabNetGeometryData.TryGetComponent(owner.m_Owner, out var componentData2))
					{
						pivot = new float3(0f, MathUtils.Center(componentData2.m_DefaultHeightRange), 0f);
					}
					value = CalculateAnimationData(transform, prefabRef, temp, componentData.m_Size, pivot);
				}
				nativeArray5[i] = value;
			}
		}

		private Animation CalculateAnimationData(Game.Objects.Transform transform, PrefabRef prefabRef, Temp temp, float3 size, float3 pivot)
		{
			Animation result = default(Animation);
			float num = float.MaxValue;
			bool flag = false;
			Game.Objects.Transform componentData2;
			if (m_InterpolatedTransformData.TryGetComponent(temp.m_Original, out var componentData))
			{
				result.m_TargetPosition = componentData.m_Position;
				result.m_Position = componentData.m_Position;
				result.m_Rotation = componentData.m_Rotation;
				num = math.distance(componentData.m_Position, transform.m_Position);
				flag = true;
			}
			else if (m_TransformData.TryGetComponent(temp.m_Original, out componentData2))
			{
				result.m_TargetPosition = componentData2.m_Position;
				result.m_Position = componentData2.m_Position;
				result.m_Rotation = componentData2.m_Rotation;
				num = math.distance(componentData2.m_Position, transform.m_Position);
				flag = true;
			}
			else
			{
				result.m_Position = transform.m_Position;
				result.m_Rotation = transform.m_Rotation;
			}
			if (m_AnimationMap.TryGetFirstValue(prefabRef.m_Prefab, out var item, out var it))
			{
				do
				{
					float num2 = math.distance(item.m_TargetPosition, transform.m_Position);
					if (num2 <= num)
					{
						num = num2;
						result = item;
						flag = true;
					}
				}
				while (m_AnimationMap.TryGetNextValue(out item, ref it));
			}
			if (flag)
			{
				size.y *= 0.5f;
				result.m_PushFactor = (size.y - pivot.y) / math.max(0.001f, m_DeltaTime * size.y * size.y);
			}
			result.m_SwayPivot = pivot;
			return result;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Animation> __Game_Tools_Animation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Animation> __Game_Tools_Animation_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> __Game_Rendering_InterpolatedTransform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_Animation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Animation>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Animation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Animation>();
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Rendering_InterpolatedTransform_RO_ComponentLookup = state.GetComponentLookup<InterpolatedTransform>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
		}
	}

	private EntityQuery m_UpdatedQuery;

	private EntityQuery m_AnimatedQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdatedQuery = GetEntityQuery(ComponentType.ReadWrite<Animation>(), ComponentType.ReadOnly<Game.Objects.Transform>(), ComponentType.ReadOnly<Updated>(), ComponentType.Exclude<Deleted>());
		m_AnimatedQuery = GetEntityQuery(ComponentType.ReadOnly<Animation>(), ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Game.Objects.Transform>());
		RequireForUpdate(m_UpdatedQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeParallelMultiHashMap<Entity, Animation> animationMap = new NativeParallelMultiHashMap<Entity, Animation>(100, Allocator.TempJob);
		AnimationMapJob jobData = new AnimationMapJob
		{
			m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Animation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimationMap = animationMap
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AnimationUpdateJob
		{
			m_DeltaTime = UnityEngine.Time.deltaTime,
			m_AnimationMap = animationMap,
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AnimationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Animation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InterpolatedTransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Rendering_InterpolatedTransform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef)
		}, dependsOn: JobChunkExtensions.Schedule(jobData, m_AnimatedQuery, base.Dependency), query: m_UpdatedQuery);
		animationMap.Dispose(jobHandle);
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
	public AnimationUpdateSystem()
	{
	}
}
