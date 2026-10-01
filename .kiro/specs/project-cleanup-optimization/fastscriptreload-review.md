# Revisão do FastScriptReload — correlação com stutter e proposta de config (config-only, gated)

**Spec:** project-cleanup-optimization · **Task:** 14.1 · **Requisitos:** 8.1, 8.2, 8.3, 8.4
**Natureza:** investigação + proposta escrita. **Nenhuma mudança de configuração foi aplicada.**
Qualquer alteração proposta aqui está **gated em confirmação explícita do usuário** e é **config-only**
(sem editar o código-fonte do pacote third-party — R8.3, AGENTS.md).

---

## 1. Escopo e restrições (o que esta tarefa faz e não faz)

- **Faz:** localiza, no pacote `Assets/_ThirdParty/FastScriptReload`, onde vive o ajuste de
  "auto-reload durante o Play"; identifica o mecanismo exato que gera o log *"full reload will be
  triggered"*; documenta o toggle config-only que desabilitaria o auto-reload durante o play; e
  registra se há evidência de correlação com os picos de frame no Editor.
- **Não faz:** não altera nenhuma configuração; não edita código third-party; não roda o Editor/Play
  via CLI (não é confiável neste ambiente — R10.1). A correlação definitiva **depende do timeline do
  Profiler + Editor.log do usuário** (ver §4 e §6).

---

## 2. Onde vive a configuração (sem tocar no código)

O FastScriptReload **não** usa um asset de settings versionado (não há `.asset`/`ProjectSettings`
próprio no repositório). Todas as opções são **preferências de Editor** persistidas via EditorPrefs
(chaveadas pelo nome do projeto `"fast-script-reload"`), definidas na janela de boas-vindas da
ferramenta.

Definições relevantes (apenas **referência de leitura** — não editar o arquivo):

- `Assets/_ThirdParty/FastScriptReload/Scripts/Editor/FastScriptReloadWelcomeScreen.cs`
  — classe `FastScriptReloadPreference`, onde ficam declarados os toggles.
- `Assets/_ThirdParty/FastScriptReload/Scripts/Editor/FastScriptReloadManager.cs`
  — consome os toggles em runtime de Editor (loop de reload, domain reload).

### Toggle alvo (auto-reload durante o Play)

```csharp
// FastScriptReloadWelcomeScreen.cs (class FastScriptReloadPreference)
public static readonly ToggleProjectEditorPreferenceDefinition EnableAutoReloadForChangedFiles =
    new ToggleProjectEditorPreferenceDefinition(
        "Enable auto Hot-Reload for changed files (in play mode)",
        "EnableAutoReloadForChangedFiles", true); // <- default: ligado
```

- **Rótulo na UI:** *"Enable auto Hot-Reload for changed files (in play mode)"*
- **Chave de preferência:** `EnableAutoReloadForChangedFiles`
- **Default:** `true` (ligado) — ou seja, por padrão o projeto **tem** auto-reload durante o play.
- Consumido em `FastScriptReloadManager.cs` (o loop só dispara o reload automático quando este toggle
  está `true` **e** passou o intervalo de `BatchScriptChangesAndReloadEveryNSeconds`).

### O log *"full reload will be triggered"*

Há **duas** fontes distintas de "full reload" no pacote — importa não confundi-las:

1. **Aviso de auto-refresh de assets do Unity** (origem exata do texto do requisito):
   `FastScriptReloadWelcomeScreen.cs` → `EnsureUserAwareOfAutoRefresh()` loga
   *"Fast Script Reload - asset auto refresh enabled - full reload will be triggered unless editor
   preference adjusted - see documentation for more details."*
   Isso dispara quando o **Auto Refresh do próprio Unity** (`kAutoRefreshMode`) está habilitado: ao
   salvar um script, o Unity recompila e faz **domain reload completo** — um pico grande e clássico de
   stutter no Editor. A "preference" citada aqui é a do **Unity** (Preferences → Asset Pipeline →
   Auto Refresh), não um código do pacote.

2. **Domain reload forçado após N hot-reloads** (mecanismo interno do pacote):
   `FastScriptReloadManager.cs` → `TriggerReloadForChangedFiles()` loga
   *"Dynamically created assembles reached over: N - triggering full domain reload to clean up…"*
   controlado pela preferência:

   ```csharp
   public static readonly IntProjectEditorPreferenceDefinition
       TriggerDomainReloadIfOverNDynamicallyLoadedAssembles =
       new IntProjectEditorPreferenceDefinition(
           "Trigger full domain reload after N hot-reloads (when not in play mode)",
           "TriggerDomainReloadIfOverNDynamicallyLoadedAssembles", 50);
   ```

Ambos convergem no mesmo sintoma observável: **domain reload = pico grande de frame no Editor**.

---

## 3. Hipótese de causa (por que isto plausivelmente gera stutter)

- Um **domain reload** recarrega todos os assemblies gerenciados; é uma das operações mais caras do
  Editor e produz exatamente o tipo de "stutter grandão" descrito no `requirements.md`.
- Com `EnableAutoReloadForChangedFiles = true` (default), **salvar um script durante o Play** aciona um
  hot-reload automático. Dependendo do modo de Auto Refresh do Unity e do acúmulo de assemblies
  dinâmicos (toggle `TriggerDomainReloadIfOverN…`), isso pode escalar para um **full domain reload**,
  interrompendo o frame.
