using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Common;
using Game.Vehicles;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class VehicleInitializeSystem : GameSystemBase
{
	[BurstCompile]
	private struct InitializeVehiclesJob : IJobParallelFor
	{
		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> m_ObjectGeometryType;

		[ReadOnly]
		public ComponentTypeHandle<CarTrailerData> m_CarTrailerType;

		[ReadOnly]
		public ComponentTypeHandle<MultipleUnitTrainData> m_MultipleUnitTrainType;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> m_SubmeshType;

		public ComponentTypeHandle<CarData> m_CarType;

		public ComponentTypeHandle<TrainData> m_TrainType;

		public ComponentTypeHandle<SwayingData> m_SwayingType;

		public ComponentTypeHandle<VehicleData> m_VehicleType;

		[ReadOnly]
		public BufferLookup<ProceduralBone> m_ProceduralBones;

		[DeallocateOnJobCompletion]
		[ReadOnly]
		public NativeArray<ArchetypeChunk> m_Chunks;

		public void Execute(int index)
		{
			ArchetypeChunk archetypeChunk = m_Chunks[index];
			NativeArray<ObjectGeometryData> nativeArray = archetypeChunk.GetNativeArray(ref m_ObjectGeometryType);
			NativeArray<CarTrailerData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_CarTrailerType);
			NativeArray<CarData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_CarType);
			NativeArray<TrainData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_TrainType);
			NativeArray<SwayingData> nativeArray5 = archetypeChunk.GetNativeArray(ref m_SwayingType);
			NativeArray<VehicleData> nativeArray6 = archetypeChunk.GetNativeArray(ref m_VehicleType);
			BufferAccessor<SubMesh> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_SubmeshType);
			bool flag = archetypeChunk.Has(ref m_MultipleUnitTrainType);
			for (int i = 0; i < nativeArray6.Length; i++)
			{
				VehicleData value = nativeArray6[i];
				value.m_SteeringBoneIndex = -1;
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<SubMesh> dynamicBuffer = bufferAccessor[i];
					int num = 0;
					for (int j = 0; j < dynamicBuffer.Length; j++)
					{
						SubMesh subMesh = dynamicBuffer[j];
						if (!m_ProceduralBones.TryGetBuffer(subMesh.m_SubMesh, out var bufferData))
						{
							continue;
						}
						for (int k = 0; k < bufferData.Length; k++)
						{
							if (bufferData[k].m_Type == BoneType.SteeringRotation)
							{
								value.m_SteeringBoneIndex = num + k;
							}
						}
						num += bufferData.Length;
					}
				}
				nativeArray6[i] = value;
			}
			for (int l = 0; l < nativeArray3.Length; l++)
			{
				ObjectGeometryData objectGeometryData = nativeArray[l];
				CarData value2 = nativeArray3[l];
				SwayingData value3 = nativeArray5[l];
				Bounds3 bounds = new Bounds3(float.MaxValue, float.MinValue);
				float3 @float = 0f;
				float3 float2 = 0f;
				int3 @int = 0;
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<SubMesh> dynamicBuffer2 = bufferAccessor[l];
					for (int m = 0; m < dynamicBuffer2.Length; m++)
					{
						SubMesh subMesh2 = dynamicBuffer2[m];
						if (!m_ProceduralBones.TryGetBuffer(subMesh2.m_SubMesh, out var bufferData2))
						{
							continue;
						}
						for (int n = 0; n < bufferData2.Length; n++)
						{
							ProceduralBone bone = bufferData2[n];
							BoneType type = bone.m_Type;
							if ((uint)(type - 4) <= 1u || type == BoneType.FixedTire)
							{
								float3 float3 = bone.m_ObjectPosition;
								if ((subMesh2.m_Flags & SubMeshFlags.HasTransform) != 0)
								{
									float3 = subMesh2.m_Position + math.rotate(subMesh2.m_Rotation, float3);
								}
								bounds |= float3;
								if (HasSteering(bufferData2, bone))
								{
									float2 += float3;
									@int.yz++;
								}
								else
								{
									@float += float3;
									@int.xz++;
								}
							}
						}
					}
				}
				if (@int.x != 0)
				{
					value2.m_PivotOffset = @float.z / (float)@int.x;
				}
				else if (@int.y != 0)
				{
					value2.m_PivotOffset = float2.z / (float)@int.y;
				}
				else
				{
					value2.m_PivotOffset = objectGeometryData.m_Size.z * -0.2f;
				}
				if (nativeArray2.Length != 0)
				{
					bounds |= nativeArray2[l].m_AttachPosition;
				}
				float2 float4;
				float num2;
				if (@int.z != 0)
				{
					float4 = math.max(0.5f, MathUtils.Size(bounds.xz) * 0.5f);
					num2 = (@float.y + float2.y) / (float)@int.z;
				}
				else
				{
					float4 = objectGeometryData.m_Size.xz * new float2(0.45f, 0.3f);
					num2 = objectGeometryData.m_Size.y * 0.25f;
				}
				float3 float5 = math.max(1f, objectGeometryData.m_Size * objectGeometryData.m_Size);
				float3 float6 = math.max(1f, value3.m_SpringFactors);
				value3.m_SpringFactors.x *= 1f + objectGeometryData.m_Size.y * (1f / 3f);
				float6.x *= 1f + objectGeometryData.m_Size.y * (1f / 6f);
				value3.m_VelocityFactors.xz = (objectGeometryData.m_Size.y * 0.5f - num2) * 12f / (float5.yy + float5.xz);
				value3.m_VelocityFactors.y = 1f;
				value3.m_DampingFactors = 1f / float6;
				value3.m_MaxPosition = math.length(objectGeometryData.m_Size) * 3f / (new float3(float4.x, 1f, float4.y) * float6);
				value3.m_SpringFactors.xz *= float4 * 12f / (float5.yy + float5.xz);
				if (@int.z != 0 && bounds.max.x - bounds.min.x < objectGeometryData.m_Size.x * 0.1f)
				{
					value3.m_VelocityFactors.x *= -0.4f;
					value3.m_SpringFactors.x *= 0.1f;
					value3.m_MaxPosition.x *= 5f;
				}
				nativeArray3[l] = value2;
				nativeArray5[l] = value3;
			}
			for (int num3 = 0; num3 < nativeArray4.Length; num3++)
			{
				ObjectGeometryData objectGeometryData2 = nativeArray[num3];
				TrainData value4 = nativeArray4[num3];
				Bounds3 bounds2 = new Bounds3(float.MaxValue, float.MinValue);
				int num4 = 0;
				if (flag)
				{
					value4.m_TrainFlags |= TrainFlags.MultiUnit;
				}
				if (bufferAccessor.Length != 0)
				{
					DynamicBuffer<SubMesh> dynamicBuffer3 = bufferAccessor[num3];
					for (int num5 = 0; num5 < dynamicBuffer3.Length; num5++)
					{
						SubMesh subMesh3 = dynamicBuffer3[num5];
						if (!m_ProceduralBones.TryGetBuffer(subMesh3.m_SubMesh, out var bufferData3))
						{
							continue;
						}
						for (int num6 = 0; num6 < bufferData3.Length; num6++)
						{
							ProceduralBone proceduralBone = bufferData3[num6];
							switch (proceduralBone.m_Type)
							{
							case BoneType.TrainBogie:
							{
								float3 float7 = proceduralBone.m_ObjectPosition;
								if ((subMesh3.m_Flags & SubMeshFlags.HasTransform) != 0)
								{
									float7 = subMesh3.m_Position + math.rotate(subMesh3.m_Rotation, float7);
								}
								bounds2 |= float7;
								num4++;
								break;
							}
							case BoneType.PantographRotation:
								value4.m_TrainFlags |= TrainFlags.Pantograph;
								break;
							}
						}
					}
				}
				if (num4 >= 2)
				{
					value4.m_BogieOffsets = new float2(bounds2.max.z, 0f - bounds2.min.z) - value4.m_BogieOffsets;
				}
				else if (num4 == 1)
				{
					float num7 = MathUtils.Size(objectGeometryData2.m_Bounds.x) * 0.5f;
					value4.m_BogieOffsets = MathUtils.Center(bounds2.z) + num7 - value4.m_BogieOffsets;
				}
				else
				{
					float2 float8 = new float2(objectGeometryData2.m_Bounds.max.z, 0f - objectGeometryData2.m_Bounds.min.z);
					value4.m_BogieOffsets = float8 - MathUtils.Size(objectGeometryData2.m_Bounds.z) * 0.15f - value4.m_BogieOffsets;
				}
				nativeArray4[num3] = value4;
			}
		}

		private bool HasSteering(DynamicBuffer<ProceduralBone> bones, ProceduralBone bone)
		{
			if (bone.m_Type == BoneType.SteeringTire || bone.m_Type == BoneType.SteeringRotation || bone.m_Type == BoneType.SteeringSuspension)
			{
				return true;
			}
			while (bone.m_ParentIndex >= 0)
			{
				bone = bones[bone.m_ParentIndex];
				if (bone.m_Type == BoneType.SteeringTire || bone.m_Type == BoneType.SteeringRotation || bone.m_Type == BoneType.SteeringSuspension)
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
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MultipleUnitTrainData> __Game_Prefabs_MultipleUnitTrainData_RO_ComponentTypeHandle;

		public ComponentTypeHandle<TrainData> __Game_Prefabs_TrainData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarData> __Game_Prefabs_CarData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<SwayingData> __Game_Prefabs_SwayingData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<VehicleData> __Game_Prefabs_VehicleData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarTractorData> __Game_Prefabs_CarTractorData_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarTrailerData> __Game_Prefabs_CarTrailerData_RW_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubMesh> __Game_Prefabs_SubMesh_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferLookup<ProceduralBone> __Game_Prefabs_ProceduralBone_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_MultipleUnitTrainData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MultipleUnitTrainData>(isReadOnly: true);
			__Game_Prefabs_TrainData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<TrainData>();
			__Game_Prefabs_CarData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarData>();
			__Game_Prefabs_SwayingData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<SwayingData>();
			__Game_Prefabs_VehicleData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<VehicleData>();
			__Game_Prefabs_CarTractorData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarTractorData>();
			__Game_Prefabs_CarTrailerData_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarTrailerData>();
			__Game_Prefabs_SubMesh_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubMesh>(isReadOnly: true);
			__Game_Prefabs_ProceduralBone_RO_BufferLookup = state.GetBufferLookup<ProceduralBone>(isReadOnly: true);
		}
	}

	private EntityQuery m_PrefabQuery;

	private PrefabSystem m_PrefabSystem;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_PrefabQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<PrefabData>()
			},
			Any = new ComponentType[4]
			{
				ComponentType.ReadWrite<CarData>(),
				ComponentType.ReadWrite<TrainData>(),
				ComponentType.ReadWrite<CarTractorData>(),
				ComponentType.ReadWrite<CarTrailerData>()
			}
		});
		RequireForUpdate(m_PrefabQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeArray<ArchetypeChunk> chunks = m_PrefabQuery.ToArchetypeChunkArray(Allocator.TempJob);
		ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<ObjectGeometryData> typeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<MultipleUnitTrainData> componentTypeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MultipleUnitTrainData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<TrainData> typeHandle3 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TrainData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CarData> typeHandle4 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<SwayingData> typeHandle5 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SwayingData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<VehicleData> componentTypeHandle2 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_VehicleData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CarTractorData> typeHandle6 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarTractorData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		ComponentTypeHandle<CarTrailerData> typeHandle7 = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CarTrailerData_RW_ComponentTypeHandle, ref base.CheckedStateRef);
		BufferTypeHandle<SubMesh> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubMesh_RO_BufferTypeHandle, ref base.CheckedStateRef);
		CompleteDependency();
		for (int i = 0; i < chunks.Length; i++)
		{
			ArchetypeChunk archetypeChunk = chunks[i];
			NativeArray<PrefabData> nativeArray = archetypeChunk.GetNativeArray(ref typeHandle);
			NativeArray<ObjectGeometryData> nativeArray2 = archetypeChunk.GetNativeArray(ref typeHandle2);
			NativeArray<TrainData> nativeArray3 = archetypeChunk.GetNativeArray(ref typeHandle3);
			NativeArray<CarData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle4);
			NativeArray<SwayingData> nativeArray5 = archetypeChunk.GetNativeArray(ref typeHandle5);
			NativeArray<CarTractorData> nativeArray6 = archetypeChunk.GetNativeArray(ref typeHandle6);
			NativeArray<CarTrailerData> nativeArray7 = archetypeChunk.GetNativeArray(ref typeHandle7);
			for (int j = 0; j < nativeArray3.Length; j++)
			{
				TrainPrefab prefab = m_PrefabSystem.GetPrefab<TrainPrefab>(nativeArray[j]);
				ObjectGeometryData objectGeometryData = nativeArray2[j];
				TrainData value = nativeArray3[j];
				float2 @float = new float2(objectGeometryData.m_Bounds.max.z, 0f - objectGeometryData.m_Bounds.min.z);
				value.m_TrackType = prefab.m_TrackType;
				value.m_EnergyType = prefab.m_EnergyType;
				value.m_MaxSpeed = prefab.m_MaxSpeed / 3.6f;
				value.m_Acceleration = prefab.m_Acceleration;
				value.m_Braking = prefab.m_Braking;
				value.m_Turning = math.radians(prefab.m_Turning);
				value.m_BogieOffsets = prefab.m_BogieOffset;
				value.m_AttachOffsets = @float - prefab.m_AttachOffset;
				nativeArray3[j] = value;
			}
			for (int k = 0; k < nativeArray4.Length; k++)
			{
				VehiclePrefab prefab2 = m_PrefabSystem.GetPrefab<VehiclePrefab>(nativeArray[k]);
				CarData value2 = nativeArray4[k];
				SwayingData value3 = nativeArray5[k];
				if (prefab2 is CarBasePrefab carBasePrefab)
				{
					value2.m_SizeClass = carBasePrefab.m_SizeClass;
					value2.m_EnergyType = carBasePrefab.m_EnergyType;
					value2.m_MaxSpeed = carBasePrefab.m_MaxSpeed / 3.6f;
					value2.m_Acceleration = carBasePrefab.m_Acceleration;
					value2.m_Braking = carBasePrefab.m_Braking;
					value2.m_Turning = math.radians(carBasePrefab.m_Turning);
					value3.m_SpringFactors = carBasePrefab.m_Stiffness;
				}
				else if (prefab2 is BicyclePrefab bicyclePrefab)
				{
					value2.m_SizeClass = SizeClass.Small;
					value2.m_EnergyType = EnergyTypes.None;
					value2.m_MaxSpeed = bicyclePrefab.m_MaxSpeed / 3.6f;
					value2.m_Acceleration = bicyclePrefab.m_Acceleration;
					value2.m_Braking = bicyclePrefab.m_Braking;
					value2.m_Turning = math.radians(bicyclePrefab.m_Turning);
					value3.m_SpringFactors = bicyclePrefab.m_Stiffness;
				}
				nativeArray4[k] = value2;
				nativeArray5[k] = value3;
			}
			for (int l = 0; l < nativeArray6.Length; l++)
			{
				CarTractor component = m_PrefabSystem.GetPrefab<VehiclePrefab>(nativeArray[l]).GetComponent<CarTractor>();
				ObjectGeometryData objectGeometryData2 = nativeArray2[l];
				CarTractorData value4 = nativeArray6[l];
				value4.m_TrailerType = component.m_TrailerType;
				value4.m_AttachPosition.xy = component.m_AttachOffset.xy;
				value4.m_AttachPosition.z = objectGeometryData2.m_Bounds.min.z + component.m_AttachOffset.z;
				if (component.m_FixedTrailer != null)
				{
					value4.m_FixedTrailer = m_PrefabSystem.GetEntity(component.m_FixedTrailer);
				}
				nativeArray6[l] = value4;
			}
			for (int m = 0; m < nativeArray7.Length; m++)
			{
				CarTrailerPrefab prefab3 = m_PrefabSystem.GetPrefab<CarTrailerPrefab>(nativeArray[m]);
				ObjectGeometryData objectGeometryData3 = nativeArray2[m];
				CarTrailerData value5 = nativeArray7[m];
				value5.m_TrailerType = prefab3.m_TrailerType;
				value5.m_MovementType = prefab3.m_MovementType;
				value5.m_AttachPosition.xy = prefab3.m_AttachOffset.xy;
				value5.m_AttachPosition.z = objectGeometryData3.m_Bounds.max.z - prefab3.m_AttachOffset.z;
				if (prefab3.m_FixedTractor != null)
				{
					value5.m_FixedTractor = m_PrefabSystem.GetEntity(prefab3.m_FixedTractor);
				}
				nativeArray7[m] = value5;
			}
		}
		JobHandle dependency = IJobParallelForExtensions.Schedule(new InitializeVehiclesJob
		{
			m_ObjectGeometryType = typeHandle2,
			m_CarTrailerType = typeHandle7,
			m_MultipleUnitTrainType = componentTypeHandle,
			m_SubmeshType = bufferTypeHandle,
			m_CarType = typeHandle4,
			m_TrainType = typeHandle3,
			m_SwayingType = typeHandle5,
			m_VehicleType = componentTypeHandle2,
			m_ProceduralBones = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ProceduralBone_RO_BufferLookup, ref base.CheckedStateRef),
			m_Chunks = chunks
		}, chunks.Length, 1, base.Dependency);
		base.Dependency = dependency;
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
	public VehicleInitializeSystem()
	{
	}
}
