using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public class UIButtonInteraction : IInputInteraction
{
	public float repeatDelay = 0.5f;

	public float repeatRate = 0.1f;

	public float pressPoint;

	private float pressPointOrDefault
	{
		get
		{
			if (!((double)pressPoint > 0.0))
			{
				return InputSystem.settings.defaultButtonPressPoint;
			}
			return pressPoint;
		}
	}

	public void Process(ref InputInteractionContext context)
	{
		switch (context.phase)
		{
		case InputActionPhase.Waiting:
			if (context.ControlIsActuated(pressPointOrDefault))
			{
				context.Started();
				context.PerformedAndStayStarted();
				context.SetTimeout(repeatDelay);
			}
			break;
		case InputActionPhase.Started:
			if (context.timerHasExpired)
			{
				context.PerformedAndStayStarted();
				context.SetTimeout(repeatRate);
			}
			else if (!context.ControlIsActuated(pressPointOrDefault))
			{
				context.Canceled();
			}
			break;
		}
	}

	public void Reset()
	{
	}

	static UIButtonInteraction()
	{
		InputSystem.RegisterInteraction<UIButtonInteraction>();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
	}
}
