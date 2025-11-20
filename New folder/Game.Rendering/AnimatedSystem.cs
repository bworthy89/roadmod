using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal.Animations;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.Serialization;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

[CompilerGenerated]
public class AnimatedSystem : GameSystemBase, IPreDeserialize
{
	public class Prepare : GameSystemBase
	{
		private AnimatedSystem m_AnimatedSystem;

		[Preserve]
		protected override void OnCreate()
		{
			base.OnCreate();
			m_AnimatedSystem = base.World.GetOrCreateSystemManaged<AnimatedSystem>();
		}

		[Preserve]
		protected override void OnUpdate()
		{
			m_AnimatedSystem.m_CurrentTime = (m_AnimatedSystem.m_CurrentTime + m_AnimatedSystem.m_RenderingSystem.lodTimerDelta) & 0xFFFF;
			JobHandle dependsOn = m_AnimatedSystem.m_AllocateDeps;
			if (m_AnimatedSystem.m_IsAllocating)
			{
				JobHandle allocateDeps = IJobExtensions.Schedule(new EndAllocationJob
				{
					m_BoneAllocator = m_AnimatedSystem.m_BoneAllocator,
					m_MetaBufferData = m_AnimatedSystem.m_MetaBufferData,
					m_FreeMetaIndices = m_AnimatedSystem.m_FreeMetaIndices,
					m_UpdatedMetaIndices = m_AnimatedSystem.m_UpdatedMetaIndices,
					m_BoneAllocationRemoves = m_AnimatedSystem.m_BoneAllocationRemoves,
					m_MetaBufferRemoves = m_AnimatedSystem.m_MetaBufferRemoves,
					m_CurrentTime = m_AnimatedSystem.m_CurrentTime
				}, dependsOn);
				m_AnimatedSystem.m_AllocateDeps = allocateDeps;
			}
			if (m_AnimatedSystem.m_TempAnimationQueue.IsCreated)
			{
				JobHandle jobHandle = IJobExtensions.Schedule(new AddAnimationInstancesJob
				{
					m_AnimationFrameData = m_AnimatedSystem.m_TempAnimationQueue,
					m_InstanceIndices = m_AnimatedSystem.m_InstanceIndices,
					m_BodyInstances = m_AnimatedSystem.m_BodyInstances,
					m_FaceInstances = m_AnimatedSystem.m_FaceInstances,
					m_CorrectiveIndices = m_AnimatedSystem.m_CorrectiveInstances,
					m_BodyTransitions = m_AnimatedSystem.m_BodyTransitions,
					m_BodyTransitions2 = m_AnimatedSystem.m_BodyTransitions2,
					m_FaceTransitions = m_AnimatedSystem.m_FaceTransitions
				}, dependsOn);
				m_AnimatedSystem.m_AllocateDeps = JobHandle.CombineDependencies(m_AnimatedSystem.m_AllocateDeps, jobHandle);
				m_AnimatedSystem.m_TempAnimationQueue.Dispose(jobHandle);
			}
			if (m_AnimatedSystem.m_TempPriorityQueue.IsCreated)
			{
				JobHandle jobHandle2 = IJobExtensions.Schedule(new UpdateAnimationPriorityJob
				{
					m_ClipPriorityData = m_AnimatedSystem.m_TempPriorityQueue,
					m_ClipPriorities = m_AnimatedSystem.m_ClipPriorities
				}, dependsOn);
				m_AnimatedSystem.m_AllocateDeps = JobHandle.CombineDependencies(m_AnimatedSystem.m_AllocateDeps, jobHandle2);
				m_AnimatedSystem.m_TempPriorityQueue.Dispose(jobHandle2);
			}
		}

		[Preserve]
		public Prepare()
		{
		}
	}

	[BurstCompile]
	private struct AddAnimationInstancesJob : IJob
	{
		public NativeQueue<AnimationFrameData> m_AnimationFrameData;

		public NativeList<RestPoseInstance> m_InstanceIndices;

		public NativeList<AnimatedInstance> m_BodyInstances;

		public NativeList<AnimatedInstance> m_FaceInstances;

		public NativeList<AnimatedInstance> m_CorrectiveIndices;

		public NativeList<AnimatedTransition> m_BodyTransitions;

		public NativeList<AnimatedTransition2> m_BodyTransitions2;

		public NativeList<AnimatedTransition> m_FaceTransitions;

		public void Execute()
		{
			m_InstanceIndices.Clear();
			m_CorrectiveIndices.Clear();
			m_BodyInstances.Clear();
			m_FaceInstances.Clear();
			m_BodyTransitions.Clear();
			m_BodyTransitions2.Clear();
			m_FaceTransitions.Clear();
			AnimationFrameData item;
			while (m_AnimationFrameData.TryDequeue(out item))
			{
				m_InstanceIndices.Add(new RestPoseInstance
				{
					m_MetaIndex = item.m_MetaIndex,
					m_RestPoseIndex = item.m_RestPoseIndex,
					m_ResetHistory = item.m_ResetHistory
				});
				if (item.m_CorrectiveIndex >= 0)
				{
					ref NativeList<AnimatedInstance> reference = ref m_CorrectiveIndices;
					AnimatedInstance value = new AnimatedInstance
					{
						m_MetaIndex = item.m_MetaIndex,
						m_CurrentIndex = item.m_CorrectiveIndex
					};
					reference.Add(in value);
				}
				if (item.m_BodyData.m_CurrentIndex >= 0)
				{
					if (item.m_BodyData.m_TransitionIndex.x >= 0)
					{
						if (item.m_BodyData.m_TransitionIndex.y >= 0)
						{
							ref NativeList<AnimatedTransition2> reference2 = ref m_BodyTransitions2;
							AnimatedTransition2 value2 = new AnimatedTransition2
							{
								m_MetaIndex = item.m_MetaIndex,
								m_CurrentIndex = item.m_BodyData.m_CurrentIndex,
								m_CurrentFrame = item.m_BodyData.m_CurrentFrame,
								m_TransitionIndex = item.m_BodyData.m_TransitionIndex,
								m_TransitionFrame = item.m_BodyData.m_TransitionFrame,
								m_TransitionWeight = item.m_BodyData.m_TransitionWeight
							};
							reference2.Add(in value2);
						}
						else
						{
							ref NativeList<AnimatedTransition> reference3 = ref m_BodyTransitions;
							AnimatedTransition value3 = new AnimatedTransition
							{
								m_MetaIndex = item.m_MetaIndex,
								m_CurrentIndex = item.m_BodyData.m_CurrentIndex,
								m_TransitionIndex = item.m_BodyData.m_TransitionIndex.x,
								m_CurrentFrame = item.m_BodyData.m_CurrentFrame,
								m_TransitionFrame = item.m_BodyData.m_TransitionFrame.x,
								m_TransitionWeight = item.m_BodyData.m_TransitionWeight.x
							};
							reference3.Add(in value3);
						}
					}
					else
					{
						ref NativeList<AnimatedInstance> reference4 = ref m_BodyInstances;
						AnimatedInstance value = new AnimatedInstance
						{
							m_MetaIndex = item.m_MetaIndex,
							m_CurrentIndex = item.m_BodyData.m_CurrentIndex,
							m_CurrentFrame = item.m_BodyData.m_CurrentFrame
						};
						reference4.Add(in value);
					}
				}
				if (item.m_FaceData.m_CurrentIndex >= 0)
				{
					if (item.m_FaceData.m_TransitionIndex >= 0)
					{
						ref NativeList<AnimatedTransition> reference5 = ref m_FaceTransitions;
						AnimatedTransition value3 = new AnimatedTransition
						{
							m_MetaIndex = item.m_MetaIndex,
							m_CurrentIndex = item.m_FaceData.m_CurrentIndex,
							m_TransitionIndex = item.m_FaceData.m_TransitionIndex,
							m_CurrentFrame = item.m_FaceData.m_CurrentFrame,
							m_TransitionFrame = item.m_FaceData.m_TransitionFrame,
							m_TransitionWeight = item.m_FaceData.m_TransitionWeight
						};
						reference5.Add(in value3);
					}
					else
					{
						ref NativeList<AnimatedInstance> reference6 = ref m_FaceInstances;
						AnimatedInstance value = new AnimatedInstance
						{
							m_MetaIndex = item.m_MetaIndex,
							m_CurrentIndex = item.m_FaceData.m_CurrentIndex,
							m_CurrentFrame = item.m_FaceData.m_CurrentFrame
						};
						reference6.Add(in value);
					}
				}
			}
		}
	}

	[BurstCompile]
	private struct UpdateAnimationPriorityJob : IJob
	{
		public NativeQueue<ClipPriorityData> m_ClipPriorityData;

		public NativeList<ClipPriorityData> m_ClipPriorities;

		public void Execute()
		{
			NativeHashMap<ClipIndex, int> nativeHashMap = new NativeHashMap<ClipIndex, int>(m_ClipPriorities.Length + 10, Allocator.Temp);
			for (int i = 0; i < m_ClipPriorities.Length; i++)
			{
				ref ClipPriorityData reference = ref m_ClipPriorities.ElementAt(i);
				reference.m_Priority = math.max(reference.m_Priority - 1, -1000000);
				nativeHashMap.Add(reference.m_ClipIndex, i);
			}
			ClipPriorityData item;
			while (m_ClipPriorityData.TryDequeue(out item))
			{
				if (nativeHashMap.TryGetValue(item.m_ClipIndex, out var item2))
				{
					ref ClipPriorityData reference2 = ref m_ClipPriorities.ElementAt(item2);
					reference2.m_Priority = math.max(reference2.m_Priority, item.m_Priority);
				}
				else
				{
					nativeHashMap.Add(item.m_ClipIndex, m_ClipPriorities.Length);
					m_ClipPriorities.Add(in item);
				}
			}
			nativeHashMap.Dispose();
			for (int j = 0; j < m_ClipPriorities.Length; j++)
			{
				ClipPriorityData clipPriorityData = m_ClipPriorities[j];
				if (clipPriorityData.m_Priority < 0 && !clipPriorityData.m_IsLoading && !clipPriorityData.m_IsLoaded)
				{
					m_ClipPriorities.RemoveAtSwapBack(j--);
				}
			}
			m_ClipPriorities.Sort();
		}
	}

	public struct AnimationData
	{
		private NativeQueue<AnimationFrameData>.ParallelWriter m_AnimationFrameData;

		private NativeQueue<ClipPriorityData>.ParallelWriter m_ClipPriorityData;

		[ReadOnly]
		private NativeHashMap<PropClipKey, ClipIndex> m_PropClipIndex;

