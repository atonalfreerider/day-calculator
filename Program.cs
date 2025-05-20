using System.Globalization;

string calText;
try
{
    string directory = AppDomain.CurrentDomain.BaseDirectory;
    string[] icsFiles = Directory.GetFiles(directory, "*.ics");
    if (icsFiles.Length == 0)
        throw new FileNotFoundException("No .ics files found in the application directory.");
    calText = File.ReadAllText(icsFiles[0]);
}
catch (Exception ex)
{
    Console.WriteLine($"Error reading calendar file: {ex.Message}");
    return;
}

// Find the latest date in the calendar to use as "today"
DateTime maxEventDay = DateTime.MinValue;
List<DateTime> allEventDays = [];
foreach (string line in calText.Split('\n'))
{
    if (line.StartsWith("DTSTART;VALUE=DATE:") || line.StartsWith("DTEND;VALUE=DATE:"))
    {
        string date = line[(line.IndexOf(':') + 1)..];
        int year = int.Parse(date[..4]);
        int month = int.Parse(date.Substring(4, 2));
        int day = int.Parse(date.Substring(6, 2));
        DateTime dt = new DateTime(year, month, day);
        allEventDays.Add(dt);
        if (dt > maxEventDay) maxEventDay = dt;
    }
}

DateTime schengenStart =
    allEventDays.Count > 0 ? allEventDays.Max().Subtract(new TimeSpan(180, 0, 0, 0)) : DateTime.MinValue;
DateTime today = allEventDays.Count > 0 ? allEventDays.Max() : DateTime.MinValue;

Console.WriteLine($"today: {Color(today.ToString(CultureInfo.InvariantCulture), "yellow")}");
Console.WriteLine(
    $"180-day window start: {Color(schengenStart.ToString(CultureInfo.InvariantCulture), "yellow")}");

string[] events = calText.Split("BEGIN:VEVENT");

// Add major Polish cities and London
string[] schengenKeywords =
[
    "POLAND", "BERLIN", "GERMANY", "FRANCE", "SPAIN", "ITALY", "NETHERLANDS", "AUSTRIA", "CZECH", "SWITZERLAND",
    "GREECE", "HUNGARY", "SLOVAKIA", "SLOVENIA", "LUXEMBOURG", "BELGIUM", "DENMARK", "ESTONIA", "FINLAND", "ICELAND",
    "LATVIA", "LITHUANIA", "MALTA", "NORWAY", "PORTUGAL", "SWEDEN",
    "WARSAW", "KRAKOW", "WROCLAW", "GDANSK", "POZNAN", "SZCZECIN", "LODZ", "KATOWICE", "LUBLIN", "BYDGOSZCZ"
];
string[] ukraineKeywords = ["UKRAINE", "KYIV", "LVIV", "ODESA", "KHARKIV"];
string[] ukKeywords = ["LONDON", "UNITED KINGDOM", "ENGLAND"];
string[] airbnbKeywords = ["AIRBNB", "RESERVATION", "BOOKING"];

// Track days in each region
HashSet<DateTime> schengenDays = [];
HashSet<DateTime> ukraineDays = [];
HashSet<DateTime> ukDays = [];
Dictionary<DateTime, string> dayCountry = new();

