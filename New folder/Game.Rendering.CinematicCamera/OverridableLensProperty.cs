using System;

namespace Game.Rendering.CinematicCamera;

public class OverridableLensProperty<T>
{
	private CameraUpdateSystem m_CameraUpdateSystem;

	private readonly Action<IGameCameraController, T> m_Setter;

	private readonly Func<IGameCameraController, T> m_Getter;

	private T m_Value;

	private bool m_OverrideState;

	public bool overrideState
	{
		get
		{
			return m_OverrideState;
		}
		set
		{
			if (!value)
			{
				SetDefault();
			}
			else
			{
				Apply(m_Value);
			}
			m_OverrideState = value;
		}
	}

	public T currentValue
	{
		get
		{
			if (overrideState)
			{
				return value;
			}
			if (m_CameraUpdateSystem.cinematicCameraController != null)
			{
				return m_Getter(m_CameraUpdateSystem.cinematicCameraController);
			}
			return default(T);
		}
	}

	public T value
	{
		get
		{
			return m_Value;
		}
		set
		{
			m_Value = value;
			if (overrideState)
			{
				Apply(m_Value);
			}
		}
	}

	public OverridableLensProperty(CameraUpdateSystem cameraUpdateSystem, Action<IGameCameraController, T> setter, Func<IGameCameraController, T> getter)
	{
		m_CameraUpdateSystem = cameraUpdateSystem;
		m_Setter = setter;
		m_Getter = getter;
	}

	public void Override(T v)
	{
		value = v;
		overrideState = true;
	}

	private void Apply(T v)
	{
		if (m_CameraUpdateSystem.cinematicCameraController != null)
		{
			m_Setter(m_CameraUpdateSystem.cinematicCameraController, v);
		}
	}

	private T GetDefault()
	{
		if (m_CameraUpdateSystem.gamePlayController != null)
		{
			return m_Getter(m_CameraUpdateSystem.gamePlayController);
		}
		return default(T);
	}

	public void Sync()
	{
		m_Value = GetDefault();
		Apply(m_Value);
	}

	private void SetDefault()
	{
		Apply(GetDefault());
	}
}
