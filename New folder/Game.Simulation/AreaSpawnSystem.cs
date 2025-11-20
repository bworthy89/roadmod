using System;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Tools;
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
public class AreaSpawnSystem : GameSystemBase
{
	[BurstCompile]
	private struct AreaSpawnJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Area> m_AreaType;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> m_GeometryType;

		[ReadOnly]
		public ComponentTypeHandle<Storage> m_StorageType;

		[ReadOnly]
		public ComponentTypeHandle<Extractor> m_ExtractorType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> m_NodeType;

		[ReadOnly]
		public BufferTypeHandle<Triangle> m_TriangleType;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> m_SubObjectType;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<CompanyData> m_CompanyData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaGeometryData;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> m_PrefabStorageAreaData;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> m_PrefabExtractorAreaData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> m_PrefabSpawnableObjectData;

		[ReadOnly]
		public ComponentLookup<BuildingData> m_PrefabBuildingData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> m_SubAreas;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public BufferLookup<Renter> m_BuildingRenters;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> m_PrefabSubNets;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> m_PrefabSubAreas;

		[ReadOnly]
		public BufferLookup<SubAreaNode> m_PrefabSubAreaNodes;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PlaceholderObjectElements;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> m_ObjectRequirements;

		[ReadOnly]
		public bool m_LefthandTraffic;

