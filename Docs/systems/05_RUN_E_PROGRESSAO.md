# Run, recompensas e conclusão

Fonte documental principal de rotas, recompensas e ciclo de run. A intenção de produto e a implementação observada são registradas separadamente.

## Experiência-alvo

Em uma run, a montagem da build deve acontecer por escolhas compreensíveis que alterem o comportamento; elite opcional e recompensas têm consequência anunciada, sem permitir duplicação/farm não intencional. O jogador deve entender por que ganhou/perdeu.

## Invariantes de produto e verificação

- Recompensa concedida **uma vez** por evento/nó/encounter conforme identidade persistente do sistema real.
- UI de seleção e salvamento devem respeitar cancelamento, reinício, extração, morte e replay sem gerar escolhas duplicadas.
- As habilidades base e progressão não devem depender de uma sinergia rara para funcionar.
- A run deve oferecer oportunidades reais de montar combinações; conferir distribuição observada por simulação de sementes e playtests, não apenas tabelas teóricas.
- O hub Nexus e as salas requerem navegação consistente, mas detalhes exatos de topologia e contagem pertencem à versão atual do projeto.

## Implementação inspecionada em 18/10/2026

Leitura de código e YAML de cenas/assets no workspace `gameplayOverhaul`, base `3a3f0ee` com alterações locais; sem abrir o Editor ou executar testes nesta consolidação.

| Parte | Comportamento e fonte |
|---|---|
| Cena jogável | [`FirstSector.unity`](../../Assets/_Project/Scenes/FirstSector.unity) serializa `_progressionMode: 1` (`ManualSlice`) e `_randomizeRooms: 1`, com definição [`FirstSectorManual.asset`](../../Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset). Não corresponde à antiga descrição de três encontros lineares. |
| Geração | [`ProgressionDirector.Manual`](../../Assets/_Project/Scripts/Core/Run/ProgressionDirector.Manual.cs) valida rota, loadout e ambiente antes de inicializar. [`ManualRunRandomizer`](../../Assets/_Project/Scripts/Core/Run/ManualRunRandomizer.cs) conserva o grafo e as escolhas exclusivas da definição, variando posições, dimensões, composição de encontros e recompensas por seed; `ManualStageEnvironment` materializa as salas. `ProceduralLegacy` continua um modo alternativo no código. |
| Caminhos | [`ManualRoutePolicy`](../../Assets/_Project/Scripts/Core/Run/ManualRoutePolicy.cs) sela a passagem escolhida e exclui o ramo alternativo. A definição usada na simulação V2 possui quatro caminhos; isso não significa quatro fases ou escolha aleatória de preferência do jogador. O boss é bloqueado enquanto a sessão não adquiriu uma Mutation. |
| Recompensas geradas | O primeiro encontro comum elegível concede uma Technique e um Protocol. Nó de Mutation preserva uma aquisição de Mutation; demais encontros comuns geram uma ou duas aquisições, elites duas ou três, alternando Technique/Protocol. Boss não gera essa seleção. São regras encontradas no randomizador, não novos compromissos de balanceamento. |
| Fronteira do encontro | [`ProgressionDirector.Encounter`](../../Assets/_Project/Scripts/Core/Run/ProgressionDirector.Encounter.cs) aguarda a fronteira mecânica/feedback confirmado e interrompe a ofensiva antes de resolver o encontro; limpa o contexto antes de preparar/ofertar recompensas. A última morte só marca uma candidata à conclusão. Spawners, atores produzidos e trabalho causal pendente precisam ser considerados. Não conceder recompensa diretamente por qualquer callback `Died`. |
| Recursos | [`ProgressionDirector.Resources`](../../Assets/_Project/Scripts/Core/Run/ProgressionDirector.Resources.cs) preenche vida/mana na inicialização uma vez. Recalcular stats preserva quantidades atuais; reparo é uma oportunidade resolvida uma vez, podendo ser dispensada. Não implica cura automática entre todos os encontros. |
| Fim e retorno | [`ProgressionDirector.Lifecycle`](../../Assets/_Project/Scripts/Core/Run/ProgressionDirector.Lifecycle.cs) diferencia morte, abandono e extração após boss; encerra a sessão/efeitos e tenta carregar Nexus. Falha de retorno fica explícita e permite retry. [`ManualRunUI`](../../Assets/_Project/Scripts/UI/ManualRunUI.cs) mostra resultado e estado de retorno. |
| Persistência | [`WeaponLoadout`](../../Assets/_Project/Scripts/Weapons/WeaponLoadout.cs) persiste seleção/desbloqueios em `PlayerPrefs`; boons/rota são estado da sessão. Não foi localizado neste fluxo um save/load de run interrompida que justifique prometer retomada. |

