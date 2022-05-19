using System.Collections.Generic;
using System.Linq;

namespace ScuffedWalls;

public class ContainerRequest : Request
{
    public const string WorkspaceKeyword = "workspace";
    public const string DefineKeyword = "function";
    protected readonly TreeList<VariableRequest> _customVariables = new(VariableRequest.Exposer);
    private readonly TreeList<VariableRequest> _primaryRequests = new(VariableRequest.Exposer);

    private readonly TreeList<VariableRequest> _varRequestContainer = new(VariableRequest.Exposer);

    private CacheableScanner<Parameter> _paramScanner;

    public ContainerRequest()
    {
        _varRequestContainer.Register(_primaryRequests);
        _varRequestContainer.Register(_customVariables);
    }

    public string Name { get; private set; }
    public List<FunctionRequest> FunctionRequests { get; } = new();

    public List<VariableRequest> VariableRequests => _varRequestContainer.Values;

    public void ResetDefaultValues()
    {
        _customVariables.Clear();
        foreach (var Var in VariableRequests) Var.ResetDefaultValue();
        foreach (var param in Parameters) param.Variables.Clear();
    }

    public void RegisterCallTime(float call)
    {
        foreach (var Fun in FunctionRequests) Fun.SetCallTime(call);
    }

    public void RegisterCustomVariables(IEnumerable<VariableRequest> cvs, bool affectPublicVariablesOnly)
    {
        foreach (var _var in cvs)
        {
            var primaryRequest = _primaryRequests.Get(_var.Name);
            if (affectPublicVariablesOnly && primaryRequest == null) continue;
            if (primaryRequest != null && primaryRequest.Public) primaryRequest.Data = _var.Data;
            else _customVariables.Add(_var);
        }
    }

    public override Request SetupFromLines(List<Parameter> Lines)
    {
        Parameters = new TreeList<Parameter>(Lines, Parameter.Exposer);
        DefiningParameter = Lines.First();
        UnderlyingParameters = new TreeList<Parameter>(Lines.Lasts(), Parameter.Exposer);
        Name = DefiningParameter.StringData?.Trim();

        _paramScanner = new CacheableScanner<Parameter>(UnderlyingParameters);
        var previous = Type.None;

        while (_paramScanner.MoveNext())
        {
            var varIs = VariableRequest.IsName(_paramScanner.Current.Clean.Name);
            var funIs = FunctionRequest.IsName(_paramScanner.Current.Clean.Name);
            if (varIs || funIs)
            {
                addLastRequest();
                previous = varIs ? Type.VariableRequest : funIs ? Type.FunctionRequest : Type.None;
            }

            _paramScanner.AddToCache();
        }

        addLastRequest();
        return this;

        void addLastRequest()
        {
            switch (previous)
            {
                case Type.FunctionRequest:
                    if (_paramScanner.AnyCached)
                        FunctionRequests.Add(
                            (FunctionRequest)new FunctionRequest().SetupFromLines(_paramScanner.GetAndResetCache()));
                    break;
                case Type.VariableRequest:
                    if (_paramScanner.AnyCached)
                        _primaryRequests.Add(
                            (VariableRequest)new VariableRequest().SetupFromLines(_paramScanner.GetAndResetCache()));
                    break;
            }
        }
    }
}