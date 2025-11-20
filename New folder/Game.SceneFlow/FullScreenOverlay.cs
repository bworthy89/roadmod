using System;
using System.Threading;
using System.Threading.Tasks;
using Game.Input;

namespace Game.SceneFlow;

public abstract class FullScreenOverlay : IScreenState
{
	protected const string kEngagementAnyKeyAction = "AnyKey";

	protected const string kEngagementContinueAction = "Continue";

	protected const string kEngagementCancelAction = "Cancel";

	protected Action m_CompletedEvent;

	protected bool m_Done;

	protected abstract OverlayScreen overlayScreen { get; }

	protected virtual string actionA => "Continue";

	protected virtual string actionB => "Cancel";

	protected virtual string continueDisplayProperty => null;

	protected virtual string cancelDisplayProperty => null;

	protected virtual int continueDisplayPriority => 20;

	protected virtual int cancelDisplayPriority => 20;

	protected virtual bool HandleScreenChange(OverlayScreen screen)
	{
		if (screen == overlayScreen)
		{
			InputManager.instance.AssociateActionsWithUser(associate: false);
		}
		else if (screen == OverlayScreen.None || screen == OverlayScreen.Loading)
		{
			InputManager.instance.AssociateActionsWithUser(associate: true);
		}
		return screen == overlayScreen;
	}

	public abstract Task Execute(GameManager manager, CancellationToken token);
}
