using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class FindOwnersSystem : GameSystemBase
{
	[BurstCompile]
	public struct SetSubEntityOwnerJob : IJobChunk
	{
		[ReadOnly]
		public NativeList<ArchetypeChunk> m_OwnerChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Curve> m_CurveType;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> m_OwnerDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		public ComponentTypeHandle<Owner> m_OwnerType;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<OwnerDefinition> nativeArray2 = chunk.GetNativeArray(ref m_OwnerDefinitionType);
			NativeArray<Owner> nativeArray3 = chunk.GetNativeArray(ref m_OwnerType);
			bool flag = chunk.Has(ref m_TempType);
			m_CommandBuffer.RemoveComponent<OwnerDefinition>(unfilteredChunkIndex, nativeArray);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				OwnerDefinition ownerDefinition = nativeArray2[i];
				Owner value = nativeArray3[i];
				float num = float.MaxValue;
				bool flag2 = value.m_Owner != Entity.Null;
				for (int j = 0; j < m_OwnerChunks.Length; j++)
				{
					ArchetypeChunk archetypeChunk = m_OwnerChunks[j];
					if (archetypeChunk.Has(ref m_TempType) != flag)
					{
						continue;
					}
					NativeArray<Entity> nativeArray4 = archetypeChunk.GetNativeArray(m_EntityType);
					NativeArray<PrefabRef> nativeArray5 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					NativeArray<Transform> nativeArray6 = archetypeChunk.GetNativeArray(ref m_TransformType);
					NativeArray<Curve> nativeArray7 = archetypeChunk.GetNativeArray(ref m_CurveType);
					int k;
					if (nativeArray6.Length != 0)
					{
						for (k = 0; k < nativeArray5.Length; k++)
						{
							if (!nativeArray5[k].m_Prefab.Equals(ownerDefinition.m_Prefab))
							{
								continue;
							}
							Transform transform = nativeArray6[k];
							if (!transform.m_Position.Equals(ownerDefinition.m_Position) || !transform.m_Rotation.Equals(ownerDefinition.m_Rotation))
							{
								continue;
							}
							goto IL_0145;
						}
					}
					else
					{
						if (nativeArray7.Length == 0)
						{
							continue;
						}
						for (int l = 0; l < nativeArray5.Length; l++)
						{
							if (!nativeArray5[l].m_Prefab.Equals(ownerDefinition.m_Prefab))
							{
								continue;
							}
							Curve curve = nativeArray7[l];
							float3 @float = ownerDefinition.m_Position - curve.m_Bezier.a;
							float3 float2 = ownerDefinition.m_Rotation.value.xyz - curve.m_Bezier.d;
							if (flag2)
							{
								if (math.any(@float != 0f) || math.any(float2 != 0f))
								{
									continue;
								}
							}
							else
							{
								@float.y *= 0.001f;
								float2.y *= 0.001f;
								float num2 = math.lengthsq(@float) + math.lengthsq(float2);
								if (num2 >= num)
								{
									continue;
								}
								num = num2;
							}
							value.m_Owner = nativeArray4[l];
						}
					}
					continue;
					IL_0145:
					value.m_Owner = nativeArray4[k];
					break;
				}
				nativeArray3[i] = value;
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Curve> __Game_Net_Curve_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<OwnerDefinition> __Game_Tools_OwnerDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Owner> __Game_Common_Owner_RW_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Curve>(isReadOnly: true);
			__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<OwnerDefinition>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Common_Owner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>();
		}
	}

	private ModificationBarrier3 m_ModificationBarrier;

	private EntityQuery m_OwnersQuery;

	private EntityQuery m_SubEntityQuery;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_OwnersQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Net.SubNet>(),
				ComponentType.ReadOnly<Game.Areas.SubArea>(),
				ComponentType.ReadOnly<Game.Objects.SubObject>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_SubEntityQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<OwnerDefinition>(),
				ComponentType.ReadOnly<Owner>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadOnly<Game.Net.Node>(),
				ComponentType.ReadOnly<Edge>(),
				ComponentType.ReadOnly<Area>(),
				ComponentType.ReadOnly<Object>()
			}
		});
		RequireForUpdate(m_SubEntityQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> ownerChunks = m_OwnersQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new SetSubEntityOwnerJob
		{
			m_OwnerChunks = ownerChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurveType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Net_Curve_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_OwnerDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_SubEntityQuery, JobHandle.CombineDependencies(outJobHandle, base.Dependency));
		ownerChunks.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
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
	public FindOwnersSystem()
	{
	}
}
