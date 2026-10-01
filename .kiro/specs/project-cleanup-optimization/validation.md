# Validação Final — project-cleanup-optimization (Task 15.1)

> Artefato da **Task 15.1** (Validação final / R1.5, R10.3): rodar a suíte EditMode completa e
> comparar o Profiler antes/depois. Este documento registra **o que foi verificado neste ambiente**
> e **o que exige o Unity Editor do usuário** — sem fabricar resultados de teste ou de Profiler.

## Ressalva de execução (R10)

Confirmado neste ambiente, consistente com a ressalva de execução do `tasks.md` e com o `baseline.md`:

- **Versão do projeto:** `6000.5.10f1` (de `ProjectSettings/ProjectVersion.txt`).
- **Editor instalado:** **nenhum** binário do Unity Editor `6000.5.10f1` foi encontrado na máquina
  (apenas o recurso do Unity Hub em `C:\Program Files\Unity Hub\resources\unity.exe`, que **não** é
  um Editor executável de projeto).
- **Consequência:** o **test runner de EditMode não pode ser dirigido via CLI** aqui. Portanto, a
  execução verde da suíte e as medições de Profiler **devem ser feitas pelo usuário no Unity Editor**.
  Nenhum resultado de teste ou de Profiler é inventado neste documento.

O que **foi** verificado via CLI/inspeção:

- **Existência e anotação** de todos os property tests novos (P1–P9) e das suítes existentes
  referenciadas.
- **`git status`** das mudanças de implementação (pooling, HUD/UI, hot paths, resources, import,
  ferramenta de limpeza).
- Observação de **`.meta` ausentes** em arquivos de teste novos (ver "Pendências antes de rodar").

---

## 1. Testes que existem e devem ser rodados no Test Runner (EditMode)

Abrir no Unity: **Window → General → Test Runner → EditMode → Run All** (assembly
`TechGuy.Tests.EditMode`, em `Assets/_Project/Scripts/Tests/EditMode/Editor/`).

### Property tests novos desta spec (P1–P9) — todos presentes e anotados

| P# | Arquivo | Propriedade (resumo) | Valida |
|----|---------|----------------------|--------|
| P1 | `SimpleObjectPoolPropertyTests.cs` | Pool: acquire nunca duplica instância viva; expande quando vazio; release permite reuso; `CreatedCount` = high-water-mark | 3.1, 3.2, 3.3, 3.7, 3.8, 10.2 |
| P2 | `ProjectileResetClearsTrackedStatePropertyTests.cs` | Após `Reset`, todos os campos rastreados voltam ao default (contadores 0, `_hit` vazio, flags false) | 3.4 |
| P3 | `UiValueCacheDirtyDecisionPropertyTests.cs` | Cache só (re)emite texto quando o valor de origem difere do anterior | 2.1, 2.3, 2.4 |
| P4 | `HudFormatterMatchesInlineFormulaPropertyTests.cs` | String do formatter é byte-idêntica à fórmula inline atual (vida/mana/asura) | 2.2, 2.6 |
| P5 | `HealthBarRecomputeFillPropertyTests.cs` | Barra recomputa sse o valor mudou; valor = `clamp01(health/max)` | 4.2 |
| P6 | `FrameThrottlePropertyTests.cs` | `FrameThrottle` dispara no máximo 1×/intervalo e nunca perde o primeiro tick | 4.1 |
| P7 | `WeaponCacheIdempotentPreloadPropertyTests.cs` | Índice válido: `get` idempotente, sem recarga após preload; índice inválido → fallback do índice 0 | 5.1 |
| P8 | `OrphanCandidateReferenceScanPropertyTests.cs` | Candidato a órfão sse o GUID não aparece em nenhum arquivo de referência; origem reportável | 7.1, 7.5, 7.7 |
| P9 | `ResourcesGuardNeverPureOrphanPropertyTests.cs` | Asset sob `**/Resources/**` nunca é marcado órfão puro (marca "requer verificação de path") | 7.2 |

> Todos os arquivos acima declaram cabeçalho `// Feature: project-cleanup-optimization, Property N` e
> `/// Validates: Requirements ...`, verificados por inspeção.

### Example/edge tests novos desta spec

| Arquivo | Cobre |
|---------|-------|
| `CleanupReportAndFallbackExampleTests.cs` | Campos do relatório + distinção total/trim (R7.3/R7.8); dependência ausente → aviso sem NRE (R4.5); preload falho → fallback com aviso (R5.5) |

### Suítes existentes referenciadas explicitamente pela Task 15.1 (devem continuar verdes)

