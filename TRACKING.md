# Tracking — SauceDemo.Playwright

Registo do trabalho no projeto: o que foi feito, decisões, resultados e próximos passos.
Entradas mais recentes primeiro.

## Estado atual

| | |
|---|---|
| Stack | .NET 10 · NUnit 5.0 · Microsoft.Playwright.NUnit 1.64.1 |
| Browsers | Chromium 156, Firefox 157, WebKit 27.2 (CI); Chromium (local) |
| Repositório | [JAntonioCoelho/sauce-demo-playwright](https://github.com/JAntonioCoelho/sauce-demo-playwright) (público) |
| CI | GitHub Actions, `tests.yml`: matriz de 3 browsers em paralelo, verde (~2 min por run) |
| Gate (`Category!=KnownBug`) | 28/28 a passar em cada browser (e em Windows, local) |
| Bugs conhecidos (`KnownBug`) | 10, todos a falhar como esperado nos 3 browsers |
| Utilizadores cobertos | todos os 6 do Sauce Demo |
| Baselines visuais | `chromium-win`, `chromium-linux`, `firefox-linux`, `webkit-linux` (2 cada) |
| Próximo passo | Acessibilidade com axe-core (`Deque.AxeCore.Playwright`) |

---

## 2026-11-13 — Chromium, Firefox e WebKit na pipeline

**Feito**
- Matriz `browser: [chromium, firefox, webkit]` com `fail-fast: false`, para se ver o resultado dos três mesmo que um falhe.
- Cada job instala só o seu browser e passa-o ao `dotnet test` de duas formas: `BROWSER` (variável de ambiente)
  e `Playwright.BrowserName`. Artefactos (`test-results-<browser>`), resumo e branch de baselines
  (`ci/visual-baselines-<browser>-linux`) levam o nome do browser, para não colidirem.
- Corrigido o `GlobalSetup`: tinha o Chromium fixo para criar o storage state, o que partia o Firefox e o WebKit
  (o Chromium nem está instalado nesses jobs). Passa a usar o browser de `BROWSER`, com Chromium por omissão.
- Baselines `firefox-linux` e `webkit-linux` geradas no 1.º run, revistas (inventário e detalhe: imagens, preços e
  layout corretos) e integradas em `main`.

**Resultado** (run com todas as baselines)

| Browser | Versão | Gate | KnownBug a falhar | Diff do inventário do `visual_user` |
|---|---|---|---|---|
| Chromium | 156 | 28/28 | 10/10 | 3,01% |
| Firefox | 157 | 28/28 | 10/10 | 2,87% |
| WebKit | 27.2 | 28/28 | 10/10 | 2,88% |

**Decisões**
- Nenhum teste precisou de ajuste por browser: os locators (`data-test` e roles) e as asserções web-first comportam-se
  igual nos três. O orçamento de login (4 s) continua a separar bem o `standard_user` do `performance_glitch_user`
  (~5,4 a 6 s nos três browsers).
- Localmente continua a correr só o Chromium; outro browser com `BROWSER=firefox` e o browser instalado.

## 2026-11-13 — GitHub e pipeline no GitHub Actions

**Feito**
- Repositório criado e commit inicial com o projeto, sem os ficheiros obsoletos `build.cmd`/`build.log`.
- `.gitattributes`: LF no repositório, CRLF nos `.cmd`/`.ps1`, PNG como binário (as baselines são comparadas byte a byte).
- `.github/workflows/tests.yml` em cada push/PR para `main`: build Release → Chromium → gate → KnownBug (sem bloquear)
  → resumo no run (`.github/scripts/Write-TestSummary.ps1`, com aviso se um KnownBug passar) → artefacto `test-results`.
- Baselines novas: commit automático para o branch `ci/visual-baselines-linux` (mais o artefacto `visual-baselines-linux`),
  para revisão e merge. Nunca vão diretamente para `main`.
- Baselines `chromium-linux` geradas no CI, revistas (imagens e preços corretos, layout alinhado) e integradas em `main`.

**Runs**

| Run | Resultado | O que mostrou |
|---|---|---|
| 1 | Gate verde, run vermelho | Upload rejeitado: nomes de traces com aspas (`("error_user")`); o passo das baselines foi saltado |
| 2 | Verde | Corrigidos os dois problemas; baselines Linux geradas |
| 3 | Verde | Baselines enviadas para o branch de revisão |
| 4 | Verde | Gate 28/28 sem inconclusivos; `visual_user` com 3,00% de pixels diferentes em Linux |

**Problemas e correções**
- `Path.GetInvalidFileNameChars()` depende do SO: em Linux só exclui `/` e `\0`, por isso as aspas passavam e o
  `upload-artifact` rejeitava-as. Novo `FileNames.ToSafe` (só `[A-Za-z0-9._-]`), igual em todos os sistemas.
- Um `if:` sem `!cancelled()` herda o `success()` implícito: o passo das baselines foi saltado porque o upload anterior falhou.
- Os artefactos ficam num blob storage fora do GitHub, nem sempre acessível de onde se revê. Daí o branch de revisão.

**Decisões**
- O run só fica vermelho se o gate falhar; os KnownBug têm `continue-on-error` e aparecem no resumo.
- `permissions: contents: write` só neste job, e só para o branch de baselines.

## 2026-11-13 — `visual_user` e regressão visual

**Objetivo:** apanhar bugs de layout que os testes funcionais não veem.

**Correção:** a entrada anterior referia `ToHaveScreenshotAsync`, mas esse método **não existe no Playwright .NET**:
a comparação de screenshots só existe no test runner de Node (`@playwright/test`). Em .NET há apenas `ScreenshotAsync`.

**Abordagem**
- Comparação própria, sem dependências novas: o Chromium descodifica os dois PNG num `canvas` e compara pixel a pixel,
  devolvendo o número de pixels diferentes e uma imagem de diff.
- Baseline gerada com o `standard_user` (referência); o `visual_user` tem de ficar igual (`KnownBug`).
- Baselines versionadas por browser e SO (`VisualBaselines/chromium-win/...`), porque a renderização de fontes muda entre sistemas.

- [x] `ImageComparer` (Fixtures): comparação via `OffscreenCanvas` no Chromium; tolerância de 10/255 por canal
- [x] `VisualBaseline` (Fixtures): screenshot de página inteira (animações desligadas, cursor escondido), depois de esperar
      por fontes e imagens; compara com `VisualBaselines/{browser}-{so}/{nome}.png`; em falha grava e anexa `-actual.png` e `-diff.png`
- [x] Só o utilizador de referência cria baselines (o teste fica `Inconclusive` para obrigar a revisão);
      `UPDATE_VISUAL_BASELINES=1` regenera-as
- [x] Caminho das baselines via `AssemblyMetadata("ProjectDirectory")` no `.csproj`, para ficarem na pasta do projeto e não em `bin/`
- [x] `VisualUserTests`: inventário e detalhe da Backpack
- [x] 1.ª corrida gerou as baselines; 2.ª corrida: `standard_user` igual à baseline (estável)
- [x] Calibração: o detalhe de produto do `visual_user` ficou igual → **não** é `KnownBug`, passou a regressão normal
- Resultado: build 0 erros · gate 28/28 · KnownBug 10/10 a falhar

**Bugs encontrados** (inventário do `visual_user`: ~2,9% dos pixels diferentes, limite 0,1%)

| Bug visual | Visto no diff |
|---|---|
| Imagem da Backpack trocada por um cão | bloco inteiro a vermelho |
| Preços aleatórios a cada carregamento (ex.: `$84.57` em vez de `$29.99`) | todos os preços |
| Ícone do carrinho fora do cabeçalho, junto ao filtro de ordenação | ícone duplicado no diff |
| Títulos da Bolt T-Shirt e da Fleece Jacket desalinhados | títulos sobrepostos |
| "Add to cart" da T-Shirt (Red) sai fora do cartão | botão deslocado |

**Decisões**
- Limite de 0,1% de pixels diferentes: o `standard_user` dá 0 diferenças entre corridas; o `visual_user` dá ~2,9%.
- Os preços aleatórios fazem o diff do `visual_user` variar entre corridas, mas fica sempre muito acima do limite.
- Baselines por SO: as de Windows não servem na pipeline Linux. Na pipeline, sem baseline, os testes visuais ficam
  `Inconclusive` (não falham) até haver `chromium-linux`.

## 2026-11-13 — `error_user` e tratamento de diálogos JavaScript

**Objetivo:** cobrir o `error_user` e treinar o tratamento de diálogos JavaScript (alertas) no Playwright.

- [x] `ErrorUserTests`: 4 testes de comportamento correto, com `standard_user` (passa) e `error_user` (`KnownBug`)
- [x] `DialogRecorder` (Fixtures): subscreve `Page.Dialog`, regista cada diálogo e aceita-o, para o teste não ficar pendurado
      e poder verificar o que apareceu. Sem handler, o Playwright fecha os diálogos sozinho e o teste nem dá por eles.
- [x] `InventoryPage`: `SelectSortOptionAsync` (escolhe sem validar) e `GetNamesInCartAsync` (produtos com botão "Remove")
- [x] Calibração: os 4 casos do `error_user` falham pelo bug certo, nenhum perdeu a marca `KnownBug`
- Resultado: build 0 erros/0 avisos · gate 25/25 · KnownBug 9/9 a falhar

**Bugs encontrados**

| Utilizador | Bug | Teste |
|---|---|---|
| `error_user` | Ordenar abre `alert: Sorting is broken! This error has been reported to Backtrace.` e a lista não muda | `Sort_ByPriceLowToHigh_SortsWithoutErrorDialog` |
| `error_user` | Só 3 dos 6 produtos entram no carrinho | `AddToCart_EveryProduct_CountsAllInCartBadge` |
| `error_user` | "Remove" não tira os produtos do carrinho (badge fica em 3) | `RemoveFromCart_EveryProductInCart_EmptiesCart` |
| `error_user` | Chega ao overview do checkout, mas "Finish" não conclui a encomenda | `Checkout_CompleteOrder_ShowsConfirmation` |

**Decisões**
- No teste de ordenação, `Assert.Multiple` reporta o diálogo e a ordem errada de uma só vez.
- O teste de "Remove" só remove o que entrou de facto no carrinho e usa `Assume` se nada entrou,
  para não depender do bug do "Add to cart".

## 2026-11-13 — Build verde, suite estável e testes de utilizadores com bugs

**Feito**
- Primeira build no Windows (SDK 10.0.403): 0 erros, 0 avisos.
- Corrigido comentário XML inválido (`--`) no `.runsettings`, que impedia o `dotnet test` de arrancar.
- Corrigida race condition: `Selectors.SetTestIdAttribute` é estado global no Playwright .NET e, com fixtures em paralelo,
  voltava a `data-testid` a meio de um teste (3 falhas intermitentes). Substituído pela extensão `ByTestId` (`[data-test="..."]`).
  Resultado: 15/15 e a suite passou de 1,4 min para ~26 s.
- Testes `ProblemUserTests` e `PerformanceGlitchUserTests`: comportamento correto com `standard_user` + utilizador com bugs (`KnownBug`).
- `build-and-test.ps1` (lançado por `build-and-test.cmd`): build → Chromium → gate → KnownBug, tudo em `build-and-test.log`.
  O `.cmd` original fechava logo após o primeiro `dotnet test --filter`, daí a mudança para PowerShell.
- `azure-pipelines.yml`: gate e KnownBug em passos separados (KnownBug com `continueOnError`).

**Bugs encontrados**

| Utilizador | Bug | Teste |
|---|---|---|
| `problem_user` | Os 6 produtos mostram a mesma imagem (`sl-404.jpg`) | `Inventory_EachProduct_ShowsItsOwnImage` |
| `problem_user` | Ordenação ignorada, o dropdown volta a `az` | `Sort_ByPriceLowToHigh_OrdersPricesAscending` |
| `problem_user` | Só 3 dos 6 produtos entram no carrinho | `AddToCart_EveryProduct_CountsAllInCartBadge` |
| `problem_user` | Checkout: o apelido não fica preenchido → "Last Name is required" | `Checkout_WithValidCustomerInfo_ReachesOverview` |
| `performance_glitch_user` | Login ~6 s (orçamento 4 s; `standard_user` ~1,5 s) | `Login_CompletesWithinPerformanceBudget` |

**Decisões**
- Testes `KnownBug` descrevem o comportamento correto; se um deixar de falhar, o bug foi corrigido.
- Orçamento de login subido de 3 s para 4 s: o `standard_user` chegou aos 2,2 s numa corrida.

## 2026-11-06 — Esqueleto do projeto

- Sauce Demo como aplicação alvo; NUnit "puro" (sem BDD).
- Page Objects sem asserções, login único via `StorageState`, trace só em falha, configuração por JSON + variáveis `SAUCEDEMO_*`.
- 15 testes iniciais (login, inventário, carrinho, checkout E2E) e `azure-pipelines.yml`.
- Dependências atualizadas para as últimas versões estáveis.
