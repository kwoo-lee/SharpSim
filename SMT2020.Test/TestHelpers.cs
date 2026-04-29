using SharpSim;

namespace SMT2020.Test;

/// <summary>
/// Builds minimal Fab/ToolGroup/Tool/Lot graphs for Dispatcher tests
/// without needing the Excel dataset or a running simulation.
/// </summary>
internal static class TestHelpers
{
    public static Fab CreateFab() => new Fab(new EventList());

    public static ToolGroup CreateToolGroup(
        Fab fab,
        int numberOfTools = 1,
        AreaType area = AreaType.Diffusion,
        ToolType type = ToolType.Table,
        ProcessingUnit unit = ProcessingUnit.Lot,
        DispatchingRuleType rank1 = DispatchingRuleType.FIFO,
        DispatchingRuleType? rank2 = null,
        DispatchingRuleType? rank3 = null,
        string name = "TG")
    {
        var tg = new ToolGroup(0, name, area, type, unit, loadingTime: 0, unloadingTime: 0);
        tg.SetDispatchingRule(new DispatchingRuleSet(rank1, rank2, rank3));
        tg.AddTool(fab, fab.History, numberOfTools);
        return tg;
    }

    /// <summary>
    /// Build a Lot with a one-step Route pointing at <paramref name="tg"/>,
    /// and a constant processing time. Lot.StepIndex is set to 0 so that
    /// CurrentStep is valid for SPT/LPT/CR rules.
    /// </summary>
    public static Lot CreateLot(
        ToolGroup tg,
        int id,
        int priority = 10,
        double processingSec = 60,
        double enqueueSec = 0,
        double dueSec = 1_000,
        double startSec = 0,
        int wafersPerLot = 1,
        string? name = null)
    {
        var route = new Route($"Route_{id}");
        var step = new Step(order: 1, description: "Step1", toolGroup: tg, processingUnit: ProcessingUnit.Lot);
        step.SetProcessingTime(new Const(processingSec), cascadingInterval: null, processingProbability: 1.0);
        route.AddStep(step);

        var lot = new Lot(
            id: id,
            name: name ?? $"Lot_{id}",
            productName: "P",
            route: route,
            wafersPerLot: wafersPerLot,
            priroity: priority,
            startTime: startSec,
            endTime: dueSec);

        lot.StepIndex = 0;
        lot.EnqueueTime = enqueueSec;
        return lot;
    }

    public static void Enqueue(this ToolGroup tg, params Lot[] lots)
    {
        foreach (var lot in lots) tg.LotQueue.Add(lot);
    }

    /// <summary>Mark a tool as reserved (busy) so the dispatcher skips it.</summary>
    public static void FillTool(Tool tool)
    {
        var sentinel = new Lot(-1_000, "_filler", "P", new Route("_"), 1, 0, 0, 0);
        tool.AssignLot(sentinel);
    }

    /// <summary>
    /// Build a fresh Step for a Batch toolgroup. SetProcessingTime triggers
    /// MaximumWaitingTimeForMin/MaxBatch population.
    /// </summary>
    public static Step MakeBatchStep(
        ToolGroup tg,
        int batchMin,
        int batchMax,
        double processingSec = 600)
    {
        var step = new Step(order: 1, description: "BatchStep", toolGroup: tg, processingUnit: ProcessingUnit.Batch);
        step.SetBatchSize(batchMin, batchMax);
        step.SetProcessingTime(new Const(processingSec), cascadingInterval: null, processingProbability: 1.0);
        return step;
    }

    /// <summary>
    /// Build a Lot whose CurrentStep is the given <paramref name="step"/> (shared by reference).
    /// Lots passed the same step end up in the same batch group.
    /// </summary>
    public static Lot LotForStep(
        Step step,
        int id,
        int wafersPerLot = 25,
        double enqueueSec = 0,
        int priority = 10)
    {
        var route = new Route($"Route_{id}");
        route.AddStep(step);

        var lot = new Lot(
            id: id,
            name: $"Lot_{id}",
            productName: "P",
            route: route,
            wafersPerLot: wafersPerLot,
            priroity: priority,
            startTime: 0,
            endTime: 100_000);
        lot.StepIndex = 0;
        lot.EnqueueTime = enqueueSec;
        return lot;
    }
}
