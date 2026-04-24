using SharpSim.Core;
namespace SharpSim.Demo;

public static class DemoRun
{
    public static void Simulate()
    {
        Simulation world = new Simulation(new EventList());

        Machine machine = new Machine(world, "Machine1");
        Source source = new Source(world, "Source1", new List<Machine> { machine });

        world.Run(20);
    }
}