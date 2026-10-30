# RUN-EXP-01 — Etapa 0: correções críticas

**Implementação localizada e validação focada concluídas em30/10/2026; regressões globais e aceite humano pendentes.** O checkpoint inicial de29/10 abaixo é histórico onde divergir do fechamento.

## Fechamento após retomada

- Retomada executada e automação única excluída. Nenhuma expansão de boons/controles/economia nesta etapa.
- **10/10 finais** no job `58493f83f2144543bdc90e15ebb9a22a` (9 casos RUN00 e1 publicado), zero ignorados. [Resultado completo](../validation/artifacts/run-exp-01/critical-final-58493f83.json).
- Hooker real/câmera publicada: job `49e010dbf1c340ffbceb968265319c5c`,1/1. Capturas e CSV em `Docs/validation/artifacts/run-exp-01/stage0/hooker-20261030-045414/`. Linha temporal: aviso0,0000; pull0,6002; segundo contato1,6026 sem reiniciar controle; liberação2,1030; contato2,6043 ignorado na janela; próximo pull3,6071. Há1,5041s de controle nessa sequência, não apenas um frame. Movimento/dash reais de input virtual estão nos testes focados separados.
- A fixture usa prefab e executor produtivos, mas suspende IA/outros atores e dirige a sequência. No teste isolado de quebra, o controller de reação foi anexado explicitamente e configurado para1 de postura. Isso **não** significa prefab produtivo sem reação: `ProgressionDirector.Manual.cs:164` adiciona/configura a autoridade no spawn normal. O prefab sozinho não a serializa. Não generalizar a fixture à run natural.
- Primeiras tentativas publicadas foram0/1: uma duração limitada a240frames não cobria4,5s em Editor rápido (`a1588f2b34144726954fecf83065619d`); depois faltava o controller que o adaptador de spawn normalmente adiciona (`69058685d6bd440eaf0f0e776729722a`, `9056c0249a0d4d45b3f10b4566cbc2ac`). Foram corrigidas premissas de fixture, não reduzida a exigência de recuperação ou alterado tuning. Não atribuir essas falhas à correção do pull.
- Baseline causal dos31 casos que falharam: arquivo PlayerActor anterior à frente foi restaurado temporariamente com backup binário e a correção reposta antes do encerramento. Job `224a96b3d7c44fdba42321f1a0f035c7`:6/31 passaram,25 falharam. Com correção, job `69058685d6bd440eaf0f0e776729722a`:6/31 passaram,25 falharam (mais5/6 checks adicionais aprovados; Hooker foi corrigido na fixture depois). **24 nomes falham nos dois estados;5 passam nos dois;2 variam.** [Comparação](../validation/artifacts/run-exp-01/comparison.json), [baseline](../validation/artifacts/run-exp-01/stage0/baseline-224a96b3.json), [reexecução](../validation/artifacts/run-exp-01/stage0/focused-69058685.json).
- Uma variação é RewardsOfferExecutableMutations, que pressupõe um card específico em oferta RNG; outra é ManualLastEcho, que passou com a correção na reexecução. Não interpretar essas duas variações como prova causal nem modificar RNG para aprovar. Testes antigos de básico também leem HP/Asura imediatamente após aceitar um ataque, antes do contato do scheduler aprovado. Lifecycle usa gate de Mutation que depende do catálogo e do fluxo antigo. São dívidas concretas de fixture/contrato; casos não explicados integralmente continuam investigação, não selos verdes.
- Três falhas de captura eram recusa correta de sobrescrever evidência em SessionState reutilizado. Configurado diretório novo na chave **TitanicCompositionEvidence** (GprPhase é outra chave). Job `9056c0249a0d4d45b3f10b4566cbc2ac`: os3 checks de composição/tickets passaram; seu quarto caso Hooker só passou após o ajuste de preparação acima. Nenhum artefato antigo foi sobrescrito.
-8/32/64 e snapshot/pool passaram no job69058685:5/5. Novas medidas em `Docs/validation/artifacts/gameplay-rework/run-exp-01/stage0/final-20261030-045056/`: pool2/dois tornados; CPU p95 do marcador0,2984/0,4065/1,0757ms; frame p95 Editor3,6293/3,2941/5,8779ms. A64:32 vítimas,64 descartes, máximo32 impactos por frame. Isso é fixture de alvos/Protocols, não64 agentes reais nem GPU/player. Não alterar caps ou dano para evitar descarte.
- Checker da skill executado por cópia-wrapper local que adapta **somente formatação de caminho** entre drives.2.551 arquivos,1 erro e1 warning: script GUID ausente em Playground.unity e MonoBehaviour de sampler em arquivo de testes com outro nome. Nenhum erro aponta aos assets novos. O GUID ausente já consta no Playground do HEAD; não corrigir cena legada fora do escopo. [Saída integral](../validation/artifacts/run-exp-01/integrity-final.json). Integridade global continua com ressalva.

