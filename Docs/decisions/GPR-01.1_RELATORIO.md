# GPR-01.1 — fundação da fatia experimental da Manopla

28/10/2026 · **CONCLUÍDA TECNICAMENTE no escopo autorizado. STOP antes de GPR-01.2.**

## Autorização e resultado

Pedido direto recebido no texto “GPR-C01 Closure Approval and GPR-01.1 Authorization”. Lido também VALIDACAO_GPR_C01_5_E_GPR01_P1_TECH_GUY_20261028.md, em Downloads: revisão documental independente, não execução Unity. O pedido direto autoriza somente01.1; aceita a direção experimental A/Conductor/Detonation e o adiamento temporário do swap.

GPR-C01 permanece aceita e encerrada; não foi solicitado novo playtest de controle. GPR-01.P1 aceita como planejamento. A direção da fatia não é aceite humano de conteúdo ainda inexistente.

Entregues alvoBasic no grafo, contratos de HP confirmado/reserva/linhagem, permissões explícitas de derivados, preservação de cooldowns em aquisição e testes de compatibilidade. **Nenhum asset/catálogo Titanic Fists criado ou habilitado; nenhuma onda, transformação visual ou nova ação de produção.** A chamada de reserva aparece no runtime como contrato e nas fixtures, sem consumidor no input publicado.

## Decisões e alternativas

### Basic é alvo de boon

BoonSlot.Basic=4 e BoonSlotMask.Basic=16 foram anexados sem renumerar Q0/W1/E2/R3. Any permanece15; AllTargets31 é explícito. O grafo mantém quatro identidades autoradas e aceita Basic adicional, limitado à Manopla. Chamadas e JSON antigos com quatro configurações continuam válidos. BoonExpansionInventory mapeia GauntletActionSlot.Basic=-1 explicitamente; não faz shift/cast de -1 em arrays. Snapshot captura BasicMutationId por execução.

Não há quinta referência no WeaponScript ou quarta skill modular. Três Q/E/R e especialRMB preservados. Alternativa de inventário/grafo paralelo foi rejeitada; a extensão coube sem migração de assets ou GUIDs. Não se expandiu universalAny para Basic.

### Confirmação, filho e proveniência

O callback de HP efetivo da transação em PlayerActor registra ConfirmedHpReceipt no ProtocolRootContext. Somente Basic direto, terminal reconhecido no snapshot e receipt positivo autorizam a reserva. Whiff, imunidade, antecipação, postura isolada, contexto morto ou callback reentrante não autorizam.

ReserveConfirmedBasicChild exige fechamento do batch/feedback nativo, admite no máximo um token por terminal e reaproveita root/parent/budget existentes. Vítimas nativas ficam excluídas; filhos deduplicam por target+generation. Proveniência Basic/produtor/parent é mantida em RunStatistics, sem nova soma de HP. Não se usou evento cosmético para autorizar dano.

A reserva mantém a raiz depois do fechamento normal do produtor, conforme contratos existentes de filhos; Dispose revoga o trabalho. Morte/troca/invalidação de contexto/encontro, disable e troca de cena encerram estado pelos owners atuais. Pausa/hitstop e cap negado impedem HP; o consumidor deve usar o relógio escalado existente, não outro timer.

### Política restrita, inativa por padrão

ProtocolBasicEffects.None é o padrão da nova reserva. Grants explícitos permitem Conductor e/ou Detonation somente nessa linhagem. As fixtures técnicas exercitam Both; o player publicado não cria esse ramo. Isso permite A isolada em01.2, sem antecipar as composições01.3/4.

No ramo permitido: uma emissão elétrica da origemTechnique; primeira morte confirmada da origemTechnique ou do Conductor pode emitir uma Detonation; sem Conductor→Conductor nem Detonation realimentando emissões. Mantidos caps, reservas, geração, dedupe, LOS e filas atuais. Nenhuma raiz nova para renovar budget. Sem mana/Asura/hook nativo extra e sem herdar Ignite/Frost/Overflow/Resonance automaticamente.

ProtocolEligibility e cascatas nativas fora desse ramo permanecem como antes; não foram reduzidos ranks, dano ou budgets autorados. A fila propaga a política até os descendentes, inclusive em reentrância.

### Aquisição segura

RefreshAfterBoonAcquisition preserva timers/estados das habilidades cuja referência permanece idêntica, inclusive o especial. Refresh explícito de equip/reset mantém seu caminho. Referências realmente substituídas continuam com reset legado; isso **não** é um sistema de swap seletivo com UI/rollback universal.

Uma regressão adicional revelou ID desconhecido aceito como aquisição vazia. O caminho agora rejeita esse ID antes de CompleteOffer, sem tocar inventário/timers. Restore/rejeição do grafo permanecem atômicos. Não se alteraram tickets, RNG, pesos, número de oportunidades ou gates de Mutation/boss.

## Testes e evidências reais

