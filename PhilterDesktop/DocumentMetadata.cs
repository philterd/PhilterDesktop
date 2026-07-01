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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>Which hidden information to strip from a redacted Word document.</summary>
    [Flags]
    public enum WordScrubOptions
    {
        None = 0,
        /// <summary>Core/extended/custom document properties (author, company, title, keywords, etc.).</summary>
        Metadata = 1,
        /// <summary>Reviewer comments.</summary>
        Comments = 2,
        /// <summary>Tracked changes (accept all insertions/deletions and drop revision marks).</summary>
        TrackedChanges = 4,
        /// <summary>Hidden text (runs marked vanish).</summary>
        HiddenText = 8
    }

    /// <summary>
    /// Strips hidden information from a redacted Word document so a "redacted" copy doesn't leak through
    /// channels other than the visible body text: document <b>properties</b> (author, company, etc.),
    /// reviewer <b>comments</b>, <b>tracked changes</b>, and <b>hidden text</b>. Each is a classic
    /// information-leak vector, so this is treated as a redaction safety step. Every operation is
    /// defensive — a malformed part must never break the redaction.
    /// </summary>
    internal static class DocumentMetadata
    {
        /// <summary>Builds the Word scrub options from the user's settings.</summary>
        public static WordScrubOptions OptionsFor(SettingsEntity settings)
        {
            WordScrubOptions options = WordScrubOptions.None;
            if (settings.ScrubDocumentMetadata) options |= WordScrubOptions.Metadata;
            if (settings.ScrubWordComments) options |= WordScrubOptions.Comments;
            if (settings.ScrubWordTrackedChanges) options |= WordScrubOptions.TrackedChanges;
            if (settings.ScrubWordHiddenText) options |= WordScrubOptions.HiddenText;
            return options;
        }

        /// <summary>Clears document properties (back-compat overload: metadata only).</summary>
        public static void ScrubDocx(string path) => ScrubDocx(path, WordScrubOptions.Metadata);

        /// <summary>Applies the requested scrubbing to a <c>.docx</c> file in place.</summary>
        public static void ScrubDocx(string path, WordScrubOptions options)
        {
            if (options == WordScrubOptions.None)
            {
                return;
            }

            // Scrub in memory, then overwrite once. Plain write (not SafeOutput): a failure must keep the
            // already-redacted file, not delete it.
            using MemoryStream buffer = SafeOutput.ReadToEditableStream(path);
            using WordprocessingDocument document = WordprocessingDocument.Open(buffer, isEditable: true);

            if (options.HasFlag(WordScrubOptions.Metadata))
            {
                ClearCoreProperties(document);
                ClearExtendedProperties(document.ExtendedFilePropertiesPart);
                RemoveCustomProperties(document, document.CustomFilePropertiesPart);
                RemoveCustomXmlParts(document);
                AnonymizeCommentAuthors(document);
            }
            if (options.HasFlag(WordScrubOptions.TrackedChanges))
            {
                AcceptRevisions(document);
            }
            if (options.HasFlag(WordScrubOptions.Comments))
            {
                RemoveComments(document);
            }
            if (options.HasFlag(WordScrubOptions.HiddenText))
            {
                RemoveHiddenText(document);
            }

            document.Save(); // flush into the buffer
            File.WriteAllBytes(path, buffer.ToArray());
        }

        /// <summary>
        /// Removes identifying document properties (core, extended, custom) from a redacted
        /// <c>.xlsx</c> file in place, so a "redacted" spreadsheet doesn't leak author / company /
        /// last-modified-by through its metadata. This is the spreadsheet counterpart to the metadata
        /// step of <see cref="ScrubDocx(string, WordScrubOptions)" />; it is gated by the same
        /// "Remove document metadata" setting at the call site.
        /// </summary>
        public static void ScrubXlsx(string path)
        {
            try
            {
                // Scrub in memory, then overwrite once (see ScrubDocx).
                using MemoryStream buffer = SafeOutput.ReadToEditableStream(path);
                using (SpreadsheetDocument document = SpreadsheetDocument.Open(buffer, isEditable: true))
                {
                    ClearCoreProperties(document);
                    ClearExtendedProperties(document.ExtendedFilePropertiesPart);
                    RemoveCustomProperties(document, document.CustomFilePropertiesPart);
                    document.Save();
                }
                File.WriteAllBytes(path, buffer.ToArray());
            }
            catch
            {
                // best effort — a metadata quirk must never fail the redaction
            }
        }

        // The document parts that hold body content (and so can carry comments/revisions/hidden text):
        // the main document, headers, footers, footnotes, and endnotes.
        private static IEnumerable<OpenXmlPart> ContentParts(WordprocessingDocument document)
        {
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                yield break;
            }
            yield return main;
            foreach (HeaderPart part in main.HeaderParts) yield return part;
            foreach (FooterPart part in main.FooterParts) yield return part;
            if (main.FootnotesPart is not null) yield return main.FootnotesPart;
            if (main.EndnotesPart is not null) yield return main.EndnotesPart;
        }

        // --- Properties -------------------------------------------------------

        // OOXML0001: PackageProperties is marked experimental in the SDK but is the supported way to edit
        // core document properties; suppress the diagnostic for this well-defined use.
