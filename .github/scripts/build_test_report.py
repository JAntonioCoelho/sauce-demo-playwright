#!/usr/bin/env python3
"""
Gera um relatório HTML único a partir dos resultados de todos os jobs da pipeline.

Entrada: a pasta onde o actions/download-artifact descarregou os artefactos, uma subpasta por artefacto
(test-results-chromium, test-results-firefox, test-results-webkit, test-results-api), cada uma com os .trx
(gate.trx e known-bugs.trx) e os anexos dos testes (traces, screenshots, diffs, relatórios de acessibilidade).

Saída: <saída>/index.html, autocontido (CSS inline e imagens embutidas em base64), mais <saída>/files/ com os
anexos grandes (traces .zip, JSON de acessibilidade) ligados a partir do HTML.

Só usa a biblioteca padrão do Python, para correr em qualquer runner sem instalar nada.
"""
from __future__ import annotations

import argparse
import base64
import html
import json
import os
import re
import shutil
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

# Ordem e nomes das colunas. Artefactos com outros nomes aparecem no fim, com o nome tal como está.
JOBS = {
    "test-results-chromium": "Chromium",
    "test-results-firefox": "Firefox",
    "test-results-webkit": "WebKit",
    "test-results-api": "API",
}

MAX_EMBEDDED_IMAGE_BYTES = 1_500_000


@dataclass
class Attachment:
    name: str
    path: Path | None  # None quando o ficheiro referido no .trx não foi encontrado


@dataclass
class TestResult:
    job: str
    run: str  # "gate" ou "known-bugs"
    name: str
    class_name: str
    outcome: str
    duration: str
    message: str
    stack: str
    stdout: str
    attachments: list[Attachment] = field(default_factory=list)

    @property
    def fixture(self) -> str:
        return self.class_name.rsplit(".", 1)[-1] if self.class_name else "(sem classe)"

    @property
    def area(self) -> str:
        return "API" if ".Api." in self.class_name else "UI"


@dataclass
class Stats:
    attachments_found: int = 0
    attachments_missing: list[str] = field(default_factory=list)


# ---------------------------------------------------------------- leitura dos .trx


def index_files(root: Path) -> dict[str, list[Path]]:
    by_name: dict[str, list[Path]] = {}
    for path in root.rglob("*"):
        if path.is_file():
            by_name.setdefault(path.name.lower(), []).append(path)
    return by_name


def resolve_attachment(trx: Path, relative_dir: str, execution_id: str, relative_path: str,
                       files: dict[str, list[Path]]) -> Path | None:
    """O VSTest copia os anexos para <resultados>/<deployment>/In/<relativeResultsDirectory>/...; procura de forma tolerante."""
    relative_path = relative_path.replace("\\", "/")
    for deployment in trx.parent.iterdir():
        if deployment.is_dir():
            candidate = deployment / "In" / relative_dir / relative_path
            if candidate.is_file():
                return candidate
    candidates = files.get(Path(relative_path).name.lower(), [])
    for marker in (relative_dir, execution_id):
        if marker:
            for candidate in candidates:
                if marker in candidate.as_posix():
                    return candidate
    return candidates[0] if len(candidates) == 1 else None


def read_trx(trx: Path, job: str, run: str, files: dict[str, list[Path]], stats: Stats) -> list[TestResult]:
    root = ET.parse(trx).getroot()
    classes = {
        unit.get("id"): (unit.find("t:TestMethod", NS).get("className") if unit.find("t:TestMethod", NS) is not None else "")
        for unit in root.iterfind("t:TestDefinitions/t:UnitTest", NS)
    }
    results = []
    for result in root.iterfind("t:Results/t:UnitTestResult", NS):
        def text(xpath: str) -> str:
            node = result.find(xpath, NS)
            return (node.text or "").strip() if node is not None else ""

        attachments = []
        for result_file in result.iterfind("t:ResultFiles/t:ResultFile", NS):
            relative_path = result_file.get("path", "")
            path = resolve_attachment(trx, result.get("relativeResultsDirectory", ""), result.get("executionId", ""),
                                      relative_path, files)
            if path is None:
                stats.attachments_missing.append(f"{job}: {result.get('testName')}: {relative_path}")
            else:
                stats.attachments_found += 1
            attachments.append(Attachment(Path(relative_path.replace("\\", "/")).name, path))

        results.append(TestResult(
            job=job,
            run=run,
            name=result.get("testName", "?"),
            class_name=classes.get(result.get("testId"), "") or "",
            outcome=result.get("outcome", "Unknown"),
            duration=result.get("duration", ""),
            message=text("t:Output/t:ErrorInfo/t:Message"),
            stack=text("t:Output/t:ErrorInfo/t:StackTrace"),
            stdout=text("t:Output/t:StdOut"),
            attachments=attachments,
        ))
    return results


