using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

static class Program
{
    static int Main(string[] args)
    {
        var rootDir = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var files = Directory.EnumerateFiles(rootDir, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                     && !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar));

        int fixedFiles = 0, fixedComments = 0;
        foreach (var path in files)
        {
            var original = File.ReadAllText(path);
            var tree = CSharpSyntaxTree.ParseText(original, new CSharpParseOptions(LanguageVersion.Preview));
            var root = tree.GetRoot();

            // Collect comment spans
            var spans = new List<TextSpan>();
            foreach (var token in root.DescendantTokens(descendIntoTrivia: true))
            {
                CollectTrivia(token.LeadingTrivia, spans);
                CollectTrivia(token.TrailingTrivia, spans);
            }
            if (spans.Count == 0)
                continue;

            // Merge overlapping spans
            spans.Sort((a,b) => a.Start.CompareTo(b.Start));
            var merged = new List<TextSpan>();
            foreach (var s in spans)
            {
                if (merged.Count == 0 || s.Start > merged[^1].End)
                    merged.Add(s);
                else
                    merged[^1] = TextSpan.FromBounds(merged[^1].Start, Math.Max(merged[^1].End, s.End));
            }

            var sb = new StringBuilder(original.Length + 128);
            int pos = 0;
            int localFixes = 0;
            foreach (var s in merged)
            {
                if (s.Start > pos)
                    sb.Append(original, pos, s.Start - pos);
                var segment = original.Substring(s.Start, s.Length);
                var fixedSeg = FixMojibake(segment);
                if (!ReferenceEquals(segment, fixedSeg) && !segment.Equals(fixedSeg, StringComparison.Ordinal))
                    localFixes++;
                sb.Append(fixedSeg);
                pos = s.End;
            }
            if (pos < original.Length)
                sb.Append(original, pos, original.Length - pos);

            var updated = sb.ToString();
            if (!updated.Equals(original, StringComparison.Ordinal))
            {
                File.WriteAllText(path, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                fixedFiles++;
                fixedComments += localFixes;
                Console.WriteLine($"fixed: {path}");
            }
        }
        Console.WriteLine($"Summary: files={fixedFiles}, comment_segments={fixedComments}");
        return 0;
    }

    static void CollectTrivia(SyntaxTriviaList triviaList, List<TextSpan> spans)
    {
        foreach (var t in triviaList)
        {
            if (t.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
                t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                spans.Add(t.Span);
            }
            else if (t.HasStructure)
            {
                // For structured trivia (xml docs), still add the span to treat as plain text replacement
                spans.Add(t.Span);
            }
        }
    }

    static string FixMojibake(string s)
    {
        // Replacements are intentionally ordered to handle common sequences first
        return s
            // Punctuation / quotes
            .Replace("â€”", "—")
            .Replace("â€“", "–")
            .Replace("â€¦", "…")
            .Replace("â€ś", "“")
            .Replace("â€ť", "”")
            .Replace("â€ž", "„")
            .Replace("â€š", "‚")
            .Replace("Â",   "")
            // Latin letters with diacritics (Croatian)
            .Replace("ÄŤ", "č").Replace("Ä", "č")
            .Replace("Ä‡", "ć").Replace("Ä", "ć")
            .Replace("Ä†", "Ć")
            .Replace("Ä‘", "đ").Replace("Ä", "Đ")
            .Replace("Å¡", "š").Replace("Å ", "Š")
            .Replace("Å¾", "ž").Replace("Å½", "Ž")
            // Frequent word stems
            .Replace("UreÄ‘", "Uređ")
            .Replace("AĹ¾", "Až")
            .Replace("GreĹ", "Greš")
            .Replace("UÄŤ", "Uč")
            .Replace("PoÄ", "Poč")
            .Replace("ZavrĹ", "Zavr")
            .Replace("BiljeĹ", "Bilješ")
            .Replace("DobavljaÄ", "Dobavljač")
            .Replace("skladiĹ", "skladiš")
            .Replace("pridruĹ¾", "pridruž")
            .Replace("KritiÄ", "Kriti")
            .Replace("IoT ure", "IoT uređ")
            .Replace("ProizvoÄ‘", "Proizvođa");
    }
}