| Arquivo | Papel |
|---------|------|
| `ProjectileMotionPropertyTests.cs` | Movimento de projétil — comportamento observável preservado após pooling (§3/§4) |
| `ArsenalRicochetDecayInvariantTests.cs` | Ricochete/decay do projétil do player — invariantes preservadas após pooling (§3) |
| `DamageAbsorbedDecisionPropertyTests.cs` | Decisão de dano absorvido — hot paths/UI preservados (§6/§7) |

### Suíte EditMode completa

Rodar **toda** a `TechGuy.Tests.EditMode` (dezenas de arquivos `*PropertyTests.cs` / `*Tests.cs` em
`Assets/_Project/Scripts/Tests/EditMode/Editor/`), não só os arquivos acima. As mudanças desta spec
alteraram código tocado por várias suítes (`Actor`, `ArsenalProjectile`, `EnemyProjectile`,
`CoinPickup`/`CoinDrop`, `PlayerHUD`, `RunRewardUI`, `CombatReadabilityUI`, `OutlineScript`,
`WeaponLoadout`, `HitboxDamage`), então a regressão só é confirmada rodando a suíte inteira em verde.

**Critério de aceite (R1.5 / R10.3):** suíte EditMode completa **verde** após pooling + hot paths.
Registrar o resultado (Run All) e, se qualquer teste falhar, triar antes de marcar a tarefa concluída.

---

## 2. Comparação de Profiler antes/depois (medição dirigida pelo usuário)

As métricas de runtime **não** são mensuráveis via CLI aqui (Play mode indirigível). Capturar
**exatamente com o mesmo roteiro do `baseline.md`** (ocioso por alguns segundos → combate com
disparos/moedas → ler contagens do módulo Memory). Preencher a coluna "Depois" e o veredito.

### Roteiro (idêntico ao baseline — reproduzir passo a passo)

1. Abrir a cena de jogo; personagem **parado** (ocioso) por alguns segundos; ler `GC Allocated In
   Frame` e tempo de frame.
2. Entrar em **combate** (player atirando, inimigos atirando, moedas caindo/coletadas); ler picos de
   GC e tempo de frame médio/pico.
3. Ler as contagens do módulo Memory (objetos/assets/texturas/materiais), em especial `Texture Memory`.

### Tabela de comparação a preencher

| Métrica | Estado | Antes (baseline) | Depois (a medir) | Meta | Veredito |
|---|---|---|---|---|---|
| `GC Allocated In Frame` | Ocioso | ~997 B em ~15 allocs/frame | _(a medir)_ | **→ ~0** após §6/§7 | _(a preencher)_ |
| `GC Allocated In Frame` / picos de GC | Combate | _(baseline a recapturar)_ | _(a medir)_ | **menos/menores picos** após §3 | _(a preencher)_ |
| Tempo de frame CPU (médio/pico) | Ocioso/Combate | _(baseline a recapturar)_ | _(a medir)_ | **menor** em hot paths (§7) | _(a preencher)_ |
| `GC Reserved` / `GC Used` | — | ~1,77 GB / ~1,41 GB | _(a medir)_ | pressão de heap **menor** ao longo do tempo | _(a preencher)_ |
| **`Texture Memory`** | — | ~91 MB | _(a medir)_ | **menor** após §9, sem perda visual | _(a preencher)_ |

> Observação: alguns valores "Antes" ficaram marcados como *a recapturar* no `baseline.md` (combate e
> tempo de frame CPU não foram quantificados numericamente). Se o usuário recapturar o baseline, usar
> o mesmo build/estado para que a comparação seja justa.

### Interpretação das metas (R1.3)

- **Ocioso → ~0 alloc/frame:** valida R2 (HUD/UI sem alocação por frame — §6) e hot paths (§7).
- **Menos/menores picos de GC em combate:** valida R3 (pooling de projéteis/moedas — §3/§4/§5) e §7.
- **`Texture Memory` menor sem perda visual:** valida R6 (import settings — §9).
- Tudo acima é **validado por Profiler + playtest manual**, conforme R1.4/R10.1.

---

## 3. Pendências antes de rodar (bloqueiam uma execução limpa)

Vários arquivos de teste **novos** desta spec estão **sem `.meta`** no `git status`. O Unity gera
`.meta` ao abrir o projeto, mas, por AGENTS.md, os `.meta` devem ser preservados/versionados junto do
asset. Antes (ou ao) abrir o Editor para rodar a suíte, deixar o Unity gerar os `.meta` e versioná-los:

- `ProjectileResetClearsTrackedStatePropertyTests.cs` (sem `.meta`)
- `UiValueCacheDirtyDecisionPropertyTests.cs` (sem `.meta`)
- `HudFormatterMatchesInlineFormulaPropertyTests.cs` (sem `.meta`)
- `HealthBarRecomputeFillPropertyTests.cs` (sem `.meta`)
- `FrameThrottlePropertyTests.cs` (sem `.meta`)
- `WeaponCacheIdempotentPreloadPropertyTests.cs` (sem `.meta`)
- `OrphanCandidateReferenceScanPropertyTests.cs` (sem `.meta`)
- `ResourcesGuardNeverPureOrphanPropertyTests.cs` (sem `.meta`)
- `CleanupReportAndFallbackExampleTests.cs` (sem `.meta`)

