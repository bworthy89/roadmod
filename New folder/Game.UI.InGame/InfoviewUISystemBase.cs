using Colossal.Serialization.Entities;
using Game.Serialization;
using Unity.Collections;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public abstract class InfoviewUISystemBase : UISystemBase, IPreDeserialize
{
	private UIUpdateState m_UpdateState;

	private bool m_Clear;

	public override GameMode gameMode => GameMode.Game;

	protected virtual bool Active => m_Clear;

	protected virtual bool Modified => false;

	protected void ResetResults<T>(NativeArray<T> results) where T : struct
	{
		for (int i = 0; i < results.Length; i++)
		{
			results[i] = default(T);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_UpdateState = UIUpdateState.Create(base.World, 256);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (Active && (Modified || m_UpdateState.Advance()))
		{
			PerformUpdate();
			m_Clear = false;
		}
	}

	protected abstract void PerformUpdate();

	public void RequestUpdate()
	{
		m_UpdateState.ForceUpdate();
	}

	public void PreDeserialize(Context context)
	{
		m_Clear = true;
		m_UpdateState.ForceUpdate();
	}

	[Preserve]
	protected InfoviewUISystemBase()
	{
	}
}