		[ReadOnly]
		private NativeList<AnimationClipData> m_AnimationClipData;

		[ReadOnly]
		private NativeList<Animation.Element> m_AnimBufferCPU;

		[ReadOnly]
		private NativeList<int> m_IndexBufferCPU;

		public AnimationData(NativeQueue<AnimationFrameData> animationFrameData, NativeQueue<ClipPriorityData> clipPriorityData, NativeHashMap<PropClipKey, ClipIndex> propClipIndex, NativeList<AnimationClipData> animationClipData, NativeList<Animation.Element> animBufferCPU, NativeList<int> indexBufferCPU)
		{
			m_AnimationFrameData = animationFrameData.AsParallelWriter();
			m_ClipPriorityData = clipPriorityData.AsParallelWriter();
			m_PropClipIndex = propClipIndex;
			m_AnimationClipData = animationClipData;
			m_AnimBufferCPU = animBufferCPU;
			m_IndexBufferCPU = indexBufferCPU;
		}

		public void SetAnimationFrame(Entity clipContainer, int restPoseClipIndex, int correctiveClipIndex, DynamicBuffer<Game.Prefabs.AnimationClip> clips, in Animated animated, float2 transition, int priority, bool reset)
		{
			Game.Prefabs.AnimationClip clip = GetClip(clipContainer, clips, restPoseClipIndex, priority + 2);
			Game.Prefabs.AnimationClip clipData = GetClip(clipContainer, clips, animated.m_ClipIndexBody0, priority + 1);
			Game.Prefabs.AnimationClip clipData0I = GetClip(clipContainer, clips, animated.m_ClipIndexBody0I, priority + 1);
			Game.Prefabs.AnimationClip clipData2 = GetClip(clipContainer, clips, animated.m_ClipIndexBody1, priority + 1);
			Game.Prefabs.AnimationClip clipData1I = GetClip(clipContainer, clips, animated.m_ClipIndexBody1I, priority + 1);
			Game.Prefabs.AnimationClip clipData3 = GetClip(clipContainer, clips, animated.m_ClipIndexFace0, priority);
			Game.Prefabs.AnimationClip clipData4 = GetClip(clipContainer, clips, animated.m_ClipIndexFace1, priority);
			Game.Prefabs.AnimationClip clip2 = GetClip(clipContainer, clips, correctiveClipIndex, priority);
			AnimationFrameData value = new AnimationFrameData
			{
				m_MetaIndex = animated.m_MetaIndex,
				m_RestPoseIndex = clip.m_InfoIndex,
				m_ResetHistory = math.select(0, 1, reset),
				m_CorrectiveIndex = clip2.m_InfoIndex,
				m_BodyData = new AnimationLayerData2
				{
					m_CurrentIndex = -1,
					m_TransitionIndex = -1
				},
				m_FaceData = new AnimationLayerData
				{
					m_CurrentIndex = -1,
					m_TransitionIndex = -1
				}
			};
			if (clipData.m_InfoIndex < 0)
			{
				if (!reset)
				{
					return;
				}
			}
			else
			{
				SetLayerData(ref value.m_BodyData, in clipData, in clipData0I, in clipData2, in clipData1I, animated.m_Time.xy, animated.m_Interpolation, transition.x);
				if (clipData3.m_InfoIndex >= 0)
				{
					SetLayerData(ref value.m_FaceData, in clipData3, in clipData4, animated.m_Time.zw, transition.y);
				}
			}
			m_AnimationFrameData.Enqueue(value);
		}

		public void RequireAnimation(Entity clipContainer, DynamicBuffer<Game.Prefabs.AnimationClip> clips, in PlaybackLayer playbackLayer, int priority)
		{
			GetClip(clipContainer, clips, playbackLayer.m_ClipIndex, priority + 1, isCpuAnim: true);
		}

		public bool GetBoneTransform(DynamicBuffer<Game.Prefabs.AnimationClip> clips, int clipIndex, int boneIndex, float time, out float3 bonePosition, out quaternion boneRotation)
		{
			bonePosition = default(float3);
			boneRotation = quaternion.identity;
			if (clipIndex < 0)
			{
				return false;
			}
			Game.Prefabs.AnimationClip clipData = clips[clipIndex];
			if (clipData.m_InfoIndex < 0)
			{
				return false;
			}
			AnimationClipData animationClipData = m_AnimationClipData[clipData.m_InfoIndex];
			int num = m_IndexBufferCPU[(int)animationClipData.m_InverseBoneAllocation.Begin + boneIndex];
			if (num < 0)
			{
				return false;
			}
			SetLayerData(out var _, out var frame, in clipData, time);
			int num2 = math.clamp((int)frame, 0, animationClipData.m_FrameCount - 1);
			int num3 = math.select(num2 + 1, 0, num2 + 1 >= animationClipData.m_FrameCount);
			float t = math.saturate(frame - (float)num2);
			int index2 = num2 * animationClipData.m_BoneCount + num + (int)animationClipData.m_AnimAllocation.Begin;
			int index3 = num3 * animationClipData.m_BoneCount + num + (int)animationClipData.m_AnimAllocation.Begin;
			Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(m_AnimBufferCPU[index2], animationClipData.m_PositionMin, animationClipData.m_PositionRange);
			Animation.ElementRaw elementRaw2 = AnimationEncoding.DecodeElement(m_AnimBufferCPU[index3], animationClipData.m_PositionMin, animationClipData.m_PositionRange);
			bonePosition = math.lerp(elementRaw.position, elementRaw2.position, t);
			boneRotation = math.slerp(new quaternion(elementRaw.rotation), new quaternion(elementRaw2.rotation), t);
			return true;
		}

		private Game.Prefabs.AnimationClip GetClip(Entity clipContainer, DynamicBuffer<Game.Prefabs.AnimationClip> clips, int index, int priority, bool isCpuAnim = false)
		{
			if (index != -1)
			{
				Game.Prefabs.AnimationClip result = clips[index];
				if (priority >= 0)
				{
					if (result.m_PropID.isValid && result.m_TargetValue == float.MinValue && m_PropClipIndex.TryGetValue(new PropClipKey(result.m_PropID, result.m_Activity, result.m_Type, result.m_Gender), out var item))
					{
						m_ClipPriorityData.Enqueue(new ClipPriorityData
						{
							m_ClipIndex = item,
							m_Priority = priority,
							m_IsCPUAnim = isCpuAnim
						});
					}
					m_ClipPriorityData.Enqueue(new ClipPriorityData
					{
						m_ClipIndex = new ClipIndex(clipContainer, index),
						m_Priority = priority,
						m_IsCPUAnim = isCpuAnim
					});
				}
				return result;
			}
			return new Game.Prefabs.AnimationClip
			{
				m_InfoIndex = -1
			};
		}

		private void SetLayerData(ref AnimationLayerData2 layerData, in Game.Prefabs.AnimationClip clipData0, in Game.Prefabs.AnimationClip clipData0I, in Game.Prefabs.AnimationClip clipData1, in Game.Prefabs.AnimationClip clipData1I, float2 time, float2 interpolation, float transition)
		{
			SetLayerData(out layerData.m_CurrentIndex, out layerData.m_CurrentFrame, in clipData0, time.x);
			if (clipData1.m_InfoIndex >= 0)
			{
				SetLayerData(out layerData.m_TransitionIndex.x, out layerData.m_TransitionFrame.x, in clipData1, time.y);
				layerData.m_TransitionWeight.x = transition;
				if (clipData0I.m_InfoIndex >= 0)
				{
					SetLayerData(out layerData.m_TransitionIndex.y, out layerData.m_TransitionFrame.y, in clipData0I, time.x);
					if (clipData1I.m_InfoIndex == clipData0I.m_InfoIndex)
					{
						layerData.m_TransitionWeight.y = math.csum(interpolation) * 0.5f;
					}
					else
					{
						layerData.m_TransitionWeight.y = interpolation.x * (1f - transition);
					}
				}
				else if (clipData1I.m_InfoIndex >= 0)
				{
					SetLayerData(out layerData.m_TransitionIndex.y, out layerData.m_TransitionFrame.y, in clipData1I, time.y);
					layerData.m_TransitionWeight.y = interpolation.y * transition;
				}
			}
			else if (clipData0I.m_InfoIndex >= 0)
			{
				SetLayerData(out layerData.m_TransitionIndex.x, out layerData.m_TransitionFrame.x, in clipData0I, time.x);
				layerData.m_TransitionWeight.x = interpolation.x;
			}
		}

		private void SetLayerData(ref AnimationLayerData layerData, in Game.Prefabs.AnimationClip clipData0, in Game.Prefabs.AnimationClip clipData1, float2 time, float transition)
		{
			SetLayerData(out layerData.m_CurrentIndex, out layerData.m_CurrentFrame, in clipData0, time.x);
			if (clipData1.m_InfoIndex >= 0)
			{
				SetLayerData(out layerData.m_TransitionIndex, out layerData.m_TransitionFrame, in clipData1, time.y);
				layerData.m_TransitionWeight = transition;
			}
		}

		private void SetLayerData(out int index, out float frame, in Game.Prefabs.AnimationClip clipData, float time)
		{
			if (clipData.m_Playback != AnimationPlayback.Once && clipData.m_Playback != AnimationPlayback.OptionalOnce)
			{
				frame = math.fmod(time, clipData.m_AnimationLength);
				frame += math.select(0f, clipData.m_AnimationLength, frame < 0f);
			}
			else
			{
				frame = math.clamp(time, 0f, clipData.m_AnimationLength);
			}
			index = clipData.m_InfoIndex;
			frame *= clipData.m_FrameRate;
		}
	}

	[BurstCompile]
	private struct EndAllocationJob : IJob
	{
		public NativeHeapAllocator m_BoneAllocator;

		public NativeList<MetaBufferData> m_MetaBufferData;

		public NativeList<int> m_UpdatedMetaIndices;

		public NativeList<int> m_FreeMetaIndices;

		public NativeQueue<AllocationRemove> m_BoneAllocationRemoves;

		public NativeQueue<IndexRemove> m_MetaBufferRemoves;

		public int m_CurrentTime;

