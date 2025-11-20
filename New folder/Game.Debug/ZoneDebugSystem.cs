using System.Runtime.CompilerServices;
using Colossal;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.Zones;
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
public class ZoneDebugSystem : BaseDebugSystem
{
	[BurstCompile]
	private struct BlockGizmoJob : IJobChunk
	{
		[ReadOnly]
		public bool m_PivotOption;

		[ReadOnly]
		public bool m_GridOption;

		[ReadOnly]
		public bool m_LotOption;

		[ReadOnly]
		public ZonePrefabs m_ZonePrefabs;

		[ReadOnly]
		public ComponentTypeHandle<Block> m_BlockType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public BufferTypeHandle<VacantLot> m_VacantLotType;

		[ReadOnly]
		public ComponentTypeHandle<Error> m_ErrorType;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_PrefabZoneData;

		public GizmoBatcher m_GizmoBatcher;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Block> nativeArray = chunk.GetNativeArray(ref m_BlockType);
			BufferAccessor<VacantLot> bufferAccessor = chunk.GetBufferAccessor(ref m_VacantLotType);
			Color color;
			Color color2;
			if (chunk.Has(ref m_ErrorType))
			{
				color = Color.red;
				color2 = Color.red;
			}
			else if (chunk.Has(ref m_TempType))
			{
				color = Color.blue;
				color2 = Color.blue;
			}
			else
			{
				color = Color.cyan;
				color2 = Color.white;
			}
			Color color3 = Color.gray * 0.5f;
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Block block = nativeArray[i];
				if (m_PivotOption)
				{
					m_GizmoBatcher.DrawWireNode(block.m_Position, 4f, color);
				}
				if (m_GridOption)
				{
					float3 @float = new float3(0f - block.m_Direction.y, 0f, block.m_Direction.x);
					float3 float2 = new float3(block.m_Direction.x, 0f, block.m_Direction.y);
					float3 float3 = block.m_Position - @float * (4f * (float)block.m_Size.x) - float2 * (4f * (float)block.m_Size.y);
					float3 float4 = @float * (8f * (float)block.m_Size.x);
					float3 float5 = float2 * (8f * (float)block.m_Size.y);
					for (int j = 0; j <= block.m_Size.x; j++)
					{
						float3 float6 = float3 + @float * ((float)j * 8f);
						Color color4 = ((j == 0 || j == block.m_Size.x) ? color2 : color3);
						m_GizmoBatcher.DrawLine(float6, float6 + float5, color4);
					}
					for (int k = 0; k <= block.m_Size.y; k++)
					{
						float3 float7 = float3 + float2 * ((float)k * 8f);
						Color color5 = ((k == 0 || k == block.m_Size.y) ? color2 : color3);
						m_GizmoBatcher.DrawLine(float7, float7 + float4, color5);
					}
				}
			}
			if (!m_LotOption)
			{
				return;
			}
			for (int l = 0; l < bufferAccessor.Length; l++)
			{
				Block block2 = nativeArray[l];
				DynamicBuffer<VacantLot> dynamicBuffer = bufferAccessor[l];
				float3 float8 = new float3(0f - block2.m_Direction.y, 0f, block2.m_Direction.x);
				float3 float9 = new float3(0f - block2.m_Direction.x, 0f, 0f - block2.m_Direction.y);
				float4x4 b = float4x4.LookAt(default(float3), float9, math.up());
				for (int m = 0; m < dynamicBuffer.Length; m++)
				{
					VacantLot vacantLot = dynamicBuffer[m];
					float2 float10 = (float2)(vacantLot.m_Area.xz + vacantLot.m_Area.yw - block2.m_Size) * 4f;
					float3 float11 = block2.m_Position + float8 * float10.x + float9 * float10.y;
					float2 float12 = (float2)(vacantLot.m_Area.yw - vacantLot.m_Area.xz) * 8f;
					float4x4 trs = math.mul(float4x4.Translate(float11), b);
					ZoneData zoneData = m_PrefabZoneData[m_ZonePrefabs[vacantLot.m_Type]];
					Color zoneColor = GetZoneColor(zoneData);
					float num = math.min((int)zoneData.m_MaxHeight, (float)vacantLot.m_Height - block2.m_Position.y);
					m_GizmoBatcher.DrawWireCube(trs, new float3(0f, num * 0.5f, 0f), new float3(float12.x, num, float12.y), zoneColor);
					float11.y += num;
					if ((vacantLot.m_Flags & LotFlags.CornerLeft) != 0)
					{
						float3 a = float11 - float8 * (float12.x * 0.5f) - float9 * (float12.y * 0.5f);
						float3 b2 = float11 - float8 * (float12.x * 0.5f - 2f) - float9 * (float12.y * 0.5f - 2f);
						m_GizmoBatcher.DrawLine(a, b2, zoneColor);
					}
					if ((vacantLot.m_Flags & LotFlags.CornerRight) != 0)
					{
						float3 a2 = float11 + float8 * (float12.x * 0.5f) - float9 * (float12.y * 0.5f);
						float3 b3 = float11 + float8 * (float12.x * 0.5f - 2f) - float9 * (float12.y * 0.5f - 2f);
						m_GizmoBatcher.DrawLine(a2, b3, zoneColor);
					}
					if ((vacantLot.m_Flags & (LotFlags.CornerLeft | LotFlags.CornerRight)) == 0)
					{
						float3 a3 = float11 - float9 * (float12.y * 0.5f);
						float3 b4 = float11 - float9 * (float12.y * 0.5f - 2f);
						m_GizmoBatcher.DrawLine(a3, b4, zoneColor);
					}
				}
			}
		}

		private Color GetZoneColor(ZoneData zoneData)
		{
			return zoneData.m_AreaType switch
			{
				AreaType.Residential => Color.green, 
				AreaType.Commercial => Color.cyan, 
				AreaType.Industrial => Color.yellow, 
				_ => Color.white, 
			};
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<Block> __Game_Zones_Block_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<VacantLot> __Game_Zones_VacantLot_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Error> __Game_Tools_Error_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Zones_Block_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Block>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Zones_VacantLot_RO_BufferTypeHandle = state.GetBufferTypeHandle<VacantLot>(isReadOnly: true);
			__Game_Tools_Error_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Error>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
		}
	}

	private EntityQuery m_BlockGroup;

	private GizmosSystem m_GizmosSystem;

	private ZoneSystem m_ZoneSystem;

	private Option m_PivotOption;

	private Option m_GridOption;

	private Option m_LotOption;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_GizmosSystem = base.World.GetOrCreateSystemManaged<GizmosSystem>();
		m_ZoneSystem = base.World.GetOrCreateSystemManaged<ZoneSystem>();
		m_BlockGroup = GetEntityQuery(ComponentType.ReadOnly<Block>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Hidden>());
		m_PivotOption = AddOption("Draw Pivots", defaultEnabled: false);
		m_GridOption = AddOption("Draw Grids", defaultEnabled: true);
		m_LotOption = AddOption("Vacant Lots", defaultEnabled: true);
		RequireForUpdate(m_BlockGroup);
		base.Enabled = false;
	}

	[Preserve]
	protected override JobHandle OnUpdate(JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new BlockGizmoJob
		{
			m_PivotOption = m_PivotOption.enabled,
			m_GridOption = m_GridOption.enabled,
			m_LotOption = m_LotOption.enabled,
			m_ZonePrefabs = m_ZoneSystem.GetPrefabs(),
			m_BlockType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Zones_Block_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_VacantLotType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Zones_VacantLot_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ErrorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Error_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GizmoBatcher = m_GizmosSystem.GetGizmosBatcher(out dependencies)
		}, m_BlockGroup, JobHandle.CombineDependencies(inputDeps, dependencies));
		m_ZoneSystem.AddPrefabsReader(jobHandle);
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
	public ZoneDebugSystem()
	{
	}
}