(`SimpleObjectPoolPropertyTests.cs` já tem `.meta`.) As pastas de produção novas
(`Core/Pooling/`, `Editor/Cleanup/`) e `FrameThrottle.cs`, `HudFormatter.cs`, `UiValueCache.cs` já
têm `.meta` versionados.

---

## 4. Resumo transparente (o que foi vs. o que falta)

**Verificado neste ambiente (CLI/inspeção):**
- Existência e anotação corretas de P1–P9 e dos example/edge tests desta spec.
- Presença das suítes existentes referenciadas (`ProjectileMotion*`, `ArsenalRicochetDecayInvariant*`,
  `DamageAbsorbedDecision*`).
- Que **não há** Editor `6000.5.10f1` instalado → EditMode runner **não** dirigível via CLI.
- `.meta` faltando em testes novos (listados acima).

**Exige o Unity Editor do usuário (não validável via CLI — não fabricado):**
- Rodar **Run All** da `TechGuy.Tests.EditMode` e confirmar **verde** (R1.5/R10.3).
- Capturar a medição **pós-mudança** no Profiler com o roteiro do baseline e preencher a tabela da
  §2, concluindo os vereditos (`GC Allocated In Frame` ocioso → ~0; menos/menores picos em combate;
  `Texture Memory` menor).

---

# RESUMO DE VALIDAÇÃO FINAL — Task 15.2 (R9.1, R9.4, R10.1, R10.4)

> Artefato da **Task 15.2**: verificação por inspeção/`git diff` e resumo de validação transparente.
> Este resumo separa, **sem fabricar resultados**, três categorias: (A) o que foi **medido no
> Profiler**, (B) o que foi **verificado por inspeção/`git diff`** e (C) o que **depende do playtest
> manual do usuário**. Também declara explicitamente **o que não pôde ser validado via CLI** (R10.1).

## Enumeração das mudanças (saída de `git diff --stat` + `git status`)

Branch: `Refactoring`. Toda a mudança de código vive sob `Assets/_Project/` (código do jogo). Nenhum
arquivo sob `Assets/_ThirdParty/` ou `Assets/RealToon/` foi modificado.

**Fontes modificadas (13 arquivos de implementação + `tasks.md`):**
`Actor.cs`, `CoinDrop.cs`, `EnemyProjectile.cs`, `PlayerActor.cs`, `CoinPickup.cs`, `OutlineScript.cs`,
`CombatReadabilityUI.cs`, `PlayerHUD.cs`, `RunRewardUI.cs`, `ArsenalProjectile.cs`, `HitboxDamage.cs`,
`WeaponLoadout.cs` — total **~722 inserções / ~147 remoções** em 13 arquivos.

**Fontes novas (com `.meta` companheiro versionável):**
`Core/FrameThrottle.cs`, `Core/Pooling/` (`SimpleObjectPool`, `ComponentPool`), `UI/HudFormatter.cs`,
`UI/UiValueCache.cs`, `Editor/Cleanup/` (`AssetReferenceScanner`, `OrphanClassifier`,
`ProjectCleanupReport`, `ProjectCleanupRemover`, `TextureImportOptimizer`) e 9 arquivos de teste
EditMode (`*PropertyTests.cs` + `CleanupReportAndFallbackExampleTests.cs`).

---

## (A) MEDIDO no Profiler — **nada mensurável via CLI aqui**

- **Status:** não há medição de Profiler neste ambiente. O Play mode/Editor do Unity não é dirigível
  via CLI (ver Ressalva R10 na Task 15.1 e o `baseline.md`). Portanto, **nenhuma métrica de runtime
  (GC/frame/`Texture Memory`) foi medida** por esta tarefa.
- A medição **pós-mudança** e a comparação antes/depois ficam **a cargo do usuário**, usando a tabela
  e o roteiro já preparados na **§2** deste documento (Task 15.1). Nenhum número é inventado aqui.

## (B) VERIFICADO por inspeção / `git diff` — confirmações concretas

1. **Nenhuma edição em código third-party (R9.5).** `git status` lista apenas arquivos sob
   `Assets/_Project/`. Filtro por `_ThirdParty` e `RealToon` nos arquivos **modificados** retorna
   vazio. O trabalho do FastScriptReload (Task 14.1) foi **config-only / documento de revisão**
   (`fastscriptreload-review.md`) — **nenhuma** fonte do pacote foi alterada; a proposta é gated em
   confirmação do usuário.
