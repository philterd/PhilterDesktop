/*
 * Copyright 2026 Philterd, LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;
using System.Net;
using System.Text;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>What an optional, more detailed report should include.</summary>
    internal sealed class RedactionReportOptions
    {
        /// <summary>
        /// Include a per-redaction table (type, location, replacement). It deliberately never lists the
        /// original detected text, so even the detailed report contains no PII — that is what the
        /// separate "Export Explanation (JSON)" feature is for.
        /// </summary>
        public bool IncludeDetailTable { get; init; }

        /// <summary>Include the machine and user name (only when the user has logging enabled).</summary>
        public bool IncludeMachineInfo { get; init; }
    }

    /// <summary>
    /// Builds a human-facing "redaction report / certificate" for one redaction: a self-contained
    /// summary (files + SHA-256 hashes, policy/context, tool version, timestamp, and counts of what was
    /// removed by type) suitable for a case file or compliance record.
    ///
    /// Unlike the explanation JSON, the report contains <b>no original (un-redacted) text</b>, so it is
    /// safe to file and share alongside the redacted copy. This class is the UI-independent core: it
    /// projects stored spans into a model and renders HTML; PDF rendering lives in
    /// <see cref="RedactionReportPdf"/>. File hashes are passed in (computed by the caller) so the
    /// builder stays pure and testable.
    /// </summary>
    internal static class RedactionReport
    {
        public const string DraftReminder =
            "Redaction is statistical and may miss items or remove too much. Review the redacted copy " +
            "before relying on or sharing it.";

        /// <summary>Builds the report model from a redaction version and its spans.</summary>
        public static RedactionReportModel Build(
            RedactionVersionEntity version,
            IReadOnlyList<RedactionSpanEntity> spans,
            string toolVersion,
            DateTimeOffset generatedAt,
            string sourceSha256,
            string outputSha256,
            RedactionReportOptions options,
            string verificationStatus = "NotRun",
            int verificationCount = 0,
            DateTime? verificationCheckedAt = null)
        {
            List<RedactionSpanEntity> ordered = spans.OrderBy(s => s.Order).ToList();

            List<(string Type, int Count)> counts = ordered
                .GroupBy(FriendlyType)
                .Select(g => (Type: g.Key, Count: g.Count()))
                .OrderByDescending(t => t.Count)
                .ThenBy(t => t.Type, StringComparer.OrdinalIgnoreCase)
                .ToList();

            List<RedactionReportRow> details = options.IncludeDetailTable
                ? ordered.Select((s, i) => new RedactionReportRow(i + 1, FriendlyType(s), LocationOf(s, version.FileType), s.Replacement)).ToList()
                : new List<RedactionReportRow>();

            return new RedactionReportModel
            {
                ToolVersion = toolVersion,
                GeneratedUtc = generatedAt.UtcDateTime,
                RedactedUtc = DateTime.SpecifyKind(version.CreatedAt, DateTimeKind.Utc),
                SourcePath = version.SourcePath,
                OutputPath = version.OutputPath,
                FileType = version.FileType,
                Policy = version.Policy,
                Context = version.Context,
                Version = version.Version,
                SourceSha256 = sourceSha256,
                OutputSha256 = outputSha256,
                TotalRedactions = ordered.Count,
                CountsByType = counts,
                Details = details,
                MachineName = options.IncludeMachineInfo ? SafeEnv(() => Environment.MachineName) : null,
                UserName = options.IncludeMachineInfo ? SafeEnv(() => Environment.UserName) : null,
                VerificationSummary = VerificationSummaryLine(verificationStatus, verificationCount, verificationCheckedAt)
            };
        }

        // A one-line verification summary for the report, or null when verification hasn't run.
        private static string? VerificationSummaryLine(string status, int count, DateTime? checkedAt)
        {
            string when = checkedAt is { } c ? $" ({FormatUtc(DateTime.SpecifyKind(c, DateTimeKind.Utc))})" : string.Empty;
            return status switch
            {
                "Clean" => $"Verified: no detectable PII remained in the output{when}.",
                "ResidualsFound" => $"Verification warning: {count} possible item{(count == 1 ? "" : "s")} may remain in the output{when}. Review before sharing.",
                "Error" => $"Verification could not be completed{when}.",
                "NamesNotChecked" => $"On-device name detection was unavailable when this was redacted{when}, so person names may remain. Review before sharing.",
                "ContentDropped" => $"Some source content may not have been carried into the output — an RTF's headers/footers/footnotes, or a PDF's annotations/form fields{when}. Review before sharing.",
                _ => null
            };
        }

        /// <summary>Renders the report model as a self-contained HTML document (inline CSS, no assets).</summary>
        public static string ToHtml(RedactionReportModel m)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">");
            sb.Append("<title>Redaction Report</title><style>");
            sb.Append("body{font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a;margin:32px;font-size:14px;}");
            sb.Append("h1{font-size:22px;margin:0 0 4px;}h2{font-size:15px;margin:24px 0 8px;border-bottom:1px solid #ddd;padding-bottom:4px;}");
            sb.Append(".sub{color:#666;margin:0 0 16px;}table{border-collapse:collapse;width:100%;}");
            sb.Append("th,td{text-align:left;padding:6px 10px;border-bottom:1px solid #eee;vertical-align:top;}");
            sb.Append("th{background:#f5f5f5;}.k{color:#666;width:170px;}.mono{font-family:Consolas,monospace;word-break:break-all;}");
            sb.Append(".total{font-size:18px;font-weight:600;}.note{color:#555;margin-top:20px;font-size:13px;}");
            sb.Append("</style></head><body>");

            sb.Append("<h1>Redaction Report</h1>");
            sb.Append($"<p class=\"sub\">Philter Desktop {E(m.ToolVersion)} &middot; generated {E(FormatUtc(m.GeneratedUtc))}</p>");

            sb.Append("<h2>Document</h2><table>");
            Row(sb, "Source file", E(m.SourcePath));
            Row(sb, "Redacted output", E(m.OutputPath));
            Row(sb, "File type", E(m.FileType));
            Row(sb, "Redacted on", E(FormatUtc(m.RedactedUtc)));
            Row(sb, "Policy", E(m.Policy));
            Row(sb, "Context", E(m.Context));
            if (m.Version > 0) Row(sb, "Version", m.Version.ToString(CultureInfo.InvariantCulture));
            if (m.MachineName is not null) Row(sb, "Machine", E(m.MachineName));
            if (m.UserName is not null) Row(sb, "User", E(m.UserName));
            sb.Append("</table>");

            sb.Append("<h2>File integrity (SHA-256)</h2><table>");
            Row(sb, "Source", $"<span class=\"mono\">{E(m.SourceSha256)}</span>");
            Row(sb, "Redacted output", $"<span class=\"mono\">{E(m.OutputSha256)}</span>");
            sb.Append("</table>");

            if (m.VerificationSummary is not null)
            {
                sb.Append("<h2>Verification</h2>");
                sb.Append($"<p>{E(m.VerificationSummary)}</p>");
            }

            sb.Append("<h2>What was removed</h2>");
            sb.Append($"<p class=\"total\">{m.TotalRedactions} redaction{(m.TotalRedactions == 1 ? "" : "s")}</p>");
            if (m.CountsByType.Count > 0)
            {
                sb.Append("<table><tr><th>Type</th><th>Count</th></tr>");
                foreach ((string type, int count) in m.CountsByType)
                {
                    sb.Append($"<tr><td>{E(type)}</td><td>{count}</td></tr>");
                }
                sb.Append("</table>");
            }

            if (m.Details.Count > 0)
            {
                sb.Append("<h2>Redaction detail</h2>");
                sb.Append("<p class=\"sub\">Location and replacement only — the original text is not included.</p>");
                sb.Append("<table><tr><th>#</th><th>Type</th><th>Location</th><th>Replacement</th></tr>");
                foreach (RedactionReportRow r in m.Details)
                {
                    sb.Append($"<tr><td>{r.Index}</td><td>{E(r.Type)}</td><td>{E(r.Location)}</td><td class=\"mono\">{E(r.Replacement)}</td></tr>");
                }
                sb.Append("</table>");
            }

            sb.Append($"<p class=\"note\">{E(DraftReminder)}</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        // --- helpers ---------------------------------------------------------

        private static void Row(StringBuilder sb, string key, string valueHtml) =>
            sb.Append($"<tr><td class=\"k\">{E(key)}</td><td>{valueHtml}</td></tr>");

        private static string E(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        internal static string FormatUtc(DateTime utc) =>
            utc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'", CultureInfo.InvariantCulture);

        // A readable type name: "EMAIL_ADDRESS" -> "Email Address". Falls back to the classification
        // label, then a generic bucket. User-added redactions are grouped on their own.
        internal static string FriendlyType(RedactionSpanEntity s)
        {
            if (s.UserAdded)
            {
                return "User-added";
            }
            string raw = !string.IsNullOrWhiteSpace(s.FilterType) ? s.FilterType
                       : !string.IsNullOrWhiteSpace(s.Classification) ? s.Classification
                       : "Other";
            return Humanize(raw);
        }

        private static string Humanize(string raw)
        {
            string[] words = raw.Replace('_', ' ').Replace('-', ' ')
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (words.Length == 0)
            {
                return raw;
            }
            return string.Join(' ', words.Select(w =>
                char.ToUpperInvariant(w[0]) + (w.Length > 1 ? w.Substring(1).ToLowerInvariant() : string.Empty)));
        }

        // A short location string per file type: PDF -> page; the ParagraphIndex doubles as a spreadsheet
        // cell ordinal (.xlsx/.csv) or an email field ordinal (.eml/.msg), so label it for what it actually
        // is rather than "Section N"; .docx/.txt/.rtf -> section; plain text with no index -> character range.
        internal static string LocationOf(RedactionSpanEntity s, string? fileType = null)
        {
            if (s.PageNumber > 0)
            {
                return $"Page {s.PageNumber}";
            }
            if (s.ParagraphIndex >= 0)
            {
                int n = s.ParagraphIndex + 1;
                return (fileType?.ToLowerInvariant()) switch
                {
                    ".xlsx" or ".csv" => $"Cell {n}",
                    ".eml" or ".msg" => $"Field {n}",
                    _ => $"Section {n}"
                };
            }
            return $"Characters {s.CharacterStart}-{s.CharacterEnd}";
        }

        private static string? SafeEnv(Func<string> read)
        {
            try { return read(); } catch { return null; }
        }
    }

    /// <summary>One row of the optional per-redaction detail table (no original text).</summary>
    internal sealed record RedactionReportRow(int Index, string Type, string Location, string Replacement);

    /// <summary>The data behind a redaction report, ready to render to HTML or PDF.</summary>
    internal sealed class RedactionReportModel
    {
        public string ToolVersion { get; init; } = string.Empty;
        public DateTime GeneratedUtc { get; init; }
        public DateTime RedactedUtc { get; init; }
        public string SourcePath { get; init; } = string.Empty;
        public string OutputPath { get; init; } = string.Empty;
        public string FileType { get; init; } = string.Empty;
        public string Policy { get; init; } = string.Empty;
        public string Context { get; init; } = string.Empty;
        public int Version { get; init; }
        public string SourceSha256 { get; init; } = string.Empty;
        public string OutputSha256 { get; init; } = string.Empty;
        public int TotalRedactions { get; init; }
        public IReadOnlyList<(string Type, int Count)> CountsByType { get; init; } = Array.Empty<(string, int)>();
        public IReadOnlyList<RedactionReportRow> Details { get; init; } = Array.Empty<RedactionReportRow>();
        public string? MachineName { get; init; }
        public string? UserName { get; init; }

        /// <summary>One-line verification summary, or null when verification has not run.</summary>
        public string? VerificationSummary { get; init; }
    }
}
