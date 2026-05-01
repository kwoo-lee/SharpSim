using ClosedXML.Excel;
using SharpSim;

namespace SMT2020;

public class Fab : Simulation
{
    public FabHistory History { get; set; } = new FabHistory();
    public MES MES { get; private set; }
    public Transport Transport { get; private set; }
    public LotRelease LotRelease { get; private set; }
    public DateTime StartDateTime { get; private set; } = DateTime.MaxValue;
    private int weekNumber = 0;

    public Fab(IEventList eventList) : base(eventList)
    {
        MES = new MES(this, History, Nodes.Count, "MES");
        Transport = new Transport(this, History, Nodes.Count, "Transport");
        LotRelease = new LotRelease(this, History, this.Nodes.Count, "LotRelease");
        LotRelease.SetLocation("Fab");
    }

    public override void Run(SimTime endOfSimulation)
    {
        this.Delay(604800, [WeeklyReport]);
        base.Run(endOfSimulation);

        LogHandler.Info($"====================================");
        // LogHandler.Info($"TOTAL FABIN : {LotRelease.LastLotId}");
        // LogHandler.Info($"TOTAL FABOUT: {MES.FabOutLots.Count}");
        LogHandler.Info($"====================================");
    }

#region [Load Data]
    public void LoadData()
    {
        string excelPath;
        excelPath = "../../../Dataset/General Data/dataset 1/SMT_2020_Model_Data_-_HVLM.xlsx";
        //excelPath = "../../../Dataset/General Data/dataset 1/SMT_2020_Model_Data_-_HVLM.xlsx";
        //excelPath = "../../../Dataset/General Data/dataset 3/SMT_2020_Model_Data_-_HVLM_E.xlsx";
        //excelPath = "../../../Dataset/General Data/dataset 4/SMT_2020_Model_Data_-_LVHM_E.xlsx";

        using var workbook = new XLWorkbook(excelPath);

        // Order Sensitive
        LoadTransports(workbook); 
        LoadToolGroups(workbook);
        LoadLotRelease(workbook);
        LoadRoutes(workbook);
    }

    private void LoadTransports(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet("Transport", out var sheet))
        {
            LogHandler.Error("'Transport' sheet not found");
            return;
        }