Etapa0 não exige redesign maior para os dois fixes e permitiu seguir para auditoria/protótipos. Não existe aprovação física do novo fogo/tripla, nem regressão global totalmente verde;322/353 permanece como execução ampliada, sem soma seletiva de reexecuções para fingir353/353.

Verificação documental/compatibilidade final: job EditMode `07f6c50e43d8432c8009e25f4d44a4a6`,37/37,0 ignorados,35,427s, filtros `TechGuy.Tests.Fluidity.EditMode.{Gpr015RewardAccessibilityTests,BoonOfferCompatibilityTests,ManualRunRandomizerTests,ProceduralRunGeometryPlanTests}`. Duas tentativas anteriores usaram namespace incorreto e descobriram0 casos; não são evidência de sucesso. Nenhum peso/frequência de reward foi ajustado para esses testes.

Estado final confirmado por execução CodeDom no Editor: `playing=false`, `compiling=false`, NexusBlockout `dirty=false`, estudo fora deBuildSettings, assistência preexistente `true`. Roslyn da ferramenta execute_code apresentou MissingMethodException em System.Reflection.Metadata após testes; CodeDom executou a mesma consulta com sucesso. Isso é limitação do compilador dinâmico da ferramenta, não falha de compilação dos scripts que passaram no runner. `git diff --cached --check` sinaliza espaços de serialização Unity em `.meta/.unity/.mat` novos e patches históricos de evidência; C#/Markdown são conferidos separadamente, sem reescrever prova histórica ou GUIDs para limpar estilo.

## Escopo e preservação

Frente independente de GPR: **RUN-EXP-01 — experiência de incursão**. O pedido direto autoriza correções críticas, depois auditoria, depois protótipos isolados e STOP. GPR-01 continua tecnicamente demonstrada com suas ressalvas; a falha histórica 203/204 e o não reconhecimento humano do fogo não foram reclassificados como aprovação.

Projeto real confirmado por Unity MCP: `D:/Dev/Personal/My_games/Tech-Guy`, branch `gameplayOverhaul`, HEAD inicial `653db8b`, Unity `6000.5.10f1`, instância `Tech-Guy@fccafb2b47cd7671`. Cena inicial `NexusBlockout`, Edit Mode, pronta. O worktree do chat em `C:/Users/gabri/.codex/worktrees/a6f0/Tech-Guy` está no commit antigo `67d8065`; ele não contém os sistemas desta solicitação e não é o alvo de implementação.

Havia alterações e arquivos novos anteriores à tarefa. Snapshot do diff inicial e listas em `Library/RunFrontCheckpoint/`. O usuário autorizou posteriormente commit/push de **todas** as alterações do projeto ao concluir as três etapas, seguido de desligamento; não executar essas ações enquanto a tarefa estiver incompleta. Não houve alteração autorada de cenas, prefabs, GUIDs, saves, preferências, economia ou controles gerais nesta etapa.

## A — cadeia de atração

Origem verificada: `EnemyCombatActions.HookLine` do Hooker. O ataque compromete linha de 12 m, tolerância de 1 m, aviso de 0,6 s; conexão chama `PlayerActor.BeginExternalPull` com duração máxima de 1,5 s. O ataque recupera por 0,4 s; a IA adiciona sua cadência. Não há aplicação de stun de postura nesse caminho: o bloqueio específico vem do pull e da desativação de `CharControlScript`, com limpeza de combo/target/buffer. Dash consulta o gate desse controlador; deslocamento do dash também detecta pull.

