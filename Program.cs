// label all day events eg POLAND or BERLIN
// download google calendar from Settings > Export
// https://calendar.google.com/calendar/u/0/r/settings/export
// input exit date in format "mm/dd/yyyy hr:min:sec AM/PM"

string calText = File.ReadAllText(args[0]);

DateTime exitDate = DateTime.Parse(args[1]);
DateTime schengenStart = exitDate.Subtract(new TimeSpan(180, 0, 0, 0));
Console.WriteLine($"exit date: {exitDate}");
Console.WriteLine($"schengen start: {schengenStart}");

string[] events = calText.Split("BEGIN:VEVENT");
int totalDays = 0;
foreach (string eventString in events)
{
    if (!eventString.Contains("SUMMARY:POLAND") && !eventString.Contains("SUMMARY:BERLIN")) continue;

    string[] lines = eventString.Split("\n");

    DateTime start = DateTime.Now;
    DateTime end = DateTime.Now;
    foreach (string line in lines)
    {
        if (line.StartsWith("DTSTART;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1) ..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            start = new DateTime(year, month, day);
        }
        else if (line.StartsWith("DTEND;VALUE=DATE:"))
        {
            string date = line[(line.IndexOf(':') + 1) ..];
            int year = int.Parse(date[..4]);
            int month = int.Parse(date.Substring(4, 2));
            int day = int.Parse(date.Substring(6, 2));
            end = new DateTime(year, month, day);
        }
    }

    if (start < schengenStart && end > schengenStart)
    {
        start = schengenStart;
    }
    else if (start < schengenStart && end < schengenStart)
    {
        continue;
    }

    int numDays = (end - start).Days;
    totalDays += numDays;
}

Console.WriteLine($"total schengen days {totalDays}");