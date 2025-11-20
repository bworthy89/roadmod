using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colossal;
using Game.Input;

namespace Game.SceneFlow;

public class LoadingScreen : IScreenState
{
	public async Task Execute(GameManager manager, CancellationToken token)
	{
		OverlayBindings overlay = manager.userInterface.overlayBindings;
		float[] progress = new float[3];
		using (InputManager.instance.CreateOverlayBarrier("LoadingScreen"))
		{
			using (overlay.ActivateScreenScoped(OverlayScreen.Loading))
			{
				while (Poll(progress))
				{
					token.ThrowIfCancellationRequested();
					overlay.SetProgress(OverlayProgressType.Outer, progress[0]);
					overlay.SetProgress(OverlayProgressType.Middle, progress[1]);
					overlay.SetProgress(OverlayProgressType.Inner, progress[2]);
					await Task.Delay(100, token);
				}
				overlay.SetProgress(OverlayProgressType.Outer, 1f);
				overlay.SetProgress(OverlayProgressType.Middle, 1f);
				overlay.SetProgress(OverlayProgressType.Inner, 1f);
				int i = 0;
				while (i < 30)
				{
					token.ThrowIfCancellationRequested();
					await Task.Yield();
					int num = i + 1;
					i = num;
				}
			}
		}
		static bool Poll(float[] array)
		{
			array[0] = TaskManager.instance.GetTaskProgress(ProgressTracker.Group.Group1);
			array[1] = TaskManager.instance.GetTaskProgress(ProgressTracker.Group.Group2);
			array[2] = TaskManager.instance.GetTaskProgress(ProgressTracker.Group.Group3);
			return array.Any((float p) => p < 1f);
		}
	}
}
