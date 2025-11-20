using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Objects;

[CompilerGenerated]
public class AlignSystem : GameSystemBase
{
	[BurstCompile]
	private struct AlignJob : IJob
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Aligned> m_AlignedType;

		[ReadOnly]
		public ComponentTypeHandle<Attached> m_AttachedType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Temp> m_TempType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentLookup<Updated> m_UpdatedData;

		[ReadOnly]
		public ComponentLookup<Aligned> m_AlignedData;

		[ReadOnly]
		public ComponentLookup<Attached> m_AttachedData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Node> m_NodeData;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> m_EdgeGeometryData;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> m_NodeGeometryData;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> m_StartNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> m_EndNodeGeometryData;

		[ReadOnly]
		public ComponentLookup<Temp> m_TempData;

		[ReadOnly]
		public ComponentLookup<Hidden> m_HiddenData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PillarData> m_PrefabPillarData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> m_PrefabNetGeometryData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> m_PrefabPlaceableObjectData;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> m_ConnectedEdges;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> m_PrefabSubObjects;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> m_PrefabPlaceholderObjects;

		public ComponentLookup<Transform> m_TransformData;

		public ComponentLookup<Stack> m_StackData;

		public BufferLookup<SubObject> m_SubObjects;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_Chunks;

		[ReadOnly]
		public ComponentTypeSet m_AppliedTypes;

