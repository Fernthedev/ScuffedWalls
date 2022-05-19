using System.Collections.Generic;
using System.Linq;

namespace ScuffedWalls;

internal class CustomFunctionHandler
{
    private readonly ContainerRequest _function;
    private VariableRequest _callTime;

    public CustomFunctionHandler(ContainerRequest function)
    {
        _function = function;
    }

    public CustomFunctionHandler(string name, ScuffedRequestParser parser = null)
    {
        _function = (parser ?? ScuffedRequestParser.Instance).CurrentRequest.GetCustomFunction(name);
    }

    public Workspace Result { get; private set; }

    public Workspace GetResult(float time, IEnumerable<VariableRequest> globalVariableArguments,
        bool affectPublicVariablesOnly)
    {
        _function.ResetDefaultValues();
        _function.RegisterCallTime(time);
        _callTime = _function.VariableRequests.FirstOrDefault(v => v.Name == "calltime");
        if (_callTime == null)
        {
            _callTime = new VariableRequest("calltime", time.ToString(), VariableRecomputeSettings.OnCreationOnly,
                false);
            _function.VariableRequests.Add(_callTime);
        }
        else
        {
            _callTime.Data = time.ToString();
        }

        _function.RegisterCustomVariables(globalVariableArguments, affectPublicVariablesOnly);

        Result = new WorkspaceRequestParser(_function, true).GetResult();

        _function.ResetDefaultValues();

        return Result;
    }
}