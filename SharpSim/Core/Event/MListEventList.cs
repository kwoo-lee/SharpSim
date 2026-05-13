namespace SharpSim;

// MList (Goh & Thng, 2003) - 2-tier pending event set
// - Current list  : 정렬된 sub-list, 순차적으로 drain
// - N개 bucket    : 시간 구간 [bucketBase, bucketBase + N*W) 를 N등분, 내부는 비정렬
// - Overflow      : bucket 범위를 벗어난 이벤트 임시 보관
//
// Bucket width W = α × avg inter-event time (샘플 기반). α=2면 bucket당 평균 ~2개 이벤트.
public class MListEventList : IEventList
{
    private const int    DefaultBucketCount  = 128;
    private const double WidthAlpha          = 2.0;
    private const int    BootstrapThreshold  = 32;
    private static readonly EventComparer Comparer = new();

    private readonly int             N;
    private readonly List<IEvent>[]  buckets;
    private readonly List<IEvent>    currentList = new();
    private readonly List<IEvent>    overflow    = new();

    private int    currentCursor;
    private int    activeBucket;
    private double bucketWidth;
    private double bucketBase;
    private bool   bucketized;
    private int    count;

    public MListEventList(int bucketCount = DefaultBucketCount)
    {
        N = bucketCount;
        buckets = new List<IEvent>[N];
        for (int i = 0; i < N; i++) buckets[i] = new List<IEvent>();
    }

    public int Count => count;

    public void Add(IEvent evt)
    {
        count++;

        // Bootstrap: bucketize 전까지는 정렬 리스트에 누적
        if (!bucketized)
        {
            InsertSorted(currentList, currentCursor, evt);
            if (currentList.Count - currentCursor >= BootstrapThreshold)
                Bucketize();
            return;
        }

        double t = (double)evt.Time;

        if (t < bucketBase)
        {
            InsertSorted(currentList, currentCursor, evt);
            return;
        }

        int idx = (int)((t - bucketBase) / bucketWidth);
        if (idx < activeBucket)        InsertSorted(currentList, currentCursor, evt);
        else if (idx >= N)             overflow.Add(evt);
        else                           buckets[idx].Add(evt);
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

        while (activeBucket < N && buckets[activeBucket].Count == 0)
            activeBucket++;

        if (activeBucket < N)
        {
            currentList.AddRange(buckets[activeBucket]);
            buckets[activeBucket].Clear();
            currentList.Sort(Comparer);
            activeBucket++;
            var e = currentList[currentCursor++];
            count--;
            return e;
        }

        if (overflow.Count > 0)
        {
            Rebucketize();
            return RetrieveNext();
        }
        return null;
    }

    public void Remove(IEvent evt)
    {
        int idx = currentList.IndexOf(evt, currentCursor);
        if (idx >= 0) { currentList.RemoveAt(idx); count--; return; }

        for (int i = activeBucket; i < N; i++)
            if (buckets[i].Remove(evt)) { count--; return; }

        if (overflow.Remove(evt)) count--;
    }

    private static void InsertSorted(List<IEvent> list, int from, IEvent evt)
    {
        int idx = list.BinarySearch(from, list.Count - from, evt, Comparer);
        if (idx < 0) idx = ~idx;
        list.Insert(idx, evt);
    }

    // 샘플 기반 W = α × (t_max - t_min) / (n - 1)
    private void Bucketize()
    {
        int remaining = currentList.Count - currentCursor;
        if (remaining < 2) return;

        double tMin = (double)currentList[currentCursor].Time;
        double tMax = (double)currentList[^1].Time;
        double span = tMax - tMin;
        if (span <= 0) return;

        bucketWidth = WidthAlpha * span / (remaining - 1);

        // 앞쪽 chunk는 current list에 유지, 나머지를 bucket으로 분배
        int keep = Math.Max(1, Math.Min(N / 4, remaining / 2));
        bucketBase = (double)currentList[currentCursor + keep - 1].Time;
        activeBucket = 0;

        for (int i = currentCursor + keep; i < currentList.Count; i++)
        {
            var e = currentList[i];
            int b = (int)(((double)e.Time - bucketBase) / bucketWidth);
            if (b < 0)       b = 0;
            if (b >= N)      overflow.Add(e);
            else             buckets[b].Add(e);
        }
        currentList.RemoveRange(currentCursor + keep, remaining - keep);

        bucketized = true;
    }

    // 모든 bucket이 소진된 뒤 overflow를 새 W로 재분배
    private void Rebucketize()
    {
        if (overflow.Count == 0) return;

        overflow.Sort(Comparer);
        double tMin = (double)overflow[0].Time;
        double tMax = (double)overflow[^1].Time;
        double span = tMax - tMin;

        if (overflow.Count > 1 && span > 0)
            bucketWidth = WidthAlpha * span / (overflow.Count - 1);
        // else: 직전 bucketWidth 재사용

        bucketBase   = tMin;
        activeBucket = 0;

        var leftover = new List<IEvent>();
        foreach (var e in overflow)
        {
            int b = (int)(((double)e.Time - bucketBase) / bucketWidth);
            if (b < 0)   b = 0;
            if (b >= N)  leftover.Add(e);
            else         buckets[b].Add(e);
        }
        overflow.Clear();
        overflow.AddRange(leftover);
    }
}
