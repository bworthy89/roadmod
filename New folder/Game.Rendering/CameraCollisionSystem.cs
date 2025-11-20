using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.City;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class CameraCollisionSystem : GameSystemBase
{
	[BurstCompile]
	private struct FindEntitiesFromTreeJob : IJob
	{
		private struct Iterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Line3.Segment m_Line;

			public float3 m_Expand;

			public NativeList<Entity> m_EntityList;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				if (MathUtils.Intersect(MathUtils.Expand(bounds.m_Bounds, m_Expand), m_Line, out var _))
				{
					return (bounds.m_Mask & BoundsMask.NotOverridden) != 0;
				}
				return false;
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
			{
				if (Intersect(bounds))
				{
					m_EntityList.Add(in entity);
				}
			}
		}

		[ReadOnly]
		public Line3.Segment m_Line;

		[ReadOnly]
		public quaternion m_Rotation;

		[ReadOnly]
		public float2 m_FovOffset;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_SearchTree;

		[WriteOnly]
		public NativeList<Entity> m_EntityList;

		public void Execute()
		{
			float3 x = math.mul(m_Rotation, new float3(m_FovOffset.x, 0f, 0f));
			float3 x2 = math.mul(m_Rotation, new float3(0f, m_FovOffset.y, 0f));
			float3 expand = math.abs(x) + math.abs(x2);
			Iterator iterator = new Iterator
			{
				m_Line = m_Line,
				m_Expand = expand,
				m_EntityList = m_EntityList
			};
			m_SearchTree.Iterate(ref iterator);
		}
	}

	[BurstCompile]
	private struct ObjectCollisionJob : IJobParallelForDefer
	{
		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Tree> m_TreeData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.NetObject> m_NetObjectData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Stack> m_StackData;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> m_UnderConstructionData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> m_OutsideConnectionData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<MeshData> m_PrefabMeshData;

		[ReadOnly]
		public ComponentLookup<ImpostorData> m_PrefabImpostorData;

		[ReadOnly]
		public ComponentLookup<SharedMeshData> m_PrefabSharedMeshData;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> m_PrefabGrowthScaleData;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> m_PrefabQuantityObjectData;

		[ReadOnly]
		public ComponentLookup<StackData> m_PrefabStackData;

		[ReadOnly]
		public BufferLookup<MeshGroup> m_MeshGroups;

		[ReadOnly]
		public BufferLookup<Skeleton> m_Skeletons;

		[ReadOnly]
		public BufferLookup<Bone> m_Bones;

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

		[ReadOnly]
		public Line3.Segment m_Line;

		[ReadOnly]
		public quaternion m_Rotation;

		[ReadOnly]
		public float2 m_FovOffset;

		[ReadOnly]
		public float m_MinClearRange;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeList<Entity> m_EntityList;

		[NativeDisableContainerSafetyRestriction]
		public NativeQueue<Collision>.ParallelWriter m_Collisions;

		public void Execute(int index)
		{
			Entity entity = m_EntityList[index];
			Game.Objects.Transform transform = m_TransformData[entity];
			PrefabRef prefabRef = m_PrefabRefData[entity];
			if (!m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				return;
			}
			if ((componentData.m_Flags & GeometryFlags.Marker) != GeometryFlags.None)
			{
				if (!m_OutsideConnectionData.HasComponent(entity))
				{
					return;
				}
			}
			else if ((componentData.m_Flags & GeometryFlags.Physical) == 0)
			{
				return;
			}
			quaternion q = math.inverse(transform.m_Rotation);
			Line line = default(Line);
			line.m_Line.a = math.mul(q, m_Line.a - transform.m_Position);
			line.m_Line.b = math.mul(q, m_Line.b - transform.m_Position);
			line.m_XVector = math.mul(q, math.mul(m_Rotation, new float3(m_FovOffset.x, 0f, 0f)));
			line.m_YVector = math.mul(q, math.mul(m_Rotation, new float3(0f, m_FovOffset.y, 0f)));
			line.m_Expand = math.abs(line.m_XVector) + math.abs(line.m_YVector);
			line.m_Scale = 1f;
			line.m_CutOffset = default(float2);
			Stack componentData2;
			StackData componentData3;
			Bounds3 bounds = ((!m_StackData.TryGetComponent(entity, out componentData2) || !m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out componentData3)) ? ObjectUtils.GetBounds(componentData) : ObjectUtils.GetBounds(componentData2, componentData, componentData3));
			if (Intersect(line, bounds, out var t))
			{
				float num = math.cmax(MathUtils.Size(bounds));
				line.m_CutOffset = new float2(t.x - num, t.y + num);
				line.m_Line = MathUtils.Cut(line.m_Line, line.m_CutOffset);
				NativeList<Collision> collisions = new NativeList<Collision>(Allocator.Temp);
				RaycastMeshes(collisions, entity, prefabRef, line);
				CheckCollisions(collisions, m_MinClearRange, t);
				for (int i = 0; i < collisions.Length; i++)
				{
					Collision value = collisions[i];
					value.m_CoverAreas = 0f;
					m_Collisions.Enqueue(value);
				}
				collisions.Dispose();
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

		private void RaycastMeshes(NativeList<Collision> collisions, Entity entity, PrefabRef prefabRef, Line line)
		{
			if (!m_Meshes.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
			{
				return;
			}
			SubMeshFlags subMeshFlags = SubMeshFlags.DefaultMissingMesh | SubMeshFlags.HasTransform;
			subMeshFlags = (SubMeshFlags)((uint)subMeshFlags | (uint)(m_LeftHandTraffic ? 65536 : 131072));
			int3 tileCounts = 0;
			float3 offsets = 0f;
			float3 scale = 0f;
			if (m_TreeData.TryGetComponent(entity, out var componentData))
			{
				subMeshFlags = ((!m_PrefabGrowthScaleData.TryGetComponent(prefabRef.m_Prefab, out var componentData2)) ? (subMeshFlags | SubMeshFlags.RequireAdult) : (subMeshFlags | BatchDataHelpers.CalculateTreeSubMeshData(componentData, componentData2, out line.m_Scale)));
			}
			if (m_NetObjectData.TryGetComponent(entity, out var componentData3))
			{
				subMeshFlags |= BatchDataHelpers.CalculateNetObjectSubMeshData(componentData3);
			}
			if (m_QuantityData.TryGetComponent(entity, out var componentData4))
			{
				subMeshFlags = ((!m_PrefabQuantityObjectData.TryGetComponent(prefabRef.m_Prefab, out var componentData5)) ? (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData4, default(QuantityObjectData), m_EditorMode)) : (subMeshFlags | BatchDataHelpers.CalculateQuantitySubMeshData(componentData4, componentData5, m_EditorMode)));
			}
			if ((m_UnderConstructionData.TryGetComponent(entity, out var componentData6) && componentData6.m_NewPrefab == Entity.Null) || (m_DestroyedData.HasComponent(entity) && m_PrefabObjectGeometryData.TryGetComponent(prefabRef.m_Prefab, out var componentData7) && (componentData7.m_Flags & (GeometryFlags.Physical | GeometryFlags.HasLot)) == (GeometryFlags.Physical | GeometryFlags.HasLot)))
			{
				return;
			}
			if (m_StackData.TryGetComponent(entity, out var componentData8) && m_PrefabStackData.TryGetComponent(prefabRef.m_Prefab, out var componentData9))
			{
				subMeshFlags |= BatchDataHelpers.CalculateStackSubMeshData(componentData8, componentData9, out tileCounts, out offsets, out scale);
			}
			else
			{
				componentData9 = default(StackData);
			}
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
					if (entity2 == Entity.Null || (m_PrefabMeshData[entity2].m_State & MeshFlags.Decal) != 0)
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
					for (int k = 0; k < falseValue; k++)
					{
						SubMesh subMesh2 = subMesh;
						Line line2 = line;
						if ((subMesh2.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd)) != 0)
						{
							BatchDataHelpers.CalculateStackSubMeshData(componentData9, offsets, scale, k, subMesh.m_Flags, ref subMesh2.m_Position, ref line2.m_Scale);
						}
						if ((subMesh2.m_Flags & (SubMeshFlags.IsStackStart | SubMeshFlags.IsStackMiddle | SubMeshFlags.IsStackEnd | SubMeshFlags.HasTransform)) != 0)
						{
							quaternion q = math.inverse(subMesh2.m_Rotation);
							line2.m_Line.a = math.mul(q, line2.m_Line.a - subMesh2.m_Position);
							line2.m_Line.b = math.mul(q, line2.m_Line.b - subMesh2.m_Position);
							line2.m_XVector = math.mul(q, line2.m_XVector);
							line2.m_YVector = math.mul(q, line2.m_YVector);
						}
						if (m_PrefabImpostorData.TryGetComponent(entity2, out var componentData10) && componentData10.m_Size != 0f)
						{
							line2.m_Scale *= componentData10.m_Size;
							line2.m_Line.a -= componentData10.m_Offset;
							line2.m_Line.b -= componentData10.m_Offset;
						}
						line2.m_Expand = math.abs(line2.m_XVector) + math.abs(line2.m_YVector);
						if (bufferData5.IsCreated)
						{
							if (bufferData6.IsCreated)
							{
								if (bufferData7.IsCreated)
								{
									int index = j - subMeshGroup.m_SubMeshRange.x + value.m_MeshOffset;
									CheckMeshIntersect(line2, vertices, indices, bufferData5, bufferData6, bufferData7, dynamicBuffer[index], collisions);
								}
								else
								{
									CheckMeshIntersect(line2, vertices, indices, bufferData5, bufferData6, collisions);
								}
							}
							else
							{
								CheckMeshIntersect(line2, vertices, indices, bufferData5, collisions);
							}
						}
						else
						{
							CheckMeshIntersect(line2, vertices, indices, collisions);
						}
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct SelectCameraPositionJob : IJob
	{
		[ReadOnly]
		public Line3.Segment m_Line;

		[ReadOnly]
		public float3 m_PreviousPosition;

		[ReadOnly]
		public float m_MinClearRange;

		[ReadOnly]
		public float m_NearPlaneRange;

		[ReadOnly]
		public float m_Smoothing;

		[ReadOnly]
		public float m_DeltaTime;

		[ReadOnly]
		public TerrainHeightData m_TerrainData;

		[ReadOnly]
		public WaterSurfaceData<SurfaceWater> m_WaterData;

		public NativeQueue<Collision> m_Collisions;

		public NativeReference<Result> m_Result;

		public void Execute()
		{
			ref Result reference = ref m_Result.ValueAsRef();
			NativeList<Collision> collisions = new NativeList<Collision>(m_Collisions.Count, Allocator.Temp);
			Collision item;
			while (m_Collisions.TryDequeue(out item))
			{
				collisions.Add(in item);
			}
			CheckCollisions(collisions, m_MinClearRange, new float2(0f, 1f));
			MathUtils.Distance(m_Line, reference.m_Position, out var t);
			MathUtils.Distance(m_Line, m_PreviousPosition, out var t2);
			float num = 0f;
			float num2 = (t + t2) * 0.5f;
			float num3 = t;
			float num4 = float.MaxValue;
			float2 @float = t;
			bool flag = true;
			for (int i = 0; i <= collisions.Length; i++)
			{
				float num5 = 1f;
				float num6 = 1f;
				if (i < collisions.Length)
				{
					Collision collision = collisions[i];
					num5 = collision.m_LineBounds.min;
					num6 = collision.m_LineBounds.max;
				}
				if (num5 - num >= m_MinClearRange)
				{
					float num7;
					float num8;
					if (num > t)
					{
						num7 = num;
						num8 = math.select(float.MaxValue, math.abs(num7 - num2), CheckOffset(num7));
					}
					else if (num5 - m_MinClearRange < t)
					{
						num7 = num5 - m_MinClearRange;
						num8 = math.select(float.MaxValue, math.abs(num7 - num2), CheckOffset(num7));
					}
					else
					{
						num7 = t;
						num8 = 0f;
					}
					if (num8 < num4 || (flag && num8 != float.MaxValue))
					{
						num3 = num7;
						num4 = num8;
						@float = new float2(num, num5) - m_NearPlaneRange;
						flag = false;
					}
				}
				else if (flag)
				{
					float num9;
					float num10;
					if (num > t)
					{
						num9 = num;
						num10 = math.select(float.MaxValue, math.abs(num9 - num2), CheckOffset(num9));
					}
					else if (num5 - m_MinClearRange < t)
					{
						num9 = num5 - m_MinClearRange;
						num10 = math.select(float.MaxValue, math.abs(num9 - num2), CheckOffset(num9));
					}
					else
					{
						num9 = t;
						num10 = float.MaxValue;
					}
					if (num10 < num4)
					{
						num3 = num9;
						num4 = num10;
						@float = new float2(num, num5) - m_NearPlaneRange;
					}
				}
				num = num6;
			}
			@float.x = math.min(@float.x, num3);
			@float.y = math.max(@float.y, num3);
			num3 += (reference.m_Offset - (num3 - t)) * math.pow(m_Smoothing, m_DeltaTime);
			num3 = math.clamp(num3, @float.x, @float.y);
			if (num3 != t)
			{
				reference.m_Position = MathUtils.Position(m_Line, num3);
			}
			reference.m_Offset = num3 - t;
			collisions.Dispose();
		}

		private bool CheckOffset(float offset)
		{
			float3 worldPosition = MathUtils.Position(m_Line, offset);
			return worldPosition.y >= WaterUtils.SampleHeight(ref m_WaterData, ref m_TerrainData, worldPosition) + m_MinClearRange;
		}
	}

	private struct Line
	{
		public Line3.Segment m_Line;

		public float3 m_XVector;

		public float3 m_YVector;

		public float2 m_CutOffset;

		public float3 m_Expand;

		public float3 m_Scale;
	}

	private struct Collision : IComparable<Collision>
	{
		public Bounds1 m_LineBounds;

		public float2 m_CoverAreas;

		public bool2 m_StartEnd;

		public int CompareTo(Collision other)
		{
			return m_LineBounds.min.CompareTo(other.m_LineBounds.min);
		}
	}

	private struct Result
	{
		public float3 m_Position;

		public float m_Offset;
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Tree> __Game_Objects_Tree_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.NetObject> __Game_Objects_NetObject_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Stack> __Game_Objects_Stack_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<UnderConstruction> __Game_Objects_UnderConstruction_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MeshData> __Game_Prefabs_MeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ImpostorData> __Game_Prefabs_ImpostorData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SharedMeshData> __Game_Prefabs_SharedMeshData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GrowthScaleData> __Game_Prefabs_GrowthScaleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<QuantityObjectData> __Game_Prefabs_QuantityObjectData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<StackData> __Game_Prefabs_StackData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<MeshGroup> __Game_Rendering_MeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Skeleton> __Game_Rendering_Skeleton_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Bone> __Game_Rendering_Bone_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMesh> __Game_Prefabs_SubMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubMeshGroup> __Game_Prefabs_SubMeshGroup_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LodMesh> __Game_Prefabs_LodMesh_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshVertex> __Game_Prefabs_MeshVertex_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshIndex> __Game_Prefabs_MeshIndex_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<MeshNode> __Game_Prefabs_MeshNode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_Tree_RO_ComponentLookup = state.GetComponentLookup<Tree>(isReadOnly: true);
			__Game_Objects_NetObject_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.NetObject>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Objects_Stack_RO_ComponentLookup = state.GetComponentLookup<Stack>(isReadOnly: true);
			__Game_Objects_UnderConstruction_RO_ComponentLookup = state.GetComponentLookup<UnderConstruction>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_MeshData_RO_ComponentLookup = state.GetComponentLookup<MeshData>(isReadOnly: true);
			__Game_Prefabs_ImpostorData_RO_ComponentLookup = state.GetComponentLookup<ImpostorData>(isReadOnly: true);
			__Game_Prefabs_SharedMeshData_RO_ComponentLookup = state.GetComponentLookup<SharedMeshData>(isReadOnly: true);
			__Game_Prefabs_GrowthScaleData_RO_ComponentLookup = state.GetComponentLookup<GrowthScaleData>(isReadOnly: true);
			__Game_Prefabs_QuantityObjectData_RO_ComponentLookup = state.GetComponentLookup<QuantityObjectData>(isReadOnly: true);
			__Game_Prefabs_StackData_RO_ComponentLookup = state.GetComponentLookup<StackData>(isReadOnly: true);
			__Game_Rendering_MeshGroup_RO_BufferLookup = state.GetBufferLookup<MeshGroup>(isReadOnly: true);
			__Game_Rendering_Skeleton_RO_BufferLookup = state.GetBufferLookup<Skeleton>(isReadOnly: true);
			__Game_Rendering_Bone_RO_BufferLookup = state.GetBufferLookup<Bone>(isReadOnly: true);
			__Game_Prefabs_SubMesh_RO_BufferLookup = state.GetBufferLookup<SubMesh>(isReadOnly: true);
			__Game_Prefabs_SubMeshGroup_RO_BufferLookup = state.GetBufferLookup<SubMeshGroup>(isReadOnly: true);
			__Game_Prefabs_LodMesh_RO_BufferLookup = state.GetBufferLookup<LodMesh>(isReadOnly: true);
			__Game_Prefabs_MeshVertex_RO_BufferLookup = state.GetBufferLookup<MeshVertex>(isReadOnly: true);
			__Game_Prefabs_MeshIndex_RO_BufferLookup = state.GetBufferLookup<MeshIndex>(isReadOnly: true);
			__Game_Prefabs_MeshNode_RO_BufferLookup = state.GetBufferLookup<MeshNode>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
		}
	}

	private CityConfigurationSystem m_CityConfigurationSystem;

	private ToolSystem m_ToolSystem;

	private SearchSystem m_ObjectSearchSystem;

	private TerrainSystem m_TerrainSystem;

	private WaterSystem m_WaterSystem;

	private float3 m_PreviousPosition;

	private quaternion m_Rotation;

	private float m_MaxForwardOffset;

	private float m_MaxBackwardOffset;

	private float m_MinClearDistance;

	private float m_NearPlane;

	private float m_Smoothing;

	private float2 m_FieldOfView;

	private NativeReference<Result> m_Result;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<SearchSystem>();
		m_TerrainSystem = base.World.GetOrCreateSystemManaged<TerrainSystem>();
		m_WaterSystem = base.World.GetOrCreateSystemManaged<WaterSystem>();
		m_Result = new NativeReference<Result>(Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Result.Dispose();
		base.OnDestroy();
	}

	public void CheckCollisions(ref float3 position, float3 previousPosition, quaternion rotation, float maxForwardOffset, float maxBackwardOffset, float minClearDistance, float nearPlane, float smoothing, float2 fieldOfView)
	{
		if (!m_ToolSystem.actionMode.IsEditor())
		{
			m_Result.ValueAsRef().m_Position = position;
			m_PreviousPosition = previousPosition;
			m_Rotation = rotation;
			m_MaxForwardOffset = maxForwardOffset;
			m_MaxBackwardOffset = maxBackwardOffset;
			m_MinClearDistance = minClearDistance;
			m_NearPlane = nearPlane;
			m_Smoothing = smoothing;
			m_FieldOfView = fieldOfView;
			Update();
			position = m_Result.ValueAsRef().m_Position;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		float3 @float = m_Result.Value.m_Position;
		float3 float2 = math.forward(m_Rotation);
		float num = m_MaxForwardOffset + m_MinClearDistance;
		float num2 = m_MaxBackwardOffset;
		Line3.Segment line = new Line3.Segment(@float - float2 * num2, @float + float2 * num);
		float2 fovOffset = math.tan(math.radians(m_FieldOfView) * 0.5f) * m_MinClearDistance;
		float minClearRange = m_MinClearDistance / (num + num2);
		float nearPlaneRange = m_NearPlane / (num + num2);
		NativeList<Entity> nativeList = new NativeList<Entity>(Allocator.TempJob);
		NativeQueue<Collision> collisions = new NativeQueue<Collision>(Allocator.TempJob);
		JobHandle dependencies;
		FindEntitiesFromTreeJob jobData = new FindEntitiesFromTreeJob
		{
			m_Line = line,
			m_Rotation = m_Rotation,
			m_FovOffset = fovOffset,
			m_SearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_EntityList = nativeList
		};
		ObjectCollisionJob jobData2 = new ObjectCollisionJob
		{
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TreeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Tree_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_NetObject_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Stack_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnderConstructionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_UnderConstruction_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OutsideConnectionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabImpostorData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ImpostorData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSharedMeshData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SharedMeshData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGrowthScaleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GrowthScaleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabQuantityObjectData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_QuantityObjectData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabStackData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_StackData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_MeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_Skeletons = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Skeleton_RO_BufferLookup, ref base.CheckedStateRef),
			m_Bones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Rendering_Bone_RO_BufferLookup, ref base.CheckedStateRef),
			m_Meshes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubMeshGroups = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubMeshGroup_RO_BufferLookup, ref base.CheckedStateRef),
			m_Lods = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LodMesh_RO_BufferLookup, ref base.CheckedStateRef),
			m_Vertices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshVertex_RO_BufferLookup, ref base.CheckedStateRef),
			m_Indices = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshIndex_RO_BufferLookup, ref base.CheckedStateRef),
			m_Nodes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_MeshNode_RO_BufferLookup, ref base.CheckedStateRef),
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_Line = line,
			m_Rotation = m_Rotation,
			m_FovOffset = fovOffset,
			m_MinClearRange = minClearRange,
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_EditorMode = m_ToolSystem.actionMode.IsEditor(),
			m_EntityList = nativeList,
			m_Collisions = collisions.AsParallelWriter()
		};
		JobHandle deps;
		SelectCameraPositionJob jobData3 = new SelectCameraPositionJob
		{
			m_Line = line,
			m_PreviousPosition = m_PreviousPosition,
			m_MinClearRange = minClearRange,
			m_NearPlaneRange = nearPlaneRange,
			m_Smoothing = m_Smoothing,
			m_DeltaTime = UnityEngine.Time.deltaTime,
			m_TerrainData = m_TerrainSystem.GetHeightData(),
			m_WaterData = m_WaterSystem.GetSurfaceData(out deps),
			m_Collisions = collisions,
			m_Result = m_Result
		};
		JobHandle jobHandle = IJobExtensions.Schedule(jobData, dependencies);
		JobHandle jobHandle2 = jobData2.Schedule(nativeList, 1, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData3, JobHandle.CombineDependencies(jobHandle2, deps));
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_TerrainSystem.AddCPUHeightReader(jobHandle3);
		m_WaterSystem.AddSurfaceReader(jobHandle3);
		nativeList.Dispose(jobHandle2);
		collisions.Dispose(jobHandle3);
		jobHandle3.Complete();
	}

	private static void CheckCollisions(NativeList<Collision> collisions, float minClearRange, float2 limits)
	{
		if (collisions.Length == 0)
		{
			return;
		}
		collisions.Sort();
		int num = 0;
		Collision value = collisions[0];
		for (int i = 1; i < collisions.Length; i++)
		{
			Collision collision = collisions[i];
			if (collision.m_LineBounds.min - value.m_LineBounds.max < minClearRange)
			{
				value.m_LineBounds.max = math.max(value.m_LineBounds.max, collision.m_LineBounds.max);
				value.m_CoverAreas += collision.m_CoverAreas;
				value.m_StartEnd |= collision.m_StartEnd;
			}
			else
			{
				value.m_StartEnd &= value.m_CoverAreas >= value.m_CoverAreas.yx * 0.5f;
				collisions[num++] = value;
				value = collision;
			}
		}
		value.m_StartEnd &= value.m_CoverAreas >= value.m_CoverAreas.yx * 0.5f;
		collisions[num++] = value;
		collisions.RemoveRange(num, collisions.Length - num);
		num = 0;
		value = collisions[0];
		if (!value.m_StartEnd.x)
		{
			value.m_LineBounds.min = math.min(value.m_LineBounds.min, limits.x);
			value.m_StartEnd.x = true;
		}
		for (int j = 1; j < collisions.Length; j++)
		{
			Collision collision2 = collisions[j];
			if (!value.m_StartEnd.y || !collision2.m_StartEnd.x)
			{
				value.m_LineBounds.max = collision2.m_LineBounds.max;
				value.m_CoverAreas += collision2.m_CoverAreas;
				value.m_StartEnd.y = collision2.m_StartEnd.y;
			}
			else
			{
				collisions[num++] = value;
				value = collision2;
			}
		}
		if (!value.m_StartEnd.y)
		{
			value.m_LineBounds.max = math.max(value.m_LineBounds.max, limits.y);
			value.m_StartEnd.y = true;
		}
		collisions[num++] = value;
		collisions.RemoveRange(num, collisions.Length - num);
	}

	private static bool Intersect(Line line, Bounds3 bounds, out float2 t)
	{
		bounds = MathUtils.Expand(bounds, line.m_Expand) * line.m_Scale;
		return MathUtils.Intersect(bounds, line.m_Line, out t);
	}

	private static void CheckTriangleIntersect(Line line, Triangle3 triangle, NativeList<Collision> collisions)
	{
		triangle *= line.m_Scale;
		if (!MathUtils.Intersect(MathUtils.Expand(MathUtils.Bounds(triangle), line.m_Expand), line.m_Line, out var _))
		{
			return;
		}
		float3 x = triangle.a - line.m_Line.a;
		float3 x2 = triangle.b - line.m_Line.a;
		float3 x3 = triangle.c - line.m_Line.a;
		Bounds2 bounds = default(Bounds2);
		bounds.max = new float2(math.lengthsq(line.m_XVector), math.lengthsq(line.m_YVector));
		bounds.min = -bounds.max;
		Triangle2 triangle2 = default(Triangle2);
		triangle2.a = new float2(math.dot(x, line.m_XVector), math.dot(x, line.m_YVector));
		triangle2.b = new float2(math.dot(x2, line.m_XVector), math.dot(x2, line.m_YVector));
		triangle2.c = new float2(math.dot(x3, line.m_XVector), math.dot(x3, line.m_YVector));
		if (!MathUtils.Intersect(bounds, triangle2, out var area))
		{
			return;
		}
		float3 @float = line.m_Line.b - line.m_Line.a;
		float3 y = @float * (1f / math.lengthsq(@float));
		Triangle1 triangle3 = default(Triangle1);
		triangle3.a = math.dot(x, y);
		triangle3.b = math.dot(x2, y);
		triangle3.c = math.dot(x3, y);
		Bounds1 bounds2 = new Bounds1(float.MaxValue, float.MinValue);
		if (MathUtils.Intersect(bounds, triangle2.ab, out var t2))
		{
			t2 = math.lerp(triangle3.a, triangle3.b, t2);
			bounds2.min = math.min(bounds2.min, math.cmin(t2));
			bounds2.max = math.max(bounds2.max, math.cmax(t2));
		}
		if (MathUtils.Intersect(bounds, triangle2.bc, out t2))
		{
			t2 = math.lerp(triangle3.b, triangle3.c, t2);
			bounds2.min = math.min(bounds2.min, math.cmin(t2));
			bounds2.max = math.max(bounds2.max, math.cmax(t2));
		}
		if (MathUtils.Intersect(bounds, triangle2.ca, out t2))
		{
			t2 = math.lerp(triangle3.c, triangle3.a, t2);
			bounds2.min = math.min(bounds2.min, math.cmin(t2));
			bounds2.max = math.max(bounds2.max, math.cmax(t2));
		}
		if (MathUtils.Intersect(triangle2, bounds.min, out t2))
		{
			bounds2 |= MathUtils.Position(triangle3, t2);
		}
		if (MathUtils.Intersect(triangle2, new float2(bounds.max.x, bounds.min.y), out t2))
		{
			bounds2 |= MathUtils.Position(triangle3, t2);
		}
		if (MathUtils.Intersect(triangle2, new float2(bounds.min.x, bounds.max.y), out t2))
		{
			bounds2 |= MathUtils.Position(triangle3, t2);
		}
		if (MathUtils.Intersect(triangle2, bounds.max, out t2))
		{
			bounds2 |= MathUtils.Position(triangle3, t2);
		}
		if (bounds2.min <= 1f && bounds2.max >= 0f)
		{
			Collision value = default(Collision);
			value.m_LineBounds.min = math.lerp(line.m_CutOffset.x, line.m_CutOffset.y, bounds2.min);
			value.m_LineBounds.max = math.lerp(line.m_CutOffset.x, line.m_CutOffset.y, bounds2.max);
			if (MathUtils.IsClockwise(triangle2))
			{
				value.m_CoverAreas = new float2(area, 0f);
				value.m_StartEnd = new bool2(x: true, y: false);
			}
			else
			{
				value.m_CoverAreas = new float2(0f, area);
				value.m_StartEnd = new bool2(x: false, y: true);
			}
			collisions.Add(in value);
		}
	}

	private static void CheckMeshIntersect(Line line, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, NativeList<Collision> collisions)
	{
		for (int i = 0; i < indices.Length; i += 3)
		{
			Triangle3 triangle = new Triangle3(vertices[indices[i].m_Index].m_Vertex, vertices[indices[i + 1].m_Index].m_Vertex, vertices[indices[i + 2].m_Index].m_Vertex);
			CheckTriangleIntersect(line, triangle, collisions);
		}
	}

	private unsafe static void CheckMeshIntersect(Line line, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, NativeList<Collision> collisions)
	{
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
			if (Intersect(line, meshNode.m_Bounds, out var _))
			{
				for (int i = meshNode.m_IndexRange.x; i < meshNode.m_IndexRange.y; i += 3)
				{
					Triangle3 triangle = new Triangle3(vertices[indices[i].m_Index].m_Vertex, vertices[indices[i + 1].m_Index].m_Vertex, vertices[indices[i + 2].m_Index].m_Vertex);
					CheckTriangleIntersect(line, triangle, collisions);
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
	}

	private unsafe static void CheckMeshIntersect(Line line, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, DynamicBuffer<ProceduralBone> prefabBones, NativeList<Collision> collisions)
	{
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
				if (Intersect(line, meshNode.m_Bounds, out var _))
				{
					for (int j = meshNode.m_IndexRange.x; j < meshNode.m_IndexRange.y; j += 3)
					{
						Triangle3 triangle = new Triangle3(vertices[indices[j].m_Index].m_Vertex, vertices[indices[j + 1].m_Index].m_Vertex, vertices[indices[j + 2].m_Index].m_Vertex);
						CheckTriangleIntersect(line, triangle, collisions);
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
		}
	}

	private unsafe static void CheckMeshIntersect(Line line, DynamicBuffer<MeshVertex> vertices, DynamicBuffer<MeshIndex> indices, DynamicBuffer<MeshNode> nodes, DynamicBuffer<ProceduralBone> prefabBones, DynamicBuffer<Bone> bones, Skeleton skeleton, NativeList<Collision> collisions)
	{
		int* ptr = stackalloc int[128];
		for (int i = 0; i < prefabBones.Length; i++)
		{
			int num = 0;
			Line line2 = line;
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
				line2.m_Line.a = math.mul(float4x, new float4(line.m_Line.a, 1f)).xyz;
				line2.m_Line.b = math.mul(float4x, new float4(line.m_Line.b, 1f)).xyz;
				line2.m_XVector = math.mul(float4x, new float4(line.m_XVector, 0f)).xyz;
				line2.m_YVector = math.mul(float4x, new float4(line.m_YVector, 0f)).xyz;
				line2.m_Expand = math.abs(line2.m_XVector) + math.abs(line2.m_YVector);
			}
			while (--num >= 0)
			{
				int index = ptr[num];
				MeshNode meshNode = nodes[index];
				if (Intersect(line2, meshNode.m_Bounds, out var _))
				{
					for (int j = meshNode.m_IndexRange.x; j < meshNode.m_IndexRange.y; j += 3)
					{
						CheckTriangleIntersect(triangle: new Triangle3(vertices[indices[j].m_Index].m_Vertex, vertices[indices[j + 1].m_Index].m_Vertex, vertices[indices[j + 2].m_Index].m_Vertex), line: line2, collisions: collisions);
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
		}
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
	public CameraCollisionSystem()
	{
	}
}
