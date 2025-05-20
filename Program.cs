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

// Define zone configs
var zones = new[]
{
    new { Name = "SCHENGEN", Keywords = new[] {
        "POLAND", "BERLIN", "GERMANY", "FRANCE", "SPAIN", "ITALY", "NETHERLANDS", "AUSTRIA", "CZECH", "SWITZERLAND",
        "GREECE", "HUNGARY", "SLOVAKIA", "SLOVENIA", "LUXEMBOURG", "BELGIUM", "DENMARK", "ESTONIA", "FINLAND", "ICELAND",
        "LATVIA", "LITHUANIA", "MALTA", "NORWAY", "PORTUGAL", "SWEDEN",
        "WARSAW", "KRAKOW", "WROCLAW", "GDANSK", "POZNAN", "SZCZECIN", "LODZ", "KATOWICE", "LUBLIN", "BYDGOSZCZ"
    }, Limit = 90, Window = 180 },
    new { Name = "UKRAINE", Keywords = new[] { "UKRAINE", "KYIV", "LVIV", "ODESA", "KHARKIV" }, Limit = 90, Window = 180 },
    new { Name = "UK", Keywords = new[] { "LONDON", "UNITED KINGDOM", "ENGLAND" }, Limit = 183, Window = 365 } // 6 months
};
string[] airbnbKeywords = ["AIRBNB", "RESERVATION", "BOOKING"];

// Parse events and assign days to zones
Dictionary<DateTime, string> dayZone = new();
string[] events = calText.Split("BEGIN:VEVENT");
List<(DateTime start, DateTime end, string zone)> eventRanges = [];

foreach (string eventString in events)
{
    string[] lines = eventString.Split('\n');
    string summary = lines.FirstOrDefault(l => l.StartsWith("SUMMARY:"))?[8..]?.ToUpper() ?? "";
    string? zone = null;
    foreach (var z in zones)
        if (z.Keywords.Any(k => summary.Contains(k)))
            zone = z.Name;
    if (zone == null && airbnbKeywords.Any(k => summary.Contains(k)))
        foreach (var z in zones)
            if (z.Keywords.Any(k => summary.Contains(k)))
                zone = z.Name;
    if (zone == null) continue;

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
    eventRanges.Add((start, end, zone));
}

// Sort events by start date
eventRanges.Sort((a, b) => a.start.CompareTo(b.start));

// Fill timeline with continuous zones or OUTSIDE
DateTime timelineStart = eventRanges.Count > 0 ? eventRanges.Min(e => e.start) : DateTime.MinValue;
DateTime timelineEnd = eventRanges.Count > 0 ? eventRanges.Max(e => e.end.AddDays(-1)) : DateTime.MinValue;
if (timelineStart == DateTime.MinValue || timelineEnd == DateTime.MinValue)
{
    Console.WriteLine("No events found.");
    return;
}

DateTime d = timelineStart;
int idx = 0;
while (d <= timelineEnd)
{
    if (idx < eventRanges.Count && d >= eventRanges[idx].start && d < eventRanges[idx].end)
    {
        dayZone[d] = eventRanges[idx].zone;
    }
    else
    {
        // If between events, fill with OUTSIDE or last zone if events are contiguous
        // If d == end of previous event and d == start of next event, use next event's zone
        bool isGap = true;
        if (idx > 0 && d == eventRanges[idx - 1].end && idx < eventRanges.Count && d == eventRanges[idx].start)
        {
            dayZone[d] = eventRanges[idx].zone;
            isGap = false;
        }
        if (isGap)
            dayZone[d] = "OUTSIDE";
    }
    // Move to next event if needed
    if (idx < eventRanges.Count && d >= eventRanges[idx].end.AddDays(-1))
        idx++;
    d = d.AddDays(1);
}

// Print summary
Console.WriteLine($"today: {Color(timelineEnd.ToString(CultureInfo.InvariantCulture), "yellow")}");
foreach (var z in zones)
{
    int used = ZoneDaysInWindow(z.Name, z.Window);
    Console.WriteLine($"{z.Name} days in window: {Color(used.ToString(), used > z.Limit ? "red" : "green")}");
}