foreach (string eventString in events)
{
    string[] lines = eventString.Split('\n');
    string summary = lines.FirstOrDefault(l => l.StartsWith("SUMMARY:"))?[8..]?.ToUpper() ?? "";
    // Try to infer country
    string? country = null;
    if (schengenKeywords.Any(k => summary.Contains(k))) country = "SCHENGEN";
    else if (ukraineKeywords.Any(k => summary.Contains(k))) country = "UKRAINE";
    else if (ukKeywords.Any(k => summary.Contains(k))) country = "UK";
    else if (airbnbKeywords.Any(k => summary.Contains(k)))
    {
        foreach (string k in schengenKeywords)
            if (summary.Contains(k))
                country = "SCHENGEN";
        foreach (string k in ukraineKeywords)
            if (summary.Contains(k))
                country = "UKRAINE";
        foreach (string k in ukKeywords)
            if (summary.Contains(k))
                country = "UK";
    }

    if (country == null) continue;

    DateTime start = DateTime.MinValue, end = DateTime.MinValue;
    foreach (string line in lines)
    {
        if (line.StartsWith("DTSTART;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1)..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            start = new DateTime(year, month, day);
        }
        else if (line.StartsWith("DTEND;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1)..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            end = new DateTime(year, month, day);
        }
    }

    if (start == DateTime.MinValue || end == DateTime.MinValue) continue;
    if (end < schengenStart) continue;
    if (start < schengenStart) start = schengenStart;
    for (DateTime d = start; d < end; d = d.AddDays(1))
    {
        switch (country)
        {
            case "SCHENGEN":
                schengenDays.Add(d);
                break;
            case "UKRAINE":
                ukraineDays.Add(d);
                break;
            case "UK":
                ukDays.Add(d);
                break;
        }

        dayCountry.TryAdd(d, country);
    }
}

// Fill in missing days as "OUTSIDE" or last known zone
DateTime schengenWindowStart = allEventDays.Count > 0
    ? allEventDays.Max().Subtract(new TimeSpan(180, 0, 0, 0))
    : DateTime.MinValue;
DateTime lastDay = allEventDays.Count > 0 ? allEventDays.Max() : DateTime.MinValue;
string? lastZone = null;
for (DateTime d = schengenWindowStart; d <= lastDay; d = d.AddDays(1))
{
    if (!dayCountry.TryGetValue(d, out string value))
    {
        value = lastZone ?? "OUTSIDE";
        dayCountry[d] = value;
    }

    lastZone = value;
}

// Rebuild schengenDays, ukraineDays, ukDays based on filled dayCountry
schengenDays = [];
ukraineDays = [];
ukDays = [];
foreach (KeyValuePair<DateTime, string> kv in dayCountry)
{
    switch (kv.Value)
    {
        case "SCHENGEN": schengenDays.Add(kv.Key); break;
        case "UKRAINE": ukraineDays.Add(kv.Key); break;
        case "UK": ukDays.Add(kv.Key); break;
    }
}

// Set today as the last day in the window
today = lastDay;
schengenStart = schengenWindowStart;

// Calculate mandatory Schengen exit date if nothing changes
const int schengenLimit = 90;
DateTime? mustExit = null;
List<DateTime> schengenList = schengenDays.OrderBy(d => d).ToList();

// Project forward if currently in Schengen
string currentZone = dayCountry[today];
if (currentZone == "SCHENGEN")
{
    // Build a rolling window of the last 180 days including today
    HashSet<DateTime> projectedSchengenDays = new(schengenDays.Where(d => d >= schengenStart && d <= today));
    DateTime probe = today;
    int used = projectedSchengenDays.Count;
    while (used < schengenLimit)
    {
        probe = probe.AddDays(1);
        projectedSchengenDays.Add(probe);
        // Remove days outside the 180-day window
        DateTime windowStart = probe.AddDays(-179);
        used = projectedSchengenDays.Count(d => d >= windowStart && d <= probe);
    }
    mustExit = probe;
}

// If currently outside Schengen, calculate how many days can return for
int schengenUsed = schengenDays.Count(d => d >= schengenStart && d <= today);
int schengenLeft = schengenLimit - schengenUsed;

Console.WriteLine($"\n{Color("==== TRAVEL SUMMARY ====", "blue")}");
Console.WriteLine(
    $"Schengen days in window: {Color(schengenUsed.ToString(), schengenUsed > schengenLimit ? "red" : "green")}");
Console.WriteLine(
    $"Ukraine days in window: {Color(ukraineDays.Count(d => d >= schengenStart && d <= today).ToString(), "green")}");
Console.WriteLine(
    $"UK days in window: {Color(ukDays.Count(d => d >= schengenStart && d <= today).ToString(), "green")}");
if (currentZone == "SCHENGEN")
{
    Console.WriteLine(mustExit != null
        ? $"{Color("MANDATORY SCHENGEN EXIT BY:", "red")} {Color(mustExit.Value.ToShortDateString(), "red")}"
        : $"{Color("No mandatory exit required yet.", "green")}");
}
else
{
    Console.WriteLine($"{Color("You are currently outside Schengen.", "yellow")}");
    Console.WriteLine($"{Color($"You can return for {schengenLeft} days.", schengenLeft > 0 ? "green" : "red")}");
}

// Show travel windows
Console.WriteLine($"\n{Color("==== TRAVEL WINDOWS ====", "blue")}");
List<DateTime> allDays = dayCountry.Keys.OrderBy(d => d).ToList();
if (allDays.Count == 0) Console.WriteLine("No travel days detected.");
else
{
    string? lastCountry = null;
    DateTime? windowStart = null;
    foreach (DateTime d in allDays)
    {
        if (lastCountry == null)
        {
            lastCountry = dayCountry[d];
            windowStart = d;
        }
        else if (dayCountry[d] != lastCountry || (windowStart != null && (d - windowStart.Value).Days > 0 &&
                                                  (d - allDays[allDays.IndexOf(d) - 1]).Days > 1))
        {
            Console.WriteLine(
                $"{Color(windowStart!.Value.ToShortDateString(), lastCountry switch
                {
                    "SCHENGEN" => "yellow",
                    "UKRAINE" => "green",
                    "UK" => "blue",
                    _ => "red"
                })} - {Color(allDays[allDays.IndexOf(d) - 1].ToShortDateString(), lastCountry switch
            {
                "SCHENGEN" => "yellow",
                "UKRAINE" => "green",
                "UK" => "blue",
                _ => "red"
            })} : {Color(lastCountry, lastCountry switch
        {
            "SCHENGEN" => "yellow",
            "UKRAINE" => "green",
            "UK" => "blue",
            _ => "red"
        })}");
            lastCountry = dayCountry[d];
            windowStart = d;
        }
    }

    // Print last window if windowStart is not null
    if (windowStart != null && lastCountry != null)
        Console.WriteLine(
            $"{Color(windowStart.Value.ToShortDateString(), lastCountry switch
            {
                "SCHENGEN" => "yellow",
                "UKRAINE" => "green",
                "UK" => "blue",
                _ => "red"
            })} - {Color(allDays.Last().ToShortDateString(), lastCountry switch
        {
            "SCHENGEN" => "yellow",
            "UKRAINE" => "green",
            "UK" => "blue",
            _ => "red"
        })} : {Color(lastCountry, lastCountry switch
    {
        "SCHENGEN" => "yellow",
        "UKRAINE" => "green",
        "UK" => "blue",
        _ => "red"
    })}");
}

return;

string Color(string text, string color) => color switch
{
    "red" => $"\e[31m{text}\e[0m",
    "green" => $"\e[32m{text}\e[0m",
    "yellow" => $"\e[33m{text}\e[0m",
    "blue" => $"\e[34m{text}\e[0m",
    _ => text
};