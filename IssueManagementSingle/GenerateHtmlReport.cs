using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using UiPath.CodedWorkflows;

namespace IssueManagement
{
    public class GenerateHtmlReport : CodedWorkflow
    {
        [Workflow]
        public string Execute(
            DataTable in_ReportData,
            string in_OutputPath = "")
        {
            if (string.IsNullOrEmpty(in_OutputPath))
            {
                in_OutputPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ExecutionReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");
            }

            int totalWorkflows = in_ReportData.Rows.Count;
            int passed = in_ReportData.AsEnumerable()
                .Count(r => r.Field<string>("Status") == "Passed");
            int failed = totalWorkflows - passed;
            double passRate = totalWorkflows > 0 ? Math.Round((double)passed / totalWorkflows * 100, 1) : 0;

            TimeSpan totalDuration = TimeSpan.Zero;
            foreach (DataRow row in in_ReportData.Rows)
            {
                if (TimeSpan.TryParse(row["Duration"]?.ToString(), out TimeSpan dur))
                {
                    totalDuration += dur;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"UTF-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("<title>UiPath Execution Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine(@"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #0f172a; color: #e2e8f0; padding: 40px; }
                .container { max-width: 1100px; margin: 0 auto; }
                .header { text-align: center; margin-bottom: 40px; }
                .header h1 { font-size: 2.2em; color: #f8fafc; margin-bottom: 8px; }
                .header .subtitle { color: #94a3b8; font-size: 1em; }
                .header .project-name { color: #38bdf8; font-weight: 600; }
                .summary-cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin-bottom: 40px; }
                .card { background: #1e293b; border-radius: 12px; padding: 24px; text-align: center; border: 1px solid #334155; transition: transform 0.2s; }
                .card:hover { transform: translateY(-2px); }
                .card .value { font-size: 2.5em; font-weight: 700; margin-bottom: 4px; }
                .card .label { color: #94a3b8; font-size: 0.9em; text-transform: uppercase; letter-spacing: 1px; }
                .card.total .value { color: #38bdf8; }
                .card.passed .value { color: #4ade80; }
                .card.failed .value { color: #f87171; }
                .card.rate .value { color: #fbbf24; }
                .card.duration .value { color: #c084fc; font-size: 1.6em; }
                .progress-bar-container { background: #334155; border-radius: 999px; height: 12px; margin: 30px 0; overflow: hidden; }
                .progress-bar { height: 100%; border-radius: 999px; transition: width 0.5s; }
                .progress-bar.green { background: linear-gradient(90deg, #22c55e, #4ade80); }
                .progress-bar.red { background: linear-gradient(90deg, #ef4444, #f87171); }
                .progress-bar.yellow { background: linear-gradient(90deg, #eab308, #fbbf24); }
                .results-table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                .results-table thead th { background: #1e293b; color: #94a3b8; padding: 14px 18px; text-align: left; font-weight: 600; text-transform: uppercase; font-size: 0.8em; letter-spacing: 1px; border-bottom: 2px solid #334155; }
                .results-table tbody tr { border-bottom: 1px solid #1e293b; transition: background 0.2s; }
                .results-table tbody tr:hover { background: #1e293b; }
                .results-table td { padding: 14px 18px; }
                .badge { display: inline-block; padding: 4px 14px; border-radius: 999px; font-size: 0.85em; font-weight: 600; }
                .badge.passed { background: rgba(74, 222, 128, 0.15); color: #4ade80; }
                .badge.failed { background: rgba(248, 113, 113, 0.15); color: #f87171; }
                .error-msg { color: #f87171; font-size: 0.85em; max-width: 300px; word-wrap: break-word; }
                .workflow-name { font-weight: 600; color: #f1f5f9; }
                .time-col { color: #94a3b8; font-size: 0.9em; }
                .section-title { font-size: 1.3em; color: #f8fafc; margin-bottom: 16px; padding-bottom: 8px; border-bottom: 2px solid #334155; }
                .footer { text-align: center; margin-top: 40px; color: #475569; font-size: 0.85em; }
                .footer a { color: #38bdf8; text-decoration: none; }
                @media (max-width: 768px) {
                    body { padding: 16px; }
                    .summary-cards { grid-template-columns: repeat(2, 1fr); }
                    .card .value { font-size: 1.8em; }
                }
            ");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class=\"container\">");

            // Header
            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1>&#9889; Execution Report</h1>");
            sb.AppendLine($"<p class=\"subtitle\">Project: <span class=\"project-name\">IssueManagement</span> &nbsp;|&nbsp; {DateTime.Now:MMMM dd, yyyy h:mm tt}</p>");
            sb.AppendLine("</div>");

            // Summary Cards
            sb.AppendLine("<div class=\"summary-cards\">");
            sb.AppendLine($"<div class=\"card total\"><div class=\"value\">{totalWorkflows}</div><div class=\"label\">Total Workflows</div></div>");
            sb.AppendLine($"<div class=\"card passed\"><div class=\"value\">{passed}</div><div class=\"label\">Passed</div></div>");
            sb.AppendLine($"<div class=\"card failed\"><div class=\"value\">{failed}</div><div class=\"label\">Failed</div></div>");
            sb.AppendLine($"<div class=\"card rate\"><div class=\"value\">{passRate}%</div><div class=\"label\">Pass Rate</div></div>");
            sb.AppendLine($"<div class=\"card duration\"><div class=\"value\">{totalDuration:hh\\:mm\\:ss}</div><div class=\"label\">Total Duration</div></div>");
            sb.AppendLine("</div>");

            // Progress Bar
            string barClass = passRate == 100 ? "green" : passRate >= 50 ? "yellow" : "red";
            sb.AppendLine("<div class=\"progress-bar-container\">");
            sb.AppendLine($"<div class=\"progress-bar {barClass}\" style=\"width: {passRate}%\"></div>");
            sb.AppendLine("</div>");

            // Results Table
            sb.AppendLine("<h2 class=\"section-title\">&#128203; Workflow Results</h2>");
            sb.AppendLine("<table class=\"results-table\">");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>#</th><th>Workflow</th><th>Status</th><th>Start Time</th><th>End Time</th><th>Duration</th><th>Error</th>");
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");

            int index = 1;
            foreach (DataRow row in in_ReportData.Rows)
            {
                string name = row["WorkflowName"]?.ToString() ?? "Unknown";
                string status = row["Status"]?.ToString() ?? "Unknown";
                string startTime = row["StartTime"]?.ToString() ?? "-";
                string endTime = row["EndTime"]?.ToString() ?? "-";
                string duration = row["Duration"]?.ToString() ?? "-";
                string error = row["ErrorMessage"]?.ToString() ?? "";

                string badgeClass = status == "Passed" ? "passed" : "failed";
                string errorHtml = string.IsNullOrEmpty(error) ? "<span style=\"color:#475569;\">-</span>" : $"<span class=\"error-msg\">{System.Net.WebUtility.HtmlEncode(error)}</span>";

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td style=\"color:#64748b;\">{index}</td>");
                sb.AppendLine($"<td class=\"workflow-name\">{System.Net.WebUtility.HtmlEncode(name)}</td>");
                sb.AppendLine($"<td><span class=\"badge {badgeClass}\">{status}</span></td>");
                sb.AppendLine($"<td class=\"time-col\">{startTime}</td>");
                sb.AppendLine($"<td class=\"time-col\">{endTime}</td>");
                sb.AppendLine($"<td class=\"time-col\">{duration}</td>");
                sb.AppendLine($"<td>{errorHtml}</td>");
                sb.AppendLine("</tr>");
                index++;
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            // Footer
            sb.AppendLine("<div class=\"footer\">");
            sb.AppendLine($"<p>Generated by <a href=\"#\">UiPath Automation</a> &nbsp;|&nbsp; {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            string directory = Path.GetDirectoryName(in_OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(in_OutputPath, sb.ToString(), Encoding.UTF8);
            Log($"Execution report generated: {in_OutputPath}");

            return in_OutputPath;
        }
    }
}