// Print mandatory exit/return info
string currentZone = dayZone[timelineEnd];
foreach (var z in zones)
{
    if (currentZone == z.Name)
    {
        // Only consider the most recent continuous stay in this zone up to today
        // Find the start of the current zone streak
        DateTime streakStart = timelineEnd;
        while (streakStart > timelineStart && dayZone.ContainsKey(streakStart.AddDays(-1)) && dayZone[streakStart.AddDays(-1)] == z.Name)
            streakStart = streakStart.AddDays(-1);

        // Build a set of all zone days in the window, but only count the current streak forward
        HashSet<DateTime> projected = new(dayZone
            .Where(kv => kv.Value == z.Name && kv.Key >= timelineEnd.AddDays(-z.Window + 1) && kv.Key < streakStart)
            .Select(kv => kv.Key));
        DateTime probe = timelineEnd;
        int used = projected.Count + (int)(probe - streakStart).TotalDays + 1;
        DateTime winStart = probe.AddDays(-z.Window + 1);

        // Project forward from today, counting only the current streak
        while (used < z.Limit)
        {
            probe = probe.AddDays(1);
            // Remove days outside the window
            winStart = probe.AddDays(-z.Window + 1);
            int streakDays = (int)(probe - streakStart).TotalDays + 1;
            used = projected.Count(d => d >= winStart && d < streakStart) + streakDays;
        }
        Console.WriteLine($"{Color($"MANDATORY {z.Name} EXIT BY:", "red")} {Color(probe.ToShortDateString(), "red")}");
    }
    else if (z.Name == "UK")
    {
        int used = ZoneDaysInWindow("UK", z.Window);
        if (used >= z.Limit)
        {
            DateTime? nextEntry = GetUkNextEntry(z.Limit, z.Window);
            if (nextEntry != null && nextEntry > timelineEnd)
                Console.WriteLine($"{Color("You can return to UK on:", "yellow")} {Color(nextEntry.Value.ToShortDateString(), "yellow")}");
        }
        else
        {
            Console.WriteLine($"{Color($"You can return to UK for {z.Limit - used} days.", "blue")}");
        }
    }
    else
    {
        int used = ZoneDaysInWindow(z.Name, z.Window);
        Console.WriteLine($"{Color($"You can return to {z.Name} for {z.Limit - used} days.", "yellow")}");
    }
}

// Print travel windows
Console.WriteLine($"\n{Color("==== TRAVEL WINDOWS ====", "blue")}");
List<DateTime> allDays = dayZone.Keys.OrderBy(d => d).ToList();
if (allDays.Count == 0) Console.WriteLine("No travel days detected.");
else
{
    string? lastCountry = null;
    DateTime? start = null;
    foreach (DateTime day in allDays)
    {
        if (lastCountry == null)
        {
            lastCountry = dayZone[day];
            start = day;
        }
        else if (dayZone[day] != lastCountry)
        {
            Console.WriteLine(
                $"{Color(start!.Value.ToShortDateString(), lastCountry switch
                {
                    "SCHENGEN" => "yellow",
                    "UKRAINE" => "green",
                    "UK" => "blue",
                    _ => "red"
                })} - {Color(day.AddDays(-1).ToShortDateString(), lastCountry switch
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
            lastCountry = dayZone[day];
            start = day;
        }
    }
    if (start != null && lastCountry != null)
        Console.WriteLine(
            $"{Color(start.Value.ToShortDateString(), lastCountry switch
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

// Helper: for UK, reset to 0 after leaving
DateTime? GetUkNextEntry(int limit, int window)
{
    // Find last day in UK before today
    DateTime? lastUkDay = null;
    for (DateTime d = timelineEnd; d >= timelineStart; d = d.AddDays(-1))
    {
        if (dayZone[d] != "UK") continue;
        lastUkDay = d;
        break;
    }
    if (lastUkDay == null) return null;
    // Find first day after lastUKDay that is not UK
    DateTime? firstOut = null;
    for (DateTime d = lastUkDay.Value.AddDays(1); d <= timelineEnd; d = d.AddDays(1))
    {
        if (dayZone[d] == "UK") continue;
        firstOut = d;
        break;
    }

    // After leaving, can return for a new 183-day period
    return firstOut ?? null;
}

// Helper: get zone days in window
int ZoneDaysInWindow(string zone, int windowDays)
{
    DateTime start = timelineEnd.AddDays(-windowDays + 1);
    return dayZone.Count(kv => kv.Value == zone && kv.Key >= start && kv.Key <= timelineEnd);
}

// Helper: get next mandatory exit for 90/180 or 183/365 rule
DateTime? GetMandatoryExit(string zone, int limit, int window)
{
    List<DateTime> days = dayZone.Where(kv => kv.Value == zone).Select(kv => kv.Key).OrderBy(d => d).ToList();
    if (days.Count == 0) return null;
    // Only consider up to today
    days = days.Where(d => d <= timelineEnd).ToList();
    for (int i = 0; i < days.Count; i++)
    {
        int count = days.Skip(i).Take(window).Count();
        if (count > limit)
            return days[i + limit];
    }
    // If currently in zone, project forward
    if (dayZone[timelineEnd] != zone) return null;
    {
        HashSet<DateTime> projected = new(days.Where(d => d >= timelineEnd.AddDays(-window + 1) && d <= timelineEnd));
        DateTime probe = timelineEnd;
        int used = projected.Count;
        while (used < limit)
        {
            probe = probe.AddDays(1);
            projected.Add(probe);
            DateTime winStart = probe.AddDays(-window + 1);
            used = projected.Count(d => d >= winStart && d <= probe);
        }
        return probe;
    }
}

string Color(string text, string color) => color switch
{
    "red" => $"\e[31m{text}\e[0m",
    "green" => $"\e[32m{text}\e[0m",
    "yellow" => $"\e[33m{text}\e[0m",
    "blue" => $"\e[34m{text}\e[0m",
    _ => text
};