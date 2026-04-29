using SharpSim;

namespace SMT2020;

public interface IDispatcher
{
    DispatchResult Do(SimTime now, ToolGroup toolGroup);
}

public struct DispatchResult
{
    public Dictionary<Tool, List<Lot>> Assignments;
    /// <summary>
    /// 보류 시 재dispatch가 필요한 절대 시각. null이면 wakeup 불필요.
    /// </summary>
    public SimTime? NextWakeup;
}