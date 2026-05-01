using SharpSim;

namespace SMT2020;

// ──────────────────────────────────────────────
// Individual dispatching rule
// ──────────────────────────────────────────────

/// <summary>
/// 개별 디스패칭 룰. GetKey()로 Lot 우선순위 키를 반환하고,
/// Descending 으로 정렬 방향을 결정한다.
/// </summary>
public abstract class DispatchingRule(DispatchingRuleType type, bool descending = false)
{
    public DispatchingRuleType Type { get; } = type;
    public bool Descending { get; } = descending;

    public abstract IComparable GetKey(Lot lot, SimTime now);

    public static DispatchingRuleType? ParseRule(string token) =>
        string.IsNullOrWhiteSpace(token) ? null : token switch
        {
            "Highest Lotpriority" => DispatchingRuleType.Priority,
            "Least Setuptime"     => DispatchingRuleType.LeastSetup,
            "FIFO"                => DispatchingRuleType.FIFO,
            "CR"                  => DispatchingRuleType.CR,
            "SPT"                 => DispatchingRuleType.SPT,
            "LPT"                 => DispatchingRuleType.LPT,
            "EDD"                 => DispatchingRuleType.EDD,
            _                     => DispatchingRuleType.FIFO,
        };

    public static DispatchingRule Create(DispatchingRuleType type) => type switch
    {
        DispatchingRuleType.FIFO        => new FifoRule(),
        DispatchingRuleType.LIFO        => new LifoRule(),
        DispatchingRuleType.Priority    => new PriorityRule(),
        DispatchingRuleType.SPT         => new SptRule(),
        DispatchingRuleType.LPT         => new LptRule(),
        DispatchingRuleType.EDD         => new EddRule(),
        DispatchingRuleType.CR          => new CrRule(),
        DispatchingRuleType.LeastSetup  => new FifoRule(),
        _                               => new FifoRule(),
    };
}

/// <summary>FIFO — EnqueueTime 오름차순</summary>
public class FifoRule() : DispatchingRule(DispatchingRuleType.FIFO)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.Trace.EnqueueTime;
}

/// <summary>LIFO — EnqueueTime 내림차순</summary>
public class LifoRule() : DispatchingRule(DispatchingRuleType.LIFO, descending: true)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.Trace.EnqueueTime;
}

/// <summary>Priority — Priority 내림차순 (숫자 클수록 우선)</summary>
public class PriorityRule() : DispatchingRule(DispatchingRuleType.Priority, descending: true)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.Priority;
}

/// <summary>SPT — 처리시간 오름차순</summary>
public class SptRule() : DispatchingRule(DispatchingRuleType.SPT)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.GetProcessingTime();
}

/// <summary>LPT — 처리시간 내림차순</summary>
public class LptRule() : DispatchingRule(DispatchingRuleType.LPT, descending: true)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.GetProcessingTime();
}

/// <summary>EDD — DueTime 오름차순</summary>
public class EddRule() : DispatchingRule(DispatchingRuleType.EDD)
{
    public override IComparable GetKey(Lot lot, SimTime now) => lot.DueTime;
}

/// <summary>
/// CR (Critical Ratio) — 잔여시간 / 처리시간 오름차순.
/// CR &lt; 1 이면 납기 초과 위험.
/// </summary>
public class CrRule() : DispatchingRule(DispatchingRuleType.CR)
{
    public override IComparable GetKey(Lot lot, SimTime now)
    {
        double remaining = (lot.DueTime - now).TotalSeconds;
        double processing = lot.GetProcessingTime();
        return processing > 0 ? remaining / processing : double.MaxValue;
    }
}


// ──────────────────────────────────────────────
// Multi-level dispatching rule set
// ──────────────────────────────────────────────

/// <summary>
/// Main → Ranking1 → Ranking2 → Ranking3 순으로 동순위를 처리하는 룰 집합.
/// Main이 같은 Lot은 Ranking1로, Ranking1도 같으면 Ranking2로, … 비교한다.
/// </summary>
public class DispatchingRuleSet
{
    public DispatchingRule Ranking1 { get; private set; }
    public DispatchingRule? Ranking2 { get; private set; }
    public DispatchingRule? Ranking3 { get; private set; }

    public DispatchingRuleSet(

        DispatchingRuleType ranking1,
        DispatchingRuleType? ranking2 = null,
        DispatchingRuleType? ranking3 = null)
    {
        Ranking1 = DispatchingRule.Create(ranking1);
        Ranking2 = ranking2.HasValue ? DispatchingRule.Create(ranking2.Value) : null;
        Ranking3 = ranking3.HasValue ? DispatchingRule.Create(ranking3.Value) : null;
    }
    
    public static DispatchingRuleSet ParseDispatchingRuleSet(string rank1Rule, string rank2Rule, string rank3Rule)
    {
        DispatchingRuleType  rank1 = DispatchingRule.ParseRule(rank1Rule) ?? DispatchingRuleType.FIFO;
        DispatchingRuleType? rank2 = DispatchingRule.ParseRule(rank2Rule);
        DispatchingRuleType? rank3 = DispatchingRule.ParseRule(rank3Rule);

        return new DispatchingRuleSet(rank1, rank2, rank3);
    }

    /// <summary>
    /// Lot 목록을 Main → Ranking1 → Ranking2 → Ranking3 순으로 정렬한다.
    /// </summary>
    public List<Lot> Sort(List<Lot> lots, SimTime now)
    {
        IOrderedEnumerable<Lot> ordered = OrderBy(lots, Ranking1, now);
        if (Ranking2 != null) ordered = ThenBy(ordered, Ranking2, now);
        if (Ranking3 != null) ordered = ThenBy(ordered, Ranking3, now);
        return [.. ordered];
    }

    public void SetRanking(
        DispatchingRuleType ranking1,
        DispatchingRuleType? ranking2 = null,
        DispatchingRuleType? ranking3 = null)
    {
        Ranking1 = DispatchingRule.Create(ranking1);
        Ranking2 = ranking2.HasValue ? DispatchingRule.Create(ranking2.Value) : null;
        Ranking3 = ranking3.HasValue ? DispatchingRule.Create(ranking3.Value) : null;
    }

    // ── helpers ──────────────────────────────────
    private static IOrderedEnumerable<Lot> OrderBy(IEnumerable<Lot> lots, DispatchingRule rule, SimTime now) =>
        rule.Descending
            ? lots.OrderByDescending(l => rule.GetKey(l, now))
            : lots.OrderBy(l => rule.GetKey(l, now));

    private static IOrderedEnumerable<Lot> ThenBy(IOrderedEnumerable<Lot> ordered, DispatchingRule rule, SimTime now) =>
        rule.Descending
            ? ordered.ThenByDescending(l => rule.GetKey(l, now))
            : ordered.ThenBy(l => rule.GetKey(l, now));
}