2. **Nenhum GUID hand-editado; `.meta`/GUID preservados (R9.2).** **Nenhum** arquivo `.meta` aparece
   como **modificado** (` M`) no `git status` — todos os `.meta` listados são **novos** (`??`),
   companheiros de arquivos de código novos. Os ajustes de import de textura (Task 10.1) são feitos
   via a **API `TextureImporter`/`AssetImporter.GetAtPath(...).SaveAndReimport()`** em
   `TextureImportOptimizer.cs` — a forma que o próprio Unity usa — e o cabeçalho do arquivo declara
   explicitamente que **nunca** recria assets, reescreve linhas de GUID ou toca conteúdo third-party;
   só altera campos do importer (`maxTextureSize`/compressão/mipmaps). (Observação: a passada de import
   em si é aplicada pelo usuário no Editor; a ferramenta e sua garantia GUID-preserving estão
   verificadas por inspeção.)
3. **Remoção remove o `.meta` junto (R7.6 / R9.2).** `ProjectCleanupRemover.cs` deleta **exclusivamente**
   via `AssetDatabase.DeleteAsset(path)`, que remove asset + `.meta` como unidade; o código afirma e
   implementa que **nunca** usa `File.Delete` cru. A remoção é **item-a-item, re-verificada e
   confirmada**; se qualquer referência de GUID existir (`.unity/.prefab/.asset/.meta`) ou o asset
   estiver sob `Resources`, a remoção é **rejeitada** e a origem relatada (R7.4/R7.5).
4. **Mover-em-vez-de-recriar / escopo restrito (R9.3/R9.6).** O `git diff --stat` mostra apenas edições
   pontuais em código de gameplay/UI (pooling, dirty-tracking, hot paths, preload) e arquivos novos
   isolados (pool puro, caches, ferramenta de Editor, testes). Não há renomeação/recriação de assets
   nem refatoração ampla não relacionada.
5. **Companheiros `.meta` presentes para todas as fontes novas.** Varredura de `git status`: **nenhum**
   `.cs` novo está sem seu `.cs.meta`. (Isto resolve/atualiza a pendência registrada na §3 — os `.meta`
   dos testes novos agora aparecem versionados junto.)
6. **Piso automatizável (R10.2) presente e anotado.** Os property tests de lógica pura **P1–P9** e os
   example/edge tests existem e estão anotados com `Validates: Requirements ...` (verificado na §1).

## (C) DEPENDE de playtest manual do usuário — **não validável via CLI** (R9.1, R9.4, R10.1)

Os itens abaixo exigem o Unity Editor do usuário e **não** foram validados automaticamente aqui:

1. **8 cenas abrem sem missing script/asset (R9.4).** Abrir cada uma das 8 cenas no Editor e confirmar
   que **nenhuma** referência quebrada (missing scripts/assets) foi introduzida. Não dirigível via CLI.
2. **Comportamento observável idêntico (R9.1).** Confirmar em playtest que **combate** (projétil do
   player/inimigo, dano, ricochete/pierce/homing/retorno), **câmera/outline no hover**, **UI/HUD**
   (vida/mana/asura, recompensas, legibilidade de combate) e **coleta de moedas** (magnet, bob,
   flourish, valor) permanecem idênticos ao atual para o mesmo estado de jogo — após o pooling e o
   dirty-tracking.
3. **Metas de performance (ligadas à §2 / Task 15.1).** `GC Allocated In Frame` ocioso → ~0 (§6/§7);
   menos/menores picos de GC em combate (§3); `Texture Memory` menor sem perda visual (§9). Tudo isto é
   **medição de Profiler + inspeção visual** do usuário — ver (A) e a §2.
4. **Suíte EditMode verde (R1.5/R10.3).** Rodar **Run All** da `TechGuy.Tests.EditMode` no Test Runner;
   o runner **não** é dirigível via CLI neste ambiente (timeout/sem Editor `6000.5.10f1` instalado).

---

## Declaração explícita do que NÃO pôde ser validado via CLI (R10.1)

- **Execução de testes EditMode/PlayMode** (Run All verde) — Editor não dirigível via CLI.
- **Qualquer métrica de Profiler** (GC/frame/`Texture Memory`), antes e depois — Play mode indirigível.
- **Abertura das 8 cenas** e checagem de missing scripts/assets — requer o Editor.
- **Igualdade de comportamento observável** (combate/câmera/UI/moedas) — requer playtest manual.
- **Aplicação efetiva** da passada de import de textura (Task 10.1) e seu efeito em `Texture Memory` —
  a ferramenta está verificada por inspeção, mas o reimport e a verificação visual rodam no Editor.

Nenhum resultado de teste, de Profiler ou de playtest foi fabricado neste resumo.
