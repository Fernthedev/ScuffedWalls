using System.Linq;

namespace ScuffedWalls.Functions;

[SFunction("[NONCALLABLE] [CUSTOMFUNCTIONDHANDLER] Scuffedwalls_v2_infrastructure_CustomFunctionDeclarationParser")]
internal class CustomFunction : ScuffedFunction
{
    private readonly string[] excludes = { "repeat", "repeataddtime" };
    private CustomFunctionHandler customFunction;

    protected override void Init()
    {
        customFunction = new CustomFunctionHandler(DefiningParameter.Clean.StringData);
    }

    protected override void Update()
    {
        var result = customFunction.GetResult(Time,
            UnderlyingParameters
                .Where(p => !excludes.Any(e => e == p.Clean.Name))
                .Select(p => new VariableRequest(p.Use().Name, p.StringData)),
            true);
        InstanceWorkspace.Add(result);
        Stats.AddStats(result.BeatMap.Stats);
    }
}