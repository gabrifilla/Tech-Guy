# Baseline de Performance — project-cleanup-optimization

> Artefato versionado do Requisito 1 (Baseline de performance mensurável). Registra as métricas de
> referência observadas **antes** da passada de otimização, para comparação objetiva antes/depois.

## Contexto da medição

- **Data da captura inicial:** 2025-02-14 (consolidada a partir da investigação ao vivo registrada em
  `requirements.md`).
- **Ambiente:** Unity Editor (Play mode) rodando no mesmo projeto Tech Guy. Medições de heap, memória e
  contagens coletadas via **Unity Profiler** (módulos Memory e CPU) durante a investigação.
- **Ferramenta:** Unity Profiler (Memory Profiler / módulo Memory e CPU Usage).
- **Restrição de ambiente (R1.4 / R10.1):** o Play mode do Unity **não** pode ser dirigido de forma
  confiável via CLI neste ambiente (o test runner de EditMode já sofre timeout). Portanto, as métricas
  em tempo de execução — especialmente `GC Allocated In Frame` **em combate** e **tempo de frame CPU
  (médio/pico)** — exigem **recaptura manual** no Profiler durante um playtest. Veja a seção
  "Métricas que exigem recaptura manual".
- **Roteiro de referência (a ser seguido no antes/depois):**
  1. Abrir a cena de jogo e deixar o personagem **parado** (estado **ocioso**) por alguns segundos;
     registrar `GC Allocated In Frame` e tempo de frame.
  2. Entrar em **combate** (disparos do player, inimigos atirando, moedas caindo/coletadas) e registrar
     os picos de GC e o tempo de frame médio/pico.
  3. Registrar as contagens do módulo Memory (objetos/assets/texturas/materiais).

## Valores observados (baseline)

### Heap gerenciado e memória (Profiler — módulo Memory)

| Métrica | Valor baseline | Fonte | Observação |
|---|---|---|---|
| `GC Reserved` (heap gerenciado reservado) | ~1,77 GB | Profiler (investigação) | Anormalmente grande — causa provável dos picos grandes de GC |
| `GC Used` (heap gerenciado em uso) | ~1,41 GB | Profiler (investigação) | — |
| App Committed Memory | ~5 GB | Profiler (investigação) | — |
| App Resident Memory | ~271 MB | Profiler (investigação) | — |

### Alocação por frame (Profiler — CPU/Memory)

| Métrica | Estado | Valor baseline | Fonte | Observação |
|---|---|---|---|---|
| `GC Allocated In Frame` | **Ocioso** | ~997 B em ~15 allocs por frame | Profiler (investigação) | Hot path alocando lixo mesmo quase-parado |
| `GC Allocated In Frame` | **Combate** | _(a recapturar — ver abaixo)_ | — | Requer playtest manual no Profiler |

### Tempo de frame CPU (Profiler — módulo CPU Usage)

| Métrica | Estado | Valor baseline | Fonte | Observação |
|---|---|---|---|---|
| Tempo de frame CPU (médio) | Ocioso / Combate | _(a recapturar — ver abaixo)_ | — | Não quantificado numericamente na investigação; requer playtest manual |
| Tempo de frame CPU (pico) | Combate | _(a recapturar — ver abaixo)_ | — | Sintoma = "stutters grandões"; picos coincidentes com coletas de GC grandes |

### Contagens de objetos/assets (Profiler — módulo Memory, ao vivo)

| Contagem | Valor baseline | Fonte |
|---|---|---|
| Asset Count | 13.382 | Profiler (investigação) |
| Texture Count | 1.100 | Profiler (investigação) |
| **Texture Memory** | ~91 MB | Profiler (investigação) |
| Material Count | 163 | Profiler (investigação) |
| Mesh Count | 30 | Profiler (investigação) |
| Object Count | 16.428 | Profiler (investigação) |
| Scene Object Count | 3.046 | Profiler (investigação) |
| GameObject Count | 579 | Profiler (investigação) |

### Peso de assets em disco (contexto de limpeza)

| Item | Tamanho | Observação |
|---|---|---|
| `Assets/_ThirdParty` | 603 MB (~85% do projeto) | Maior frente de limpeza |
| └ "STYLIZED MALE CHARACTER" | 565 MB | Candidato primário a remoção/trim (sujeito a confirmação item-a-item) |
| └ FastScriptReload | 22 MB | Possível contribuinte de stutter no Editor (R8) |
| └ Blink | 9,6 MB | — |
| └ TextMesh Pro | 4,9 MB | **MANTER** — usado pela UI |
| └ QuickOutline | ~0 MB | **MANTER** — usado por `OutlineScript` |
| `Assets/_Project` | 110 MB | — |
| `Assets/Plugins` | 12 MB | — |

## Métricas que exigem recaptura manual (R1.4 / R10.1)

As métricas abaixo **não** foram quantificadas numericamente na investigação e **não** podem ser medidas
de forma confiável via CLI neste ambiente. Elas devem ser capturadas **manualmente** pelo usuário no
Unity Profiler durante um playtest, seguindo o roteiro de referência acima, e preenchidas nas tabelas
("_a recapturar_"):

1. **`GC Allocated In Frame` em combate** — abrir o Profiler, entrar em combate e registrar a alocação
   por frame e os picos de GC durante disparos/moedas.
2. **Tempo de frame CPU (médio/pico)**, tanto em ocioso quanto em combate — módulo CPU Usage do Profiler.

Essas métricas são declaradas como **validadas por Profiler + playtest manual do usuário**, conforme
R1.4 e R10.1. A medição **pós-mudança** (Task 15.1 / R1.5) deve usar exatamente o mesmo roteiro para ser
comparável.

## Metas (relativas ao baseline — R1.3)

As metas são expressas de forma **relativa** ao baseline acima, evitando números absolutos não
verificáveis neste ambiente:

- **Alocação por frame em ocioso → ~0.** Reduzir `GC Allocated In Frame` em estado ocioso de ~997 B /
  ~15 allocs para **próximo de zero** (meta principal de R2 — HUD/UI sem alocação por frame).
- **Menos e menores picos de GC em combate.** Reduzir a **frequência** e o **tamanho** dos picos de GC
  durante o combate em relação ao baseline (via object pooling de projéteis/moedas — R3 — e hot paths
  mais baratos — R4), reduzindo os "stutters grandões".
- **Pressão de heap menor ao longo do tempo.** Com menos lixo por frame, o crescimento de `GC Used`
  entre coletas deve ser menor que o baseline (coletas menos frequentes).
- **`Texture Memory` menor.** Reduzir a memória de textura reportada pelo Profiler em relação a ~91 MB
  (via otimização de import settings — R6), **sem** degradar a aparência percebida.
- **Tempo de frame CPU em hot paths menor.** Reduzir o custo por frame de `OutlineScript`, `Actor`
  (barra de vida por evento) e lookups repetidos de `GetComponent` (R4), medido no módulo CPU.
- **Sem hitch de primeiro uso.** Eliminar o pico de primeiro acesso na troca de arma e no disparo de
  efeitos via preload/cache de `Resources` (R5).

> Todas as metas acima são validadas via **Profiler + playtest manual** (antes/depois com o mesmo
> roteiro) e por **inspeção/`git diff`**, com relato explícito do que não pôde ser validado via CLI
> (R10). A lógica pura (object pool, formatters, throttle, detector de órfãos) é coberta por property
> tests automatizados em EditMode (R10.2).
