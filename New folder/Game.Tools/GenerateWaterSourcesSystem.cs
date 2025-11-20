using System.Runtime.CompilerServices;
using Game.Common;
using Game.Objects;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateWaterSourcesSystem : GameSystemBase
{
	[BurstCompile]
	private struct GenerateBrushesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<WaterSourceDefinition> m_WaterSourceDefinitionType;

		[ReadOnly]
		public EntityArchetype m_WaterSourceArchetype;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<WaterSourceDefinition> nativeArray2 = chunk.GetNativeArray(ref m_WaterSourceDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				WaterSourceDefinition waterSourceDefinition = nativeArray2[i];
				Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, m_WaterSourceArchetype);
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new WaterSourceData
				{
					SourceNameId = waterSourceDefinition.m_SourceNameId,
					m_ConstantDepth = waterSourceDefinition.m_ConstantDepth,
					m_Radius = waterSourceDefinition.m_Radius,
					m_Polluted = waterSourceDefinition.m_Polluted,
					m_Height = waterSourceDefinition.m_Height,
					m_id = waterSourceDefinition.m_SourceId,
					m_modifier = 1f
				});
				m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, new Transform
				{
					m_Position = new float3(waterSourceDefinition.m_Position.x, waterSourceDefinition.m_Position.y, waterSourceDefinition.m_Position.z),
					m_Rotation = quaternion.identity
				});
				if ((creationDefinition.m_Flags & CreationFlags.Permanent) != 0)
				{
					continue;
				}
				Temp component = new Temp
				{
					m_Original = creationDefinition.m_Original
				};
				if ((creationDefinition.m_Flags & CreationFlags.Select) != 0)
				{
					component.m_Flags = TempFlags.Select;
					if ((creationDefinition.m_Flags & CreationFlags.Dragging) != 0)
					{
						component.m_Flags |= TempFlags.Dragging;
					}
				}
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component);
				m_CommandBuffer.AddComponent(unfilteredChunkIndex, creationDefinition.m_Original, default(Hidden));
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
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterSourceDefinition> __Game_Tools_WaterSourceDefinition_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_WaterSourceDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterSourceDefinition>(isReadOnly: true);
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

	private EntityArchetype m_WaterSourceArchetype;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_DefinitionQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<CreationDefinition>(),
				ComponentType.ReadOnly<Updated>()
			},
			Any = new ComponentType[1] { ComponentType.ReadOnly<WaterSourceDefinition>() }
		});
		m_WaterSourceArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<WaterSourceData>(), ComponentType.ReadWrite<Transform>(), ComponentType.ReadWrite<Temp>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		GenerateBrushesJob jobData = new GenerateBrushesJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterSourceDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_WaterSourceDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterSourceArchetype = m_WaterSourceArchetype,
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer().AsParallelWriter()
		};
		base.Dependency = JobChunkExtensions.ScheduleParallel(jobData, m_DefinitionQuery, base.Dependency);
		m_ModificationBarrier.AddJobHandleForProducer(base.Dependency);
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
	public GenerateWaterSourcesSystem()
	{
	}
}
