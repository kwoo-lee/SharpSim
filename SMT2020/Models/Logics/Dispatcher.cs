using DocumentFormat.OpenXml.Office.CustomUI;
using SharpSim;

namespace SMT2020;

public class Dispatcher : IDispatcher
{
    public DispatchResult Do(SimTime now, ToolGroup toolGroup)
    {
        if (toolGroup.Toolype == ToolType.Batch)
            return BatchLogic(now, toolGroup);

        return toolGroup.AreaType == AreaType.Litho
            ? PhotoLogic(now, toolGroup)
            : BaseLogic(now, toolGroup);
    }

    /// <summary>
    /// 기본 단일-Lot 디스패칭 로직.
    /// 1. 가용 Tool 선별 (Breakdown/PM 제외)
    /// 2. LotQueue를 DispatchingRuleSet(Main→Ranking1→2→3)으로 정렬
    /// 3. 가용 Tool과 정렬된 Lot을 순서대로 1:1 매칭
    /// </summary>
    private static DispatchResult BaseLogic(SimTime now, ToolGroup toolGroup)
    {
        var assignments = new Dictionary<Tool, List<Lot>>();

        if (toolGroup.LotQueue.Count == 0)
            return new DispatchResult { Assignments = assignments };

        var availTools = toolGroup.Tools
            .Where(t => t.State is not (ToolState.Breakdown or ToolState.PM) && !t.IsReserved)
            .ToList();

        if (availTools.Count == 0)
            return new DispatchResult { Assignments = assignments };

        var sortedLots = toolGroup.DispatchingRuleSet.Sort([.. toolGroup.LotQueue], now);

        int lotIdx = 0;
        foreach (var tool in availTools)
        {
            if (lotIdx >= sortedLots.Count) break;
            assignments[tool] = [sortedLots[lotIdx++]];
        }

        return new DispatchResult { Assignments = assignments };
    }

    private static DispatchResult PhotoLogic(SimTime now, ToolGroup toolGroup)
    {
        // TBD: Litho 전용 로직 (Setup 고려 등)
        return BaseLogic(now, toolGroup);
    }

    /// <summary>
    /// Batch 디스패칭 로직. 가용 BatchTool 각각에 대해, queue를 정렬한 뒤
    /// 다음 세 조건 중 하나를 만족할 때만 lots를 묶어서 할당한다.
    ///   1. tentative batch size >= BatchMaximum
    ///   2. tentative batch size >= BatchMinimum  AND  longest wait > MaximumWaitingTimeForMaxBatch
    ///   3. tentative batch size <  BatchMinimum  AND  longest wait > MaximumWaitingTimeForMinBatch
    /// 어느 조건도 만족하지 않으면 보류하고, 임계점에 도달하는 시각을 NextWakeup으로 돌려준다.
    /// </summary>
    private static DispatchResult BatchLogic(SimTime now, ToolGroup toolGroup)
    {
        var assignments = new Dictionary<Tool, List<Lot>>();
        if (toolGroup.LotQueue.Count == 0)
            return new DispatchResult { Assignments = assignments };

        var availTools = toolGroup.Tools
            .Where(t => t.State is not (ToolState.Breakdown or ToolState.PM) && !t.IsReserved)
            .ToList();
        if (availTools.Count == 0)
            return new DispatchResult { Assignments = assignments };

        var sortedLots = toolGroup.DispatchingRuleSet.Sort([.. toolGroup.LotQueue], now);

        // 같은 Step끼리만 batch 가능 → 정렬 순서를 유지하면서 Step별로 그룹화
        var lotsByStep = new Dictionary<Step, List<Lot>>();
        foreach (var lot in sortedLots)
        {
            Step? s = lot.CurrentStep;
            if (s == null) continue;
            if (!lotsByStep.TryGetValue(s, out var bucket))
            {
                bucket = [];
                lotsByStep[s] = bucket;
            }
            bucket.Add(lot);
        }

        SimTime? nextWakeup = null;
        int toolIdx = 0;

        foreach (var (step, lots) in lotsByStep)
        {
            if (toolIdx >= availTools.Count) break;

            int batchMin = step.BatchMinimum > 0 ? step.BatchMinimum : 75;
            int batchMax = step.BatchMaximum > 0 ? step.BatchMaximum : 100;

            // 정렬 순서대로 wafer 합계가 batchMax를 넘지 않게 lot을 채움
            var tempBatch = new List<Lot>();
            int batchWaferCount = 0;
            foreach (var lot in lots)
            {
                if (batchWaferCount + lot.WafersPerLot > batchMax) continue;
                tempBatch.Add(lot);
                batchWaferCount += lot.WafersPerLot;
            }

            if (tempBatch.Count == 0) continue;

            SimTime oldestEnqueue = tempBatch.Min(l => l.EnqueueTime);
            SimTime longestWait = now - oldestEnqueue;

            bool dispatch =
                batchWaferCount >= batchMax
                || (batchWaferCount >= batchMin
                    && step.MaximumWaitingTimeForMaxBatch.HasValue
                    && longestWait > step.MaximumWaitingTimeForMaxBatch.Value)
                || (batchWaferCount < batchMin
                    && step.MaximumWaitingTimeForMinBatch.HasValue
                    && longestWait > step.MaximumWaitingTimeForMinBatch.Value);

            if (dispatch)
            {
                assignments[availTools[toolIdx]] = tempBatch;
                toolIdx++;
                continue;
            }

            // 보류 — 임계점에 닿는 시각을 wakeup으로 등록 (그룹별 임계점이 다를 수 있으므로 모두 평가)
            double? threshold = batchWaferCount >= batchMin
                ? step.MaximumWaitingTimeForMaxBatch
                : step.MaximumWaitingTimeForMinBatch;

            if (threshold.HasValue)
            {
                SimTime wakeupAt = oldestEnqueue + (SimTime)threshold.Value;
                if (nextWakeup == null || wakeupAt < nextWakeup.Value)
                    nextWakeup = wakeupAt;
            }
            // 다른 step 그룹은 독립적으로 평가되어야 함 → 계속 진행
        }

        return new DispatchResult { Assignments = assignments, NextWakeup = nextWakeup };
    }
}