		public void Execute()
		{
			if (m_UpdatedMetaIndices.Length >= 2)
			{
				m_UpdatedMetaIndices.Sort();
			}
			while (!m_BoneAllocationRemoves.IsEmpty())
			{
				AllocationRemove allocationRemove = m_BoneAllocationRemoves.Peek();
				if (CheckTimeOffset(allocationRemove.m_RemoveTime))
				{
					break;
				}
				m_BoneAllocationRemoves.Dequeue();
				m_BoneAllocator.Release(allocationRemove.m_Allocation);
			}
			bool flag = false;
			while (!m_MetaBufferRemoves.IsEmpty())
			{
				IndexRemove indexRemove = m_MetaBufferRemoves.Peek();
				if (CheckTimeOffset(indexRemove.m_RemoveTime))
				{
					break;
				}
				m_MetaBufferRemoves.Dequeue();
				if (indexRemove.m_Index == m_MetaBufferData.Length - 1)
				{
					m_MetaBufferData.RemoveAt(indexRemove.m_Index);
					continue;
				}
				m_FreeMetaIndices.Add(in indexRemove.m_Index);
				flag = true;
			}
			if (flag)
			{
				if (m_FreeMetaIndices.Length >= 2)
				{
					m_FreeMetaIndices.Sort(default(ReverseIntComparer));
				}
				int num = m_MetaBufferData.Length - 1;
				for (int i = 0; i < m_FreeMetaIndices.Length && m_FreeMetaIndices[i] == num; i++)
				{
					num--;
				}
				int count = m_MetaBufferData.Length - 1 - num;
				m_MetaBufferData.RemoveRange(num + 1, count);
				m_FreeMetaIndices.RemoveRange(0, count);
			}
		}

		private bool CheckTimeOffset(int removeTime)
		{
			int num = m_CurrentTime - removeTime;
			num += math.select(0, 65536, num < 0);
			return num < 255;
		}
	}

	public struct IndexRemove
	{
		public int m_Index;

		public int m_RemoveTime;
	}

	public struct AllocationRemove
	{
		public NativeHeapBlock m_Allocation;

		public int m_RemoveTime;
	}

	public struct AllocationData
	{
		private NativeHeapAllocator m_BoneAllocator;

		private NativeList<MetaBufferData> m_MetaBufferData;

		private NativeList<int> m_FreeMetaIndices;

		private NativeList<int> m_UpdatedMetaIndices;

		private NativeQueue<AllocationRemove> m_BoneAllocationRemoves;

		private NativeQueue<IndexRemove> m_MetaBufferRemoves;

		private int m_CurrentTime;

		public AllocationData(NativeHeapAllocator boneAllocator, NativeList<MetaBufferData> metaBufferData, NativeList<int> freeMetaIndices, NativeList<int> updatedMetaIndices, NativeQueue<AllocationRemove> boneAllocationRemoves, NativeQueue<IndexRemove> metaBufferRemoves, int currentTime)
		{
			m_BoneAllocator = boneAllocator;
			m_MetaBufferData = metaBufferData;
			m_FreeMetaIndices = freeMetaIndices;
			m_UpdatedMetaIndices = updatedMetaIndices;
			m_BoneAllocationRemoves = boneAllocationRemoves;
			m_MetaBufferRemoves = metaBufferRemoves;
			m_CurrentTime = currentTime;
		}

		public unsafe NativeHeapBlock AllocateBones(int boneCount)
		{
			NativeHeapBlock result = m_BoneAllocator.Allocate((uint)boneCount);
			if (result.Empty)
			{
				m_BoneAllocator.Resize(m_BoneAllocator.Size + 2097152u / (uint)sizeof(BoneElement));
				result = m_BoneAllocator.Allocate((uint)boneCount);
			}
			return result;
		}

		public void ReleaseBones(NativeHeapBlock allocation)
		{
			m_BoneAllocationRemoves.Enqueue(new AllocationRemove
			{
				m_Allocation = allocation,
				m_RemoveTime = m_CurrentTime
			});
		}

		public int AddMetaBufferData(MetaBufferData metaBufferData)
		{
			int value;
			if (m_FreeMetaIndices.IsEmpty)
			{
				value = m_MetaBufferData.Length;
				m_MetaBufferData.Add(in metaBufferData);
			}
			else
			{
				value = m_FreeMetaIndices[m_FreeMetaIndices.Length - 1];
				m_FreeMetaIndices.RemoveAt(m_FreeMetaIndices.Length - 1);
				m_MetaBufferData[value] = metaBufferData;
			}
			m_UpdatedMetaIndices.Add(in value);
			return value;
		}

		public void RemoveMetaBufferData(int metaIndex)
		{
			m_MetaBufferRemoves.Enqueue(new IndexRemove
			{
				m_Index = metaIndex,
				m_RemoveTime = m_CurrentTime
			});
		}
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct ReverseIntComparer : IComparer<int>
	{
		public int Compare(int x, int y)
		{
			return y - x;
		}
	}

	public struct AnimationLayerData
	{
		public int m_CurrentIndex;

		public float m_CurrentFrame;

		public int m_TransitionIndex;

		public float m_TransitionFrame;

		public float m_TransitionWeight;
	}

	public struct AnimationLayerData2
	{
		public int m_CurrentIndex;

		public float m_CurrentFrame;

		public int2 m_TransitionIndex;

		public float2 m_TransitionFrame;

		public float2 m_TransitionWeight;
	}

	public struct AnimationFrameData
	{
		public int m_MetaIndex;

		public int m_RestPoseIndex;

		public int m_ResetHistory;

		public int m_CorrectiveIndex;

		public AnimationLayerData2 m_BodyData;

		public AnimationLayerData m_FaceData;
	}

	public struct ClipPriorityData : IComparable<ClipPriorityData>
	{
		public ClipIndex m_ClipIndex;

		public int m_Priority;

		public bool m_IsLoading;

		public bool m_IsLoaded;

		public bool m_IsCPUAnim;

		public int CompareTo(ClipPriorityData other)
		{
			return math.select(other.m_Priority - m_Priority, math.select(1, -1, m_IsLoading), m_IsLoading != other.m_IsLoading);
		}
	}

	public struct ClipIndex : IEquatable<ClipIndex>
	{
		public Entity m_ClipContainer;

		public int m_Index;

		public ClipIndex(Entity clipContainer, int clipIndex)
		{
			m_ClipContainer = clipContainer;
			m_Index = clipIndex;
		}

		public bool Equals(ClipIndex other)
		{
			if (m_ClipContainer.Equals(other.m_ClipContainer))
			{
				return m_Index == other.m_Index;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_ClipContainer.GetHashCode() * 5039 + m_Index;
		}
	}

	public struct PropClipKey : IEquatable<PropClipKey>
	{
		public AnimatedPropID m_PropID;

		public ActivityType m_ActivityType;

		public Game.Prefabs.AnimationType m_AnimationType;

		public GenderMask m_Gender;

		public PropClipKey(AnimatedPropID propID, ActivityType activityType, Game.Prefabs.AnimationType animationType, GenderMask gender)
		{
			m_PropID = propID;
			m_ActivityType = activityType;
			m_AnimationType = animationType;
			m_Gender = gender;
		}

		public bool Equals(PropClipKey other)
		{
			if (m_PropID == other.m_PropID && m_ActivityType == other.m_ActivityType && m_AnimationType == other.m_AnimationType)
			{
				return m_Gender == other.m_Gender;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (int)((m_PropID.GetHashCode() << 10) + ((int)m_ActivityType << 5) + m_AnimationType) ^ (int)m_Gender;
		}
	}

	public struct AnimationClipData
	{
		public NativeHeapBlock m_AnimAllocation;

		public NativeHeapBlock m_HierarchyAllocation;

		public NativeHeapBlock m_ShapeAllocation;

		public NativeHeapBlock m_BoneAllocation;

		public NativeHeapBlock m_InverseBoneAllocation;

		public float3 m_PositionMin;

		public float3 m_PositionRange;

		public int m_BoneCount;

		public int m_FrameCount;
	}

	public const uint BONEBUFFER_MEMORY_DEFAULT = 8388608u;

	public const uint BONEBUFFER_MEMORY_INCREMENT = 2097152u;

	public const uint ANIMBUFFER_MEMORY_DEFAULT = 33554432u;

	public const uint ANIMBUFFER_MEMORY_INCREMENT = 8388608u;

	public const uint ANIMBUFFER_CPU_MEMORY_DEFAULT = 1048576u;

	public const uint ANIMBUFFER_CPU_MEMORY_INCREMENT = 262144u;

	public const uint METABUFFER_MEMORY_DEFAULT = 1048576u;

	public const uint METABUFFER_MEMORY_INCREMENT = 262144u;

	public const uint INDEXBUFFER_MEMORY_DEFAULT = 65536u;

	public const uint INDEXBUFFER_MEMORY_INCREMENT = 16384u;

	public const uint INDEXBUFFER_CPU_MEMORY_DEFAULT = 4096u;

	public const uint INDEXBUFFER_CPU_MEMORY_INCREMENT = 1024u;

	public const uint MAX_ASYNC_LOADING_COUNT = 10u;

	private const string ANIMATION_COMPUTE_SHADER_RESOURCE = "Didimo/AnimationBlendCompute";

	private const string SHADER_BLEND_ANIMATION_LAYER0_KERNEL_NAME = "BlendAnimationLayer0";

	private const string SHADER_BLEND_ANIMATION_LAYER1_KERNEL_NAME = "BlendAnimationLayer1";

	private const string SHADER_BLEND_ANIMATION_LAYER2_KERNEL_NAME = "BlendAnimationLayer2";

	private const string SHADER_BLEND_TRANSITION_LAYER0_KERNEL_NAME = "BlendTransitionLayer0";

	private const string SHADER_BLEND_TRANSITION2_LAYER0_KERNEL_NAME = "BlendTransition2Layer0";

	private const string SHADER_BLEND_TRANSITION_LAYER1_KERNEL_NAME = "BlendTransitionLayer1";

	private const string SHADER_BLEND_REST_POSE_KERNEL_NAME = "BlendRestPose";

	private const string SHADER_CONVERT_COORDINATES_KERNEL_NAME = "ConvertLocalCoordinates";

	private const string SHADER_CONVERT_COORDINATES_WITH_HISTORY_KERNEL_NAME = "ConvertLocalCoordinatesWithHistory";

	private static ILog log;

	private RenderingSystem m_RenderingSystem;

	private PrefabSystem m_PrefabSystem;

	private NativeHeapAllocator m_BoneAllocator;

	private NativeHeapAllocator m_AnimAllocator;

	private NativeHeapAllocator m_AnimAllocatorCPU;

	private NativeHeapAllocator m_IndexAllocator;

	private NativeHeapAllocator m_IndexAllocatorCPU;

	private NativeList<MetaBufferData> m_MetaBufferData;

	private NativeList<int> m_FreeMetaIndices;

	private NativeList<int> m_UpdatedMetaIndices;

	private NativeList<RestPoseInstance> m_InstanceIndices;

	private NativeList<AnimatedInstance> m_BodyInstances;