**Causa reproduzida:** cada conexão substituía a coroutine de pull anterior e reiniciava sua duração. Dois executores reais de HookLine, defasados, mantinham a perda de controle além do limite de um pull. Isso não foi inferido apenas de timers: a sequência ofensiva de produção foi executada em PlayMode sobre jogador/NavMesh reais da fixture.

Correção localizada em `PlayerActor`: pull ativo não é substituído; recuperação de **0,6 s de gameplay** bloqueia somente outro pull externo e não concede imunidade a dano/stagger/CC geral. O relógio suspende com pausa/hitstop. Liberação preserva ownership do controlador; fonte morta/inativa/interrompida encerra o efeito; disable/reativação do jogador limpa a coroutine e devolve somente o controle suspenso por ela. Estado ativo explícito evita depender de um handle de coroutine já encerrada sincronicamente. Durações inválidas/nulas são recusadas. Duração/deslocamento do pull original e tempos/dano dos demais ataques não foram retunados.

Os gates atuais de input direto exigem soltura e nova pressão após desativação; esse contrato geral não foi removido. A fixture demonstra nova pressão de movimento e dash dentro da oportunidade, não aceitação automática de tecla permanentemente segurada.

## B — fogo do tornado

Captura nova da câmera publicada confirmou vento branco dominante e traços laranja internos. Antes da intervenção, os quatro comparadores de produção passaram: sucesso técnico não demonstra reconhecimento humano. A autoria existente usa `GauntletExecutionSnapshot`/política aceita, três fitas de fogo com MaterialPropertyBlock e pool com geração; nenhuma alteração de snapshot, ranks, dano ou oportunidade Detonation foi feita.

Adicionada apresentação geométrica animada: oito línguas de chama com ponta e oscilação, malha externa laranja e núcleo quente, dupla face, arrays fixos e duas malhas reutilizadas por tornado. As fitas continuam presentes; vento é mais fino somente quando há fogo, conservando contorno branco e raio elétrico azul. Material URP Unlit opaco evita ordenação de transparência como requisito do novo volume. Não há colliders, dano, burn/DoT, consulta de física ou novo produtor de combate. Geometria visual não redefine raio/altura/velocidade/alcance/TTL/dano mecânicos. Release/clear desabilitam malhas; reutilização sem Detonation permanece sem fogo.

Duas rodadas visuais foram comparadas na câmera real. A segunda ocupa mais área laranja/amarela e deixa o vento como contorno. **Reconhecimento artístico pelo desenvolvedor continua pendente.** PNGs não demonstram sozinhos o movimento das chamas, nem aprovam legibilidade durante uma partida humana.

## Execuções Unity observadas até este checkpoint

| Job | Filtro / resultado observado |
|---|---|
| `7b9b297b04864bbcb1175e82d3f1ecdf` | Primeira tentativa RUN00 antes de importação: **0 testes**, não é validação. Depois foi feito refresh completo. |
| `536c164f61e447158a42b8ad989a8f34` | RUN00 antes da correção: **1/3 aprovado, 2 falhas**; sobreposição e dois HookLine reais não devolvem controle. Caso duração zero já passava. |
| `780a64160c4740a483feec5eaa6f945f` | Mesmos três casos após correção: **3/3**, incluindo movimento/dash. |
| `a090032f409243eb8077ca3737c8d5e1` | Quatro `Gpr015ProductionAuditTests.GPR015GameView*InPublishedCombat`, antes do fogo novo: **4/4**. |
| `b8b7eb0b00204e90a3c2e0d77a6e9897` | RUN00 expandido: **6/6**; pausa/hitstop, disable/ownership e geometria/pool de fogo incluídos. |
| `9281e581a4964a84838fe038541264e6` | Quatro comparadores publicados após primeira rodada geométrica: **4/4**. |
| `83b0e92087454cbab4ae0cefd9955271` | Quatro comparadores publicados após segunda rodada: **4/4**. |
| `243f6a6df28144cb8f581b52514616fc` | BasicAttackR1PlayModeTests + ArchetypeIntegrationTests + EnemyPhysicalAttackTests + PostureReactionPlayModeTests: **322/353**, 31 falhas, zero ignorados, duração403,206s. **Os nove RUN00 passaram**, incluindo os três casos de lifecycle/repetição adicionados por último. Nomes/estados de todos os casos e mensagens/stacks das falhas em [resultado bruto](../validation/artifacts/run-exp-01/stage0/regression-243f6a6d.json). Não é regressão verde. |

