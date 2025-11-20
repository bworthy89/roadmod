using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Areas;

public static class RaycastJobs
{
	[BurstCompile]
	public struct FindAreaJob : IJobParallelFor
	{
		private struct ValidationData
		{
			public bool m_EditorMode;

			public RaycastInput m_Input;

			public ComponentLookup<Owner> m_OwnerData;

			public ComponentLookup<Placeholder> m_PlaceholderData;

			public ComponentLookup<Attachment> m_AttachmentData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

			public bool ValidateResult(ref RaycastResult result)
			{
				if (!m_EditorMode)
				{
					PrefabRef prefabRef = m_PrefabRefData[result.m_Owner];
					if ((m_PrefabAreaData[prefabRef.m_Prefab].m_Flags & GeometryFlags.HiddenIngame) != 0)
					{
						return false;
					}
				}
				TypeMask typeMask = TypeMask.Areas;
				Entity owner = Entity.Null;
				TypeMask typeMask2 = TypeMask.None;
				while (true)
				{
					if ((m_Input.m_Flags & RaycastFlags.UpgradeIsMain) != 0)
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
					else if ((m_Input.m_Flags & RaycastFlags.SubBuildings) != 0 && m_BuildingData.HasComponent(result.m_Owner) && m_ServiceUpgradeData.HasComponent(result.m_Owner))
					{
						break;
					}
					if (!m_OwnerData.HasComponent(result.m_Owner))
					{
						break;
					}
					if ((m_Input.m_TypeMask & typeMask) != TypeMask.None)
					{
						owner = result.m_Owner;
						typeMask2 = typeMask;
					}
					result.m_Owner = m_OwnerData[result.m_Owner].m_Owner;
					if (!m_Nodes.HasBuffer(result.m_Owner))
					{
						typeMask = TypeMask.StaticObjects;
					}
				}
				if ((m_Input.m_Flags & RaycastFlags.SubElements) != 0 && (m_Input.m_TypeMask & typeMask2) != TypeMask.None)
				{
					result.m_Owner = owner;
					typeMask = typeMask2;
				}
				else if ((m_Input.m_Flags & RaycastFlags.NoMainElements) != 0)
				{
					return false;
				}
				if ((m_Input.m_TypeMask & typeMask) == 0)
				{
					return false;
				}
				switch (typeMask)
				{
				case TypeMask.Areas:
				{
					PrefabRef prefabRef2 = m_PrefabRefData[result.m_Owner];
					return (AreaUtils.GetTypeMask(m_PrefabAreaData[prefabRef2.m_Prefab].m_Type) & m_Input.m_AreaTypeMask) != 0;
				}
				case TypeMask.StaticObjects:
					return CheckPlaceholder(ref result.m_Owner);
				default:
					return true;
				}
			}

			private bool CheckPlaceholder(ref Entity entity)
			{
				if ((m_Input.m_Flags & RaycastFlags.Placeholders) != 0)
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
		}

		private struct GroundIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public int m_Index;

			public RaycastResult m_Result;

			public ComponentLookup<Space> m_SpaceData;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public ValidationData m_ValidationData;