## Contrato de recompensa implementado

[`RunRewardPolicy`](../../Assets/_Project/Scripts/Core/Run/RunRewardPolicy.cs) identifica cada pedido por **sessão + nó + ordinal**, com estados `Pending`, `Committing`, `Resolved` e `Faulted`. Reabrir a seleção com inventário idêntico preserva escolhas; mudança de inventário incrementa a revisão e invalida clique antigo. A seed de recompensa deriva da seed de rota (ou identidade da sessão), nó, ordinal e revisão, sem consumir RNG compartilhado.

O efeito é aplicado antes de avançar o ordinal; somente ticket resolvido publica conclusão. Erro durante commit deixa `Faulted`, com diagnóstico, e exige recuperação/abandono da sessão. Isso evita nova concessão por repetir confirmação, mas não deve ser descrito como rollback transacional de qualquer efeito já aplicado.

Sem elegível, o ticket pode oferecer Protocol alternativo/continuar. A primeira Mutation obrigatória não recebe esse fallback: sem escolha válida o boss permanece bloqueado e a sessão deve ser abandonada. Fechar a janela preserva a pendência; input precisa ser solto antes de confirmar a próxima escolha. Ver [`RunBoons.Manual`](../../Assets/_Project/Scripts/Core/Run/RunBoons.Manual.cs) e o [contrato de boons](04_BOONS.md).

## Estatísticas de fim de run

**Decisão de produto:** tela com dados legíveis, especialmente **dano por habilidade**; usar *conteúdo analítico* de Megabonk com apresentação coerente com Diablo/Hades. Proposta de conteúdo para validação com jogador: tempo, encontros, dano total, breakdown de skills, fontes de dano recebido, eliminados, boons adquiridos e destaques/sinergias.

**Cuidado técnico:** métricas por habilidade devem atribuir efeitos derivados sem dupla contagem; uma única fonte de cálculo e totalizadores reconciliáveis. Não inventar ranking de skill a partir de último hit. Se fontes forem incertas, rotular como estimativa ou adiar.

**Atualização VS-02B.2:** relatório detalhado e consulta no Nexus implementados sobre o backend VS-02A, sem mudar políticas de rota/recompensa; fluxo e limites atuais em [HUD/UI](06_HUD_E_UI.md#vs-02b2--resultados-no-unity-22102026). A auditoria de20/10 abaixo permanece histórica.

**Auditoria do Vertical Slice, 20/10/2026:** o resultado básico é apresentado por `ManualRunUI` na cena da run durante `Returning`, inclusive retry. `ProgressionDirector.Lifecycle` encerra a sessão e carrega NexusLobby; não foi localizado transporte de resumo para o hub. No smoke real de abandono, o Nexus carregou sem `ProgressionDirector`/`ManualRunUI` e sem painel de resultado. Portanto, **retornar ao Nexus funciona, consultar ali o resultado da última run ainda é uma lacuna**, distinta do breakdown analítico. Não confundir saldo persistente de moedas/arma selecionada com relatório da run. Evidências e limites na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md); escopo da entrega mínima ainda depende do [plano](../03_PRIORIDADES.md).

## Evidência e pendências

Resultados históricos Unity de 18/10 cobrem partes dos contratos e a simulação da distribuição; estão sintetizados na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md). A simulação V2 usa o randomizador, política de tickets e catálogos publicados, mas pressupõe vitória em todos os encontros e força distribuição igual dos quatro caminhos. Não mede duração, sobrevivência ou preferência humana. Nada nesta consolidação comprova um smoke atual do caminho completo ou aprovação de gameplay.

## Aceite

Teste completo do caminho hub → encontro → reward → build transformada → boss/fim → estatísticas → retorno, incluindo morte/abandono e repetição. Conferir screenshot e integridade dos números do relatório com eventos brutos.


