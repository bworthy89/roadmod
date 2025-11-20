using Game.Simulation;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Rendering;

public class WindTextureSystem : GameSystemBase
{
	[BurstCompile]
	private struct WindTextureJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<Wind> m_WindMap;

		public NativeArray<float2> m_WindTexture;

		public void Execute(int index)
		{
			m_WindTexture[index] = m_WindMap[index].m_Wind;
		}
	}

	private WindSystem m_WindSystem;

	private Texture2D m_WindTexture;

	private JobHandle m_UpdateHandle;

	private bool m_RequireUpdate;

	private bool m_RequireApply;

	public Texture2D WindTexture => m_WindTexture;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_WindSystem = base.World.GetOrCreateSystemManaged<WindSystem>();
		m_WindTexture = new Texture2D(WindSystem.kTextureSize, WindSystem.kTextureSize, TextureFormat.RGFloat, mipChain: false, linear: true)
		{
			name = "WindTexture",
			hideFlags = HideFlags.HideAndDontSave
		};
	}

	public void RequireUpdate()
	{
		m_RequireUpdate = true;
	}

	public void CompleteUpdate()
	{
		if (m_RequireApply)
		{
			m_RequireApply = false;
			m_UpdateHandle.Complete();
			m_WindTexture.Apply();
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (m_RequireUpdate)
		{
			m_RequireUpdate = false;
			m_RequireApply = true;
			JobHandle dependencies;
			WindTextureJob jobData = new WindTextureJob
			{
				m_WindMap = m_WindSystem.GetMap(readOnly: true, out dependencies),
				m_WindTexture = m_WindTexture.GetRawTextureData<float2>()
			};
			m_UpdateHandle = jobData.Schedule(WindSystem.kTextureSize * WindSystem.kTextureSize, dependencies);
			m_WindSystem.AddReader(m_UpdateHandle);
		}
	}

	[Preserve]
	public WindTextureSystem()
	{
	}
}
