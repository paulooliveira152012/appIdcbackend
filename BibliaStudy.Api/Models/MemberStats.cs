namespace BibliaStudy.Api.Models;

public class Stats
{
    public Guid Id { get; set; }
    public string EbdAttendance { get; set; } = "";
    public string EbdPoints { get; set; } = "";
    public string CelulaAttendance { get; set; } = "";
    public string CelulaPoints { get; set; } = "";
    public string SundayServiceAttendance { get; set; } = "";
    public string PrayerServiceAttendance { get; set; } = "";
    public string StudySericeAttendance { get; set; } = "";
    public string VisiorsService { get; set; } = "";
    public string VisitorsPoints { get; set; } = "";
}