	private NativeList<AnimatedInstance> m_FaceInstances;

	private NativeList<AnimatedInstance> m_CorrectiveInstances;

	private NativeList<AnimatedTransition> m_BodyTransitions;

	private NativeList<AnimatedTransition2> m_BodyTransitions2;

	private NativeList<AnimatedTransition> m_FaceTransitions;

	private NativeList<ClipPriorityData> m_ClipPriorities;

	private NativeList<AnimationClipData> m_AnimationClipData;

	private NativeList<Animation.Element> m_AnimBufferCPU;

	private NativeList<int> m_IndexBufferCPU;

	private NativeList<int> m_FreeAnimIndices;

	private NativeQueue<AllocationRemove> m_BoneAllocationRemoves;

	private NativeQueue<IndexRemove> m_MetaBufferRemoves;

	private NativeHashMap<PropClipKey, ClipIndex> m_PropClipIndex;

	private ComputeBuffer m_BoneBuffer;

	private ComputeBuffer m_BoneHistoryBuffer;

	private ComputeBuffer m_LocalTRSBlendPoseBuffer;

	private ComputeBuffer m_LocalTRSBoneBuffer;

	private ComputeBuffer m_AnimInfoBuffer;

	private ComputeBuffer m_AnimBuffer;

	private ComputeBuffer m_MetaBuffer;

	private ComputeBuffer m_IndexBuffer;

	private ComputeBuffer m_InstanceBuffer;

	private ComputeBuffer m_BodyInstanceBuffer;

	private ComputeBuffer m_FaceInstanceBuffer;

	private ComputeBuffer m_CorrectiveInstanceBuffer;

	private ComputeBuffer m_BodyTransitionBuffer;

	private ComputeBuffer m_BodyTransition2Buffer;

	private ComputeBuffer m_FaceTransitionBuffer;

	private int m_AnimationCount;

	private int m_AnimationCountCPU;

	private int m_MaxBoneCount;

	private int m_MaxActiveBoneCount;

	private int m_CurrentTime;

	private bool m_IsAllocating;

	private Dictionary<string, int> m_PropIDs;

	private ComputeShader m_AnimationComputeShader;

	private NativeQueue<AnimationFrameData> m_TempAnimationQueue;

	private NativeQueue<ClipPriorityData> m_TempPriorityQueue;

	private JobHandle m_AllocateDeps;

	private int m_BlendAnimationLayer0KernelIx;

	private int m_BlendAnimationLayer1KernelIx;

	private int m_BlendAnimationLayer2KernelIx;

	private int m_BlendTransitionLayer0KernelIx;

	private int m_BlendTransition2Layer0KernelIx;

	private int m_BlendTransitionLayer1KernelIx;

	private int m_BlendRestPoseKernelIx;

	private int m_ConvertLocalCoordinatesKernelIx;

	private int m_ConvertLocalCoordinatesWithHistoryKernelIx;

	private int m_IndexBufferID;

	private int m_MetadataBufferID;

	private int m_MetaIndexBufferID;

	private int m_AnimatedInstanceBufferID;

	private int m_AnimatedTransitionBufferID;

	private int m_AnimatedTransition2BufferID;

	private int m_AnimationInfoBufferID;

	private int m_AnimationBoneBufferID;

	private int m_InstanceCountID;

	private int m_BodyInstanceCountID;

	private int m_BodyTransitionCountID;

	private int m_BodyTransition2CountID;

	private int m_FaceInstanceCountID;

	private int m_FaceTransitionCountID;

	private int m_CorrectiveInstanceCountID;

	private int m_LocalTRSBlendPoseBufferID;

	private int m_LocalTRSBoneBufferID;

	private int m_BoneBufferID;

	private int m_BoneHistoryBufferID;

	[Preserve]
	protected unsafe override void OnCreate()
	{
		log = LogManager.GetLogger("Rendering");
		base.OnCreate();
		m_RenderingSystem = base.World.GetOrCreateSystemManaged<RenderingSystem>();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_BoneAllocator = new NativeHeapAllocator(8388608u / (uint)sizeof(BoneElement), 1u, Allocator.Persistent);
		m_AnimAllocator = new NativeHeapAllocator(33554432u / (uint)sizeof(Animation.Element), 1u, Allocator.Persistent);
		m_AnimAllocatorCPU = new NativeHeapAllocator(1048576u / (uint)sizeof(Animation.Element), 1u, Allocator.Persistent);
		m_IndexAllocator = new NativeHeapAllocator(16384u, 1u, Allocator.Persistent);
		m_IndexAllocatorCPU = new NativeHeapAllocator(1024u, 1u, Allocator.Persistent);
		m_MetaBufferData = new NativeList<MetaBufferData>(1000, Allocator.Persistent);
		m_FreeMetaIndices = new NativeList<int>(10, Allocator.Persistent);
		m_UpdatedMetaIndices = new NativeList<int>(10, Allocator.Persistent);
		m_InstanceIndices = new NativeList<RestPoseInstance>(1000, Allocator.Persistent);
		m_BodyInstances = new NativeList<AnimatedInstance>(1000, Allocator.Persistent);
		m_FaceInstances = new NativeList<AnimatedInstance>(1000, Allocator.Persistent);
		m_CorrectiveInstances = new NativeList<AnimatedInstance>(100, Allocator.Persistent);
		m_BodyTransitions = new NativeList<AnimatedTransition>(100, Allocator.Persistent);
		m_BodyTransitions2 = new NativeList<AnimatedTransition2>(100, Allocator.Persistent);
		m_FaceTransitions = new NativeList<AnimatedTransition>(100, Allocator.Persistent);
		m_ClipPriorities = new NativeList<ClipPriorityData>(10, Allocator.Persistent);
		m_AnimationClipData = new NativeList<AnimationClipData>(10, Allocator.Persistent);
		m_AnimBufferCPU = new NativeList<Animation.Element>(Allocator.Persistent);
		m_IndexBufferCPU = new NativeList<int>(Allocator.Persistent);
		m_FreeAnimIndices = new NativeList<int>(10, Allocator.Persistent);
		m_BoneAllocationRemoves = new NativeQueue<AllocationRemove>(Allocator.Persistent);
		m_MetaBufferRemoves = new NativeQueue<IndexRemove>(Allocator.Persistent);
		m_PropClipIndex = new NativeHashMap<PropClipKey, ClipIndex>(100, Allocator.Persistent);
		m_PropIDs = new Dictionary<string, int>();
		m_AnimationComputeShader = UnityEngine.Object.Instantiate(Resources.Load<ComputeShader>("Didimo/AnimationBlendCompute"));
		m_BlendAnimationLayer0KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer0");
		m_BlendAnimationLayer1KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer1");
		m_BlendAnimationLayer2KernelIx = m_AnimationComputeShader.FindKernel("BlendAnimationLayer2");
		m_BlendTransitionLayer0KernelIx = m_AnimationComputeShader.FindKernel("BlendTransitionLayer0");
		m_BlendTransition2Layer0KernelIx = m_AnimationComputeShader.FindKernel("BlendTransition2Layer0");
		m_BlendTransitionLayer1KernelIx = m_AnimationComputeShader.FindKernel("BlendTransitionLayer1");
		m_BlendRestPoseKernelIx = m_AnimationComputeShader.FindKernel("BlendRestPose");
		m_ConvertLocalCoordinatesKernelIx = m_AnimationComputeShader.FindKernel("ConvertLocalCoordinates");
		m_ConvertLocalCoordinatesWithHistoryKernelIx = m_AnimationComputeShader.FindKernel("ConvertLocalCoordinatesWithHistory");
		m_IndexBufferID = Shader.PropertyToID("IndexDataBuffer");
		m_MetadataBufferID = Shader.PropertyToID("MetaDataBuffer");
		m_MetaIndexBufferID = Shader.PropertyToID("MetaIndexBuffer");
		m_AnimatedInstanceBufferID = Shader.PropertyToID("AnimatedInstanceBuffer");
		m_AnimatedTransitionBufferID = Shader.PropertyToID("AnimatedTransitionBuffer");
		m_AnimatedTransition2BufferID = Shader.PropertyToID("AnimatedTransition2Buffer");
		m_AnimationInfoBufferID = Shader.PropertyToID("AnimationInfoBuffer");
		m_AnimationBoneBufferID = Shader.PropertyToID("AnimationBoneBuffer");
		m_InstanceCountID = Shader.PropertyToID("instanceCount");
		m_BodyInstanceCountID = Shader.PropertyToID("bodyInstanceCount");
		m_BodyTransitionCountID = Shader.PropertyToID("bodyTransitionCount");
		m_BodyTransition2CountID = Shader.PropertyToID("bodyTransition2Count");
		m_FaceInstanceCountID = Shader.PropertyToID("faceInstanceCount");
		m_FaceTransitionCountID = Shader.PropertyToID("faceTransitionCount");
		m_CorrectiveInstanceCountID = Shader.PropertyToID("correctiveInstanceCount");
		m_LocalTRSBlendPoseBufferID = Shader.PropertyToID("LocalTRSBlendPoseBuffer");
		m_LocalTRSBoneBufferID = Shader.PropertyToID("LocalTRSBoneBuffer");
		m_BoneBufferID = Shader.PropertyToID("BoneBuffer");
		m_BoneHistoryBufferID = Shader.PropertyToID("BoneHistoryBuffer");
		m_BoneAllocator.Allocate(1u);
		m_MetaBufferData.Add(default(MetaBufferData));
	}

