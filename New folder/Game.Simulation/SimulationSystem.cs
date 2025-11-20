using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Colossal;
using Colossal.Serialization.Entities;
using Game.Pathfind;
using Game.SceneFlow;
using Game.Settings;
using Game.Tools;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class SimulationSystem : GameSystemBase, ISimulationSystem, IDefaultSerializable, ISerializable
{
	public enum PerformancePreference
	{
		FrameRate,
		Balanced,
		SimulationSpeed
	}

	private struct SimulationEndTimeJob : IJob
	{
		public GCHandle m_Stopwatch;

		public void Execute()
		{
			((Stopwatch)m_Stopwatch.Target).Stop();
			m_Stopwatch.Free();
		}
	}

	public const float PENDING_FRAMES_SPEED_FACTOR = 1f / 48f;

	private const int LOADING_COUNT = 1024;

	public const string kLoadingTask = "LoadSimulation";

	private UpdateSystem m_UpdateSystem;

	private ToolSystem m_ToolSystem;

	private PathfindResultSystem m_PathfindResultSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private float m_Timer;

	private float m_LastSpeed;

	private float m_SelectedSpeed;

	private int m_LoadingCount;

	private int m_StepCount;

	private bool m_IsLoading;

	private JobHandle m_WatchDeps;

	private Stopwatch m_Stopwatch;

	public uint frameIndex { get; private set; }

	public float frameTime { get; private set; }

	public float selectedSpeed
	{
		get
		{
			return m_SelectedSpeed;
		}
		set
		{
			if (!m_IsLoading)
			{
				m_SelectedSpeed = value;
			}
		}
	}

	public float smoothSpeed { get; private set; }

	public float loadingProgress
	{
		get
		{
			if (!m_IsLoading)
			{
				return 1f;
			}
			return TaskManager.instance.GetTaskProgress("LoadSimulation");
		}
		private set
		{
			if (m_IsLoading)
			{
				TaskManager.instance.progress.Report(new ProgressTracker("LoadSimulation", ProgressTracker.Group.Group3)
				{
					progress = value
				});
			}
		}
	}

	public float frameDuration { get; private set; }

	public PerformancePreference performancePreference { get; set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		selectedSpeed = 1f;
		performancePreference = SharedSettings.instance?.general.performancePreference ?? PerformancePreference.Balanced;
		m_Stopwatch = new Stopwatch();
		m_UpdateSystem = base.World.GetOrCreateSystemManaged<UpdateSystem>();
		m_ToolSystem = base.World.GetOrCreateSystemManaged<ToolSystem>();
		m_PathfindResultSystem = base.World.GetOrCreateSystemManaged<PathfindResultSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_WatchDeps.Complete();
		base.OnDestroy();
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		writer.Write(frameIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		frameIndex = value;
		frameTime = 0f;
		m_Timer = 0f;
	}

	public void SetDefaults(Context context)
	{
		frameIndex = 0u;
		frameTime = 0f;
		m_Timer = 0f;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		selectedSpeed = 0f;
		loadingProgress = 0f;
		m_LoadingCount = ((purpose == Purpose.NewGame) ? 1024 : 0);
		m_IsLoading = true;
	}

	private void UpdateLoadingProgress()
	{
		if (m_LoadingCount > 0)
		{
			m_UpdateSystem.Update(SystemUpdatePhase.PreSimulation);
			int num = 8;
			for (int i = 0; i < num; i++)
			{
				frameIndex++;
				m_UpdateSystem.Update(SystemUpdatePhase.LoadSimulation, frameIndex, i);
			}
			m_UpdateSystem.Update(SystemUpdatePhase.PostSimulation);
			m_LoadingCount -= num;
		}
		if (m_LoadingCount > 0)
		{
			loadingProgress = math.clamp(1f - (float)m_LoadingCount / 1024f, 0f, 0.99999f);
		}
		else
		{
			loadingProgress = 1f;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_StepCount != 0)
		{
			m_WatchDeps.Complete();
			frameDuration = (float)m_Stopwatch.ElapsedTicks / (float)(Stopwatch.Frequency * m_StepCount);
			m_Stopwatch.Reset();
			m_StepCount = 0;
		}
		else
		{
			frameDuration = 0f;
		}
		if (m_IsLoading)
		{
			if (loadingProgress != 1f)
			{
				UpdateLoadingProgress();
				return;
			}
			if (!GameManager.instance.isGameLoading)
			{
				m_IsLoading = false;
				GameplaySettings gameplaySettings = SharedSettings.instance?.gameplay;
				selectedSpeed = ((gameplaySettings != null && gameplaySettings.pausedAfterLoading) ? 0f : 1f);
			}
		}
		else if (GameManager.instance.isGameLoading)
		{
			selectedSpeed = 0f;
		}
		int num;
		if (selectedSpeed == 0f)
		{
			num = 0;
			smoothSpeed = 0f;
		}
		else
		{
			float deltaTime = UnityEngine.Time.deltaTime;
			float num2 = deltaTime * selectedSpeed;
			float num3 = 1f;
			if (m_PathfindResultSystem.pendingSimulationFrame < uint.MaxValue)
			{
				int num4 = (int)math.max(0u, m_PathfindResultSystem.pendingSimulationFrame - frameIndex - 1);
				num3 = math.min(1f, (float)num4 * (1f / 48f));
				num2 *= num3;
			}
			m_Timer += num2;
			num = (int)math.floor(m_Timer * 60f);
			num2 *= 60f;
			if (m_PathfindResultSystem.pendingSimulationFrame < uint.MaxValue)
			{
				int num5 = (int)math.max(0u, m_PathfindResultSystem.pendingSimulationFrame - frameIndex - 1);
				num = math.min(num, num5);
				num2 = math.min(num2, num5);
			}
			if (performancePreference != PerformancePreference.SimulationSpeed)
			{
				float currentElapsedTime = m_EndFrameBarrier.currentElapsedTime;
				float f = (m_EndFrameBarrier.lastElapsedTime - currentElapsedTime) / math.max(0.001f, frameDuration);
				int num6 = math.max(1, (performancePreference == PerformancePreference.FrameRate) ? Mathf.FloorToInt(f) : Mathf.CeilToInt(f));
				num = math.min(num, num6);
				num2 = math.min(num2, num6);
			}
			m_Timer = math.clamp(m_Timer - (float)num / 60f, 0f, 1f / 60f);
			int num7 = math.max(1, math.min(8, Mathf.RoundToInt(selectedSpeed * num3 * 2f)));
			num = math.clamp(num, 0, num7);
			num2 = math.clamp(num2, 0f, num7);
			float num8 = num2 / math.max(1E-06f, 60f * deltaTime);
			float y = math.lerp(num8, smoothSpeed, math.pow(0.5f, deltaTime));
			float y2 = smoothSpeed + selectedSpeed - m_LastSpeed;
			if (num8 > smoothSpeed)
			{
				smoothSpeed = math.max(math.min(num8, y2), y);
			}
			else
			{
				smoothSpeed = math.min(math.max(num8, y2), y);
			}
		}
		frameTime = m_Timer * 60f;
		m_LastSpeed = selectedSpeed;
		m_UpdateSystem.Update(SystemUpdatePhase.PreSimulation);
		if (num != 0)
		{
			m_StepCount = num;
			m_Stopwatch.Start();
			for (int i = 0; i < num; i++)
			{
				frameIndex++;
				if (m_ToolSystem.actionMode.IsEditor())
				{
					m_UpdateSystem.Update(SystemUpdatePhase.EditorSimulation, frameIndex, i);
				}
				if (m_ToolSystem.actionMode.IsGame())
				{
					m_UpdateSystem.Update(SystemUpdatePhase.GameSimulation, frameIndex, i);
				}
			}
			SimulationEndTimeJob jobData = new SimulationEndTimeJob
			{
				m_Stopwatch = GCHandle.Alloc(m_Stopwatch)
			};
			m_WatchDeps = jobData.Schedule(m_EndFrameBarrier.producerHandle);
		}
		m_UpdateSystem.Update(SystemUpdatePhase.PostSimulation);
	}

	[Preserve]
	public SimulationSystem()
	{
	}
}