Os comparadores publicados carregam FirstSector/câmera normal/HeavySlam real, mas **assistem** a aquisição de boons, posições/HP dos inimigos e desativam sua IA para a composição. Não são aquisição natural nem playtest humano. Os quatro percursos/tickets da GPR anterior são evidência histórica, não reexecução desta frente.

## Artefatos visuais novos

Preservados em diretórios com timestamps do writer existente, sem sobrescrever artefatos antigos:

- Baseline: `Docs/validation/artifacts/gameplay-rework/gpr-01-5/c/20261030-021842-5371621` (AB), `20261030-021845-4780502` (AC), `20261030-021848-3355089` (ABC), `20261030-021851-2910194` (A).
- Primeira rodada: `20261030-022129-8428935` (AB), `20261030-022132-7387660` (AC), `20261030-022135-5827730` (ABC), `20261030-022138-4352419` (A), no mesmo diretório pai.
- Segunda rodada: `20261030-022258-8402001` (AB), `20261030-022301-8303955` (AC), `20261030-022304-7973688` (ABC), `20261030-022307-6746736` (A), no mesmo diretório pai.

Cada diretório contém viagem/aviso em dois instantes, evento confirmado, cleanup, contexto e recibos causais. Os PNGs AC/ABC foram abertos e inspecionados pelo agente. Comparações têm composição assistida; número/ações de outros inimigos podem variar, não há certificação pixel a pixel de experimento determinístico.

## Pendências no checkpoint inicial — resolução no fechamento acima

- Consultar e registrar resultado integral do job ampliado; separar falhas temporais herdadas das regressões demonstradas, sem enfraquecer testes.
- Os nove casos RUN00 passaram na bateria ampliada; resta diagnóstico das31 falhas externas ao filtro novo e comparação causal onde necessário. Não classificá-las todas como herdadas sem evidência.
- Captura/timeline de Hooker com prefab real na Game View publicada; fixture de execução acima não substitui essa evidência.
- Conferir interrupção por reação legítima e custos gráficos novos sob 8/32/64 inimigos, respeitando limites de Editor.
- Conferir Console/compilação final e restaurar Editor/cena; não alterar configurações temporárias do Test Runner enquanto ele executa.
- Atualizar ledger e matriz com resultados finais; ainda não feitos neste checkpoint.

Console inicial: oito warnings de bindings legados `AbilityHolder`. Durante edição de classe parcial houve falha do **FastScriptReload** (`BuildDirectRig` não encontrado na montagem parcial temporária e acesso a FindObjectsOfType fora da main thread); compilação normal subsequente e início dos testes funcionaram. Não foi limpo o Console nem alterado o plugin. Uma chamada de teste a `ImpactFeedbackScope.Contact` inexistente foi corrigida antes de executar a bateria, para `ConfirmDamage(true)`.

Checker externo da skill `unity-csharp-scripting` foi tentado com Python3.14. Falhou na resolução de pacote Tripo3D fora do projeto: `os.path.relpath` entre C: e D: lança ValueError. Não é resultado de integridade verde. `.meta` dos dois scripts novos existe após importação real. Editor confirmado depois da bateria: NexusBlockout, EditMode, sem compilação/teste em curso; diffs temporários de EditorBuildSettings/ProjectAuditorSettings foram restaurados pelo runner (sem diff remanescente).

## Arquivos desta etapa e skills

Mudanças desta frente até agora: `PlayerActor.cs`, `TitanicFistsVisual.cs` (já tinha alterações anteriores preservadas), novos `TitanicFlameSilhouette.cs` e `RunFrontCriticalPlayModeTests.cs`, seus `.meta` gerados pelo Unity e este relatório/checkpoint. Não atribuir à frente os demais diffs anteriores.

Skills aplicadas: router; unity-mcp-orchestrator; unity-csharp-scripting; game-feel; locais tech-guy-combat-unity, tech-guy-unity-acceptance, tech-guy-game-vision, tech-guy-boon-design. OpenAI Docs usada somente para a retomada agendada. Skills específicas de mundo/layout/encontros e pipeline artístico serão consultadas na etapa correspondente.
