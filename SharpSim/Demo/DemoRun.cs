using SharpSim.Core;
namespace SharpSim.Demo;

public static class DemoRun
{
    class History : IHistory
    {
        public void InitializeReport(SimTime now)
        {
            Console.WriteLine($"Simulation starts at {now}");
        }

        public void ReportWeekly(SimTime now)
        {
            Console.WriteLine($"Weekly report at {now}");
        }
    }
    public static void Simulate()
    {
        Simulation<History> world = new Simulation<History>(new EventList(), new History());

        Machine machine = new Machine(world, "Machine1");
        Source source = new Source(world, "Source1", new List<Machine> { machine });

        world.Run(20);
    }
}