## Lifecycle do RunSummary — VS-02A (21/10/2026)

A coleta de dados foi autorizada nesta etapa; **a interface final continua fora do escopo**. O contrato de atribuição e denominadores está em [telemetria](../validation/03_TELEMETRIA_E_GAME_FEEL.md#run-analytics--contrato-vs-02a-21102026), sem outra fonte de totalização.

`ProgressionDirector.Statistics` marca o início da sessão manual e cria o observador após vincular `RunBoons`/inicializar recursos, antes da liberação de input. Registra inimigos autorados e produzidos, fases e tickets efetivamente resolvidos. `CurrentRunSummary` devolve cópia de dados primitivos da sessão atual. O teardown finaliza **antes** de limpar boons, cancelar/remover inimigos e descarregar a cena; `OnDestroy` reaproveita a finalização idempotente com fallback Aborted. Outcome `Extracted` produz Victory, `Dead` produz Death e `Aborted` permanece Aborted.

`RunSummaryStore.TryRead(out summary, out diagnostic)` recupera o último resultado concluído no Nexus, sem exigir o diretor da cena anterior. Chave separada e versionada `TechGuy.RunSummary.v1` no PlayerPrefs existente; não modifica formatos/chaves permanentes de moeda, loadout ou desbloqueios. Valida versão, resultado, valores finitos e reconciliação antes de aceitar o resumo. Dado ausente/corrompido/incompatível retorna diagnóstico, sem inventar zero nem migrar saves. Falha de gravação é explicitada e não altera decisões de combate. Uma nova run tem nova identidade/coletor e substitui o resultado persistido apenas quando concluída; não existe histórico ilimitado nem save/resume de run.

Campos disponíveis: sessão/seed/arma inicial, outcome/duração, mortes observadas/crédito letal, HP efetivo/overkill, partições por habilidade/produtor/Mutation/modificadores, usos/transações positivas, maior impacto, HP recebido, build adquirida, recompensas/fases e destaques factuais. A futura VS-02B deve consumir esses fatos e rotular lacunas; não recalcular dano a partir de eventos de HUD.


### Investigação do último resumo — VS-02B (22/10/2026)

Inspeção do código e MCP somente leitura; nenhuma mudança no Store. Há um escritor de produção: `RunStatistics.Finish` → `RunSummaryStore.TryWrite` → `PlayerPrefs.SetString/Save`. `ProgressionDirector` o chama no teardown antes do unload e no OnDestroy (fallback Aborted); guardas de Finalized/teardown e SessionId impedem repetição do mesmo coletor. Store valida os fatos, mas não compara ordem cronológica nem registra autor/instante da gravação. Portanto, “último resultado” significa **última gravação válida**, normalmente a última run manual finalizada nessa instalação; não é histórico nem arbitragem entre processos. Um JSON válido de uma fixture pode substituí-lo.

| Registro | Evidência e atribuição possível |
|---|---|
| `c7fa8269-0403-4043-b437-25c1b7fcab40`, Aborted,564HP | `vs02a/production-04/nexus-summary.json`, driver de produção assistida e snapshot exportado `vs02b1/store-summary.json`; início21/10/2026 23:16:05.643UTC (20:16:05 em São Paulo), duração58,910534s. Finalização da terceira run assistida pelo lifecycle real, conforme evidência VS-02A; não é dado sintético do protótipo. |
| `bfc32dfb-3d35-4330-a503-53054d388278`, Victory,4266HP | Leitura histórica22/10 02:51:40UTC e nova leitura MCP22/10; início22/10 01:29:25.753UTC (21/10 22:29:25 em São Paulo), duração218,749395s. Registro persistido no Windows e Store atual concordam. Não há evento de gravação/autor preservado que permita atribuir essa run a humano, fixture ou outra ferramenta. Não chamar essa Victory de gameplay natural comprovada. |

Histórico deste chat não tem registros entre22/10 00:11:56.965UTC (task_complete) e02:43:20.926UTC (retomada); o início bfc está nesse intervalo. Isso enfraquece a hipótese de um comando de escrita deste chat durante a criação do protótipo, mas não prova quem jogou/gravação de outro processo. O protótipo só lê `data.js` congelado e não se conecta ao Unity/PlayerPrefs. **Causa específica da substituição permanece aberta; nenhuma evidência de corrupção ou defeito do Store.** O início posterior de bfc e a nova identidade são compatíveis com nova run finalizada, sem provar a origem.

Risco de contaminação confirmado estaticamente: `RunStatisticsPlayModeTests` preserva/restaura a chave no setup/teardown; `ManualPublishedRunPlayModeTests` carrega FirstSector, atravessa/finaliza runs e salva preferências, porém seu backup contém apenas loadout/unlocks. Logo pode deixar um resultado de teste no Store. Outras fixtures que destroem diretor ou encerram sessões precisam da mesma auditoria antes da VS-02B.2; não foram executadas nesta revisão para evitar escrever no resultado atual. Isso é problema de isolamento de teste, não justificativa para modificar persistência ou atribuir bfc a essa fixture sem log.

MCP verificou que duas leituras produzem instâncias independentes; alterar somente a cópia local não alterou a leitura seguinte (4266HP). Probe inicial teve erro de compilação em memória por `Object` ambíguo, corrigido para `System.Object`; nenhum script foi criado/compilado no projeto. Registro em `Library/GameplayObservation/vs02b-review/snapshot-probe.json`.

No Windows Editor a chave foi lida, sem escrita, em HKCU/Software/Unity/UnityEditor/DefaultCompany/T.G - Tech Guy, nome hash `TechGuy.RunSummary.v1_h3722393526`, com bfc/Victory/4266. A [documentação Unity de PlayerPrefs](https://docs.unity.com/ja-jp/engine/6000.6/script-reference/unityengine/playerprefs) diferencia local do Editor e do standalone; clones do projeto com mesma company/product podem compartilhar preferências do Editor, sem isolamento por Git worktree. `Save()` fornece persistência local; não houve teste de reiniciar o jogo/build nesta etapa. Não prometer que Editor e executável compartilham o mesmo resultado nem que uma run em andamento é retomada.

Plano de consumo: congelar cópia por abertura, identificar ID/início, atualizar somente em nova consulta, e autoabrir apenas para ID esperado no retorno confirmado. Fluxo e gates de input em [HUD/UI](06_HUD_E_UI.md#vs-02b--revisão-e-preparação-da-implementação-22102026). Nenhuma alteração de schema, ordenação cronológica ou Store proposta sem defeito demonstrado; isolar fixtures é a menor correção futura do risco conhecido.


### Integração de apresentação — VS-02B.2 (22/10/2026)

`BeginManualReturn` acrescenta apenas `RunResultsHandoff.Expect(ManualSession.Id.ToString())` após teardown e antes do carregamento. ID é transitório, resetado no início do runtime, consumido uma vez pelo presenter no Nexus carregado. Store/schema, outcome, retry e finalização permanecem iguais. Não existe autoabertura de registro velho no boot ou reescrita de resumo para adaptar UI; ID divergente produz estado indisponível. [HUD/UI](06_HUD_E_UI.md#vs-02b2--resultados-no-unity-22102026) possui a responsabilidade principal da consulta/snapshot/input.

Fixtures que criam/finalizam diretores agora herdam `RunSummaryIsolatedPlayModeFixture`, inclusive ManualPublishedRun e BasicAttackR1/partials; RunStatistics também ganhou a proteção adicional. Setup captura existência/bytes; teardown finaliza diretores remanescentes antes de restaurar preferências, em finally, com Save. `RunSummaryPreferenceScope` é exclusivamente de testes. Também guarda backup temporário no Editor SessionState e recupera na saída de Play/reload caso o runner seja interrompido antes do teardown. Não acrescenta chave de produção no PlayerPrefs ou altera RunSummaryStore.

Uma inicialização de teste bloqueada por erro de compilação transitório de Outline deixou runner tardio/interrompido e um resumo sintético seed42/Victory/100HP. O resíduo foi preservado como evidência e o backup original restaurado explicitamente como cleanup de teste. Esse achado motivou a recuperação adicional. Probe posterior criou escopo de teste, alterou a chave e saiu de Play sem Dispose: o callback restaurou automaticamente o resumo original e limpou Pending. Bateria normal45/45 também preservou os bytes. Registro original `bfc32dfb` ficou intacto ao final, inclusive armazenamento persistido; não se atribui a ele a origem do resíduo atual.
