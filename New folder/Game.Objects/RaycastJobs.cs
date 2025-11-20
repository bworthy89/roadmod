using Colossal.Collections;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Objects;

public static class RaycastJobs
{
	[BurstCompile]
	public struct RaycastStaticObjectsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeArray<RaycastSystem.EntityResult> m_Objects;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeArray<RaycastResult> m_TerrainResults;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Elevation> m_ElevationData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<NetObject> m_NetObjectData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<Secondary> m_SecondaryData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<Game.Tools.EditorContainer> m_EditorContainerData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> m_PrefabGrowthScaleData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Overridden> m_OverriddenData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<Orphan> m_OrphanData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<Game.Areas.Lot> m_LotAreaData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubNet> m_SubNets;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<NetData> m_PrefabNetData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<ImpostorData> m_PrefabImpostorData;

		[ReadOnly]
		public ComponentLookup<SharedMeshData> m_PrefabSharedMeshData;

		[ReadOnly]
		public BufferLookup<SubMesh> m_Meshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<LodMesh> m_Lods;

		[ReadOnly]
		public BufferLookup<MeshVertex> m_Vertices;

		[ReadOnly]
		public BufferLookup<MeshIndex> m_Indices;

		[ReadOnly]
		public BufferLookup<MeshNode> m_Nodes;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastSystem.EntityResult entityResult = m_Objects[index];
			RaycastInput input = m_Input[entityResult.m_RaycastIndex];
			if ((input.m_TypeMask & (TypeMask.StaticObjects | TypeMask.Net)) == 0 || m_OverriddenData.HasComponent(entityResult.m_Entity) || !IsNearCamera(entityResult.m_Entity) || ((input.m_Flags & RaycastFlags.IgnoreSecondary) != 0 && m_SecondaryData.HasComponent(entityResult.m_Entity)))
			{
				return;
			}
			Transform transform = m_TransformData[entityResult.m_Entity];
			PrefabRef prefabRef = m_PrefabRefData[entityResult.m_Entity];
			Line3.Segment segment = input.m_Line + input.m_Offset;
			bool flag = false;
			if (m_PrefabObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				if ((componentData.m_Flags & GeometryFlags.Marker) != GeometryFlags.None)
				{
					if ((input.m_Flags & RaycastFlags.OutsideConnections) != 0 && m_OutsideConnectionData.HasComponent(entityResult.m_Entity))
					{
						if ((input.m_TypeMask & TypeMask.StaticObjects) != TypeMask.None)
						{
							input.m_Flags |= RaycastFlags.SubElements;
						}
					}
					else if ((input.m_Flags & RaycastFlags.Markers) == 0)
					{
						return;
					}
				}
				else
				{
					Elevation componentData2;
					CollisionMask collisionMask = ((!m_ElevationData.TryGetComponent(entityResult.m_Entity, out componentData2)) ? ObjectUtils.GetCollisionMask(componentData, ignoreMarkers: true) : ObjectUtils.GetCollisionMask(componentData, componentData2, ignoreMarkers: true));
					if ((collisionMask & input.m_CollisionMask) == 0)
					{
						if ((input.m_CollisionMask & CollisionMask.Underground) == 0 || (input.m_Flags & RaycastFlags.PartialSurface) == 0 || !(componentData.m_Bounds.min.y < 0f))
						{
							return;
						}
						flag = true;
					}
				}
				quaternion quaternion = math.inverse(transform.m_Rotation);
				Line3.Segment line = new Line3.Segment
				{
					a = math.mul(quaternion, segment.a - transform.m_Position),
					b = math.mul(quaternion, segment.b - transform.m_Position)
				};
				RaycastResult result = new RaycastResult
				{
					m_Owner = entityResult.m_Entity,
					m_Hit = 
					{
						m_HitEntity = entityResult.m_Entity,
						m_Position = transform.m_Position,
						m_NormalizedDistance = 1f
					}
				};
				Stack componentData3;
				StackData componentData4;
				Bounds3 bounds = ((!m_StackData.TryGetComponent(entityResult.m_Entity, out componentData3) || !m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData4)) ? ObjectUtils.GetBounds(componentData) : ObjectUtils.GetBounds(componentData3, componentData, componentData4));
				if (MathUtils.Intersect(bounds, line, out var t) && t.x < result.m_Hit.m_NormalizedDistance)
				{
					float3 hitPosition = MathUtils.Position(segment, t.x);
					result.m_Hit.m_HitPosition = hitPosition;
					result.m_Hit.m_NormalizedDistance = t.x;
					result.m_Hit.m_CellIndex = new int2(-1, -1);
					float num = math.cmax(MathUtils.Size(bounds));
					t = math.saturate(new float2(t.x - num, t.y + num));
					if (flag && MathUtils.Intersect(line.y, 0f, out var t2))
					{
						if (line.b.y > line.a.y)
						{
							t.y = math.min(t.y, t2);
						}
						else
						{
							t.x = math.max(t.x, t2);
						}
					}
					if (t.y > t.x)
					{
						Line3.Segment localLine = MathUtils.Cut(line, t);
						if (!RaycastMeshes(in input, ref result, entityResult.m_Entity, prefabRef, segment, localLine, transform.m_Rotation, t))
						{
							result.m_Hit.m_NormalizedDistance = 1f;
						}
					}
					else
					{
						result.m_Hit.m_NormalizedDistance = 1f;
					}
				}
				if ((componentData.m_Flags & GeometryFlags.HasLot) != GeometryFlags.None && (input.m_Flags & RaycastFlags.BuildingLots) != 0 && !flag)
				{
					RaycastLot(ref result, componentData, segment, entityResult.m_RaycastIndex, quaternion, transform.m_Position);
				}
				if (result.m_Hit.m_NormalizedDistance < 1f && ValidateResult(in input, ref result))
				{
					m_Results.Accumulate(entityResult.m_RaycastIndex, result);
				}
			}
			else
			{
				if ((input.m_Flags & RaycastFlags.EditorContainers) == 0 && m_EditorContainerData.HasComponent(entityResult.m_Entity))
				{
					return;
				}
				float t3;
				float num2 = MathUtils.Distance(segment, transform.m_Position, out t3);
				if (num2 < 1f)
				{
					RaycastResult result2 = new RaycastResult
					{
						m_Owner = entityResult.m_Entity,
						m_Hit = 
						{
							m_HitEntity = entityResult.m_Entity,
							m_Position = transform.m_Position,
							m_HitPosition = MathUtils.Position(segment, t3),
							m_NormalizedDistance = t3 - (1f - num2) / math.max(1f, MathUtils.Length(segment)),
							m_CellIndex = new int2(0, -1)
						}
					};
					if (ValidateResult(in input, ref result2))
					{
						m_Results.Accumulate(entityResult.m_RaycastIndex, result2);
					}
				}
			}
		}

		private bool IsNearCamera(Entity entity)
		{
			if (m_CullingInfoData.TryGetComponent(entity, out var componentData) && componentData.m_CullingIndex != 0)
			{
				return (m_CullingData[componentData.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0;
			}
			return false;
		}

		private bool ValidateResult(in RaycastInput input, ref RaycastResult result)
		{
			TypeMask typeMask = TypeMask.StaticObjects;
			float3 position = result.m_Hit.m_Position;
			Entity owner = Entity.Null;
			TypeMask typeMask2 = TypeMask.None;
			while (true)
			{
				if ((input.m_Flags & RaycastFlags.UpgradeIsMain) != 0)
				{
					if (m_ServiceUpgradeData.HasComponent(result.m_Owner))
					{
						break;
					}
					if (m_InstalledUpgrades.TryGetBuffer(result.m_Owner, out var bufferData) && bufferData.Length != 0)
					{
						owner = Entity.Null;
						typeMask2 = TypeMask.None;
						typeMask = TypeMask.StaticObjects;
						result.m_Owner = bufferData[0].m_Upgrade;
						break;
					}
				}
				else if ((input.m_Flags & RaycastFlags.SubBuildings) != 0 && m_ServiceUpgradeData.HasComponent(result.m_Owner) && (typeMask == TypeMask.Net || m_BuildingData.HasComponent(result.m_Owner)))
				{
					break;
				}
				if (!m_OwnerData.TryGetComponent(result.m_Owner, out var componentData))
				{
					break;
				}
				if (typeMask != TypeMask.Net || typeMask2 != TypeMask.Net || (input.m_Flags & RaycastFlags.ElevateOffset) == 0)
				{
					owner = result.m_Owner;
					typeMask2 = typeMask;
				}
				if (m_NodeData.TryGetComponent(componentData.m_Owner, out var componentData2))
				{
					typeMask = TypeMask.Net;
					result.m_Owner = componentData.m_Owner;
					position = componentData2.m_Position;
					if ((input.m_TypeMask & (TypeMask.StaticObjects | TypeMask.Net)) == TypeMask.Net)
					{
						typeMask2 = TypeMask.None;
						result.m_Hit.m_Position = position;
						break;
					}
				}
				else if (m_EdgeData.HasComponent(componentData.m_Owner))
				{
					typeMask = TypeMask.Net;
					result.m_Owner = componentData.m_Owner;
					Curve curve = m_CurveData[componentData.m_Owner];
					MathUtils.Distance(curve.m_Bezier, result.m_Hit.m_Position, out var t);
					position = MathUtils.Position(curve.m_Bezier, t);
					if ((input.m_TypeMask & (TypeMask.StaticObjects | TypeMask.Net)) == TypeMask.Net)
					{
						typeMask2 = TypeMask.None;
						result.m_Hit.m_Position = position;
						break;
					}
				}
				else if (m_LotAreaData.HasComponent(componentData.m_Owner))
				{
					typeMask = TypeMask.Areas;
					result.m_Owner = componentData.m_Owner;
					if ((input.m_TypeMask & TypeMask.Areas) == 0)
					{
						return false;
					}
				}
				else
				{
					typeMask = TypeMask.StaticObjects;
					result.m_Owner = componentData.m_Owner;
				}
			}
			if ((input.m_Flags & RaycastFlags.SubElements) != 0 && (input.m_TypeMask & typeMask2) != TypeMask.None)
			{
				result.m_Owner = owner;
				typeMask = typeMask2;
				if (typeMask2 == TypeMask.Net)
				{
					result.m_Hit.m_Position = position;
				}
			}
			else if ((input.m_Flags & RaycastFlags.NoMainElements) != 0)
			{
				return false;
			}
			if ((input.m_TypeMask & typeMask) == 0)
			{
				if ((input.m_TypeMask & TypeMask.Net) != TypeMask.None)
				{
					return FindClosestNode(in input, ref result);
				}
				return false;
			}
			switch (typeMask)
			{
			case TypeMask.Net:
			{
				PrefabRef prefabRef = m_PrefabRefData[result.m_Owner];
				if ((m_PrefabNetData[prefabRef.m_Prefab].m_ConnectLayers & input.m_NetLayerMask) != Layer.None && (input.m_Flags & RaycastFlags.ElevateOffset) == 0)
				{
					return CheckNetCollisionMask(in input, result.m_Owner);
				}
				return false;
			}
			case TypeMask.StaticObjects:
				return CheckPlaceholder(in input, ref result.m_Owner);
			default:
				return true;
			}
		}

		private bool CheckNetCollisionMask(in RaycastInput input, Entity owner)
		{
			if (m_CompositionData.TryGetComponent(owner, out var componentData))
			{
				return CheckCompositionCollisionMask(in input, componentData.m_Edge);
			}
			if (m_OrphanData.TryGetComponent(owner, out var componentData2))
			{
				return CheckCompositionCollisionMask(in input, componentData2.m_Composition);
			}
			if (m_ConnectedEdges.TryGetBuffer(owner, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity edge = bufferData[i].m_Edge;
					Edge edge2 = m_EdgeData[edge];
					if (edge2.m_Start == owner && m_CompositionData.TryGetComponent(edge, out componentData) && !CheckCompositionCollisionMask(in input, componentData.m_StartNode))
					{
						return false;
					}
					if (edge2.m_End == owner && m_CompositionData.TryGetComponent(edge, out componentData) && !CheckCompositionCollisionMask(in input, componentData.m_EndNode))
					{
						return false;
					}
				}
				return true;
			}
			return true;
		}

		private bool CheckCompositionCollisionMask(in RaycastInput input, Entity composition)
		{
			if (m_PrefabCompositionData.TryGetComponent(composition, out var componentData))
			{
				if ((componentData.m_State & CompositionState.Marker) != 0)
				{
					if ((input.m_Flags & RaycastFlags.Markers) == 0)
					{
						return false;
					}
				}
				else if ((NetUtils.GetCollisionMask(componentData, ignoreMarkers: true) & input.m_CollisionMask) == 0)
				{
					return false;
				}
			}
			return true;
		}

		private bool CheckPlaceholder(in RaycastInput input, ref Entity entity)
		{
			if ((input.m_Flags & RaycastFlags.Placeholders) != 0)
			{
				return true;
			}
			if (m_PlaceholderData.HasComponent(entity))
			{
				if (m_AttachmentData.HasComponent(entity))
				{
					Attachment attachment = m_AttachmentData[entity];
					if (m_PrefabRefData.HasComponent(attachment.m_Attached))
					{
						entity = attachment.m_Attached;
						return true;
					}
				}
				return false;
			}
			return true;
		}

		private bool FindClosestNode(in RaycastInput input, ref RaycastResult result)
		{
			if (!m_SubNets.HasBuffer(result.m_Owner) || (input.m_Flags & (RaycastFlags.SubElements | RaycastFlags.NoMainElements)) != 0)
			{
				return false;
			}
			float num = float.MaxValue;
			Entity entity = Entity.Null;
			float3 position = default(float3);
			DynamicBuffer<Game.Net.SubNet> dynamicBuffer = m_SubNets[result.m_Owner];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity subNet = dynamicBuffer[i].m_SubNet;
				if (m_NodeData.HasComponent(subNet))
				{
					PrefabRef prefabRef = m_PrefabRefData[subNet];
					if ((m_PrefabNetData[prefabRef.m_Prefab].m_ConnectLayers & input.m_NetLayerMask) != Layer.None)
					{
						Game.Net.Node node = m_NodeData[subNet];
						float num2 = math.distance(result.m_Hit.m_HitPosition, node.m_Position);
						if (num2 < num)
						{
							num = num2;
							entity = subNet;
							position = node.m_Position;
						}
					}
				}
				else
				{
					if (!m_EdgeData.HasComponent(subNet))
					{
						continue;
					}
					PrefabRef prefabRef2 = m_PrefabRefData[subNet];
					if ((m_PrefabNetData[prefabRef2.m_Prefab].m_ConnectLayers & input.m_NetLayerMask) != Layer.None)
					{
						Curve curve = m_CurveData[subNet];
						float t;
						float num3 = MathUtils.Distance(curve.m_Bezier, result.m_Hit.m_HitPosition, out t);
						if (num3 < num)
						{
							num = num3;
							entity = subNet;
							position = MathUtils.Position(curve.m_Bezier, t);
						}
					}
				}
			}
			if (entity == Entity.Null)
			{
				return false;
			}
			result.m_Owner = entity;
			result.m_Hit.m_Position = position;
			return true;
		}

		private void RaycastLot(ref RaycastResult result, ObjectGeometryData prefabObjectData, Line3.Segment worldLine, int raycastIndex, quaternion inverseRotation, float3 position)
		{
			RaycastResult raycastResult = m_TerrainResults[raycastIndex];
			if (raycastResult.m_Owner == Entity.Null)
			{
				return;
			}
			bool flag;
			float2 @float;
			if ((prefabObjectData.m_Flags & GeometryFlags.Standing) != GeometryFlags.None)
			{
				flag = (prefabObjectData.m_Flags & GeometryFlags.CircularLeg) != 0;
				@float = prefabObjectData.m_LegSize.xz + prefabObjectData.m_LegOffset * 2f + 0.4f;
			}
			else
			{
				flag = (prefabObjectData.m_Flags & GeometryFlags.Circular) != 0;
				@float = prefabObjectData.m_Size.xz + 0.4f;
			}
			float3 float2 = math.mul(inverseRotation, raycastResult.m_Hit.m_HitPosition - position);
			@float *= 0.5f;
			if (flag)
			{
				if (math.length(float2.xz) > math.csum(@float) * 0.5f)
				{
					return;
				}
			}
			else if (!math.all((float2.xz >= -@float) & (float2.xz <= @float)))
			{
				return;
			}
			float num = math.distance(raycastResult.m_Hit.m_HitPosition, worldLine.a) / math.max(1f, MathUtils.Length(worldLine));
			if (num < result.m_Hit.m_NormalizedDistance)
			{
				result.m_Hit.m_NormalizedDistance = num;
				result.m_Hit.m_HitPosition = raycastResult.m_Hit.m_HitPosition;
				result.m_Hit.m_HitDirection = raycastResult.m_Hit.m_HitDirection;
				result.m_Hit.m_CellIndex = -1;
			}
		}

		private bool HasCachedMesh(Entity mesh, out Entity sharedMesh)
		{
			if (m_PrefabSharedMeshData.TryGetComponent(mesh, out var componentData))
			{
				sharedMesh = componentData.m_Mesh;
			}
			else
			{
				sharedMesh = mesh;
			}
			return m_Vertices.HasBuffer(sharedMesh);
		}

		private bool RaycastMeshes(in RaycastInput input, ref RaycastResult result, Entity entity, PrefabRef prefabRef, Line3.Segment worldLine, Line3.Segment localLine, quaternion localToWorldRotation, float2 cutOffset)
		{
			bool flag = false;
			RaycastHit hit = result.m_Hit;
			hit.m_NormalizedDistance = 2f;
			if (m_Meshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				SubMeshFlags subMeshFlags = SubMeshFlags.DefaultMissingMesh | SubMeshFlags.HasTransform | SubMeshFlags.OutlineOnly;
				subMeshFlags = (SubMeshFlags)((uint)subMeshFlags | (uint)(m_LeftHandTraffic ? 65536 : 131072));
				float3 scale = 1f;
				float3 offsets = 1f;
				float3 scale2 = 1f;
				int3 tileCounts = 0;
				if (m_TreeData.TryGetComponent(entity, out var componentData))
				{
					subMeshFlags = ((!m_PrefabGrowthScaleData.TryGetComponent(prefabRef.m_Prefab, out var componentData2)) ? (subMeshFlags | SubMeshFlags.RequireAdult) : (subMeshFlags | BatchDataHelpers.CalculateTreeSubMeshData(componentData, componentData2, out scale)));
				}
				if (m_StackData.TryGetComponent(entity, out var componentData3) && m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
				{
					subMeshFlags |= BatchDataHelpers.CalculateStackSubMeshData(componentData3, componentData4, out tileCounts, out offsets, out scale2);
				}
				else
				{
					componentData4 = default(StackData);
				}
				if (m_NetObjectData.TryGetComponent(entity, out var componentData5))
				{
					subMeshFlags |= BatchDataHelpers.CalculateNetObjectSubMeshData(componentData5);
				}
				if (m_QuantityData.TryGetComponent(entity, out var componentData6))
				{
					subMeshFlags = ((!m_PrefabQuantityObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData7)) ? (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData6, default(QuantityObjectData), m_EditorMode)) : (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData6, componentData7, m_EditorMode)));
				}
				if (m_UnderConstructionData.TryGetComponent(entity, out var componentData8) && componentData8.m_NewPrefab == Entity.Null)
				{
					return false;
				}
				if (m_DestroyedData.HasComponent(entity) && m_PrefabObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData9) && (componentData9.m_Flags & (GeometryFlags.Physical | GeometryFlags.HasLot)) == (GeometryFlags.Physical | GeometryFlags.HasLot))
				{
					return false;
				}
				bool flag2 = false;
				bool flag3 = false;
				DynamicBuffer<MeshGroup> bufferData2 = default(DynamicBuffer<MeshGroup>);
				int num = 1;
				if (m_SubMeshGroups.TryGetBuffer(prefabRef.m_Prefab, out var bufferData3) && m_MeshGroups.TryGetBuffer(entity, out bufferData2))
				{
					num = bufferData2.Length;
				}
				SubMeshGroup subMeshGroup = default(SubMeshGroup);
				for (int i = 0; i < num; i++)
				{
					MeshGroup value;
					if (bufferData3.IsCreated)
					{
						CollectionUtils.TryGet(bufferData2, i, out value);
						subMeshGroup = bufferData3[value.m_SubMeshGroup];
					}
					else
					{
						subMeshGroup.m_SubMeshRange = new int2(0, bufferData.Length);
						value = default(MeshGroup);
					}
					for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
					{
						SubMesh subMesh = bufferData[j];
						if ((subMesh.m_Flags & subMeshFlags) != subMesh.m_Flags)
						{
							continue;
						}
						int falseValue = 1;
						falseValue = math.select(falseValue, tileCounts.x, (subMesh.m_Flags & SubMeshFlags.IsStackStart) != 0);
						falseValue = math.select(falseValue, tileCounts.y, (subMesh.m_Flags & SubMeshFlags.IsStackMiddle) != 0);
						falseValue = math.select(falseValue, tileCounts.z, (subMesh.m_Flags & SubMeshFlags.IsStackEnd) != 0);
						if (falseValue < 1)
						{
							continue;
						}
						Entity entity2 = Entity.Null;
						DynamicBuffer<LodMesh> bufferData4;
						if (HasCachedMesh(subMesh.m_SubMesh, out var sharedMesh))
						{
							entity2 = sharedMesh;
						}
						else if (m_Lods.TryGetBuffer(subMesh.m_SubMesh, out bufferData4))
						{
							for (int num2 = bufferData4.Length - 1; num2 >= 0; num2--)
							{
								if (HasCachedMesh(bufferData4[num2].m_LodMesh, out sharedMesh))
								{
									entity2 = sharedMesh;
									break;
								}
							}
						}
						if ((input.m_Flags & RaycastFlags.Decals) == 0 && (m_PrefabMeshData[(entity2 != Entity.Null) ? entity2 : subMesh.m_SubMesh].m_State & MeshFlags.Decal) != 0)
						{
							continue;
						}
						if (entity2 == Entity.Null)
						{
							flag3 = true;
							continue;
						}
						DynamicBuffer<MeshVertex> vertices = m_Vertices[entity2];
						DynamicBuffer<MeshIndex> indices = m_Indices[entity2];
						DynamicBuffer<MeshNode> bufferData5 = default(DynamicBuffer<MeshNode>);
						DynamicBuffer<ProceduralBone> bufferData6 = default(DynamicBuffer<ProceduralBone>);
						DynamicBuffer<Bone> bufferData7 = default(DynamicBuffer<Bone>);
						DynamicBuffer<Skeleton> dynamicBuffer = default(DynamicBuffer<Skeleton>);
						if (m_Nodes.TryGetBuffer(entity2, out bufferData5) && m_ProceduralBones.TryGetBuffer(entity2, out bufferData6) && m_Bones.TryGetBuffer(entity, out bufferData7))
						{
							dynamicBuffer = m_Skeletons[entity];
							if (dynamicBuffer.Length == 0)
							{
								bufferData7 = default(DynamicBuffer<Bone>);
								dynamicBuffer = default(DynamicBuffer<Skeleton>);
							}
						}
						flag2 |= indices.Length != 0;
						flag3 |= indices.Length == 0;
						int num3 = j - subMeshGroup.m_SubMeshRange.x + value.m_MeshOffset;
						for (int k = 0; k < falseValue; k++)
						{
							float3 subMeshPosition = subMesh.m_Position;
							float3 subMeshScale = scale;
							if ((subMesh.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd)) != 0)
							{
								BatchDataHelpers.CalculateStackSubMeshData(componentData4, offsets, scale2, k, subMesh.m_Flags, ref subMeshPosition, ref subMeshScale);
							}
							Line3.Segment localLine2 = localLine;
							if ((subMesh.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd | SubMeshFlags.HasTransform)) != 0)
							{
								quaternion q = math.inverse(subMesh.m_Rotation);
								localLine2.a = math.mul(q, localLine.a - subMeshPosition) / subMeshScale;
								localLine2.b = math.mul(q, localLine.b - subMeshPosition) / subMeshScale;
							}
							else if (math.any(subMeshScale != 1f))
							{
								localLine2.a = localLine.a / subMeshScale;
								localLine2.b = localLine.b / subMeshScale;
							}
							if (m_PrefabImpostorData.TryGetComponent(entity2, out var componentData10) && componentData10.m_Size != 0f)
							{
								localLine2.a = (localLine2.a - componentData10.m_Offset) / componentData10.m_Size;
								localLine2.b = (localLine2.b - componentData10.m_Offset) / componentData10.m_Size;
							}
							if (bufferData5.IsCreated)
							{
								if (bufferData6.IsCreated)
								{
									if (bufferData7.IsCreated)
									{
										if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, bufferData6, bufferData7, dynamicBuffer[num3], new int2(num3, -1), ref hit))
										{
											hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
										}
									}
									else if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, bufferData6, new int2(num3, -1), ref hit))
									{
										hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
									}
								}
								else if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, new int2(num3, -1), ref hit))
								{
									hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
								}
							}
							else if (CheckMeshIntersect(localLine2, vertices, indices, new int2(num3, -1), ref hit))
							{
								hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
							}
						}
					}
				}
				flag = bufferData.Length != 0 && (flag2 || !flag3);
			}
			if (!flag && (m_PrefabObjectData[prefabRef.m_Prefab].m_Flags & GeometryFlags.HasLot) == 0)
			{
				result.m_Hit.m_NormalizedDistance += 10f / math.max(1f, MathUtils.Length(worldLine));
				return true;
			}
			if (hit.m_NormalizedDistance < 2f)
			{
				hit.m_NormalizedDistance = math.lerp(cutOffset.x, cutOffset.y, hit.m_NormalizedDistance);
				hit.m_HitPosition = MathUtils.Position(worldLine, hit.m_NormalizedDistance);
				hit.m_HitDirection = math.normalizesafe(math.rotate(localToWorldRotation, hit.m_HitDirection));
				result.m_Hit = hit;
				return true;
			}
			return false;
		}
	}

	[BurstCompile]
	public struct GetSourceRangesJob : IJob
	{
		[ReadOnly]
		public NativeList<RaycastSystem.EntityResult> m_EdgeList;

		[ReadOnly]
		public NativeList<RaycastSystem.EntityResult> m_StaticObjectList;

		[NativeDisableParallelForRestriction]
		public NativeArray<int4> m_Ranges;

		public void Execute()
		{
			int4 value = new int4(m_EdgeList.Length + 1, 0, m_StaticObjectList.Length + 1, 0);
			for (int i = 0; i < m_Ranges.Length; i++)
			{
				m_Ranges[i] = value;
			}
			for (int j = 0; j < m_EdgeList.Length; j++)
			{
				RaycastSystem.EntityResult entityResult = m_EdgeList[j];
				ref int4 reference = ref m_Ranges.ElementAt(entityResult.m_RaycastIndex);
				reference.x = math.min(reference.x, j);
				reference.y = j;
			}
			for (int k = 0; k < m_StaticObjectList.Length; k++)
			{
				RaycastSystem.EntityResult entityResult2 = m_StaticObjectList[k];
				ref int4 reference2 = ref m_Ranges.ElementAt(entityResult2.m_RaycastIndex);
				reference2.z = math.min(reference2.z, k);
				reference2.w = k;
			}
		}
	}

	[BurstCompile]
	public struct ExtractLaneObjectsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Composition> m_CompositionData;

		[ReadOnly]
		public ComponentLookup<NetCompositionData> m_PrefabCompositionData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public NativeList<RaycastSystem.EntityResult> m_EdgeList;

		[ReadOnly]
		public NativeList<RaycastSystem.EntityResult> m_StaticObjectList;

		[ReadOnly]
		public NativeArray<int4> m_Ranges;

		public NativeQueue<RaycastSystem.EntityResult>.ParallelWriter m_MovingObjectQueue;

		public void Execute(int index)
		{
			RaycastInput input = m_Input[index];
			if ((input.m_TypeMask & TypeMask.MovingObjects) == 0)
			{
				return;
			}
			int4 @int = m_Ranges[index];
			if (@int.x > @int.y && @int.z > @int.w)
			{
				return;
			}
			int2 int2 = math.max(0, @int.yw - @int.xz + 1);
			NativeParallelHashSet<Entity> checkedEntities = new NativeParallelHashSet<Entity>(int2.x * 8 + int2.y, Allocator.Temp);
			for (int i = @int.x; i <= @int.y; i++)
			{
				RaycastSystem.EntityResult entity = m_EdgeList[i];
				if (entity.m_RaycastIndex == index && m_CompositionData.TryGetComponent(entity.m_Entity, out var componentData))
				{
					Edge edge = m_EdgeData[entity.m_Entity];
					NetCompositionData compositionData = m_PrefabCompositionData[componentData.m_StartNode];
					NetCompositionData compositionData2 = m_PrefabCompositionData[componentData.m_Edge];
					NetCompositionData compositionData3 = m_PrefabCompositionData[componentData.m_EndNode];
					CollisionMask collisionMask = NetUtils.GetCollisionMask(compositionData, ignoreMarkers: false);
					CollisionMask collisionMask2 = NetUtils.GetCollisionMask(compositionData2, ignoreMarkers: false);
					CollisionMask collisionMask3 = NetUtils.GetCollisionMask(compositionData3, ignoreMarkers: false);
					if ((collisionMask & input.m_CollisionMask) != 0)
					{
						TryCheckNode(input, checkedEntities, new RaycastSystem.EntityResult
						{
							m_Entity = edge.m_Start,
							m_RaycastIndex = entity.m_RaycastIndex
						}, entity.m_Entity);
					}
					if ((collisionMask2 & input.m_CollisionMask) != 0)
					{
						TryCheckLanes(checkedEntities, entity);
					}
					if ((collisionMask3 & input.m_CollisionMask) != 0)
					{
						TryCheckNode(input, checkedEntities, new RaycastSystem.EntityResult
						{
							m_Entity = edge.m_End,
							m_RaycastIndex = entity.m_RaycastIndex
						}, entity.m_Entity);
					}
				}
			}
			for (int j = @int.z; j <= @int.w; j++)
			{
				RaycastSystem.EntityResult obj = m_StaticObjectList[j];
				if (obj.m_RaycastIndex == index)
				{
					TryCheckObject(checkedEntities, obj);
				}
			}
		}

		private void TryCheckObject(NativeParallelHashSet<Entity> checkedEntities, RaycastSystem.EntityResult obj)
		{
			TryCheckLanes(checkedEntities, obj);
		}

		private void TryCheckNode(RaycastInput input, NativeParallelHashSet<Entity> checkedEntities, RaycastSystem.EntityResult node, Entity ignoreEdge)
		{
			DynamicBuffer<ConnectedEdge> dynamicBuffer = m_ConnectedEdges[node.m_Entity];
			TryCheckLanes(checkedEntities, node);
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				RaycastSystem.EntityResult entity = new RaycastSystem.EntityResult
				{
					m_Entity = dynamicBuffer[i].m_Edge,
					m_RaycastIndex = node.m_RaycastIndex
				};
				if (!(entity.m_Entity == ignoreEdge))
				{
					Composition composition = m_CompositionData[entity.m_Entity];
					if ((NetUtils.GetCollisionMask(m_PrefabCompositionData[composition.m_Edge], ignoreMarkers: false) & input.m_CollisionMask) != 0)
					{
						TryCheckLanes(checkedEntities, entity);
					}
				}
			}
		}

		private void TryCheckLanes(NativeParallelHashSet<Entity> checkedEntities, RaycastSystem.EntityResult entity)
		{
			if (checkedEntities.Add(entity.m_Entity) && m_SubLanes.TryGetBuffer(entity.m_Entity, out var bufferData))
			{
				CheckLanes(entity.m_RaycastIndex, bufferData);
			}
		}

		private void CheckLanes(int raycastIndex, DynamicBuffer<Game.Net.SubLane> lanes)
		{
			for (int i = 0; i < lanes.Length; i++)
			{
				Entity subLane = lanes[i].m_SubLane;
				if (m_LaneObjects.TryGetBuffer(subLane, out var bufferData))
				{
					for (int j = 0; j < bufferData.Length; j++)
					{
						m_MovingObjectQueue.Enqueue(new RaycastSystem.EntityResult
						{
							m_Entity = bufferData[j].m_LaneObject,
							m_RaycastIndex = raycastIndex
						});
					}
				}
			}
		}
	}

	[BurstCompile]
	public struct RaycastMovingObjectsJob : IJobParallelForDefer
	{
		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public NativeArray<RaycastSystem.EntityResult> m_ObjectList;

		[ReadOnly]
		public NativeList<PreCullingData> m_CullingData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<CullingInfo> m_CullingInfoData;

		[ReadOnly]
		public ComponentLookup<InterpolatedTransform> m_InterpolatedTransformData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<SharedMeshData> m_PrefabSharedMeshData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<Passenger> m_Passengers;

		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<SubMesh> m_Meshes;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> m_SubMeshGroups;

		[ReadOnly]
		public BufferLookup<LodMesh> m_Lods;

		[ReadOnly]
		public BufferLookup<MeshVertex> m_Vertices;

		[ReadOnly]
		public BufferLookup<MeshIndex> m_Indices;

		[ReadOnly]
		public BufferLookup<MeshNode> m_Nodes;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			RaycastSystem.EntityResult entityResult = m_ObjectList[index];
			RaycastInput input = m_Input[entityResult.m_RaycastIndex];
			if ((input.m_TypeMask & TypeMask.MovingObjects) != TypeMask.None)
			{
				RaycastObjects(entityResult.m_RaycastIndex, input, entityResult.m_Entity, entityResult.m_Entity);
			}
		}

		private void RaycastObjects(int raycastIndex, RaycastInput input, Entity owner, Entity entity)
		{
			RaycastObject(raycastIndex, input, owner, entity);
			if (m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subObject = bufferData[i].m_SubObject;
					RaycastObjects(raycastIndex, input, owner, subObject);
				}
			}
			if (m_Passengers.TryGetBuffer(entity, out var bufferData2))
			{
				for (int j = 0; j < bufferData2.Length; j++)
				{
					Entity passenger = bufferData2[j].m_Passenger;
					RaycastObjects(raycastIndex, input, passenger, passenger);
				}
			}
		}

		private void RaycastObject(int raycastIndex, RaycastInput input, Entity owner, Entity entity)
		{
			if (!IsNearCamera(entity))
			{
				return;
			}
			InterpolatedTransform componentData;
			Transform transform = ((!m_InterpolatedTransformData.TryGetComponent(entity, out componentData)) ? m_TransformData[entity] : componentData.ToTransform());
			PrefabRef prefabRefData = m_PrefabRefData[entity];
			if (!m_PrefabObjectData.TryGetComponent(prefabRefData.m_Prefab, out var componentData2))
			{
				return;
			}
			float t;
			float num = MathUtils.DistanceSquared(input.m_Line, transform.m_Position, out t);
			float3 size = componentData2.m_Size;
			size.xz *= 0.5f;
			if (num > math.lengthsq(size))
			{
				return;
			}
			Bounds3 bounds = componentData2.m_Bounds;
			quaternion q = math.inverse(transform.m_Rotation);
			Line3.Segment line = new Line3.Segment
			{
				a = math.mul(q, input.m_Line.a - transform.m_Position),
				b = math.mul(q, input.m_Line.b - transform.m_Position)
			};
			if (MathUtils.Intersect(bounds, line, out var t2))
			{
				float3 hitPosition = MathUtils.Position(input.m_Line, t2.x);
				RaycastResult result = new RaycastResult
				{
					m_Owner = owner,
					m_Hit = 
					{
						m_HitEntity = entity,
						m_Position = transform.m_Position,
						m_HitPosition = hitPosition,
						m_NormalizedDistance = t2.x,
						m_CellIndex = new int2(-1, -1)
					}
				};
				float num2 = math.cmax(MathUtils.Size(bounds));
				t2 = math.saturate(new float2(t2.x - num2, t2.y + num2));
				line = MathUtils.Cut(line, t2);
				if (RaycastMeshes(input, ref result, entity, prefabRefData, input.m_Line, line, transform.m_Rotation, t2))
				{
					m_Results.Accumulate(raycastIndex, result);
				}
			}
		}

		private bool IsNearCamera(Entity entity)
		{
			if (m_CullingInfoData.TryGetComponent(entity, out var componentData) && componentData.m_CullingIndex != 0)
			{
				return (m_CullingData[componentData.m_CullingIndex].m_Flags & PreCullingFlags.NearCamera) != 0;
			}
			return false;
		}

		private bool HasCachedMesh(Entity mesh, out Entity sharedMesh)
		{
			if (m_PrefabSharedMeshData.TryGetComponent(mesh, out var componentData))
			{
				sharedMesh = componentData.m_Mesh;
			}
			else
			{
				sharedMesh = mesh;
			}
			return m_Vertices.HasBuffer(sharedMesh);
		}

		private bool RaycastMeshes(RaycastInput input, ref RaycastResult result, Entity entity, PrefabRef prefabRefData, Line3.Segment worldLine, Line3.Segment localLine, quaternion localToWorldRotation, float2 cutOffset)
		{
			bool flag = false;
			RaycastHit hit = result.m_Hit;
			hit.m_NormalizedDistance = 2f;
			if (m_Meshes.TryGetBuffer(prefabRefData.m_Prefab, out var bufferData))
			{
				SubMeshFlags subMeshFlags = SubMeshFlags.DefaultMissingMesh | SubMeshFlags.HasTransform;
				subMeshFlags = (SubMeshFlags)((uint)subMeshFlags | (uint)(m_LeftHandTraffic ? 65536 : 131072));
				if (m_QuantityData.TryGetComponent(entity, out var componentData))
				{
					subMeshFlags = ((!m_PrefabQuantityObjectData.TryGetComponent(prefabRefData.m_Prefab, out var componentData2)) ? (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData, default(QuantityObjectData), m_EditorMode)) : (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData, componentData2, m_EditorMode)));
				}
				bool flag2 = false;
				bool flag3 = false;
				DynamicBuffer<MeshGroup> bufferData2 = default(DynamicBuffer<MeshGroup>);
				int num = 1;
				if (m_SubMeshGroups.TryGetBuffer(prefabRefData.m_Prefab, out var bufferData3) && m_MeshGroups.TryGetBuffer(entity, out bufferData2))
				{
					num = bufferData2.Length;
				}
				SubMeshGroup subMeshGroup = default(SubMeshGroup);
				for (int i = 0; i < num; i++)
				{
					MeshGroup value;
					if (bufferData3.IsCreated)
					{
						CollectionUtils.TryGet(bufferData2, i, out value);
						subMeshGroup = bufferData3[value.m_SubMeshGroup];
					}
					else
					{
						subMeshGroup.m_SubMeshRange = new int2(0, bufferData.Length);
						value = default(MeshGroup);
					}
					for (int j = subMeshGroup.m_SubMeshRange.x; j < subMeshGroup.m_SubMeshRange.y; j++)
					{
						SubMesh subMesh = bufferData[j];
						if ((subMesh.m_Flags & subMeshFlags) != subMesh.m_Flags)
						{
							continue;
						}
						Entity entity2 = Entity.Null;
						DynamicBuffer<LodMesh> bufferData4;
						if (HasCachedMesh(subMesh.m_SubMesh, out var sharedMesh))
						{
							entity2 = sharedMesh;
						}
						else if (m_Lods.TryGetBuffer(subMesh.m_SubMesh, out bufferData4))
						{
							for (int num2 = bufferData4.Length - 1; num2 >= 0; num2--)
							{
								if (HasCachedMesh(bufferData4[num2].m_LodMesh, out sharedMesh))
								{
									entity2 = sharedMesh;
									break;
								}
							}
						}
						if ((input.m_Flags & RaycastFlags.Decals) == 0 && (m_PrefabMeshData[(entity2 != Entity.Null) ? entity2 : subMesh.m_SubMesh].m_State & MeshFlags.Decal) != 0)
						{
							continue;
						}
						if (entity2 == Entity.Null)
						{
							flag3 = true;
							continue;
						}
						Line3.Segment localLine2 = localLine;
						if ((subMesh.m_Flags & SubMeshFlags.HasTransform) != 0)
						{
							quaternion q = math.inverse(subMesh.m_Rotation);
							localLine2.a = math.mul(q, localLine.a - subMesh.m_Position);
							localLine2.b = math.mul(q, localLine.b - subMesh.m_Position);
						}
						DynamicBuffer<MeshVertex> vertices = m_Vertices[entity2];
						DynamicBuffer<MeshIndex> indices = m_Indices[entity2];
						flag2 |= indices.Length != 0;
						flag3 |= indices.Length == 0;
						int num3 = j - subMeshGroup.m_SubMeshRange.x + value.m_MeshOffset;
						if (m_Nodes.TryGetBuffer(entity2, out var bufferData5))
						{
							if (m_ProceduralBones.TryGetBuffer(entity2, out var bufferData6))
							{
								if (m_Bones.TryGetBuffer(entity, out var bufferData7))
								{
									DynamicBuffer<Skeleton> dynamicBuffer = m_Skeletons[entity];
									if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, bufferData6, bufferData7, dynamicBuffer[num3], new int2(num3, -1), ref hit))
									{
										hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
									}
								}
								else if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, bufferData6, new int2(num3, -1), ref hit))
								{
									hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
								}
							}
							else if (CheckMeshIntersect(localLine2, vertices, indices, bufferData5, new int2(num3, -1), ref hit))
							{
								hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
							}
						}
						else if (CheckMeshIntersect(localLine2, vertices, indices, new int2(num3, -1), ref hit))
						{
							hit.m_HitDirection = math.rotate(subMesh.m_Rotation, hit.m_HitDirection);
						}
					}
				}
				flag = bufferData.Length != 0 && (flag2 || !flag3);
			}
			if (!flag)
			{
				return true;
			}
			if (hit.m_NormalizedDistance < 2f)
			{
				hit.m_NormalizedDistance = math.lerp(cutOffset.x, cutOffset.y, hit.m_NormalizedDistance);
				hit.m_HitPosition = MathUtils.Position(worldLine, hit.m_NormalizedDistance);
				hit.m_HitDirection = math.normalizesafe(math.rotate(localToWorldRotation, hit.m_HitDirection));
				result.m_Hit = hit;
				return true;
			}
			return false;
		}
	}

	private static bool CheckMeshIntersect(Line3.Segment localLine, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, int2 elementIndex, ref RaycastHit hit)
	{
		bool result = false;
		for (int i = 0; i < indices.Length; i += 3)
		{
			Triangle3 triangle = new Triangle3(vertices[indices[i].m_Index].m_Vertex, vertices[indices[i + 1].m_Index].m_Vertex, vertices[indices[i + 2].m_Index].m_Vertex);
			if (MathUtils.Intersect(triangle, localLine, out var t) && t.z < hit.m_NormalizedDistance)
			{
				hit.m_HitDirection = MathUtils.NormalCW(triangle);
				hit.m_NormalizedDistance = t.z;
				hit.m_CellIndex = elementIndex;
				result = true;
			}
		}
		return result;
	}

	private unsafe static bool CheckMeshIntersect(Line3.Segment localLine, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, int2 elementIndex, ref RaycastHit hit)
	{
		bool result = false;
		int* ptr = stackalloc int[128];
		int num = 0;
		if (nodes.Length != 0)
		{
			ptr[num++] = 0;
		}
		while (--num >= 0)
		{
			int index = ptr[num];
			MeshNode meshNode = nodes[index];
			if (!MathUtils.Intersect(meshNode.m_Bounds, localLine, out var _))
			{
				continue;
			}
			for (int i = meshNode.m_IndexRange.x; i < meshNode.m_IndexRange.y; i += 3)
			{
				Triangle3 triangle = new Triangle3(vertices[indices[i].m_Index].m_Vertex, vertices[indices[i + 1].m_Index].m_Vertex, vertices[indices[i + 2].m_Index].m_Vertex);
				if (MathUtils.Intersect(triangle, localLine, out var t2) && t2.z < hit.m_NormalizedDistance)
				{
					hit.m_HitDirection = MathUtils.NormalCW(triangle);
					hit.m_NormalizedDistance = t2.z;
					hit.m_CellIndex = elementIndex;
					result = true;
				}
			}
			ptr[num] = meshNode.m_SubNodes1.x;
			num = math.select(num, num + 1, meshNode.m_SubNodes1.x != -1);
			ptr[num] = meshNode.m_SubNodes1.y;
			num = math.select(num, num + 1, meshNode.m_SubNodes1.y != -1);
			ptr[num] = meshNode.m_SubNodes1.z;
			num = math.select(num, num + 1, meshNode.m_SubNodes1.z != -1);
			ptr[num] = meshNode.m_SubNodes1.w;
			num = math.select(num, num + 1, meshNode.m_SubNodes1.w != -1);
			ptr[num] = meshNode.m_SubNodes2.x;
			num = math.select(num, num + 1, meshNode.m_SubNodes2.x != -1);
			ptr[num] = meshNode.m_SubNodes2.y;
			num = math.select(num, num + 1, meshNode.m_SubNodes2.y != -1);
			ptr[num] = meshNode.m_SubNodes2.z;
			num = math.select(num, num + 1, meshNode.m_SubNodes2.z != -1);
			ptr[num] = meshNode.m_SubNodes2.w;
			num = math.select(num, num + 1, meshNode.m_SubNodes2.w != -1);
		}
		return result;
	}

	private unsafe static bool CheckMeshIntersect(Line3.Segment localLine, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, DynamicBuffer<ProceduralBone> prefabBones, int2 elementIndex, ref RaycastHit hit)
	{
		bool result = false;
		int* ptr = stackalloc int[128];
		for (int i = 0; i < prefabBones.Length; i++)
		{
			int num = 0;
			if (math.any(MathUtils.Size(nodes[i].m_Bounds) > 0f))
			{
				ptr[num++] = i;
			}
			while (--num >= 0)
			{
				int index = ptr[num];
				MeshNode meshNode = nodes[index];
				if (!MathUtils.Intersect(meshNode.m_Bounds, localLine, out var _))
				{
					continue;
				}
				for (int j = meshNode.m_IndexRange.x; j < meshNode.m_IndexRange.y; j += 3)
				{
					Triangle3 triangle = new Triangle3(vertices[indices[j].m_Index].m_Vertex, vertices[indices[j + 1].m_Index].m_Vertex, vertices[indices[j + 2].m_Index].m_Vertex);
					if (MathUtils.Intersect(triangle, localLine, out var t2) && t2.z < hit.m_NormalizedDistance)
					{
						hit.m_HitDirection = MathUtils.NormalCW(triangle);
						hit.m_NormalizedDistance = t2.z;
						hit.m_CellIndex = elementIndex;
						result = true;
					}
				}
				ptr[num] = meshNode.m_SubNodes1.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.x != -1);
				ptr[num] = meshNode.m_SubNodes1.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.y != -1);
				ptr[num] = meshNode.m_SubNodes1.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.z != -1);
				ptr[num] = meshNode.m_SubNodes1.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.w != -1);
				ptr[num] = meshNode.m_SubNodes2.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.x != -1);
				ptr[num] = meshNode.m_SubNodes2.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.y != -1);
				ptr[num] = meshNode.m_SubNodes2.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.z != -1);
				ptr[num] = meshNode.m_SubNodes2.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.w != -1);
			}
		}
		return result;
	}

	private unsafe static bool CheckMeshIntersect(Line3.Segment localLine, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, DynamicBuffer<ProceduralBone> prefabBones, DynamicBuffer<Bone> bones, Skeleton skeleton, int2 elementIndex, ref RaycastHit hit)
	{
		bool result = false;
		int* ptr = stackalloc int[128];
		for (int i = 0; i < prefabBones.Length; i++)
		{
			int num = 0;
			Line3.Segment line = default(Line3.Segment);
			ProceduralBone proceduralBone = prefabBones[i];
			if (math.any(MathUtils.Size(nodes[proceduralBone.m_BindIndex].m_Bounds) > 0f))
			{
				ptr[num++] = proceduralBone.m_BindIndex;
				Bone bone = bones[skeleton.m_BoneOffset + i];
				float4x4 float4x = float4x4.TRS(bone.m_Position, bone.m_Rotation, bone.m_Scale);
				int parentIndex = proceduralBone.m_ParentIndex;
				while (parentIndex >= 0)
				{
					Bone bone2 = bones[skeleton.m_BoneOffset + parentIndex];
					ProceduralBone proceduralBone2 = prefabBones[parentIndex];
					float4x = math.mul(float4x4.TRS(bone2.m_Position, bone2.m_Rotation, bone2.m_Scale), float4x);
					parentIndex = proceduralBone2.m_ParentIndex;
				}
				float4x = math.mul(float4x, proceduralBone.m_BindPose);
				float4x = math.inverse(float4x);
				line.a = math.mul(float4x, new float4(localLine.a, 1f)).xyz;
				line.b = math.mul(float4x, new float4(localLine.b, 1f)).xyz;
			}
			while (--num >= 0)
			{
				int index = ptr[num];
				MeshNode meshNode = nodes[index];
				if (!MathUtils.Intersect(meshNode.m_Bounds, line, out var _))
				{
					continue;
				}
				for (int j = meshNode.m_IndexRange.x; j < meshNode.m_IndexRange.y; j += 3)
				{
					Triangle3 triangle = new Triangle3(vertices[indices[j].m_Index].m_Vertex, vertices[indices[j + 1].m_Index].m_Vertex, vertices[indices[j + 2].m_Index].m_Vertex);
					if (MathUtils.Intersect(triangle, line, out var t2) && t2.z < hit.m_NormalizedDistance)
					{
						hit.m_HitDirection = MathUtils.NormalCW(triangle);
						hit.m_NormalizedDistance = t2.z;
						hit.m_CellIndex = elementIndex;
						result = true;
					}
				}
				ptr[num] = meshNode.m_SubNodes1.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.x != -1);
				ptr[num] = meshNode.m_SubNodes1.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.y != -1);
				ptr[num] = meshNode.m_SubNodes1.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.z != -1);
				ptr[num] = meshNode.m_SubNodes1.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes1.w != -1);
				ptr[num] = meshNode.m_SubNodes2.x;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.x != -1);
				ptr[num] = meshNode.m_SubNodes2.y;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.y != -1);
				ptr[num] = meshNode.m_SubNodes2.z;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.z != -1);
				ptr[num] = meshNode.m_SubNodes2.w;
				num = math.select(num, num + 1, meshNode.m_SubNodes2.w != -1);
			}
		}
		return result;
	}
}