		[ReadOnly]
		public TerrainHeightData m_TerrainHeightData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterSurfaceData;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			for (int i = 0; i < m_Chunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_Chunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				NativeArray<Aligned> nativeArray2 = archetypeChunk.GetNativeArray(ref m_AlignedType);
				NativeArray<Attached> nativeArray3 = archetypeChunk.GetNativeArray(ref m_AttachedType);
				NativeArray<Owner> nativeArray4 = archetypeChunk.GetNativeArray(ref m_OwnerType);
				NativeArray<PrefabRef> nativeArray5 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
				bool isTemp = archetypeChunk.Has(ref m_TempType);
				for (int j = 0; j < nativeArray2.Length; j++)
				{
					Entity entity = nativeArray[j];
					Aligned aligned = nativeArray2[j];
					Owner owner = nativeArray4[j];
					PrefabRef prefabRef = nativeArray5[j];
					Attached attached = default(Attached);
					if (nativeArray3.Length != 0)
					{
						attached = nativeArray3[j];
					}
					if (attached.m_Parent == Entity.Null)
					{
						Transform transform = m_TransformData[entity];
						Transform transform2 = transform;
						Align(entity, aligned, owner, prefabRef, isTemp, ref transform2);
						if (!transform2.Equals(transform))
						{
							MoveObject(entity, transform, transform2);
						}
					}
				}
			}
		}

		private void MoveObject(Entity entity, Transform oldTransform, Transform newTransform)
		{
			m_TransformData[entity] = newTransform;
			if (!m_SubObjects.TryGetBuffer(entity, out var bufferData))
			{
				return;
			}
			Transform inverseParentTransform = ObjectUtils.InverseTransform(oldTransform);
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				if (m_OwnerData.TryGetComponent(subObject, out var componentData) && !(componentData.m_Owner != entity) && m_UpdatedData.HasComponent(subObject) && !m_AlignedData.HasComponent(subObject))
				{
					Transform transform = m_TransformData[subObject];
					Transform newTransform2 = ObjectUtils.LocalToWorld(newTransform, ObjectUtils.WorldToLocal(inverseParentTransform, transform));
					if (!newTransform2.Equals(transform))
					{
						MoveObject(subObject, transform, newTransform2);
					}
				}
			}
		}

		private void Align(Entity entity, Aligned aligned, Owner owner, PrefabRef prefabRef, bool isTemp, ref Transform transform)
		{
			PrefabRef ownerPrefabRef = m_PrefabRefData[owner.m_Owner];
			if (!m_PrefabSubObjects.HasBuffer(ownerPrefabRef.m_Prefab))
			{
				return;
			}
			DynamicBuffer<Game.Prefabs.SubObject> dynamicBuffer = m_PrefabSubObjects[ownerPrefabRef.m_Prefab];
			if (dynamicBuffer.Length == 0)
			{
				return;
			}
			int index = aligned.m_SubObjectIndex % dynamicBuffer.Length;
			Game.Prefabs.SubObject subObject = dynamicBuffer[index];
			PillarData pillarData = new PillarData
			{
				m_Type = PillarType.None
			};
			if (m_PrefabPillarData.HasComponent(prefabRef.m_Prefab))
			{
				pillarData = m_PrefabPillarData[prefabRef.m_Prefab];
				switch (pillarData.m_Type)
				{
				case PillarType.Vertical:
					return;
				case PillarType.Standalone:
					subObject.m_Flags |= SubObjectFlags.AnchorTop;
					subObject.m_Flags |= SubObjectFlags.OnGround;
					break;
				case PillarType.Base:
					subObject.m_Flags |= SubObjectFlags.OnGround;
					break;
				}
			}
			Transform transform2 = new Transform(float3.zero, quaternion.identity);
			if (m_CurveData.HasComponent(owner.m_Owner))
			{
				EdgeGeometry edgeGeometry = m_EdgeGeometryData[owner.m_Owner];
				Curve curve = m_CurveData[owner.m_Owner];
				float3 start;
				float3 end;
				if ((subObject.m_Flags & SubObjectFlags.MiddlePlacement) == 0)
				{
					if (math.distancesq(transform.m_Position, curve.m_Bezier.a) < math.distancesq(transform.m_Position, curve.m_Bezier.d))
					{
						start = edgeGeometry.m_Start.m_Right.a;
						end = edgeGeometry.m_Start.m_Left.a;
					}
					else
					{
						start = edgeGeometry.m_End.m_Left.d;
						end = edgeGeometry.m_End.m_Right.d;
					}
				}
				else if ((subObject.m_Flags & SubObjectFlags.EvenSpacing) != 0)
				{
					float num = MathUtils.Length(curve.m_Bezier.xz);
					int num2 = math.max(1, (int)(num / math.max(1f, subObject.m_Position.z) - 0.5f));
					float num3 = (float)(math.clamp(aligned.m_SubObjectIndex / dynamicBuffer.Length, 0, num2 - 1) + 1) / (float)(num2 + 1);
					subObject.m_Position.z = 0f;
					if (num3 >= 0.5f)
					{
						num3 -= 0.5f;
						edgeGeometry.m_Start = edgeGeometry.m_End;
					}
					num3 = math.saturate(num3 * 2f);
					Bounds1 t = new Bounds1(0f, 1f);
					Bounds1 t2 = new Bounds1(0f, 1f);
					MathUtils.ClampLength(edgeGeometry.m_Start.m_Left.xz, ref t, num3 * MathUtils.Length(edgeGeometry.m_Start.m_Left.xz));
					MathUtils.ClampLength(edgeGeometry.m_Start.m_Right.xz, ref t2, num3 * MathUtils.Length(edgeGeometry.m_Start.m_Right.xz));
					start = MathUtils.Position(edgeGeometry.m_Start.m_Left, t.max);
					end = MathUtils.Position(edgeGeometry.m_Start.m_Right, t2.max);
				}
				else
				{
					start = edgeGeometry.m_Start.m_Left.d;
					end = edgeGeometry.m_Start.m_Right.d;
				}
				float3 position = math.lerp(start, end, 0.5f);
				transform2 = new Transform(position, NetUtils.GetNodeRotation(new float3
				{
					xz = MathUtils.Right(end.xz - start.xz)
				}));
			}
			else if (m_NodeData.HasComponent(owner.m_Owner))
			{
				NodeGeometry nodeGeometry = m_NodeGeometryData[owner.m_Owner];
				Node node = m_NodeData[owner.m_Owner];
				if ((subObject.m_Flags & SubObjectFlags.EdgePlacement) != 0)
				{
					subObject.m_Position.z = 0f;
				}
				transform2 = new Transform(node.m_Position, node.m_Rotation)
				{
					m_Position = 
					{
						y = nodeGeometry.m_Position
					}
				};
			}
			quaternion rotation = transform.m_Rotation;
			transform = ObjectUtils.LocalToWorld(transform2, subObject.m_Position, subObject.m_Rotation);
			if ((subObject.m_Flags & SubObjectFlags.RequireDeadEnd) != 0 && math.dot(math.forward(rotation), math.forward(transform.m_Rotation)) < 0f)
			{
				subObject.m_Rotation = math.mul(quaternion.RotateY(MathF.PI), subObject.m_Rotation);
				transform.m_Rotation = math.mul(transform.m_Rotation, subObject.m_Rotation);
			}
			if (pillarData.m_Type == PillarType.Horizontal)
			{
				Game.Prefabs.SubObject prefabSubObject = subObject;
				prefabSubObject.m_Flags |= SubObjectFlags.AnchorTop;
				prefabSubObject.m_Flags |= SubObjectFlags.OnGround;
				AlignVerticalPillars(entity, aligned, owner, ownerPrefabRef, transform2, prefabSubObject, isTemp, ref prefabRef, ref transform);
			}
			ObjectGeometryData prefabGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			AlignHeight(entity, owner, prefabRef, subObject, prefabGeometryData, transform2, ref transform);
		}

		private void AlignVerticalPillars(Entity entity, Aligned aligned, Owner owner, PrefabRef ownerPrefabRef, Transform parentTransform, Game.Prefabs.SubObject prefabSubObject, bool isTemp, ref PrefabRef prefabRef, ref Transform transform)
		{
			if (FindVerticalPillars(entity, aligned, owner, isTemp, out var pillar, out var pillar2))
			{
				if (pillar2 != Entity.Null)
				{
					AlignDoubleVerticalPillars(entity, pillar, pillar2, owner, ownerPrefabRef, parentTransform, prefabSubObject, ref prefabRef, ref transform);
				}
				else
				{
					AlignSingleVerticalPillar(entity, pillar, selectPrefab: true, owner, ownerPrefabRef, parentTransform, prefabSubObject, ref prefabRef, ref transform);
				}
			}
		}

		private void AlignSingleVerticalPillar(Entity entity, Entity pillar1, bool selectPrefab, Owner owner, PrefabRef ownerPrefabRef, Transform parentTransform, Game.Prefabs.SubObject prefabSubObject, ref PrefabRef prefabRef, ref Transform transform)
		{
			Attached attached = default(Attached);
			if (m_AttachedData.HasComponent(pillar1))
			{
				attached = m_AttachedData[pillar1];
			}
			Transform transform2 = m_TransformData[pillar1];
			if (attached.m_Parent == Entity.Null)
			{
				transform2 = transform;
			}
			PrefabRef prefabRef2 = m_PrefabRefData[pillar1];
			ObjectGeometryData prefabGeometryData = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
			float num = prefabGeometryData.m_Size.x * 0.5f;
			float num2 = 0f;
			Bounds1 bounds = new Bounds1(0f, 1000000f);
			if (m_PrefabNetGeometryData.HasComponent(ownerPrefabRef.m_Prefab))
			{
				num2 = m_PrefabNetGeometryData[ownerPrefabRef.m_Prefab].m_ElevatedWidth - 1f;
			}
			if (attached.m_Parent != Entity.Null)
			{
				float3 x = transform2.m_Position - transform.m_Position;
				x.y = 0f;
				float num3 = math.length(x);
				if (num3 >= 0.1f)
				{
					x /= num3;
					float num4 = (0f - num2) * 0.5f;
					float num5 = math.max(num2 * 0.5f, num3 + num);
					float num6 = (num4 + num5) * 0.5f;
					num2 = num5 - num4;
					bounds = new Bounds1(math.abs(num3 - num6));
					float3 @float = new float3
					{
						xz = MathUtils.Left(x.xz)
					};
					if (math.dot(math.forward(parentTransform.m_Rotation), @float) < 0f)
					{
						@float = -@float;
					}
					transform.m_Position.xz += x.xz * num6;
					transform.m_Rotation = quaternion.LookRotation(@float, math.up());
				}
				AlignRotation(transform.m_Rotation, ref transform2);
			}
			else
			{
				transform2 = transform;
			}
			if (selectPrefab)
			{
				SelectHorizontalPillar(entity, prefabSubObject, num2, bounds, bounds, ref prefabRef);
			}
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			float minHeightOffset = float.MaxValue;
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, transform.m_Position);
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.min.x, 0f, 0f)));
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.max.x, 0f, 0f)));
			transform.m_Position.y += math.select(0f, minHeightOffset, minHeightOffset != float.MaxValue);
			transform2.m_Position.y = transform.m_Position.y;
			AlignHeight(pillar1, owner, prefabRef2, prefabSubObject, prefabGeometryData, parentTransform, ref transform2);
			m_TransformData[pillar1] = transform2;
		}

		private void AlignDoubleVerticalPillars(Entity entity, Entity pillar1, Entity pillar2, Owner owner, PrefabRef ownerPrefabRef, Transform parentTransform, Game.Prefabs.SubObject prefabSubObject, ref PrefabRef prefabRef, ref Transform transform)
		{
			Attached a = default(Attached);
			Attached b = default(Attached);
			if (m_AttachedData.HasComponent(pillar1))
			{
				a = m_AttachedData[pillar1];
			}
			if (m_AttachedData.HasComponent(pillar2))
			{
				b = m_AttachedData[pillar2];
			}
			if (b.m_Parent != Entity.Null && a.m_Parent == Entity.Null)
			{
				CommonUtils.Swap(ref pillar1, ref pillar2);
				CommonUtils.Swap(ref a, ref b);
			}
			Transform transform2 = m_TransformData[pillar1];
			Transform transform3 = m_TransformData[pillar2];
			PrefabRef prefabRef2 = m_PrefabRefData[pillar1];
			PrefabRef prefabRef3 = m_PrefabRefData[pillar2];
			ObjectGeometryData prefabGeometryData = m_PrefabObjectGeometryData[prefabRef2.m_Prefab];
			ObjectGeometryData prefabGeometryData2 = m_PrefabObjectGeometryData[prefabRef3.m_Prefab];
			float num = prefabGeometryData.m_Size.x * 0.5f;
			float num2 = prefabGeometryData2.m_Size.x * 0.5f;
			float num3 = num + num2;
			float num4 = 0f;
			Bounds1 offsetRange = new Bounds1(0f, 1000000f);
			Bounds1 offsetRange2 = new Bounds1(0f, 1000000f);
			if (m_PrefabNetGeometryData.HasComponent(ownerPrefabRef.m_Prefab))
			{
				num4 = m_PrefabNetGeometryData[ownerPrefabRef.m_Prefab].m_ElevatedWidth - 1f;
			}
			if (a.m_Parent != Entity.Null && b.m_Parent != Entity.Null)
			{
				float3 x = transform3.m_Position - transform2.m_Position;
				x.y = 0f;
				float num5 = math.length(x);
				if (num5 < num3)
				{
					RemoveObject(pillar2, b, owner);
					AlignSingleVerticalPillar(entity, pillar1, selectPrefab: true, owner, ownerPrefabRef, parentTransform, prefabSubObject, ref prefabRef, ref transform);
					return;
				}
				x /= num5;
				float num6 = math.dot(x.xz, parentTransform.m_Position.xz - transform2.m_Position.xz);
				float num7 = math.min(num6 - num4 * 0.5f, 0f - num);
				float num8 = math.max(num6 + num4 * 0.5f, num5 + num2);
				num6 = (num7 + num8) * 0.5f;
				num4 = num8 - num7;
				offsetRange = new Bounds1(math.abs(0f - num6));
				offsetRange2 = new Bounds1(math.abs(num5 - num6));
				SelectHorizontalPillar(entity, prefabSubObject, num4, offsetRange, offsetRange2, ref prefabRef);
				float3 @float = new float3
				{
					xz = MathUtils.Left(x.xz)
				};
				if (math.dot(math.forward(parentTransform.m_Rotation), @float) < 0f)
				{
					@float = -@float;
				}
				transform.m_Position.xz = transform2.m_Position.xz + x.xz * num6;
				transform.m_Rotation = quaternion.LookRotation(@float, math.up());
				AlignRotation(transform.m_Rotation, ref transform2);
				AlignRotation(transform.m_Rotation, ref transform3);
			}
			else if (a.m_Parent != Entity.Null)
			{
				float3 x2 = transform2.m_Position - transform.m_Position;
				x2.y = 0f;
				float num9 = math.length(x2);
				if (num9 < num3 * 0.5f)
				{
					RemoveObject(pillar2, b, owner);
					AlignSingleVerticalPillar(entity, pillar1, selectPrefab: true, owner, ownerPrefabRef, parentTransform, prefabSubObject, ref prefabRef, ref transform);
					return;
				}
				x2 /= num9;
				float num10 = math.min((0f - num4) * 0.5f, 0f - num2);
				float num11 = math.max(num4 * 0.5f, num9 + num);
				float num12 = (num10 + num11) * 0.5f;
				num4 = num11 - num10;
				offsetRange = new Bounds1(math.abs(num9 - num12));
				SelectHorizontalPillar(entity, prefabSubObject, num4, offsetRange, offsetRange2, ref prefabRef);
				float num13 = 0f - math.max(MathUtils.Center(m_PrefabPillarData[prefabRef.m_Prefab].m_OffsetRange), num3 - num9);
				float3 float2 = new float3
				{
					xz = MathUtils.Left(x2.xz)
				};
				if (math.dot(math.forward(parentTransform.m_Rotation), float2) < 0f)
				{
					float2 = -float2;
					num13 = 0f - num13;
				}
				transform.m_Position.xz += x2.xz * num12;
				transform.m_Rotation = quaternion.LookRotation(float2, math.up());
				transform3.m_Rotation = transform.m_Rotation;
				transform3.m_Position = ObjectUtils.LocalToWorld(transform, new float3(num13, 0f, 0f));
				AlignRotation(transform.m_Rotation, ref transform2);
			}
			else
			{
				SelectHorizontalPillar(entity, prefabSubObject, num4, offsetRange, offsetRange2, ref prefabRef);
				PillarData pillarData = m_PrefabPillarData[prefabRef.m_Prefab];
				if (pillarData.m_OffsetRange.min <= 0f)
				{
					RemoveObject(pillar2, b, owner);
					AlignSingleVerticalPillar(entity, pillar1, selectPrefab: false, owner, ownerPrefabRef, parentTransform, prefabSubObject, ref prefabRef, ref transform);
					return;
				}
				Transform inverseParentTransform = ObjectUtils.InverseTransform(parentTransform);
				float x3 = ObjectUtils.WorldToLocal(inverseParentTransform, transform2.m_Position).x;
				float x4 = ObjectUtils.WorldToLocal(inverseParentTransform, transform3.m_Position).x;
				float num14 = math.max(MathUtils.Center(pillarData.m_OffsetRange), num3 * 0.5f);
				if (x4 >= x3)
				{
					x3 = 0f - num14;
					x4 = num14;
				}
				else
				{
					x3 = num14;
					x4 = 0f - num14;
				}
				transform2.m_Rotation = transform.m_Rotation;
				transform3.m_Rotation = transform.m_Rotation;
				transform2.m_Position = ObjectUtils.LocalToWorld(transform, new float3(x3, 0f, 0f));
				transform3.m_Position = ObjectUtils.LocalToWorld(transform, new float3(x4, 0f, 0f));
			}
			ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[prefabRef.m_Prefab];
			float minHeightOffset = float.MaxValue;
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, transform.m_Position);
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.min.x, 0f, 0f)));
			LimitNodeHeight(ref minHeightOffset, owner.m_Owner, ObjectUtils.LocalToWorld(transform, new float3(objectGeometryData.m_Bounds.max.x, 0f, 0f)));
			transform.m_Position.y += math.select(0f, minHeightOffset, minHeightOffset != float.MaxValue);
			transform2.m_Position.y = transform.m_Position.y;
			transform3.m_Position.y = transform.m_Position.y;
			AlignHeight(pillar1, owner, prefabRef2, prefabSubObject, prefabGeometryData, parentTransform, ref transform2);
			AlignHeight(pillar2, owner, prefabRef3, prefabSubObject, prefabGeometryData2, parentTransform, ref transform3);
			m_TransformData[pillar1] = transform2;
			m_TransformData[pillar2] = transform3;
		}

		private void LimitNodeHeight(ref float minHeightOffset, Entity owner, float3 position)
		{
			if (!m_NodeGeometryData.TryGetComponent(owner, out var componentData))
			{
				return;
			}
			float bestDistance = float.MaxValue;
			float bestHeight = float.MaxValue;
			EdgeIterator edgeIterator = new EdgeIterator(Entity.Null, owner, m_ConnectedEdges, m_EdgeData, m_TempData, m_HiddenData);
			EdgeIteratorValue value;
			while (edgeIterator.GetNext(out value))
			{
				EdgeNodeGeometry edgeNodeGeometry = ((!value.m_End) ? m_StartNodeGeometryData[value.m_Edge].m_Geometry : m_EndNodeGeometryData[value.m_Edge].m_Geometry);
				if (edgeNodeGeometry.m_Left.m_Length.x >= 0.05f)
				{
					LimitNodeHeight(ref bestDistance, ref bestHeight, edgeNodeGeometry.m_Left.m_Left, position);
				}
				if (edgeNodeGeometry.m_Left.m_Length.y >= 0.05f)
				{
					LimitNodeHeight(ref bestDistance, ref bestHeight, edgeNodeGeometry.m_Left.m_Right, position);
				}
				if (edgeNodeGeometry.m_Right.m_Length.x >= 0.05f)
				{
					LimitNodeHeight(ref bestDistance, ref bestHeight, edgeNodeGeometry.m_Right.m_Left, position);
				}
				if (edgeNodeGeometry.m_Right.m_Length.y >= 0.05f)
				{
					LimitNodeHeight(ref bestDistance, ref bestHeight, edgeNodeGeometry.m_Right.m_Right, position);
				}
			}
			minHeightOffset = math.min(minHeightOffset, bestHeight - componentData.m_Position);
		}

		private void LimitNodeHeight(ref float bestDistance, ref float bestHeight, Bezier4x3 curve, float3 position)
		{
			float t;
			float num = MathUtils.Distance(curve.xz, position.xz, out t);
			if (num < bestDistance)
			{
				bestDistance = num;
				bestHeight = MathUtils.Position(curve.y, t);
			}
		}

		private void SelectHorizontalPillar(Entity entity, Game.Prefabs.SubObject prefabSubObject, float targetWidth, Bounds1 offsetRange1, Bounds1 offsetRange2, ref PrefabRef prefabRef)
		{
			if (!m_PrefabPlaceholderObjects.HasBuffer(prefabSubObject.m_Prefab))
			{
				return;
			}
			DynamicBuffer<PlaceholderObjectElement> dynamicBuffer = m_PrefabPlaceholderObjects[prefabSubObject.m_Prefab];
			float num = float.MinValue;
			Entity entity2 = prefabRef.m_Prefab;
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity entity3 = dynamicBuffer[i].m_Object;
				if (!m_PrefabPillarData.HasComponent(entity3))
				{
					continue;
				}
				PillarData pillarData = m_PrefabPillarData[entity3];
				if (pillarData.m_Type == PillarType.Horizontal && m_PrefabObjectGeometryData.HasComponent(entity3))
				{
					ObjectGeometryData objectGeometryData = m_PrefabObjectGeometryData[entity3];
					float max = pillarData.m_OffsetRange.max;
					float num2 = 1f / (1f + math.abs(objectGeometryData.m_Size.x - targetWidth));
					num2 += 0.01f / (1f + math.max(0f, max));
					if (!MathUtils.Intersect(pillarData.m_OffsetRange, offsetRange1))
					{
						num2 -= 1f + math.max(offsetRange1.min - pillarData.m_OffsetRange.max, pillarData.m_OffsetRange.min - offsetRange1.max);
					}
					if (!MathUtils.Intersect(pillarData.m_OffsetRange, offsetRange2))
					{
						num2 -= 1f + math.max(offsetRange2.min - pillarData.m_OffsetRange.max, pillarData.m_OffsetRange.min - offsetRange2.max);
					}
					if (num2 > num)
					{
						num = num2;
						entity2 = entity3;
					}
				}
			}
			if (entity2 != prefabRef.m_Prefab)
			{
				prefabRef.m_Prefab = entity2;
				m_CommandBuffer.SetComponent(entity, prefabRef);
			}
		}

		private bool FindVerticalPillars(Entity entity, Aligned aligned, Owner owner, bool isTemp, out Entity pillar1, out Entity pillar2)
		{
			pillar1 = Entity.Null;
			pillar2 = Entity.Null;
			if (owner.m_Owner == Entity.Null)
			{
				return false;
			}
			if (isTemp && !m_TempData.HasComponent(owner.m_Owner))
			{
				for (int i = 0; i < m_Chunks.Length; i++)
				{
					ArchetypeChunk archetypeChunk = m_Chunks[i];
					if (!archetypeChunk.Has(ref m_TempType))
					{
						continue;
					}
					NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
					NativeArray<Aligned> nativeArray2 = archetypeChunk.GetNativeArray(ref m_AlignedType);
					NativeArray<Owner> nativeArray3 = archetypeChunk.GetNativeArray(ref m_OwnerType);
					NativeArray<PrefabRef> nativeArray4 = archetypeChunk.GetNativeArray(ref m_PrefabRefType);
					for (int j = 0; j < nativeArray.Length; j++)
					{
						Entity entity2 = nativeArray[j];
						if (entity2 == entity || nativeArray2[j].m_SubObjectIndex != aligned.m_SubObjectIndex || nativeArray3[j].m_Owner != owner.m_Owner)
						{
							continue;
						}
						PrefabRef prefabRef = nativeArray4[j];
						if (m_PrefabPillarData.HasComponent(prefabRef.m_Prefab) && m_PrefabPillarData[prefabRef.m_Prefab].m_Type == PillarType.Vertical)
						{
							if (pillar1 == Entity.Null)
							{
								pillar1 = entity2;
							}
							else if (pillar2 == Entity.Null)
							{
								pillar2 = entity2;
							}
						}
					}
				}
			}
			else
			{
				if (!m_SubObjects.HasBuffer(owner.m_Owner))
				{
					return false;
				}
				DynamicBuffer<SubObject> dynamicBuffer = m_SubObjects[owner.m_Owner];
				for (int k = 0; k < dynamicBuffer.Length; k++)
				{
					SubObject subObject = dynamicBuffer[k];
					if (subObject.m_SubObject == entity || !m_AlignedData.HasComponent(subObject.m_SubObject) || m_AlignedData[subObject.m_SubObject].m_SubObjectIndex != aligned.m_SubObjectIndex || !m_OwnerData.HasComponent(subObject.m_SubObject) || m_OwnerData[subObject.m_SubObject].m_Owner != owner.m_Owner)
					{
						continue;
					}
					PrefabRef prefabRef2 = m_PrefabRefData[subObject.m_SubObject];
					if (m_PrefabPillarData.HasComponent(prefabRef2.m_Prefab) && m_PrefabPillarData[prefabRef2.m_Prefab].m_Type == PillarType.Vertical)
					{
						if (pillar1 == Entity.Null)
						{
							pillar1 = subObject.m_SubObject;
						}
						else if (pillar2 == Entity.Null)
						{
							pillar2 = subObject.m_SubObject;
						}
					}
				}
			}
			return pillar1 != Entity.Null;
		}

		private void AlignHeight(Entity entity, Owner owner, PrefabRef prefabRef, Game.Prefabs.SubObject prefabSubObject, ObjectGeometryData prefabGeometryData, Transform parentTransform, ref Transform transform)
		{
			if ((prefabSubObject.m_Flags & SubObjectFlags.AnchorTop) != 0)
			{
				transform.m_Position.y -= prefabGeometryData.m_Bounds.max.y;
				if (m_PrefabPlaceableObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					transform.m_Position.y += componentData.m_PlacementOffset.y;
				}
			}
			else if ((prefabSubObject.m_Flags & SubObjectFlags.AnchorCenter) != 0)
			{
				float num = (prefabGeometryData.m_Bounds.max.y - prefabGeometryData.m_Bounds.min.y) * 0.5f;
				transform.m_Position.y -= num;
			}
			float terrainHeight = transform.m_Position.y;
			float num2 = transform.m_Position.y;
			if ((prefabSubObject.m_Flags & SubObjectFlags.OnGround) != 0)
			{
				WaterUtils.SampleHeight(ref m_WaterSurfaceData, ref m_TerrainHeightData, transform.m_Position, out terrainHeight, out var waterHeight, out var waterDepth);
				num2 = math.select(terrainHeight, waterHeight, waterDepth >= 0.2f);
			}
			if (m_StackData.TryGetComponent(entity, out var componentData2))
			{
				componentData2.m_Range.min = num2 - transform.m_Position.y + prefabGeometryData.m_Bounds.min.y;
				componentData2.m_Range.max = prefabGeometryData.m_Bounds.max.y;
				if (m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData3))
				{
					if (num2 > terrainHeight)
					{
						componentData2.m_Range.min = math.min(componentData2.m_Range.min, componentData2.m_Range.max - MathUtils.Size(componentData3.m_FirstBounds) - MathUtils.Size(componentData3.m_LastBounds));
						componentData2.m_Range.min = math.max(componentData2.m_Range.min, terrainHeight - transform.m_Position.y + prefabGeometryData.m_Bounds.min.y);
					}
					BatchDataHelpers.AlignStack(ref componentData2, componentData3, start: false, end: true);
				}
				m_StackData[entity] = componentData2;
			}
			else if ((prefabSubObject.m_Flags & (SubObjectFlags.AnchorTop | SubObjectFlags.AnchorCenter | SubObjectFlags.OnGround)) == SubObjectFlags.OnGround)
			{
				transform.m_Position.y = num2;
			}
		}

		private void AlignRotation(quaternion targetRotation, ref Transform transform)
		{
			quaternion quaternion = math.mul(quaternion.RotateY(MathF.PI / 2f), transform.m_Rotation);
			quaternion quaternion2 = math.mul(quaternion.RotateY(MathF.PI), transform.m_Rotation);
			quaternion quaternion3 = math.mul(quaternion.RotateY(-MathF.PI / 2f), transform.m_Rotation);
			float num = MathUtils.RotationAngle(targetRotation, transform.m_Rotation);
			float num2 = MathUtils.RotationAngle(targetRotation, quaternion);
			float num3 = MathUtils.RotationAngle(targetRotation, quaternion2);
			float num4 = MathUtils.RotationAngle(targetRotation, quaternion3);
			if (num2 < num)
			{
				num = num2;
				transform.m_Rotation = quaternion;
			}
			if (num3 < num)
			{
				num = num3;
				transform.m_Rotation = quaternion2;
			}
			if (num4 < num)
			{
				num = num4;
				transform.m_Rotation = quaternion3;
			}
		}

		private void RemoveObject(Entity entity, Attached attached, Owner owner)
		{
			m_CommandBuffer.RemoveComponent(entity, in m_AppliedTypes);
			m_CommandBuffer.AddComponent<Deleted>(entity);
			if (attached.m_Parent != owner.m_Owner && m_SubObjects.HasBuffer(attached.m_Parent))
			{
				CollectionUtils.RemoveValue(m_SubObjects[attached.m_Parent], new SubObject(entity));
			}
			if (m_SubObjects.HasBuffer(owner.m_Owner))
			{
				CollectionUtils.RemoveValue(m_SubObjects[owner.m_Owner], new SubObject(entity));
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Aligned> __Game_Objects_Aligned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Attached> __Game_Objects_Attached_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Temp> __Game_Tools_Temp_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<Updated> __Game_Common_Updated_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Aligned> __Game_Objects_Aligned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Attached> __Game_Objects_Attached_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Node> __Game_Net_Node_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeGeometry> __Game_Net_EdgeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NodeGeometry> __Game_Net_NodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StartNodeGeometry> __Game_Net_StartNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EndNodeGeometry> __Game_Net_EndNodeGeometry_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Temp> __Game_Tools_Temp_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Hidden> __Game_Tools_Hidden_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PillarData> __Game_Prefabs_PillarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetGeometryData> __Game_Prefabs_NetGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PlaceableObjectData> __Game_Prefabs_PlaceableObjectData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedEdge> __Game_Net_ConnectedEdge_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Prefabs.SubObject> __Game_Prefabs_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<PlaceholderObjectElement> __Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup;

		public ComponentLookup<Transform> __Game_Objects_Transform_RW_ComponentLookup;

		public ComponentLookup<Stack> __Game_Objects_Stack_RW_ComponentLookup;

		public BufferLookup<SubObject> __Game_Objects_SubObject_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Aligned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Aligned>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Attached>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Temp>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Common_Updated_RO_ComponentLookup = state.GetComponentLookup<Updated>(isReadOnly: true);
			__Game_Objects_Aligned_RO_ComponentLookup = state.GetComponentLookup<Aligned>(isReadOnly: true);
			__Game_Objects_Attached_RO_ComponentLookup = state.GetComponentLookup<Attached>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Edge>(isReadOnly: true);
			__Game_Net_Node_RO_ComponentLookup = state.GetComponentLookup<Node>(isReadOnly: true);
			__Game_Net_EdgeGeometry_RO_ComponentLookup = state.GetComponentLookup<EdgeGeometry>(isReadOnly: true);
			__Game_Net_NodeGeometry_RO_ComponentLookup = state.GetComponentLookup<NodeGeometry>(isReadOnly: true);
			__Game_Net_StartNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<StartNodeGeometry>(isReadOnly: true);
			__Game_Net_EndNodeGeometry_RO_ComponentLookup = state.GetComponentLookup<EndNodeGeometry>(isReadOnly: true);
			__Game_Tools_Temp_RO_ComponentLookup = state.GetComponentLookup<Temp>(isReadOnly: true);
			__Game_Tools_Hidden_RO_ComponentLookup = state.GetComponentLookup<Hidden>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PillarData_RO_ComponentLookup = state.GetComponentLookup<PillarData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_NetGeometryData_RO_ComponentLookup = state.GetComponentLookup<NetGeometryData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup = state.GetComponentLookup<PlaceableObjectData>(isReadOnly: true);
			__Game_Net_ConnectedEdge_RO_BufferLookup = state.GetBufferLookup<ConnectedEdge>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Prefabs.SubObject>(isReadOnly: true);
			__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup = state.GetBufferLookup<PlaceholderObjectElement>(isReadOnly: true);
			__Game_Objects_Transform_RW_ComponentLookup = state.GetComponentLookup<Transform>();
			__Game_Objects_Stack_RW_ComponentLookup = state.GetComponentLookup<Stack>();
			__Game_Objects_SubObject_RW_BufferLookup = state.GetBufferLookup<SubObject>();
		}
	}

	private ModificationBarrier4 m_ModificationBarrier;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private EntityQuery m_UpdateQuery;

	private ComponentTypeSet m_AppliedTypes;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier4>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_UpdateQuery = GetEntityQuery(ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Aligned>(), ComponentType.Exclude<Deleted>());
		m_AppliedTypes = new ComponentTypeSet(ComponentType.ReadWrite<Applied>(), ComponentType.ReadWrite<Created>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_UpdateQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> chunks = m_UpdateQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle deps;
		JobHandle jobHandle = IJobExtensions.Schedule(new AlignJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_AlignedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Aligned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AttachedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TempType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UpdatedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Updated_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AlignedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Aligned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AttachedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Attached_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Node_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StartNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_StartNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EndNodeGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EndNodeGeometry_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TempData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Temp_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HiddenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Tools_Hidden_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPillarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PillarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabNetGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_NetGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPlaceableObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PlaceableObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedEdges = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ConnectedEdge_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabSubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabPlaceholderObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceholderObjectElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RW_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RW_ComponentLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RW_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = chunks,
			m_AppliedTypes = m_AppliedTypes,
			m_TerrainHeightData = m_TerrainSystem.GetHeightData(waitForPending: true),
			m_WaterSurfaceData = m_WaterSystem.GetSurfaceData(out deps),
			m_CommandBuffer = m_ModificationBarrier.CreateCommandBuffer()
		}, JobHandle.CombineDependencies(base.Dependency, outJobHandle, deps));
		chunks.Dispose(jobHandle);
		m_ModificationBarrier.AddJobHandleForProducer(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle);
		m_WaterSystem.AddSurfaceReader(jobHandle);
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
	public AlignSystem()
	{
	}
}
