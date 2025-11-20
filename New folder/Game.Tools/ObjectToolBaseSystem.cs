using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public abstract class ObjectToolBaseSystem : ToolBaseSystem
{
	public struct AttachmentData
	{
		public Entity m_Entity;

		public float3 m_Offset;
	}

	[BurstCompile]
	private struct CreateDefinitionsJob : IJob
	{
		private struct VariationData
		{
			public Entity m_Prefab;

			public int m_Probability;
		}

		private struct BrushIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public NativeParallelHashSet<Entity> m_RequirePrefab;

			public Bounds2 m_Bounds;

			public RandomSeed m_RandomSeed;

			public Line3.Segment m_BrushLine;

			public float4 m_BrushCellSizeFactor;

			public float4 m_BrushTextureSizeAdd;

			public float2 m_BrushDirX;

			public float2 m_BrushDirZ;

			public float2 m_BrushCellSize;

			public int2 m_BrushResolution;

			public float m_TileSize;

			public float m_BrushStrength;

			public float m_StrengthFactor;

			public int m_BrushCount;

			public DynamicBuffer<BrushCell> m_BrushCells;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

			public ComponentLookup<EditorContainer> m_EditorContainerData;

			public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public EntityCommandBuffer m_CommandBuffer;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Bounds) || m_OwnerData.HasComponent(item))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[item];
				ObjectGeometryData componentData;
				if (m_RequirePrefab.IsCreated)
				{
					if (!m_RequirePrefab.Contains(prefabRef.m_Prefab))
					{
						return;
					}
				}
				else if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out componentData) || (componentData.m_Flags & Game.Objects.GeometryFlags.Brushable) == 0)
				{
					return;
				}
				Game.Objects.Transform transform = m_TransformData[item];
				int2 @int = (int2)math.floor(transform.m_Position.xz / m_TileSize);
				int index = ((@int.y & 0xFFFF) << 16) | (@int.x & 0xFFFF);
				Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
				if (random.NextFloat(1f) >= m_BrushStrength)
				{
					return;
				}
				float num = 0f;
				Bounds2 bounds2 = default(Bounds2);
				bounds2.min = (float2)@int * m_TileSize;
				bounds2.max = bounds2.min + m_TileSize;
				for (int i = 1; i <= m_BrushCount; i++)
				{
					float3 @float = MathUtils.Position(m_BrushLine, (float)i / (float)m_BrushCount);
					Bounds2 bounds3 = bounds2;
					bounds3.min -= @float.xz;
					bounds3.max -= @float.xz;
					float4 float2 = new float4(bounds3.min, bounds3.max);
					float4 x = new float4(math.dot(float2.xy, m_BrushDirX), math.dot(float2.xw, m_BrushDirX), math.dot(float2.zy, m_BrushDirX), math.dot(float2.zw, m_BrushDirX));
					float4 x2 = new float4(math.dot(float2.xy, m_BrushDirZ), math.dot(float2.xw, m_BrushDirZ), math.dot(float2.zy, m_BrushDirZ), math.dot(float2.zw, m_BrushDirZ));
					int4 valueToClamp = (int4)math.floor(new float4(math.cmin(x), math.cmin(x2), math.cmax(x), math.cmax(x2)) * m_BrushCellSizeFactor + m_BrushTextureSizeAdd);
					valueToClamp = math.clamp(valueToClamp, 0, m_BrushResolution.xyxy - 1);
					for (int j = valueToClamp.y; j <= valueToClamp.w; j++)
					{
						float2 float3 = m_BrushDirZ * (((float)j - m_BrushTextureSizeAdd.y) * m_BrushCellSize.y);
						float2 float4 = m_BrushDirZ * (((float)(j + 1) - m_BrushTextureSizeAdd.y) * m_BrushCellSize.y);
						for (int k = valueToClamp.x; k <= valueToClamp.z; k++)
						{
							int index2 = k + m_BrushResolution.x * j;
							BrushCell brushCell = m_BrushCells[index2];
							if (brushCell.m_Opacity >= 0.0001f)
							{
								float2 float5 = m_BrushDirX * (((float)k - m_BrushTextureSizeAdd.x) * m_BrushCellSize.x);
								float2 float6 = m_BrushDirX * (((float)(k + 1) - m_BrushTextureSizeAdd.x) * m_BrushCellSize.x);
								if (MathUtils.Intersect(quad: new Quad2(float3 + float5, float3 + float6, float4 + float6, float4 + float5), bounds: bounds3, area: out var area))
								{
									num += brushCell.m_Opacity * area;
								}
							}
						}
					}
				}
				num *= m_StrengthFactor;
				if (!(math.abs(num) >= 0.0001f) || !(random.NextFloat() < num))
				{
					return;
				}
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Original = item
				};
				component.m_Flags |= CreationFlags.Delete;
				m_CommandBuffer.AddComponent(e, default(Updated));
				ObjectDefinition component2 = new ObjectDefinition
				{
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation
				};
				if (m_ElevationData.TryGetComponent(item, out var componentData2))
				{
					component2.m_Elevation = componentData2.m_Elevation;
					component2.m_ParentMesh = ObjectUtils.GetSubParentMesh(componentData2.m_Flags);
					if ((componentData2.m_Flags & ElevationFlags.Lowered) != 0)
					{
						component.m_Flags |= CreationFlags.Lowered;
					}
				}
				else
				{
					component2.m_ParentMesh = -1;
				}
				component2.m_Probability = 100;
				component2.m_PrefabSubIndex = -1;
				if (m_LocalTransformCacheData.HasComponent(item))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[item];
					component2.m_LocalPosition = localTransformCache.m_Position;
					component2.m_LocalRotation = localTransformCache.m_Rotation;
					component2.m_ParentMesh = localTransformCache.m_ParentMesh;
					component2.m_GroupIndex = localTransformCache.m_GroupIndex;
					component2.m_Probability = localTransformCache.m_Probability;
					component2.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
				}
				else
				{
					component2.m_LocalPosition = transform.m_Position;
					component2.m_LocalRotation = transform.m_Rotation;
				}
				if (m_EditorContainerData.HasComponent(item))
				{
					EditorContainer editorContainer = m_EditorContainerData[item];
					component.m_SubPrefab = editorContainer.m_Prefab;
					component2.m_Scale = editorContainer.m_Scale;
					component2.m_Intensity = editorContainer.m_Intensity;
					component2.m_GroupIndex = editorContainer.m_GroupIndex;
				}
				m_CommandBuffer.AddComponent(e, component2);
				m_CommandBuffer.AddComponent(e, component);
			}
		}

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public bool m_Removing;

		[ReadOnly]
		public bool m_Stamping;

		[ReadOnly]
		public float m_BrushSize;

		[ReadOnly]
		public float m_BrushAngle;

		[ReadOnly]
		public float m_BrushStrength;

		[ReadOnly]
		public float m_Distance;

		[ReadOnly]
		public float m_DeltaTime;

		[ReadOnly]
		public Entity m_ObjectPrefab;

		[ReadOnly]
		public Entity m_TransformPrefab;

		[ReadOnly]
		public Entity m_BrushPrefab;

		[ReadOnly]
		public Entity m_Owner;

		[ReadOnly]
		public Entity m_Original;

		[ReadOnly]
		public Entity m_LaneEditor;

		[ReadOnly]
		public Entity m_Theme;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public Snap m_Snap;

		[ReadOnly]
		public AgeMask m_AgeMask;

		[ReadOnly]
		public NativeList<ControlPoint> m_ControlPoints;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeReference<AttachmentData> m_AttachmentPrefab;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> m_LocalTransformCacheData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> m_LotData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> m_NetElevationData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Upgraded> m_UpgradedData;

		[ReadOnly]
		public ComponentLookup<Clear> m_AreaClearData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Space> m_AreaSpaceData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_AreaLotData;

		[ReadOnly]
		public ComponentLookup<EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetObjectData> m_PrefabNetObjectData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<AssetStampData> m_PrefabAssetStampData;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> m_PrefabBuildingExtensionData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> m_PlaceholderBuildingData;

		[ReadOnly]
		public ComponentLookup<BrushData> m_PrefabBrushData;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> m_PrefabBuildingTerraformData;

		[ReadOnly]
		public ComponentLookup<CreatureSpawnData> m_PrefabCreatureSpawnData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> m_CachedNodes;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> m_AreaNodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_AreaTriangles;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> m_PrefabSubLanes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderElements;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_PrefabRequirementElements;

		[ReadOnly]
		public BufferLookup<ServiceUpgradeBuilding> m_PrefabServiceUpgradeBuilding;

		[ReadOnly]
		public BufferLookup<BrushCell> m_PrefabBrushCells;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			ControlPoint startPoint = m_ControlPoints[0];
			Entity entity = m_Owner;
			Entity entity2 = m_Original;
			Entity updatedTopLevel = Entity.Null;
			Entity lotEntity = Entity.Null;
			OwnerDefinition ownerDefinition = default(OwnerDefinition);
			bool upgrade = false;
			bool flag = entity2 != Entity.Null;
			bool topLevel = true;
			int parentMesh = ((!(entity != Entity.Null)) ? (-1) : 0);
			if (!flag && m_PrefabNetObjectData.HasComponent(m_ObjectPrefab) && m_AttachedData.HasComponent(startPoint.m_OriginalEntity) && (m_EditorMode || !m_OwnerData.HasComponent(startPoint.m_OriginalEntity)))
			{
				Attached attached = m_AttachedData[startPoint.m_OriginalEntity];
				if (m_NodeData.HasComponent(attached.m_Parent) || m_EdgeData.HasComponent(attached.m_Parent))
				{
					entity2 = startPoint.m_OriginalEntity;
					startPoint.m_OriginalEntity = attached.m_Parent;
					upgrade = true;
				}
			}
			Owner componentData4;
			if (m_EditorMode)
			{
				Entity entity3 = startPoint.m_OriginalEntity;
				int num = startPoint.m_ElementIndex.x;
				while (m_OwnerData.HasComponent(entity3) && !m_BuildingData.HasComponent(entity3))
				{
					if (m_LocalTransformCacheData.HasComponent(entity3))
					{
						num = m_LocalTransformCacheData[entity3].m_ParentMesh;
						num += math.select(1000, -1000, num < 0);
					}
					entity3 = m_OwnerData[entity3].m_Owner;
				}
				if (m_InstalledUpgrades.TryGetBuffer(entity3, out var bufferData) && bufferData.Length != 0)
				{
					entity3 = bufferData[0].m_Upgrade;
				}
				bool flag2 = false;
				if (m_PrefabRefData.TryGetComponent(entity3, out var componentData) && m_PrefabServiceUpgradeBuilding.TryGetBuffer(m_ObjectPrefab, out var bufferData2))
				{
					Entity entity4 = Entity.Null;
					if (m_TransformData.TryGetComponent(entity3, out var componentData2) && m_PrefabBuildingExtensionData.TryGetComponent(m_ObjectPrefab, out var componentData3))
					{
						for (int i = 0; i < bufferData2.Length; i++)
						{
							if (bufferData2[i].m_Building == componentData.m_Prefab)
							{
								entity4 = entity3;
								startPoint.m_Position = ObjectUtils.LocalToWorld(componentData2, componentData3.m_Position);
								startPoint.m_Rotation = componentData2.m_Rotation;
								break;
							}
						}
					}
					entity3 = entity4;
					flag2 = true;
				}
				if (m_TransformData.HasComponent(entity3) && m_SubObjects.HasBuffer(entity3))
				{
					entity = entity3;
					topLevel = flag2;
					parentMesh = num;
				}
				if (m_OwnerData.HasComponent(entity2))
				{
					Owner owner = m_OwnerData[entity2];
					if (owner.m_Owner != entity)
					{
						entity = owner.m_Owner;
						topLevel = flag2;
						parentMesh = -1;
					}
				}
				if (!m_EdgeData.HasComponent(startPoint.m_OriginalEntity) && !m_NodeData.HasComponent(startPoint.m_OriginalEntity))
				{
					startPoint.m_OriginalEntity = Entity.Null;
				}
			}
			else if (flag && entity == Entity.Null && m_OwnerData.TryGetComponent(entity2, out componentData4))
			{
				entity = componentData4.m_Owner;
			}
			NativeHashSet<Entity> attachedEntities = default(NativeHashSet<Entity>);
			NativeList<ClearAreaData> clearAreas = default(NativeList<ClearAreaData>);
			if (m_TransformData.HasComponent(entity))
			{
				Game.Objects.Transform transform = m_TransformData[entity];
				m_ElevationData.TryGetComponent(entity, out var componentData5);
				Entity owner2 = Entity.Null;
				if (m_OwnerData.HasComponent(entity))
				{
					owner2 = m_OwnerData[entity].m_Owner;
				}
				ownerDefinition.m_Prefab = m_PrefabRefData[entity].m_Prefab;
				ownerDefinition.m_Position = transform.m_Position;
				ownerDefinition.m_Rotation = transform.m_Rotation;
				if (m_Stamping || CheckParentPrefab(ownerDefinition.m_Prefab, m_ObjectPrefab))
				{
					updatedTopLevel = entity;
					if (m_PrefabServiceUpgradeBuilding.HasBuffer(m_ObjectPrefab))
					{
						ClearAreaHelpers.FillClearAreas(ownerTransform: new Game.Objects.Transform(startPoint.m_Position, startPoint.m_Rotation), ownerPrefab: m_ObjectPrefab, prefabObjectGeometryData: m_PrefabObjectGeometryData, prefabAreaGeometryData: m_PrefabAreaGeometryData, prefabSubAreas: m_PrefabSubAreas, prefabSubAreaNodes: m_PrefabSubAreaNodes, clearAreas: ref clearAreas);
						ClearAreaHelpers.InitClearAreas(clearAreas, transform);
						if (entity2 == Entity.Null)
						{
							lotEntity = entity;
						}
					}
					bool flag3 = m_ObjectPrefab == Entity.Null;
					Entity parent = Entity.Null;
					if (flag3 && m_InstalledUpgrades.TryGetBuffer(entity, out var bufferData3))
					{
						ClearAreaHelpers.FillClearAreas(bufferData3, Entity.Null, m_TransformData, m_AreaClearData, m_PrefabRefData, m_PrefabObjectGeometryData, m_SubAreas, m_AreaNodes, m_AreaTriangles, ref clearAreas);
						ClearAreaHelpers.InitClearAreas(clearAreas, transform);
					}
					if (flag3 && m_AttachedData.TryGetComponent(entity, out var componentData6) && m_BuildingData.HasComponent(componentData6.m_Parent))
					{
						Game.Objects.Transform transform2 = m_TransformData[componentData6.m_Parent];
						parent = m_PrefabRefData[componentData6.m_Parent].m_Prefab;
						UpdateObject(Entity.Null, Entity.Null, Entity.Null, componentData6.m_Parent, Entity.Null, componentData6.m_Parent, Entity.Null, transform2, 0f, default(OwnerDefinition), ref attachedEntities, clearAreas, upgrade: false, relocate: false, rebuild: false, topLevel: true, optional: false, -1, -1);
					}
					UpdateObject(Entity.Null, Entity.Null, owner2, entity, parent, updatedTopLevel, Entity.Null, transform, componentData5.m_Elevation, default(OwnerDefinition), ref attachedEntities, clearAreas, upgrade: true, relocate: false, flag3, topLevel: true, optional: false, -1, -1);
					if (m_AttachmentData.TryGetComponent(entity, out var componentData7) && m_BuildingData.HasComponent(componentData7.m_Attached))
					{
						Game.Objects.Transform transform3 = m_TransformData[componentData7.m_Attached];
						parent = m_PrefabRefData[entity].m_Prefab;
						UpdateObject(Entity.Null, Entity.Null, Entity.Null, componentData7.m_Attached, parent, componentData7.m_Attached, Entity.Null, transform3, 0f, default(OwnerDefinition), ref attachedEntities, clearAreas, upgrade: true, relocate: false, rebuild: false, topLevel: true, optional: false, -1, -1);
					}
					if (clearAreas.IsCreated)
					{
						clearAreas.Clear();
					}
				}
				else
				{
					ownerDefinition = default(OwnerDefinition);
				}
			}
			if (entity2 != Entity.Null && m_InstalledUpgrades.TryGetBuffer(entity2, out var bufferData4))
			{
				ClearAreaHelpers.FillClearAreas(bufferData4, Entity.Null, m_TransformData, m_AreaClearData, m_PrefabRefData, m_PrefabObjectGeometryData, m_SubAreas, m_AreaNodes, m_AreaTriangles, ref clearAreas);
				ClearAreaHelpers.TransformClearAreas(clearAreas, m_TransformData[entity2], new Game.Objects.Transform(startPoint.m_Position, startPoint.m_Rotation));
				ClearAreaHelpers.InitClearAreas(clearAreas, new Game.Objects.Transform(startPoint.m_Position, startPoint.m_Rotation));
			}
			if (m_ObjectPrefab != Entity.Null)
			{
				if (m_BrushPrefab != Entity.Null)
				{
					if (m_ControlPoints.Length >= 2)
					{
						CreateBrushes(startPoint, m_ControlPoints[1], updatedTopLevel, ownerDefinition, ref attachedEntities, clearAreas, topLevel, parentMesh);
					}
				}
				else if (m_Distance > 0f)
				{
					CreateCurve(startPoint, m_ControlPoints[math.min(1, m_ControlPoints.Length - 1)], m_ControlPoints[m_ControlPoints.Length - 1], updatedTopLevel, ownerDefinition, ref attachedEntities, clearAreas, topLevel, parentMesh);
				}
				else
				{
					Entity entity5 = m_ObjectPrefab;
					if (entity2 == Entity.Null && (!m_EditorMode || ownerDefinition.m_Prefab == Entity.Null) && m_PrefabPlaceholderElements.TryGetBuffer(m_ObjectPrefab, out var bufferData5) && !m_PrefabCreatureSpawnData.HasComponent(m_ObjectPrefab))
					{
						Unity.Mathematics.Random random = m_RandomSeed.GetRandom(1000000);
						int num2 = 0;
						for (int j = 0; j < bufferData5.Length; j++)
						{
							if (GetVariationData(bufferData5[j], out var variation))
							{
								num2 += variation.m_Probability;
								if (random.NextInt(num2) < variation.m_Probability)
								{
									entity5 = variation.m_Prefab;
								}
							}
						}
					}
					UpdateObject(entity5, m_TransformPrefab, Entity.Null, entity2, startPoint.m_OriginalEntity, updatedTopLevel, lotEntity, new Game.Objects.Transform(startPoint.m_Position, startPoint.m_Rotation), startPoint.m_Elevation, ownerDefinition, ref attachedEntities, clearAreas, upgrade, flag, rebuild: false, topLevel, optional: false, parentMesh, 0);
					if (m_AttachmentPrefab.IsCreated && m_AttachmentPrefab.Value.m_Entity != Entity.Null)
					{
						Game.Objects.Transform transform4 = new Game.Objects.Transform(startPoint.m_Position, startPoint.m_Rotation);
						transform4.m_Position += math.rotate(transform4.m_Rotation, m_AttachmentPrefab.Value.m_Offset);
						UpdateObject(m_AttachmentPrefab.Value.m_Entity, Entity.Null, Entity.Null, Entity.Null, entity5, updatedTopLevel, Entity.Null, transform4, startPoint.m_Elevation, ownerDefinition, ref attachedEntities, clearAreas, upgrade: false, relocate: false, rebuild: false, topLevel, optional: false, parentMesh, 0);
					}
				}
			}
			if (attachedEntities.IsCreated)
			{
				attachedEntities.Dispose();
			}
			if (clearAreas.IsCreated)
			{
				clearAreas.Dispose();
			}
		}

		private bool GetVariationData(PlaceholderObjectElement placeholder, out VariationData variation)
		{
			variation = new VariationData
			{
				m_Prefab = placeholder.m_Object,
				m_Probability = 100
			};
			if (m_PrefabRequirementElements.TryGetBuffer(variation.m_Prefab, out var bufferData))
			{
				int num = -1;
				bool flag = true;
				for (int i = 0; i < bufferData.Length; i++)
				{
					ObjectRequirementElement objectRequirementElement = bufferData[i];
					if (objectRequirementElement.m_Group != num)
					{
						if (!flag)
						{
							break;
						}
						num = objectRequirementElement.m_Group;
						flag = false;
					}
					flag |= m_Theme == objectRequirementElement.m_Requirement;
				}
				if (!flag)
				{
					return false;
				}
			}
			if (m_PrefabSpawnableObjectData.TryGetComponent(variation.m_Prefab, out var componentData))
			{
				variation.m_Probability = componentData.m_Probability;
			}
			return true;
		}

		private void CreateCurve(ControlPoint startPoint, ControlPoint middlePoint, ControlPoint endPoint, Entity updatedTopLevel, OwnerDefinition ownerDefinition, ref NativeHashSet<Entity> attachedEntities, NativeList<ClearAreaData> clearAreas, bool topLevel, int parentMesh)
		{
			Bezier4x3 curve = default(Bezier4x3);
			bool flag = false;
			float num = math.distance(startPoint.m_Position.xz, endPoint.m_Position.xz);
			if (!startPoint.m_Position.xz.Equals(middlePoint.m_Position.xz) && !endPoint.m_Position.xz.Equals(middlePoint.m_Position.xz))
			{
				float3 value = middlePoint.m_Position - startPoint.m_Position;
				float3 value2 = endPoint.m_Position - middlePoint.m_Position;
				value = MathUtils.Normalize(value, value.xz);
				value2 = MathUtils.Normalize(value2, value2.xz);
				curve = NetUtils.FitCurve(startPoint.m_Position, value, value2, endPoint.m_Position);
				flag = true;
				num = MathUtils.Length(curve.xz);
			}
			m_PrefabPlaceableObjectData.TryGetComponent(m_ObjectPrefab, out var componentData);
			NativeList<VariationData> nativeList = default(NativeList<VariationData>);
			if ((!m_EditorMode || ownerDefinition.m_Prefab == Entity.Null) && m_PrefabPlaceholderElements.TryGetBuffer(m_ObjectPrefab, out var bufferData) && !m_PrefabCreatureSpawnData.HasComponent(m_ObjectPrefab))
			{
				nativeList = new NativeList<VariationData>(bufferData.Length, Allocator.Temp);
				for (int i = 0; i < bufferData.Length; i++)
				{
					if (GetVariationData(bufferData[i], out var variation))
					{
						nativeList.Add(in variation);
					}
				}
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(1000000);
			int num2 = (int)(num / m_Distance + 1.5f);
			float num3 = 1f / (float)math.max(1, num2 - 1);
			for (int j = 0; j < num2; j++)
			{
				float num4 = (float)j * num3;
				float3 position = startPoint.m_Position;
				quaternion quaternion = startPoint.m_Rotation;
				if (j != 0)
				{
					float num5 = MathF.PI * 2f;
					num5 = ((componentData.m_RotationSymmetry == RotationSymmetry.Any) ? random.NextFloat(num5) : ((componentData.m_RotationSymmetry == RotationSymmetry.None) ? 0f : (num5 * ((float)random.NextInt((int)componentData.m_RotationSymmetry) / (float)(int)componentData.m_RotationSymmetry))));
					if (flag)
					{
						Bounds1 t = new Bounds1(0f, 1f);
						if (j < num2 - 1 && MathUtils.ClampLength(curve.xz, ref t, num4 * num))
						{
							num4 = t.max;
						}
						position = MathUtils.Position(curve, num4);
						float2 value3 = MathUtils.StartTangent(curve).xz;
						float2 value4 = MathUtils.Tangent(curve, num4).xz;
						if (MathUtils.TryNormalize(ref value3) && MathUtils.TryNormalize(ref value4))
						{
							num5 += MathUtils.RotationAngleRight(value3, value4);
						}
					}
					else
					{
						position = math.lerp(startPoint.m_Position, endPoint.m_Position, num4);
					}
					if (num5 != 0f)
					{
						quaternion = math.normalizesafe(math.mul(quaternion, quaternion.RotateY(num5)), quaternion.identity);
					}
				}
				float elevation;
				Game.Objects.Transform transform = SampleTransform(componentData, position, quaternion, out elevation);
				if (parentMesh != -1)
				{
					transform.m_Position.y = position.y;
					transform.m_Rotation = quaternion;
					elevation = startPoint.m_Elevation;
				}
				Entity entity = Entity.Null;
				if (nativeList.IsCreated)
				{
					int num6 = 0;
					for (int k = 0; k < nativeList.Length; k++)
					{
						VariationData variationData = nativeList[k];
						num6 += variationData.m_Probability;
						if (random.NextInt(num6) < variationData.m_Probability)
						{
							entity = variationData.m_Prefab;
						}
					}
				}
				else
				{
					entity = m_ObjectPrefab;
				}
				if (entity != Entity.Null)
				{
					UpdateObject(entity, m_TransformPrefab, Entity.Null, Entity.Null, Entity.Null, updatedTopLevel, Entity.Null, transform, elevation, ownerDefinition, ref attachedEntities, clearAreas, upgrade: false, relocate: false, rebuild: false, topLevel, optional: false, parentMesh, j);
				}
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private void CreateBrushes(ControlPoint startPoint, ControlPoint endPoint, Entity updatedTopLevel, OwnerDefinition ownerDefinition, ref NativeHashSet<Entity> attachedEntities, NativeList<ClearAreaData> clearAreas, bool topLevel, int parentMesh)
		{
			if (endPoint.Equals(default(ControlPoint)))
			{
				return;
			}
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = m_BrushPrefab
			};
			BrushDefinition component2 = new BrushDefinition
			{
				m_Tool = m_ObjectPrefab
			};
			if (startPoint.Equals(default(ControlPoint)))
			{
				component2.m_Line = new Line3.Segment(endPoint.m_Position, endPoint.m_Position);
			}
			else
			{
				component2.m_Line = new Line3.Segment(startPoint.m_Position, endPoint.m_Position);
			}
			component2.m_Size = m_BrushSize;
			component2.m_Angle = m_BrushAngle;
			component2.m_Strength = m_BrushStrength;
			component2.m_Time = m_DeltaTime;
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, component2);
			m_CommandBuffer.AddComponent(e, default(Updated));
			BrushData brushData = m_PrefabBrushData[m_BrushPrefab];
			DynamicBuffer<BrushCell> brushCells = m_PrefabBrushCells[m_BrushPrefab];
			if (math.any(brushData.m_Resolution == 0) || brushCells.Length == 0)
			{
				return;
			}
			float num = MathUtils.Length(component2.m_Line);
			int num2 = 1 + Mathf.FloorToInt(num / (m_BrushSize * 0.25f));
			float num3 = m_BrushStrength * m_BrushStrength * math.saturate(m_DeltaTime * 10f);
			quaternion q = quaternion.RotateY(m_BrushAngle);
			float2 xz = math.mul(q, new float3(1f, 0f, 0f)).xz;
			float2 xz2 = math.mul(q, new float3(0f, 0f, 1f)).xz;
			float num4 = 16f;
			float2 @float = (math.abs(xz) + math.abs(xz2)) * (m_BrushSize * 0.5f);
			Bounds2 bounds = new Bounds2(float.MaxValue, float.MinValue);
			for (int i = 1; i <= num2; i++)
			{
				float3 float2 = MathUtils.Position(component2.m_Line, (float)i / (float)num2);
				bounds |= new Bounds2(float2.xz - @float, float2.xz + @float);
			}
			float2 float3 = m_BrushSize / (float2)brushData.m_Resolution;
			float4 xyxy = (1f / float3).xyxy;
			float4 xyxy2 = ((float2)brushData.m_Resolution * 0.5f).xyxy;
			float num5 = 1f / ((float)num2 * num4 * num4);
			if (m_Removing)
			{
				BrushIterator iterator = new BrushIterator
				{
					m_Bounds = bounds,
					m_RandomSeed = m_RandomSeed,
					m_BrushLine = component2.m_Line,
					m_BrushCellSizeFactor = xyxy,
					m_BrushTextureSizeAdd = xyxy2,
					m_BrushDirX = xz,
					m_BrushDirZ = xz2,
					m_BrushCellSize = float3,
					m_BrushResolution = brushData.m_Resolution,
					m_TileSize = num4,
					m_BrushStrength = num3,
					m_StrengthFactor = num5,
					m_BrushCount = num2,
					m_BrushCells = brushCells,
					m_OwnerData = m_OwnerData,
					m_TransformData = m_TransformData,
					m_ElevationData = m_ElevationData,
					m_EditorContainerData = m_EditorContainerData,
					m_LocalTransformCacheData = m_LocalTransformCacheData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData,
					m_CommandBuffer = m_CommandBuffer
				};
				if ((m_Snap & Snap.PrefabType) != Snap.None && m_ObjectPrefab != Entity.Null)
				{
					if (m_PrefabPlaceholderElements.TryGetBuffer(m_ObjectPrefab, out var bufferData))
					{
						iterator.m_RequirePrefab = new NativeParallelHashSet<Entity>(1 + bufferData.Length, Allocator.Temp);
						iterator.m_RequirePrefab.Add(m_ObjectPrefab);
						for (int j = 0; j < bufferData.Length; j++)
						{
							iterator.m_RequirePrefab.Add(bufferData[j].m_Object);
						}
					}
					else
					{
						iterator.m_RequirePrefab = new NativeParallelHashSet<Entity>(1, Allocator.Temp);
						iterator.m_RequirePrefab.Add(m_ObjectPrefab);
					}
				}
				m_ObjectSearchTree.Iterate(ref iterator);
				if (iterator.m_RequirePrefab.IsCreated)
				{
					iterator.m_RequirePrefab.Dispose();
				}
				return;
			}
			m_PrefabPlaceableObjectData.TryGetComponent(m_ObjectPrefab, out var componentData);
			NativeList<VariationData> nativeList = default(NativeList<VariationData>);
			if (m_PrefabPlaceholderElements.TryGetBuffer(m_ObjectPrefab, out var bufferData2) && !m_PrefabCreatureSpawnData.HasComponent(m_ObjectPrefab))
			{
				nativeList = new NativeList<VariationData>(bufferData2.Length, Allocator.Temp);
				for (int k = 0; k < bufferData2.Length; k++)
				{
					if (GetVariationData(bufferData2[k], out var variation))
					{
						nativeList.Add(in variation);
					}
				}
			}
			int4 @int = (int4)math.floor(new float4(bounds.min, bounds.max) / num4);
			int2 int2 = 0;
			Bounds2 bounds2 = default(Bounds2);
			for (int l = 0; l < 3; l++)
			{
				float num6 = 0f;
				bool flag = false;
				for (int m = @int.y; m <= @int.w; m++)
				{
					bounds2.min.y = (float)m * num4;
					bounds2.max.y = bounds2.min.y + num4;
					for (int n = @int.x; n <= @int.z; n++)
					{
						bounds2.min.x = (float)n * num4;
						bounds2.max.x = bounds2.min.x + num4;
						int index = ((m & 0xFFFF) << 16) | (n & 0xFFFF);
						Unity.Mathematics.Random random = m_RandomSeed.GetRandom(index);
						float num7 = random.NextFloat(4f);
						switch (l)
						{
						case 0:
							if (num7 >= num3)
							{
								continue;
							}
							break;
						case 2:
							if (n != int2.x || m != int2.y)
							{
								continue;
							}
							break;
						}
						float num8 = 0f;
						if (l != 2)
						{
							for (int num9 = 1; num9 <= num2; num9++)
							{
								float3 float4 = MathUtils.Position(component2.m_Line, (float)num9 / (float)num2);
								Bounds2 bounds3 = bounds2;
								bounds3.min -= float4.xz;
								bounds3.max -= float4.xz;
								float4 float5 = new float4(bounds3.min, bounds3.max);
								float4 x = new float4(math.dot(float5.xy, xz), math.dot(float5.xw, xz), math.dot(float5.zy, xz), math.dot(float5.zw, xz));
								float4 x2 = new float4(math.dot(float5.xy, xz2), math.dot(float5.xw, xz2), math.dot(float5.zy, xz2), math.dot(float5.zw, xz2));
								int4 valueToClamp = (int4)math.floor(new float4(math.cmin(x), math.cmin(x2), math.cmax(x), math.cmax(x2)) * xyxy + xyxy2);
								valueToClamp = math.clamp(valueToClamp, 0, brushData.m_Resolution.xyxy - 1);
								for (int num10 = valueToClamp.y; num10 <= valueToClamp.w; num10++)
								{
									float2 float6 = xz2 * (((float)num10 - xyxy2.y) * float3.y);
									float2 float7 = xz2 * (((float)(num10 + 1) - xyxy2.y) * float3.y);
									for (int num11 = valueToClamp.x; num11 <= valueToClamp.z; num11++)
									{
										int index2 = num11 + brushData.m_Resolution.x * num10;
										BrushCell brushCell = brushCells[index2];
										if (brushCell.m_Opacity >= 0.0001f)
										{
											float2 float8 = xz * (((float)num11 - xyxy2.x) * float3.x);
											float2 float9 = xz * (((float)(num11 + 1) - xyxy2.x) * float3.x);
											if (MathUtils.Intersect(quad: new Quad2(float6 + float8, float6 + float9, float7 + float9, float7 + float8), bounds: bounds3, area: out var area))
											{
												num8 += brushCell.m_Opacity * area;
											}
										}
									}
								}
							}
							num8 *= num5;
							if (math.abs(num8) < 0.0001f)
							{
								continue;
							}
						}
						float4 float10 = random.NextFloat4(new float4(1f, 1f, 1f, MathF.PI * 2f));
						switch (l)
						{
						case 0:
							if (float10.x >= num8)
							{
								continue;
							}
							break;
						case 1:
							num6 += num8;
							if (float10.x * num6 < num8)
							{
								int2 = new int2(n, m);
							}
							continue;
						}
						float elevation;
						Game.Objects.Transform transform = SampleTransform(position: new float3
						{
							xz = math.lerp(bounds2.min, bounds2.max, float10.yz)
						}, placeableObjectData: componentData, rotation: quaternion.RotateY(float10.w), elevation: out elevation);
						Entity entity = Entity.Null;
						if (nativeList.IsCreated)
						{
							int num12 = 0;
							for (int num13 = 0; num13 < nativeList.Length; num13++)
							{
								VariationData variationData = nativeList[num13];
								num12 += variationData.m_Probability;
								if (random.NextInt(num12) < variationData.m_Probability)
								{
									entity = variationData.m_Prefab;
								}
							}
						}
						else
						{
							entity = m_ObjectPrefab;
						}
						if (entity != Entity.Null)
						{
							index = ((n & 0xFFFF) << 16) | (m & 0xFFFF);
							UpdateObject(entity, m_TransformPrefab, Entity.Null, Entity.Null, Entity.Null, updatedTopLevel, Entity.Null, transform, elevation, ownerDefinition, ref attachedEntities, clearAreas, upgrade: false, relocate: false, rebuild: false, topLevel, optional: true, parentMesh, index);
						}
						flag = true;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (nativeList.IsCreated)
			{
				nativeList.Dispose();
			}
		}

		private Game.Objects.Transform SampleTransform(PlaceableObjectData placeableObjectData, float3 position, quaternion rotation, out float elevation)
		{
			float3 normal;
			float num = TerrainUtils.SampleHeight(ref m_TerrainHeightData, position, out normal);
			elevation = 0f;
			if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
			{
				float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, position);
				num2 += placeableObjectData.m_PlacementOffset.y;
				elevation = math.max(0f, num2 - num);
				num = math.max(num, num2);
			}
			else if ((placeableObjectData.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
			{
				num += placeableObjectData.m_PlacementOffset.y;
			}
			else
			{
				float num3 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, position, out float waterDepth);
				if (waterDepth >= 0.2f)
				{
					num3 += placeableObjectData.m_PlacementOffset.y;
					if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.Floating) != Game.Objects.PlacementFlags.None)
					{
						elevation = math.max(0f, num3 - num);
					}
					num = math.max(num, num3);
				}
			}
			Game.Objects.Transform result = default(Game.Objects.Transform);
			result.m_Position = position;
			result.m_Position.y = num;
			result.m_Rotation = rotation;
			if ((m_Snap & Snap.Upright) == 0)
			{
				float3 forward = math.cross(math.right(), normal);
				result.m_Rotation = math.mul(quaternion.LookRotation(forward, normal), result.m_Rotation);
			}
			return result;
		}

		private bool CheckParentPrefab(Entity parentPrefab, Entity objectPrefab)
		{
			if (parentPrefab == objectPrefab)
			{
				return false;
			}
			if (m_PrefabSubObjects.HasBuffer(objectPrefab))
			{
				DynamicBuffer<Game.Prefabs.SubObject> dynamicBuffer = m_PrefabSubObjects[objectPrefab];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					if (!CheckParentPrefab(parentPrefab, dynamicBuffer[i].m_Prefab))
					{
						return false;
					}
				}
			}
			return true;
		}

		private void UpdateObject(Entity objectPrefab, Entity transformPrefab, Entity owner, Entity original, Entity parent, Entity updatedTopLevel, Entity lotEntity, Game.Objects.Transform transform, float elevation, OwnerDefinition ownerDefinition, ref NativeHashSet<Entity> attachedEntities, NativeList<ClearAreaData> clearAreas, bool upgrade, bool relocate, bool rebuild, bool topLevel, bool optional, int parentMesh, int randomIndex)
		{
			OwnerDefinition ownerDefinition2 = ownerDefinition;
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(randomIndex);
			bool flag = m_PrefabAssetStampData.HasComponent(objectPrefab);
			if (!flag || (!m_Stamping && ownerDefinition.m_Prefab == Entity.Null))
			{
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = objectPrefab,
					m_SubPrefab = transformPrefab,
					m_Owner = owner,
					m_Original = original,
					m_RandomSeed = random.NextInt()
				};
				if (optional)
				{
					component.m_Flags |= CreationFlags.Optional;
				}
				if (objectPrefab == Entity.Null && m_PrefabRefData.HasComponent(original))
				{
					objectPrefab = m_PrefabRefData[original].m_Prefab;
				}
				if (m_PrefabBuildingData.HasComponent(objectPrefab))
				{
					parentMesh = -1;
				}
				ObjectDefinition component2 = new ObjectDefinition
				{
					m_ParentMesh = parentMesh,
					m_Position = transform.m_Position,
					m_Rotation = transform.m_Rotation,
					m_Probability = 100,
					m_PrefabSubIndex = -1,
					m_Scale = 1f,
					m_Intensity = 1f
				};
				if (original == Entity.Null && transformPrefab != Entity.Null)
				{
					component2.m_GroupIndex = -1;
				}
				if (m_PrefabPlaceableObjectData.HasComponent(objectPrefab))
				{
					PlaceableObjectData placeableObjectData = m_PrefabPlaceableObjectData[objectPrefab];
					if ((placeableObjectData.m_Flags & Game.Objects.PlacementFlags.HasProbability) != Game.Objects.PlacementFlags.None)
					{
						component2.m_Probability = placeableObjectData.m_DefaultProbability;
					}
				}
				if (m_EditorContainerData.HasComponent(original))
				{
					EditorContainer editorContainer = m_EditorContainerData[original];
					component.m_SubPrefab = editorContainer.m_Prefab;
					component2.m_Scale = editorContainer.m_Scale;
					component2.m_Intensity = editorContainer.m_Intensity;
					component2.m_GroupIndex = editorContainer.m_GroupIndex;
				}
				if (m_LocalTransformCacheData.HasComponent(original))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[original];
					component2.m_Probability = localTransformCache.m_Probability;
					component2.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
				}
				if (parentMesh != -1)
				{
					component2.m_Elevation = transform.m_Position.y - ownerDefinition.m_Position.y;
				}
				else
				{
					component2.m_Elevation = elevation;
				}
				if (m_EditorMode)
				{
					component2.m_Age = random.NextFloat(1f);
				}
				else
				{
					component2.m_Age = ToolUtils.GetRandomAge(ref random, m_AgeMask);
				}
				if (ownerDefinition.m_Prefab != Entity.Null)
				{
					m_CommandBuffer.AddComponent(e, ownerDefinition);
					Game.Objects.Transform transform2 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(new Game.Objects.Transform(ownerDefinition.m_Position, ownerDefinition.m_Rotation)), transform);
					component2.m_LocalPosition = transform2.m_Position;
					component2.m_LocalRotation = transform2.m_Rotation;
				}
				else if (m_TransformData.HasComponent(owner))
				{
					Game.Objects.Transform transform3 = ObjectUtils.WorldToLocal(ObjectUtils.InverseTransform(m_TransformData[owner]), transform);
					component2.m_LocalPosition = transform3.m_Position;
					component2.m_LocalRotation = transform3.m_Rotation;
				}
				else
				{
					component2.m_LocalPosition = transform.m_Position;
					component2.m_LocalRotation = transform.m_Rotation;
				}
				Entity entity = Entity.Null;
				if (m_SubObjects.HasBuffer(parent))
				{
					BuildingData componentData;
					bool flag2 = m_PrefabBuildingData.TryGetComponent(objectPrefab, out componentData);
					if (!flag2)
					{
						component.m_Flags |= CreationFlags.Attach;
					}
					if (parentMesh == -1 && m_NetElevationData.HasComponent(parent))
					{
						component2.m_ParentMesh = 0;
						component2.m_Elevation = math.csum(m_NetElevationData[parent].m_Elevation) * 0.5f;
						if (IsLoweredParent(parent))
						{
							component.m_Flags |= CreationFlags.Lowered;
						}
					}
					if (flag2 || m_PrefabNetObjectData.HasComponent(objectPrefab))
					{
						bool extendNetUpdate = flag2 && (componentData.m_Flags & Game.Prefabs.BuildingFlags.CanBeOnRoadArea) == 0;
						entity = parent;
						UpdateAttachedParent(parent, original, updatedTopLevel, extendNetUpdate, ref attachedEntities);
					}
					else
					{
						component.m_Attached = parent;
					}
				}
				else if (m_PlaceholderBuildingData.HasComponent(parent))
				{
					component.m_Flags |= CreationFlags.Attach;
					component.m_Attached = parent;
				}
				if (m_AttachedData.HasComponent(original))
				{
					Attached attached = m_AttachedData[original];
					if (attached.m_Parent != entity)
					{
						UpdateAttachedParent(attached.m_Parent, original, updatedTopLevel, extendNetUpdate: false, ref attachedEntities);
					}
				}
				if (relocate && m_SubObjects.TryGetBuffer(original, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						if (m_AttachedData.TryGetComponent(bufferData[i].m_SubObject, out var componentData2))
						{
							UpdateAttachedParent(componentData2.m_Parent, original, updatedTopLevel, extendNetUpdate: false, ref attachedEntities);
						}
					}
				}
				if (upgrade)
				{
					component.m_Flags |= CreationFlags.Upgrade | CreationFlags.Parent;
				}
				if (relocate)
				{
					component.m_Flags |= CreationFlags.Relocate;
				}
				if (rebuild)
				{
					component.m_Flags |= CreationFlags.Repair;
				}
				ownerDefinition2.m_Prefab = objectPrefab;
				ownerDefinition2.m_Position = component2.m_Position;
				ownerDefinition2.m_Rotation = component2.m_Rotation;
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, component2);
				m_CommandBuffer.AddComponent(e, default(Updated));
			}
			else
			{
				if (m_PrefabSubObjects.HasBuffer(objectPrefab))
				{
					DynamicBuffer<Game.Prefabs.SubObject> dynamicBuffer = m_PrefabSubObjects[objectPrefab];
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Game.Prefabs.SubObject subObject = dynamicBuffer[j];
						Game.Objects.Transform transform4 = ObjectUtils.LocalToWorld(transform, subObject.m_Position, subObject.m_Rotation);
						UpdateObject(subObject.m_Prefab, Entity.Null, owner, Entity.Null, parent, updatedTopLevel, lotEntity, transform4, elevation, ownerDefinition, ref attachedEntities, default(NativeList<ClearAreaData>), upgrade: false, relocate: false, rebuild: false, topLevel: false, optional: false, parentMesh, j);
					}
				}
				original = Entity.Null;
				topLevel = true;
			}
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			Game.Objects.Transform mainInverseTransform = transform;
			if (original != Entity.Null)
			{
				mainInverseTransform = ObjectUtils.InverseTransform(m_TransformData[original]);
			}
			UpdateSubObjects(transform, transform, mainInverseTransform, objectPrefab, original, relocate, rebuild, topLevel, upgrade, ownerDefinition2, ref random, ref selectedSpawnables);
			UpdateSubNets(transform, transform, mainInverseTransform, objectPrefab, original, lotEntity, relocate, topLevel, flag && m_Stamping, ownerDefinition2, clearAreas, ref random);
			UpdateSubAreas(transform, objectPrefab, original, relocate, rebuild, topLevel, ownerDefinition2, clearAreas, ref random, ref selectedSpawnables);
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void UpdateAttachedParent(Entity parent, Entity original, Entity updatedTopLevel, bool extendNetUpdate, ref NativeHashSet<Entity> attachedEntities)
		{
			if (original != Entity.Null || updatedTopLevel != Entity.Null)
			{
				Entity entity = parent;
				if (entity == updatedTopLevel || entity == original)
				{
					return;
				}
				Owner componentData;
				while (m_OwnerData.TryGetComponent(entity, out componentData))
				{
					entity = componentData.m_Owner;
					if (entity == updatedTopLevel || entity == original)
					{
						return;
					}
				}
			}
			if (!attachedEntities.IsCreated)
			{
				attachedEntities = new NativeHashSet<Entity>(16, Allocator.Temp);
			}
			if (!attachedEntities.Add(parent))
			{
				return;
			}
			if (m_EdgeData.HasComponent(parent))
			{
				Edge edge = m_EdgeData[parent];
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Original = parent
				};
				component.m_Flags |= CreationFlags.Align;
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				NetCourse component2 = default(NetCourse);
				component2.m_Curve = m_CurveData[parent].m_Bezier;
				component2.m_Length = MathUtils.Length(component2.m_Curve);
				component2.m_FixedIndex = -1;
				component2.m_StartPosition.m_Entity = edge.m_Start;
				component2.m_StartPosition.m_Position = component2.m_Curve.a;
				component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve));
				component2.m_StartPosition.m_CourseDelta = 0f;
				component2.m_EndPosition.m_Entity = edge.m_End;
				component2.m_EndPosition.m_Position = component2.m_Curve.d;
				component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve));
				component2.m_EndPosition.m_CourseDelta = 1f;
				m_CommandBuffer.AddComponent(e, component2);
				if (!extendNetUpdate)
				{
					return;
				}
				if (m_ConnectedEdges.TryGetBuffer(edge.m_Start, out var bufferData))
				{
					for (int i = 0; i < bufferData.Length; i++)
					{
						if (bufferData[i].m_Edge != parent)
						{
							UpdateAttachedParent(bufferData[i].m_Edge, original, updatedTopLevel, extendNetUpdate: false, ref attachedEntities);
						}
					}
				}
				if (!m_ConnectedEdges.TryGetBuffer(edge.m_End, out bufferData))
				{
					return;
				}
				for (int j = 0; j < bufferData.Length; j++)
				{
					if (bufferData[j].m_Edge != parent)
					{
						UpdateAttachedParent(bufferData[j].m_Edge, original, updatedTopLevel, extendNetUpdate: false, ref attachedEntities);
					}
				}
			}
			else if (m_NodeData.HasComponent(parent))
			{
				Game.Net.Node node = m_NodeData[parent];
				Entity e2 = m_CommandBuffer.CreateEntity();
				CreationDefinition component3 = new CreationDefinition
				{
					m_Original = parent
				};
				m_CommandBuffer.AddComponent(e2, component3);
				m_CommandBuffer.AddComponent(e2, default(Updated));
				NetCourse component4 = new NetCourse
				{
					m_Curve = new Bezier4x3(node.m_Position, node.m_Position, node.m_Position, node.m_Position),
					m_Length = 0f,
					m_FixedIndex = -1,
					m_StartPosition = 
					{
						m_Entity = parent,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 0f
					},
					m_EndPosition = 
					{
						m_Entity = parent,
						m_Position = node.m_Position,
						m_Rotation = node.m_Rotation,
						m_CourseDelta = 1f
					}
				};
				m_CommandBuffer.AddComponent(e2, component4);
			}
		}

		private bool IsLoweredParent(Entity entity)
		{
			if (m_CompositionData.TryGetComponent(entity, out var componentData) && m_PrefabCompositionData.TryGetComponent(componentData.m_Edge, out var componentData2) && ((componentData2.m_Flags.m_Left | componentData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
			{
				return true;
			}
			if (m_OrphanData.TryGetComponent(entity, out var componentData3) && m_PrefabCompositionData.TryGetComponent(componentData3.m_Composition, out componentData2) && ((componentData2.m_Flags.m_Left | componentData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
			{
				return true;
			}
			if (m_ConnectedEdges.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					ConnectedEdge connectedEdge = bufferData[i];
					Edge edge = m_EdgeData[connectedEdge.m_Edge];
					if (edge.m_Start == entity)
					{
						if (m_CompositionData.TryGetComponent(connectedEdge.m_Edge, out componentData) && m_PrefabCompositionData.TryGetComponent(componentData.m_StartNode, out componentData2) && ((componentData2.m_Flags.m_Left | componentData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
						{
							return true;
						}
					}
					else if (edge.m_End == entity && m_CompositionData.TryGetComponent(connectedEdge.m_Edge, out componentData) && m_PrefabCompositionData.TryGetComponent(componentData.m_EndNode, out componentData2) && ((componentData2.m_Flags.m_Left | componentData2.m_Flags.m_Right) & CompositionFlags.Side.Lowered) != 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void UpdateSubObjects(Game.Objects.Transform transform, Game.Objects.Transform mainTransform, Game.Objects.Transform mainInverseTransform, Entity prefab, Entity original, bool relocate, bool rebuild, bool topLevel, bool isParent, OwnerDefinition ownerDefinition, ref Unity.Mathematics.Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			if (!m_InstalledUpgrades.HasBuffer(original) || !m_TransformData.HasComponent(original))
			{
				return;
			}
			Game.Objects.Transform inverseParentTransform = ObjectUtils.InverseTransform(m_TransformData[original]);
			DynamicBuffer<InstalledUpgrade> dynamicBuffer = m_InstalledUpgrades[original];
			Game.Objects.Transform transform2 = default(Game.Objects.Transform);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity upgrade = dynamicBuffer[i].m_Upgrade;
				if (upgrade == m_Original || !m_TransformData.HasComponent(upgrade))
				{
					continue;
				}
				Entity e = m_CommandBuffer.CreateEntity();
				CreationDefinition component = new CreationDefinition
				{
					m_Original = upgrade
				};
				if (relocate)
				{
					component.m_Flags |= CreationFlags.Relocate;
				}
				if (rebuild)
				{
					component.m_Flags |= CreationFlags.Repair;
				}
				if (isParent)
				{
					component.m_Flags |= CreationFlags.Parent;
					if (m_ObjectPrefab == Entity.Null)
					{
						component.m_Flags |= CreationFlags.Upgrade;
					}
				}
				m_CommandBuffer.AddComponent(e, component);
				m_CommandBuffer.AddComponent(e, default(Updated));
				if (ownerDefinition.m_Prefab != Entity.Null)
				{
					m_CommandBuffer.AddComponent(e, ownerDefinition);
				}
				ObjectDefinition component2 = new ObjectDefinition
				{
					m_Probability = 100,
					m_PrefabSubIndex = -1
				};
				if (m_LocalTransformCacheData.HasComponent(upgrade))
				{
					LocalTransformCache localTransformCache = m_LocalTransformCacheData[upgrade];
					component2.m_ParentMesh = localTransformCache.m_ParentMesh;
					component2.m_GroupIndex = localTransformCache.m_GroupIndex;
					component2.m_Probability = localTransformCache.m_Probability;
					component2.m_PrefabSubIndex = localTransformCache.m_PrefabSubIndex;
					transform2.m_Position = localTransformCache.m_Position;
					transform2.m_Rotation = localTransformCache.m_Rotation;
				}
				else
				{
					component2.m_ParentMesh = (m_BuildingData.HasComponent(upgrade) ? (-1) : 0);
					transform2 = ObjectUtils.WorldToLocal(inverseParentTransform, m_TransformData[upgrade]);
				}
				if (m_ElevationData.TryGetComponent(upgrade, out var componentData))
				{
					component2.m_Elevation = componentData.m_Elevation;
				}
				Game.Objects.Transform transform3 = ObjectUtils.LocalToWorld(transform, transform2);
				transform3.m_Rotation = math.normalize(transform3.m_Rotation);
				if (relocate && m_BuildingData.HasComponent(upgrade) && m_PrefabRefData.TryGetComponent(upgrade, out var componentData2) && m_PrefabPlaceableObjectData.TryGetComponent(componentData2.m_Prefab, out var componentData3))
				{
					float num = TerrainUtils.SampleHeight(ref m_TerrainHeightData, transform3.m_Position);
					if ((componentData3.m_Flags & Game.Objects.PlacementFlags.Hovering) != Game.Objects.PlacementFlags.None)
					{
						float num2 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, transform3.m_Position);
						num2 += componentData3.m_PlacementOffset.y;
						component2.m_Elevation = math.max(0f, num2 - num);
						num = math.max(num, num2);
					}
					else if ((componentData3.m_Flags & (Game.Objects.PlacementFlags.Shoreline | Game.Objects.PlacementFlags.Floating)) == 0)
					{
						num += componentData3.m_PlacementOffset.y;
					}
					else
					{
						float num3 = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, transform3.m_Position, out float waterDepth);
						if (waterDepth >= 0.2f)
						{
							num3 += componentData3.m_PlacementOffset.y;
							if ((componentData3.m_Flags & Game.Objects.PlacementFlags.Floating) != Game.Objects.PlacementFlags.None)
							{
								component2.m_Elevation = math.max(0f, num3 - num);
							}
							num = math.max(num, num3);
						}
					}
					transform3.m_Position.y = num;
				}
				component2.m_Position = transform3.m_Position;
				component2.m_Rotation = transform3.m_Rotation;
				component2.m_LocalPosition = transform2.m_Position;
				component2.m_LocalRotation = transform2.m_Rotation;
				m_CommandBuffer.AddComponent(e, component2);
				OwnerDefinition ownerDefinition2 = new OwnerDefinition
				{
					m_Prefab = m_PrefabRefData[upgrade].m_Prefab,
					m_Position = transform3.m_Position,
					m_Rotation = transform3.m_Rotation
				};
				UpdateSubNets(transform3, mainTransform, mainInverseTransform, ownerDefinition2.m_Prefab, upgrade, Entity.Null, relocate, topLevel: true, isStamping: false, ownerDefinition2, default(NativeList<ClearAreaData>), ref random);
				UpdateSubAreas(transform3, ownerDefinition2.m_Prefab, upgrade, relocate, rebuild, topLevel: true, ownerDefinition2, default(NativeList<ClearAreaData>), ref random, ref selectedSpawnables);
			}
		}

		private void CreateSubNet(Entity netPrefab, Entity lanePrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, Game.Objects.Transform parentTransform, OwnerDefinition ownerDefinition, NativeList<ClearAreaData> clearAreas, BuildingUtils.LotInfo lotInfo, bool hasLot, bool isStamping, ref Unity.Mathematics.Random random)
		{
			m_PrefabNetGeometryData.TryGetComponent(netPrefab, out var componentData);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_SubPrefab = lanePrefab,
				m_RandomSeed = random.NextInt()
			};
			bool flag = parentMesh.x >= 0 && parentMesh.y >= 0;
			NetCourse component2 = default(NetCourse);
			if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
			{
				curve.y = default(Bezier4x1);
				Curve curve2 = new Curve
				{
					m_Bezier = ObjectUtils.LocalToWorld(parentTransform.m_Position, parentTransform.m_Rotation, curve)
				};
				component2.m_Curve = NetUtils.AdjustPosition(curve2, fixedStart: false, linearMiddle: false, fixedEnd: false, ref m_TerrainHeightData, ref m_WaterSurfaceData).m_Bezier;
			}
			else if (!flag)
			{
				Curve curve3 = new Curve
				{
					m_Bezier = ObjectUtils.LocalToWorld(parentTransform.m_Position, parentTransform.m_Rotation, curve)
				};
				bool flag2 = parentMesh.x >= 0;
				bool flag3 = parentMesh.y >= 0;
				flag = flag2 || flag3;
				if ((componentData.m_Flags & Game.Net.GeometryFlags.FlattenTerrain) != 0)
				{
					if (hasLot)
					{
						component2.m_Curve = NetUtils.AdjustPosition(curve3, flag2, flag, flag3, ref lotInfo).m_Bezier;
						component2.m_Curve.a.y += curve.a.y;
						component2.m_Curve.b.y += curve.b.y;
						component2.m_Curve.c.y += curve.c.y;
						component2.m_Curve.d.y += curve.d.y;
					}
					else
					{
						component2.m_Curve = curve3.m_Bezier;
					}
				}
				else
				{
					component2.m_Curve = NetUtils.AdjustPosition(curve3, flag2, flag, flag3, ref m_TerrainHeightData).m_Bezier;
					component2.m_Curve.a.y += curve.a.y;
					component2.m_Curve.b.y += curve.b.y;
					component2.m_Curve.c.y += curve.c.y;
					component2.m_Curve.d.y += curve.d.y;
				}
			}
			else
			{
				component2.m_Curve = ObjectUtils.LocalToWorld(parentTransform.m_Position, parentTransform.m_Rotation, curve);
			}
			bool onGround = !flag || math.cmin(math.abs(curve.y.abcd)) < 2f;
			if (ClearAreaHelpers.ShouldClear(clearAreas, component2.m_Curve, onGround))
			{
				return;
			}
			if (isStamping)
			{
				component.m_Flags |= CreationFlags.Stamping;
			}
			Entity e = m_CommandBuffer.CreateEntity();
			m_CommandBuffer.AddComponent(e, component);
			m_CommandBuffer.AddComponent(e, default(Updated));
			if (ownerDefinition.m_Prefab != Entity.Null)
			{
				m_CommandBuffer.AddComponent(e, ownerDefinition);
			}
			component2.m_StartPosition.m_Position = component2.m_Curve.a;
			component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), parentTransform.m_Rotation);
			component2.m_StartPosition.m_CourseDelta = 0f;
			component2.m_StartPosition.m_Elevation = curve.a.y;
			component2.m_StartPosition.m_ParentMesh = parentMesh.x;
			if (nodeIndex.x >= 0)
			{
				if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
				{
					component2.m_StartPosition.m_Position.xz = ObjectUtils.LocalToWorld(parentTransform, nodePositions[nodeIndex.x].xyz).xz;
					component2.m_StartPosition.m_Position.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, component2.m_StartPosition.m_Position);
				}
				else
				{
					component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(parentTransform, nodePositions[nodeIndex.x].xyz);
				}
			}
			component2.m_EndPosition.m_Position = component2.m_Curve.d;
			component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), parentTransform.m_Rotation);
			component2.m_EndPosition.m_CourseDelta = 1f;
			component2.m_EndPosition.m_Elevation = curve.d.y;
			component2.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				if ((componentData.m_Flags & Game.Net.GeometryFlags.OnWater) != 0)
				{
					component2.m_EndPosition.m_Position.xz = ObjectUtils.LocalToWorld(parentTransform, nodePositions[nodeIndex.y].xyz).xz;
					component2.m_EndPosition.m_Position.y = WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, component2.m_EndPosition.m_Position);
				}
				else
				{
					component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(parentTransform, nodePositions[nodeIndex.y].xyz);
				}
			}
			component2.m_Length = MathUtils.Length(component2.m_Curve);
			component2.m_FixedIndex = -1;
			component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst;
			component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast;
			if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
			{
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			if (ownerDefinition.m_Prefab == Entity.Null)
			{
				component2.m_StartPosition.m_Flags |= CoursePosFlags.FreeHeight;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.FreeHeight;
			}
			m_CommandBuffer.AddComponent(e, component2);
			if (upgrades != default(CompositionFlags))
			{
				Upgraded component3 = new Upgraded
				{
					m_Flags = upgrades
				};
				m_CommandBuffer.AddComponent(e, component3);
			}
			if (m_EditorMode)
			{
				LocalCurveCache component4 = new LocalCurveCache
				{
					m_Curve = curve
				};
				m_CommandBuffer.AddComponent(e, component4);
			}
		}

		private bool GetOwnerLot(Entity lotOwner, out BuildingUtils.LotInfo lotInfo)
		{
			if (m_LotData.TryGetComponent(lotOwner, out var componentData) && m_TransformData.TryGetComponent(lotOwner, out var componentData2) && m_PrefabRefData.TryGetComponent(lotOwner, out var componentData3) && m_PrefabBuildingData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
			{
				float2 extents = new float2(componentData4.m_LotSize) * 4f;
				m_ElevationData.TryGetComponent(lotOwner, out var componentData5);
				m_InstalledUpgrades.TryGetBuffer(lotOwner, out var bufferData);
				lotInfo = BuildingUtils.CalculateLotInfo(extents, componentData2, componentData5, componentData, componentData3, bufferData, m_TransformData, m_PrefabRefData, m_PrefabObjectGeometryData, m_PrefabBuildingTerraformData, m_PrefabBuildingExtensionData, defaultNoSmooth: false, out var _);
				return true;
			}
			lotInfo = default(BuildingUtils.LotInfo);
			return false;
		}

		private void UpdateSubNets(Game.Objects.Transform transform, Game.Objects.Transform mainTransform, Game.Objects.Transform mainInverseTransform, Entity prefab, Entity original, Entity lotEntity, bool relocate, bool topLevel, bool isStamping, OwnerDefinition ownerDefinition, NativeList<ClearAreaData> clearAreas, ref Unity.Mathematics.Random random)
		{
			bool flag = original == Entity.Null || (relocate && m_EditorMode);
			if (flag && topLevel && m_PrefabSubNets.HasBuffer(prefab))
			{
				DynamicBuffer<Game.Prefabs.SubNet> subNets = m_PrefabSubNets[prefab];
				NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
				BuildingUtils.LotInfo lotInfo;
				bool ownerLot = GetOwnerLot(lotEntity, out lotInfo);
				for (int i = 0; i < subNets.Length; i++)
				{
					Game.Prefabs.SubNet subNet = subNets[i];
					if (subNet.m_NodeIndex.x >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.x)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subNet.m_NodeIndex.x] += new float4(subNet.m_Curve.a, 1f);
					}
					if (subNet.m_NodeIndex.y >= 0)
					{
						while (nodePositions.Length <= subNet.m_NodeIndex.y)
						{
							nodePositions.Add(default(float4));
						}
						nodePositions[subNet.m_NodeIndex.y] += new float4(subNet.m_Curve.d, 1f);
					}
				}
				for (int j = 0; j < nodePositions.Length; j++)
				{
					nodePositions[j] /= math.max(1f, nodePositions[j].w);
				}
				for (int k = 0; k < subNets.Length; k++)
				{
					Game.Prefabs.SubNet subNet2 = NetUtils.GetSubNet(subNets, k, m_LefthandTraffic, ref m_PrefabNetGeometryData);
					CreateSubNet(subNet2.m_Prefab, Entity.Null, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, transform, ownerDefinition, clearAreas, lotInfo, ownerLot, isStamping, ref random);
				}
				nodePositions.Dispose();
			}
			if (flag && topLevel && m_EditorMode && m_PrefabSubLanes.HasBuffer(prefab))
			{
				DynamicBuffer<Game.Prefabs.SubLane> dynamicBuffer = m_PrefabSubLanes[prefab];
				NativeList<float4> nodePositions2 = new NativeList<float4>(dynamicBuffer.Length * 2, Allocator.Temp);
				for (int l = 0; l < dynamicBuffer.Length; l++)
				{
					Game.Prefabs.SubLane subLane = dynamicBuffer[l];
					if (subLane.m_NodeIndex.x >= 0)
					{
						while (nodePositions2.Length <= subLane.m_NodeIndex.x)
						{
							nodePositions2.Add(default(float4));
						}
						nodePositions2[subLane.m_NodeIndex.x] += new float4(subLane.m_Curve.a, 1f);
					}
					if (subLane.m_NodeIndex.y >= 0)
					{
						while (nodePositions2.Length <= subLane.m_NodeIndex.y)
						{
							nodePositions2.Add(default(float4));
						}
						nodePositions2[subLane.m_NodeIndex.y] += new float4(subLane.m_Curve.d, 1f);
					}
				}
				for (int m = 0; m < nodePositions2.Length; m++)
				{
					nodePositions2[m] /= math.max(1f, nodePositions2[m].w);
				}
				for (int n = 0; n < dynamicBuffer.Length; n++)
				{
					Game.Prefabs.SubLane subLane2 = dynamicBuffer[n];
					CreateSubNet(m_LaneEditor, subLane2.m_Prefab, subLane2.m_Curve, subLane2.m_NodeIndex, subLane2.m_ParentMesh, default(CompositionFlags), nodePositions2, transform, ownerDefinition, clearAreas, default(BuildingUtils.LotInfo), hasLot: false, isStamping, ref random);
				}
				nodePositions2.Dispose();
			}
			if (!m_SubNets.HasBuffer(original))
			{
				return;
			}
			DynamicBuffer<Game.Net.SubNet> dynamicBuffer2 = m_SubNets[original];
			NativeHashMap<Entity, int> nativeHashMap = default(NativeHashMap<Entity, int>);
			NativeList<float4> nodePositions3 = default(NativeList<float4>);
			BuildingUtils.LotInfo lotInfo2 = default(BuildingUtils.LotInfo);
			bool hasLot = false;
			if (!flag && relocate)
			{
				nativeHashMap = new NativeHashMap<Entity, int>(dynamicBuffer2.Length, Allocator.Temp);
				nodePositions3 = new NativeList<float4>(dynamicBuffer2.Length, Allocator.Temp);
				hasLot = GetOwnerLot(lotEntity, out lotInfo2);
				for (int num = 0; num < dynamicBuffer2.Length; num++)
				{
					Entity subNet3 = dynamicBuffer2[num].m_SubNet;
					Edge componentData2;
					if (m_NodeData.TryGetComponent(subNet3, out var componentData))
					{
						if (nativeHashMap.TryAdd(subNet3, nodePositions3.Length))
						{
							componentData.m_Position = ObjectUtils.WorldToLocal(mainInverseTransform, componentData.m_Position);
							nodePositions3.Add(new float4(componentData.m_Position, 1f));
						}
					}
					else if (m_EdgeData.TryGetComponent(subNet3, out componentData2))
					{
						if (nativeHashMap.TryAdd(componentData2.m_Start, nodePositions3.Length))
						{
							componentData.m_Position = ObjectUtils.WorldToLocal(mainInverseTransform, m_NodeData[componentData2.m_Start].m_Position);
							nodePositions3.Add(new float4(componentData.m_Position, 1f));
						}
						if (nativeHashMap.TryAdd(componentData2.m_End, nodePositions3.Length))
						{
							componentData.m_Position = ObjectUtils.WorldToLocal(mainInverseTransform, m_NodeData[componentData2.m_End].m_Position);
							nodePositions3.Add(new float4(componentData.m_Position, 1f));
						}
					}
				}
			}
			for (int num2 = 0; num2 < dynamicBuffer2.Length; num2++)
			{
				Entity subNet4 = dynamicBuffer2[num2].m_SubNet;
				if (m_NodeData.TryGetComponent(subNet4, out var componentData3))
				{
					if (HasEdgeStartOrEnd(subNet4, original))
					{
						continue;
					}
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Original = subNet4
					};
					Game.Net.Elevation componentData4;
					bool flag2 = m_NetElevationData.TryGetComponent(subNet4, out componentData4);
					bool onGround = !flag2 || math.cmin(math.abs(componentData4.m_Elevation)) < 2f;
					if (flag || relocate || ClearAreaHelpers.ShouldClear(clearAreas, componentData3.m_Position, onGround))
					{
						component.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
					}
					else if (ownerDefinition.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(e, ownerDefinition);
					}
					if (m_EditorContainerData.HasComponent(subNet4))
					{
						component.m_SubPrefab = m_EditorContainerData[subNet4].m_Prefab;
					}
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, default(Updated));
					NetCourse component2 = new NetCourse
					{
						m_Curve = new Bezier4x3(componentData3.m_Position, componentData3.m_Position, componentData3.m_Position, componentData3.m_Position),
						m_Length = 0f,
						m_FixedIndex = -1,
						m_StartPosition = 
						{
							m_Entity = subNet4,
							m_Position = componentData3.m_Position,
							m_Rotation = componentData3.m_Rotation,
							m_CourseDelta = 0f,
							m_ParentMesh = -1
						},
						m_EndPosition = 
						{
							m_Entity = subNet4,
							m_Position = componentData3.m_Position,
							m_Rotation = componentData3.m_Rotation,
							m_CourseDelta = 1f,
							m_ParentMesh = -1
						}
					};
					m_CommandBuffer.AddComponent(e, component2);
					if (!flag && relocate)
					{
						Entity netPrefab = m_PrefabRefData[subNet4];
						componentData3.m_Position = ObjectUtils.WorldToLocal(mainInverseTransform, componentData3.m_Position);
						component2.m_Curve = new Bezier4x3(componentData3.m_Position, componentData3.m_Position, componentData3.m_Position, componentData3.m_Position);
						if (!flag2)
						{
							component2.m_Curve.y = default(Bezier4x1);
						}
						int num3 = nativeHashMap[subNet4];
						int num4 = ((!flag2) ? (-1) : 0);
						m_UpgradedData.TryGetComponent(subNet4, out var componentData5);
						CreateSubNet(netPrefab, component.m_SubPrefab, component2.m_Curve, num3, num4, componentData5.m_Flags, nodePositions3, mainTransform, ownerDefinition, clearAreas, lotInfo2, hasLot, isStamping: false, ref random);
					}
				}
				else
				{
					if (!m_EdgeData.TryGetComponent(subNet4, out var componentData6))
					{
						continue;
					}
					Entity e2 = m_CommandBuffer.CreateEntity();
					CreationDefinition component3 = new CreationDefinition
					{
						m_Original = subNet4
					};
					Curve curve = m_CurveData[subNet4];
					Game.Net.Elevation componentData7;
					bool flag3 = m_NetElevationData.TryGetComponent(subNet4, out componentData7);
					bool onGround2 = !flag3 || math.cmin(math.abs(componentData7.m_Elevation)) < 2f;
					if (flag || relocate || ClearAreaHelpers.ShouldClear(clearAreas, curve.m_Bezier, onGround2))
					{
						component3.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
					}
					else if (ownerDefinition.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(e2, ownerDefinition);
					}
					if (m_EditorContainerData.HasComponent(subNet4))
					{
						component3.m_SubPrefab = m_EditorContainerData[subNet4].m_Prefab;
					}
					m_CommandBuffer.AddComponent(e2, component3);
					m_CommandBuffer.AddComponent(e2, default(Updated));
					NetCourse component4 = default(NetCourse);
					component4.m_Curve = curve.m_Bezier;
					component4.m_Length = MathUtils.Length(component4.m_Curve);
					component4.m_FixedIndex = -1;
					component4.m_StartPosition.m_Entity = componentData6.m_Start;
					component4.m_StartPosition.m_Position = component4.m_Curve.a;
					component4.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component4.m_Curve));
					component4.m_StartPosition.m_CourseDelta = 0f;
					component4.m_StartPosition.m_ParentMesh = -1;
					component4.m_EndPosition.m_Entity = componentData6.m_End;
					component4.m_EndPosition.m_Position = component4.m_Curve.d;
					component4.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component4.m_Curve));
					component4.m_EndPosition.m_CourseDelta = 1f;
					component4.m_EndPosition.m_ParentMesh = -1;
					m_CommandBuffer.AddComponent(e2, component4);
					if (!flag && relocate)
					{
						Entity netPrefab2 = m_PrefabRefData[subNet4];
						component4.m_Curve.a = ObjectUtils.WorldToLocal(mainInverseTransform, component4.m_Curve.a);
						component4.m_Curve.b = ObjectUtils.WorldToLocal(mainInverseTransform, component4.m_Curve.b);
						component4.m_Curve.c = ObjectUtils.WorldToLocal(mainInverseTransform, component4.m_Curve.c);
						component4.m_Curve.d = ObjectUtils.WorldToLocal(mainInverseTransform, component4.m_Curve.d);
						if (!flag3)
						{
							component4.m_Curve.y = default(Bezier4x1);
						}
						int2 nodeIndex = new int2(nativeHashMap[componentData6.m_Start], nativeHashMap[componentData6.m_End]);
						int2 parentMesh = new int2((!m_NetElevationData.HasComponent(componentData6.m_Start)) ? (-1) : 0, (!m_NetElevationData.HasComponent(componentData6.m_End)) ? (-1) : 0);
						m_UpgradedData.TryGetComponent(subNet4, out var componentData8);
						if (m_CompositionData.TryGetComponent(subNet4, out var componentData9) && m_PrefabCompositionData.TryGetComponent(componentData9.m_Edge, out var componentData10))
						{
							componentData8.m_Flags.m_General |= componentData10.m_Flags.m_General & CompositionFlags.General.Elevated;
						}
						CreateSubNet(netPrefab2, component3.m_SubPrefab, component4.m_Curve, nodeIndex, parentMesh, componentData8.m_Flags, nodePositions3, mainTransform, ownerDefinition, clearAreas, lotInfo2, hasLot, isStamping: false, ref random);
					}
				}
			}
			if (nativeHashMap.IsCreated)
			{
				nativeHashMap.Dispose();
			}
			if (nodePositions3.IsCreated)
			{
				nodePositions3.Dispose();
			}
		}

		private void UpdateSubAreas(Game.Objects.Transform transform, Entity prefab, Entity original, bool relocate, bool rebuild, bool topLevel, OwnerDefinition ownerDefinition, NativeList<ClearAreaData> clearAreas, ref Unity.Mathematics.Random random, ref NativeParallelHashMap<Entity, int> selectedSpawnables)
		{
			bool flag = original == Entity.Null || relocate || rebuild;
			if (flag && topLevel && m_PrefabSubAreas.HasBuffer(prefab))
			{
				DynamicBuffer<Game.Prefabs.SubArea> dynamicBuffer = m_PrefabSubAreas[prefab];
				DynamicBuffer<SubAreaNode> dynamicBuffer2 = m_PrefabSubAreaNodes[prefab];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Game.Prefabs.SubArea subArea = dynamicBuffer[i];
					int seed;
					if (!m_EditorMode && m_PrefabPlaceholderElements.HasBuffer(subArea.m_Prefab))
					{
						DynamicBuffer<PlaceholderObjectElement> placeholderElements = m_PrefabPlaceholderElements[subArea.m_Prefab];
						if (!selectedSpawnables.IsCreated)
						{
							selectedSpawnables = new NativeParallelHashMap<Entity, int>(10, Allocator.Temp);
						}
						if (!AreaUtils.SelectAreaPrefab(placeholderElements, m_PrefabSpawnableObjectData, selectedSpawnables, ref random, out subArea.m_Prefab, out seed))
						{
							continue;
						}
					}
					else
					{
						seed = random.NextInt();
					}
					AreaGeometryData areaGeometryData = m_PrefabAreaGeometryData[subArea.m_Prefab];
					if (areaGeometryData.m_Type == AreaType.Space)
					{
						if (ClearAreaHelpers.ShouldClear(clearAreas, dynamicBuffer2, subArea.m_NodeRange, transform))
						{
							continue;
						}
					}
					else if (areaGeometryData.m_Type == AreaType.Lot && rebuild)
					{
						continue;
					}
					Entity e = m_CommandBuffer.CreateEntity();
					CreationDefinition component = new CreationDefinition
					{
						m_Prefab = subArea.m_Prefab,
						m_RandomSeed = seed
					};
					if (areaGeometryData.m_Type != AreaType.Lot)
					{
						component.m_Flags |= CreationFlags.Hidden;
					}
					m_CommandBuffer.AddComponent(e, component);
					m_CommandBuffer.AddComponent(e, default(Updated));
					if (ownerDefinition.m_Prefab != Entity.Null)
					{
						m_CommandBuffer.AddComponent(e, ownerDefinition);
					}
					DynamicBuffer<Game.Areas.Node> dynamicBuffer3 = m_CommandBuffer.AddBuffer<Game.Areas.Node>(e);
					dynamicBuffer3.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
					DynamicBuffer<LocalNodeCache> dynamicBuffer4 = default(DynamicBuffer<LocalNodeCache>);
					if (m_EditorMode)
					{
						dynamicBuffer4 = m_CommandBuffer.AddBuffer<LocalNodeCache>(e);
						dynamicBuffer4.ResizeUninitialized(dynamicBuffer3.Length);
					}
					int num = GetFirstNodeIndex(dynamicBuffer2, subArea.m_NodeRange);
					int num2 = 0;
					for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
					{
						float3 position = dynamicBuffer2[num].m_Position;
						float3 position2 = ObjectUtils.LocalToWorld(transform, position);
						int parentMesh = dynamicBuffer2[num].m_ParentMesh;
						float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
						dynamicBuffer3[num2] = new Game.Areas.Node(position2, elevation);
						if (m_EditorMode)
						{
							dynamicBuffer4[num2] = new LocalNodeCache
							{
								m_Position = position,
								m_ParentMesh = parentMesh
							};
						}
						num2++;
						if (++num == subArea.m_NodeRange.y)
						{
							num = subArea.m_NodeRange.x;
						}
					}
				}
			}
			if (!m_SubAreas.HasBuffer(original))
			{
				return;
			}
			DynamicBuffer<Game.Areas.SubArea> dynamicBuffer5 = m_SubAreas[original];
			for (int k = 0; k < dynamicBuffer5.Length; k++)
			{
				Entity area = dynamicBuffer5[k].m_Area;
				DynamicBuffer<Game.Areas.Node> nodes = m_AreaNodes[area];
				bool flag2 = flag;
				if (!flag2 && m_AreaSpaceData.HasComponent(area))
				{
					DynamicBuffer<Triangle> triangles = m_AreaTriangles[area];
					flag2 = ClearAreaHelpers.ShouldClear(clearAreas, nodes, triangles, transform);
				}
				if (m_AreaLotData.HasComponent(area))
				{
					if (!flag2)
					{
						continue;
					}
					flag2 = !rebuild;
				}
				Entity e2 = m_CommandBuffer.CreateEntity();
				CreationDefinition component2 = new CreationDefinition
				{
					m_Original = area
				};
				if (flag2)
				{
					component2.m_Flags |= CreationFlags.Delete | CreationFlags.Hidden;
				}
				else if (ownerDefinition.m_Prefab != Entity.Null)
				{
					m_CommandBuffer.AddComponent(e2, ownerDefinition);
				}
				m_CommandBuffer.AddComponent(e2, component2);
				m_CommandBuffer.AddComponent(e2, default(Updated));
				m_CommandBuffer.AddBuffer<Game.Areas.Node>(e2).CopyFrom(nodes.AsNativeArray());
				if (m_CachedNodes.HasBuffer(area))
				{
					DynamicBuffer<LocalNodeCache> dynamicBuffer6 = m_CachedNodes[area];
					m_CommandBuffer.AddBuffer<LocalNodeCache>(e2).CopyFrom(dynamicBuffer6.AsNativeArray());
				}
			}
		}

		private bool HasEdgeStartOrEnd(Entity node, Entity owner)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity edge = dynamicBuffer[i].m_Edge;
				Edge edge2 = m_EdgeData[edge];
				if ((edge2.m_Start == node || edge2.m_End == node) && m_OwnerData.HasComponent(edge) && m_OwnerData[edge].m_Owner == owner)
				{
					return true;
				}
			}
			return false;
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LocalTransformCache> __Game_Tools_LocalTransformCache_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Elevation> __Game_Objects_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Lot> __Game_Buildings_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.Elevation> __Game_Net_Elevation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Orphan> __Game_Net_Orphan_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Upgraded> __Game_Net_Upgraded_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Composition> __Game_Net_Composition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Clear> __Game_Areas_Clear_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Space> __Game_Areas_Space_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> __Game_Areas_Lot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EditorContainer> __Game_Tools_EditorContainer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetObjectData> __Game_Prefabs_NetObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AssetStampData> __Game_Prefabs_AssetStampData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingExtensionData> __Game_Prefabs_BuildingExtensionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BrushData> __Game_Prefabs_BrushData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingTerraformData> __Game_Prefabs_BuildingTerraformData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CreatureSpawnData> __Game_Prefabs_CreatureSpawnData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceholderBuildingData> __Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> __Game_Prefabs_NetCompositionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalNodeCache> __Game_Tools_LocalNodeCache_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> __Game_Net_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.Node> __Game_Areas_Node_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Triangle> __Game_Areas_Triangle_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubLane> __Game_Prefabs_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceUpgradeBuilding> __Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<BrushCell> __Game_Prefabs_BrushCell_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Tools_LocalTransformCache_RO_ComponentLookup = state.GetComponentLookup<LocalTransformCache>(isReadOnly: true);
			__Game_Objects_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Elevation>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Lot>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Node>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Elevation_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Elevation>(isReadOnly: true);
			__Game_Net_Orphan_RO_ComponentLookup = state.GetComponentLookup<Orphan>(isReadOnly: true);
			__Game_Net_Upgraded_RO_ComponentLookup = state.GetComponentLookup<Upgraded>(isReadOnly: true);
			__Game_Net_Composition_RO_ComponentLookup = state.GetComponentLookup<Composition>(isReadOnly: true);
			__Game_Areas_Clear_RO_ComponentLookup = state.GetComponentLookup<Clear>(isReadOnly: true);
			__Game_Areas_Space_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Space>(isReadOnly: true);
			__Game_Areas_Lot_RO_ComponentLookup = state.GetComponentLookup<Game.Areas.Lot>(isReadOnly: true);
			__Game_Tools_EditorContainer_RO_ComponentLookup = state.GetComponentLookup<EditorContainer>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_NetObjectData_RO_ComponentLookup = state.GetComponentLookup<NetObjectData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_AssetStampData_RO_ComponentLookup = state.GetComponentLookup<AssetStampData>(isReadOnly: true);
			__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup = state.GetComponentLookup<BuildingExtensionData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_BrushData_RO_ComponentLookup = state.GetComponentLookup<BrushData>(isReadOnly: true);
			__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup = state.GetComponentLookup<BuildingTerraformData>(isReadOnly: true);
			__Game_Prefabs_CreatureSpawnData_RO_ComponentLookup = state.GetComponentLookup<CreatureSpawnData>(isReadOnly: true);
			__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup = state.GetComponentLookup<PlaceholderBuildingData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetCompositionData_RO_ComponentLookup = state.GetComponentLookup<NetCompositionData>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Tools_LocalNodeCache_RO_BufferLookup = state.GetBufferLookup<LocalNodeCache>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferLookup = state.GetBufferLookup<InstalledUpgrade>(isReadOnly: true);
			__Game_Net_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubNet>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferLookup = state.GetBufferLookup<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferLookup = state.GetBufferLookup<Triangle>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubLane>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup = state.GetBufferLookup<ServiceUpgradeBuilding>(isReadOnly: true);
			__Game_Prefabs_BrushCell_RO_BufferLookup = state.GetBufferLookup<BrushCell>(isReadOnly: true);
		}
	}

	protected ToolOutputBarrier m_ToolOutputBarrier;

	protected Game.Objects.SearchSystem m_ObjectSearchSystem;

	protected WaterSystem m_WaterSystem;

	protected TerrainSystem m_TerrainSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ToolOutputBarrier = base.World.GetOrCreateSystemManaged<ToolOutputBarrier>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
	}

	protected JobHandle CreateDefinitions(Entity objectPrefab, Entity transformPrefab, Entity brushPrefab, Entity owner, Entity original, Entity laneEditor, Entity theme, NativeList<ControlPoint> controlPoints, NativeReference<AttachmentData> attachmentPrefab, bool editorMode, bool lefthandTraffic, bool removing, bool stamping, float brushSize, float brushAngle, float brushStrength, float distance, float deltaTime, RandomSeed randomSeed, Snap snap, AgeMask ageMask, JobHandle inputDeps)
	{
		JobHandle dependencies;
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new CreateDefinitionsJob
		{
			m_EditorMode = editorMode,
			m_LefthandTraffic = lefthandTraffic,
			m_Removing = removing,
			m_Stamping = stamping,
			m_BrushSize = brushSize,
			m_BrushAngle = brushAngle,
			m_BrushStrength = brushStrength,
			m_Distance = distance,
			m_DeltaTime = deltaTime,
			m_ObjectPrefab = objectPrefab,
			m_TransformPrefab = transformPrefab,
			m_BrushPrefab = brushPrefab,
			m_Owner = owner,
			m_Original = original,
			m_LaneEditor = laneEditor,
			m_Theme = theme,
			m_RandomSeed = randomSeed,
			m_Snap = snap,
			m_AgeMask = ageMask,
			m_ControlPoints = controlPoints,
			m_AttachmentPrefab = attachmentPrefab,
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LocalTransformCacheData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_LocalTransformCache_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetElevationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Elevation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OrphanData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Orphan_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UpgradedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Upgraded_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Composition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaClearData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Clear_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaSpaceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Space_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AreaLotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_Lot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EditorContainerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_EditorContainer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAssetStampData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AssetStampData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingExtensionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingExtensionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBrushData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BrushData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingTerraformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingTerraformData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCreatureSpawnData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CreatureSpawnData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceholderBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCompositionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetCompositionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CachedNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Tools_LocalNodeCache_RO_BufferLookup, ref base.CheckedStateRef),
			m_InstalledUpgrades = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Node_RO_BufferLookup, ref base.CheckedStateRef),
			m_AreaTriangles = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabRequirementElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabServiceUpgradeBuilding = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabBrushCells = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_BrushCell_RO_BufferLookup, ref base.CheckedStateRef),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_CommandBuffer = m_ToolOutputBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(inputDeps, dependencies, deps));
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_ToolOutputBarrier.AddJobHandleForProducer(jobHandle);
		return jobHandle;
	}

	public static int GetFirstNodeIndex(DynamicBuffer<SubAreaNode> nodes, int2 range)
	{
		int result = 0;
		float num = float.MaxValue;
		for (int i = range.x; i < range.y; i++)
		{
			int index = math.select(i + 1, range.x, i + 1 == range.y);
			float t;
			float num2 = MathUtils.Distance(new Line2.Segment(nodes[i].m_Position.xz, nodes[index].m_Position.xz), default(float2), out t);
			if (num2 < num)
			{
				result = i;
				num = num2;
			}
		}
		return result;
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
	protected ObjectToolBaseSystem()
	{
	}
}
