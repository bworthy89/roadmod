using System.Threading;
using System.Threading.Tasks;
using Game.Input;

namespace Game.SceneFlow;

public class WaitScreen
{
	private const OverlayScreen k_OverlayScreen = OverlayScreen.Wait;

	public async Task Execute(GameManager manager, CancellationToken token, Task taskToWaitFor)
	{
		using (InputManager.instance.CreateOverlayBarrier("WaitScreen"))
		{
			OverlayBindings overlayBindings = manager.userInterface.overlayBindings;
			using (overlayBindings.ActivateScreenScoped(OverlayScreen.Wait))
			{
				await taskToWaitFor;
			}
		}
	}
}
