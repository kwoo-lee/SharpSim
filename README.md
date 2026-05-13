# SharpSim

[![NuGet](https://img.shields.io/nuget/v/SharpSim.svg?style=flat-square&logo=nuget&color=004880)](https://www.nuget.org/packages/SharpSim/)
[![Downloads](https://img.shields.io/nuget/dt/SharpSim.svg?style=flat-square&color=004880)](https://www.nuget.org/packages/SharpSim/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg?style=flat-square)](https://dotnet.microsoft.com/)

A minimal, no-magic **discrete-event simulation** library for .NET.
SharpSim gives you the core building blocks — events, an event list, and a simulation clock — then gets out of your way so you can model whatever domain you have in plain C#.

---

## Install

```bash
dotnet add package SharpSim
```

```xml
<PackageReference Include="SharpSim" Version="0.2.0" />
```

---

## Quick start

```csharp
using SharpSim;

var sim = new Simulation(new EventList());

sim.Delay(5,  new List<Action> { () => Console.WriteLine($"[t={sim.Now}] hello")  });
sim.Delay(10, new List<Action> { () => Console.WriteLine($"[t={sim.Now}] world!") });

sim.Run(endOfSimulation: 100);
```

```
[t=5] hello
[t=10] world!
```

Events fire in time order, the clock advances to each event's scheduled time, and the loop exits when no events remain or `endOfSimulation` is reached.

---

## Concepts

| Type | Role |
| --- | --- |
| `Simulation` | Owns the event list and the simulation clock. Drives the run loop. |
| `IEvent` / `Event<TNode>` | Domain events. Override `Execute()` to do the work. |
| `IEventList` / `EventList` | Time-ordered priority queue of pending events. |
| `SimTime` | Strongly typed time value with arithmetic and formatting helpers. |
| `SimNode<TSim, THistory>` | Reusable base for entities that own state and react to events. |

### Custom events

```csharp
public class Arrival : Event<MyNode>
{
    public Arrival(SimTime t, MyNode node) : base(t, node) {}

    public override void Execute()
    {
        Console.WriteLine($"[{Time}] arrival at {Node.Name}");
        // Schedule a follow-up after 3 time units
        Node.Sim.Schedule(new Arrival(Time + 3, Node));
    }
}
```

---

## Event calendars

The pending event set is the hot loop of any discrete-event simulator. SharpSim exposes `IEventList` so you can pick the implementation that fits your workload — or plug in your own.

| Implementation | Insert | Pop min | Best for |
| --- | --- | --- | --- |
| `EventList` | O(n) | O(1) | Small queues, reference / debugging |
| `MListEventList` | O(1) amortized | O(bucket size) | General-purpose DES |
| `MultiTierMListEventList` | O(1) amortized | O(bucket size) | Large queues with wide or multimodal time scales |

`MListEventList` implements the two-tier pending-event-set from Goh & Thng (2003): `N` unsorted time-bucket sub-lists plus a sorted *current list* that drains one bucket at a time. The bucket width is set from a sample-based estimate of the average inter-event gap,

```
W = α · (t_max − t_min) / (n − 1),    α = 2
```

`MultiTierMListEventList` extends this with a Ladder-Queue-style hierarchy: when a coarse bucket would contain more than a threshold of events, it is recursively subdivided into a finer tier on demand.

### Benchmarks

Apple Silicon, .NET 8, Release. Times include both inserts and pops.

**Fill-then-Drain** — insert N random events, then drain all:

| N | `EventList` | `MListEventList` | `MultiTierMListEventList` |
| ---: | ---: | ---: | ---: |
| 1,000   |     0.44 ms |   0.26 ms |   0.32 ms |
| 10,000  |    14.22 ms |   5.92 ms |   **3.46 ms** |
| 100,000 | 1,485.65 ms | 125.64 ms | **112.79 ms** |

`EventList` is effectively O(n²) overall — each sorted insert shifts the list tail. The MList variants stay roughly linear: **~13× faster** at 100k events.

**Hold-model** (DES-style) — keep `hold` in-flight events, run `steps` pop-then-schedule cycles:

| steps   | hold   | `EventList` | `MListEventList` | `MultiTierMListEventList` |
| ---:    | ---:   | ---:        | ---:             | ---: |
| 100,000 |    100 |    17.77 ms |  **9.33 ms**     | 12.70 ms |
| 100,000 |  1,000 |    41.04 ms | **17.83 ms**     | 24.35 ms |
| 100,000 | 10,000 |   259.93 ms |    29.77 ms      | **21.30 ms** |

For small in-flight populations the two-tier `MListEventList` wins outright. Once the working set is large enough that individual buckets balloon (here around 10k), the multi-tier version's on-demand subdivision pays off.

### Picking one

```csharp
// Reference / debugging — deterministic and easy to inspect
var sim = new Simulation(new EventList(), history);

// General-purpose default for larger simulations
var sim = new Simulation(new MListEventList(), history);

// Wide or multimodal time scales, large working sets
var sim = new Simulation(new MultiTierMListEventList(), history);
```

All three are drop-in `IEventList` implementations; the rest of the simulator is unchanged.

---

## Why SharpSim

- **Tiny core.** The runtime fits in a handful of files you can read in fifteen minutes.
- **No DSL, no reflection, no codegen.** Events are plain C# classes; the loop is a `while`.
- **Strongly typed time.** `SimTime` arithmetic prevents the usual "seconds or milliseconds?" bugs.
- **Bring your own domain.** Queues, dispatchers, statistics, and policies are yours to design.

---

## Status

`0.2.0` — adds `MListEventList` and `MultiTierMListEventList` alongside the original `EventList`. The core API (`Simulation`, `IEvent`, `IEventList`, `SimTime`, `SimNode`) is stable enough to build on. Auxiliary modules (geometry, graph, distributions) may shift before `1.0`.

---

## License

MIT © 2026 [Kwanwoo Lee](mailto:kwoolee94@gmail.com)
