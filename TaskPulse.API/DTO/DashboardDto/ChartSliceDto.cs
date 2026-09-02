namespace TaskPulse.API.DTO.DashboardDto;

// One bar or slice: a label and its count. Every chart on the dashboard is a
// list of these, so the frontend has one shape to render rather than a
// different payload per chart.
public class ChartSliceDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
