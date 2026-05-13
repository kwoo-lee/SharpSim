namespace SharpSim;

// Multi-tier MList (Goh & Thng + Ladder Queue 계열)
// - Tier들이 계층을 이룸: tiers[0] coarsest → tiers[^1] finest
// - 각 finer tier는 한 coarse bucket을 N등분해서 생성 (on-demand)
// - Drain 시 가장 finer tier의 active bucket부터 처리
//   bucket size가 threshold 초과면 다시 finer tier로 subdivide
// - Insert는 finest → coarsest 순으로 적합한 tier 탐색
public class MultiTierMListEventList : IEventList
{
    private const int    DefaultBucketsPerTier = 64;
    private const int    MaxTiers              = 8;
    private const int    BootstrapThreshold    = 32;
    private const int    SubdivideThreshold    = 50;   // bucket size > 이 값이면 subdivide
    private const double WidthAlpha            = 2.0;
    private static readonly EventComparer Comparer = new();

    private class Tier
    {
        public readonly List<IEvent>[] Buckets;
        public readonly double         Width;
        public readonly double         Base;
        public          int            Active;

        public Tier(int n, double width, double @base)
        {
            Buckets = new List<IEvent>[n];
            for (int i = 0; i < n; i++) Buckets[i] = new List<IEvent>();
            Width = width;
            Base  = @base;
        }
    }

    private readonly int           N;
    private readonly List<IEvent>  currentList = new();
    private readonly List<IEvent>  overflow    = new();
    private readonly List<Tier>    tiers       = new();

    private int  currentCursor;
    private int  count;
    private bool bootstrapped;

    public MultiTierMListEventList(int bucketsPerTier = DefaultBucketsPerTier)
    {
        N = bucketsPerTier;
    }

    public int Count => count;

    public void Add(IEvent evt)
    {
        count++;

        if (!bootstrapped)
        {
            InsertSorted(currentList, currentCursor, evt);
            if (currentList.Count - currentCursor >= BootstrapThreshold)
                Bootstrap();
            return;
        }
        Place(evt);
    }

    public IEvent? RetrieveNext()
    {
        if (count == 0) return null;

        if (currentCursor < currentList.Count)
        {
            var e = currentList[currentCursor++];
            count--;
            return e;
        }

        currentList.Clear();
        currentCursor = 0;

        var bucket = DequeueNextBucket();
        if (bucket == null) return null;

        currentList.AddRange(bucket);
        currentList.Sort(Comparer);

        var ev = currentList[currentCursor++];
        count--;
        return ev;
    }

    public void Remove(IEvent evt)
    {
        int idx = currentList.IndexOf(evt, currentCursor);
        if (idx >= 0) { currentList.RemoveAt(idx); count--; return; }

        foreach (var tier in tiers)
            for (int i = tier.Active; i < N; i++)
                if (tier.Buckets[i].Remove(evt)) { count--; return; }

        if (overflow.Remove(evt)) count--;
    }

    // ─── 삽입 경로 ──────────────────────────────────────────────────────
    private void Place(IEvent evt)
    {
        double t = (double)evt.Time;

        // finest → coarsest: 처음으로 t를 수용하는 tier에 삽입
        for (int i = tiers.Count - 1; i >= 0; i--)
        {
            var tier = tiers[i];
            double minT = tier.Base + tier.Active * tier.Width;
            double maxT = tier.Base + N * tier.Width;

            if (t < minT)  continue;   // 이 tier의 active 이전 (과거쪽)
            if (t >= maxT) continue;   // 이 tier 범위 밖 (미래쪽)

            int b = (int)((t - tier.Base) / tier.Width);
            if (b < 0) b = 0;
            tier.Buckets[b].Add(evt);
            return;
        }

        // 어떤 tier도 수용 못 함 → past면 current list, future면 overflow
        bool isPast = tiers.Count > 0
                      && t < tiers[^1].Base + tiers[^1].Active * tiers[^1].Width;
        if (isPast) InsertSorted(currentList, currentCursor, evt);
        else        overflow.Add(evt);
    }