	[Preserve]
	protected override void OnDestroy()
	{
		if (m_IsAllocating)
		{
			m_AllocateDeps.Complete();
			m_IsAllocating = false;
		}
		m_BoneAllocator.Dispose();
		m_AnimAllocator.Dispose();
		m_AnimAllocatorCPU.Dispose();
		m_IndexAllocator.Dispose();
		m_IndexAllocatorCPU.Dispose();
		m_MetaBufferData.Dispose();
		m_FreeMetaIndices.Dispose();
		m_UpdatedMetaIndices.Dispose();
		m_InstanceIndices.Dispose();
		m_BodyInstances.Dispose();
		m_FaceInstances.Dispose();
		m_CorrectiveInstances.Dispose();
		m_BodyTransitions.Dispose();
		m_BodyTransitions2.Dispose();
		m_FaceTransitions.Dispose();
		m_ClipPriorities.Dispose();
		m_AnimationClipData.Dispose();
		m_AnimBufferCPU.Dispose();
		m_IndexBufferCPU.Dispose();
		m_FreeAnimIndices.Dispose();
		m_BoneAllocationRemoves.Dispose();
		m_MetaBufferRemoves.Dispose();
		m_PropClipIndex.Dispose();
		if (m_BoneBuffer != null)
		{
			m_BoneBuffer.Release();
		}
		if (m_BoneHistoryBuffer != null)
		{
			m_BoneHistoryBuffer.Release();
		}
		if (m_LocalTRSBlendPoseBuffer != null)
		{
			m_LocalTRSBlendPoseBuffer.Release();
		}
		if (m_LocalTRSBoneBuffer != null)
		{
			m_LocalTRSBoneBuffer.Release();
		}
		if (m_AnimInfoBuffer != null)
		{
			m_AnimInfoBuffer.Release();
		}
		if (m_AnimBuffer != null)
		{
			m_AnimBuffer.Release();
		}
		if (m_MetaBuffer != null)
		{
			m_MetaBuffer.Release();
		}
		if (m_IndexBuffer != null)
		{
			m_IndexBuffer.Release();
		}
		if (m_InstanceBuffer != null)
		{
			m_InstanceBuffer.Release();
		}
		if (m_BodyInstanceBuffer != null)
		{
			m_BodyInstanceBuffer.Release();
		}
		if (m_FaceInstanceBuffer != null)
		{
			m_FaceInstanceBuffer.Release();
		}
		if (m_CorrectiveInstanceBuffer != null)
		{
			m_CorrectiveInstanceBuffer.Release();
		}
		if (m_BodyTransitionBuffer != null)
		{
			m_BodyTransitionBuffer.Release();
		}
		if (m_BodyTransition2Buffer != null)
		{
			m_BodyTransition2Buffer.Release();
		}
		if (m_FaceTransitionBuffer != null)
		{
			m_FaceTransitionBuffer.Release();
		}
		UnityEngine.Object.DestroyImmediate(m_AnimationComputeShader);
		base.OnDestroy();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_IsAllocating)
		{
			m_AllocateDeps.Complete();
			m_IsAllocating = false;
			ResizeBoneBuffer();
			ResizeMetaBuffer();
			UpdateAnimations();
			UpdateMetaData();
			UpdateInstances();
		}
		PlayAnimations();
	}

	private void PlayAnimations()
	{
		if (m_InstanceIndices.Length != 0 && m_MaxBoneCount != 0)
		{
			ResizeBoneHistoryBuffer();
			m_AnimationComputeShader.SetInt(m_InstanceCountID, m_InstanceIndices.Length);
			m_AnimationComputeShader.SetInt(m_BodyInstanceCountID, m_BodyInstances.Length);
			m_AnimationComputeShader.SetInt(m_BodyTransitionCountID, m_BodyTransitions.Length);
			m_AnimationComputeShader.SetInt(m_BodyTransition2CountID, m_BodyTransitions2.Length);
			m_AnimationComputeShader.SetInt(m_FaceInstanceCountID, m_FaceInstances.Length);
			m_AnimationComputeShader.SetInt(m_FaceTransitionCountID, m_FaceTransitions.Length);
			m_AnimationComputeShader.SetInt(m_CorrectiveInstanceCountID, m_CorrectiveInstances.Length);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_MetadataBufferID, m_MetaBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_MetaIndexBufferID, m_InstanceBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
			m_AnimationComputeShader.SetBuffer(m_BlendRestPoseKernelIx, m_IndexBufferID, m_IndexBuffer);
			if (m_BodyInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_AnimatedInstanceBufferID, m_BodyInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_BodyTransitions.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_AnimatedTransitionBufferID, m_BodyTransitionBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_BodyTransitions2.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_AnimatedTransition2BufferID, m_BodyTransition2Buffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransition2Layer0KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_FaceInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_AnimatedInstanceBufferID, m_FaceInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer1KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_FaceTransitions.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_AnimatedTransitionBufferID, m_FaceTransitionBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendTransitionLayer1KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			if (m_CorrectiveInstances.Length != 0)
			{
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimationInfoBufferID, m_AnimInfoBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimationBoneBufferID, m_AnimBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_MetadataBufferID, m_MetaBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_AnimatedInstanceBufferID, m_CorrectiveInstanceBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
				m_AnimationComputeShader.SetBuffer(m_BlendAnimationLayer2KernelIx, m_IndexBufferID, m_IndexBuffer);
			}
			int kernelIndex = (m_RenderingSystem.motionVectors ? m_ConvertLocalCoordinatesWithHistoryKernelIx : m_ConvertLocalCoordinatesKernelIx);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_MetadataBufferID, m_MetaBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_MetaIndexBufferID, m_InstanceBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_LocalTRSBoneBufferID, m_LocalTRSBoneBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_LocalTRSBlendPoseBufferID, m_LocalTRSBlendPoseBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_BoneBufferID, m_BoneBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_IndexBufferID, m_IndexBuffer);
			m_AnimationComputeShader.SetBuffer(kernelIndex, m_AnimationInfoBufferID, m_AnimInfoBuffer);
			if (m_RenderingSystem.motionVectors)
			{
				m_AnimationComputeShader.SetBuffer(kernelIndex, m_BoneHistoryBufferID, m_BoneHistoryBuffer);
			}
			else
			{
				m_AnimationComputeShader.SetBuffer(kernelIndex, m_BoneHistoryBufferID, m_BoneBuffer);
			}
			m_AnimationComputeShader.GetKernelThreadGroupSizes(m_BlendRestPoseKernelIx, out var x, out var y, out var _);
			int threadGroupsX = (m_InstanceIndices.Length + (int)x - 1) / (int)x;
			int threadGroupsY = (m_MaxBoneCount + (int)y - 1) / (int)y;
			int threadGroupsY2 = (m_MaxActiveBoneCount + (int)y - 1) / (int)y;
			m_AnimationComputeShader.Dispatch(m_BlendRestPoseKernelIx, threadGroupsX, threadGroupsY, 1);
			if (m_BodyInstances.Length != 0)
			{
				int threadGroupsX2 = (m_BodyInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer0KernelIx, threadGroupsX2, threadGroupsY2, 1);
			}
			if (m_BodyTransitions.Length != 0)
			{
				int threadGroupsX3 = (m_BodyTransitions.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransitionLayer0KernelIx, threadGroupsX3, threadGroupsY, 1);
			}
			if (m_BodyTransitions2.Length != 0)
			{
				int threadGroupsX4 = (m_BodyTransitions2.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransition2Layer0KernelIx, threadGroupsX4, threadGroupsY, 1);
			}
			if (m_FaceInstances.Length != 0)
			{
				int threadGroupsX5 = (m_FaceInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer1KernelIx, threadGroupsX5, threadGroupsY2, 1);
			}
			if (m_FaceTransitions.Length != 0)
			{
				int threadGroupsX6 = (m_FaceTransitions.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendTransitionLayer1KernelIx, threadGroupsX6, threadGroupsY, 1);
			}
			if (m_CorrectiveInstances.Length != 0)
			{
				int threadGroupsX7 = (m_CorrectiveInstances.Length + (int)x - 1) / (int)x;
				m_AnimationComputeShader.Dispatch(m_BlendAnimationLayer2KernelIx, threadGroupsX7, threadGroupsY2, 1);
			}
			m_AnimationComputeShader.Dispatch(kernelIndex, threadGroupsX, threadGroupsY, 1);
		}
	}

	private void UpdateInstances()
	{
		UpdateInstanceBuffer(ref m_InstanceBuffer, m_InstanceIndices, "InstanceBuffer");
		UpdateInstanceBuffer(ref m_BodyInstanceBuffer, m_BodyInstances, "BodyInstanceBuffer");
		UpdateInstanceBuffer(ref m_FaceInstanceBuffer, m_FaceInstances, "FaceInstanceBuffer");
		UpdateInstanceBuffer(ref m_CorrectiveInstanceBuffer, m_CorrectiveInstances, "CorrectiveInstanceBuffer");
		UpdateInstanceBuffer(ref m_BodyTransitionBuffer, m_BodyTransitions, "BodyTransitionBuffer");
		UpdateInstanceBuffer(ref m_BodyTransition2Buffer, m_BodyTransitions2, "BodyTransitionBuffer2");
		UpdateInstanceBuffer(ref m_FaceTransitionBuffer, m_FaceTransitions, "FaceTransitionBuffer");
	}

	private unsafe void UpdateInstanceBuffer<T>(ref ComputeBuffer buffer, NativeList<T> data, string name) where T : unmanaged
	{
		if (buffer == null || buffer.count != data.Capacity)
		{
			if (buffer != null)
			{
				buffer.Release();
			}
			buffer = new ComputeBuffer(data.Capacity, sizeof(T), ComputeBufferType.Structured);
			buffer.name = name;
		}
		if (data.Length != 0)
		{
			buffer.SetData(data.AsArray(), 0, 0, data.Length);
		}
	}

	private void UpdateAnimations()
	{
		int num = 0;
		for (int i = 0; i < m_ClipPriorities.Length; i++)
		{
			ref ClipPriorityData reference = ref m_ClipPriorities.ElementAt(i);
			if (reference.m_Priority < 0 && !reference.m_IsLoading)
			{
				break;
			}
			if (reference.m_IsLoaded)
			{
				continue;
			}
			PrefabBase prefab = m_PrefabSystem.GetPrefab<PrefabBase>(reference.m_ClipIndex.m_ClipContainer);
			AnimationAsset animation;
			if (prefab is CharacterStyle characterStyle)
			{
				animation = characterStyle.GetAnimation(reference.m_ClipIndex.m_Index);
			}
			else if (prefab is ActivityPropPrefab activityPropPrefab)
			{
				animation = activityPropPrefab.GetAnimation(reference.m_ClipIndex.m_Index);
			}
			else
			{
				if (!prefab.TryGet<ProceduralAnimationProperties>(out var component))
				{
					log.ErrorFormat("Invalid animation container: {0}", prefab ? prefab.name : "null");
					reference.m_IsLoading = false;
					reference.m_IsLoaded = true;
					continue;
				}
				animation = component.GetAnimation(reference.m_ClipIndex.m_Index);
			}
			try
			{
				reference.m_IsLoading = true;
				if (animation.AsyncLoad(out var clip))
				{
					if (LoadAnimation(reference.m_ClipIndex, clip))
					{
						reference.m_IsLoading = false;
						reference.m_IsLoaded = true;
						animation.Unload();
					}
					else if (reference.m_Priority < 0)
					{
						reference.m_IsLoading = false;
						animation.Unload();
					}
				}
				else if ((long)(++num) == 10)
				{
					break;
				}
			}
			catch (Exception exception)
			{
				log.ErrorFormat(exception, "Error when loading animation: {0}->{1}", prefab.name, animation.name);
				reference.m_IsLoading = false;
				reference.m_IsLoaded = true;
				animation.Unload();
			}
		}
	}