Projeto D:/Projetos/Gabriel/Tech-Guy, Unity6000.5.10f1, MCP Tech-Guy@ec9dce4ca709f3be. Importação/compilação, Console, Unity Test Framework e inspeções pelo MCP real. Sem batchmode concorrente ou simulação externa substituindo Unity.

| Seleção final | Resultado | Duração | Job |
|---|---|---|---|
| EditMode: grafo/compatibilidade/ofertas/seeds/recompensas preservadas/Asura | **90/90**,0 ignorados |177,3077715s |1e87e68d7b7e47cc817df8f05a495354 |
| PlayMode: fundação +GI/Asura/três armas/Protocols/tickets/estatísticas | **120/120**,0 ignorados |62,4437199s |b13bdf682a7247a7a9d6fc74005e9d23 |

**210 casos distintos em filtros selecionados, não suíte global.** Quinze casos focados da fundação estão incluídos no PlayMode, não somados novamente. Seletores exatos em [test-requests.json](../validation/artifacts/gameplay-rework/gpr-01-1/test-requests.json); resultados completos em [edit-final.json](../validation/artifacts/gameplay-rework/gpr-01-1/edit-final.json) e [play-final.json](../validation/artifacts/gameplay-rework/gpr-01-1/play-final.json).

Cobertura nova: Basic explícito/Any legado; enums e JSON antigos; restore/rejeição sem commit; save→unload→reload de catálogo temporário e campo serializado16; clocks dos quatro executores; receipt terminal único; whiff/immune; dedupe/exclusão de vítimas nativas; cap negado/sem nova raiz; padrãoNone; Conductor→kill→Detonation limitado; pooled target; reentrância; pausa imediata/retomada; disable/fim de run/cena com filho pendente; HP e parent/produtor reconciliados.

O catálogo temporário fixture_basic_roundtrip é de teste, não Titanic Fists. Foi salvo isoladamente por SaveAssetIfDirty e excluído com a metadata pelo AssetDatabase. Nenhuma concessão assistida nas fixtures é prova de oferta natural A/B/C.

### Falhas e ajustes preservados

- Primeira tentativa24f602b… não iniciou: fixture referia enum privado como público; corrigido antes do resultado vermelho válido. Também corrigidos owner de imunidade como UnityEngine.Object e namespace do relógio numa iteração. Não contar erro de compilação como regressão comportamental.
- Vermelho válido386bebd3…: **0/2**, máscaraBasic rejeitada e aquisição zerava cooldown2→0. Ambos verdes posteriormente, mesmas condições centrais.
- Focado intermediário6ba44d41…:3/4; a asserção de rejeição revelou aquisição vazia de ID desconhecido. Corrigido no owner, sem enfraquecer o teste. Um seletor por prefixo não selecionou os testes causais; filtros posteriores usaram os nomes completos.
- Focados9/9 e13/13 passaram antes do guard explícito de pausa/hitstop. Depois,10/14 e11/14 expuseram fixtures emitindo antes de o relógio Protocol sair do hitstop. Preparação passou a aguardar IsCombatHitStopped e ScaledDelta; valores/aserts de HP permaneceram. Bateria14/14 posterior aprovada; padrãoNone acrescentou o15º caso final.
- Regressão105797bf…:101/103. Duas fixtures pressupunham catálogo legado: Techniques esgotadas sem adquirir Augments atuais e primeiro card sempre WeaponRunModifiers. Atualizada somente a preparação para o pool executável/adaptadores reais. Preservadas asserções de recursos/assets imutáveis, ticket único, clique obsoleto e Mutation contabilizada só na aquisição.
- Dois nomes de analytics usados numa seleção intermediária não correspondiam aos métodos reais; E/R corrigidos para AnalyticsNativeEMatchesRealHp/AnalyticsNativeRMatchesRealHp na seleção final. Não se contou seletor sem caso como PASS.
- A ponte desconectou durante o EditMode de simulação longa; resultado90/90 foi recuperado pelo MCP após conclusão, não inferido da perda de conexão.

JSONs intermediários permanecem na [pasta da atividade](../validation/artifacts/gameplay-rework/gpr-01-1/). A rodada119/119 anterior à opção explícitaNone é intermediária; o resultado vigente é120/120.

## Editor, integridade e limites

NexusBlockout restaurada aberta e limpa em Edit Mode ao fim; quatro referências de habilidade autoradas, movimento direto,0 scripts ausentes. GUID d9c5ad58160cf614e984e1c4f2599d39 preservado. Test Runner deixou inicialmente InitTestScene em memória; reaberta a cena original pelo Editor, sem salvar cenário temporário.

Inventário inicial:1613 arquivos protegidos de fontes/assets/metas/config. **1596 iguais,17 fontes alteradas intencionalmente**, zero alteração de cena/prefab/asset/meta/config preexistente. Dois testes novos + respectivas metas;0 GUIDs duplicados em Assets/_Project. [Integridade](../validation/artifacts/gameplay-rework/gpr-01-1/integrity.json) / [estado do Editor](../validation/artifacts/gameplay-rework/gpr-01-1/editor-final.json).

