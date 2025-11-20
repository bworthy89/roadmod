using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Modding.Toolchain.Dependencies;

public class RiderDependency : BaseIDEDependency
{
	public override string name => "Rider";

	public override string icon => "Media/Toolchain/Rider.svg";

	public override bool canBeInstalled => false;

	public override bool canBeUninstalled => false;

	public override string minVersion => "2021.3.3";

	protected override Task<string> GetIDEVersion(CancellationToken token)
	{
		RiderPathLocator.RiderInfo[] array = (from a in RiderPathLocator.GetAllRiderPaths()
			orderby a.BuildNumber descending
			select a).ToArray();
		if (array.Length != 0)
		{
			return Task.FromResult(array.First().ProductInfo.version);
		}
		return Task.FromResult(string.Empty);
	}
}