	private unsafe bool LoadAnimation(ClipIndex clipIndex, Colossal.Animations.AnimationClip animation)
	{
		int num = -1;
		int num2 = 1;
		int num3 = 0;
		bool flag = false;
		ActivityPropData component2;
		DynamicBuffer<ProceduralBone> buffer;
		if (base.EntityManager.TryGetComponent<CharacterStyleData>(clipIndex.m_ClipContainer, out var component))
		{
			num = component.m_RestPoseClipIndex;
			num2 = component.m_ShapeCount;
			num3 = component.m_BoneCount;
		}
		else if (base.EntityManager.TryGetComponent<ActivityPropData>(clipIndex.m_ClipContainer, out component2))
		{
			num = component2.m_RestPoseClipIndex;
			num2 = component2.m_ShapeCount;
			num3 = component2.m_BoneCount;
		}
		else if (base.EntityManager.TryGetBuffer(clipIndex.m_ClipContainer, isReadOnly: true, out buffer))
		{
			num3 = buffer.Length;
			flag = true;
		}
		DynamicBuffer<Game.Prefabs.AnimationClip> buffer2 = base.EntityManager.GetBuffer<Game.Prefabs.AnimationClip>(clipIndex.m_ClipContainer);
		base.EntityManager.TryGetBuffer(clipIndex.m_ClipContainer, isReadOnly: true, out DynamicBuffer<AnimationMotion> buffer3);
		ref Game.Prefabs.AnimationClip reference = ref buffer2.ElementAt(clipIndex.m_Index);
		DynamicBuffer<RestPoseElement> buffer4 = default(DynamicBuffer<RestPoseElement>);
		if (reference.m_RootMotionBone != -1 && (!base.EntityManager.TryGetBuffer(clipIndex.m_ClipContainer, isReadOnly: true, out buffer4) || buffer4.Length == 0))
		{
			return false;
		}
		NativeHeapAllocator nativeHeapAllocator = (flag ? m_AnimAllocatorCPU : m_AnimAllocator);
		uint num4 = (uint)animation.m_Animation.elements.Length;
		AnimationClipData value = new AnimationClipData
		{
			m_AnimAllocation = nativeHeapAllocator.Allocate(num4)
		};
		(flag ? ref m_AnimationCountCPU : ref m_AnimationCount)++;
		int num5 = m_ClipPriorities.Length - 1;
		while (value.m_AnimAllocation.Empty)
		{
			if (num5 >= 0)
			{
				ref ClipPriorityData reference2 = ref m_ClipPriorities.ElementAt(num5--);
				if (reference2.m_Priority >= 0)
				{
					num5 = -1;
					continue;
				}
				if (reference2.m_ClipIndex.Equals(clipIndex) || reference2.m_IsLoading || !reference2.m_IsLoaded || reference2.m_IsCPUAnim != flag)
				{
					continue;
				}
				reference2.m_IsLoaded = false;
				UnloadAnimation(reference2.m_ClipIndex);
			}
			else
			{
				uint num6 = (uint)(flag ? 262144 : 8388608) / (uint)sizeof(Animation.Element);
				num6 = (num6 + num4 - 1) / num6 * num6;
				nativeHeapAllocator.Resize(nativeHeapAllocator.Size + num6);
			}
			value.m_AnimAllocation = nativeHeapAllocator.Allocate(num4);
		}
		if (num == clipIndex.m_Index)
		{
			value.m_HierarchyAllocation = AllocateIndexData(flag, (uint)animation.m_BoneHierarchy.Length);
			CacheRestPose(clipIndex.m_ClipContainer, animation);
		}
		if (!flag)
		{
			value.m_ShapeAllocation = AllocateIndexData(flag, (uint)num2);
		}
		value.m_BoneAllocation = AllocateIndexData(flag, (uint)animation.m_Animation.boneIndices.Length);
		value.m_InverseBoneAllocation = AllocateIndexData(flag, (uint)num3);
		value.m_BoneCount = animation.m_Animation.boneIndices.Length;
		value.m_FrameCount = animation.m_Animation.frameCount;
		value.m_PositionMin = animation.m_Animation.positionMin;
		value.m_PositionRange = animation.m_Animation.positionRange;
		m_MaxBoneCount = math.max(m_MaxBoneCount, animation.m_BoneHierarchy.Length);
		m_MaxActiveBoneCount = math.max(m_MaxActiveBoneCount, value.m_BoneCount);
		if (m_FreeAnimIndices.IsEmpty)
		{
			reference.m_InfoIndex = m_AnimationClipData.Length;
			m_AnimationClipData.Add(in value);
		}
		else
		{
			reference.m_InfoIndex = m_FreeAnimIndices[m_FreeAnimIndices.Length - 1];
			m_FreeAnimIndices.RemoveAt(m_FreeAnimIndices.Length - 1);
			m_AnimationClipData[reference.m_InfoIndex] = value;
		}
		ResizeAnimInfoBuffer();
		ResizeAnimBuffer();
		ResizeIndexBuffer();
		if (!flag)
		{
			NativeArray<AnimationInfoData> data = new NativeArray<AnimationInfoData>(1, Allocator.Temp) { [0] = new AnimationInfoData
			{
				m_Offset = (int)value.m_AnimAllocation.Begin,
				m_Hierarchy = (value.m_HierarchyAllocation.Empty ? (-1) : ((int)value.m_HierarchyAllocation.Begin)),
				m_Shapes = (int)value.m_ShapeAllocation.Begin,
				m_Bones = (int)value.m_BoneAllocation.Begin,
				m_InverseBones = (int)value.m_InverseBoneAllocation.Begin,
				m_ShapeCount = animation.m_Animation.shapeIndices.Length,
				m_BoneCount = value.m_BoneCount,
				m_Type = (int)animation.m_Animation.type,
				m_PositionMin = value.m_PositionMin,
				m_PositionRange = value.m_PositionRange
			} };
			m_AnimInfoBuffer.SetData(data, 0, reference.m_InfoIndex, 1);
			data.Dispose();
		}
		NativeArray<Animation.Element> nativeArray;
		if (flag)
		{
			nativeArray = m_AnimBufferCPU.AsArray().GetSubArray((int)value.m_AnimAllocation.Begin, (int)num4);
			nativeArray.CopyFrom(animation.m_Animation.elements);
		}
		else
		{
			nativeArray = new NativeArray<Animation.Element>(animation.m_Animation.elements, Allocator.Temp);
		}
		if (reference.m_RootMotionBone != -1 && reference.m_Layer != Game.Prefabs.AnimationLayer.Prop)
		{
			NativeArray<AnimationMotion> subArray = buffer3.AsNativeArray().GetSubArray(reference.m_MotionRange.x, reference.m_MotionRange.y - reference.m_MotionRange.x);
			RemoveRootMotion(animation, reference, buffer4, subArray, nativeArray);
		}
		if (reference.m_TargetValue == float.MinValue)
		{
			ClipIndex item;
			DynamicBuffer<Game.Prefabs.AnimationClip> buffer5;
			if (reference.m_Layer == Game.Prefabs.AnimationLayer.Prop)
			{
				reference.m_TargetValue = FindTargetValue(animation, reference, nativeArray);
			}
			else if (reference.m_PropID.isValid && m_PropClipIndex.TryGetValue(new PropClipKey(reference.m_PropID, reference.m_Activity, reference.m_Type, reference.m_Gender), out item) && base.EntityManager.TryGetBuffer(item.m_ClipContainer, isReadOnly: true, out buffer5) && buffer5.Length > item.m_Index)
			{
				reference.m_TargetValue = buffer5[item.m_Index].m_TargetValue;
			}
		}
		if (!flag)
		{
			m_AnimBuffer.SetData(nativeArray, 0, (int)value.m_AnimAllocation.Begin, (int)num4);
			nativeArray.Dispose();
		}
		if (!value.m_HierarchyAllocation.Empty)
		{
			m_IndexBuffer.SetData(animation.m_BoneHierarchy, 0, (int)value.m_HierarchyAllocation.Begin, animation.m_BoneHierarchy.Length);
		}
		if (!value.m_ShapeAllocation.Empty)
		{
			NativeArray<int> data2 = new NativeArray<int>(num2, Allocator.Temp);
			for (int i = 0; i < data2.Length; i++)
			{
				data2[i] = -1;
			}
			for (int j = 0; j < animation.m_Animation.shapeIndices.Length; j++)
			{
				data2[animation.m_Animation.shapeIndices[j]] = j;
			}
			m_IndexBuffer.SetData(data2, 0, (int)value.m_ShapeAllocation.Begin, data2.Length);
			data2.Dispose();
		}
		if (!value.m_BoneAllocation.Empty)
		{
			if (flag)
			{
				m_IndexBufferCPU.AsArray().GetSubArray((int)value.m_BoneAllocation.Begin, animation.m_Animation.boneIndices.Length).CopyFrom(animation.m_Animation.boneIndices);
			}
			else
			{
				m_IndexBuffer.SetData(animation.m_Animation.boneIndices, 0, (int)value.m_BoneAllocation.Begin, animation.m_Animation.boneIndices.Length);
			}
		}
		if (!value.m_InverseBoneAllocation.Empty)
		{
			NativeArray<int> data3 = ((!flag) ? new NativeArray<int>(num3, Allocator.Temp) : m_IndexBufferCPU.AsArray().GetSubArray((int)value.m_InverseBoneAllocation.Begin, num3));
			for (int k = 0; k < data3.Length; k++)
			{
				data3[k] = -1;
			}
			for (int l = 0; l < animation.m_Animation.boneIndices.Length; l++)
			{
				data3[animation.m_Animation.boneIndices[l]] = l;
			}
			if (!flag)
			{
				m_IndexBuffer.SetData(data3, 0, (int)value.m_InverseBoneAllocation.Begin, data3.Length);
				data3.Dispose();
			}
		}
		return true;
	}

	private NativeHeapBlock AllocateIndexData(bool isCpuAnim, uint size)
	{
		NativeHeapAllocator nativeHeapAllocator = (isCpuAnim ? m_IndexAllocatorCPU : m_IndexAllocator);
		NativeHeapBlock result = nativeHeapAllocator.Allocate(size);
		while (result.Empty)
		{
			uint num = (uint)(isCpuAnim ? 1024 : 16384) / 4u;
			num = (num + size - 1) / num * num;
			nativeHeapAllocator.Resize(nativeHeapAllocator.Size + num);
			result = nativeHeapAllocator.Allocate(size);
		}
		return result;
	}

