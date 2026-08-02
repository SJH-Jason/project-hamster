using HamsterRace.Domain;
using HamsterRace.Simulation;

// CLI:跑一場比賽,印出文字戰報。
//   用法:  dotnet run --project src/HamsterRace.Console -- [seed] [distance] [hamstersFile]
//   例:    dotnet run --project src/HamsterRace.Console -- 42 100
//          dotnet run --project src/HamsterRace.Console -- 42 100 hamsters_brownie_conserve.json

ulong seed = args.Length > 0 && ulong.TryParse(args[0], out var s) ? s : 42UL;
double distance = args.Length > 1 && double.TryParse(args[1], out var d) ? d : 100.0;
string hamstersFile = args.Length > 2 ? args[2] : "hamsters.json";

// 找 data/*.json(從執行檔往上找專案根)
var hamsters = DataLoader.LoadHamsters(FindDataFile(hamstersFile));
var cards = DataLoader.LoadCards(FindDataFile("cards.json"));
var skills = DataLoader.LoadSkills(FindDataFile("skills.json"));
var environment = DataLoader.LoadEnvironment(FindDataFile("races.json"));

var config = new RaceConfig
{
    RaceId = "R-001",
    Seed = seed,
    DistanceMeters = distance,
    Hamsters = hamsters,
    Cards = cards,
    Skills = skills,
    Environment = environment,
};

var result = new RaceSimulator().Run(config);
Console.WriteLine(TextReport.Render(result));

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
