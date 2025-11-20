using System.Runtime.CompilerServices;
using Colossal;
using Colossal.Mathematics;
using Game.Areas;
using Game.Common;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Debug;

[CompilerGenerated]
public class AreaDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct AreaGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_LotOption;

		[ReadOnly]
		public bool m_DistrictOption;

		[ReadOnly]
		public bool m_MapTileOption;

		[ReadOnly]
		public bool m_SpaceOption;

		[ReadOnly]
		public bool m_SurfaceOption;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Lot> m_LotType;

		[ReadOnly]
		public ComponentTypeHandle<District> m_DistrictType;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> m_MapTileType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Areas.Space> m_SpaceType;

		[ReadOnly]
		public ComponentTypeHandle<Surface> m_SurfaceType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Color color;
			float num;
			if (chunk.Has(ref m_LotType))
			{
				if (!m_LotOption)
				{
					return;
				}
				color = Color.cyan;
				num = AreaUtils.GetMinNodeDistance(AreaType.Lot) * 0.5f;
			}
			else if (chunk.Has(ref m_DistrictType))
			{
				if (!m_DistrictOption)
				{
					return;
				}
				color = Color.white;
				num = AreaUtils.GetMinNodeDistance(AreaType.District) * 0.5f;
			}
			else if (chunk.Has(ref m_MapTileType))
			{
				if (!m_MapTileOption)
				{
					return;
				}
				color = Color.yellow;
				num = AreaUtils.GetMinNodeDistance(AreaType.MapTile) * 0.5f;
			}
			else if (chunk.Has(ref m_SpaceType))
			{
				if (!m_SpaceOption)
				{
					return;
				}
				color = Color.green;
				num = AreaUtils.GetMinNodeDistance(AreaType.Space) * 0.5f;
			}
			else if (chunk.Has(ref m_SurfaceType))
			{
				if (!m_SurfaceOption)
				{
					return;
				}
				color = Color.magenta;
				num = AreaUtils.GetMinNodeDistance(AreaType.Surface) * 0.5f;
			}
			else
			{
				color = Color.black;
				num = AreaUtils.GetMinNodeDistance(AreaType.None) * 0.5f;
			}
			NativeArray<Area> nativeArray = chunk.GetNativeArray(ref m_AreaType);
			BufferAccessor<Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
			BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
			if (chunk.Has(ref m_ErrorType))
			{
				color = Color.red;
			}
			else if (chunk.Has(ref m_TempType))
			{
				color = Color.blue;
			}
			Color color2 = Color.gray * 0.5f;
			float newLength = num * 0.2f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Area area = nativeArray[i];
				DynamicBuffer<Node> dynamicBuffer = bufferAccessor[i];
				DynamicBuffer<Triangle> dynamicBuffer2 = bufferAccessor2[i];
				float3 @float = dynamicBuffer[0].m_Position;
				if (dynamicBuffer[0].m_Elevation == float.MinValue)
				{
					m_GizmoBatcher.DrawWireSphere(@float, num, color);
				}
				else
				{
					m_GizmoBatcher.DrawWireNode(@float, num, color);
				}
				for (int j = 1; j < dynamicBuffer.Length; j++)
				{
					float3 position = dynamicBuffer[j].m_Position;
					m_GizmoBatcher.DrawLine(@float, position, color);
					if (dynamicBuffer[j].m_Elevation == float.MinValue)
					{
						m_GizmoBatcher.DrawWireSphere(position, num, color);
					}
					else
					{
						m_GizmoBatcher.DrawWireNode(position, num, color);
					}
					@float = position;
				}
				if ((area.m_Flags & AreaFlags.Complete) != 0)
				{
					float3 position2 = dynamicBuffer[0].m_Position;
					m_GizmoBatcher.DrawLine(@float, position2, color);
				}
				for (int k = 0; k < dynamicBuffer2.Length; k++)
				{
					Triangle triangle = dynamicBuffer2[k];
					float3 position3 = dynamicBuffer[triangle.m_Indices.x].m_Position;
					float3 position4 = dynamicBuffer[triangle.m_Indices.y].m_Position;
					float3 position5 = dynamicBuffer[triangle.m_Indices.z].m_Position;
					float3 value = position4 - position3;
					float3 value2 = position5 - position4;
					float3 value3 = position3 - position5;
					MathUtils.TryNormalize(ref value, newLength);
					MathUtils.TryNormalize(ref value2, newLength);
					MathUtils.TryNormalize(ref value3, newLength);
					position3 += value - value3;
					position4 += value2 - value;
					position5 += value3 - value2;
					m_GizmoBatcher.DrawLine(position3, position4, color2);
					m_GizmoBatcher.DrawLine(position4, position5, color2);
					m_GizmoBatcher.DrawLine(position5, position3, color2);
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
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Lot> __Game_Areas_Lot_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<District> __Game_Areas_District_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MapTile> __Game_Areas_MapTile_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Areas.Space> __Game_Areas_Space_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Surface> __Game_Areas_Surface_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Lot>(isReadOnly: true);
			__Game_Areas_District_RO_ComponentTypeHandle = state.GetComponentTypeHandle<District>(isReadOnly: true);
			__Game_Areas_MapTile_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MapTile>(isReadOnly: true);
			__Game_Areas_Space_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Areas.Space>(isReadOnly: true);
			__Game_Areas_Surface_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Surface>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
		}
	}

	private EntityQuery m_AreaGroup;

	private GizmosSystem m_GizmosSystem;

	private Option m_LotOption;

	private Option m_DistrictOption;

	private Option m_MapTileOption;

	private Option m_SpaceOption;

	private Option m_SurfaceOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_AreaGroup = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Node>(), ComponentType.ReadOnly<Triangle>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Hidden>());
		m_LotOption = AddOption("Lots", defaultEnabled: true);
		m_DistrictOption = AddOption("Districts", defaultEnabled: true);
		m_MapTileOption = AddOption("Map Tiles", defaultEnabled: false);
		m_SpaceOption = AddOption("Spaces", defaultEnabled: true);
		m_SurfaceOption = AddOption("Surfaces", defaultEnabled: true);
		RequireForUpdate(m_AreaGroup);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AreaGizmoJob
		{
			m_LotOption = m_LotOption.enabled,
			m_DistrictOption = m_DistrictOption.enabled,
			m_MapTileOption = m_MapTileOption.enabled,
			m_SpaceOption = m_SpaceOption.enabled,
			m_SurfaceOption = m_SurfaceOption.enabled,
			m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_LotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_District_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MapTileType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_MapTile_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpaceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Space_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SurfaceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Surface_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, m_AreaGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_GizmosSystem.AddGizmosBatcherWriter(jobHandle);
		return jobHandle;
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
	public AreaDebugSystem()
	{
	}
}