	private void UnloadAnimation(ClipIndex clipIndex)
	{
		int num = -1;
		bool flag = false;
		ActivityPropData component2;
		DynamicBuffer<ProceduralBone> buffer;
		if (base.EntityManager.TryGetComponent<CharacterStyleData>(clipIndex.m_ClipContainer, out var component))
		{
			num = component.m_RestPoseClipIndex;
		}
		else if (base.EntityManager.TryGetComponent<ActivityPropData>(clipIndex.m_ClipContainer, out component2))
		{
			num = component2.m_RestPoseClipIndex;
		}
		else if (base.EntityManager.TryGetBuffer(clipIndex.m_ClipContainer, isReadOnly: true, out buffer))
		{
			flag = true;
		}
		ref Game.Prefabs.AnimationClip reference = ref base.EntityManager.GetBuffer<Game.Prefabs.AnimationClip>(clipIndex.m_ClipContainer).ElementAt(clipIndex.m_Index);
		if (reference.m_InfoIndex >= 0)
		{
			AnimationClipData animationClipData = m_AnimationClipData[reference.m_InfoIndex];
			NativeHeapAllocator nativeHeapAllocator = (flag ? m_AnimAllocatorCPU : m_AnimAllocator);
			NativeHeapAllocator nativeHeapAllocator2 = (flag ? m_IndexAllocatorCPU : m_IndexAllocator);
			if (num == clipIndex.m_Index)
			{
				UnCacheRestPose(clipIndex.m_ClipContainer);
			}
			if (!animationClipData.m_AnimAllocation.Empty)
			{
				nativeHeapAllocator.Release(animationClipData.m_AnimAllocation);
			}
			if (!animationClipData.m_HierarchyAllocation.Empty)
			{
				nativeHeapAllocator2.Release(animationClipData.m_HierarchyAllocation);
			}
			if (!animationClipData.m_ShapeAllocation.Empty)
			{
				nativeHeapAllocator2.Release(animationClipData.m_ShapeAllocation);
			}
			if (!animationClipData.m_BoneAllocation.Empty)
			{
				nativeHeapAllocator2.Release(animationClipData.m_BoneAllocation);
			}
			if (!animationClipData.m_InverseBoneAllocation.Empty)
			{
				nativeHeapAllocator2.Release(animationClipData.m_InverseBoneAllocation);
			}
			if (reference.m_InfoIndex == m_AnimationClipData.Length - 1)
			{
				m_AnimationClipData.RemoveAt(reference.m_InfoIndex);
			}
			else
			{
				m_FreeAnimIndices.Add(in reference.m_InfoIndex);
			}
			reference.m_InfoIndex = -1;
			(flag ? ref m_AnimationCountCPU : ref m_AnimationCount)--;
		}
	}

	private void CacheRestPose(Entity clipContainer, Colossal.Animations.AnimationClip restPose)
	{
		DynamicBuffer<RestPoseElement> buffer = base.EntityManager.GetBuffer<RestPoseElement>(clipContainer);
		buffer.ResizeUninitialized(restPose.m_Animation.elements.Length);
		for (int i = 0; i < buffer.Length; i++)
		{
			Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(restPose.m_Animation.elements[i], restPose.m_Animation.positionMin, restPose.m_Animation.positionRange);
			buffer[i] = new RestPoseElement
			{
				m_Position = elementRaw.position,
				m_Rotation = elementRaw.rotation
			};
		}
	}

	private void UnCacheRestPose(Entity clipContainer)
	{
		DynamicBuffer<RestPoseElement> buffer = base.EntityManager.GetBuffer<RestPoseElement>(clipContainer);
		buffer.Clear();
		buffer.TrimExcess();
	}

	private float FindTargetValue(Colossal.Animations.AnimationClip animation, Game.Prefabs.AnimationClip animationClip, NativeArray<Animation.Element> elements)
	{
		if (animationClip.m_Activity == ActivityType.Driving)
		{
			Game.Prefabs.AnimationType type = animationClip.m_Type;
			if ((uint)(type - 6) <= 3u)
			{
				return FindTargetRotation(animation, elements);
			}
		}
		return 0f;
	}

	private float FindTargetRotation(Colossal.Animations.AnimationClip animation, NativeArray<Animation.Element> elements)
	{
		int num = animation.m_Animation.shapeIndices.Length;
		int num2 = animation.m_Animation.boneIndices.Length;
		float num3 = 0f;
		for (int i = 0; i < num2; i++)
		{
			Animation.ElementRaw elementRaw = AnimationEncoding.DecodeElement(elements.ElementAt(i * num), animation.m_Animation.positionMin, animation.m_Animation.positionRange);
			float y = MathUtils.RotationAngle(quaternion.identity, elementRaw.rotation);
			num3 = math.max(num3, y);
		}
		return num3;
	}

	public static void RemoveRootMotion(Colossal.Animations.AnimationClip animation, Game.Prefabs.AnimationClip animationClip, DynamicBuffer<RestPoseElement> restPose, NativeArray<AnimationMotion> motions, NativeArray<Animation.Element> elements)
	{
		int[] inverseBoneIndices = animation.GetInverseBoneIndices();
		int num = animation.m_Animation.shapeIndices.Length;
		int num2 = animation.m_Animation.boneIndices.Length;
		int num3 = num * num2;
		int num4 = elements.Length / num3 - 1;
		int num5 = inverseBoneIndices.Length;
		int num6 = restPose.Length / num5;
		for (int i = 0; i <= num4; i++)
		{
			float t = math.select((float)i / (float)(num4 - 1), 0f, i >= num4);
			for (int j = 0; j < num; j++)
			{
				int num7 = animation.m_Animation.shapeIndices[j];
				AnimationMotion animationMotion = motions[num7];
				int num8 = inverseBoneIndices[animationClip.m_RootMotionBone];
				if (num8 < 0)
				{
					continue;
				}
				int num9 = i * num3 + num8 * num;
				ref Animation.Element reference = ref elements.ElementAt(num9 + j);
				Animation.ElementRaw input = AnimationEncoding.DecodeElement(reference, animation.m_Animation.positionMin, animation.m_Animation.positionRange);
				quaternion quaternion = math.slerp(animationMotion.m_StartRotation, animationMotion.m_EndRotation, t);
				float3 @float = ((animationClip.m_Playback == AnimationPlayback.Once) ? MathUtils.Position(new Bezier4x3(animationMotion.m_StartOffset, animationMotion.m_StartOffset, animationMotion.m_EndOffset, animationMotion.m_EndOffset), t) : math.lerp(animationMotion.m_StartOffset, animationMotion.m_EndOffset, t));
				if (num7 != 0)
				{
					AnimationMotion animationMotion2 = motions[0];
					quaternion b = math.slerp(animationMotion2.m_StartRotation, animationMotion2.m_EndRotation, t);
					float3 float2 = ((animationClip.m_Playback == AnimationPlayback.Once) ? MathUtils.Position(new Bezier4x3(animationMotion2.m_StartOffset, animationMotion2.m_StartOffset, animationMotion2.m_EndOffset, animationMotion2.m_EndOffset), t) : math.lerp(animationMotion2.m_StartOffset, animationMotion2.m_EndOffset, t));
					@float += float2;
					quaternion = math.mul(quaternion, b);
				}
				for (int num10 = animation.m_BoneHierarchy[animationClip.m_RootMotionBone]; num10 != -1; num10 = animation.m_BoneHierarchy[num10])
				{
					num8 = inverseBoneIndices[num10];
					if (num8 >= 0)
					{
						num9 = i * num3 + num8 * num;
						ref Animation.Element reference2 = ref elements.ElementAt(num9 + j);
						Animation.ElementRaw input2 = AnimationEncoding.DecodeElement(reference2, animation.m_Animation.positionMin, animation.m_Animation.positionRange);
						if ((double)input2.position.y < -0.1)
						{
							input2.position *= -1f;
						}
						input.position = input2.position + math.mul(input2.rotation, input.position);
						input.rotation = math.mul((quaternion)input2.rotation, (quaternion)input.rotation).value;
						reference = AnimationEncoding.EncodeElement(input, animation.m_Animation.positionMin, animation.m_Animation.positionRange);
						input2.position = float3.zero;
						input2.rotation = quaternion.identity.value;
						reference2 = AnimationEncoding.EncodeElement(input2, animation.m_Animation.positionMin, animation.m_Animation.positionRange);
					}
					else
					{
						int num11 = num10 * num6;
						RestPoseElement restPoseElement = restPose[num11 + num7];
						restPoseElement.m_Rotation = math.inverse(restPoseElement.m_Rotation);
						@float = math.mul(restPoseElement.m_Rotation, @float - restPoseElement.m_Position);
						quaternion = math.normalize(math.mul(restPoseElement.m_Rotation, quaternion));
					}
				}
				quaternion = math.inverse(quaternion);
				input.position = math.mul(quaternion, input.position - @float);
				input.rotation = math.normalize(math.mul(quaternion, input.rotation)).value;
				reference = AnimationEncoding.EncodeElement(input, animation.m_Animation.positionMin, animation.m_Animation.positionRange);
			}
		}
	}

	private void UpdateMetaData()
	{
		int num = 0;
		while (num < m_UpdatedMetaIndices.Length)
		{
			int num2 = m_UpdatedMetaIndices[num++];
			int num3 = num2 + 1;
			while (num < m_UpdatedMetaIndices.Length)
			{
				int num4 = m_UpdatedMetaIndices[num];
				if (num4 != num3)
				{
					break;
				}
				num++;
				num3 = num4 + 1;
			}
			m_MetaBuffer.SetData(m_MetaBufferData.AsArray(), num2, num2, num3 - num2);
		}
		m_UpdatedMetaIndices.Clear();
	}

	private unsafe void ResizeBoneBuffer()
	{
		int num = ((m_BoneBuffer != null) ? m_BoneBuffer.count : 0);
		int size = (int)m_BoneAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured)
			{
				name = "Bone buffer"
			};
			Shader.SetGlobalBuffer("boneBuffer", computeBuffer);
			if (m_BoneHistoryBuffer == null)
			{
				Shader.SetGlobalBuffer("boneHistoryBuffer", computeBuffer);
			}
			if (m_BoneBuffer != null)
			{
				m_BoneBuffer.Release();
			}
			if (m_LocalTRSBlendPoseBuffer != null)
			{
				m_LocalTRSBlendPoseBuffer.Release();
			}
			if (m_LocalTRSBoneBuffer != null)
			{
				m_LocalTRSBoneBuffer.Release();
			}
			BoneElement[] array = new BoneElement[computeBuffer.count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = new BoneElement
				{
					m_Matrix = float4x4.identity
				};
			}
			computeBuffer.SetData(array);
			m_BoneBuffer = computeBuffer;
			m_LocalTRSBlendPoseBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
			m_LocalTRSBoneBuffer = new ComputeBuffer(size, sizeof(BoneElement), ComputeBufferType.Structured);
			m_LocalTRSBlendPoseBuffer.name = "LocalTRSBlendPoseBuffer";
			m_LocalTRSBoneBuffer.name = "LocalTRSBoneBuffer";
		}
	}

