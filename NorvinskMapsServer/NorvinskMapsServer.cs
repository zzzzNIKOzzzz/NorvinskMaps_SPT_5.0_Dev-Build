using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Utils;

namespace NorvinskMaps;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "zzzNIKOzzz.NorvinskMaps";
    public string Name { get; init; } = "NorvinskMaps";
    public string Author { get; init; } = "zzzNIKOzzz";
    public List<string>? Contributors { get; init; } = ["dirtbikercj, acidphantasm"];
    public SemanticVersioning.Version Version { get; init; } = new("1.1.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("5.0.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public bool HasPrepatcher { get; init; }
    public string License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.Preload)]
public class NorvinskMapsServer(
    ModHelper modHelper,
    CustomStaticRouter customStaticRouter)
    : IOnLoad
{
    private ModConfig? _modConfig;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");

        customStaticRouter.PassConfig(_modConfig);

        return Task.CompletedTask;
    }
}

public class ModConfig
{
    public bool AllowShowFriendlyPlayerMarkersInRaid { get; set; } = true;
    public bool AllowShowEnemyPlayerMarkersInRaid { get; set; } = true;
    public bool AllowShowBossMarkersInRaid { get; set; } = true;
    public bool AllowShowScavMarkersInRaid { get; set; } = true;
    public string ForcedLocale { get; set; } = "";
}