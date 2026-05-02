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
<PackageReference Include="SharpSim" Version="0.1.0" />
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

## Why SharpSim

- **Tiny core.** The runtime fits in a handful of files you can read in fifteen minutes.
- **No DSL, no reflection, no codegen.** Events are plain C# classes; the loop is a `while`.
- **Strongly typed time.** `SimTime` arithmetic prevents the usual "seconds or milliseconds?" bugs.
- **Bring your own domain.** Queues, dispatchers, statistics, and policies are yours to design.

---

## Status

`0.1.0` — early preview. The core API (`Simulation`, `IEvent`, `EventList`, `SimTime`, `SimNode`) is stable enough to build on. Auxiliary modules (geometry, graph, distributions) may shift before `1.0`.

---

## License

MIT © 2026 [Kwanwoo Lee](mailto:kwoolee94@gmail.com)
