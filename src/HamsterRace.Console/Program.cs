using HamsterRace.Domain;
using HamsterRace.Simulation;

// CLI:
//   單場:  dotnet run --project src/HamsterRace.Console -- [seed] [distance] [hamstersFile]
//   批次:  dotnet run --project src/HamsterRace.Console -- batch [場數] [distance] [hamstersFile]
//   例:    dotnet run --project src/HamsterRace.Console -- 42 100
//          dotnet run --project src/HamsterRace.Console -- batch 10000 100

bool batchMode = args.Length > 0 && args[0].Equals("batch", StringComparison.OrdinalIgnoreCase);

int races = batchMode && args.Length > 1 && int.TryParse(args[1], out var n) ? n : 10000;
ulong seed = !batchMode && args.Length > 0 && ulong.TryParse(args[0], out var s) ? s : 42UL;
double distance = args.Length > 2 && double.TryParse(args[2], out var d2) ? d2
                : (!batchMode && args.Length > 1 && double.TryParse(args[1], out var d1) ? d1 : 100.0);
string hamstersFile = args.Length > 3 ? args[3] : "hamsters.json";

// 找 data/*.json(從執行檔往上找專案根)
var hamsters = DataLoader.LoadHamsters(FindDataFile(hamstersFile));
var cards = DataLoader.LoadCards(FindDataFile("cards.json"));
var skills = DataLoader.LoadSkills(FindDataFile("skills.json"));
var environment = DataLoader.LoadEnvironment(FindDataFile("races.json"));
var rules = DataLoader.LoadRules(FindDataFile("rules.json"));

var config = new RaceConfig
{
    RaceId = "R-001",
    Seed = seed,
    DistanceMeters = distance,
    Hamsters = hamsters,
    Cards = cards,
    Skills = skills,
    Environment = environment,
    Rules = rules,
};

if (batchMode)
{
    var rep = new BatchSimulator().Run(config, races);
    Console.WriteLine(BatchSimulator.Render(rep));
}
else
{
    var result = new RaceSimulator().Run(config);
    Console.WriteLine(TextReport.Render(result));
}

static string FindDataFile(string fileName)
{
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        string candidate = Path.Combine(dir.FullName, "data", fileName);
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    throw new FileNotFoundException($"找不到 data/{fileName}");
}
