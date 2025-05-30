﻿using System.Globalization;
using day_calculator;

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
    string summary = lines.FirstOrDefault(l => l.StartsWith("SUMMARY:"))?[8..]?.ToUpperInvariant().Trim() ?? "";
    string location = lines.FirstOrDefault(l => l.StartsWith("LOCATION:"))?[9..]?.ToUpperInvariant().Trim() ?? "";

    // Special case: disregard "ukraine torch"
    if (summary.Contains("UKRAINE TORCH") || summary.Contains("UKRAINE WAR ROOM")|| summary.Contains("UKRAINE DISCUSSION"))
        continue;

    string? zone = null;

    // Detect zone by keywords (whole word match)
    foreach (var z in zones)
        if (z.Keywords.Any(k => ContainsWholeWord(summary, k) || ContainsWholeWord(location, k)))
            zone = z.Name;

    // Detect by airport code in summary or location (whole word match)
    foreach (var code in Airports.AirportCodes)
        if (ContainsWholeWord(summary, code) || ContainsWholeWord(location, code))
            zone = Airports.AirportToZone[code];

    // Detect travel: "Location > Location" or "Location -> Location"
    if (location.Contains(">") || location.Contains("->"))
    {
        string[] parts = location.Replace("->", ">").Split('>');
        if (parts.Length == 2)
        {
            string to = parts[1].Trim();
            string? toZone = null;
            foreach (var z in zones)
            {
                if (z.Keywords.Any(k => ContainsWholeWord(to, k))) toZone = z.Name;
            }
            foreach (var code in Airports.AirportCodes)
            {
                if (ContainsWholeWord(to, code)) toZone = Airports.AirportToZone[code];
            }
            // If travel detected, prefer destination zone
            if (toZone != null)
                zone = toZone;
        }
    }

    if (zone == null && airbnbKeywords.Any(k => ContainsWholeWord(summary, k) || ContainsWholeWord(location, k)))
        foreach (var z in zones)
            if (z.Keywords.Any(k => ContainsWholeWord(summary, k) || ContainsWholeWord(location, k)))
                zone = z.Name;
    if (zone == null) continue;

    DateTime start = DateTime.MinValue, end = DateTime.MinValue;
    bool allDay = false;
    foreach (string line in lines)
    {
        if (line.StartsWith("DTSTART;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1)..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            start = new DateTime(year, month, day);
            allDay = true;
        }
        else if (line.StartsWith("DTEND;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1)..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            end = new DateTime(year, month, day);
        }
        else if (line.StartsWith("DTSTART:"))
        {
            // Not all-day event, parse as UTC or local
            string date = line[(line.IndexOf(':') + 1)..].Trim();
            if (date.Length >= 8)
            {
                int year = int.Parse(date[..4]);
                int month = int.Parse(date.Substring(4, 2));
                int day = int.Parse(date.Substring(6, 2));
                int hour = date.Length >= 11 ? int.Parse(date.Substring(9, 2)) : 0;
                int min = date.Length >= 13 ? int.Parse(date.Substring(11, 2)) : 0;
                start = new DateTime(year, month, day, hour, min, 0);
                allDay = false;
            }
        }
        else if (line.StartsWith("DTEND:"))
        {
            string date = line[(line.IndexOf(':') + 1)..].Trim();
            if (date.Length >= 8)
            {
                int year = int.Parse(date[..4]);
                int month = int.Parse(date.Substring(4, 2));
                int day = int.Parse(date.Substring(6, 2));
                int hour = date.Length >= 11 ? int.Parse(date.Substring(9, 2)) : 0;
                int min = date.Length >= 13 ? int.Parse(date.Substring(11, 2)) : 0;
                end = new DateTime(year, month, day, hour, min, 0);
            }
        }
    }
    if (start == DateTime.MinValue || end == DateTime.MinValue) continue;

    // For non-all-day events, treat as single day for zone marking
    if (!allDay)
    {
        DateTime day = start.Date;
        eventRanges.Add((day, day.AddDays(1), zone));
    }
    else
    {
        eventRanges.Add((start, end, zone));
    }

    continue;

    // Helper for whole word match
    bool ContainsWholeWord(string haystack, string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return false;
        var tokens = haystack.Split([' ', ',', ';', '.', '-', '/', '\\', '>', '<', '(', ')', '[', ']', ':', '|', '\t', '\r', '\n'
        ], StringSplitOptions.RemoveEmptyEntries);
        return tokens.Contains(word);
    }
}

// Sort events by start date
eventRanges.Sort((a, b) => a.start.CompareTo(b.start));

// Fill timeline: assume zone changes whenever an event (all-day or timed) starts with a zone
DateTime timelineStart = eventRanges.Count > 0 ? eventRanges.Min(e => e.start) : DateTime.MinValue;
DateTime timelineEnd = eventRanges.Count > 0 ? eventRanges.Max(e => e.end.AddDays(-1)) : DateTime.MinValue;
if (timelineStart == DateTime.MinValue || timelineEnd == DateTime.MinValue)
{
    Console.WriteLine("No events found.");
    return;
}

DateTime d = timelineStart;
int idx = 0;
string? currentZone = null;
while (d <= timelineEnd)
{
    // If today is the start of any event, change zone to that event's zone
    bool changed = false;
    while (idx < eventRanges.Count && d == eventRanges[idx].start)
    {
        currentZone = eventRanges[idx].zone;
        changed = true;
        // If multiple events start on the same day, prefer the last one (arbitrary, but deterministic)
        idx++;
    }
    // If today is within an event, but not the start, keep currentZone
    // If not in any event, keep previous zone

    // Assign zone for today
    if (currentZone != null)
        dayZone[d] = currentZone;
    else
        dayZone[d] = "OUTSIDE";

    // If we've passed the end of the current event, move idx forward
    while (idx < eventRanges.Count && d >= eventRanges[idx].end.AddDays(-1))
        idx++;
    d = d.AddDays(1);
}

// Print summary
Console.WriteLine($"Last Event Date: {Color(timelineEnd.ToString(CultureInfo.InvariantCulture), "yellow")}");
foreach (var z in zones)
{
    int used = ZoneDaysInWindow(z.Name, z.Window);
    Console.WriteLine($"{z.Name} days in window: {Color(used.ToString(), used > z.Limit ? "red" : "green")}");
}

// Print mandatory exit/return info
string printCurrentZone = dayZone[timelineEnd];
foreach (var z in zones)
{
    if (printCurrentZone == z.Name)
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

        // Project forward from today, counting only the current streak
        while (used < z.Limit)
        {
            probe = probe.AddDays(1);
            // Remove days outside the window
            int streakDays = (int)(probe - streakStart).TotalDays + 1;
            used = projected.Count(d => d < streakStart) + streakDays;
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
    DateTime windowBreak = timelineEnd.AddDays(-180 + 1);

    foreach (DateTime day in allDays)
    {
        if (lastCountry == null)
        {
            lastCountry = dayZone[day];
            start = day;
        }
        // Insert 180 window break if it falls inside this window
        else if (dayZone[day] != lastCountry || day == windowBreak)
        {
            DateTime windowEnd = (day == windowBreak) ? day.AddDays(-1) : day.AddDays(-1);
            Console.WriteLine(
                $"{Color(start!.Value.ToShortDateString(), lastCountry switch
                {
                    "SCHENGEN" => "yellow",
                    "UKRAINE" => "green",
                    "UK" => "blue",
                    _ => "red"
                })} - {Color(windowEnd.ToShortDateString(), lastCountry switch
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

            // Print the break if at the 180 window
            if (day == windowBreak)
                Console.WriteLine("-------180 Window------");

            // Start new window
            lastCountry = dayZone[day];
            start = day;
        }
    }
    // Print last window if needed
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

string Color(string text, string color) => color switch
{
    "red" => $"\e[31m{text}\e[0m",
    "green" => $"\e[32m{text}\e[0m",
    "yellow" => $"\e[33m{text}\e[0m",
    "blue" => $"\e[34m{text}\e[0m",
    _ => text
};