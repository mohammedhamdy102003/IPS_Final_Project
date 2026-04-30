using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using IPS_PROJECT.Models;

namespace IPS_PROJECT.Services
{
    public class PdfReportService
    {
        public byte[] GenerateExecutiveReport(List<EVENTS> events, int totalThreats, int benignCount, string range, DateTime? start = null, DateTime? end = null)
        {

            string currentRange = range?.ToLower().Trim() ?? "default";

            string mainTitle = "IPS SECURITY REPORT";
            string periodDescription = "";


            switch (currentRange)
            {
                case "today":
                    mainTitle = "DAILY IPS SECURITY REPORT";
                    periodDescription = $"Data for Today: {DateTime.Today:yyyy-MM-dd}";
                    break;
                case "week":
                    mainTitle = "WEEKLY IPS SECURITY REPORT";
                    periodDescription = $"Last 7 Days: {DateTime.Today.AddDays(-7):yyyy-MM-dd} to {DateTime.Today:yyyy-MM-dd}";
                    break;
                case "month":
                    mainTitle = "MONTHLY IPS SECURITY REPORT";
                    periodDescription = $"Last 30 Days: {DateTime.Today.AddMonths(-1):yyyy-MM-dd} to {DateTime.Today:yyyy-MM-dd}";
                    break;
                case "custom":
                    mainTitle = "CUSTOM RANGE SECURITY REPORT";
                    periodDescription = $"Period: {(start.HasValue ? start.Value.ToString("yyyy-MM-dd") : "N/A")} to {(end.HasValue ? end.Value.ToString("yyyy-MM-dd") : "N/A")}";
                    break;
                default:

                    mainTitle = "IPS GENERAL STATUS REPORT";
                    periodDescription = $"Analysis of {events.Count} Recorded Events";
                    break;
            }

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(mainTitle)
                                .FontSize(20)
                                .SemiBold()
                                .FontColor(Colors.Blue.Medium);

                            col.Item().Text(text =>
                            {
                                text.Span("Report Scope: ").SemiBold();
                                text.Span(periodDescription);
                            });

                            col.Item().Text(text =>
                            {
                                text.Span("Generated On: ").SemiBold().FontSize(9);
                                text.Span($"{DateTime.Now:f}").Italic().FontSize(9);
                            });
                        });

                        row.ConstantItem(100).AlignCenter().Text("CONFIDENTIAL")
                            .FontColor(Colors.Red.Medium)
                            .Bold();
                    });

                    page.Content().PaddingVertical(15).Column(col =>
                    {
                        // Summary Cards
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Padding(5).Background(Colors.Grey.Lighten4).Column(c => {
                                c.Item().AlignCenter().Text("Total Analyzed").FontSize(9);
                                c.Item().AlignCenter().Text(events.Count.ToString()).FontSize(16).Bold();
                            });
                            row.RelativeItem().Padding(5).Background(Colors.Red.Lighten5).Column(c => {
                                c.Item().AlignCenter().Text("Threats Blocked").FontSize(9);
                                c.Item().AlignCenter().Text(totalThreats.ToString()).FontSize(16).Bold().FontColor(Colors.Red.Medium);
                            });
                            row.RelativeItem().Padding(5).Background(Colors.Green.Lighten5).Column(c => {
                                c.Item().AlignCenter().Text("Clean Traffic").FontSize(9);
                                c.Item().AlignCenter().Text(benignCount.ToString()).FontSize(16).Bold().FontColor(Colors.Green.Medium);
                            });
                        });

                        col.Item().PaddingTop(20).Text($"Detailed Logs Analysis ({currentRange.ToUpper()})")
                            .FontSize(14)
                            .SemiBold()
                            .Underline();

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Timestamp");
                                header.Cell().Element(CellStyle).Text("Source IP");
                                header.Cell().Element(CellStyle).Text("Attack Type");
                                header.Cell().Element(CellStyle).Text("Confidence");
                                header.Cell().Element(CellStyle).Text("Status");

                                static IContainer CellStyle(IContainer container) =>
                                    container.DefaultTextStyle(x => x.Bold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                            });

                            foreach (var e in events)
                            {
                                table.Cell().Element(RowStyle).Text(e.Timestamp.ToString("yyyy-MM-dd HH:mm"));
                                table.Cell().Element(RowStyle).Text(e.SourceIp ?? "N/A");
                                table.Cell().Element(RowStyle).Text(e.Prediction ?? "Unknown");
                                table.Cell().Element(RowStyle).Text($"{e.Confidence:0}%");
                                table.Cell().Element(RowStyle).Text(e.Status)
                                     .FontColor(e.Status == "Blocked" ? Colors.Red.Medium : Colors.Green.Medium);

                                static IContainer RowStyle(IContainer container) =>
                                    container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span($" | IPS System v3.6.1 | Filter: {currentRange}");
                    });
                });
            }).GeneratePdf();
        }
    }
}