			public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Result.m_Hit.m_HitPosition.xz);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (MathUtils.Intersect(bounds.m_Bounds.xz, m_Result.m_Hit.m_HitPosition.xz) && !m_SpaceData.HasComponent(areaItem.m_Area) && MathUtils.Intersect(AreaUtils.GetTriangle3(m_Nodes[areaItem.m_Area], m_Triangles[areaItem.m_Area][areaItem.m_Triangle]).xz, m_Result.m_Hit.m_HitPosition.xz, out var _))
				{
					RaycastResult result = m_Result;
					result.m_Owner = areaItem.m_Area;
					if (m_ValidationData.ValidateResult(ref result))
					{
						m_Results.Accumulate(m_Index, result);
					}
				}
			}
		}

		private struct OvergroundIterator : INativeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, QuadTreeBoundsXZ>
		{
			public int m_Index;

			public Line3.Segment m_Line;

			public ComponentLookup<Space> m_SpaceData;

			public BufferLookup<Node> m_Nodes;

			public BufferLookup<Triangle> m_Triangles;

			public ValidationData m_ValidationData;

			public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				float2 t;
				return MathUtils.Intersect(bounds.m_Bounds, m_Line, out t);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, AreaSearchItem areaItem)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Line, out var _) || !m_SpaceData.HasComponent(areaItem.m_Area))
				{
					return;
				}
				Triangle3 triangle = AreaUtils.GetTriangle3(m_Nodes[areaItem.m_Area], m_Triangles[areaItem.m_Area][areaItem.m_Triangle]);
				if (MathUtils.Intersect(triangle, m_Line, out var t2))
				{
					RaycastResult result = default(RaycastResult);
					result.m_Owner = areaItem.m_Area;
					result.m_Hit.m_HitEntity = result.m_Owner;
					result.m_Hit.m_Position = MathUtils.Position(m_Line, t2.z);
					result.m_Hit.m_HitPosition = result.m_Hit.m_Position;
					result.m_Hit.m_HitDirection = MathUtils.NormalCW(triangle);
					result.m_Hit.m_NormalizedDistance = t2.z - 1f / math.max(1f, MathUtils.Length(m_Line));
					result.m_Hit.m_CellIndex = new int2(-1, -1);
					if (m_ValidationData.ValidateResult(ref result))
					{
						m_Results.Accumulate(m_Index, result);
					}
				}
			}
		}

		[ReadOnly]
		public bool m_EditorMode;

		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Space> m_SpaceData;

		[ReadOnly]
		public ComponentLookup<Placeholder> m_PlaceholderData;

		[ReadOnly]
		public ComponentLookup<Attachment> m_AttachmentData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.ServiceUpgrade> m_ServiceUpgradeData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<AreaGeometryData> m_PrefabAreaData;

		[ReadOnly]
		public BufferLookup<Node> m_Nodes;

		[ReadOnly]
		public BufferLookup<Triangle> m_Triangles;

		[ReadOnly]
		public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;

		[ReadOnly]
		public NativeQuadTree<AreaSearchItem, QuadTreeBoundsXZ> m_SearchTree;

		[ReadOnly]
		public NativeArray<RaycastResult> m_TerrainResults;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(int index)
		{
			int index2 = index % m_Input.Length;
			RaycastInput input = m_Input[index2];
			if ((input.m_TypeMask & TypeMask.Areas) == 0)
			{
				return;
			}
			ValidationData validationData = new ValidationData
			{
				m_EditorMode = m_EditorMode,
				m_Input = input,
				m_OwnerData = m_OwnerData,
				m_PlaceholderData = m_PlaceholderData,
				m_AttachmentData = m_AttachmentData,
				m_BuildingData = m_BuildingData,
				m_ServiceUpgradeData = m_ServiceUpgradeData,
				m_PrefabRefData = m_PrefabRefData,
				m_PrefabAreaData = m_PrefabAreaData,
				m_Nodes = m_Nodes,
				m_InstalledUpgrades = m_InstalledUpgrades
			};
			if (index < m_TerrainResults.Length)
			{
				RaycastResult result = m_TerrainResults[index];
				if (!(result.m_Owner == Entity.Null))
				{
					result.m_Owner = Entity.Null;
					result.m_Hit.m_CellIndex = new int2(-1, -1);
					result.m_Hit.m_NormalizedDistance -= 0.25f / math.max(1f, MathUtils.Length(input.m_Line));
					GroundIterator iterator = new GroundIterator
					{
						m_Index = index2,
						m_Result = result,
						m_SpaceData = m_SpaceData,
						m_Nodes = m_Nodes,
						m_Triangles = m_Triangles,
						m_ValidationData = validationData,
						m_Results = m_Results
					};
					m_SearchTree.Iterate(ref iterator);
				}
			}
			else
			{
				OvergroundIterator iterator2 = new OvergroundIterator
				{
					m_Index = index2,
					m_Line = input.m_Line,
					m_SpaceData = m_SpaceData,
					m_Nodes = m_Nodes,
					m_Triangles = m_Triangles,
					m_ValidationData = validationData,
					m_Results = m_Results
				};
				m_SearchTree.Iterate(ref iterator2);
			}
		}
	}

	[BurstCompile]
	public struct RaycastLabelsJob : IJobChunk
	{
		[ReadOnly]
		public NativeArray<RaycastInput> m_Input;

		[ReadOnly]
		public float3 m_CameraRight;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Geometry> m_GeometryType;

		[ReadOnly]
		public BufferTypeHandle<LabelExtents> m_LabelExtentsType;

		[NativeDisableContainerSafetyRestriction]
		public NativeAccumulator<RaycastResult>.ParallelWriter m_Results;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Geometry> nativeArray2 = chunk.GetNativeArray(ref m_GeometryType);
			BufferAccessor<LabelExtents> bufferAccessor = chunk.GetBufferAccessor(ref m_LabelExtentsType);
			quaternion labelRotation = AreaUtils.CalculateLabelRotation(m_CameraRight);
			Quad3 quad = default(Quad3);
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				Geometry geometry = nativeArray2[i];
				DynamicBuffer<LabelExtents> dynamicBuffer = bufferAccessor[i];
				float3 labelPosition = AreaUtils.CalculateLabelPosition(geometry);
				for (int j = 0; j < m_Input.Length; j++)
				{
					RaycastInput raycastInput = m_Input[j];
					if ((raycastInput.m_TypeMask & TypeMask.Labels) == 0)
					{
						continue;
					}
					float4x4 a = AreaUtils.CalculateLabelMatrix(raycastInput.m_Line.a, labelPosition, labelRotation);
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						Bounds2 bounds = dynamicBuffer[k].m_Bounds;
						quad.a = math.transform(a, new float3(bounds.min.xy, 0f));
						quad.b = math.transform(a, new float3(bounds.min.x, bounds.max.y, 0f));
						quad.c = math.transform(a, new float3(bounds.max.xy, 0f));
						quad.d = math.transform(a, new float3(bounds.max.x, bounds.min.y, 0f));
						if (MathUtils.Intersect(quad, raycastInput.m_Line, out var t))
						{
							float num = MathUtils.Size(bounds.y) * AreaUtils.CalculateLabelScale(raycastInput.m_Line.a, labelPosition);
							RaycastResult value = default(RaycastResult);
							value.m_Owner = nativeArray[i];
							value.m_Hit.m_HitEntity = value.m_Owner;
							value.m_Hit.m_Position = geometry.m_CenterPosition;
							value.m_Hit.m_HitPosition = MathUtils.Position(raycastInput.m_Line, t);
							value.m_Hit.m_NormalizedDistance = t - num / math.max(1f, MathUtils.Length(raycastInput.m_Line));
							m_Results.Accumulate(j, value);
						}
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}
}
