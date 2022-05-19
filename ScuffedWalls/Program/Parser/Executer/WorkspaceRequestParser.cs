using System.Collections.Generic;
using ModChart;

namespace ScuffedWalls;

public class WorkspaceRequestParser : IRequestParser<ContainerRequest, Workspace>
{
    private IEnumerator<FunctionRequest> _functionRequestEnumerator;

    private IEnumerator<VariableRequest> _variableRequestEnumerator;

    public WorkspaceRequestParser(ContainerRequest containerrequest, bool hideLogs = false)
    {
        //Instance = this;
        HideLogs = hideLogs;
        CurrentRequest = containerrequest;
    }

    public bool HideLogs { get; set; }
    public TreeList<AssignableInlineVariable> GlobalVariables { get; private set; }
    public ContainerRequest CurrentRequest { get; }

    public Workspace Result { get; private set; }

    public Workspace GetResult()
    {
        GlobalVariables = new TreeList<AssignableInlineVariable>(AssignableInlineVariable.Exposer);
        foreach (var param in CurrentRequest.Parameters) param.Variables.Register(GlobalVariables);

        var workspace = new Workspace(BeatMap.Empty, CurrentRequest.Name);

        _variableRequestEnumerator = CurrentRequest.VariableRequests.GetEnumerator();
        _functionRequestEnumerator = CurrentRequest.FunctionRequests.GetEnumerator();

        while (_variableRequestEnumerator.MoveNext())
        {
            var result = new VariableRequestParser(_variableRequestEnumerator.Current, HideLogs).GetResult();
            if (result != null) GlobalVariables.AddRange(result);
        }

        // Parameter.AssignVariables(_request.Parameters, globalvariables);

        while (_functionRequestEnumerator.MoveNext())
            new FunctionRequestParser(_functionRequestEnumerator.Current, workspace, HideLogs).GetResult();
        Result = workspace;
        return workspace;
    }
}