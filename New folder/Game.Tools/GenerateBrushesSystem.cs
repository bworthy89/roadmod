using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class GenerateBrushesSystem : GameSystemBase
{
	[BurstCompile]
	private struct GenerateBrushesJob : IJobChunk
	{
		[ReadOnly]
		public ComponentTypeHandle<CreationDefinition> m_CreationDefinitionType;

		[ReadOnly]
		public ComponentTypeHandle<BrushDefinition> m_BrushDefinitionType;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<BrushData> m_PrefabBrushData;

		[ReadOnly]
		public ComponentLookup<TerraformingData> m_PrefabTerraformingData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CreationDefinition> nativeArray = chunk.GetNativeArray(ref m_CreationDefinitionType);
			NativeArray<BrushDefinition> nativeArray2 = chunk.GetNativeArray(ref m_BrushDefinitionType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				CreationDefinition creationDefinition = nativeArray[i];
				BrushDefinition brushDefinition = nativeArray2[i];
				PrefabRef component = new PrefabRef
				{
					m_Prefab = creationDefinition.m_Prefab
				};
				if (creationDefinition.m_Original != Entity.Null)
				{
					m_CommandBuffer.AddComponent(unfilteredChunkIndex, creationDefinition.m_Original, default(Hidden));
					component.m_Prefab = m_PrefabRefData[creationDefinition.m_Original].m_Prefab;
				}
				BrushData brushData = m_PrefabBrushData[component.m_Prefab];
				Temp component2 = new Temp
				{
					m_Original = creationDefinition.m_Original
				};
				component2.m_Flags |= TempFlags.Essential;
				if ((creationDefinition.m_Flags & CreationFlags.Delete) != 0)
				{
					component2.m_Flags |= TempFlags.Delete;
				}
				else if ((creationDefinition.m_Flags & CreationFlags.Select) != 0)
				{
					component2.m_Flags |= TempFlags.Select;
				}
				else
				{
					component2.m_Flags |= TempFlags.Create;
				}
				if (!m_PrefabTerraformingData.TryGetComponent(brushDefinition.m_Tool, out var componentData) || componentData.m_Target == TerraformingTarget.None)
				{
					component2.m_Flags |= TempFlags.Cancel;
				}
				float num = MathUtils.Length(brushDefinition.m_Line);
				float num2 = brushDefinition.m_Strength * brushDefinition.m_Strength;
				int num3 = 1 + Mathf.FloorToInt(num / (brushDefinition.m_Size * 0.25f));
				float num4 = brushDefinition.m_Time / (float)num3;
				Brush component3 = new Brush
				{
					m_Tool = brushDefinition.m_Tool,
					m_Angle = brushDefinition.m_Angle,
					m_Size = brushDefinition.m_Size,
					m_Strength = (num2 * num2 * (1f - num4) + num2 * num4) * math.sign(brushDefinition.m_Strength),
					m_Opacity = 1f / (float)num3,
					m_Target = brushDefinition.m_Target,
					m_Start = brushDefinition.m_Start
				};
				for (int j = 1; j <= num3; j++)
				{
					component3.m_Position = MathUtils.Position(brushDefinition.m_Line, (float)j / (float)num3);
					Entity e = m_CommandBuffer.CreateEntity(unfilteredChunkIndex, brushData.m_Archetype);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, component);
					m_CommandBuffer.SetComponent(unfilteredChunkIndex, e, component3);
					if ((creationDefinition.m_Flags & CreationFlags.Permanent) == 0)
					{
						m_CommandBuffer.AddComponent(unfilteredChunkIndex, e, component2);
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
		public ComponentTypeHandle<CreationDefinition> __Game_Tools_CreationDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BrushDefinition> __Game_Tools_BrushDefinition_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BrushData> __Game_Prefabs_BrushData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TerraformingData> __Game_Prefabs_TerraformingData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Tools_CreationDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CreationDefinition>(isReadOnly: true);
			__Game_Tools_BrushDefinition_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BrushDefinition>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BrushData_RO_ComponentLookup = state.GetComponentLookup<BrushData>(isReadOnly: true);
			__Game_Prefabs_TerraformingData_RO_ComponentLookup = state.GetComponentLookup<TerraformingData>(isReadOnly: true);
		}
	}

	private ModificationBarrier1 m_ModificationBarrier;

	private EntityQuery m_DefinitionQuery;

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
			Any = new ComponentType[1] { ComponentType.ReadOnly<BrushDefinition>() }
		});
		RequireForUpdate(m_DefinitionQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		GenerateBrushesJob jobData = new GenerateBrushesJob
		{
			m_CreationDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_CreationDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BrushDefinitionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_BrushDefinition_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBrushData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BrushData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTerraformingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TerraformingData_RO_ComponentLookup, ref base.CheckedStateRef),
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
	public GenerateBrushesSystem()
	{
	}
}