def load(input_dir: Path, stats: Stats) -> tuple[list[str], list[TestResult]]:
    jobs, results = [], []
    folders = sorted((p for p in input_dir.iterdir() if p.is_dir() and p.name.startswith("test-results")),
                     key=lambda p: (list(JOBS).index(p.name) if p.name in JOBS else len(JOBS), p.name))
    for folder in folders:
        job = JOBS.get(folder.name, folder.name)
        jobs.append(job)
        files = index_files(folder)
        for trx in sorted(folder.rglob("*.trx")):
            run = "known-bugs" if "known" in trx.stem.lower() else "gate"
            results.extend(read_trx(trx, job, run, files, stats))
    return jobs, results


# ---------------------------------------------------------------- formatação


def esc(value: str) -> str:
    return html.escape(value or "", quote=True)


def first_line(value: str) -> str:
    return next((line.strip() for line in (value or "").splitlines() if line.strip()), "")


def seconds(duration: str) -> float:
    match = re.match(r"(\d+):(\d+):([\d.]+)", duration or "")
    return int(match[1]) * 3600 + int(match[2]) * 60 + float(match[3]) if match else 0.0


def fmt_seconds(total: float) -> str:
    return f"{total:.1f} s" if total < 120 else f"{total / 60:.1f} min"


OUTCOME_LABEL = {"Passed": "Passou", "Failed": "Falhou", "NotExecuted": "Não executado", "Inconclusive": "Inconclusivo"}


def outcome_class(result: TestResult) -> str:
    if result.run == "known-bugs":
        return "expected" if result.outcome == "Failed" else ("warn" if result.outcome == "Passed" else "skip")
    return {"Passed": "pass", "Failed": "fail"}.get(result.outcome, "skip")