#pragma warning disable OOXML0001
        private static void ClearCoreProperties(OpenXmlPackage package)
        {
            try
            {
                IPackageProperties p = package.PackageProperties;
                p.Creator = null;
                p.Title = null;
                p.Subject = null;
                p.Keywords = null;
                p.Description = null;
                p.LastModifiedBy = null;
                p.Category = null;
                p.ContentStatus = null;
                p.Identifier = null;
                p.Language = null;
                p.Revision = null;
                p.Version = null;
                p.Created = null;
                p.Modified = null;
                p.LastPrinted = null;
            }
            catch
            {
                // best effort — never let a properties quirk fail the redaction
            }
        }
#pragma warning restore OOXML0001

        // Extended (app.xml) properties: remove every field that carries identity or document content —
        // company, manager, a hyperlink base path, the template path (which can reveal a user/share path),
        // and the cached part titles / heading outline (which mirror document text and so can hold PII that
        // was redacted from the body). Structural fields (application name, statistics) are left so the
        // file stays well-formed.
        private static void ClearExtendedProperties(ExtendedFilePropertiesPart? ext)
        {
            try
            {
                DocumentFormat.OpenXml.ExtendedProperties.Properties? p = ext?.Properties;
                if (p is null)
                {
                    return;
                }
                p.Company?.Remove();
                p.Manager?.Remove();
                p.HyperlinkBase?.Remove();
                p.Template?.Remove();
                p.TitlesOfParts?.Remove();
                p.HeadingPairs?.Remove();
                p.Save();
            }
            catch
            {
                // best effort
            }
        }

        // Custom properties (custom.xml): drop the whole part, since these are entirely user/org-defined.
        private static void RemoveCustomProperties(OpenXmlPackage package, CustomFilePropertiesPart? custom)
        {
            try
            {
                if (custom is not null)
                {
                    package.DeletePart(custom);
                }
            }
            catch
            {
                // best effort
            }
        }

        // Custom XML data stores (word/customXml/*): a data-bound content control reads its displayed value
        // from one of these stores, which keeps the ORIGINAL (unredacted) value even after the visible run
        // text is redacted — and Word can refresh the control from it. They're user-data stores, so remove
        // them with the metadata scrub so bound PII can't persist or reappear.
        private static void RemoveCustomXmlParts(WordprocessingDocument document)
        {
            try
            {
                MainDocumentPart? main = document.MainDocumentPart;
                if (main is null)
                {
                    return;
                }
                foreach (CustomXmlPart part in main.GetPartsOfType<CustomXmlPart>().ToList())
                {
                    main.DeletePart(part);
                }
            }
            catch
            {
                // best effort
            }
        }

        // The WordprocessingML namespace. Matching by (namespace, local name) keeps the scrubber robust
        // against the SDK mapping the same element (e.g. w:ins) to different classes by context.
        private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        private static bool Is(OpenXmlElement e, string localName) => e.LocalName == localName && e.NamespaceUri == W;

        // --- Comments ---------------------------------------------------------

        // Comments that are KEPT still carry the reviewer's identity: the w:author / w:initials on each
        // comment and the display names in word/people.xml. Replace authors with consistent per-author
        // pseudonyms ("Reviewer 1", "Reviewer 2", …) so the conversation stays readable without real
        // names, and drop the people part outright (it only stores reviewer identities). Gated by the
        // "remove document metadata" option, the same class of identifying metadata.
        private static void AnonymizeCommentAuthors(WordprocessingDocument document)
        {
            try
            {
                MainDocumentPart? main = document.MainDocumentPart;
                if (main is null)
                {
                    return;
                }

                if (main.WordprocessingCommentsPart?.Comments is { } comments)
                {
                    var aliases = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (Comment comment in comments.Elements<Comment>())
                    {
                        string original = comment.Author?.Value ?? string.Empty;
                        if (!aliases.TryGetValue(original, out int number))
                        {
                            number = aliases.Count + 1;
                            aliases[original] = number;
                        }
                        comment.Author = "Reviewer " + number;
                        comment.Initials = "R" + number;
                    }
                }

                // people.xml holds reviewer display names / presence for @mentions — remove it entirely.
                foreach (WordprocessingPeoplePart part in main.GetPartsOfType<WordprocessingPeoplePart>().ToList())
                {
                    main.DeletePart(part);
                }
            }
            catch
            {
                // best effort — never let a comment-author quirk fail the redaction
            }
        }

        private static void RemoveComments(WordprocessingDocument document)
        {
            try
            {
                foreach (OpenXmlPart part in ContentParts(document))
                {
                    OpenXmlElement? root = part.RootElement;
                    if (root is null)
                    {
                        continue;
                    }
                    foreach (OpenXmlElement marker in root.Descendants()
                                 .Where(e => Is(e, "commentRangeStart") || Is(e, "commentRangeEnd")).ToList())
                    {
                        marker.Remove();
                    }
                    // The little superscript reference lives inside a run; remove that whole run.
                    foreach (OpenXmlElement reference in root.Descendants().Where(e => Is(e, "commentReference")).ToList())
                    {
                        (reference.Ancestors().FirstOrDefault(a => Is(a, "r")) ?? reference).Remove();
                    }
                }

                MainDocumentPart? main = document.MainDocumentPart;
                if (main is null)
                {
                    return;
                }
                // The comment text and author info live in separate parts — delete them outright.
                if (main.WordprocessingCommentsPart is { } comments) main.DeletePart(comments);
                foreach (WordprocessingCommentsExPart part in main.GetPartsOfType<WordprocessingCommentsExPart>().ToList()) main.DeletePart(part);
                foreach (WordprocessingPeoplePart part in main.GetPartsOfType<WordprocessingPeoplePart>().ToList()) main.DeletePart(part);
            }
            catch
            {
                // best effort
            }
        }

        // --- Tracked changes (accept all) -------------------------------------

        private static readonly string[] RevisionMarks =
        {
            "rPrChange", "pPrChange", "tblPrChange", "trPrChange", "tcPrChange", "tblPrExChange",
            "sectPrChange", "numberingChange", "moveFromRangeStart", "moveFromRangeEnd",
            "moveToRangeStart", "moveToRangeEnd"
        };

        private static void AcceptRevisions(WordprocessingDocument document)
        {
            try
            {
                foreach (OpenXmlPart part in ContentParts(document))
                {
                    OpenXmlElement? root = part.RootElement;
                    if (root is null)
                    {
                        continue;
                    }

                    // Deletions / moves-from: the content was removed, so drop the wrapper and content.
                    foreach (OpenXmlElement del in root.Descendants().Where(e => Is(e, "del") || Is(e, "moveFrom")).ToList())
                    {
                        del.Remove();
                    }

                    // Insertions / moves-to: keep the content, drop the revision wrapper.
                    Unwrap(root.Descendants().Where(e => Is(e, "ins") || Is(e, "moveTo")).ToList());

                    // Property-change records and range markers carry author/date metadata — remove them.
                    foreach (OpenXmlElement mark in root.Descendants().Where(e => RevisionMarks.Any(m => Is(e, m))).ToList())
                    {
                        mark.Remove();
                    }
                }
            }
            catch
            {
                // best effort
            }
        }

        // --- Hidden text ------------------------------------------------------

        private static void RemoveHiddenText(WordprocessingDocument document)
        {
            try
            {
                foreach (OpenXmlPart part in ContentParts(document))
                {
                    OpenXmlElement? root = part.RootElement;
                    if (root is null)
                    {
                        continue;
                    }
                    foreach (OpenXmlElement run in root.Descendants().Where(e => Is(e, "r")).ToList())
                    {
                        OpenXmlElement? rPr = run.ChildElements.FirstOrDefault(c => Is(c, "rPr"));
                        OpenXmlElement? vanish = rPr?.ChildElements.FirstOrDefault(c => Is(c, "vanish"));
                        if (vanish is not null && !IsExplicitlyOff(vanish))
                        {
                            run.Remove();
                        }
                    }
                }
            }
            catch
            {
                // best effort
            }
        }

        // A w:vanish with w:val="0"/"false" is turned off; otherwise (present, or val="1"/"true") it hides.
        private static bool IsExplicitlyOff(OpenXmlElement vanish) =>
            vanish.GetAttributes().Any(a => a.LocalName == "val" && (a.Value is "0" or "false"));

        // --- helpers ----------------------------------------------------------

        // Replaces each element with its children in place (keeping the content, dropping the wrapper).
        private static void Unwrap(IReadOnlyList<OpenXmlElement> elements)
        {
            foreach (OpenXmlElement element in elements)
            {
                OpenXmlElement? parent = element.Parent;
                if (parent is null)
                {
                    continue;
                }
                foreach (OpenXmlElement child in element.ChildElements.ToList())
                {
                    child.Remove();
                    parent.InsertBefore(child, element);
                }
                element.Remove();
            }
        }
    }
}