	private unsafe void ResizeBoneHistoryBuffer()
	{
		int num = ((m_BoneHistoryBuffer != null) ? m_BoneHistoryBuffer.count : 0);
		int num2 = (int)(m_RenderingSystem.motionVectors ? m_BoneAllocator.Size : 0);
		if (num == num2)
		{
			return;
		}
		if (num2 == 0)
		{
			if (m_BoneHistoryBuffer != null)
			{
				if (m_BoneHistoryBuffer != null)
				{
					m_BoneHistoryBuffer.Release();
				}
				m_BoneHistoryBuffer = null;
			}
			if (m_BoneBuffer != null)
			{
				Shader.SetGlobalBuffer("boneHistoryBuffer", m_BoneBuffer);
			}
			return;
		}
		ComputeBuffer computeBuffer = new ComputeBuffer(num2, sizeof(BoneElement), ComputeBufferType.Structured)
		{
			name = "Bone history buffer"
		};
		Shader.SetGlobalBuffer("boneHistoryBuffer", computeBuffer);
		if (m_BoneHistoryBuffer != null)
		{
			m_BoneHistoryBuffer.Release();
		}
		BoneElement[] array = new BoneElement[computeBuffer.count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new BoneElement
			{
				m_Matrix = float4x4.identity
			};
		}
		computeBuffer.SetData(array);
		m_BoneHistoryBuffer = computeBuffer;
	}

	private unsafe void ResizeAnimInfoBuffer()
	{
		int num = ((m_AnimInfoBuffer != null) ? m_AnimInfoBuffer.count : 0);
		int capacity = m_AnimationClipData.Capacity;
		if (num != capacity)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(capacity, sizeof(AnimationInfoData), ComputeBufferType.Structured)
			{
				name = "Animation info buffer"
			};
			int num2 = math.min(num, capacity);
			if (num2 > 0)
			{
				AnimationInfoData[] data = new AnimationInfoData[num2];
				m_AnimInfoBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_AnimInfoBuffer != null)
			{
				m_AnimInfoBuffer.Release();
			}
			m_AnimInfoBuffer = computeBuffer;
		}
	}

	private unsafe void ResizeAnimBuffer()
	{
		int num = ((m_AnimBuffer != null) ? m_AnimBuffer.count : 0);
		int size = (int)m_AnimAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, sizeof(Animation.Element), ComputeBufferType.Structured)
			{
				name = "Animation buffer"
			};
			int num2 = math.min(num, size);
			if (num2 > 0)
			{
				Animation.Element[] data = new Animation.Element[num2];
				m_AnimBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_AnimBuffer != null)
			{
				m_AnimBuffer.Release();
			}
			m_AnimBuffer = computeBuffer;
		}
		num = m_AnimBufferCPU.Length;
		size = (int)m_AnimAllocatorCPU.Size;
		if (num != size)
		{
			m_AnimBufferCPU.ResizeUninitialized(size);
		}
	}

	private unsafe void ResizeMetaBuffer()
	{
		int num = ((m_MetaBuffer != null) ? m_MetaBuffer.count : 0);
		int num2 = 1048576 / sizeof(MetaBufferData);
		if (m_MetaBufferData.Length > num && m_MetaBufferData.Length > num2)
		{
			num2 += ((m_MetaBufferData.Length - num2) * sizeof(MetaBufferData) + 262144 - 1) / 262144 * 262144 / sizeof(MetaBufferData);
		}
		else if (num > num2)
		{
			num2 = num;
		}
		if (num != num2)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(num2, sizeof(MetaBufferData), ComputeBufferType.Structured)
			{
				name = "Meta buffer"
			};
			Shader.SetGlobalBuffer("metaBuffer", computeBuffer);
			if (m_MetaBuffer != null)
			{
				computeBuffer.SetData(m_MetaBufferData.AsArray(), 0, 0, num);
				m_MetaBuffer.Release();
			}
			else
			{
				computeBuffer.SetData(m_MetaBufferData.AsArray(), 0, 0, 1);
			}
			m_MetaBuffer = computeBuffer;
		}
	}

	private void ResizeIndexBuffer()
	{
		int num = ((m_IndexBuffer != null) ? m_IndexBuffer.count : 0);
		int size = (int)m_IndexAllocator.Size;
		if (num != size)
		{
			ComputeBuffer computeBuffer = new ComputeBuffer(size, 4, ComputeBufferType.Structured)
			{
				name = "Index buffer"
			};
			int num2 = math.min(num, size);
			if (num2 > 0)
			{
				int[] data = new int[num2];
				m_IndexBuffer.GetData(data, 0, 0, num2);
				computeBuffer.SetData(data);
			}
			if (m_IndexBuffer != null)
			{
				m_IndexBuffer.Release();
			}
			m_IndexBuffer = computeBuffer;
		}
		num = m_IndexBufferCPU.Length;
		size = (int)m_IndexAllocatorCPU.Size;
		if (num != size)
		{
			m_IndexBufferCPU.ResizeUninitialized(size);
		}
	}

	public void PreDeserialize(Context context)
	{
		if (m_IsAllocating)
		{
			m_AllocateDeps.Complete();
			m_IsAllocating = false;
		}
		m_BoneAllocator.Clear();
		m_MetaBufferData.Clear();
		m_FreeMetaIndices.Clear();
		m_UpdatedMetaIndices.Clear();
		m_InstanceIndices.Clear();
		m_BodyInstances.Clear();
		m_FaceInstances.Clear();
		m_CorrectiveInstances.Clear();
		m_BodyTransitions.Clear();
		m_FaceTransitions.Clear();
		m_BoneAllocationRemoves.Clear();
		m_MetaBufferRemoves.Clear();
		m_BoneAllocator.Allocate(1u);
		m_MetaBufferData.Add(default(MetaBufferData));
	}

	public AllocationData GetAllocationData(out JobHandle dependencies)
	{
		dependencies = m_AllocateDeps;
		m_IsAllocating = true;
		return new AllocationData(m_BoneAllocator, m_MetaBufferData, m_FreeMetaIndices, m_UpdatedMetaIndices, m_BoneAllocationRemoves, m_MetaBufferRemoves, m_CurrentTime);
	}

	public AnimationData GetAnimationData(out JobHandle dependencies)
	{
		dependencies = m_AllocateDeps;
		if (!m_TempAnimationQueue.IsCreated)
		{
			m_TempAnimationQueue = new NativeQueue<AnimationFrameData>(Allocator.TempJob);
		}
		if (!m_TempPriorityQueue.IsCreated)
		{
			m_TempPriorityQueue = new NativeQueue<ClipPriorityData>(Allocator.TempJob);
		}
		m_IsAllocating = true;
		return new AnimationData(m_TempAnimationQueue, m_TempPriorityQueue, m_PropClipIndex, m_AnimationClipData, m_AnimBufferCPU, m_IndexBufferCPU);
	}

	public void AddAllocationWriter(JobHandle handle)
	{
		m_AllocateDeps = handle;
	}

	public void AddAnimationWriter(JobHandle handle)
	{
		m_AllocateDeps = handle;
	}

	public AnimatedPropID GetPropID(string name)
	{
		int value = -1;
		if (!string.IsNullOrEmpty(name) && !m_PropIDs.TryGetValue(name, out value))
		{
			value = m_PropIDs.Count;
			m_PropIDs.Add(name, value);
		}
		return new AnimatedPropID(value);
	}

	public void AddPropClip(AnimatedPropID propID, ActivityType activityType, Game.Prefabs.AnimationType animationType, GenderMask gender, Entity clipContainer, int index)
	{
		if (m_IsAllocating)
		{
			m_AllocateDeps.Complete();
			m_IsAllocating = false;
		}
		m_PropClipIndex[new PropClipKey(propID, activityType, animationType, gender)] = new ClipIndex(clipContainer, index);
	}

	public unsafe void GetBoneStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		allocatedSize = m_BoneAllocator.UsedSpace * (uint)sizeof(BoneElement);
		bufferSize = m_BoneAllocator.Size * (uint)sizeof(BoneElement);
		if (m_RenderingSystem.motionVectors)
		{
			allocatedSize <<= 1;
			bufferSize <<= 1;
		}
		count = (uint)(m_MetaBufferData.Length - m_FreeMetaIndices.Length - 1);
	}

	public unsafe void GetAnimStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		allocatedSize = m_AnimAllocator.UsedSpace * (uint)sizeof(Animation.Element);
		bufferSize = m_AnimAllocator.Size * (uint)sizeof(Animation.Element);
		count = (uint)m_AnimationCount;
	}

	public unsafe void GetAnimStatsCPU(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		allocatedSize = m_AnimAllocatorCPU.UsedSpace * (uint)sizeof(Animation.Element);
		bufferSize = m_AnimAllocatorCPU.Size * (uint)sizeof(Animation.Element);
		count = (uint)m_AnimationCountCPU;
	}

	public void GetIndexStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		allocatedSize = m_IndexAllocator.UsedSpace * 4;
		bufferSize = m_IndexAllocator.Size * 4;
		count = (uint)m_AnimationCount;
	}

	public void GetIndexStatsCPU(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		allocatedSize = m_IndexAllocatorCPU.UsedSpace * 4;
		bufferSize = m_IndexAllocatorCPU.Size * 4;
		count = (uint)m_AnimationCountCPU;
	}

	public unsafe void GetMetaStats(out uint allocatedSize, out uint bufferSize, out uint count)
	{
		m_AllocateDeps.Complete();
		count = (uint)(m_MetaBufferData.Length - m_FreeMetaIndices.Length - 1);
		if (m_MetaBuffer != null)
		{
			allocatedSize = (count + 1) * (uint)sizeof(MetaBufferData);
			bufferSize = (uint)(m_MetaBuffer.count * sizeof(MetaBufferData));
		}
		else
		{
			allocatedSize = 0u;
			bufferSize = 0u;
		}
	}

	[Preserve]
	public AnimatedSystem()
	{
	}
}