Moedas, seleção, tutorial, unlocks, resumo e chave de dash consultados permaneceram iguais à captura inicial; sem restaurar snapshot antigo sobre progresso humano. Ver prefs-final.json. Isso não é dump universal de PlayerPrefs. Alterações preexistentes de C01/Nexus/EditorBuildSettings e exclusão de Docs.zip preservadas; nenhum commit/push ou alteração em terceiros.

Console final mantém erro preexistente de Tripo3D com caminho local ausente. Há aviso legado de keys e avisos de fixtures sem câmera/dash/controller; não se declara Console global limpa. Nenhum Clear manual. Não houve retuning de dano base, cadência, animação, recovery, cancel, TTL ou Asura.

Não foram executados build/restart standalone, benchmark de swarm, visual de mutação ou playtest humano novo. Fonte/HP/linhagem foram exercitados por fixtures reais, não por uma onda acabada. AutoriaA, card, alcance percebido, composição natural A/B/C e economia de aquisição continuam sem validação até suas etapas.

## Arquivos criados e modificados

Criados em Assets/_Project/Scripts/Tests/PlayMode: GPR01FoundationTests.cs e GPR01BasicReceiptPlayModeTests.cs, mais as duas metas geradas pelo Unity.

Fontes existentes alteradas:

- Abilities/AbilityHolder.cs; Abilities/Weapon/GauntletExecutionSnapshot.cs.
- Characters/Combat/Protocols/ProtocolBudget.cs, ProtocolImpactIdentity.cs, ProtocolRuntime.cs, ProtocolRuntime.Scope.cs e ProtocolRuntime.Damage.cs.
- Characters/Player/PlayerActor.cs.
- Core/Run/BoonDefinition.cs, BoonCatalogDefinition.cs, BoonCapabilityGraph.cs, BoonOfferCompatibility.cs, BoonExpansionCatalog.cs, BoonExpansionInventory.cs e RunStatistics.cs; Core/RunBoons.cs.
- Tests/PlayMode/ManualRunRewardPlayModeTests.cs.

Documentação: este relatório; requisitos/design/tasks/validação/ownership/migração/índice/plano/fatia GPR e nota de estado da visão recebida; ADR0006, ledger, matriz, sistemas01/02/04, estado/prioridades e README. Evidências novas somente na pasta gpr-01-1, sem adicionar ambientes .pyimg ou arquivos gerados externos.

## Skills aplicadas

[tech-guy-game-vision](../../.agents/skills/tech-guy-game-vision/SKILL.md), [tech-guy-combat-unity](../../.agents/skills/tech-guy-combat-unity/SKILL.md), [tech-guy-boon-design](../../.agents/skills/tech-guy-boon-design/SKILL.md) e [tech-guy-unity-acceptance](../../.agents/skills/tech-guy-unity-acceptance/SKILL.md), com suas referências. Nenhuma skill gamedev adicional aplicável foi encontrada no catálogo disponível/.agents/skills; nenhuma instalação/uso fictício ou delegação.

## Prontidão, riscos e próxima aprovação

A fundação está **pronta tecnicamente para revisar a implementação experimental de A**, não para declarar Titanic Fists pronta. A próxima implementação deve consumir o token após o batch nativo, respeitar ScaledDelta e snapshots, aplicar coeficiente de dano uma única vez e provar geometria/receipts reais, sem ampliar grants automaticamente.

A migração de dados ficou limitada aos alvos/enums anexados. Ainda não há asset Basic de produção nem save/resume de uma run inteira validado; não usar o round-trip do catálogo como essa prova. Rollback universal de troca de skill não foi implementado; política Faulted dos tickets continua a existente.

RF-TROCA Q/E/R permanece NÃO IMPLEMENTADO/NÃO VALIDADO em GPR-03.1, com gate próprio para compatibilidade, investimento, cooldown/recursos, ticket, UI e rollback. Q/Avanço aqui é compatibilidade de ações para aproximação, não sinergia mecânica skill↔básico. A+B+C futura exige comparadores base/A/B+C/A+B/A+C/A+B+C/sem morte, isolando cascatas nativas.

**Próxima proposta: GPR-01.2 somenteA** — registrar uma MutationBasic, onda instantânea1,5×/0,60× provisórios, cardLMB e sobrepeça visual reversível, com provas base/A e cleanup. Usar grantsNone; não ativar composições novas de01.3/4, especial novo, retuning base ou UI de swap. [Escopo exato](../gameplay/GPR_01_VERTICAL_SLICE.md#fundação011-e-próximo-escopo).

**STOP. Aguardar revisão deste relatório e autorização explícita de01.2.01.3–6 também não estão autorizadas.**