        foreach (var row in sheet.RangeUsed().RowsUsed().Skip(1))
        {
            string from = row.Cell(1).GetString().Trim();
            string to   = row.Cell(2).GetString().Trim();
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                continue;

            string distStr = row.Cell(3).GetString().Trim();
            double mean    = row.Cell(4).GetDouble();
            double offset  = row.Cell(5).TryGetValue(out double offVal) ? offVal : 0;
            string units   = row.Cell(6).GetString();

            double meanSec   = ToTimeSpan(mean, units);
            double offsetSec = offset > 0 ? ToTimeSpan(offset, units) : 0;

            DistributionType distType = DistributionType.Constant;
            if (!string.IsNullOrEmpty(distStr))
                Enum.TryParse(distStr, ignoreCase: true, out distType);

            Distribution dist = Statistics.GetDistribution(distType, meanSec, offsetSec);

            Transport.Locations.Add(from);
            Transport.Locations.Add(to);
            Transport.DeliveryTimes[(from, to)] = dist;
        }
    }

    private void LoadToolGroups(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet("Toolgroups", out var sheet))
        {
            LogHandler.Error($"'Toolgroups' sheet not found");
            return;
        }

        foreach (var row in sheet.RangeUsed().RowsUsed().Skip(1))
        {
            string areaName = row.Cell(1).GetString().Trim();
            string toolGroupName = row.Cell(2).GetString().Trim();
            if (string.IsNullOrEmpty(toolGroupName))
                continue;

            int numberOfTools = (int)row.Cell(3).GetDouble(); // TODO
            bool isCascade = row.Cell(4).GetString().Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);
            bool isBatch = row.Cell(5).GetString().Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);
            string batchUnit = row.Cell(7).GetString().Trim();

            double loadingTime = ToTimeSpan(row.Cell(8).GetDouble(), row.Cell(9).GetString());
            double unloadingTime = ToTimeSpan(row.Cell(10).GetDouble(), row.Cell(11).GetString());
            string location  = row.Cell(12).GetString().Trim();
            //string mainRule  = row.Cell(13).GetString().Trim();
            string rank1Rule = row.Cell(14).GetString().Trim();
            string rank2Rule = row.Cell(15).GetString().Trim();
            string rank3Rule = row.Cell(16).GetString().Trim();

            if (!Enum.TryParse<AreaType>(areaName, ignoreCase: true, out var areaType))
                throw new NotSupportedException($"Unknown AreaType '{areaName}' for toolgroup '{toolGroupName}'");

            ToolType toolType = isBatch ? ToolType.Batch
                              : isCascade ? ToolType.Cascade
                              : ToolType.Table;

            ProcessingUnit processingUnit = toolType switch
            {
                ToolType.Batch => batchUnit.Equals("Wafer", StringComparison.OrdinalIgnoreCase)
                    ? ProcessingUnit.Wafer
                    : ProcessingUnit.Batch,
                ToolType.Cascade => ProcessingUnit.Lot,
                _ => ProcessingUnit.Lot,
            };

            if (string.IsNullOrEmpty(location))
                throw new InvalidDataException(
                    $"Toolgroup '{toolGroupName}' has no LOCATION value (column 12)");
            if (toolGroupName != "Delay_32" && !Transport.Locations.Contains(location))
                throw new InvalidDataException(
                    $"Toolgroup '{toolGroupName}' location '{location}' is not declared in the Transport sheet. " +
                    $"Known locations: [{string.Join(", ", Transport.Locations)}]");

            var toolGroup = new ToolGroup(MES.ToolGroups.Count, toolGroupName, areaType, toolType, processingUnit, loadingTime, unloadingTime);
            toolGroup.SetDispatchingRule(DispatchingRuleSet.ParseDispatchingRuleSet(rank1Rule, rank2Rule, rank3Rule));
            toolGroup.SetLocation(location);

            // if(toolGroupName == "Diffusion_FE_120")
            //     numberOfTools = 1;
            MES.AddToolGroup(toolGroup, numberOfTools);
        }
    }

    private void LoadRoutes(XLWorkbook workbook)
    {
        foreach (var (routeName, route) in MES.Routes)
        {
            if (!workbook.TryGetWorksheet(routeName, out var sheet))
            {
                LogHandler.Error($"Route sheet '{routeName}' not found");
                continue;
            }

            foreach (var row in sheet.RangeUsed().RowsUsed().Skip(1))
            {
                if (!int.TryParse(row.Cell(2).GetString(), out int stepOrder))
                    continue;

                string description = row.Cell(3).GetString().Trim();
                string toolGroupName = row.Cell(5).GetString().Trim();
                string processingUnitStr = row.Cell(6).GetString().Trim();

                if (!Enum.TryParse<ProcessingUnit>(processingUnitStr, ignoreCase: true, out var processingUnit))
                    throw new NotSupportedException($"Unknown ProcessingUnit '{processingUnitStr}' at step {stepOrder} of {routeName}");

                ToolGroup toolGroup = MES.ToolGroupByName[toolGroupName];
                var step = new Step(stepOrder, description, toolGroup, processingUnit);

                // Processing time
                string ptDistStr = row.Cell(7).GetString().Trim();
                double ptMean = row.Cell(8).GetDouble();
                double ptOffset = row.Cell(9).TryGetValue(out double offsetVal) ? offsetVal : 0;
                string ptUnits = row.Cell(10).GetString();
                double meanSec = ToTimeSpan(ptMean, ptUnits);
                double offsetSec = ptOffset > 0 ? ToTimeSpan(ptOffset, ptUnits) : 0;

                DistributionType ptType = DistributionType.Constant;
                if (!string.IsNullOrEmpty(ptDistStr))
                    Enum.TryParse(ptDistStr, ignoreCase: true, out ptType);
                Distribution processingTime = Statistics.GetDistribution(ptType, meanSec, offsetSec);

                // Cascading interval (optional)
                Distribution? cascadingInterval = null;
                if (!row.Cell(11).IsEmpty()) // Cell11: CASCADING INTERVAL
                {
                    double ciMean = row.Cell(11).GetDouble();
                    string ciUnits = row.Cell(12).GetString();
                    cascadingInterval = new Const(ToTimeSpan(ciMean, ciUnits));
                }

                // Sampling probability (optional, percent)
                double processingProbability = 1;
                if (row.Cell(25).TryGetValue(out double samplingPct))
                    processingProbability = samplingPct / 100;

                step.SetProcessingTime(processingTime, cascadingInterval, processingProbability);

                // Batch size (optional)
                if (!row.Cell(13).IsEmpty() && !row.Cell(14).IsEmpty())
                    step.SetBatchSize((int)row.Cell(13).GetDouble(), (int)row.Cell(14).GetDouble());

                // LTL dedication (optional)
                if (uint.TryParse(row.Cell(21).GetString(), out uint ltlStep))
                    step.SetLTLDedication(ltlStep);

                // Rework (optional)
                if (row.Cell(22).TryGetValue(out double reworkProb)
                    && uint.TryParse(row.Cell(24).GetString(), out uint reworkStep))
                {
                    step.SetRework(reworkStep, reworkProb / 100);
                }

                // Critical Queue Time (optional)
                if (uint.TryParse(row.Cell(26).GetString(), out uint cqtStep)
                    && row.Cell(27).TryGetValue(out double cqtValue))
                {
                    string cqtUnits = row.Cell(28).GetString();
                    step.SetCriticalQueueTime(cqtStep, ToTimeSpan(cqtValue, cqtUnits));
                }

                route.AddStep(step);
            }
        }

        // // Verification Code
        // foreach(var temp in Routes.Values)
        // {
        //     System.Console.WriteLine(temp.Steps.Count);
        // }
        
        // var temp2 = Routes["Route_Product_3"];
        // foreach(var (stepindex, step) in temp2.Steps)
        // {
        //     System.Console.WriteLine($"{stepindex}, {step.ToolGroupName} | {step.ProcessingUnit}");   
        // }
    }

    private void LoadLotRelease(XLWorkbook workbook)
    {
        var planByRoute = new Dictionary<string, List<ReleasePlan>>();
        var lotListByRoute = new Dictionary<string, List<Lot>>();

        // 1. Production Lot Release Schedule (* No Eng Lots *)
        if (!workbook.TryGetWorksheet("Lotrelease", out var sheet))
        {
            LogHandler.Error($"'Lotrelease' sheet not found");
            return;
        }

        foreach (var row in sheet.RangeUsed().RowsUsed().Skip(1))
        {
            string productName = row.Cell(1).GetString();
            string routeName = row.Cell(2).GetString();
            string lotType = row.Cell(3).GetString();
            int priority = int.TryParse(row.Cell(4).GetString(), out int temp1) ? temp1 : 10;
            int wafersPerLot = int.TryParse(row.Cell(6).GetString(), out int temp2)? temp2 : -1;

            if (!MES.Routes.ContainsKey(routeName))
            {
                MES.Routes[routeName] = new Route(routeName);
                planByRoute[routeName] = new List<ReleasePlan>();
                lotListByRoute[routeName] = new List<Lot>();
            }

            if(wafersPerLot > 0)
            {
                DateTime startDate = row.Cell(7).GetDateTime();
                string releaseDist = row.Cell(8).GetString();
                double releaseInterval = row.Cell(9).GetDouble();
                string releaseUnits = row.Cell(10).GetString();
                int lotsPerRelease = (int)row.Cell(11).GetDouble();
                DateTime dueDate = row.Cell(12).GetDateTime();

                if(this.StartDateTime > startDate)
                    this.StartDateTime = startDate;
                    
                SimTime cycleTime = dueDate - startDate;
                DistributionType distType = DistributionType.None;
                if(Enum.TryParse(releaseDist, ignoreCase: true, out distType))
                    distType = DistributionType.Constant;

                double interval = ToTimeSpan(releaseInterval, releaseUnits);
                Distribution distribution = Statistics.GetDistribution(distType, new double[] { interval });

                planByRoute[routeName].Add(new ReleasePlan(
                    productName, 
                    MES.Routes[routeName],
                    lotType,
                    wafersPerLot,
                    priority,
                    startDate,
                    cycleTime, 
                    distribution,
                    lotsPerRelease
                ));
            }
        }

        // 1.2 Variable Due Lots (Will Overwrite ReleasePlan)
        if(workbook.TryGetWorksheet("Lotrelease - variable due dates", out var varSheet))
        {
            foreach (var row in varSheet.RangeUsed().RowsUsed().Skip(1))
            {
                string productName = row.Cell(1).GetString();
                string routeName = row.Cell(2).GetString();
                string lotName = row.Cell(3).GetString();
                int priority = (int)row.Cell(4).GetDouble();
                int wafersPerLot = (int)row.Cell(6).GetDouble();
                DateTime startDate = row.Cell(7).GetDateTime();
                DateTime dueDate = row.Cell(8).GetDateTime();

                SimTime startTime = startDate - this.StartDateTime;
                SimTime dueTime = dueDate - this.StartDateTime;

                // If variable due date lot release exist, delete plan
                if(planByRoute[routeName].Count > 0)
                    planByRoute[routeName].RemoveAll(p => p.Priority == priority);

                var lot = new Lot(++LotRelease.LastLotId, lotName, productName, MES.Routes[routeName], priority, wafersPerLot, startTime, dueTime);
                lotListByRoute[routeName].Add(lot);
            }           
        }

        // 2. Enginerring Lot Release Scehdule
        if(workbook.TryGetWorksheet("Lotrelease - Engineering", out var engSheet))
        {
            foreach (var row in engSheet.RangeUsed().RowsUsed().Skip(1))
            {
                string productName = row.Cell(1).GetString();
                string routeName = row.Cell(2).GetString();
                string lotName = row.Cell(3).GetString();
                int priority = (int)row.Cell(4).GetDouble();
                int wafersPerLot = (int)row.Cell(5).GetDouble();
                DateTime startDate = row.Cell(6).GetDateTime();
                DateTime dueDate = row.Cell(7).GetDateTime();

                SimTime startTime = startDate - this.StartDateTime;
                SimTime dueTime = dueDate - this.StartDateTime;

                var lot = new Lot(++LotRelease.LastLotId, lotName, productName, MES.Routes[routeName], priority, wafersPerLot, startTime, dueTime);
                lotListByRoute[routeName].Add(lot);
            }         
        }

        foreach (var lots in lotListByRoute.Values)
            lots.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

        // Finally, Generate LotRelease Node. 
        LotRelease.SetReleasePlan(planByRoute);
        LotRelease.SetFutureLotList(lotListByRoute);
    }
