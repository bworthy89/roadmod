using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Common;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ApplyBrushesSystem : GameSystemBase
{
	private interface ICellModifier<TCell> where TCell : struct, ISerializable
	{
		void Apply(ref TCell cell, float strength);
	}

	private struct NaturalResourcesModifier : ICellModifier<NaturalResourceCell>
	{
		private MapFeature m_MapFeature;

		public NaturalResourcesModifier(MapFeature mapFeature)
		{
			m_MapFeature = mapFeature;
		}

		public void Apply(ref NaturalResourceCell cell, float strength)
		{
			switch (m_MapFeature)
			{
			case MapFeature.Ore:
				Apply(ref cell.m_Ore, strength);
				break;
			case MapFeature.Oil:
				Apply(ref cell.m_Oil, strength);
				break;
			case MapFeature.FertileLand:
				Apply(ref cell.m_Fertility, strength);
				break;
			case MapFeature.Forest:
				break;
			}
		}

		private void Apply(ref NaturalResourceAmount cellData, float strength)
		{
			float amount = (float)(int)cellData.m_Base * 0.0001f;
			Apply(ref amount, strength);
			cellData.m_Base = (ushort)math.clamp(Mathf.RoundToInt(amount * 10000f), 0, 10000);
		}

		private void Apply(ref float amount, float strength)
		{
			amount += strength;
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct GroundWaterModifier : ICellModifier<GroundWater>
	{
		public void Apply(ref GroundWater cell, float strength)
		{
			float amount = (float)cell.m_Amount * 0.0001f;
			Apply(ref amount, strength);
			cell.m_Amount = (short)math.clamp(Mathf.RoundToInt(amount * 10000f), 0, 10000);
			cell.m_Max = cell.m_Amount;
		}

		private void Apply(ref float amount, float strength)
		{
			amount += strength;
		}
	}

	[BurstCompile]
	private struct ApplyCellMapBrushJob<TCell, TModifier> : IJobParallelFor where TCell : struct, ISerializable where TModifier : ICellModifier<TCell>
	{
		[ReadOnly]
		public int4 m_Coords;

		[ReadOnly]
		public Brush m_Brush;

		[ReadOnly]
		public Entity m_Prefab;

		[ReadOnly]
		public TerraformingType m_TerraformingType;

		[ReadOnly]
		public TModifier m_CellModifier;

		[ReadOnly]
		public float4 m_TextureSizeAdd;

		[ReadOnly]
		public float2 m_CellSize;

		[ReadOnly]
		public int2 m_TextureSize;

		[ReadOnly]
		public ComponentLookup<BrushData> m_BrushData;

		[ReadOnly]
		public BufferLookup<BrushCell> m_BrushCells;

		[NativeDisableParallelForRestriction]
		public NativeArray<TCell> m_Buffer;

		public void Execute(int index)
		{
			int num = m_Coords.y + index;
			Bounds2 bounds = default(Bounds2);
			bounds.min.y = ((float)num - m_TextureSizeAdd.y) * m_CellSize.y - m_Brush.m_Position.z;
			bounds.max.y = bounds.min.y + m_CellSize.y;
			quaternion q = quaternion.RotateY(m_Brush.m_Angle);
			float2 xz = math.mul(q, new float3(1f, 0f, 0f)).xz;
			float2 xz2 = math.mul(q, new float3(0f, 0f, 1f)).xz;
			BrushData brushData = m_BrushData[m_Prefab];
			DynamicBuffer<BrushCell> dynamicBuffer = m_BrushCells[m_Prefab];
			if (math.any(brushData.m_Resolution == 0) || dynamicBuffer.Length == 0)
			{
				return;
			}
			float2 @float = m_Brush.m_Size / (float2)brushData.m_Resolution;
			float4 xyxy = (1f / @float).xyxy;
			float4 xyxy2 = ((float2)brushData.m_Resolution * 0.5f).xyxy;
			float num2 = m_Brush.m_Strength / (m_CellSize.x * m_CellSize.y);
			for (int i = m_Coords.x; i <= m_Coords.z; i++)
			{
				bounds.min.x = ((float)i - m_TextureSizeAdd.x) * m_CellSize.x - m_Brush.m_Position.x;
				bounds.max.x = bounds.min.x + m_CellSize.x;
				float4 float2 = new float4(bounds.min, bounds.max);
				float4 x = new float4(math.dot(float2.xy, xz), math.dot(float2.xw, xz), math.dot(float2.zy, xz), math.dot(float2.zw, xz));
				float4 x2 = new float4(math.dot(float2.xy, xz2), math.dot(float2.xw, xz2), math.dot(float2.zy, xz2), math.dot(float2.zw, xz2));
				int4 valueToClamp = (int4)math.floor(new float4(math.cmin(x), math.cmin(x2), math.cmax(x), math.cmax(x2)) * xyxy + xyxy2);
				valueToClamp = math.clamp(valueToClamp, 0, brushData.m_Resolution.xyxy - 1);
				float num3 = 0f;
				for (int j = valueToClamp.y; j <= valueToClamp.w; j++)
				{
					float2 float3 = xz2 * (((float)j - xyxy2.y) * @float.y);
					float2 float4 = xz2 * (((float)(j + 1) - xyxy2.y) * @float.y);
					for (int k = valueToClamp.x; k <= valueToClamp.z; k++)
					{
						int index2 = k + brushData.m_Resolution.x * j;
						BrushCell brushCell = dynamicBuffer[index2];
						if (brushCell.m_Opacity >= 0.0001f)
						{
							float2 float5 = xz * (((float)k - xyxy2.x) * @float.x);
							float2 float6 = xz * (((float)(k + 1) - xyxy2.x) * @float.x);
							if (MathUtils.Intersect(quad: new Quad2(float3 + float5, float3 + float6, float4 + float6, float4 + float5), bounds: bounds, area: out var area))
							{
								num3 += brushCell.m_Opacity * area;
							}
						}
					}
				}
				num3 *= num2;
				if (math.abs(num3) >= 0.0001f)
				{
					int index3 = i + m_TextureSize.x * num;
					TCell cell = m_Buffer[index3];
					m_CellModifier.Apply(ref cell, num3);
					m_Buffer[index3] = cell;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Brush> __Game_Tools_Brush_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<BrushData> __Game_Prefabs_BrushData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<BrushCell> __Game_Prefabs_BrushCell_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Tools_Brush_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Brush>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_BrushData_RO_ComponentLookup = state.GetComponentLookup<BrushData>(isReadOnly: true);
			__Game_Prefabs_BrushCell_RO_BufferLookup = state.GetBufferLookup<BrushCell>(isReadOnly: true);
		}
	}

	private ToolOutputBarrier m_ToolOutputBarrier;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private TerrainSystem m_TerrainSystem;

	private TerrainMaterialSystem m_TerrainMaterialSystem;

	private PrefabSystem m_PrefabSystem;

	private EntityQuery m_TempQuery;

	private ComponentTypeSet m_AppliedDeletedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_TerrainMaterialSystem = base.World.GetOrCreateSystemManaged<TerrainMaterialSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_TempQuery = GetEntityQuery(ComponentType.ReadOnly<Temp>(), ComponentType.ReadOnly<Brush>());
		m_AppliedDeletedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Deleted>());
		RequireForUpdate(m_TempQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<Brush> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Brush_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<PrefabRef> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		EntityCommandBuffer entityCommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer();
		JobHandle jobHandle = default(JobHandle);
		NativeArray<ArchetypeChunk> nativeArray = m_TempQuery.ToArchetypeChunkArray(Allocator.TempJob);
		try
		{
			CompleteDependency();
			for (int i = 0; i < nativeArray.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray[i];
				NativeArray<Entity> nativeArray2 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<Brush> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle);
				NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle2);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity e = nativeArray2[j];
					Brush brush = nativeArray3[j];
					PrefabRef prefabRef = nativeArray4[j];
					if (base.EntityManager.TryGetComponent<TerraformingData>(brush.m_Tool, out var component))
					{
						switch (component.m_Target)
						{
						case TerraformingTarget.Ore:
							jobHandle = JobHandle.CombineDependencies(jobHandle, ApplyCellMapBrush(m_NaturalResourceSystem, new NaturalResourcesModifier(MapFeature.Ore), brush, prefabRef.m_Prefab, component.m_Type, default(ApplyCellMapBrushJob<NaturalResourceCell, NaturalResourcesModifier>)));
							break;
						case TerraformingTarget.Oil:
							jobHandle = JobHandle.CombineDependencies(jobHandle, ApplyCellMapBrush(m_NaturalResourceSystem, new NaturalResourcesModifier(MapFeature.Oil), brush, prefabRef.m_Prefab, component.m_Type, default(ApplyCellMapBrushJob<NaturalResourceCell, NaturalResourcesModifier>)));
							break;
						case TerraformingTarget.FertileLand:
							jobHandle = JobHandle.CombineDependencies(jobHandle, ApplyCellMapBrush(m_NaturalResourceSystem, new NaturalResourcesModifier(MapFeature.FertileLand), brush, prefabRef.m_Prefab, component.m_Type, default(ApplyCellMapBrushJob<NaturalResourceCell, NaturalResourcesModifier>)));
							break;
						case TerraformingTarget.GroundWater:
							jobHandle = JobHandle.CombineDependencies(jobHandle, ApplyCellMapBrush(m_GroundWaterSystem, default(GroundWaterModifier), brush, prefabRef.m_Prefab, component.m_Type, default(ApplyCellMapBrushJob<GroundWater, GroundWaterModifier>)));
							break;
						case TerraformingTarget.Height:
							ApplyHeight(brush, prefabRef.m_Prefab, component.m_Type);
							break;
						case TerraformingTarget.Material:
							ApplyMaterial(brush, prefabRef.m_Prefab);
							break;
						}
					}
					entityCommandBuffer.AddComponent(e, in m_AppliedDeletedTypes);
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
			base.Dependency = jobHandle;
		}
	}

	private void ApplyMaterial(Brush brush, Entity prefab)
	{
		m_TerrainMaterialSystem.GetOrAddMaterialIndex(brush.m_Tool);
	}

	private void ApplyHeight(Brush brush, Entity prefab, TerraformingType terraformingType)
	{
		Bounds2 bounds = ToolUtils.GetBounds(brush);
		BrushPrefab prefab2 = m_PrefabSystem.GetPrefab<BrushPrefab>(prefab);
		if ((terraformingType != TerraformingType.Level && terraformingType != TerraformingType.Slope) || !(brush.m_Strength < 0f))
		{
			if (terraformingType == TerraformingType.Soften && brush.m_Strength < 0f)
			{
				brush.m_Strength = math.abs(brush.m_Strength) * 2f;
			}
			m_TerrainSystem.ApplyBrush(terraformingType, bounds, brush, prefab2.m_Texture);
		}
	}

	private JobHandle ApplyCellMapBrush<TCell, TModifier>(CellMapSystem<TCell> cellMapSystem, TModifier modifier, Brush brush, Entity prefab, TerraformingType terraformingType, ApplyCellMapBrushJob<TCell, TModifier> applyCellMapBrushJob) where TCell : struct, ISerializable where TModifier : ICellModifier<TCell>
	{
		Bounds2 bounds = ToolUtils.GetBounds(brush);
		JobHandle dependencies;
		CellMapData<TCell> data = cellMapSystem.GetData(readOnly: false, out dependencies);
		float4 xyxy = (1f / data.m_CellSize).xyxy;
		float4 xyxy2 = ((float2)data.m_TextureSize * 0.5f).xyxy;
		int4 valueToClamp = (int4)math.floor(new float4(bounds.min, bounds.max) * xyxy + xyxy2);
		valueToClamp = math.clamp(valueToClamp, 0, data.m_TextureSize.xyxy - 1);
		applyCellMapBrushJob = new ApplyCellMapBrushJob<TCell, TModifier>
		{
			m_Coords = valueToClamp,
			m_Brush = brush,
			m_Prefab = prefab,
			m_TerraformingType = terraformingType,
			m_CellModifier = modifier,
			m_TextureSizeAdd = xyxy2,
			m_CellSize = data.m_CellSize,
			m_TextureSize = data.m_TextureSize,
			m_BrushData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BrushData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BrushCells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BrushCell_RO_BufferLookup, ref base.CheckedStateRef),
			m_Buffer = data.m_Buffer
		};
		JobHandle jobHandle = IJobParallelForExtensions.Schedule(applyCellMapBrushJob, valueToClamp.w - valueToClamp.y + 1, 1, dependencies);
		cellMapSystem.AddWriter(jobHandle);
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
	public ApplyBrushesSystem()
	{
	}
}
