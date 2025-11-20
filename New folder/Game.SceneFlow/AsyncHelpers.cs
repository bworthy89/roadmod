using System;
using System.Threading.Tasks;

namespace Game.SceneFlow;

public static class AsyncHelpers
{
	public static async Task<bool> AwaitWithTimeout(this Task task, TimeSpan timeout)
	{
		Task task2 = Task.Delay(timeout);
		return await Task.WhenAny(task, task2) == task;
	}
}
