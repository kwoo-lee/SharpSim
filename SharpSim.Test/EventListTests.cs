namespace SharpSim.Test;

public class EventListTests
{
    public static IEnumerable<object[]> Implementations =>
        new List<object[]>
        {
            new object[] { "EventList",              (Func<IEventList>)(() => new EventList()) },
            new object[] { "MListEventList",         (Func<IEventList>)(() => new MListEventList()) },
            new object[] { "MultiTierMListEventList",(Func<IEventList>)(() => new MultiTierMListEventList()) },
        };

    [Theory]
    [MemberData(nameof(Implementations))]
    public void RandomInsertion_PopsInTimeOrder(string name, Func<IEventList> factory)
    {
        var list = factory();
        var rng  = new Random(42);
        var times = new List<double>();

        for (int i = 0; i < 100; i++)
        {
            double t = rng.Next(0, 100_000);
            times.Add(t);
            list.Add(new TimeDelayEvent(t));
        }
        Assert.Equal(100, list.Count);

        times.Sort();
        for (int i = 0; i < 100; i++)
        {
            var e = list.RetrieveNext();
            Assert.NotNull(e);
            Assert.Equal(times[i], (double)e!.Time, 3);
        }
        Assert.Equal(0, list.Count);
        Assert.Null(list.RetrieveNext());
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void SequentialInsertion_PopsInOrder(string name, Func<IEventList> factory)
    {
        var list = factory();
        for (int i = 0; i < 100; i++)
            list.Add(new TimeDelayEvent(i * 1.0));

        for (int i = 0; i < 100; i++)
        {
            var e = list.RetrieveNext();
            Assert.NotNull(e);
            Assert.Equal(i * 1.0, (double)e!.Time, 3);
        }
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void ReverseInsertion_PopsInOrder(string name, Func<IEventList> factory)
    {
        var list = factory();
        for (int i = 99; i >= 0; i--)
            list.Add(new TimeDelayEvent(i * 1.0));

        for (int i = 0; i < 100; i++)
        {
            var e = list.RetrieveNext();
            Assert.NotNull(e);
            Assert.Equal(i * 1.0, (double)e!.Time, 3);
        }
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void InterleavedAddPop_MaintainsOrder(string name, Func<IEventList> factory)
    {
        // DES 시맨틱: 스케줄링 시점은 항상 now + delay (now 이후)
        var list = factory();
        var rng  = new Random(7);
        double now        = 0;
        double lastPopped = double.NegativeInfinity;
        int    added      = 0;
        int    popped     = 0;

        // 초기 시드
        for (; added < 20; added++)
            list.Add(new TimeDelayEvent(now + rng.Next(1, 1000)));

        while (popped < 100)
        {
            if (added < 100 && (list.Count == 0 || rng.NextDouble() < 0.5))
            {
                list.Add(new TimeDelayEvent(now + rng.Next(1, 1000)));
                added++;
            }
            else
            {
                var e = list.RetrieveNext();
                Assert.NotNull(e);
                double t = (double)e!.Time;
                Assert.True(t >= lastPopped, $"{name}: order violated {t} < {lastPopped}");
                lastPopped = t;
                now        = t;
                popped++;
            }
        }
        Assert.Equal(0, list.Count);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void Remove_DecreasesCountAndExcludesEvent(string name, Func<IEventList> factory)
    {
        var list   = factory();
        var events = new List<IEvent>();
        for (int i = 0; i < 100; i++)
        {
            var e = new TimeDelayEvent(i * 10.0);
            events.Add(e);
            list.Add(e);
        }
        Assert.Equal(100, list.Count);

        for (int i = 1; i < 100; i += 2) list.Remove(events[i]);
        Assert.Equal(50, list.Count);

        for (int i = 0; i < 100; i += 2)
        {
            var e = list.RetrieveNext();
            Assert.NotNull(e);
            Assert.Equal(i * 10.0, (double)e!.Time, 3);
        }
        Assert.Equal(0, list.Count);
    }

    [Theory]
    [MemberData(nameof(Implementations))]
    public void ClusteredTimes_PopsInOrder(string name, Func<IEventList> factory)
    {
        // 시간 분포가 multimodal 한 경우 (bucket width 추정에 어려움)
        var list = factory();
        var rng  = new Random(123);
        var times = new List<double>();

        for (int i = 0; i < 50; i++) times.Add(rng.Next(0,    1000));     // 클러스터 A
        for (int i = 0; i < 50; i++) times.Add(rng.Next(900_000, 1_000_000)); // 클러스터 B (멀리)

        foreach (var t in times) list.Add(new TimeDelayEvent(t));
        Assert.Equal(100, list.Count);

        times.Sort();
        for (int i = 0; i < 100; i++)
        {
            var e = list.RetrieveNext();
            Assert.NotNull(e);
            Assert.Equal(times[i], (double)e!.Time, 3);
        }
    }
}