- **Importante:** o hot-reload do FastScriptReload só dispara **quando um arquivo muda** (salvar
  script). Ele **não** aloca por frame nem roda no hot path de gameplay. Portanto ele **não** é a causa
  da pressão de GC descrita no requisito principal (heap ~1,77 GB, ~997 B/frame ocioso) — essa pressão
  vem do HUD/UI e de Instantiate/Destroy (Tasks 2–9). O FastScriptReload é um **suspeito separado**: um
  stutter **episódico de Editor** correlacionado a **salvar scripts**, não ao combate em si.

---

## 4. Evidência de correlação — status: **INCONCLUSIVO via CLI**

Para correlacionar o auto-reload com picos de frame é preciso cruzar **duas** fontes temporais:

1. As entradas de log do FastScriptReload (*"full reload will be triggered"* / *"triggering full
   domain reload"*) com **timestamp**, no `Editor.log` do Unity.
2. O **timeline do Unity Profiler** no mesmo instante, mostrando o spike de CPU/frame.

Nenhuma dessas fontes pode ser capturada de forma confiável neste ambiente:

- O Play mode / Editor do Unity **não é dirigível via CLI** aqui (o próprio test runner de EditMode
  sofre timeout — R10.1).
- Uma busca no repositório pelo texto *"full reload will be triggered"* só encontra a **string de
  origem** no código do pacote e as menções na própria spec — **não** há `Editor.log` nem captura de
  Profiler versionada que prove a coincidência temporal.

**Conclusão desta tarefa:** a correlação é **plausível pelo mecanismo** (domain reload ⇒ spike), mas
**não há evidência medida** no repositório que a confirme. A confirmação definitiva **depende do
usuário** reproduzir o roteiro de §6 com Profiler + Editor.log abertos.

---

## 5. Proposta (config-only, **requer confirmação do usuário** — R8.2/R8.3)

> Nada abaixo foi aplicado. São passos manuais na UI da ferramenta, reversíveis, sem tocar no código.

**SE** o usuário observar que os spikes coincidem com os logs de reload ao salvar scripts durante o
Play (§6), então, em ordem de menor para maior impacto:

1. **Preferido — desabilitar o auto-reload durante o Play (toggle da ferramenta):**
   `Window → Fast Script Reload → Start Screen` → desmarcar
   **"Enable auto Hot-Reload for changed files (in play mode)"**
   (preferência `EnableAutoReloadForChangedFiles`). Isso remove o reload automático em play sem
   remover a ferramenta; hot-reload sob demanda continua disponível. É **config-only** (EditorPrefs),
   **reversível** e **não** edita o pacote.

2. **Complementar — ajustar o Auto Refresh do Unity** (não é config do pacote, mas é a origem do log
   do item 1 em §2): `Edit → Preferences → Asset Pipeline → Auto Refresh` para *Enabled Outside
   Playmode* (ou desligado), evitando domain reload ao salvar durante o Play. Também reversível.

3. **Alternativa fina — elevar/baixar o limiar de domain reload:**
   `TriggerDomainReloadIfOverNDynamicallyLoadedAssembles` controla de quantos em quantos hot-reloads
   um full domain reload é forçado. Mexer aqui é mais sutil e **não** recomendado antes de confirmar a
   causa.

**Recomendação se a evidência for coletada e confirmar a causa:** aplicar **apenas o passo 1** (+ passo
2 se necessário), um de cada vez, com confirmação. **Se a evidência não for coletada ou for
inconclusiva:** **manter o estado atual** (nenhuma mudança às cegas — R8.4). Dado que, no momento, a
avaliação está **inconclusiva** (§4), o estado atual **permanece inalterado**.

---

## 6. Roteiro para o usuário coletar a evidência (dirigível só manualmente)

1. Abrir o **Unity Profiler** (modo Timeline/CPU) e deixar gravando.
2. Entrar em **Play** no Editor e manter o jogo rodando alguns segundos (frame estável).
3. Editar e **salvar** um script de gameplay (ex.: um `Debug.Log` em algum `Update`).
4. Observar: aparece no Console/`Editor.log` o aviso de reload (*"full reload will be triggered"* /
   *"triggering full domain reload"*)? Nesse mesmo instante há um **pico grande** de frame no Profiler?
   - **Sim para ambos** ⇒ correlação confirmada ⇒ aplicar §5 passo 1 (com confirmação).
   - **Não / sem coincidência** ⇒ inconclusivo ⇒ manter o estado atual (R8.4).
5. Opcional: salvar as evidências (print do Profiler + trecho do `Editor.log` com timestamps) junto
   desta spec para registro.

---

## 7. Decisão registrada

- **R8.1 (correlação + evidência):** mecanismo identificado e documentado; **evidência medida ausente**
  no repositório — correlação **inconclusiva via CLI**, depende do Profiler/Editor.log do usuário.
- **R8.2 (proposta config-only):** documentado o toggle exato
  `EnableAutoReloadForChangedFiles` ("…in play mode", default `true`) a desmarcar na janela da
  ferramenta, **se** a causa for confirmada.
- **R8.3 (sem editar código / gated):** nenhuma mudança aplicada; proposta é 100% config-only e
  **requer confirmação do usuário**; o código third-party **não** foi tocado.
- **R8.4 (inconclusivo ⇒ manter estado):** como a correlação está inconclusiva neste momento, o
  **estado atual é mantido**, sem alteração às cegas.
