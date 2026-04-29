using SharpSim;
using Xunit.Abstractions;

namespace SMT2020.Test;

public class DispatcherTests
{
    private readonly IDispatcher _dispatcher = new Dispatcher();
    private readonly SimTime _now = new SimTime(0.0);
    private readonly ITestOutputHelper _output;

    public DispatcherTests(ITestOutputHelper output) => _output = output;

    // ──────────────────────────────────────────────
    // Empty-input edge cases
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_EmptyLotQueue_ReturnsEmpty()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Empty(result);
    }

    [Fact]
    public void Do_NoTools_ReturnsEmpty()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 0);
        tg.Enqueue(TestHelpers.CreateLot(tg, 1));

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Empty(result);
    }

    [Fact]
    public void Do_AllToolsBreakdown_ReturnsEmpty()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        foreach (var t in tg.Tools) t.SetState(ToolState.Breakdown);
        tg.Enqueue(TestHelpers.CreateLot(tg, 1));

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Empty(result);
    }

    [Fact]
    public void Do_AllToolsPM_ReturnsEmpty()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        foreach (var t in tg.Tools) t.SetState(ToolState.PM);
        tg.Enqueue(TestHelpers.CreateLot(tg, 1));

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Empty(result);
    }

    // ──────────────────────────────────────────────
    // Basic 1:1 matching shape
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_SingleLotSingleTool_AssignsOneToOne()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1);
        var lot = TestHelpers.CreateLot(tg, 1);
        tg.Enqueue(lot);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Single(result);
        var (tool, lots) = Assert.Single(result);
        Assert.Same(tg.Tools[0], tool);
        Assert.Single(lots);
        Assert.Same(lot, lots[0]);
    }

    [Fact]
    public void Do_MoreLotsThanTools_AssignsTopLotsOnly()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);

        var l1 = TestHelpers.CreateLot(tg, 1, enqueueSec: 0);
        var l2 = TestHelpers.CreateLot(tg, 2, enqueueSec: 1);
        var l3 = TestHelpers.CreateLot(tg, 3, enqueueSec: 2);
        tg.Enqueue(l1, l2, l3);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Equal(2, result.Count);
        var assignedLots = result.SelectMany(kv => kv.Value).ToList();
        Assert.Contains(l1, assignedLots);
        Assert.Contains(l2, assignedLots);
        Assert.DoesNotContain(l3, assignedLots);
    }

    [Fact]
    public void Do_MoreToolsThanLots_OnlyAssignsAvailableLots()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 3);

        var l1 = TestHelpers.CreateLot(tg, 1);
        var l2 = TestHelpers.CreateLot(tg, 2);
        tg.Enqueue(l1, l2);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Equal(2, result.Count);
        Assert.All(result.Values, lots => Assert.Single(lots));
    }

    [Fact]
    public void Do_AssignsExactlyOneLotPerTool()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 3);
        for (int i = 1; i <= 5; i++) tg.LotQueue.Add(TestHelpers.CreateLot(tg, i));

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.All(result.Values, lots => Assert.Single(lots));
        Assert.Equal(3, result.Count);
    }

    // ──────────────────────────────────────────────
    // Tool availability filters
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_BreakdownToolIsSkipped_HealthyToolGetsLot()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        tg.Tools[0].SetState(ToolState.Breakdown);

        var lot = TestHelpers.CreateLot(tg, 1);
        tg.Enqueue(lot);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Single(result);
        Assert.True(result.ContainsKey(tg.Tools[1]));
        Assert.False(result.ContainsKey(tg.Tools[0]));
    }

    [Fact]
    public void Do_PMToolIsSkipped_HealthyToolGetsLot()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        tg.Tools[1].SetState(ToolState.PM);

        var lot = TestHelpers.CreateLot(tg, 1);
        tg.Enqueue(lot);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Single(result);
        Assert.True(result.ContainsKey(tg.Tools[0]));
        Assert.False(result.ContainsKey(tg.Tools[1]));
    }

    [Fact]
    public void Do_IdleAndBusyTools_BothEligibleWhenPortsFree()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        tg.Tools[0].SetState(ToolState.Idle);
        tg.Tools[1].SetState(ToolState.Busy);

        var l1 = TestHelpers.CreateLot(tg, 1);
        var l2 = TestHelpers.CreateLot(tg, 2);
        tg.Enqueue(l1, l2);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Do_ToolWithAllPortsTaken_IsSkipped()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        TestHelpers.FillTool(tg.Tools[0]);

        var lot = TestHelpers.CreateLot(tg, 1);
        tg.Enqueue(lot);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Single(result);
        Assert.True(result.ContainsKey(tg.Tools[1]));
        Assert.False(result.ContainsKey(tg.Tools[0]));
    }

    // ──────────────────────────────────────────────
    // Dispatching rules — ordering
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_FifoRule_PicksEarliestEnqueueFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.FIFO);

        var late = TestHelpers.CreateLot(tg, 1, enqueueSec: 100);
        var early = TestHelpers.CreateLot(tg, 2, enqueueSec: 10);
        var mid = TestHelpers.CreateLot(tg, 3, enqueueSec: 50);
        tg.Enqueue(late, early, mid);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(early, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_LifoRule_PicksLatestEnqueueFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.LIFO);

        var late = TestHelpers.CreateLot(tg, 1, enqueueSec: 100);
        var early = TestHelpers.CreateLot(tg, 2, enqueueSec: 10);
        tg.Enqueue(early, late);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(late, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_PriorityRule_PicksHighestPriorityFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.Priority);

        var low = TestHelpers.CreateLot(tg, 1, priority: 1);
        var high = TestHelpers.CreateLot(tg, 2, priority: 99);
        var mid = TestHelpers.CreateLot(tg, 3, priority: 50);
        tg.Enqueue(low, high, mid);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(high, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_SptRule_PicksShortestProcessingFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.SPT);

        var slow = TestHelpers.CreateLot(tg, 1, processingSec: 500);
        var fast = TestHelpers.CreateLot(tg, 2, processingSec: 30);
        var mid = TestHelpers.CreateLot(tg, 3, processingSec: 100);
        tg.Enqueue(slow, fast, mid);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(fast, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_LptRule_PicksLongestProcessingFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.LPT);

        var slow = TestHelpers.CreateLot(tg, 1, processingSec: 500);
        var fast = TestHelpers.CreateLot(tg, 2, processingSec: 30);
        tg.Enqueue(fast, slow);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(slow, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_EddRule_PicksEarliestDueDateFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.EDD);

        var lateDue = TestHelpers.CreateLot(tg, 1, dueSec: 10_000);
        var earlyDue = TestHelpers.CreateLot(tg, 2, dueSec: 200);
        var midDue = TestHelpers.CreateLot(tg, 3, dueSec: 1_000);
        tg.Enqueue(lateDue, earlyDue, midDue);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(earlyDue, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_CrRule_PicksMostCriticallyOverdueFirst()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 1, rank1: DispatchingRuleType.CR);

        // CR = (due - now) / processing.  Smaller = more critical.
        // safe : (1000 - 0) / 100 = 10
        // tight: (200  - 0) / 100 = 2
        // crit : (50   - 0) / 100 = 0.5  ← most critical
        var safe = TestHelpers.CreateLot(tg, 1, processingSec: 100, dueSec: 1_000);
        var tight = TestHelpers.CreateLot(tg, 2, processingSec: 100, dueSec: 200);
        var crit = TestHelpers.CreateLot(tg, 3, processingSec: 100, dueSec: 50);
        tg.Enqueue(safe, tight, crit);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(crit, Assert.Single(result.Values).Single());
    }

    // ──────────────────────────────────────────────
    // Multi-level ranking tie-breakers
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_PriorityTiesBrokenByFifo_AcrossSecondaryRule()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(
            fab,
            numberOfTools: 1,
            rank1: DispatchingRuleType.Priority,
            rank2: DispatchingRuleType.FIFO);

        // Same priority — FIFO breaks the tie (earlier enqueue wins).
        var laterSamePri = TestHelpers.CreateLot(tg, 1, priority: 50, enqueueSec: 100);
        var earlierSamePri = TestHelpers.CreateLot(tg, 2, priority: 50, enqueueSec: 10);
        var lowestPri = TestHelpers.CreateLot(tg, 3, priority: 1, enqueueSec: 0);
        tg.Enqueue(laterSamePri, lowestPri, earlierSamePri);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Same(earlierSamePri, Assert.Single(result.Values).Single());
    }

    [Fact]
    public void Do_TwoToolsTwoLots_OrderedByRule_HighestFirstToFirstTool()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2, rank1: DispatchingRuleType.Priority);

        var p1 = TestHelpers.CreateLot(tg, 1, priority: 1);
        var p9 = TestHelpers.CreateLot(tg, 2, priority: 9);
        var p5 = TestHelpers.CreateLot(tg, 3, priority: 5);
        tg.Enqueue(p1, p9, p5);

        var result = _dispatcher.Do(_now, tg).Assignments;

        // First available tool (Tools[0]) gets the highest-priority lot.
        Assert.Same(p9, result[tg.Tools[0]].Single());
        Assert.Same(p5, result[tg.Tools[1]].Single());
        Assert.Equal(2, result.Count);
    }

    // ──────────────────────────────────────────────
    // ToolGroup type / area routing
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_LithoArea_StillProducesOneToOneAssignment()
    {
        // PhotoLogic currently delegates to BaseLogic — verify Litho path is wired
        // and produces the same shape (single lot per available tool).
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(
            fab,
            numberOfTools: 2,
            area: AreaType.Litho,
            type: ToolType.Table,
            rank1: DispatchingRuleType.FIFO);

        var l1 = TestHelpers.CreateLot(tg, 1, enqueueSec: 0);
        var l2 = TestHelpers.CreateLot(tg, 2, enqueueSec: 1);
        tg.Enqueue(l1, l2);

        var result = _dispatcher.Do(_now, tg).Assignments;

        Assert.Equal(2, result.Count);
        Assert.All(result.Values, lots => Assert.Single(lots));
    }

    // ──────────────────────────────────────────────
    // BatchLogic — wafer-sum thresholds & wait-time policy
    // ──────────────────────────────────────────────

    private static ToolGroup MakeBatchTG(Fab fab, int numberOfTools = 1) =>
        TestHelpers.CreateToolGroup(fab, numberOfTools: numberOfTools, type: ToolType.Batch);

    [Fact]
    public void Batch_WaferSumReachesMax_DispatchesImmediately()
    {
        // 4 lots × 25 wafers = 100 wafers == BatchMaximum → 즉시 dispatch (조건 1)
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100);
        for (int i = 1; i <= 4; i++)
            tg.LotQueue.Add(TestHelpers.LotForStep(step, i, wafersPerLot: 25));

        var dr = _dispatcher.Do(_now, tg);

        Assert.Single(dr.Assignments);
        var assigned = dr.Assignments[tg.Tools[0]];
        Assert.Equal(4, assigned.Count);
        Assert.Equal(100, assigned.Sum(l => l.WafersPerLot));
        _output.WriteLine($"NextWakeup = {(dr.NextWakeup.HasValue ? ((double)dr.NextWakeup.Value).ToString() : "null")}");
        Assert.Null(dr.NextWakeup);
    }

    [Fact]
    public void Batch_BelowMin_NotEnoughWait_HoldsAndSchedulesWakeup()
    {
        // 1 lot × 25 wafers, BatchMin=75. wait=0 < MaxWaitForMinBatch(=180) → 보류
        // wakeup = enqueue(0) + 180 = 180
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100, processingSec: 600);
        tg.LotQueue.Add(TestHelpers.LotForStep(step, 1, wafersPerLot: 25, enqueueSec: 0));

        var dr = _dispatcher.Do(_now, tg);

        Assert.Empty(dr.Assignments);
        Assert.NotNull(dr.NextWakeup);
        Assert.Equal(180.0, (double)dr.NextWakeup.Value, precision: 3);
    }

    [Fact]
    public void Batch_BelowMin_PastMinBatchWait_DispatchesPartial()
    {
        // 1 lot × 25, BatchMin=75. now=200 > MaxWaitForMinBatch(180) → 조건 3 만족 → dispatch
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100, processingSec: 600);
        tg.LotQueue.Add(TestHelpers.LotForStep(step, 1, wafersPerLot: 25, enqueueSec: 0));

        var now = new SimTime(200.0);
        var dr = _dispatcher.Do(now, tg);

        Assert.Single(dr.Assignments);
        Assert.Single(dr.Assignments[tg.Tools[0]]);
    }

    [Fact]
    public void Batch_AboveMin_NotEnoughWait_HoldsAndSchedulesShorterWakeup()
    {
        // 3 lots × 25 = 75 wafers == BatchMin (>= min, < max). wait=0 < MaxWaitForMaxBatch(=60) → 보류
        // wakeup = enqueue(0) + 60
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100, processingSec: 600);
        for (int i = 1; i <= 3; i++)
            tg.LotQueue.Add(TestHelpers.LotForStep(step, i, wafersPerLot: 25, enqueueSec: 0));

        var dr = _dispatcher.Do(_now, tg);

        Assert.Empty(dr.Assignments);
        Assert.NotNull(dr.NextWakeup);
        Assert.Equal(60.0, (double)dr.NextWakeup.Value, precision: 3);
    }

    [Fact]
    public void Batch_AboveMin_PastMaxBatchWait_Dispatches()
    {
        // 3 lots × 25 = 75 wafers == BatchMin. now=70 > MaxWaitForMaxBatch(60) → 조건 2 → dispatch
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100, processingSec: 600);
        for (int i = 1; i <= 3; i++)
            tg.LotQueue.Add(TestHelpers.LotForStep(step, i, wafersPerLot: 25, enqueueSec: 0));

        var now = new SimTime(70.0);
        var dr = _dispatcher.Do(now, tg);

        Assert.Single(dr.Assignments);
        Assert.Equal(3, dr.Assignments[tg.Tools[0]].Count);
    }

    [Fact]
    public void Batch_DifferentSteps_AreNotMixed()
    {
        // 같은 ToolGroup이지만 Step이 다른 두 lot은 같은 batch에 묶이지 않음.
        // 각 step group은 독립 평가되어, 두 그룹 모두 wafer 부족(50 < 75)으로 보류.
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab, numberOfTools: 2);
        var stepA = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100);
        var stepB = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100);

        tg.LotQueue.Add(TestHelpers.LotForStep(stepA, 1, wafersPerLot: 50));
        tg.LotQueue.Add(TestHelpers.LotForStep(stepB, 2, wafersPerLot: 50));

        var dr = _dispatcher.Do(_now, tg);

        // 두 그룹 다 batchMin 미달 + wait=0 → 둘 다 보류
        Assert.Empty(dr.Assignments);
        Assert.NotNull(dr.NextWakeup);
    }

    [Fact]
    public void Batch_DifferentSteps_BothHitMax_GetSeparateTools()
    {
        // 다른 step끼리 각자 batchMax(100) 도달 → 가용 tool 두 대에 각각 하나씩 할당
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab, numberOfTools: 2);
        var stepA = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100);
        var stepB = TestHelpers.MakeBatchStep(tg, batchMin: 75, batchMax: 100);

        var a1 = TestHelpers.LotForStep(stepA, 1, wafersPerLot: 100);
        var b1 = TestHelpers.LotForStep(stepB, 2, wafersPerLot: 100);
        tg.LotQueue.Add(a1);
        tg.LotQueue.Add(b1);

        var dr = _dispatcher.Do(_now, tg);

        Assert.Equal(2, dr.Assignments.Count);
        var allAssigned = dr.Assignments.SelectMany(kv => kv.Value).ToList();
        Assert.Contains(a1, allAssigned);
        Assert.Contains(b1, allAssigned);
    }

    [Fact]
    public void Batch_GreedyFit_SkipsOversizedLot()
    {
        // batchMax=80. 25 + 50 = 75 (다음 25는 100 초과 → fit 안 됨 X, 75+25=100인데 max=80 초과).
        // 첫 25: 0+25=25 ≤ 80 ✓
        // 다음 50: 25+50=75 ≤ 80 ✓
        // 다음 25: 75+25=100 > 80 → skip
        // → 2 lots, 75 wafers.
        var fab = TestHelpers.CreateFab();
        var tg = MakeBatchTG(fab);
        var step = TestHelpers.MakeBatchStep(tg, batchMin: 50, batchMax: 80, processingSec: 600);

        var l1 = TestHelpers.LotForStep(step, 1, wafersPerLot: 25);
        var l2 = TestHelpers.LotForStep(step, 2, wafersPerLot: 50);
        var l3 = TestHelpers.LotForStep(step, 3, wafersPerLot: 25);
        tg.LotQueue.Add(l1);
        tg.LotQueue.Add(l2);
        tg.LotQueue.Add(l3);

        // wait=0이지만 75 ≥ batchMin(50). 그러나 wait=0 < MaxWaitForMaxBatch(60) → 보류 예상
        // 실제 dispatch를 검증하려면 wait 충분.
        var now = new SimTime(70.0);
        var dr = _dispatcher.Do(now, tg);

        Assert.Single(dr.Assignments);
        var assigned = dr.Assignments[tg.Tools[0]];
        Assert.Equal(2, assigned.Count);
        Assert.Contains(l1, assigned);
        Assert.Contains(l2, assigned);
        Assert.DoesNotContain(l3, assigned);
    }

    // ──────────────────────────────────────────────
    // Result-shape invariants
    // ──────────────────────────────────────────────

    [Fact]
    public void Do_AssignmentsAreUniqueLotsAcrossTools()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 3);
        for (int i = 1; i <= 5; i++) tg.LotQueue.Add(TestHelpers.CreateLot(tg, i));

        var result = _dispatcher.Do(_now, tg).Assignments;

        var allAssigned = result.SelectMany(kv => kv.Value).ToList();
        Assert.Equal(allAssigned.Count, allAssigned.Distinct().Count());
    }

    [Fact]
    public void Do_DoesNotMutateLotQueue()
    {
        var fab = TestHelpers.CreateFab();
        var tg = TestHelpers.CreateToolGroup(fab, numberOfTools: 2);
        for (int i = 1; i <= 3; i++) tg.LotQueue.Add(TestHelpers.CreateLot(tg, i));
        var before = tg.LotQueue.ToList();

        _dispatcher.Do(_now, tg);

        Assert.Equal(before, tg.LotQueue);
    }
}
