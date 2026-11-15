using System.Text.Json;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;

namespace SauceDemo.Tests.Fixtures;

public sealed record AccessibilityViolation(
    string RuleId,
    string Impact,
    string Help,
    string HelpUrl,
    IReadOnlyList<string> Targets);

/// <summary>
/// Corre o axe-core na página e devolve as violações WCAG. Cada violação é escrita no output do teste
/// (linha <c>A11Y</c>) e o relatório completo fica anexado ao teste em JSON.
/// </summary>
public static class AccessibilityScanner
{
    /// <summary>
    /// Regras WCAG 2.0, 2.1 e 2.2, níveis A e AA. As regras "best-practice" do axe ficam de fora:
    /// são boas recomendações, mas não são requisitos WCAG e dariam um gate demasiado ruidoso.
    /// </summary>
    public static readonly IReadOnlyList<string> WcagTags = ["wcag2a", "wcag2aa", "wcag21a", "wcag21aa", "wcag22aa"];

    private static readonly JsonSerializerOptions ReportJson = new() { WriteIndented = true };

    public static async Task<IReadOnlyList<AccessibilityViolation>> ScanAsync(IPage page, string pageName)
    {
        var options = new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = new List<string>(WcagTags) },
            ResultTypes = new HashSet<ResultType> { ResultType.Violations },
        };

        var result = await page.RunAxe(options);
        var violations = result.Violations
            .Select(violation => new AccessibilityViolation(
                violation.Id,
                violation.Impact ?? "unknown",
                violation.Help,
                violation.HelpUrl,
                violation.Nodes.Select(node => node.Target?.ToString() ?? "?").ToList()))
            .OrderBy(violation => violation.RuleId, StringComparer.Ordinal)
            .ToList();

        foreach (var violation in violations)
        {
            TestContext.Out.WriteLine(
                $"A11Y {pageName}: {violation.RuleId} ({violation.Impact}) - {violation.Help} " +
                $"[{violation.Targets.Count} elemento(s): {string.Join(" | ", violation.Targets.Take(3))}]");
        }

        var directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "accessibility");
        Directory.CreateDirectory(directory);
        var reportPath = Path.Combine(
            directory, $"{FileNames.ToSafe(pageName)}-{FileNames.ToSafe(TestContext.CurrentContext.Test.Name)}.json");
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(violations, ReportJson));
        TestContext.AddTestAttachment(reportPath, "Violações de acessibilidade (axe-core)");

        return violations;
    }

    public static string Describe(string pageName, IEnumerable<AccessibilityViolation> violations) =>
        $"Violações WCAG em '{pageName}':" + Environment.NewLine + string.Join(Environment.NewLine,
            violations.Select(violation =>
                $"  - {violation.RuleId} ({violation.Impact}): {violation.Help}. " +
                $"{violation.Targets.Count} elemento(s), ex.: {violation.Targets.FirstOrDefault()}. {violation.HelpUrl}"));
}