#endregion [Load Data End]
    
#region [Reports]
    private void WeeklyReport()
    {
        weekNumber++;

        FabReport();

        this.Delay(604800, [WeeklyReport]);
    }

    private void FabReport()
    {
        string path = Path.Combine(this.LogPath, "FabWeekly.csv");
        bool needHeader = !File.Exists(path);

        int fabIn = History.FabInByProduct.Values.Sum();
        int fabOut = History.FabOutByProduct.Values.Sum();
        double avgCT = fabOut > 0
            ? History.WeeklyTotalCTSeconds / fabOut / 86400.0
            : 0.0;

        using (var writer = new StreamWriter(path, append: true))
        {
            if (needHeader)
                writer.WriteLine("Week,FabIn,FabOut,AvgCT_Days,WIP");
            writer.WriteLine($"{weekNumber},{fabIn},{fabOut},{avgCT:F2},{History.WIP}");
        }

        History.ResetWeekly();
    }
#endregion [Reports End]

#region [Utils]
    private static double ToTimeSpan(double value, string units)
    {
        return units.Trim().ToLowerInvariant() switch
        {
            "sec" or "secs" or "second" or "seconds" => value,
            "min" or "mins" or "minute" or "minutes" => value * 60.0,
            "hr" or "hrs" or "hour" or "hours" => value * 3600,
            "day" or "days" => value * 86400,
            "week" or "weeks" => value * 604800,
            _ => throw new NotSupportedException($"Unknown release interval unit: {units}")
        };
    }    
#endregion [Utils End]
}