class Report:
    def __init__(self, jobs: list[str], results: list[TestResult], output: Path, meta: dict[str, str]):
        self.jobs, self.results, self.output, self.meta = jobs, results, output, meta
        self.files_dir = output / "files"
        self.files_dir.mkdir(parents=True, exist_ok=True)
        self.copied: dict[Path, str] = {}

    # ---- anexos

    def link(self, attachment: Attachment, job: str) -> str:
        if attachment.path is None:
            return f'<span class="muted">{esc(attachment.name)} (não encontrado)</span>'
        if attachment.path not in self.copied:
            target = self.files_dir / re.sub(r"[^A-Za-z0-9._-]", "_", f"{job}-{attachment.path.name}")
            shutil.copyfile(attachment.path, target)
            self.copied[attachment.path] = f"files/{target.name}"
        return f'<a href="{esc(self.copied[attachment.path])}">{esc(attachment.name)}</a>'

    @staticmethod
    def image(attachment: Attachment, caption: str) -> str:
        if attachment.path is None or attachment.path.stat().st_size > MAX_EMBEDDED_IMAGE_BYTES:
            return ""
        data = base64.b64encode(attachment.path.read_bytes()).decode()
        return (f'<figure><img loading="lazy" alt="{esc(caption)}" src="data:image/png;base64,{data}">'
                f"<figcaption>{esc(caption)}</figcaption></figure>")

    def attachments_html(self, result: TestResult, images: bool = True) -> str:
        if not result.attachments:
            return ""
        figures = "".join(
            self.image(a, f"{result.job} · {a.name}") for a in result.attachments
            if images and a.name.lower().endswith(".png") and "-diff" in a.name.lower())
        links = " · ".join(self.link(a, result.job) for a in result.attachments)
        return f'<div class="files">Anexos: {links}</div>' + (f'<div class="gallery">{figures}</div>' if figures else "")

    # ---- secções

    def overview(self) -> str:
        cards = []
        for job in self.jobs:
            gate = [r for r in self.results if r.job == job and r.run == "gate"]
            known = [r for r in self.results if r.job == job and r.run == "known-bugs"]
            passed = sum(r.outcome == "Passed" for r in gate)
            failed = sum(r.outcome == "Failed" for r in gate)
            other = len(gate) - passed - failed
            known_failing = sum(r.outcome == "Failed" for r in known)
            known_passing = sum(r.outcome == "Passed" for r in known)
            state = "fail" if failed else "pass"
            duration = fmt_seconds(sum(seconds(r.duration) for r in gate + known))
            warning = f'<p class="warn-text">{known_passing} bug(s) conhecido(s) passaram</p>' if known_passing else ""
            cards.append(f"""
            <div class="card {state}">
              <h3>{esc(job)}</h3>
              <p class="big">{passed}<span>/{len(gate)}</span></p>
              <p>gate · {failed} falha(s) · {other} não executado(s)</p>
              <p>{known_failing}/{len(known)} bugs conhecidos a falhar</p>
              {warning}
              <p class="muted">{duration} de testes</p>
            </div>""")
        return f'<section><h2>Resumo</h2><div class="cards">{"".join(cards)}</div></section>'

    def failures(self) -> str:
        failed = [r for r in self.results if r.run == "gate" and r.outcome == "Failed"]
        if not failed:
            return '<section><h2>Falhas no gate</h2><p class="ok-text">Nenhuma falha no gate.</p></section>'
        items = []
        for r in failed:
            # As imagens de diff já aparecem na secção de regressão visual: aqui só o link, para não as embutir duas vezes.
            has_diff = any(a.name.lower().endswith("-diff.png") and a.path for a in r.attachments)
            see_visual = '<p class="muted">Diff na secção <a href="#visual">Regressão visual</a>.</p>' if has_diff else ""
            items.append(f"""
            <article class="failure">
              <h3><span class="tag">{esc(r.job)}</span> {esc(r.fixture)} › {esc(r.name)}</h3>
              <pre class="message">{esc(r.message)}</pre>
              <details><summary>Stack trace</summary><pre>{esc(r.stack)}</pre></details>
              {self.attachments_html(r, images=False)}{see_visual}
            </article>""")
        return f'<section><h2>Falhas no gate ({len(failed)})</h2>{"".join(items)}</section>'

    def matrix(self) -> str:
        gate = [r for r in self.results if r.run == "gate"]
        by_test: dict[tuple[str, str, str], dict[str, TestResult]] = {}
        for r in gate:
            by_test.setdefault((r.area, r.fixture, r.name), {})[r.job] = r
        rows, current = [], None
        for (area, fixture, name) in sorted(by_test):
            if (area, fixture) != current:
                current = (area, fixture)
                rows.append(f'<tr class="group"><th colspan="{len(self.jobs) + 1}">{esc(fixture)}'
                            f' <span class="muted">({esc(area)})</span></th></tr>')
            cells = []
            for job in self.jobs:
                r = by_test[(area, fixture, name)].get(job)
                if r is None:
                    cells.append('<td class="na">·</td>')
                else:
                    label = OUTCOME_LABEL.get(r.outcome, r.outcome)
                    cells.append(f'<td class="{outcome_class(r)}" title="{esc(label)} · {esc(r.duration)}">'
                                 f'{"✓" if r.outcome == "Passed" else "✗" if r.outcome == "Failed" else "–"}</td>')
            rows.append(f'<tr><td class="name">{esc(name)}</td>{"".join(cells)}</tr>')
        head = "".join(f"<th>{esc(job)}</th>" for job in self.jobs)
        return f"""
        <section>
          <h2>Matriz do gate</h2>
          <p class="muted">✓ passou · ✗ falhou · – não executado ou inconclusivo · <span class="na">·</span> não se aplica a este job</p>
          <div class="scroll"><table class="matrix"><thead><tr><th>Teste</th>{head}</tr></thead><tbody>{"".join(rows)}</tbody></table></div>
        </section>"""

    def known_bugs(self) -> str:
        known = [r for r in self.results if r.run == "known-bugs"]
        if not known:
            return ""
        by_test: dict[tuple[str, str], dict[str, TestResult]] = {}
        for r in known:
            by_test.setdefault((r.fixture, r.name), {})[r.job] = r
        rows = []
        for (fixture, name), per_job in sorted(by_test.items()):
            cells = []
            for job in self.jobs:
                r = per_job.get(job)
                if r is None:
                    cells.append('<td class="na">·</td>')
                elif r.outcome == "Failed":
                    cells.append(f'<td class="expected" title="{esc(first_line(r.message))}">falha</td>')
                elif r.outcome == "Passed":
                    cells.append('<td class="warn">passou ⚠</td>')
                else:
                    cells.append('<td class="skip">–</td>')
            reason = first_line(next((r.message for r in per_job.values() if r.message), ""))
            rows.append(f'<tr><td class="name">{esc(fixture)} › {esc(name)}<div class="reason">{esc(reason)}</div></td>'
                        f'{"".join(cells)}</tr>')
        head = "".join(f"<th>{esc(job)}</th>" for job in self.jobs)
        return f"""
        <section>
          <h2>Bugs conhecidos ({len(by_test)})</h2>
          <p class="muted">Descrevem o comportamento correto e têm de falhar enquanto o bug existir. "Passou ⚠" quer dizer
          que o bug pode ter sido corrigido: o teste deve sair da categoria KnownBug.</p>
          <div class="scroll"><table class="matrix known"><thead><tr><th>Teste e motivo</th>{head}</tr></thead>
          <tbody>{"".join(rows)}</tbody></table></div>
        </section>"""

    def visual(self) -> str:
        with_diffs = [r for r in self.results
                      if any(a.name.lower().endswith("-diff.png") and a.path for a in r.attachments)]
        if not with_diffs:
            return '<section><h2>Regressão visual</h2><p class="ok-text">Sem diferenças visuais neste run.</p></section>'
        blocks = []
        for r in sorted(with_diffs, key=lambda r: (r.name, self.jobs.index(r.job) if r.job in self.jobs else 99)):
            expected = " <span class=\"tag expected\">bug conhecido</span>" if r.run == "known-bugs" else ""
            blocks.append(f"""
            <article class="visual">
              <h3><span class="tag">{esc(r.job)}</span> {esc(r.name)}{expected}</h3>
              <p class="muted">{esc(first_line(r.message))}</p>
              {self.attachments_html(r)}
            </article>""")
        return (f'<section id="visual"><h2>Regressão visual</h2><p class="muted">Diferenças face à baseline a vermelho, '
                f'sobre a imagem esperada esbatida.</p>{"".join(blocks)}</section>')

    def accessibility(self) -> str:
        rows: dict[str, dict[str, str]] = {}
        details = []
        for r in self.results:
            for a in r.attachments:
                if a.path is None or not a.name.lower().endswith(".json"):
                    continue
                try:
                    violations = json.loads(a.path.read_text(encoding="utf-8-sig"))
                except (ValueError, OSError):
                    continue
                page = a.name.split("-Page_")[0].split("-Scanner_")[0]
                count = len(violations)
                rows.setdefault(page, {})[r.job] = (
                    f'<td class="{"pass" if count == 0 or page == "controlo-positivo" else "fail"}">{count}</td>')
                if violations:
                    items = "".join(
                        f'<li><code>{esc(v.get("RuleId", "?"))}</code> ({esc(v.get("Impact", "?"))}): {esc(v.get("Help", ""))}'
                        f' · {len(v.get("Targets", []))} elemento(s)</li>' for v in violations)
                    details.append(f'<details><summary>{esc(r.job)} · {esc(page)} · {count} violação(ões)</summary>'
                                   f'<ul>{items}</ul></details>')
        if not rows:
            return ""
        jobs = [job for job in self.jobs if any(job in per_job for per_job in rows.values())]
        head = "".join(f"<th>{esc(job)}</th>" for job in jobs)
        body = "".join(
            f'<tr><td class="name">{esc(page)}</td>'
            + "".join(per_job.get(job, '<td class="na">·</td>') for job in jobs) + "</tr>"
            for page, per_job in sorted(rows.items(), key=lambda item: (item[0] == "controlo-positivo", item[0])))
        return f"""
        <section>
          <h2>Acessibilidade (WCAG 2.x A/AA, axe-core)</h2>
          <p class="muted">Número de violações por página. O "controlo-positivo" tem violações plantadas de propósito:
          tem de ter mais de zero, para provar que o scanner as deteta.</p>
          <div class="scroll"><table class="matrix"><thead><tr><th>Página</th>{head}</tr></thead><tbody>{body}</tbody></table></div>
          {"".join(details)}
        </section>"""

    def render(self) -> str:
        gate_failed = any(r.run == "gate" and r.outcome == "Failed" for r in self.results)
        status = ("fail", "Gate vermelho") if gate_failed else ("pass", "Gate verde")
        run_link = (f'<a href="{esc(self.meta["run_url"])}">run no GitHub Actions</a>' if self.meta.get("run_url") else "")
        commit = esc(self.meta.get("commit", "")[:7])
        generated = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M UTC")
        traces_note = ('<p class="muted">Traces (.zip): abre em <a href="https://trace.playwright.dev">trace.playwright.dev</a> '
                       '(arrasta o ficheiro) ou com <code>pwsh playwright.ps1 show-trace &lt;ficheiro&gt;</code>.</p>')
        return f"""<!doctype html>
<html lang="pt">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Relatório de testes</title>
<style>{CSS}</style>
</head>
<body>
<header>
  <h1>Relatório de testes <span class="muted">SauceDemo.Playwright</span></h1>
  <p class="status {status[0]}">{status[1]}</p>
  <p class="muted">Commit {commit} · {esc(self.meta.get("ref", ""))} · {run_link} · gerado {generated}</p>
</header>
<main>
{self.overview()}
{self.failures()}
{self.matrix()}
{self.known_bugs()}
{self.visual()}
{self.accessibility()}
{traces_note}
</main>
</body>
</html>"""


