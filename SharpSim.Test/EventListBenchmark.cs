using System.Diagnostics;
using Xunit.Abstractions;

namespace SharpSim.Test;

public class EventListBenchmark
{
    private readonly ITestOutputHelper output;

    public EventListBenchmark(ITestOutputHelper output) { this.output = output; }

    private static readonly (string Name, Func<IEventList> Factory)[] Impls =
    {
        ("EventList            ", () => new EventList()),
        ("MListEventList       ", () => new MListEventList()),
        ("MultiTierMListEventList", () => new MultiTierMListEventList()),
    };

    [Fact]
    public void Bench_FillThenDrain()
    {
        output.WriteLine("[Fill-then-Drain] N개 삽입 후 전부 pop");
        output.WriteLine("size       | impl                    | total(ms) |  ops/sec");
        output.WriteLine("-----------|-------------------------|-----------|----------");
        foreach (int size in new[] { 1_000, 10_000, 100_000 })
        {
            foreach (var (name, factory) in Impls)
            {
                var rng = new Random(42);
                var times = new double[size];
                for (int i = 0; i < size; i++) times[i] = rng.NextDouble() * size;

                // Warmup
                Run(factory(), times);

                var sw = Stopwatch.StartNew();
                Run(factory(), times);
                sw.Stop();
                double ms = sw.Elapsed.TotalMilliseconds;
                double ops = size * 2 / (ms / 1000.0);
                output.WriteLine($"{size,10:N0} | {name} | {ms,9:F2} | {ops,9:N0}");
            }
        }

        static void Run(IEventList list, double[] times)
        {
            foreach (var t in times) list.Add(new TimeDelayEvent(t));
            while (list.RetrieveNext() != null) { }
        }
    }

    [Fact]
    public void Bench_DesStyle_HoldModel()
    {
        // 전형적 DES 부하: 항상 일정한 in-flight event 수 유지
        // 매 step: pop 1개 → now 갱신 → schedule 1개 (now + exp(mean))
        output.WriteLine("[DES Hold-Model] in-flight 유지, 총 N step (Hold 모델)");
        output.WriteLine("steps      | hold | impl                    | total(ms) | step/sec");
        output.WriteLine("-----------|------|-------------------------|-----------|----------");

        foreach (int steps in new[] { 10_000, 100_000 })
        foreach (int hold in new[] { 100, 1_000, 10_000 })
        {
            foreach (var (name, factory) in Impls)
            {
                // Warmup
                RunHold(factory(), steps / 10, hold, 1.0);

                var sw = Stopwatch.StartNew();
                RunHold(factory(), steps, hold, 1.0);
                sw.Stop();
                double ms = sw.Elapsed.TotalMilliseconds;
                double sps = steps / (ms / 1000.0);
                output.WriteLine($"{steps,10:N0} | {hold,4} | {name} | {ms,9:F2} | {sps,9:N0}");
            }
        }

        static void RunHold(IEventList list, int steps, int hold, double meanDelay)
        {
            var rng = new Random(7);
            double now = 0;

            // 초기 hold개 시드
            for (int i = 0; i < hold; i++)
            {
                double d = -Math.Log(1.0 - rng.NextDouble()) * meanDelay;
                list.Add(new TimeDelayEvent(now + d));
            }

            for (int i = 0; i < steps; i++)
            {
                var e = list.RetrieveNext();
                if (e == null) break;
                now = (double)e.Time;

                double d = -Math.Log(1.0 - rng.NextDouble()) * meanDelay;
                list.Add(new TimeDelayEvent(now + d));
            }
        }
    }
}
