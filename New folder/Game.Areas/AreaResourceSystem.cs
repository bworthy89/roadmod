using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Areas;

[CompilerGenerated]
public class AreaResourceSystem : GameSystemBase, IPostDeserialize
{
	[BurstCompile]
	private struct FindUpdatedAreasWithBrushesJob : IJobParallelForDefer
	{
		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public BufferLookup<WoodResource> m_WoodResourceData;

			public BufferLookup<MapFeatureElement> m_MapFeatureElements;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && (m_WoodResourceData.HasBuffer(item.m_Area) || m_MapFeatureElements.HasBuffer(item.m_Area)))
				{
					Triangle2 triangle = AreaUtils.GetTriangle2(m_Nodes[item.m_Area], m_Triangles[item.m_Area][item.m_Triangle]);
					if (MathUtils.Intersect(m_Bounds, triangle))
					{
						m_UpdateBuffer.Enqueue(item.m_Area);
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<Brush> m_Brushes;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public BufferLookup<WoodResource> m_WoodResourceData;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

		public void Execute(int index)
		{
			AreaIterator iterator = new AreaIterator
			{
				m_Bounds = ToolUtils.GetBounds(m_Brushes[index]),
				m_WoodResourceData = m_WoodResourceData,
				m_MapFeatureElements = m_MapFeatureElements,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles,
				m_UpdateBuffer = m_UpdateBuffer
			};
			m_AreaTree.Iterate(ref iterator);
			m_WoodResourceData = iterator.m_WoodResourceData;
			m_MapFeatureElements = iterator.m_MapFeatureElements;
			m_Nodes = iterator.m_Nodes;
			m_Triangles = iterator.m_Triangles;
		}
	}

	[BurstCompile]
	private struct FindUpdatedAreasWithBoundsJob : IJobParallelForDefer
	{
		private struct AreaIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public Bounds2 m_Bounds;

			public BufferLookup<WoodResource> m_WoodResourceData;

			public BufferLookup<MapFeatureElement> m_MapFeatureElements;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem item)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) && (m_WoodResourceData.HasBuffer(item.m_Area) || m_MapFeatureElements.HasBuffer(item.m_Area)))
				{
					Triangle2 triangle = AreaUtils.GetTriangle2(m_Nodes[item.m_Area], m_Triangles[item.m_Area][item.m_Triangle]);
					if (MathUtils.Intersect(m_Bounds, triangle))
					{
						m_UpdateBuffer.Enqueue(item.m_Area);
					}
				}
			}
		}

		[ReadOnly]
		public NativeArray<Bounds2> m_Bounds;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_AreaTree;

		[ReadOnly]
		public BufferLookup<WoodResource> m_WoodResourceData;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		public NativeQueue<Entity>.ParallelWriter m_UpdateBuffer;

		public void Execute(int index)
		{
			AreaIterator iterator = new AreaIterator
			{
				m_Bounds = m_Bounds[index],
				m_WoodResourceData = m_WoodResourceData,
				m_MapFeatureElements = m_MapFeatureElements,
				m_Nodes = m_Nodes,
				m_Triangles = m_Triangles,
				m_UpdateBuffer = m_UpdateBuffer
			};
			m_AreaTree.Iterate(ref iterator);
			m_WoodResourceData = iterator.m_WoodResourceData;
			m_MapFeatureElements = iterator.m_MapFeatureElements;
			m_Nodes = iterator.m_Nodes;
			m_Triangles = iterator.m_Triangles;
		}
	}

	[BurstCompile]
	private struct CollectUpdatedAreasJob : IJob
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct EntityComparer : IComparer<Entity>
		{
			public int Compare(Entity x, Entity y)
			{
				return x.Index - y.Index;
			}
		}

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_UpdatedAreaChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_MapTileChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		public NativeQueue<Entity> m_UpdateBuffer;

		public NativeList<Entity> m_UpdateList;

		public NativeArray<float2> m_LastCityModifiers;

		public Entity m_City;

		public void Execute()
		{
			int count = m_UpdateBuffer.Count;
			int num = 0;
			for (int i = 0; i < m_UpdatedAreaChunks.Length; i++)
			{
				num += m_UpdatedAreaChunks[i].Count;
			}
			bool flag = UpdateResourceModifiers();
			if (flag)
			{
				for (int j = 0; j < m_MapTileChunks.Length; j++)
				{
					num += m_MapTileChunks[j].Count;
				}
			}
			m_UpdateList.ResizeUninitialized(count + num);
			for (int k = 0; k < count; k++)
			{
				m_UpdateList[k] = m_UpdateBuffer.Dequeue();
			}
			for (int l = 0; l < m_UpdatedAreaChunks.Length; l++)
			{
				NativeArray<Entity> nativeArray = m_UpdatedAreaChunks[l].GetNativeArray(m_EntityType);
				for (int m = 0; m < nativeArray.Length; m++)
				{
					m_UpdateList[count++] = nativeArray[m];
				}
			}
			if (flag)
			{
				for (int n = 0; n < m_MapTileChunks.Length; n++)
				{
					NativeArray<Entity> nativeArray2 = m_MapTileChunks[n].GetNativeArray(m_EntityType);
					for (int num2 = 0; num2 < nativeArray2.Length; num2++)
					{
						m_UpdateList[count++] = nativeArray2[num2];
					}
				}
			}
			m_UpdateList.Sort(default(EntityComparer));
			Entity entity = Entity.Null;
			int num3 = 0;
			int num4 = 0;
			while (num3 < m_UpdateList.Length)
			{
				Entity entity2 = m_UpdateList[num3++];
				if (entity2 != entity)
				{
					m_UpdateList[num4++] = entity2;
					entity = entity2;
				}
			}
			if (num4 < m_UpdateList.Length)
			{
				m_UpdateList.RemoveRange(num4, m_UpdateList.Length - num4);
			}
		}

		private bool UpdateResourceModifiers()
		{
			float2 @float;
			float2 float2;
			if (m_City != Entity.Null)
			{
				DynamicBuffer<CityModifier> modifiers = m_CityModifiers[m_City];
				@float = CityUtils.GetModifier(modifiers, CityModifierType.OreResourceAmount);
				float2 = CityUtils.GetModifier(modifiers, CityModifierType.OilResourceAmount);
			}
			else
			{
				float2 = default(float2);
				@float = float2;
			}
			bool result = !m_LastCityModifiers[0].Equals(@float) || !m_LastCityModifiers[1].Equals(float2);
			m_LastCityModifiers[0] = @float;
			m_LastCityModifiers[1] = float2;
			return result;
		}
	}

	[BurstCompile]
	public struct UpdateAreaResourcesJob : IJobParallelForDefer
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		private struct WoodResourceComparer : IComparer<WoodResource>
		{
			public int Compare(WoodResource x, WoodResource y)
			{
				return x.m_Tree.Index - y.m_Tree.Index;
			}
		}

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public bool m_FullUpdate;

		[ReadOnly]
		public NativeArray<Entity> m_UpdateList;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectTree;

		[ReadOnly]
		public CellMapData<NaturalResourceCell> m_NaturalResourceData;

		[ReadOnly]
		public CellMapData<GroundWater> m_GroundWaterResourceData;

		[ReadOnly]
		public ComponentLookup<Geometry> m_GeometryData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Plant> m_PlantData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_ExtractorAreaData;

		[ReadOnly]
		public ComponentLookup<TreeData> m_PrefabTreeData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Extractor> m_ExtractorData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<WoodResource> m_WoodResources;

		[NativeDisableParallelForRestriction]
		public BufferLookup<MapFeatureElement> m_MapFeatureElements;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public Bounds1 m_BuildableLandMaxSlope;

		public void Execute(int index)
		{
			Entity entity = m_UpdateList[index];
			DynamicBuffer<Node> nodes = m_Nodes[entity];
			DynamicBuffer<Triangle> triangles = m_Triangles[entity];
			DynamicBuffer<CityModifier> cityModifiers = default(DynamicBuffer<CityModifier>);
			if (m_City != Entity.Null)
			{
				cityModifiers = m_CityModifiers[m_City];
			}
			if (m_ExtractorData.HasComponent(entity))
			{
				PrefabRef prefabRef = m_PrefabRefData[entity];
				ExtractorAreaData extractorAreaData = m_ExtractorAreaData[prefabRef.m_Prefab];
				Extractor extractor = m_ExtractorData[entity];
				extractor.m_ResourceAmount = 0f;
				extractor.m_MaxConcentration = 0f;
				switch (extractorAreaData.m_MapFeature)
				{
				case MapFeature.Forest:
					if (m_WoodResources.HasBuffer(entity))
					{
						DynamicBuffer<WoodResource> woodResources = m_WoodResources[entity];
						CalculateWoodResources(nodes, triangles, ref extractor, woodResources);
					}
					break;
				case MapFeature.FertileLand:
				case MapFeature.Oil:
				case MapFeature.Ore:
				case MapFeature.Fish:
					CalculateNaturalResources(nodes, triangles, cityModifiers, ref extractor, extractorAreaData.m_MapFeature);
					break;
				}
				m_ExtractorData[entity] = extractor;
			}
			if (m_MapFeatureElements.HasBuffer(entity))
			{
				Geometry geometry = m_GeometryData[entity];
				DynamicBuffer<MapFeatureElement> buffer = m_MapFeatureElements[entity];
				CollectionUtils.ResizeInitialized(buffer, 9);
				WoodIterator iterator = new WoodIterator
				{
					m_TreeData = m_TreeData,
					m_PlantData = m_PlantData,
					m_TransformData = m_TransformData,
					m_DamagedData = m_DamagedData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabTreeData = m_PrefabTreeData
				};
				for (int i = 0; i < triangles.Length; i++)
				{
					iterator.m_Triangle = AreaUtils.GetTriangle2(nodes, triangles[i]);
					iterator.m_Bounds = MathUtils.Bounds(iterator.m_Triangle);
					m_ObjectTree.Iterate(ref iterator);
				}
				float4 resources = float4.zero;
				float4 renewal = float4.zero;
				float groundWater = 0f;
				float buildableArea = 0f;
				CalculateNaturalResources(nodes, triangles, cityModifiers, ref resources, ref renewal, ref groundWater, ref buildableArea);
				buffer[0] = new MapFeatureElement(geometry.m_SurfaceArea, 0f);
				buffer[3] = new MapFeatureElement(iterator.m_WoodAmount, iterator.m_GrowthRate);
				buffer[2] = new MapFeatureElement(resources.x, renewal.x);
				buffer[5] = new MapFeatureElement(resources.y, renewal.y);
				buffer[4] = new MapFeatureElement(resources.z, renewal.z);
				buffer[8] = new MapFeatureElement(resources.w, renewal.w);
				buffer[7] = new MapFeatureElement(groundWater, 0f);
				buffer[1] = new MapFeatureElement(buildableArea, 0f);
			}
		}

		private void CalculateWoodResources(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, ref Extractor extractor, DynamicBuffer<WoodResource> woodResources)
		{
			if (m_FullUpdate)
			{
				woodResources.Clear();
				TreeIterator iterator = new TreeIterator
				{
					m_TransformData = m_TransformData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabTreeData = m_PrefabTreeData,
					m_Buffer = woodResources
				};
				for (int i = 0; i < triangles.Length; i++)
				{
					iterator.m_Triangle = AreaUtils.GetTriangle2(nodes, triangles[i]);
					iterator.m_Bounds = MathUtils.Bounds(iterator.m_Triangle);
					m_ObjectTree.Iterate(ref iterator);
				}
				woodResources.AsNativeArray().Sort(default(WoodResourceComparer));
				WoodResource woodResource = default(WoodResource);
				int num = 0;
				int num2 = 0;
				while (num < woodResources.Length)
				{
					WoodResource woodResource2 = woodResources[num++];
					if (woodResource2.m_Tree != woodResource.m_Tree)
					{
						woodResources[num2++] = woodResource2;
						woodResource = woodResource2;
					}
				}
				if (num2 < woodResources.Length)
				{
					woodResources.RemoveRange(num2, woodResources.Length - num2);
				}
			}
			for (int j = 0; j < woodResources.Length; j++)
			{
				Entity tree = woodResources[j].m_Tree;
				Tree tree2 = m_TreeData[tree];
				Plant plant = m_PlantData[tree];
				PrefabRef prefabRef = m_PrefabRefData[tree];
				m_DamagedData.TryGetComponent(tree, out var componentData);
				if (m_PrefabTreeData.TryGetComponent(prefabRef.m_Prefab, out var componentData2))
				{
					float num3 = ObjectUtils.CalculateWoodAmount(tree2, plant, componentData, componentData2);
					if (num3 > 0f)
					{
						extractor.m_ResourceAmount += num3;
						extractor.m_MaxConcentration = math.max(extractor.m_MaxConcentration, num3 * (1f / componentData2.m_WoodAmount));
					}
				}
			}
			extractor.m_MaxConcentration = math.min(extractor.m_MaxConcentration, 1f);
		}

		private void CalculateNaturalResources(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, DynamicBuffer<CityModifier> cityModifiers, ref Extractor extractor, MapFeature mapFeature)
		{
			float4 xyxy = (1f / m_NaturalResourceData.m_CellSize).xyxy;
			float4 xyxy2 = ((float2)m_NaturalResourceData.m_TextureSize * 0.5f).xyxy;
			float num = 1f / (m_NaturalResourceData.m_CellSize.x * m_NaturalResourceData.m_CellSize.y);
			Bounds2 bounds2 = default(Bounds2);
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle2 triangle = AreaUtils.GetTriangle2(nodes, triangles[i]);
				Bounds2 bounds = MathUtils.Bounds(triangle);
				int4 valueToClamp = (int4)math.floor(new float4(bounds.min, bounds.max) * xyxy + xyxy2);
				valueToClamp = math.clamp(valueToClamp, 0, m_NaturalResourceData.m_TextureSize.xyxy - 1);
				float num2 = 0f;
				float num3 = 0f;
				for (int j = valueToClamp.y; j <= valueToClamp.w; j++)
				{
					bounds2.min.y = ((float)j - xyxy2.y) * m_NaturalResourceData.m_CellSize.y;
					bounds2.max.y = bounds2.min.y + m_NaturalResourceData.m_CellSize.y;
					for (int k = valueToClamp.x; k <= valueToClamp.z; k++)
					{
						NaturalResourceCell naturalResourceCell = m_NaturalResourceData.m_Buffer[k + m_NaturalResourceData.m_TextureSize.x * j];
						float valueToClamp2;
						switch (mapFeature)
						{
						case MapFeature.FertileLand:
							valueToClamp2 = (int)naturalResourceCell.m_Fertility.m_Base;
							valueToClamp2 -= (float)(int)naturalResourceCell.m_Fertility.m_Used;
							break;
						case MapFeature.Ore:
							valueToClamp2 = (int)naturalResourceCell.m_Ore.m_Base;
							if (cityModifiers.IsCreated)
							{
								CityUtils.ApplyModifier(ref valueToClamp2, cityModifiers, CityModifierType.OreResourceAmount);
							}
							valueToClamp2 -= (float)(int)naturalResourceCell.m_Ore.m_Used;
							break;
						case MapFeature.Oil:
							valueToClamp2 = (int)naturalResourceCell.m_Oil.m_Base;
							if (cityModifiers.IsCreated)
							{
								CityUtils.ApplyModifier(ref valueToClamp2, cityModifiers, CityModifierType.OilResourceAmount);
							}
							valueToClamp2 -= (float)(int)naturalResourceCell.m_Oil.m_Used;
							break;
						case MapFeature.Fish:
							valueToClamp2 = (int)naturalResourceCell.m_Fish.m_Base;
							valueToClamp2 -= (float)(int)naturalResourceCell.m_Fish.m_Used;
							break;
						default:
							valueToClamp2 = 0f;
							break;
						}
						valueToClamp2 = math.clamp(valueToClamp2, 0f, 65535f);
						if (valueToClamp2 != 0f)
						{
							bounds2.min.x = ((float)k - xyxy2.x) * m_NaturalResourceData.m_CellSize.x;
							bounds2.max.x = bounds2.min.x + m_NaturalResourceData.m_CellSize.x;
							if (MathUtils.Intersect(bounds2, triangle, out var area))
							{
								num2 += area * math.min(valueToClamp2 * 0.0001f, 1f);
								num3 += area;
								extractor.m_ResourceAmount += valueToClamp2 * area * num;
							}
						}
					}
				}
				num2 = ((num3 > 0.01f) ? (num2 / num3) : 0f);
				extractor.m_MaxConcentration = math.max(extractor.m_MaxConcentration, num2);
			}
		}

		private void CalculateNaturalResources(DynamicBuffer<Node> nodes, DynamicBuffer<Triangle> triangles, DynamicBuffer<CityModifier> cityModifiers, ref float4 resources, ref float4 renewal, ref float groundWater, ref float buildableArea)
		{
			float4 upperBound = new float4(800f, 0f, 0f, 800f);
			float4 xyxy = (1f / m_NaturalResourceData.m_CellSize).xyxy;
			float4 xyxy2 = ((float2)m_NaturalResourceData.m_TextureSize * 0.5f).xyxy;
			float num = 1f / (m_NaturalResourceData.m_CellSize.x * m_NaturalResourceData.m_CellSize.y);
			Bounds2 bounds2 = default(Bounds2);
			for (int i = 0; i < triangles.Length; i++)
			{
				Triangle2 triangle = AreaUtils.GetTriangle2(nodes, triangles[i]);
				Bounds2 bounds = MathUtils.Bounds(triangle);
				int4 valueToClamp = (int4)math.floor(new float4(bounds.min, bounds.max) * xyxy + xyxy2);
				valueToClamp = math.clamp(valueToClamp, 0, m_NaturalResourceData.m_TextureSize.xyxy - 1);
				for (int j = valueToClamp.y; j <= valueToClamp.w; j++)
				{
					bounds2.min.y = ((float)j - xyxy2.y) * m_NaturalResourceData.m_CellSize.y;
					bounds2.max.y = bounds2.min.y + m_NaturalResourceData.m_CellSize.y;
					for (int k = valueToClamp.x; k <= valueToClamp.z; k++)
					{
						NaturalResourceCell naturalResourceCell = m_NaturalResourceData.m_Buffer[k + m_NaturalResourceData.m_TextureSize.x * j];
						GroundWater groundWater2 = m_GroundWaterResourceData.m_Buffer[k + m_GroundWaterResourceData.m_TextureSize.x * j];
						float4 baseResources = naturalResourceCell.GetBaseResources();
						float4 usedResources = naturalResourceCell.GetUsedResources();
						if (cityModifiers.IsCreated)
						{
							CityUtils.ApplyModifier(ref baseResources.y, cityModifiers, CityModifierType.OreResourceAmount);
							CityUtils.ApplyModifier(ref baseResources.z, cityModifiers, CityModifierType.OilResourceAmount);
						}
						float4 @float = math.clamp(baseResources, 0f, upperBound);
						baseResources -= usedResources;
						baseResources = math.clamp(baseResources, 0f, 65535f);
						bounds2.min.x = ((float)k - xyxy2.x) * m_NaturalResourceData.m_CellSize.x;
						bounds2.max.x = bounds2.min.x + m_NaturalResourceData.m_CellSize.x;
						float3 worldPos = new float3(0.5f * (bounds2.min.x + bounds2.max.x), 0f, 0.5f * (bounds2.min.y + bounds2.max.y));
						groundWater += groundWater2.m_Amount;
						float num2 = CalculateBuildable(worldPos, m_NaturalResourceData.m_CellSize, m_WaterSurfaceData, m_TerrainHeightData, m_BuildableLandMaxSlope);
						if (MathUtils.Intersect(bounds2, triangle, out var area))
						{
							float num3 = area * num;
							resources += baseResources * num3;
							renewal += @float * num3;
							buildableArea += num2 * area;
						}
					}
				}
			}
		}
	}

	private struct TreeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds2 m_Bounds;

		public Triangle2 m_Triangle;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<TreeData> m_PrefabTreeData;

		public DynamicBuffer<WoodResource> m_Buffer;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((bounds.m_Mask & (BoundsMask.IsTree | BoundsMask.NotOverridden)) != (BoundsMask.IsTree | BoundsMask.NotOverridden))
			{
				return false;
			}
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
			{
				return false;
			}
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_Triangle);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if ((bounds.m_Mask & (BoundsMask.IsTree | BoundsMask.NotOverridden)) != (BoundsMask.IsTree | BoundsMask.NotOverridden) || !MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || !MathUtils.Intersect(bounds.m_Bounds.xz, m_Triangle))
			{
				return;
			}
			Transform transform = m_TransformData[entity];
			if (MathUtils.Intersect(m_Triangle, transform.m_Position.xz))
			{
				PrefabRef prefabRef = m_PrefabRefData[entity];
				if (m_PrefabTreeData.HasComponent(prefabRef.m_Prefab) && m_PrefabTreeData[prefabRef.m_Prefab].m_WoodAmount >= 1f)
				{
					m_Buffer.Add(new WoodResource(entity));
				}
			}
		}
	}

	private struct WoodIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
	{
		public Bounds2 m_Bounds;

		public Triangle2 m_Triangle;

		public ComponentLookup<Tree> m_TreeData;

		public ComponentLookup<Plant> m_PlantData;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<Damaged> m_DamagedData;

		public ComponentLookup<PrefabRef> m_PrefabRefData;

		public ComponentLookup<TreeData> m_PrefabTreeData;

		public float m_WoodAmount;

		public float m_GrowthRate;

		public bool Intersect(QuadTreeBoundsXZ bounds)
		{
			if ((bounds.m_Mask & (BoundsMask.IsTree | BoundsMask.NotOverridden)) != (BoundsMask.IsTree | BoundsMask.NotOverridden))
			{
				return false;
			}
			if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds))
			{
				return false;
			}
			return MathUtils.Intersect(bounds.m_Bounds.xz, m_Triangle);
		}

		public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
		{
			if ((bounds.m_Mask & (BoundsMask.IsTree | BoundsMask.NotOverridden)) != (BoundsMask.IsTree | BoundsMask.NotOverridden) || !MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || !MathUtils.Intersect(bounds.m_Bounds.xz, m_Triangle))
			{
				return;
			}
			Transform transform = m_TransformData[entity];
			if (MathUtils.Intersect(m_Triangle, transform.m_Position.xz))
			{
				Tree tree = m_TreeData[entity];
				Plant plant = m_PlantData[entity];
				PrefabRef prefabRef = m_PrefabRefData[entity];
				m_DamagedData.TryGetComponent(entity, out var componentData);
				if (m_PrefabTreeData.TryGetComponent(prefabRef.m_Prefab, out var componentData2) && componentData2.m_WoodAmount >= 1f)
				{
					m_WoodAmount += ObjectUtils.CalculateWoodAmount(tree, plant, componentData, componentData2);
					m_GrowthRate += ObjectUtils.CalculateGrowthRate(tree, plant, componentData2);
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public BufferLookup<WoodResource> __Game_Areas_WoodResource_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<Geometry> __Game_Areas_Geometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Plant> __Game_Objects_Plant_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TreeData> __Game_Prefabs_TreeData_RO_ComponentLookup;

		public ComponentLookup<Extractor> __Game_Areas_Extractor_RW_ComponentLookup;

		public BufferLookup<WoodResource> __Game_Areas_WoodResource_RW_BufferLookup;

		public BufferLookup<MapFeatureElement> __Game_Areas_MapFeatureElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Areas_WoodResource_RO_BufferLookup = state.GetBufferLookup<WoodResource>(isReadOnly: true);
			__Game_Areas_MapFeatureElement_RO_BufferLookup = state.GetBufferLookup<MapFeatureElement>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentLookup = state.GetComponentLookup<Geometry>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_Plant_RO_ComponentLookup = state.GetComponentLookup<Plant>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_TreeData_RO_ComponentLookup = state.GetComponentLookup<TreeData>(isReadOnly: true);
			__Game_Areas_Extractor_RW_ComponentLookup = state.GetComponentLookup<Extractor>();
			__Game_Areas_WoodResource_RW_BufferLookup = state.GetBufferLookup<WoodResource>();
			__Game_Areas_MapFeatureElement_RW_BufferLookup = state.GetBufferLookup<MapFeatureElement>();
		}
	}

	private Game.Objects.UpdateCollectSystem m_ObjectUpdateCollectSystem;

	private SearchSystem m_AreaSearchSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private NaturalResourceSystem m_NaturalResourceSystem;

	private GroundWaterSystem m_GroundWaterSystem;

	private CitySystem m_CitySystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_UpdatedAreaQuery;

	private EntityQuery m_MapTileQuery;

	private EntityQuery m_BrushQuery;

	private NativeArray<float2> m_LastCityModifiers;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_596039173_0;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ObjectUpdateCollectSystem = base.World.GetOrCreateSystemManaged<Game.Objects.UpdateCollectSystem>();
		m_AreaSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_NaturalResourceSystem = base.World.GetOrCreateSystemManaged<NaturalResourceSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_GroundWaterSystem = base.World.GetOrCreateSystemManaged<GroundWaterSystem>();
		m_UpdatedAreaQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[1] { ComponentType.ReadOnly<Updated>() },
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Extractor>(),
				ComponentType.ReadOnly<MapFeatureElement>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Deleted>() }
		});
		m_MapTileQuery = GetEntityQuery(ComponentType.ReadOnly<MapFeatureElement>(), ComponentType.Exclude<Native>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Updated>());
		m_BrushQuery = GetEntityQuery(ComponentType.ReadOnly<Brush>(), ComponentType.ReadOnly<Applied>());
		m_LastCityModifiers = new NativeArray<float2>(2, Allocator.Persistent);
		RequireForUpdate<AreasConfigurationData>();
	}

	public void PostDeserialize(Context context)
	{
		if (!context.format.Has(FormatTags.FishResource))
		{
			using (EntityQuery entityQuery = base.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<MapFeatureElement>()))
			{
				base.EntityManager.AddComponent<Updated>(entityQuery);
			}
		}
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		if (m_CitySystem.City != Entity.Null)
		{
			DynamicBuffer<CityModifier> buffer = base.EntityManager.GetBuffer<CityModifier>(m_CitySystem.City, isReadOnly: true);
			m_LastCityModifiers[0] = CityUtils.GetModifier(buffer, CityModifierType.OreResourceAmount);
			m_LastCityModifiers[1] = CityUtils.GetModifier(buffer, CityModifierType.OilResourceAmount);
		}
		else
		{
			ref NativeArray<float2> reference = ref m_LastCityModifiers;
			float2 value = (m_LastCityModifiers[1] = default(float2));
			reference[0] = value;
		}
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_LastCityModifiers.Dispose();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		bool flag = !m_BrushQuery.IsEmptyIgnoreFilter;
		if (!m_UpdatedAreaQuery.IsEmptyIgnoreFilter || flag || m_ObjectUpdateCollectSystem.isUpdated)
		{
			NativeQueue<Entity> updateBuffer = new NativeQueue<Entity>(Allocator.TempJob);
			NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
			NativeQueue<Entity>.ParallelWriter updateBuffer2 = updateBuffer.AsParallelWriter();
			if (flag)
			{
				JobHandle outJobHandle;
				NativeList<Brush> list = m_BrushQuery.ToComponentDataListAsync<Brush>(Allocator.TempJob, out outJobHandle);
				JobHandle dependencies;
				JobHandle jobHandle = new FindUpdatedAreasWithBrushesJob
				{
					m_Brushes = list.AsDeferredJobArray(),
					m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies),
					m_WoodResourceData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_WoodResource_RO_BufferLookup, ref base.CheckedStateRef),
					m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
					m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
					m_UpdateBuffer = updateBuffer2
				}.Schedule(list, 1, JobHandle.CombineDependencies(base.Dependency, outJobHandle, dependencies));
				list.Dispose(jobHandle);
				m_AreaSearchSystem.AddSearchTreeReader(jobHandle);
				base.Dependency = jobHandle;
			}
			if (m_ObjectUpdateCollectSystem.isUpdated)
			{
				JobHandle dependencies2;
				NativeList<Bounds2> updatedBounds = m_ObjectUpdateCollectSystem.GetUpdatedBounds(out dependencies2);
				JobHandle dependencies3;
				JobHandle jobHandle2 = new FindUpdatedAreasWithBoundsJob
				{
					m_Bounds = updatedBounds.AsDeferredJobArray(),
					m_AreaTree = m_AreaSearchSystem.GetSearchTree(readOnly: true, out dependencies3),
					m_WoodResourceData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_WoodResource_RO_BufferLookup, ref base.CheckedStateRef),
					m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RO_BufferLookup, ref base.CheckedStateRef),
					m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
					m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
					m_UpdateBuffer = updateBuffer2
				}.Schedule(updatedBounds, 1, JobHandle.CombineDependencies(base.Dependency, dependencies2, dependencies3));
				m_ObjectUpdateCollectSystem.AddBoundsReader(jobHandle2);
				m_AreaSearchSystem.AddSearchTreeReader(jobHandle2);
				base.Dependency = jobHandle2;
			}
			JobHandle outJobHandle2;
			NativeList<ArchetypeChunk> updatedAreaChunks = m_UpdatedAreaQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
			JobHandle outJobHandle3;
			NativeList<ArchetypeChunk> mapTileChunks = m_MapTileQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle3);
			CollectUpdatedAreasJob jobData = new CollectUpdatedAreasJob
			{
				m_UpdatedAreaChunks = updatedAreaChunks,
				m_MapTileChunks = mapTileChunks,
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_UpdateBuffer = updateBuffer,
				m_UpdateList = nativeList,
				m_LastCityModifiers = m_LastCityModifiers,
				m_City = m_CitySystem.City
			};
			JobHandle dependencies4;
			JobHandle dependencies5;
			JobHandle dependencies6;
			JobHandle deps;
			UpdateAreaResourcesJob jobData2 = new UpdateAreaResourcesJob
			{
				m_City = m_CitySystem.City,
				m_FullUpdate = true,
				m_UpdateList = nativeList.AsDeferredJobArray(),
				m_ObjectTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies4),
				m_NaturalResourceData = m_NaturalResourceSystem.GetData(readOnly: true, out dependencies5),
				m_GroundWaterResourceData = m_GroundWaterSystem.GetData(readOnly: true, out dependencies6),
				m_GeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Plant_RO_ComponentLookup, ref base.CheckedStateRef),
				m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
				m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_PrefabTreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TreeData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
				m_Triangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_ExtractorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Extractor_RW_ComponentLookup, ref base.CheckedStateRef),
				m_WoodResources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_WoodResource_RW_BufferLookup, ref base.CheckedStateRef),
				m_MapFeatureElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_MapFeatureElement_RW_BufferLookup, ref base.CheckedStateRef),
				m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
				m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
				m_BuildableLandMaxSlope = __query_596039173_0.GetSingleton<AreasConfigurationData>().m_BuildableLandMaxSlope
			};
			JobHandle jobHandle3 = IJobExtensions.Schedule(jobData, JobHandle.CombineDependencies(base.Dependency, outJobHandle2, outJobHandle3));
			JobHandle jobHandle4 = jobData2.Schedule(nativeList, 1, JobUtils.CombineDependencies(jobHandle3, dependencies4, dependencies5, deps, dependencies6));
			updateBuffer.Dispose(jobHandle3);
			nativeList.Dispose(jobHandle4);
			updatedAreaChunks.Dispose(jobHandle3);
			mapTileChunks.Dispose(jobHandle3);
			m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle4);
			m_NaturalResourceSystem.AddReader(jobHandle4);
			m_WaterSystem.AddSurfaceReader(jobHandle4);
			m_TerrainSystem.AddCPUHeightReader(jobHandle4);
			m_GroundWaterSystem.AddReader(jobHandle4);
			base.Dependency = jobHandle4;
		}
	}

	public static float CalculateBuildable(float3 worldPos, float2 cellSize, WaterSurfaceData<SurfaceWater> m_WaterSurfaceData, TerrainHeightData terrainHeightData, Bounds1 buildableLandMaxSlope)
	{
		float num = WaterUtils.SampleDepth(ref m_WaterSurfaceData, worldPos);
		float result = 0f;
		if (num < 0.1f)
		{
			float num2 = TerrainUtils.SampleHeight(ref terrainHeightData, worldPos + new float3(-0.5f * cellSize.x, 0f, 0f));
			float num3 = TerrainUtils.SampleHeight(ref terrainHeightData, worldPos + new float3(0.5f * cellSize.x, 0f, 0f));
			float3 x = new float3(cellSize.x, num3 - num2, 0f);
			float num4 = TerrainUtils.SampleHeight(ref terrainHeightData, worldPos + new float3(0f, 0f, 0f - cellSize.y));
			float num5 = TerrainUtils.SampleHeight(ref terrainHeightData, worldPos + new float3(0f, 0f, cellSize.y));
			float3 y = new float3(0f, num5 - num4, cellSize.y);
			float3 x2 = math.cross(x, y);
			float3 y2 = math.up();
			float x3 = math.length(math.cross(x2, y2)) / math.dot(x2, y2);
			result = math.saturate(math.unlerp(buildableLandMaxSlope.max, buildableLandMaxSlope.min, math.abs(x3)));
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<AreasConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_596039173_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public AreaResourceSystem()
	{
	}
}