    // ─── Pop 경로 ───────────────────────────────────────────────────────
    private List<IEvent>? DequeueNextBucket()
    {
        while (true)
        {
            while (tiers.Count > 0)
            {
                var t = tiers[^1];
                while (t.Active < N && t.Buckets[t.Active].Count == 0) t.Active++;

                if (t.Active < N)
                {
                    var bucket = t.Buckets[t.Active];
                    t.Buckets[t.Active] = new List<IEvent>();
                    double bBase = t.Base + t.Active * t.Width;
                    t.Active++;

                    // 큰 bucket이면 finer tier로 subdivide 후 재진입
                    if (bucket.Count > SubdivideThreshold && tiers.Count < MaxTiers)
                    {
                        tiers.Add(Subdivide(bucket, bBase, t.Width));
                        continue;
                    }
                    return bucket;
                }

                // 현재 finest tier 완전 소진 → 제거하고 한 단계 위로
                tiers.RemoveAt(tiers.Count - 1);
            }

            if (overflow.Count == 0) return null;
            BuildTierFromOverflow();
        }
    }

    // ─── Tier 빌더들 ────────────────────────────────────────────────────
    private Tier Subdivide(List<IEvent> bucket, double rangeBase, double rangeWidth)
    {
        double w = rangeWidth / N;
        if (w <= 0) w = double.Epsilon;
        var tier = new Tier(N, w, rangeBase);
        foreach (var e in bucket)
        {
            int idx = (int)(((double)e.Time - rangeBase) / w);
            if (idx < 0)   idx = 0;
            if (idx >= N)  idx = N - 1;
            tier.Buckets[idx].Add(e);
        }
        return tier;
    }

    private void BuildTierFromOverflow()
    {
        overflow.Sort(Comparer);
        double tMin = (double)overflow[0].Time;
        double tMax = (double)overflow[^1].Time;
        double span = tMax - tMin;

        double w = (overflow.Count > 1 && span > 0)
                   ? WidthAlpha * span / (overflow.Count - 1)
                   : 1.0;

        var tier = new Tier(N, w, tMin);
        var leftover = new List<IEvent>();
        foreach (var e in overflow)
        {
            int idx = (int)(((double)e.Time - tMin) / w);
            if (idx < 0)   idx = 0;
            if (idx >= N)  leftover.Add(e);
            else           tier.Buckets[idx].Add(e);
        }
        overflow.Clear();
        overflow.AddRange(leftover);
        tiers.Add(tier);
    }

    private void Bootstrap()
    {
        int remaining = currentList.Count - currentCursor;
        if (remaining < 2) return;

        double tMin = (double)currentList[currentCursor].Time;
        double tMax = (double)currentList[^1].Time;
        double span = tMax - tMin;
        if (span <= 0) return;

        double w = WidthAlpha * span / (remaining - 1);

        int keep = Math.Max(1, Math.Min(N / 4, remaining / 2));
        double @base = (double)currentList[currentCursor + keep - 1].Time;
        var tier = new Tier(N, w, @base);

        for (int i = currentCursor + keep; i < currentList.Count; i++)
        {
            var e = currentList[i];
            int idx = (int)(((double)e.Time - @base) / w);
            if (idx < 0)   idx = 0;
            if (idx >= N)  overflow.Add(e);
            else           tier.Buckets[idx].Add(e);
        }
        currentList.RemoveRange(currentCursor + keep, remaining - keep);
        tiers.Add(tier);
        bootstrapped = true;
    }

    private static void InsertSorted(List<IEvent> list, int from, IEvent evt)
    {
        int idx = list.BinarySearch(from, list.Count - from, evt, Comparer);
        if (idx < 0) idx = ~idx;
        list.Insert(idx, evt);
    }
}