		[ReadOnly]
		public bool m_DebugFastSpawn;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_DefinitionArchetype;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeList<AreaUtils.ObjectItem> objects = default(NativeList<AreaUtils.ObjectItem>);
			NativeParallelHashSet<Entity> placeholderRequirements = default(NativeParallelHashSet<Entity>);
			NativeArray<Storage> nativeArray = chunk.GetNativeArray(ref m_StorageType);
			NativeArray<Extractor> nativeArray2 = chunk.GetNativeArray(ref m_ExtractorType);
			if (nativeArray.Length != 0 || nativeArray2.Length != 0)
			{
				NativeArray<Entity> nativeArray3 = chunk.GetNativeArray(m_EntityType);
				NativeArray<Area> nativeArray4 = chunk.GetNativeArray(ref m_AreaType);
				NativeArray<Geometry> nativeArray5 = chunk.GetNativeArray(ref m_GeometryType);
				NativeArray<Owner> nativeArray6 = chunk.GetNativeArray(ref m_OwnerType);
				NativeArray<PrefabRef> nativeArray7 = chunk.GetNativeArray(ref m_PrefabRefType);
				BufferAccessor<Game.Areas.Node> bufferAccessor = chunk.GetBufferAccessor(ref m_NodeType);
				BufferAccessor<Triangle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_TriangleType);
				BufferAccessor<Game.Objects.SubObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubObjectType);
				for (int i = 0; i < nativeArray3.Length; i++)
				{
					Geometry geometry = nativeArray5[i];
					PrefabRef prefabRef = nativeArray7[i];
					DynamicBuffer<Game.Objects.SubObject> dynamicBuffer = bufferAccessor3[i];
					AreaGeometryData areaData = m_PrefabAreaGeometryData[prefabRef.m_Prefab];
					float num = 0f;
					if (nativeArray.Length != 0)
					{
						Storage storage = nativeArray[i];
						StorageAreaData prefabStorageData = m_PrefabStorageAreaData[prefabRef.m_Prefab];
						if (m_DebugFastSpawn)
						{
							storage.m_Amount = AreaUtils.CalculateStorageCapacity(geometry, prefabStorageData);
						}
						num = math.max(num, AreaUtils.CalculateStorageObjectArea(geometry, storage, prefabStorageData));
					}
					if (nativeArray2.Length != 0)
					{
						Extractor extractor = nativeArray2[i];
						ExtractorAreaData extractorAreaData = m_PrefabExtractorAreaData[prefabRef.m_Prefab];
						if (m_DebugFastSpawn)
						{
							extractor.m_TotalExtracted = extractor.m_ResourceAmount;
						}
						num = math.max(num, AreaUtils.CalculateExtractorObjectArea(geometry, extractor, extractorAreaData));
					}
					if (num < 1f)
					{
						continue;
					}
					if (!objects.IsCreated && dynamicBuffer.Length > 0)
					{
						objects = new NativeList<AreaUtils.ObjectItem>(dynamicBuffer.Length, Allocator.Temp);
					}
					float num2 = 0f;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						Entity subObject = dynamicBuffer[j].m_SubObject;
						if (m_SecondaryData.HasComponent(subObject))
						{
							continue;
						}
						PrefabRef prefabRef2 = m_PrefabRefData[subObject];
						if (m_PrefabObjectGeometryData.TryGetComponent(prefabRef2.m_Prefab, out var componentData))
						{
							Transform transform = m_TransformData[subObject];
							float num3;
							if ((componentData.m_Flags & Game.Objects.GeometryFlags.Circular) != Game.Objects.GeometryFlags.None)
							{
								num3 = componentData.m_Size.x * 0.5f;
							}
							else
							{
								num3 = math.length(MathUtils.Size(componentData.m_Bounds.xz)) * 0.5f;
								transform.m_Position.xz -= math.rotate(transform.m_Rotation, MathUtils.Center(componentData.m_Bounds)).xz;
							}
							if (m_PrefabBuildingData.HasComponent(prefabRef2.m_Prefab))
							{
								num3 += AreaUtils.GetMinNodeDistance(areaData);
							}
							num2 += num3 * num3 * MathF.PI;
							objects.Add(new AreaUtils.ObjectItem(num3, transform.m_Position.xz, subObject));
						}
					}
					if (num2 >= num)
					{
						continue;
					}
					if (m_PrefabSubObjects.HasBuffer(prefabRef.m_Prefab))
					{
						DynamicBuffer<Game.Prefabs.SubObject> prefabSubObjects = m_PrefabSubObjects[prefabRef.m_Prefab];
						Owner owner = default(Owner);
						if (nativeArray6.Length != 0)
						{
							owner = nativeArray6[i];
						}
						if (TryGetObjectPrefab(ref random, ref placeholderRequirements, owner, prefabSubObjects, out var prefab))
						{
							Area area = nativeArray4[i];
							DynamicBuffer<Game.Areas.Node> nodes = bufferAccessor[i];
							DynamicBuffer<Triangle> triangles = bufferAccessor2[i];
							ObjectGeometryData objectGeometryData = default(ObjectGeometryData);
							if (m_PrefabObjectGeometryData.HasComponent(prefab))
							{
								objectGeometryData = m_PrefabObjectGeometryData[prefab];
							}
							float extraRadius = 0f;
							bool flag = false;
							if (m_PrefabBuildingData.HasComponent(prefab))
							{
								extraRadius = AreaUtils.GetMinNodeDistance(areaData);
								flag = true;
							}
							if (AreaUtils.TryGetRandomObjectLocation(ref random, objectGeometryData, area, geometry, extraRadius, nodes, triangles, objects, out var transform2))
							{
								Entity entity = nativeArray3[i];
								Game.Objects.Elevation elevation = new Game.Objects.Elevation(0f, ElevationFlags.OnGround);
								transform2 = ObjectUtils.AdjustPosition(transform2, ref elevation, prefab, out var _, ref m_TerrainHeightData, ref m_WaterSurfaceData, ref m_PrefabPlaceableObjectData, ref m_PrefabObjectGeometryData);
								SpawnObject(unfilteredChunkIndex, entity, prefab, transform2, elevation, ref random);
								if (objects.IsCreated && objects.Length != 0)
								{
									for (int k = 0; k < objects.Length; k++)
									{
										Entity entity2 = objects[k].m_Entity;
										PrefabRef prefabRef3 = m_PrefabRefData[entity2];
										if (m_PrefabBuildingData.HasComponent(prefabRef3.m_Prefab))
										{
											objects.RemoveAtSwapBack(k--);
											m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity2, default(Deleted));
											flag = true;
										}
									}
									if (objects.Length != 0)
									{
										m_CommandBuffer.AddComponent(unfilteredChunkIndex, entity, default(Updated));
									}
								}
								if (flag && m_SubAreas.TryGetBuffer(entity, out var bufferData))
								{
									for (int l = 0; l < bufferData.Length; l++)
									{
										m_CommandBuffer.AddComponent(unfilteredChunkIndex, bufferData[l].m_Area, default(Updated));
									}
								}
							}
						}
					}
					if (objects.IsCreated)
					{
						objects.Clear();
					}
				}
			}
			if (objects.IsCreated)
			{
				objects.Dispose();
			}
			if (placeholderRequirements.IsCreated)
			{
				placeholderRequirements.Dispose();
			}
		}

		private bool TryGetObjectPrefab(ref Unity.Mathematics.Random random, ref NativeParallelHashSet<Entity> placeholderRequirements, Owner owner, DynamicBuffer<Game.Prefabs.SubObject> prefabSubObjects, out Entity prefab)
		{
			prefab = Entity.Null;
			int num = 0;
			for (int i = 0; i < prefabSubObjects.Length; i++)
			{
				if ((prefabSubObjects[i].m_Flags & SubObjectFlags.EdgePlacement) == 0)
				{
					num++;
				}
			}
			if (num == 0)
			{
				return false;
			}
			num = random.NextInt(num);
			for (int j = 0; j < prefabSubObjects.Length; j++)
			{
				Game.Prefabs.SubObject subObject = prefabSubObjects[j];
				if ((subObject.m_Flags & SubObjectFlags.EdgePlacement) == 0 && num-- == 0)
				{
					prefab = subObject.m_Prefab;
					break;
				}
			}
			return TryGetObjectPrefab(ref random, ref prefab, ref placeholderRequirements, owner);
		}

		private bool TryGetObjectPrefab(ref Unity.Mathematics.Random random, ref Entity prefab, ref NativeParallelHashSet<Entity> placeholderRequirements, Owner owner)
		{
			if (!m_PlaceholderObjectElements.HasBuffer(prefab))
			{
				return true;
			}
			DynamicBuffer<PlaceholderObjectElement> dynamicBuffer = m_PlaceholderObjectElements[prefab];
			int num = 0;
			bool flag = false;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity entity = dynamicBuffer[i].m_Object;
				if (m_ObjectRequirements.TryGetBuffer(entity, out var bufferData))
				{
					if (!flag)
					{
						flag = true;
						FillRequirements(ref placeholderRequirements, owner);
					}
					int num2 = -1;
					bool flag2 = true;
					for (int j = 0; j < bufferData.Length; j++)
					{
						ObjectRequirementElement objectRequirementElement = bufferData[j];
						if (objectRequirementElement.m_Group != num2)
						{
							if (!flag2)
							{
								break;
							}
							num2 = objectRequirementElement.m_Group;
							flag2 = false;
						}
						flag2 |= placeholderRequirements.Contains(objectRequirementElement.m_Requirement);
					}
					if (!flag2)
					{
						continue;
					}
				}
				int probability = m_PrefabSpawnableObjectData[entity].m_Probability;
				num += probability;
				if (random.NextInt(num) < probability)
				{
					prefab = entity;
				}
			}
			return random.NextInt(100) < num;
		}

		private void FillRequirements(ref NativeParallelHashSet<Entity> placeholderRequirements, Owner owner)
		{
			if (placeholderRequirements.IsCreated)
			{
				placeholderRequirements.Clear();
			}
			else
			{
				placeholderRequirements = new NativeParallelHashSet<Entity>(10, Allocator.Temp);
			}
			if (m_OwnerData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				owner = componentData;
			}
			Entity entity = owner.m_Owner;
			if (m_AttachmentData.TryGetComponent(owner.m_Owner, out var componentData2))
			{
				entity = componentData2.m_Attached;
			}
			if (!m_BuildingRenters.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity renter = bufferData[i].m_Renter;
				if (m_CompanyData.HasComponent(renter))
				{
					Entity prefab = m_PrefabRefData[renter].m_Prefab;
					Entity brand = m_CompanyData[renter].m_Brand;
					if (brand != Entity.Null)
					{
						placeholderRequirements.Add(brand);
					}
					placeholderRequirements.Add(prefab);
				}
			}
		}

		private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubArea> subAreas, DynamicBuffer<SubAreaNode> subAreaNodes, ref Unity.Mathematics.Random random)
		{
			NativeParallelHashMap<Entity, int> selectedSpawnables = default(NativeParallelHashMap<Entity, int>);
			for (int i = 0; i < subAreas.Length; i++)
			{
				Game.Prefabs.SubArea subArea = subAreas[i];
				int seed;
				if (m_PlaceholderObjectElements.HasBuffer(subArea.m_Prefab))
				{
					DynamicBuffer<PlaceholderObjectElement> placeholderElements = m_PlaceholderObjectElements[subArea.m_Prefab];
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
				Entity e = m_CommandBuffer.CreateEntity(jobIndex);
				CreationDefinition component = new CreationDefinition
				{
					m_Prefab = subArea.m_Prefab,
					m_RandomSeed = seed
				};
				component.m_Flags |= CreationFlags.Permanent;
				m_CommandBuffer.AddComponent(jobIndex, e, component);
				m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
				m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
				DynamicBuffer<Game.Areas.Node> dynamicBuffer = m_CommandBuffer.AddBuffer<Game.Areas.Node>(jobIndex, e);
				dynamicBuffer.ResizeUninitialized(subArea.m_NodeRange.y - subArea.m_NodeRange.x + 1);
				int num = ObjectToolBaseSystem.GetFirstNodeIndex(subAreaNodes, subArea.m_NodeRange);
				int num2 = 0;
				for (int j = subArea.m_NodeRange.x; j <= subArea.m_NodeRange.y; j++)
				{
					float3 position = subAreaNodes[num].m_Position;
					float3 position2 = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, position);
					int parentMesh = subAreaNodes[num].m_ParentMesh;
					float elevation = math.select(float.MinValue, position.y, parentMesh >= 0);
					dynamicBuffer[num2] = new Game.Areas.Node(position2, elevation);
					num2++;
					if (++num == subArea.m_NodeRange.y)
					{
						num = subArea.m_NodeRange.x;
					}
				}
			}
			if (selectedSpawnables.IsCreated)
			{
				selectedSpawnables.Dispose();
			}
		}

		private void Spawn(int jobIndex, OwnerDefinition ownerDefinition, DynamicBuffer<Game.Prefabs.SubNet> subNets, ref Unity.Mathematics.Random random)
		{
			NativeList<float4> nodePositions = new NativeList<float4>(subNets.Length * 2, Allocator.Temp);
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
				CreateSubNet(jobIndex, subNet2.m_Prefab, subNet2.m_Curve, subNet2.m_NodeIndex, subNet2.m_ParentMesh, subNet2.m_Upgrades, nodePositions, ownerDefinition, ref random);
			}
			nodePositions.Dispose();
		}

		private void CreateSubNet(int jobIndex, Entity netPrefab, Bezier4x3 curve, int2 nodeIndex, int2 parentMesh, CompositionFlags upgrades, NativeList<float4> nodePositions, OwnerDefinition ownerDefinition, ref Unity.Mathematics.Random random)
		{
			Entity e = m_CommandBuffer.CreateEntity(jobIndex);
			CreationDefinition component = new CreationDefinition
			{
				m_Prefab = netPrefab,
				m_RandomSeed = random.NextInt()
			};
			component.m_Flags |= CreationFlags.Permanent;
			m_CommandBuffer.AddComponent(jobIndex, e, component);
			m_CommandBuffer.AddComponent(jobIndex, e, default(Updated));
			m_CommandBuffer.AddComponent(jobIndex, e, ownerDefinition);
			NetCourse component2 = default(NetCourse);
			component2.m_Curve = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, curve);
			component2.m_StartPosition.m_Position = component2.m_Curve.a;
			component2.m_StartPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.StartTangent(component2.m_Curve), ownerDefinition.m_Rotation);
			component2.m_StartPosition.m_CourseDelta = 0f;
			component2.m_StartPosition.m_Elevation = curve.a.y;
			component2.m_StartPosition.m_ParentMesh = parentMesh.x;
			if (nodeIndex.x >= 0)
			{
				component2.m_StartPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.x].xyz);
			}
			component2.m_EndPosition.m_Position = component2.m_Curve.d;
			component2.m_EndPosition.m_Rotation = NetUtils.GetNodeRotation(MathUtils.EndTangent(component2.m_Curve), ownerDefinition.m_Rotation);
			component2.m_EndPosition.m_CourseDelta = 1f;
			component2.m_EndPosition.m_Elevation = curve.d.y;
			component2.m_EndPosition.m_ParentMesh = parentMesh.y;
			if (nodeIndex.y >= 0)
			{
				component2.m_EndPosition.m_Position = ObjectUtils.LocalToWorld(ownerDefinition.m_Position, ownerDefinition.m_Rotation, nodePositions[nodeIndex.y].xyz);
			}
			component2.m_Length = MathUtils.Length(component2.m_Curve);
			component2.m_FixedIndex = -1;
			component2.m_StartPosition.m_Flags |= CoursePosFlags.IsFirst | CoursePosFlags.DisableMerge;
			component2.m_EndPosition.m_Flags |= CoursePosFlags.IsLast | CoursePosFlags.DisableMerge;
			if (component2.m_StartPosition.m_Position.Equals(component2.m_EndPosition.m_Position))
			{
				component2.m_StartPosition.m_Flags |= CoursePosFlags.IsLast;
				component2.m_EndPosition.m_Flags |= CoursePosFlags.IsFirst;
			}
			m_CommandBuffer.AddComponent(jobIndex, e, component2);
			if (upgrades != default(CompositionFlags))
			{
				Upgraded component3 = new Upgraded
				{
					m_Flags = upgrades
				};
				m_CommandBuffer.AddComponent(jobIndex, e, component3);
			}
		}

		private void SpawnObject(int jobIndex, Entity entity, Entity prefab, Transform transform, Game.Objects.Elevation elevation, ref Unity.Mathematics.Random random)
		{
			CreationDefinition component = new CreationDefinition
			{
				m_Owner = entity,
				m_Prefab = prefab
			};
			component.m_Flags |= CreationFlags.Permanent;
			component.m_RandomSeed = random.NextInt();
			ObjectDefinition component2 = new ObjectDefinition
			{
				m_ParentMesh = -1,
				m_Elevation = elevation.m_Elevation,
				m_Position = transform.m_Position,
				m_Rotation = transform.m_Rotation,
				m_LocalPosition = transform.m_Position,
				m_LocalRotation = transform.m_Rotation
			};
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_DefinitionArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, component);
			m_CommandBuffer.SetComponent(jobIndex, e, component2);
			OwnerDefinition ownerDefinition = new OwnerDefinition
			{
				m_Prefab = prefab,
				m_Position = component2.m_Position,
				m_Rotation = component2.m_Rotation
			};
			if (m_PrefabSubAreas.HasBuffer(prefab))
			{
				Spawn(jobIndex, ownerDefinition, m_PrefabSubAreas[prefab], m_PrefabSubAreaNodes[prefab], ref random);
			}
			if (m_PrefabSubNets.HasBuffer(prefab))
			{
				Spawn(jobIndex, ownerDefinition, m_PrefabSubNets[prefab], ref random);
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
		public ComponentTypeHandle<Area> __Game_Areas_Area_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> __Game_Areas_Geometry_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Storage> __Game_Areas_Storage_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Extractor> __Game_Areas_Extractor_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Areas.Node> __Game_Areas_Node_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Triangle> __Game_Areas_Triangle_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attachment> __Game_Objects_Attachment_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Secondary> __Game_Objects_Secondary_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CompanyData> __Game_Companies_CompanyData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StorageAreaData> __Game_Prefabs_StorageAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> __Game_Prefabs_AreaGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableObjectData> __Game_Prefabs_SpawnableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<BuildingData> __Game_Prefabs_BuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Areas.SubArea> __Game_Areas_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubNet> __Game_Prefabs_SubNet_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubAreaNode> __Game_Prefabs_SubAreaNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ObjectRequirementElement> __Game_Prefabs_ObjectRequirementElement_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_Area_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Area>(isReadOnly: true);
			__Game_Areas_Geometry_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Geometry>(isReadOnly: true);
			__Game_Areas_Storage_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Storage>(isReadOnly: true);
			__Game_Areas_Extractor_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Extractor>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Areas_Node_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Areas.Node>(isReadOnly: true);
			__Game_Areas_Triangle_RO_BufferTypeHandle = state.GetBufferTypeHandle<Triangle>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_Attachment_RO_ComponentLookup = state.GetComponentLookup<Attachment>(isReadOnly: true);
			__Game_Objects_Secondary_RO_ComponentLookup = state.GetComponentLookup<Secondary>(isReadOnly: true);
			__Game_Companies_CompanyData_RO_ComponentLookup = state.GetComponentLookup<CompanyData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_StorageAreaData_RO_ComponentLookup = state.GetComponentLookup<StorageAreaData>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup = state.GetComponentLookup<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_AreaGeometryData_RO_ComponentLookup = state.GetComponentLookup<AreaGeometryData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup = state.GetComponentLookup<SpawnableObjectData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Prefabs_BuildingData_RO_ComponentLookup = state.GetComponentLookup<BuildingData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Areas_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Areas.SubArea>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_SubNet_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubNet>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubArea>(isReadOnly: true);
			__Game_Prefabs_SubAreaNode_RO_BufferLookup = state.GetBufferLookup<SubAreaNode>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup = state.GetBufferLookup<ObjectRequirementElement>(isReadOnly: true);
		}
	}

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_AreaQuery;

	private EntityArchetype m_DefinitionArchetype;

	private TypeHandle __TypeHandle;

	public bool debugFastSpawn { get; set; }

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 64;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_AreaQuery = GetEntityQuery(ComponentType.ReadOnly<Area>(), ComponentType.ReadOnly<Geometry>(), ComponentType.ReadOnly<Game.Objects.SubObject>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>());
		m_DefinitionArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<CreationDefinition>(), ComponentType.ReadWrite<ObjectDefinition>(), ComponentType.ReadWrite<Updated>(), ComponentType.ReadWrite<Deleted>());
		RequireForUpdate(m_AreaQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle deps;
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new AreaSpawnJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Area_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GeometryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Geometry_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StorageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Storage_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_Extractor_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NodeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Node_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_TriangleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Areas_Triangle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachmentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attachment_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SecondaryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Secondary_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CompanyData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Companies_CompanyData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStorageAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StorageAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabExtractorAreaData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAreaGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AreaGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_BuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_BuildingRenters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubNets = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubNet_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubAreaNodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubAreaNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_PlaceholderObjectElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ObjectRequirements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ObjectRequirementElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_DebugFastSpawn = debugFastSpawn,
			m_LefthandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_RandomSeed = RandomSeed.Next(),
			m_DefinitionArchetype = m_DefinitionArchetype,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter()
		}, m_AreaQuery, JobHandle.CombineDependencies(base.Dependency, deps));
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
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
	public AreaSpawnSystem()
	{
	}
}
