# SauceDemo.Playwright

[![Testes](https://github.com/JAntonioCoelho/sauce-demo-playwright/actions/workflows/tests.yml/badge.svg)](https://github.com/JAntonioCoelho/sauce-demo-playwright/actions/workflows/tests.yml)

Testes E2E de UI ao [Sauce Demo](https://www.saucedemo.com) com Playwright for .NET e NUnit.

## Pré-requisitos

- .NET 10 SDK
- PowerShell (`pwsh`), para instalar os browsers do Playwright

## Correr tudo de uma vez (Windows)

Duplo clique em `build-and-test.cmd`: faz build, instala o Chromium, corre o gate e depois os `KnownBug`.
O resultado fica em `build-and-test.log` e os `.trx` em `TestResults/`.

## Primeira execução

```bash
dotnet build
pwsh SauceDemo.Tests/bin/Debug/net10.0/playwright.ps1 install
dotnet test
```

## Comandos úteis

```bash
# Só smoke tests
dotnet test --filter "Category=Smoke"

# Outro browser
dotnet test -- Playwright.BrowserName=firefox

# Com browser visível
dotnet test -- Playwright.LaunchOptions.Headless=false

# Outro ambiente (lê appsettings.Staging.json, se existir)
TEST_ENVIRONMENT=Staging dotnet test

# Override pontual por variável de ambiente
SAUCEDEMO_BaseUrl=https://outro-url dotnet test
```

Quando um teste falha, o trace fica em `bin/<config>/net10.0/playwright-traces/`.
Para o abrir, usa `pwsh bin/Debug/net10.0/playwright.ps1 show-trace <ficheiro.zip>` ou arrasta-o para trace.playwright.dev.

## Integração contínua (GitHub Actions)

O workflow `.github/workflows/tests.yml` corre em cada push e pull request para `main`, em Linux com Chromium:

1. Build em Release e instalação do Chromium.
2. **Gate** (`Category!=KnownBug`): decide se o run fica verde.
3. **Bugs conhecidos** (`Category=KnownBug`): devem falhar e não bloqueiam. Se algum passar, o resumo do run avisa.
4. Resumo no próprio run e artefacto `test-results` com os `.trx`, os traces e os diffs visuais.

**Baselines visuais de Linux:** se um run criar baselines novas (ficheiros que ainda não estão no repo), publica-as no
artefacto `visual-baselines-linux`. Revê as imagens e faz commit em `SauceDemo.Tests/VisualBaselines/chromium-linux/`.
Para regenerar todas: *Actions → Testes → Run workflow*, com "Regenerar as baselines visuais de Linux".

O `azure-pipelines.yml` faz o mesmo em Azure DevOps.

## Estrutura

| Pasta | Conteúdo |
|---|---|
| `Config/` | Leitura da configuração (JSON + variáveis de ambiente) |
| `Fixtures/` | Classes base, tracing, setup global com login reutilizado |
| `Pages/` | Page Objects e componentes. Só ações e locators, nunca asserções |
| `TestData/` | Utilizadores, produtos e dados de cliente |
| `Tests/` | Testes organizados por funcionalidade |

## Convenções

- **Locators:** `ByTestId` (extensão própria, atributo `data-test`) e `GetByRole`. Nada de XPath.
  Não usar `Selectors.SetTestIdAttribute`: é estado global e, em paralelo, volta ao valor por omissão a meio dos testes.
- **Esperas:** nunca `Thread.Sleep` nem `WaitForTimeoutAsync`. Usar asserções web-first (`Expect(...)`), que já fazem retry.
- **Isolamento:** cada teste tem um `BrowserContext` novo. Nenhum teste depende de outro.
- **Login:** feito uma vez no `GlobalSetup` e reutilizado via `StorageState` nos testes que herdam de `AuthenticatedTest`.
- **Bugs conhecidos:** testes que descrevem o comportamento correto e correm com o `standard_user` (tem de passar)
  e com um utilizador com bugs (`problem_user`, `performance_glitch_user`, `error_user`, `visual_user`), na categoria `KnownBug`. O gate corre
  com `--filter "Category!=KnownBug"`; os `KnownBug` correm à parte e devem falhar. Se um deixar de falhar, o bug foi corrigido.
- **Diálogos JavaScript:** usar o `DialogRecorder` (`using var dialogs = new DialogRecorder(Page);`), que regista e aceita
  alertas. Sem handler, o Playwright fecha-os sozinho e o teste não dá por eles.
- **Regressão visual:** `VisualBaseline.AssertMatchesAsync(Page, "nome", isReference)` compara com
  `VisualBaselines/{browser}-{so}/nome.png` (versionadas). Na primeira corrida a baseline é criada e o teste fica
  `Inconclusive` até ser revista. Para regenerar: `UPDATE_VISUAL_BASELINES=1`. Em falha, o `-diff.png` mostra as diferenças a vermelho.
- **Tracking:** o histórico de trabalho, decisões e bugs encontrados está em `TRACKING.md`.
- **Nomes dos testes:** `Funcionalidade_Cenario_ResultadoEsperado`.
- **Paralelismo:** as fixtures correm em paralelo e os testes dentro de cada fixture em sequência. O número de workers está no `.runsettings`.