CSS = """
:root { --bg:#fbfbfa; --fg:#1d1d1b; --muted:#6b6b66; --line:#e3e2dd; --card:#fff;
        --pass:#1f7a4d; --pass-bg:#e6f4ec; --fail:#b42318; --fail-bg:#fdecea;
        --warn:#9a5b00; --warn-bg:#fff4e0; --expected:#5b4bb3; --expected-bg:#efecfb; --skip-bg:#f1f1ee; }
@media (prefers-color-scheme: dark) {
  :root { --bg:#161615; --fg:#ecebe6; --muted:#a3a29b; --line:#2f2e2b; --card:#1f1f1d;
          --pass:#6cd19a; --pass-bg:#16301f; --fail:#ff8a7a; --fail-bg:#3a1714;
          --warn:#f2b45c; --warn-bg:#36280f; --expected:#b4a8f5; --expected-bg:#26213f; --skip-bg:#262624; } }
* { box-sizing:border-box; }
body { margin:0; background:var(--bg); color:var(--fg); font:15px/1.5 system-ui,-apple-system,"Segoe UI",sans-serif; }
header, main { max-width:1180px; margin:0 auto; padding:0 16px; }
header { padding-top:28px; }
h1 { font-size:26px; margin:0 0 6px; } h1 .muted { font-weight:400; font-size:18px; }
h2 { font-size:19px; margin:36px 0 12px; padding-bottom:6px; border-bottom:1px solid var(--line); }
h3 { font-size:15px; margin:0 0 8px; }
.muted { color:var(--muted); } a { color:inherit; }
.status { display:inline-block; font-weight:600; padding:4px 12px; border-radius:999px; margin:4px 0 8px; }
.status.pass { background:var(--pass-bg); color:var(--pass); } .status.fail { background:var(--fail-bg); color:var(--fail); }
.cards { display:grid; grid-template-columns:repeat(auto-fit,minmax(200px,1fr)); gap:12px; }
.card { background:var(--card); border:1px solid var(--line); border-left:4px solid var(--pass); border-radius:8px; padding:14px 16px; }
.card.fail { border-left-color:var(--fail); }
.card p { margin:2px 0; } .card .big { font-size:30px; font-weight:650; } .card .big span { font-size:16px; color:var(--muted); }
.ok-text { color:var(--pass); } .warn-text { color:var(--warn); font-weight:600; }
.scroll { overflow-x:auto; }
table.matrix { border-collapse:collapse; width:100%; background:var(--card); }
.matrix th, .matrix td { border-bottom:1px solid var(--line); padding:5px 10px; text-align:center; white-space:nowrap; }
.matrix td.name, .matrix th:first-child { text-align:left; white-space:normal; }
.matrix tr.group th { text-align:left; background:var(--skip-bg); font-weight:600; }
td.pass { color:var(--pass); } td.fail { background:var(--fail-bg); color:var(--fail); font-weight:700; }
td.expected { background:var(--expected-bg); color:var(--expected); } td.warn { background:var(--warn-bg); color:var(--warn); font-weight:700; }
td.skip { color:var(--muted); } td.na, span.na { color:var(--line); }
.reason { color:var(--muted); font-size:13px; }
.failure, .visual { background:var(--card); border:1px solid var(--line); border-radius:8px; padding:14px 16px; margin:12px 0; }
.failure { border-left:4px solid var(--fail); }
.tag { display:inline-block; font-size:12px; font-weight:600; padding:1px 8px; border-radius:6px; background:var(--skip-bg); }
.tag.expected { background:var(--expected-bg); color:var(--expected); }
pre { background:var(--skip-bg); padding:10px; border-radius:6px; overflow-x:auto; font-size:13px; white-space:pre-wrap; word-break:break-word; }
pre.message { color:var(--fail); }
.files { font-size:13px; margin:8px 0; word-break:break-all; }
.gallery { display:grid; grid-template-columns:repeat(auto-fill,minmax(300px,460px)); gap:12px; }
figure { margin:0; } figure img { width:100%; border:1px solid var(--line); border-radius:6px; }
figcaption { font-size:12px; color:var(--muted); }
details { margin:6px 0; } summary { cursor:pointer; }
code { font-size:13px; }
"""


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--run-url", default=os.environ.get("RUN_URL", ""))
    parser.add_argument("--commit", default=os.environ.get("GITHUB_SHA", ""))
    parser.add_argument("--ref", default=os.environ.get("GITHUB_REF_NAME", ""))
    args = parser.parse_args()

    stats = Stats()
    jobs, results = load(args.input, stats)
    if not results:
        print(f"Nenhum .trx encontrado em {args.input}", file=sys.stderr)
        return 1

    report = Report(jobs, results, args.output, {"run_url": args.run_url, "commit": args.commit, "ref": args.ref})
    (args.output / "index.html").write_text(report.render(), encoding="utf-8")

    # Resumo para o log: é por aqui que se confirma que nada ficou para trás.
    print(f"Jobs: {', '.join(jobs)}")
    for job in jobs:
        for run in ("gate", "known-bugs"):
            subset = [r for r in results if r.job == job and r.run == run]
            outcomes = {o: sum(r.outcome == o for r in subset) for o in sorted({r.outcome for r in subset})}
            print(f"  {job:9} {run:10} {len(subset):3} testes {outcomes}")
    print(f"Anexos encontrados: {stats.attachments_found}, em falta: {len(stats.attachments_missing)}")
    for missing in stats.attachments_missing[:20]:
        print(f"  em falta: {missing}")
    size = (args.output / "index.html").stat().st_size
    print(f"Relatório: {args.output / 'index.html'} ({size / 1024:.0f} KB) + {len(report.copied)} ficheiro(s) em files/")
    return 0


if __name__ == "__main__":
    sys.exit(main())
