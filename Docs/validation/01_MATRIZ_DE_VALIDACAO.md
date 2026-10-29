# Matriz de validação: prova antes de declarar pronto

## Estado vigente — GPR-01.2b, 28/10/2026

O desenvolvedor testou fisicamente a onda instantânea anterior: funcionalidade técnica aceita, resultado de gameplay considerado pouco transformador e rejeitado como design final. A revisão autorizada substitui essa onda por um **tornado de vento móvel**, mantendo o ID da Mutation, aquisição, controles e combate nativo. A evidência de GPR-01.2 abaixo permanece histórica.

Cada terminal realmente executado no contato do scheduler, com A no snapshot aceito, autoriza no máximo um tornado **inclusive em whiff**. Pedido/cancelamento pré-contato e estágios intermediários não emitem. Contrato específico de emissão separado de `ReserveConfirmedBasicChild`, cujo requisito de HP confirmado permanece intacto. Emissão não produz HP/estatística/recurso nem nova raiz; impactos posteriores usam a fila e os receipts existentes, mesma raiz/parent e caps. Política do ramo: `ProtocolBasicEffects.None`.

Definição experimental única: deslocamento de 5,5 m a 3,5 m/s, vida máxima escalada de 2 s, raio 0,65 m, altura 1,8 m e offset inicial 0,75 m. Direção comprometida não acompanha mouse/rotação posterior. Dano = 0,60 × `CaptureAttackDamage` não crítico capturado na aceitação do terminal (dano authored + stats/Momentum elegíveis naquela aceitação); não depende de receipt, não sorteia crítico nem reaplica multiplicadores do atacante. Mitigação/HP seguem o alvo. Uma vítima por handle+generation, mantendo a exclusão das vítimas nativas já confirmadas.

Overlap inicial + capsule sweep por passo; sólidos e LOS bloqueiam. Reservas negadas/saturação encerram com segurança. Pausa/hitstop congelam; troca de arma/contexto, fim da run, disable/morte e cena invalidam/limpam reservas e apresentação. VFX reversível, pool limitado e leases por geração, sem Rigidbody/segundo executor/alteração permanente de prefab.

Implementação e validação desta revisão registradas no [relatório GPR-01.2b](../decisions/GPR-01.2b_RELATORIO.md). **Aceite físico do tornado novo pendente. STOP antes de GPR-01.3**: somente recomendar revisão da composição com Conductor após avaliação do desenvolvedor; nenhum grant novo de Conductor/Detonation, swap modular, tuning nativo ou quarta skill implementado.

### Fechamento técnico da GPR-01.2b

**CONCLUÍDA TECNICAMENTE NO ESCOPO TESTADO; HUMANO PENDENTE.**29/29 focados (28 da Mutation+1 interação),90/90 EditMode e187/187 regressões:305 nomes distintos finais, zero ignorados; não suíte global. Focados joba0ca7eec1c9f48668c0bcf4601e6b9ed; EditMode4b30bf2757f3424eb46e2d268dd40f82; regressão02090688af2f415ea00dbd3b27cb386f. Falhas intermediárias, correções de fixtures e intermitências históricas registradas no relatório, sem relaxar asserts/clocks.

- [x] Separar contato terminal/whiff de receipt positivo sem relaxar reserva confirmada global.
- [x] Tornado progressivo, direção capturada, pacote não crítico estável, sweep/LOS, caps/dedupe e cleanup.
- [x] Pool/VFX reversíveis; captura Game View em6 quadros eCSV; aquisição por ticket preservada.
- [x] Regressões de controles/GI/armas e integridade de assets/evidência histórica no alcance declarado.
- [ ] Revisão física do tornado pelo desenvolvedor; sensação/arte/balanceamento e carga em swarms pendentes.
- [ ] Autorizar separadamente GPR-01.3 antes de compor Conductor; nenhuma composição nova habilitada.

### Matriz vigente da revisão do tornado

| Critério | Prova executada | Limite / resultado |
|---|---|---|
| Whiff/terminal/commit/cancel |TornadoTerminalWhiff, UncommittedRequestAndCanceledTerminal, CommittedNativeContactSurvivesRecoveryCancel eNoBoon |28 testes da Mutation aprovados; não entrada física |
| Trajetória/tempo/dano |KeepsCommittedDirection, WhiffDamageUsesAcceptedNoncriticalPacket, SweepsThinTargetsAtFourFps |5,5m/3,5m/s,12HP no pacote da fixture;250ms de delta injetado não mede FPS físico |
| Parede/caps/dedupe |WallStopsTravel, BudgetExhaustion, MultipleTargets, PoolingDedupesEachTargetGeneration |Sólidos bloqueiam, várias vítimas/colliders, sem renovar raiz |
| Lifecycle/política |PauseAndHitstop, SwapAndReturn, Disable, EndRunAndNewRun, SceneTransition, PlayerDeath, ReentrantWeaponSwap, NonePolicy, ImpactAttribution |Zero futuros/raízes/VFX ativos após encerramento; sem novo Conductor/Detonation/recurso |
| Oferta/catálogo |TitanicCatalog, AcquisitionKeepsCooldowns, RealRewardTicket |ID único, Basic explícito/Any15, seed0/node5 em rota de fixture, sem grant no teste de oferta |
| Controles/GI/armas |187/187 regressões finais |G/QER/RMB/WASD, Bow/Spear e receipts preservados; fixture deixa de pressupor G e usa bindingF atual |
| Visual |6 PNG reais do Game View +travel.csv |Vórtice desloca antes de HP; câmera/rig/alvo de fixture, arte/feel físico pendentes |
| Integridade/Console |2459 arquivos iniciais:2451 iguais/8 fontes intencionais;0 GUID duplicado;0 erros nativos/526 avisos |Nenhuma cena/prefab/asset/meta/config/evidência histórica alterada; read_console tem classificação divergente de Debug.Log |

[Evidências e contagem sem duplicação](artifacts/gameplay-rework/gpr-01-2b/test-summary.json). [Relatório com tentativas falhas e ressalvas](../decisions/GPR-01.2b_RELATORIO.md).

## Histórico — estado GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2, 28/10/2026

GPR-01.1 aceita tecnicamente pelo desenvolvedor; GPR-C01 continua encerrada. Autorizada somente a primeira Mutation jogável **Punhos Titânicos**. Implementados catálogo Basic da Manopla, onda na raiz nativa com HP confirmado e grants `ProtocolBasicEffects.None`, sobrepeça reversível e testes focados. Implementação concluída com ressalva de regressão:18/18 focados e90/90 EditMode;187/187 na última bateria ampliada.295 casos distintos finais, com ocorrências intermitentes anteriores de hitstop/burn registradas e não declaradas corrigidas. Zero ignorados; não confundir automação com aceite humano. [Relatório da atividade](../decisions/GPR-01.2_RELATORIO.md).

Parâmetros experimentais: caixa frontal de 1,5× o alcance/largura efetivos do soco, altura preservada; 0,60× o maior dano pré-mitigação entre receipts nativos positivos do terminal, coeficiente aplicado uma vez. Sem tuning do básico/Asura/QER, nova ação, recurso, postura ou grant Conductor/Detonation. Nova aquisição usa os tickets/catálogos existentes; nenhuma política de recompensa ampliada.

**STOP após GPR-01.2. GPR-01.3 e etapas seguintes não autorizadas.** Revisão humana da mutação e de seu visual permanece pendente; moonwalk/cadência e swap modular continuam diferidos. As seções anteriores preservadas abaixo são históricas onde divergirem deste estado.



## Evidência vigente GPR-01.2

| Seleção/critério | Resultado | Limite |
|---|---|---|
| Mutation e lifecycle |18/18 PlayMode, zero ignorados; job8cce2d5baf4a44209f2bde148c7c2576 |Automação por API real; inclui receipt/coeficiente, geometria, pooling, caps, reentrância e onda vazia autorizada |
| Grafo/catálogos/seed |90/90 EditMode; job82b5fd3cb02b45668d4d87ef5feaf580 |Seleção existente; expectativa exata da Manopla inclui somente o novo ID autorizado |
| Regressão ampliada |187/187 PlayMode; jobd064e012a3ae4fb1830b136d4845da1c |Última execução verde não corrige por si as intermitências anteriores de burn/hitstop |
| Contagem |295 nomes distintos, sem sobreposição/ignorados |Não é suíte global; [resumo calculado](artifacts/gameplay-rework/gpr-01-2/test-summary.json) |
| Oferta A |Ticket legítimo/seed0, node5; Mutation contabilizada uma vez |Rota de fixture; sem grant do boon; não uma run física completa |
| Visual |4 Game View PNG reais e inspecionados; extra12HP a2,65m vs frente nativa2,05m |Rig publicado/câmera de fixture; grant assistido só no comparador visual; humano pendente |
| Integridade |1794 iniciais:1781 iguais/13 fontes intencionais/0 ausentes; 0 asset/meta/config preexistente alterado |Zero GUID duplicado em Assets/_Project; checker genérico falhou em caminho externo C:/D: |
| Console |Contadores nativos:0 erros/526 avisos; sem Clear manual |Avisos de fixtures/pacote; divergência de classificação de Debug.Log pelo read_console registrada |

[Relatório completo](../decisions/GPR-01.2_RELATORIO.md). **STOP antes de01.3; revisão humana de A pendente.**

## Histórico da entrega GPR-01.1 — superado pelo estado vigente acima

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](../gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Fechamento GPR-C01.5 e readiness GPR-01.P1

| Critério | Resultado | Fonte e limite |
|---|---|---|
| C01 técnica |116 nomes distintos Passed/0failed/0skipped em seis filtros finais, relidos nesta rodada | final-input31, final-ui9, final-movement23, final-combat30, final-dash8, final-edit15 em artifacts/gameplay-rework/c01-4d; não suíte global/nova execução |
| C01 humano |APROVADO por declaração direta nesta conversa,28/10/2026 | WASD/mira/dash/LMB/combos/RMB-Asura/QER/fluidez/configuração/interação experimentadas; não aprovação de catálogo/arte futura |
| C01.5a/b |CONCLUÍDAS, faseC01 COMPLETA | [Relatório5](../decisions/GPR-C01.5_RELATORIO.md) e [consolidado](../decisions/GPR-C01_PHASE_SUMMARY.md); sem exigir outro playtest completo |
| Refinamentos |DIFERIDOS NÃO BLOQUEADORES | Moonwalk: strafe/backpedal/forward/blend2D local futuro; básicos rápidos: tuning futuro. Nenhuma alteração de timing/animação nesta rodada |
| Cobertura não certificada |PENDENTE quando relevante | Standalone/restart, hotplug/dispositivos físicos completos, gamepad, crowd64/móveis/overlap. Pendência não é falha demonstrada do controle aprovado |
| GPR-01.P1 |PLANEJAMENTO CONCLUÍDO; design aguardando revisão | [FatiaA/B/C](../gameplay/GPR_01_VERTICAL_SLICE.md); leitura estática do grafo/receipt/protocolos/timers; zero gameplay novo/zero testes Unity novos |

As evidências anteriores mantêm seus resultados, revisões e limites. O aceite atual encerra somente a pendência humana dos controles atuais; não transforma capturas virtuais antigas em playtest humano, nem aprova GPR-01.



## Histórico do fechamento C01.4,28/10/2026

**CONCLUÍDA TECNICAMENTE; aguardando playtest humano.** Três skillsQ/E/R + especialRMB, semX/aliasF11; InteractG e finisherF, pausa remapeável/Escape seguro. DashCursor padrão ou Movimento com fallbackolhar por decisão direta.116 casos distintos aprovados em filtros agregados (115 na rodada completa +9UI posterior com1 novo), zero ignorados; não suíte global. Snapshots automatizados iguais e mudanças posteriores do perfil preservadas. Editor finalNexusLobby limpa/Edit Mode; Tripo3D preexistente pendente. [Consolidado](../decisions/GPR-C01.4_RELATORIO.md). Não iniciar C01.5/GPR-01 sem autorização.


## Histórico C01.4 — layout revisado, execução sequencial

C01.3b aceita tecnicamente pelo desenvolvedor. Decisão mais recente substitui quatro slots Q/E/R/X por **três Q/E/R + especial separado RMB**, preservando índices/assets/executores.4a concluída tecnicamente4/4 focados, relatório individual e captura real; cumulativa pendente.4b concluída:1/1 política/persistência e8/8 dash; Movimento sem input usa facing por decisão direta mais recente.4c concluída2/2: InteractG separado de skills/finisherF; contexto e objetos/remap validados.4d concluída tecnicamente: Q/E/R+especial separado, semX;116 casos distintos finais agregados, capturas1280x720, relatórios4a/b/c/d e consolidado. Preservadas falhas intermediárias; sem suíte global/aceite físico novo. STOP após4,5/GPR-01 exigem aprovação. Sem C01.5/GPR-01.


## Histórico C01.3b — fechamento na data,28/10/2026

**CONCLUÍDA TECNICAMENTE:105/105 casos distintos finais, zero ignorados**, via Unity MCP no checkout correto/Editor6000.5.10f1. Aceite humano anterior das últimas correções recebido para WASD, básicos, Q/E/R e Asura/RMB; básicos rápidos ficam como refinamento futuro, sem mudar cadência. [Relatório](../decisions/GPR-C01.3b_RELATORIO.md).

Corrigidas fixtures que criavam dispositivos virtuais antes da política de foco, e o gate de soltura que descartava o primeiro comando novo no frame seguinte. Dois testes falharam0/2 antes da correção de produção;22 básicos/input+23 movimento+7 contextos+30 combate+8 dash+15 EditMode passaram após as correções. Ajustado teste Cadenciada para agir no frame de recovery observado, mantendo asserções/tempos. Outputs e falhas anteriores preservados em c01-3b/correcao e gate; não suíte global. Runtime publicado com input virtual:3 básicos0/1/2 e1 Asura por RMB,4 PNG; não novo teste físico.

F11 deixou de ativar Asura direta por decisão explícita; binding salva/executor/custos/estágios preservados. Frame de soltura continua bloqueado; held não atravessa UI/foco/pausa. Editor final NexusLobby limpa/Edit Mode; permanece erro de pacote Tripo3D preexistente. Causa física do incidente RMB antigo não confirmada retrospectivamente; aceite humano posterior inclui RMB funcional.

**Gate de C01.4 satisfeito;4a–4d já autorizadas, ainda não implementadas.** Não iniciar C01.5/GPR-01. Interact separado precisa respeitar F/finisher; X pode ficar vazio, nunca duplicar Asura. Autorizações e evidências3a abaixo são históricas onde conflitam com este fechamento.

| Mudança | Inspeção/diff | Unity compila/import | EditMode | PlayMode/cena | Imagens/vídeo | Humano |
|---|---|---|---|---|---|---|
| Documentação simples | obrigatório | não aplicável | — | — | — | aprovar decisão quando for de design |
| Algoritmo puro de dano | obrigatório | recomendado | obrigatório | se integrado | logs úteis | se altera feel |
| Comportamento de arma | obrigatório | obrigatório | conforme lógica | obrigatório | vídeo + logs | obrigatório para feel |
| Boon com executor | obrigatório | obrigatório | contrato/IDs | obrigatório | vídeo + captura | obrigatório para diversão |
| Prefab/material/HUD | obrigatório | obrigatório | quando possível | obrigatório | screenshot comparável | obrigatório para aparência |
| Enemy AI/telegraph | obrigatório | obrigatório | regra | obrigatório | vídeo timeline | obrigatório para justiça |
| Save/schema/run | obrigatório | obrigatório | serialization | cenário real/round-trip | quando relevante | somente se impacto UX |

## Semântica do resultado

- `IMPLEMENTADO_NAO_VALIDADO`: código existe, mas não há todos os testes necessários.
- `VALIDADO_TECNICAMENTE`: compilação no Unity e verificações relevantes executadas com resultados registrados.
- `VALIDADO_EM_GAMEPLAY`: comportamento observado em build/Editor com cenário reproduzível e feedback registrado.
- `APROVADO_PARA_ENTREGA`: usuário aprovou os aspectos que exigem julgamento autoral, sem pendências bloqueantes.

`dotnet build` é evidência auxiliar, não substituto do Unity. Resultados históricos não cobrem alterações posteriores.

## Unity MCP

Verificar instância/projeto antes da task. Coletar Console inicial; inspecionar componentes/serialized data; salvar/reabrir quando cabível; compilar/testar; não limpar Console para esconder falhas; terminar fora de Play Mode. Se indisponível registrar ferramenta, falha e limites; continuar apenas nas partes que permitem prova externa.

## Evidências por execução

`task_id`, commit/hash, data, versão Unity/pipeline, cena+seed+câmera, resolução, comandos/testes executados, resultado, Console antes/depois, paths das capturas/vídeos, problemas e aceite humano. Evidência inválida: 'deve funcionar', 'compilou no dotnet', 'passou antes do último refactor', 'simulado' quando não foi.

## Gates especiais

- Combate: feedback temporal e controle dos alvos requer vídeo/telemetria, além de testes.
- Boons: oferta/compatibilidade/custo/persistência/run + efeito visível e budget.
- UI: contraste em Game View alvo, população real de dados e legibilidade de swarm.
- Arquitetura: GUIDs, referências e saves não podem ser afetados silenciosamente.

## Registro consolidado de evidências — 18/10/2026

Esta seção preserva o resultado essencial sem depender de documentos antigos removidos intencionalmente. A consolidação atual fez **leitura de código, cenas/assets serializados e respostas históricas**, não executou compilação/testes Unity ou gameplay. Não havia ferramenta Unity MCP exposta nesta sessão; não foi possível conferir instância, cena aberta ou Console atual.

**Revisão inspecionada:** workspace `gameplayOverhaul`, base Git `3a3f0ee`, com alterações locais extensas. Essa base identifica o ponto de partida, não um snapshot completo dos arquivos executados. Os registros históricos também atravessam correções locais e não permitem atribuir toda a cobertura a um único commit limpo. O projeto inspecionado declara Unity `6000.5.10f1` e URP `17.5.0`; não inferir por isso a versão de cada execução sem seu registro.

| Evidência histórica consultada | Resultado observado | Limite da conclusão |
|---|---|---|
| `cleanup-edit-result.json.response.txt` | Resumo EditMode: 40/40 `Passed`. | A resposta de resultado não inclui os 40 casos detalhados; não usá-la como lista individual comprovada nem somar como cobertura distinta de toda outra execução. |
| `expansion-edit-result.json.response.txt` | 59/59 EditMode com casos detalhados: `BowBoonTests`, `SpearBoonTests`, `BoonCapabilityGraphTests` e `BoonExpansionOfferTests`. | Contratos/algoritmos/ofertas; não valida aparência, tempo de resposta ou aceitação humana. |
| Respostas PlayMode de expansão, diagnóstico, correção, regressão, final e encontros publicados | 102 nomes completos de casos distintos com último resultado observado `Passed`, agregando execuções e usando `finished_unix_ms` para escolher o último estado por nome. | **Não foi uma execução única de suíte com 102/102.** Houve falhas intermediárias: expansão 24/38 aprovados e execução chamada `final` com 98/99, seguidas por correções/reexecuções. Não declarar suíte completa aprovada nem cobertura automática de cada arma/lote só pela contagem. |
| `published-play-result.json.response.txt` e `gauntlet-published-play-result.json.response.txt` | Três casos de encontro publicado aprovados; a reexecução de manopla passou 1/1 após falha inicial. | Cenários automatizados específicos, já incluídos na agregação PlayMode; não equivalem a um playtest humano completo. |
| `catalog-roundtrip.json.response.txt` | Catálogo com 55 IDs únicos/31 habilitados salvo, descarregado, reimportado e recarregado; GUID, script e valores serializados preservados, sem definições divergentes. | Round-trip desse asset nesse momento; não certifica todos os prefabs, saves ou mudanças posteriores. |
| `inspect-references.json.response.txt` | Inspeção histórica de `NexusLobby`, `FirstSector`, `PlayerArmature` e `PlayerHUD`: zero missing scripts reportados e bindings críticos registrados. | Checagem pontual desses objetos. Não é auditoria atual de todas as referências do projeto. |
| `real-route-edit-result.json.response.txt` + resumo V2 | Três testes Unity EditMode de simulação passaram; 300 seeds por arma em dois modos = 600 runs simuladas por arma, 1.800 no total. | Simula políticas/seleções com vitórias pressupostas; não executa 1.800 partidas de gameplay, navegação ou duração real. |

**Proveniência bruta opcional:** respostas em `Library/CleanupAudit/V1-20261018/`; CSV/JSON/resumo em `Library/BoonReworkValidation/V2/`. `Library/` é ignorada pelo Git e pode ser regenerada/removida: esses caminhos são localizadores de evidência histórica, não pré-requisitos para entender a documentação. Os scripts nesses diretórios não foram reexecutados pela consolidação. Caso seja necessário auditar detalhes ausentes em outro checkout, registrar a limitação e produzir nova execução identificada, sem restaurar a documentação antiga.

### O que a simulação V2 demonstrou

[`BoonRealRouteSimulationTests`](../../Assets/_Project/Scripts/Tests/EditMode/Editor/BoonRealRouteSimulationTests.cs) carrega `FirstSectorManual`, loadouts e catálogo de Protocols, usa `ManualRunRandomizer.Generate`, `ManualRoutePolicy` e tickets de produção; estratifica igualmente quatro caminhos (75 seeds por caminho/modo). Compara pesos de produção 2:1 com contrafactual uniforme; escolha de cards é automatizada e distinta de preferência humana.

| Arma | Builds finais distintas em 300 runs de produção | Runs com Duo em produção | Runs com Duo em 300 runs uniformes |
|---|---:|---:|---:|
| Manopla | 300 | 16 | 10 |
| Arco | 258 | 47 | 48 |
| Lança | 268 | 12 | 18 |

No conjunto observado houve 6–14 oportunidades por run e uma oportunidade de Mutation em cada run. `duo_bow_eclipse_archer` não completou pré-requisitos, não se tornou elegível e não foi adquirido em nenhum dos dois modos do arco. Isso pede revisão da oportunidade de montagem antes de prometer acesso consistente; não autoriza balanceamento automático. Builds distintas e presença das famílias não provam arquétipos perceptíveis, diversão ou equilíbrio.

### Pendências de aceite

- Identificar nova revisão executável e repetir apenas as verificações afetadas por mudanças posteriores; não transportar os resultados históricos para qualquer estado futuro.
- Smoke atual do caminho Nexus → encontro → recompensa → transformação → boss/extração ou morte/abandono → retorno, com Console e cenário identificados.
- Vídeo/capturas comparáveis, legibilidade em swarm, custo de CPU/GC e avaliação humana dos lotes compartilhados. Não foi encontrado aceite do desenvolvedor para o conjunto completo nas evidências consultadas.
- Relatório de dano por habilidade ainda é lacuna de implementação; sua reconciliação causal e legibilidade exigirão testes próprios quando autorizados. Ver [run](../systems/05_RUN_E_PROGRESSAO.md) e [telemetria](03_TELEMETRIA_E_GAME_FEEL.md).

## Verificações desta consolidação documental

- Checagem de existência de links Markdown locais: 41 arquivos (AGENTS, README, Docs e skills/references), 277 links, nenhum destino ausente. Frontmatter obrigatório e nome/diretório das quatro skills conferidos.
- Agregador Python: AST e `--help` verificados sem executar agregação nem gerar evidência. O validador `quick_validate.py` das skills foi tentado, mas os runtimes consultados não têm PyYAML; nenhum pacote foi instalado. A conferência básica de frontmatter/links não substitui o validador completo.
- Hashes anteriores/posteriores e lista de exclusões permitem distinguir esta consolidação das alterações locais preexistentes. C# alterado nesta tarefa limita-se a destinos de captura/validação e criação dos diretórios correspondentes; asserções e gameplay não foram modificados.
- `git diff --check` global também encontra whitespace preexistente em `FirstSector.unity`; a cena não foi modificada por esta consolidação. Verificar o escopo documental/de caminhos separadamente, sem limpar alterações alheias.
- Nenhuma compilação/importação, Console, EditMode, PlayMode, screenshot ou playtest novo foi executado. Os destinos novos das ferramentas ainda exigem smoke no Unity quando essa verificação for autorizada/disponível.

## Observação de gameplay — execução de 18/10/2026

Base Git `3a3f0ee`, branch `gameplayOverhaul` com alterações locais (não snapshot limpo). Unity 6000.5.10f1, instância MCP `Tech-Guy@fccafb2b47cd7671`, projeto `D:/Dev/Personal/My_games/Tech-Guy`. Ferramentas/recursos descobertos antes de executar; Console inicial sem erros. Evidências brutas locais sob `Library/GameplayObservation/`, ignoradas pelo Git e opcionais em outro checkout.

| Verificação realmente executada | Resultado | Limite |
|---|---|---|
| Compilação/importação Unity e Console | Infraestrutura compilada; Console sem erros após correções | Não é validação global dos sistemas preexistentes |
| Python: `python -m unittest discover -s Tools/GameplayObservation -p 'test_*.py'` | 36/36 passaram, incluindo integridade/condições, envelopes MCP, timeout, recusa de captura, caminhos e pranchas | Fixtures Python sintéticas não são imagens de gameplay |
| EditMode: `TechGuy.Tests.ObservationTelemetryTests` | 10/10 passaram; job `ba858de8a33d49e6a617dd811b63babb` | Lifecycle/IO/subscrições e contratos de entrada, não balanceamento |
| PlayMode: `TechGuy.Tests.WorldClickResolverTests` | 5/5 passaram; job `1f83b76fe5004c16bb18874e9e5f557f` | Seleção de alvo isolada; não certifica bindings ou combate completo |
| Gameplay real `GAUNTLET_SMALL_GROUP` | Duas execuções completas de FirstSector/nó 1, seed efetiva 1701, cinco inimigos, manopla base; 120 PNG 1280×720 por execução; 360 frames de simulação e 5.815 eventos por execução | Agenda de seis básicos; sem boons e sem cobertura de habilidades nessa dupla; captura adicional abaixo cobre Q/W/E, não R/cancelamentos/elite/boss |
| Comparação offline | Condições iguais, 120 pares alinhados, zero lacunas; 117 diferenças de pixels zero; maior média RGB absoluta 0,0312/255 | Repetição sem mudança de gameplay; não demonstra melhora/piora nem determinismo universal |

**Localizadores existentes:** before `20261018T233321344-46fd7d8e/`; after `20261018T233346675-c8d5293f/`; comparação `gauntlet-repeatability-v1/comparison.json` e `contact-sheet-001.png` a `contact-sheet-015.png`, todos relativos à pasta bruta acima. Cada execução contém `manifest.json`, `events.jsonl` e `frames/`. Os manifestos guardam revisão/dirty state, hashes, câmera/render e restauração concluída.

**Observação factual:** comandos aceitos nos frames 30, 75, 120, 165, 210, 255; inimigo `enemy-002@1` recebeu 30, 20 e 10 HP nos três primeiros, morreu no frame 120; player recebeu 21,59998 HP no frame 182. Três aplicações de hitstop registradas. Essas contagens coincidiram entre execuções. Não somar ProtocolImpact/HookHit/HookBasicHit novamente. O primeiro callback BasicAttackPerformed já ocorre depois do impacto síncrono.

**Inspeção visual efetivamente feita:** PNGs originais before nos frames 30 e 75, frame 120 da primeira captura de diagnóstico, além da prancha comparativa 008 (frames 168–189). Não foi assistido vídeo nem examinada manualmente cada imagem. Jogador claro sobre fundo escuro, vida/mana e slots visíveis; telegraph vermelho comprido passa pela região do jogador e chega à área do HUD; número de dano/ícone e barra se sobrepõem no inimigo adjacente. Três prioridades de investigação, sem correção de gameplay autorizada: (1) cobertura insuficiente das transições de reação neste fixture de básicos — transições de reação só apareceram como estado inicial, então ausência de reação visível não prova ausência da mecânica; (2) contraste/hierarquia do telegraph junto ao corpo e rodapé, conferir o trecho 120 e a resposta ao projétil nos frames 168–189; (3) feedback de dano/barra do alvo muito próximos no frame 75, conferir em sequência e resoluções menores. São observações/hipóteses de revisão, não decisões de produto nem aprovação de feel.

**Diagnósticos preservados:** a execução `20261018T233134614-15bfe707` reteve 120 imagens mas falhou na restauração da resolução temporária (nome truncado pelo Unity); não foi usada como baseline. O primeiro teste de cancelamento `20261018T233954817-d19f32eb` falhou na restauração da cena durante transição; NexusLobby foi restaurada explicitamente e o guard foi corrigido. Chamadas iniciais do runner com zero testes não foram contadas; um GUID inválido do novo arquivo de teste impedia importação, corrigido antes dos 10 testes finais. Um caso novo tinha expectativa incorreta sobre `current` passivo; foi corrigido para testar comandos proibidos em `current`. Nenhum erro do Console foi limpo para validar.

**Status:** `VALIDADO_TECNICAMENTE` para captura/comparação da manopla e testes listados; gameplay desse fixture observada com limites acima. Preparação automática de swarm, elite/boss, escolha e resultado de run pendente; alternativa disponível é preparar o fluxo real e observar com `current`. Vídeo codificado, benchmark, aprovação humana de game feel e validação visual das outras armas não realizados.

**Verificações adicionais após correções de teardown:** cancelamento real `20261018T234227246-922839e4` terminou `cancelled`, com evidência parcial preservada, restauração completa e retorno a NexusLobby limpa em Edit Mode; Console sem erros. A tentativa anterior `20261018T234132393-2bb6e1ee` expôs uma coroutine ainda retomando durante saída de Play Mode (erro de screenshot fora de Play Mode); foi retida como falha, com cena restaurada, e os guards de estágio foram corrigidos. Reexecução EditMode final: 10/10, job `a49e33af8c7e43eb9eb12903148483d9`.

**Habilidades reais:** `20261018T234326917-e6ae04d9` completou 540 frames, 180 PNGs e 8.485 eventos, restaurando o Editor. A agenda em `transport/gauntlet-skills-request.json` usou W no frame 30, tentativa de finish em 90 (recusada), E em 150, Q em 300 e básico em 420; as três habilidades e o básico foram aceitos. `SkillStateChanged` expôs Startup/Active/Recovery para Punhos Relâmpago (30/37/110), Impacto de Choque (150/163/184) e Avanço Relâmpago (300/307/324). São fronteiras amostradas, não marcadores de animação exatos. Houve nove recibos de perda de HP e sete ProtocolImpact sobrepostos; não somar esses canais. Postura do alvo atingido variou de 100 a 28, mas não houve stun/airborne/control lock observado; não prometer esses estados como cobertos. O PNG original do frame 45 foi aberto e examinado. Essa agenda diferente é evidência complementar, não par comparável ao baseline de básicos.

**Integridade:** 1.859 arquivos existentes de runtime/assets/configuração tiveram SHA-256 comparado com `transport/protected-before.json`, sem mudanças; nenhuma funcionalidade do jogo foi editada. Permanecem 546 exclusões pendentes preexistentes, com riscos em [migração e limpeza](../05_MIGRACAO_E_LIMPEZA.md); nenhuma foi revertida ou confirmada. No fechamento, NexusLobby limpa, Edit Mode, seleção vazia, timeScale 1, captureDeltaTime 0, qualidade 5, shader async false e runInBackground true, iguais ao snapshot inicial desses campos. Links locais de 41 Markdown verificados sem destino ausente. O `git diff --check` global encontra whitespace preexistente em FirstSector; a cena mantém o hash inicial e não foi formatada por esta tarefa.

## Gameplay Intelligence 1.0 — contrato da primeira iteração

Pedido autorizado do desenvolvedor: uma melhoria pequena da Manopla dentro dos contratos existentes, com antes/depois equivalente; sem nova direção de combate. Base atual capturada em `Library/GameplayObservation/20261019T004412743-7015ee64/` (seed 1701, cinco comuns de papéis distintos, básicos). A execução anterior `20261018T234326917-e6ae04d9` continua acessível; seu PNG 45 foi aberto novamente. A baseline nova evita presumir que o dirty workspace permaneceu igual.

Contrato conforme o template de tarefa: entrada = impactos sucessivos não letais no mesmo inimigo; comportamento desejado = o feedback visual GetHit começa no contato de cada impacto, inclusive quando o estado anterior ainda é GetHit; exceções = None/morte/estado sem clip não ganham reações novas; não objetivos = alterar interrupção, dano, postura, recursos, hitstop, cancelamentos, IA ou duração autorada. Reutilizar `CombatReactionController.PlayAnimation` e o Animator/clip existentes; não adicionar sistema de reação.

Diagnóstico inicial: (1) flinch repetido não reinicia: GetHit entra no frame 30 com normalizedTime 0, porém no segundo dano (75) continua em 1,1000; `Animator.Play(stateName)` usa tempo padrão que preserva o estado atual. (2) recuperação visual não encerra GetHit: no frame 110 o tempo normalizado é 1,95, embora o clip não cíclico dure 0,6667s; o estado não possui transição de saída e a IA com parâmetro Speed só atualiza esse parâmetro. (3) legibilidade: no frame 75 da baseline anterior número/ícone/barra se sobrepõem no alvo próximo; precisa revisar âncoras e sequência antes de decidir mudança de HUD. Os três são problemas de apresentação; ausência de stun não prova falha de interrupção, que depende de windup/perfil.

Mudança selecionada: reiniciar explicitamente o flinch a partir do tempo normalizado zero em cada ApplyReaction elegível. Risco: feedback repetido reiniciar durante cadeias; verificar não alterar mecânica nem semântica de outras chamadas de PlayAnimation, e conferir Arco/Lança. Gates: teste PlayMode deve reproduzir o segundo hit e falhar antes da correção; depois passar; comparar a agenda de básicos e amostras de dano/recursos/IA; testar postura, hitstop, cancelamentos e ranged. Estado final/limites serão registrados nesta mesma seção.

### Resultado da iteração — uma correção de sincronização

**Implementação:** `CombatReactionController.ApplyReaction` solicita reinício apenas da apresentação de flinch. O helper privado usa `Animator.Play(stateName, 0, 0f)` nesse caso; chamadas de stun preservam a semântica anterior. Não foram modificados dano, HP, mana, postura, chance crítica, duração de CC, regras de interrupção, hitstop, cancelamentos, IA, assets ou cenas. O controller é compartilhado: Arco/Lança também recebem o reinício correto de flinch em impactos sucessivos, sem mudança dos seus contratos mecânicos.

| Problema | Desejado / causa provável / menor proposta / risco |
|---|---|
| P1 — flinch não reinicia no segundo impacto | Feedback deve marcar cada contato; antes GetHit em 75 continua em 1,10. Causa confirmada pelo teste: Play sem tempo explícito preserva o estado ativo. Reutilizado controller + Animator + clip GetHit; corrigido somente o reinício. Risco de apresentação repetida sob pressão foi limitado à chamada de flinch; nenhum controle mecânico adicional |
| P2 — recuperação visual fica em GetHit após o fim do clip | Retomar locomotion/apresentação coerente, sem prender IA. Clip real GetHit não cíclico de 0,6667s não tem transição de saída; ramo Speed da IA não troca de estado. Reutilizar controller/Animator/EnemyLocomotionPresentation; próxima proposta mínima é terminar a apresentação pelo dono correto, com proteção de ataques/stun/launch e cleanup. Risco de sobrescrever ataque/CC exige teste próprio; não corrigido nesta iteração |
| P3 — número/ícone/barra disputam espaço no alvo próximo | Preservar leitura de corpo/contato/HP conforme pilares. PNG 78 antes e sequência 72–93 mostram sobreposição. Causa provável: `DamagePopup.OnGUI` usa offset mundial fixo + retângulo de texto/marker e não evita a barra. Reutilizar DamagePopup/DamageNumberBurst e barra existente; estudar ajuste de âncora/separação, sem HUD novo. Risco de ocupar telegraph/corpos em grupo ou cortar texto em resoluções menores; proposta não implementada |

**Teste de regressão antes/depois:** `PostureReactionPlayModeTests.RepeatedFlinchRestartsAtEachContactWithoutAddingControlOrDisplacement` usa um Grunt real com seu clip/perfil, isolado de navegação/IA (não é playtest de arena). No runtime anterior falhou na asserção do segundo contato: normalizedTime 0,375 em vez de <0,01; job `51f5e377ba804fbfa14dc31bf3ff8e19`. Após a correção passou junto da suíte. Duas tentativas anteriores ajustaram somente o preparo do teste (profile criado pelo spawner e avaliação inicial do Animator); não são evidência do bug nem foram contadas como testes de gameplay. Respostas brutas em `Library/GameplayObservation/iteration1/`.

**Testes realmente executados depois:**

- PlayMode: **37/37**, job `bc989f0793e5444993d373936492b71e`: PostureReactionPlayModeTests, HitFeedbackR7PlayModeTests, DashCancelResolverPlayModeTests, RangedFluidityPlayModeTests. Inclui quebra/launch/knockdown/fallback, pausa/hitstop, windup interrompível, recuperação/resume, cancelamento/dash/imunidade e locomoção/kiting. São fixtures com controle de estado, não 37 partidas naturais.
- EditMode: **37/37**, job `4a216b8b56f24b69acdc2ca01d2849bc`: RangedFluidityContractTests, AsuraFinishControlTests e ObservationTelemetryTests. Cobrem gates/modelos/bindings e lifecycle de evidência, sem aprovação de feel.
- Unity importou a correção; Console final sem erros, sem chamadas para limpar Console.

**Capturas novas equivalentes:** todas em FirstSector/nó 1, seed 1701, cinco comuns (Grunt/Heavy/Charger/Sniper, incluindo um MediumMob), 1280×720, mesma agenda de seis básicos, câmera/render/fingerprint inicial iguais. Cada lado tem 120 PNGs reais e JSONL. Revisão é base Git mais alterações locais, registradas no manifesto; não confundir HEAD com snapshot imutável.

| Arma | Diretório before | Diretório after | Comparação sob `iteration1/` |
|---|---|---|---|
| Manopla | `20261019T004412743-7015ee64` | `20261019T005139152-87479ef1` | `gauntlet-comparison/comparison.json` |
| Arco | `20261019T004645439-a21d67cc` | `20261019T005208674-48c12386` | `bow-comparison/comparison.json` |
| Lança | `20261019T004725375-6e8f143f` | `20261019T005250195-e34ef419` | `spear-comparison/comparison.json` |

Todos os caminhos acima são relativos a `Library/GameplayObservation/`. Comparador validou condições sem override, 120 pares e zero lacunas em cada arma. Pranchas contact-sheet-001 a 015 acompanham cada comparison.json. A baseline histórica não foi sobrescrita.

**Resultado comprovado no cenário da Manopla:** dano no mesmo Grunt em 30/75/120 permanece 30/20/10 HP; seis comandos aceitos e uma morte em ambos. No segundo impacto, Animator GetHit passa de **1,1000 antes para 0,0000 depois** no frame 75; em 90 passa de 1,4500 para 0,3000, mostrando nova progressão do clip. Mana final 1500, Asura 9, três hitstops aplicados, sem hitstop/cast residual e quatro cooldowns zerados em ambos. Não houve stun/airborne/quebra nessa agenda. O Sniper prepara/executa/recupera em ambos; início/impacto/recuperação deslocaram 2–3 frames (107/168/195 antes, 109/171/198 depois), dano recebido idêntico 21,59998 HP nos frames 182/185. Isso limita alegações de timing idêntico: o hitstop usa relógio real e o readback/sampling pode variar. Não atribuir a diferença automaticamente ao patch nem declarar latência melhorada.

**Regressão das outras armas:** Arco preservou exatamente a timeline de HP (33/80/123/170/180/224/269), 96 HP total nos inimigos e 21,59998 recebido; Lança preservou 30/75/120/180, 60 HP nos inimigos e 21,59998 recebido. Seis comandos aceitos, uma morte, mana final 1500, energia 0 e nenhum hitstop aplicado em ambos os lados dessas armas. Isso comprova o cenário de básicos, não todas as combinações de skills/boons. Detalhes em `iteration1/event-checks.json`.

**Visão efetivamente realizada nesta iteração:** PNG histórico 45; baseline nova 30; PNGs before/after 78 da Manopla; prancha comparativa 004 (72–93). Há diferença de pose após o segundo contato, coerente com o reinício/progressão registrados. Não foi assistido vídeo nem revista manualmente toda a sequência de todas as armas. A sincronização foi corrigida; satisfação/peso subjetivo e fidelidade ao Breaker dependem da revisão humana.

**Limites e próximo passo:** o único alvo isolado desta iteração foi o Grunt do teste de apresentação; gameplay capturada foi contato repetido com um Grunt dentro do grupo de cinco, não uma arena exclusiva 1×1. Quebra e interrupção elegível foram verificadas por testes controlados, não por uma nova quebra natural na captura. O alvo morre sem quebrar postura nessa agenda; alterar HP/postura/kit para produzir a demonstração estaria fora do patch. Não foi introduzido stun ou launch para fabricar impacto. Próxima melhoria recomendada: P2, fechar o ciclo visual GetHit → locomotion sem interromper ação/CC e sem reescrever a resistência aprovada. P3 e sincronização do contato da animação do atacante continuam pendentes. **Status:** correção `VALIDADA_TECNICAMENTE`, observada em gameplay com limites descritos; game feel não marcado `APROVADO`.

## Gameplay Intelligence 1.1 — contrato P2

Escopo autorizado: encerrar a apresentação GetHit, preservando o reinício da 1.0 e as mecânicas. Inspeção: GetHit do AnimatorController de inimigos não tem transição de saída; Idle é a raiz do BlendTree Speed. EnemyAI possui um caminho Speed (só escreve parâmetro) e um fallback Idle/Walk. EnemyCombatActions/SkillAnimationPlayer já possuem a execução/apresentação ofensiva; PostureReactionState já possui controle/pausa. Nenhuma resistência ou regra ofensiva será reescrita.

Contrato conforme template de tarefa: entrada = reação elegível em inimigo vivo; saída = clip GetHit próprio concluído retorna à locomoção existente. Novo impacto reinicia o clip. Prioridades = morte/disable, estado/transition externa, controle (stun/launch/knockdown/congelamento), execução ofensiva/procedural e recovery existentes prevalecem. Relógio = normalizedTime real do Animator, sem timer de gameplay paralelo; não encerrar durante pausa/hitstop. Cleanup cancela ownership para que pooled/re-enabled/controller trocado não recebam saída atrasada.

Responsabilidade escolhida: CombatReactionController mantém somente ownership da apresentação iniciada e libera o clip concluído; EnemyAI conserva seleção/velocidade/cadência. Serão consultados gates somente leitura dos donos ofensivos existentes. O fallback binário de locomoção deve respeitar o flinch ativo. Nenhum manager/AnimatorController adicional. Não objetivos: P3, HUD/números, balanceamento, novos CC, dano, recursos, IA decisória ou mecânicas de arma. Critérios: regressão falha antes e passa depois; cenário de impacto único e múltiplo equivalentes; testes de prioridade/cleanup/pausa/hitstop/ranks e armas; Console e estado do Editor conferidos. Resultados serão registrados nesta seção, sem relatório separado.

### Resultado P2 — recuperação visual implementada

**Causa confirmada:** GetHit é não cíclico, dura 0,6667s e tem zero transições de saída no controller real. O caminho Speed da IA atualiza um parâmetro sem sair desse estado; o fallback binário, em contraste, podia trocá-lo por locomoção antes do fim. O CombatReactionController iniciava o clip sem possuir um encerramento. A recuperação visual não depende de mudar a duração do stagger ou criar controle mecânico.

**Implementação e fronteiras:** CombatReactionController possui apenas a apresentação GetHit que iniciou, identificada pelo estado e RuntimeAnimatorController. Em LateUpdate, após normalizedTime >=1, sem pausa/hitstop nem prioridade, libera para a raiz Idle do blend Speed existente ou Idle/Walk pela histerese existente de EnemyLocomotionPresentation. O novo impacto mantém o reinício em zero da 1.0. Estado/transition externo ou controller trocado abandona ownership; morte, disable e reconfiguração fazem cleanup. Não há timer, barramento, AnimatorController ou componente novo.

O dono consulta, sem escrever, a execução/recovery de EnemyAI e o `_busy` de EnemyCombatActions, além de SkillAnimationPlayer.IsPlaying e controle/congelamento existentes. Essas consultas impedem iniciar/liberar flinch sobre apresentação prioritária. EnemyAI ganhou somente a consulta do guard que já existia e a proteção do fallback binário contra truncar o clip possuído; não mudou decisão ofensiva, percepção, navegação, cooldown ou resistência. Nenhum dado serializado, dano, recurso ou mecânica de arma foi alterado. P3 permaneceu fora do escopo.

**Testes:**

- Antes da correção, `FinishedFlinchReturnsToLocomotionWithoutChangingMechanics` falhou exatamente porque GetHit concluído não retornava a Idle; job `1d64bb8f40584d4ba12f0fe76458ba4c`, resposta em `iteration11/regression-before.json`.
- Depois, **45/45 PlayMode** passaram, job `9559ed41361a42ea88e2cfb50b565375`: PostureReactionPlayModeTests, RangedFluidityPlayModeTests, HitFeedbackR7PlayModeTests e DashCancelResolverPlayModeTests. Novos casos: fim de clip sem mudança mecânica; reinício adia a saída; ataque externo e disable; pausa/hitstop; windup que não pode ser interrompido; nove combinações de rank Normal/Elite/Boss com stun/knockup/knockdown e fallback; morte; fallback binário com NavMesh real. Os testes anteriores de interrupção elegível/quebra/retomada/dash também continuaram passando.
- **22/22 EditMode** passaram, job `7d4e3c09da6e4a01a6c44ea9e0006027`: RangedFluidityContractTests e ObservationTelemetryTests.
- A primeira passagem dos novos testes teve três falhas de preparo: EnemyVariant.Start reaplicava o perfil depois do início da reação. O fixture isolado passou a aplicar o perfil explicitamente sem esse Start tardio; runtime não foi mudado para contornar a asserção. Passagem intermediária Posture: 19/19, job `1da156d9ec3a4038bfe083790206b690`. Respostas intermediárias preservadas.

**Capturas reais, caminhos relativos a `Library/GameplayObservation/`:**

| Cenário | Before | After | Comparação sob `iteration11/` |
|---|---|---|---|
| Um impacto da Manopla no frame 30 | `20261019T010058349-55c2af4b` | `20261019T010858180-fdcf598d` | `single-comparison/comparison.json` |
| Dois impactos da Manopla nos frames 30/50 | `20261019T010521016-fa71a4f2` | `20261019T010932996-fbe283c7` | `multiple-comparison/comparison.json` |
| Básicos do Arco | `20261019T005208674-48c12386` | `20261019T011151296-ea10b657` | `bow-comparison/comparison.json` |
| Básicos da Lança | `20261019T005250195-e34ef419` | `20261019T011242171-8af5144a` | `spear-comparison/comparison.json` |

Manopla: baselines novas antes de editar runtime, 240 frames e 80 PNGs por lado. Arco/Lança: baselines da versão 1.0 reutilizadas e explicitamente identificadas; 360 frames e 120 PNGs por lado. O comparador confirmou mesmo cenário, seed, arma, agenda, câmera, renderização, resolução e fingerprint inicial, sem overrides; zero lacunas nas quatro comparações. As baselines antigas não foram sobrescritas. Todas usam FirstSector/nó 1 real e cinco inimigos comuns de papéis distintos; não são arenas exclusivas 1×1. A captura preliminar `20261019T010237975-b5fddbd5` tinha um terceiro golpe letal no frame 90; a agenda de dois impactos foi capturada antes do patch para permitir observar também a recuperação do alvo vivo.

**Evidência temporal da melhoria:** no impacto único, GetHit começa em 30 e atinge normalizedTime 1 no frame 71 em ambos. Antes continua em 1,225 no frame 80 e 2,975 em 150; depois entra em blend no 72 e a primeira amostra de Idle aparece em 76. `Idle` é a raiz do blend de locomoção; não significa inimigo mecanicamente parado. No cenário duplo, o frame 50 reinicia GetHit em zero nos dois lados; a nova reprodução chega ao fim em 92 e retorna a Idle em 97 somente depois da correção. Assim, a saída não usa o vencimento do primeiro impacto.

**Mecânicas observadas:** no único, 30 HP no inimigo no frame 30 e 21,59998 HP recebidos em 181 nos dois lados; mana final 1500, Asura 3 e um hitstop aplicado. No duplo, 30/20 HP nos frames 30/50, mana 1500, Asura 6 e dois hitstops em ambos; o dano recebido de 21,59998 ocorreu em 183/184 (antes/depois). Não inferir melhoria de latência: hitstop em relógio real e readback continuam introduzindo variação. Arco e Lança preservaram exatamente suas timelines de dano e recursos finais nesses cenários de básicos; energia 0 e nenhum hitstop aplicado em ambos os lados. Dados em `event-checks.json` e `ranged-event-checks.json`.

**Imagens realmente examinadas:** PNGs before/after do impacto único no frame 99; prancha `single-comparison/contact-sheet-004.png` (72–93); prancha `multiple-comparison/contact-sheet-005.png` (96–117). A pose muda após o fim do clip, coerente com os estados registrados. Não foi visto vídeo nem revisado manualmente cada PNG de todas as armas.

**Limites:** prioridade, morte, pausa, hitstop e ranks/fallbacks foram exercitados em fixtures controlados, não numa nova run completa com boss/elite. O controller real não tem estado Stun; os testes verificam o controle/pose existentes sem inventar um clip. Controllers externos com outro vocabulário de locomoção exigem contrato próprio; esta entrega cobre as convenções atuais Idle/Walk/Speed. Não há aprovação humana de game feel. P3 continua pendente. **Status:** P2 `VALIDADO_TECNICAMENTE` e recuperação observada no cenário real descrito, preservando a avaliação humana.


## Gameplay Intelligence 1.2 — contrato de apresentação da Manopla

Autorização: uma melhoria controlada dentro dos contratos atuais; sem ajuste de timing, cancelamento, dano, recursos, resistências ou IA. Inspeção inicial: Unity 6000.5.10f1, NexusLobby em Edit Mode, Console sem erros; base `3a3f0ee` com alterações locais. Recursos/instância/projeto confirmados pelo MCP.

Lacuna confirmada no Editor: `Resources.Load<SkillAnimationLibrary>("Combat/SkillAnimations")` retorna null; só existe EnemyMotions como biblioteca. `SkillAnimationPlayer.Strike/Contact` encerram sem apresentar a ação. Extensão escolhida: fornecer ao caminho de recurso existente apenas PunchLeft/PunchRight/Slam, referenciando os mesmos clips/contactos já utilizados em EnemyMotions. Não há sistema, animação autoral ou controlador novo. Não executar CombatPolishBuilder.Build: também chama BalanceAsura e gera outras armas. Asset ausente não consta nas exclusões Git pendentes; nenhuma exclusão será restaurada.

Contrato conforme template: entrada = Q/W/E/R nativas já aceitas pelo holder; resultado = SkillAnimationPlayer usa os clips existentes no relógio do executor e amostra o contato no impacto. Cleanup/release/pausa continuam com os donos atuais. Não objetivos: básico, novo micro-stagger, buffering de skill, ajuste de cronologia, HUD/P3, outras armas ou aprovação humana. Critérios: teste do recurso falha antes e passa depois; capturas equivalentes mostram a alteração de pose; dano/recursos/inputs/fases comparados; regressões relevantes, import/Console e integridade de assets existentes conferidos. Arco/Lança não ganham entradas novas nessa biblioteca.


Investigação complementar antes de aceitar o patch: só conectar o recurso não mudou a pose renderizada (candidatos `20261019T013455537-abcbb4ad`, `20261019T013525815-0feaefba`). O coletor existente ganhou leitura de `SkillAnimationPlayer.IsPlaying/CurrentMotion/NormalizedTime`; candidato `20261019T013751910-2750cc22` demonstrou graph ativo, apesar do corpo imóvel. Teste com TG_Rigged e o AnimatorController de produção falhou após uma atualização do engine: erro angular de 75,505 graus no braço, job `ef497f2c233c449090e3c23b513862c0`. O teste inicial usava outro modelo/sem a mesma concorrência do controller e não demonstrava esse defeito. Resultados parciais não são melhoria de gameplay.

A mesma fatia de apresentação precisa também reaplicar a última amostra manual após a avaliação do Animator. `SkillAnimationPlayer.ReapplyPose` apenas avalia o graph existente com delta zero; o LateUpdate já existente de BreakerGauntletCombat chama isso somente enquanto executa a Manopla. Não há relógio, prioridade, controller ou animação novos; não se muda a apresentação de outros executores. O teste de modelo passou a usar TG_Rigged e controller de produção, verificando a reaplicação depois da atualização, além de recurso/pose/cleanup.


### Mapa integrado e diagnóstico priorizado (1.2)

Referência de intenção: pressão/compromisso, antecipação e impacto legível do Breaker, conforme os canônicos do Tech Guy. Não foi medida frame data de Lost Ark nem copiado kit/timing externo. Peso/diversão permanecem julgamentos de playtest humano.

| Ciclo | Caminho real inspecionado | Divergência/limite |
|---|---|---|
| Input | CharControlScript alimenta BasicAttackDriver por tap/hold/target. A API direcional usada pelo observador entra no mesmo PerformAttack, mas não testa binding físico/driver de hold. | Captura não mede latência teclado → comando. |
| Básico | CanStartBasicAttack → Play da animação em zero → área/projétil e dano síncronos → busy/recovery → próximo intervalo. Gauntlet BasicProfile possui ImpactEvent em 0,4, mas PerformAttack não agenda esse evento. | Dano no início do clip, sem startup visual correspondente. Não atrasar dano sem decisão de contrato. |
| Skill | AbilityHolder valida ready/mana/requisito/cancel; TryActivate debita uma vez. Breaker captura snapshot e limita avanço pelo NavMesh; coroutine avança elapsed e Strike; FixedUpdate passa progress ao ActionImpactScheduler/ledger. | Fases/impactos do profile e tempos de animação dos AreaHitSteps coexistem. |
| Impacto | Scheduler → Contact/ApplyHitStep → transação HP/reação/postura → notificações/energia/feedback confirmado. | Contact síncrono pode ser substituído por Strike posterior antes do render. A correção 1.2 torna a pose persistente no frame, sem reconciliar todos os tempos legados. |
| Reação | CombatReactionController consulta perfil/threshold/janela/proteção; interrupção elegível cancela a execução inimiga. Quebra tem prioridade e fallback por rank; flinch próprio mantém ciclo 1.0/1.1. | GetHit visual não comprova micro-stagger mecânico; controle/launch não foram fabricados. |
| Hitstop | Escopo confirmado aplica HitStopRunner uma vez por impacto, duração em relógio real; elapsed/Animator usam suas integrações existentes. | Readback/custo do Editor alteram quantidade de frames durante hitstop. Não converter PNG nominal em duração real. |
| Recovery/próxima ação | RestoreCastState libera graph/nav/execução; CancelResolver lê regras. Dash e continuação Cadenciada usam o buffer existente; movimento guarda ordem navegável em recovery. | Skill comum fora da janela termina em Busy; não há retry geral de skills. |
| Ataque inimigo | EnemyAI → EnemyCombatActions → EnemyAttackExecution: telegraph com windup mínimo de 0,25 s, beat de dano, recovery; cancel invalida versão e limpa aviso. | A agenda estacionária capturou SniperShot, mas não um alvo próximo em windup interrompível. |

| Prioridade | Problema e evidência | Impacto/frequência/gravidade estimados | Causa/extensão, complexidade e risco |
|---|---|---|---|
| 1 — corrigido nesta fatia | Q/W/E sem movimento corporal visível: W frame 39 e E frame 63 da baseline; recurso ausente + pose manual sobrescrita pelo controller, reproduzida em teste. | Alta: perde antecipação, execução e recuperação em cada skill. Estimativa de impacto perceptivo, não nota humana. | Recurso já previsto + clips existentes + commit da última pose no LateUpdate do dono. Pequena; risco de pose residual/override, coberto por cleanup/cancel e capturas. |
| 2 — pendente | Básicos removem 30/20/10 HP nos frames de comando 30/75/120; no primeiro, Animator recém-iniciado. Código executa Play(0) e dano na mesma chamada. | Alta, a cada básico: contato/antecipação não explicam o dano. | BasicProfile/scheduler existem, mas migrar a emissão altera timing e pode afetar cancel/alcance/procs. Complexidade média, risco alto; exige contrato/decisão antes de implementar. |
| 3 — pendente | Tempos visuais de skill não seguem integralmente o scheduler. No E corrigido, primeiro dano frame 47 já tem motion Slam em 0,157; segundo frame 63 em 0,600, enquanto contato autorado desse clip é 0,55. | Alta em skills; sincronização/peso ainda incompletos. A leitura temporal é factual; julgamento visual final pendente. | Strike usa steps.delay; emissão usa ImpactEvent.At × duração e Contact pode ser sobrescrito no mesmo frame. Reutilizar scheduler/SkillAnimationPlayer, sem outro clock. Complexidade média; risco de sequência, multi-hit e mutações. |
| 4 — pendente | W aceito em 30; Q em 90 termina Busy e não dispara após recovery, captura `20261019T013610633-ae2dd10d`. | Potencialmente alta nas transições; frequência depende do jogador e pede teste humano. | TryCancelActiveForSkill não armazena miss temporal; InputBuffer já existe para dash/Cadenciada. Extensão média com decisão de elegibilidade/feedback/buffer; risco de ação inesperada e cobrança duplicada. Não liberalizar janelas. |
| 5 — pendente (P3 anterior) | Número/ícone/barra do alvo se sobrepõem aos efeitos e silhueta nos frames W39/E63. | Legibilidade do resultado comprometida no melee; frequência por hit. Gravidade média estimada, maior sob pressão ainda não capturada. | Apresentações existentes de dano/stance/feedback; ajustar hierarquia/posicionamento quando autorizado. Complexidade pequena/média; risco de esconder informação. HUD não alterado. |

O comentário antigo de CharControlScript que chama todo básico de cosmético não descreve a Manopla atual: Gauntlet.asset sobrescreve reação para Stagger e contribuições 0,35/0,45/0,6. A decisão usa ReactionPolicy e janela real, não a descrição do comentário. Não mudar resistência/interrupção com base nessa frase. Interrupção elegível e fallback por rank permanecem cobertos pelos testes, sem alegar demonstração natural onde não ocorreu.

### Cobertura real dos cenários solicitados

- Básicos em um alvo próximo dentro de encontro de cinco: baseline `20261019T013227167-985843c0`, após `20261019T014229286-26cfb1a7`. Não é arena 1×1 nem sequência atingindo cinco simultaneamente; os outros alvos estão espalhados. Menor extensão para isolar: selecionar um spawn de produção como fixture declarada, preservando stats/IA, e registrar contagem/fingerprint no manifesto. Para pressão de cinco: dirigir aproximação/posicionamento real até agrupar naturalmente, sem injetar agrupamento/dano.
- Q/W/E: agenda W30/E150/Q300 de 540 frames. W acerta o alvo próximo; E/Q tardios executam, mas não conectam nesse alvo já morto/distante. E isolado30 de 240 frames produz dois impactos confirmados. Sem declarar que Q tardio confirmou dano.
- Quebra efetiva: E isolado causa 10/44 HP e reduz postura, mas não quebra esse alvo. W o mata antes da quebra. Nenhuma StanceBroken natural nova foi observada. Testes controlados cobrem quebra/launch/ranks; não são essa demonstração. Menor extensão: direcionar a agenda/rota a um alvo de produção sobrevivente com perfil adequado; não mudar HP/postura/dano para obtê-la.
- Interrupção: tentativa W155 (`20261019T013202143-4c65393b`) acerta o alvo próximo enquanto o Sniper distante prepara/executa ataque. São atores diferentes; não comprova interrupção. Necessária seleção explícita de alvo + movimento até alcance, mantendo janelas/resistências.
- Ataque → movimento → dash: o cliente seeded só dirige basic/skill/finish e current não dirige input. Validação automatizada existe nos testes de navegação/input/cancel, mas não foi capturada uma sequência natural nova. Menor extensão: alimentar os dispositivos virtuais já existentes/rotas públicas de movimento e dash, adicionando agenda e condições do manifesto ao validador atual. Nenhum driver de input paralelo será criado nesta tarefa.



### Resultado da melhoria 1.2 e evidências

**Implementado:** biblioteca de três entradas com GUIDs dos clips existentes, sem duplicá-los; reaplicação com delta zero no LateUpdate do executor da Manopla; três campos de observação do graph manual no coletor já existente. A biblioteca sozinha foi insuficiente; o fix de ordem de apresentação é necessário para o corpo aparecer animado. Não foi criada outra FSM nem trocado AnimatorController. Arco/Lança não têm entrada nova e não chamam ReapplyPose; EnemyMotions e sua execução não foram alteradas.

Arquivos da fatia: `Abilities/Weapon/BreakerGauntletCombat.cs` (uma chamada de apresentação), `Abilities/Weapon/SkillAnimationPlayer.cs` (helper interno de reaplicação), `Editor/CombatPolishPreview.ObservationTelemetry.cs` (leituras), `Tests/PlayMode/BreakerKitEmissionPlayModeTests.cs` (dois casos), todos sob Assets/_Project/Scripts. Novo recurso `Assets/_Project/Resources/Combat/SkillAnimations.asset` e metas. Canônicos de combate/captura/matriz e manifesto documental atualizados.

**Testes efetivamente executados:**

- Regressão de recurso ausente: 0/1, falha esperada antes, job `fc41a3523cd048a78b570f72cfe11562`, `iteration12/regression-before.json`.
- Regressão com TG_Rigged + AnimatorController atual: pose de contato perdida após engine update, erro angular 75,505 graus, job `ef497f2c233c449090e3c23b513862c0`, `controller-regression-before.json`. Depois, o caso de reaplicação testa o mesmo modelo/controller e limpeza/root motion; a captura real valida a ligação do LateUpdate.
- **62/62 PlayMode**: job `4fb38afa3fdb43aea3394e4923f66655`, `playmode-first.json`. Classes efetivamente encontradas: ActionImpactSchedulerPlayModeTests, BreakerKitEmissionPlayModeTests, CancelNoDuplicationPlayModeTests, DashCancelResolverPlayModeTests, HitFeedbackR7PlayModeTests, PostureReactionPlayModeTests, RangedFluidityPlayModeTests. Abrange ledger, kit Q/W/E/R, pose/recurso, cobrança/cancelamento/projétil emitido, hitstop/pausa, GetHit 1.0/1.1, controle/ranks/prioridade e locomoção ranged.
- **22/22 EditMode**: job `8ad5f4ee4003483caf4bfea1cf6b3a8d`, `editmode-final.json`: RangedFluidityContractTests e ObservationTelemetryTests.
- **Suíte ampliada: 146/158 PlayMode**, job `e120b661086c4cb986e2dba29143c56f`, `playmode-broad-candidate.json`: BasicAttackR1PlayModeTests (classe partial inclui movimento, run e boons), DamageConfirmationPlayModeTests (namespace TechGuy.Tests.PlayMode), CompatibilityContractPlayModeTests. Não declarar suíte global verde.
- Os 12 que falharam foram repetidos isoladamente sem o comportamento novo: recurso temporariamente fora de Resources e sem chamada ReapplyPose. Resultado **3/12 passaram, 9 falharam**, job `9be9ea75349b47118e4f04d33b4c6d8d`. Candidato restaurado e mesmos 12 repetidos: **3/12 passaram, mesmas 9 falharam**, job `e4472b5fe1774893bd2187d312cf26e9`. Arquivos `broad-failures-baseline.json` / `broad-failures-candidate.json`. Nenhuma falha exclusiva do candidato foi identificada nessa comparação; isso não diagnostica toda a causa das falhas herdadas.
- As nove persistentes: AuthoredEliteSpawnsExistingRankAndTwoActualDefenseAcquisitionsRequireInputRelease; GauntletPhase3AuthoredZeroChildDelaysKeepProducerFirst; GauntletPhase3LegacyShockwaveKeepsItsEffectEventsAndDistinctProducer; GauntletPhase3SharedHudGroupsEffectiveGainsShowsMutationsAndUnbinds; ManualExhaustedTechniqueAppliesRetainedProtocolOnceAndPreservesResourcesAndSourceAssets; ManualImpossibleFirstMutationKeepsBossBlockedAndAllowsAbortWithoutVictory; PauseConfirmedAbortWaitsForResumeFrameAndReturnsFromUnavailableMutationWithoutVictory; RunFocusDiscountsEffectiveQCostOnceForEachWeaponFamily; ActualDamageMergesPerVictim_AndLethalConfirmationSurvivesDeath. As outras três (DemolishingAdvance, ManualRewardStaleInventoryClick, Release_StopsHoldRepeats) passaram nas duas repetições isoladas: dependência de contexto/ordem/timing continua pendente. Nenhuma asserção foi afrouxada nem sistema adjacente corrigido.

As respostas preservam nomes/estados/asserts/stacks; logs repetitivos de fixtures sem AudioListener foram omitidos dos JSONs de testes para reduzir volume. Nenhum desses warnings é aprovação de áudio. O teste inicial de pose foi fortalecido para o modelo/controller real após o candidato sem efeito visual; não esconder esse diagnóstico.

**Capturas finais e comparações:** caminhos relativos a Library/GameplayObservation. Cinco comparações com condições equivalentes verificadas, sem overrides e sem lacunas; alinhamento por frame original, sem deslocar para esconder timing.

| Agenda | Antes | Depois | Pares e relatório sob iteration12/ |
|---|---|---|---|
| E isolado30, 240 frames | `20261019T013133154-4c103479` | `20261019T014131289-b0c5f93d` | 80; `shock-final-comparison/comparison.json` |
| W30/E150/Q300, 540 frames | `20261019T013048567-0f5fa44c` | `20261019T014205970-e381db2d` | 180; `skills-final-comparison/comparison.json` |
| Seis básicos, 360 frames | `20261019T013227167-985843c0` | `20261019T014229286-26cfb1a7` | 120; `basics-final-comparison/comparison.json` |
| Básicos Arco, 360 frames | `20261019T011151296-ea10b657` | `20261019T014255052-9817791a` | 120; `bow-final-comparison/comparison.json` |
| Básicos Lança, 360 frames | `20261019T011242171-8af5144a` | `20261019T014314637-0b1d819f` | 120; `spear-final-comparison/comparison.json` |

Baselines de Manopla foram capturadas antes do patch nesta iteração; Arco/Lança reutilizam as finais 1.1 explicitamente. Seed 1701, cinco spawns reais, 1280×720, mesmo estado inicial/câmera/agenda/render. Cada run contém PNGs e events.jsonl; manifestos registram revisão/condições/restauração. Dados resumidos em `event-checks.json` e `state-checks.json`. Campo novo ausente em baseline não é um zero medido de graph ativo.

**Comprovado no E isolado:** corpo antes em pose de repouso durante os efeitos; depois há antecipação/alteração corporal/slam e retorno, vistos nos PNGs. Comando aceito30; danos 10/44 HP nos frames47/63, dano recebido21,59998 em186, idênticos; mana imediatamente1402 e final1500, Asura10 e dois hitstops em ambos. Mesmas transições/fim mecânico, graph liberado no frame84 após o patch. 2.640 amostras comuns de ActorSnapshot/ReactionSnapshot não diferiram em posição/rotação/HP/nav stop/postura/controle/launch/break/proteção. Isso prova a mudança de apresentação sem mudar esses resultados nesse cenário; não prova peso subjetivo nem a sincronização completa de todas as skills.

**Demais resultados/limites:** básicos mantiveram 30/20/10 HP em30/75/120, dano recebido em182, Asura9/mana1500/hitstop3 e 3.480 amostras comuns sem diferença mecânica nos campos conferidos. Arco/Lança mantiveram suas timelines completas de dano e recursos finais (mana1500, energia0, hitstop0). Q/E tardios mantiveram fases, mas não confirmaram dano na agenda combinada. W manteve os quatro primeiros impactos; quinto mudou de 9,2 normal para 13,8 crítico; os frames finais foram79/88/104 antes e78/85/100 depois. Total removido desse alvo60 HP nos dois lados, energia10/hitstop7, mas isso não equivale a preservar dano por hit.

Os sete instantes de impacto W em tempo escalado coincidem a seis casas (0,643333; 0,783333; 0,903333; 1,063333; 1,203333; 1,303333; 1,523333); recovery começou em t1,566695/1,566687, nos frames111/106. Relógio real/readback e RNG global de crítico são hipóteses para variação, não causa comprovada. Repetição sem novas mudanças `20261019T015222826-78ebb47d` repetiu o crítico e mudou último impacto100→101; `skills-repeat-checks.json`. Assim, **equivalência por hit do W continua inconclusiva**; não alegar ausência universal de regressão nem ganho de velocidade. A prova principal da melhoria é o E isolado equivalente. Próxima extensão de evidência possível: tornar a sequência do RNG de crítico observável/controlada com a seam existente, registrando essa condição, sem alterar chances/dano autorados.

**Inspeção visual realmente realizada:** baseline W39 e E63; candidato inicial sem efeito W39; crops diagnósticos before/candidate E63; versão final E63 e prancha shock-final-comparison/contact-sheet-003.png (48–69); finais W39 e Q315. Não foi assistido vídeo nem examinada manualmente cada imagem das cinco comparações. Os clips reaproveitados não foram aprovados como linguagem final da Manopla por esta tarefa; feedback audiovisual e pressão de grupo ainda pedem playtest humano.

**Pendências e recomendação:** reconciliar primeiro a apresentação das skills com os ImpactEvents existentes, preservando o instante mecânico, e obter revisão humana desse slam/socos. O básico exige contrato próprio antes de mover o dano para contato. Manter P3/HUD, buffer geral de skills, demonstração natural de quebra/interrupção/1×1/grupo compacto/dash e nove falhas herdadas separados. R real não capturado (energia cheia não foi injetada). Status: apresentação corporal corrigida tecnicamente e observada no cenário E; aceite de game feel pendente; cobertura integrada ainda parcial.


Fechamento de integridade 1.2: snapshot de Assets/_Project, Packages e ProjectSettings mostrou somente os quatro C# da fatia alterados e os três arquivos novos de recurso/metas. Nenhuma cena, prefab, AnimatorController, configuração, clip existente ou outro ScriptableObject foi modificado. As 546 exclusões Git pendentes permaneceram exatamente iguais. Candidato foi restaurado byte a byte após a reprodução de baseline e o asset temporário não permaneceu no projeto. Links canônicos sem destinos ausentes, 41 hashes documentais atualizados e diff --check do escopo C# aprovado. Editor final: NexusLobby limpa, Edit Mode, timeScale1, captureDelta0, seleção vazia, qualidade5; Console sem erros. `iteration12/integrity-final.json` e `editor-final.json` registram os checks. Não houve commit nem aceite humano de game feel.


## Gameplay Intelligence 1.3 — contrato de sincronização Q/W/E

Início em 20/10/2026 (America/Sao_Paulo), branch gameplayOverhaul/base 3a3f0ee, workspace com alterações locais preservadas. MCP confirmou Tech-Guy@fccafb2b47cd7671, projeto D:/Dev/Personal/My_games/Tech-Guy, Unity 6000.5.10f1, NexusLobby em Edit Mode. Console inicial: zero erros, aviso de depreciação do Input Manager. AGENTS.md atual e skills locais de combate/aceite/visão consultados; referências a documentação antiga não autorizam restaurá-la.

Escopo: apresentação de Q/W/E nativas sem Mutation. Scheduler/ImpactEvents/ledger continuam os únicos donos da emissão mecânica; nenhum timestamp, custo, cooldown, hitstop, gate de interrupção/cancel ou básico muda. A apresentação usará os tempos dos eventos em vez dos delays legados dos steps; o contato emitido deve permanecer visível no seu frame e durante hitstop, sem ser substituído pelo avanço seguinte da coroutine. Pausa conserva a pose; cancel/morte/troca/novo cast limpam a apresentação. Reutilizar SkillAnimationPlayer e LateUpdate existentes, sem controller/clock paralelo. R nativo (Channel Timed, 18 eventos, custo/energia únicos) e Cadenciada permanecem no caminho atual; investigar/testar não autoriza mudar sua execução. Mutations e executores Arco/Lança não serão migrados implicitamente.

Critérios: testes reproduzem contato perdido antes e passam depois com modelos/clips reais; capturas Q/W/E equivalentes, pose no frame do evento, dano/recursos/fases comparados; consecutivos/pausa/cleanup, regressões e Console. Matriz recebe classificação das falhas anteriores e resultados reais; game feel depende de aceite humano. Evidência bruta em Library/GameplayObservation/iteration13, sem relatório documental paralelo.


### Auditoria das 12 falhas herdadas antes do gameplay 1.3

Nova execução dos mesmos 12 em conjunto, sem mudança de runtime: job `d1562529557a470f8278955d72979ee4`, 2/12 passaram, 10 falharam. A classificação histórica nove persistentes/três sensíveis descreve a execução 1.2, não garante causa única. Fontes/asserts/stacks foram lidos; nenhum desses testes ou sistema adjacente foi alterado.

| Caso histórico | Classificação investigada em 1.3 / evidência |
|---|---|
| AuthoredEliteSpawnsExistingRankAndTwoActualDefenseAcquisitionsRequireInputRelease | Fixture de rota/recompensa falha em ReachManualReward/TryCommitDestination antes da validação visual. Não atribuir à animação; reconciliação da sequência com ManualRoutePolicy pendente. |
| GauntletPhase3AuthoredZeroChildDelaysKeepProducerFirst | Fixture injeta PressureField e ranks legados por reflexão. PressureField não é Mutation ofertada pelo catálogo vigente. O teste aguarda filhos em relógio real; hoje também falha no producer. Compatibilidade/emissão desse preset precisa investigação própria; FlurryEcho ainda é retido, não deve ser chamado integralmente aposentado nem reativado outro conteúdo para satisfazê-lo. |
| GauntletPhase3LegacyShockwaveKeepsItsEffectEventsAndDistinctProducer | Injeção de FlurryEcho/Shockwave por reflexão; zero echoes, antes de validar a onda. Shockwave não é oferta vigente, FlurryEcho é retido. Falha de preset/compatibilidade de emissão permanece aberta; não resolvida nem atribuída ao novo visual. |
| GauntletPhase3SharedHudGroupsEffectiveGainsShowsMutationsAndUnbinds | Loop Select sobre todos os enums exige sucesso, mas DemolishingAdvance e EclipseUppercut ocupam Q e Select sem replace rejeita slot ocupado. Expectativa de fixture incompatível com inventário atual; não liberar duas Mutations por slot. |
| ManualExhaustedTechniqueAppliesRetainedProtocolOnceAndPreservesResourcesAndSourceAssets | Teste esgota catálogo legado e exige ManualTicket.IsAlternative. Inventário expanded possui outras ofertas: esgotar só técnicas legadas não comprova catálogo atual esgotado. Migrar fixture em tarefa própria; não retirar novas ofertas. |
| ManualImpossibleFirstMutationKeepsBossBlockedAndAllowsAbortWithoutVictory | Mesmo loop Enum/Select colide em Q antes de exercitar recuperação/abort. Fixture deve representar indisponibilidade por slots atuais, sem mudar o gate do boss. |
| PauseConfirmedAbortWaitsForResumeFrameAndReturnsFromUnavailableMutationWithoutVictory | Mesma colisão de Mutations antes de testar pausa/abort. A falha não comprova defeito de pausa. |
| RunFocusDiscountsEffectiveQCostOnceForEachWeaponFamily | Exige encontrar oferta focus. RunBoonOfferCatalog não oferece esse bônus legado de stats/custo; teste antigo fora do catálogo atual. Não reativar focus. |
| ActualDamageMergesPerVictim_AndLethalConfirmationSurvivesDeath | Query global de todos os DamagePopup, sem isolamento prévio. Falhou com cinco em conjunto; sozinho passou 1/1, job `4f5c12d270714a62aed7a699710e2475`. Reclassificado como contaminação/contexto de apresentação no teste, apesar de historicamente persistir no grupo. Causa completa do ciclo dos popups ainda requer tarefa própria. |
| GauntletPhase3DemolishingAdvanceTraversesBodiesAndLaunchesOnlyOnBreak (sensível histórica) | Passou em conjunto e isolado. Valida posição/HP em cast real sujeito a timing/physics; causa da falha ampla anterior não confirmada. Não rebalancear o dash/ataque. |
| ManualRewardStaleInventoryClickRebuildsThenRealMutationIsRecordedOnlyOnAcquisition (sensível histórica) | Agora falhou também isolado: assume WeaponRunModifiers.TryDefinition para a primeira oferta, mas o catálogo contém IDs expanded. Sensibilidade à seleção/seed/catálogo, não apenas ordem de execução. Atualização da fixture pendente. |
| Release_StopsHoldRepeats_PreservesTapExecution (sensível histórica) | Passou em conjunto e isolado. A espera acumula Time.deltaTime e exige repeats dentro de 3 intervalos, com busy/approach/frames reais. Fragilidade temporal provável; não demonstrada alteração de básico e nenhum básico será modificado. |

Isolados dos três sensíveis históricos registrados em `historical-sensitive-alone.json`; auditoria conjunta em `audit-initial.json`; popup isolado em `popup-alone.json`, sob iteration13. Nomes, asserts/stacks preservados; logs repetitivos omitidos. Causas não confirmadas continuam hipóteses. Não há autorização implícita para consertar run/boons/HUD/ataques básicos nesta fatia.


### Implementação e verificação da 1.3

Causa confirmada: coroutine selecionava motion/delay do AreaHitStep, enquanto FixedUpdate emitia pelos ImpactEvents; o Contact imediato era substituído antes do render. Alteração restrita a BreakerGauntletCombat: cursor visual do último evento emitido + frame, reset por execução, seleção temporal pelos eventos e apresentação final no LateUpdate. SkillAnimationPlayer permanece reutilizado sem alteração. Nenhum timer mecânico foi substituído nem curva/clip/asset foi reautorado. O caminho legado permanece para R, Mutations e ações sem scheduler. Revisão do diff contra cópia 1.2 cujo hash coincide com o início 1.3 confirmou esse escopo.

**Teste antes/depois:** fixture com TG_Rigged, AnimatorController e habilidades reais, isolada de AI/NavMesh, executa cast e observa fim do frame. Antes falhou no E: contato esperado0,55, render observado0,594838; job `665274d1f19d44419e1f0e75eb7fc6dd`, `contact-regression-before.json`. A primeira tentativa de fixture tinha uma referência por reflexão a campo público e gerou NullReferenceException; foi corrigida na fixture, sem contar esse erro como reprodução do bug. Depois passaram os quatro casos novos: Q/W/E mantendo contato até render e durante pausa, e Q→E→W sem graph/contato residual. Esses testes não usam alvo adulterado de produção nem comprovam dano por si; as capturas abaixo exercitam dano real.

**Resultados Unity atuais:**

- 11/11 no kit após correção, job `fc3bb5073ea948afb4901279294ec75b`, `kit-after.json`.
- **66/66 PlayMode** focados, job `1ace12c2639349df822a1d4983265ab5`, `playmode-focused.json`: kit, scheduler/ledger, cancel/no duplication/projétil emitido, dash/buffer, hitstop/pausa, PostureReaction e RangedFluidity. O arquivo AsuraEarlyFinish é uma partial de BasicAttackR1PlayModeTests, não classe separada; seus casos foram executados explicitamente na próxima rodada.
- **7/7 PlayMode** adicionais, job `926f7275888f4be0b714e348f73e090b`, `r-and-movement.json`: AsuraEarlyFinishAfterFirstStrikeUsesOneCostAndThreeQuarterNativePower; AfterSecondStrikePreservesTheFullThreeStrikePower; WaitAndContactRespectPauseMenusAndRealHitstop; MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce; PauseDiscardsRecoveryMovementAndDoesNotReplayOnResume; AcceptedNewSkillClearsRecoveryMovementInsteadOfReplayingItAfterward; AcceptedDashClearsRecoveryMovementWithoutAnOldOrderAfterTheDash.
- **22/22 EditMode**, job `e371a1b2290348ef95723853e1079070`, `editmode-final.json`: RangedFluidityContractTests e ObservationTelemetryTests.
- A bateria herdada de 12, repetida depois, teve **1/12 aprovado e 11 falhas**, job `d8fa9b86e36c4e4fb0e403e8d8150f55`, `audit-after.json`. Em relação ao grupo inicial 2/12, Demolishing voltou a falhar. Novamente isolado passou1/1, job `375d8c1cdbc44de6b58d5989530dc543`, `demolishing-after-alone.json`. A Mutation usa o caminho inalterado; isso reforça sensibilidade, mas não fecha a causa da falha conjunta. Popup também passou isolado antes. Não declarar a suíte ampla verde, nem dizer que todas as falhas são obsoletas: compatibilidade/emissão, ciclo de popup e fragilidade de fixture permanecem com causas parciais/pendentes.

R investigado: assets/profile e executor mantêm Channel Timed nativo, 18 eventos e ganho/proteção/custo existentes; Cadenciada tem executor/continuation/cash-out próprios. Testes de emissão nativa e três cenários Cadenciada passaram. Nenhuma execução de R foi modificada; não houve captura de R com energia conquistada naturalmente e não se afirma que sua sincronização visual foi resolvida.

**Capturas novas:** todas FirstSector/nó1 real, cinco spawns de produção, seed1701, 1280×720, comandos aceitos no frame30, um PNG por frame. Nenhum HP/postura/dano/IA foi alterado para fabricar o resultado. Antes capturado nesta iteração, após auditoria e antes do patch; depois sob mesmas condições verificadas pelo comparador, sem overrides ou lacunas.

| Skill | Antes | Depois | Relatório sob iteration13/ |
|---|---|---|---|
| Q,180frames | `20261020T233031980-7c238711` | `20261020T233807689-1b931acb` | `q-comparison/comparison.json`,180pares |
| W,240frames | `20261020T233047828-8e84617d` | `20261020T233830021-6c067a23` | `w-comparison/comparison.json`,240pares |
| E,180frames | `20261020T233106085-9c226992` | `20261020T233852823-585debb0` | `e-comparison/comparison.json`,180pares |

Caminhos acima relativos a Library/GameplayObservation, cada run com manifest.json/events.jsonl/frames. Parâmetros em iteration13/*-request.json. Telemetria/checks em `event-checks.json`, `comparison-checks.json`; o verificador local reproduzível `check_contact_evidence.py` passou para os11 impactos confirmados. Não tratar um campo/estado do graph como aprovação visual: os PNGs também foram abertos.

| Critério | Antes → depois observado |
|---|---|
| Q contato | Primeiro dano43: PunchRight0,1717 → PunchLeft0,3333. Segundo53: PunchRight0,4444 → PunchRight0,3333. |
| W contato | Os sete danos39/48/57/67/77/84/98 agora alternam a mão correta e mantêm contato0,3333 no frame de emissão. Antes havia motion seguinte e fases0,0617…0,6296. |
| E contato | Primeiro dano47: Slam0,1571 → PunchLeft0,3333. Segundo62: Slam0,6000 → Slam0,55. |
| Dano ofensivo | Q14/22; W seis9,2 +4,8 HP restantes do alvo; E10/44. Mesmos atores, frames e timestamps escalados exatos antes/depois nesta rodada, inclusive W. Não extrapolar para todo RNG/seed. |
| Recursos/feedback | Custos56/77/98, finais mana1500/1500/1473; Asura10 em cada cenário; hitstops2/7/2 antes/depois. Contato mantido nas amostras com hitstop ativo, inclusive Q53–54, W98–99 e E62–64. |
| Recovery | Q e E mesmas transições/fim observados: Q57/69 e E68/82 (recovery/graph liberado). W101/109 →102/110. Em tempo escalado, entrada de recovery W1,566678 →1,566680; variação de um frame sob hitstop real/readback, sem alteração do profile. |
| Cooldown | Q/E mesmos timers finais; W restante2,833321 →2,849988 no frame final. Recarga nominal/código do holder não mudaram; não declarar igualdade frame a frame do cooldown W. |
| Alvos/retomada | Campos mecânicos comparados de ActorSnapshot/ReactionSnapshot sem diferença em Q/E. W teve15 amostras distintas (inclui rotação do Sniper e dano recebido187→188,21,59998HP); não afirmar determinismo completo de IA. |

**Inspeção visual realizada:** PNG Q43 antes/depois; W39 antes/depois; E47 antes/depois e E62 depois; prancha e-comparison/contact-sheet-008.png (56–63). A alteração de pose no contato está visível. Não foi assistido vídeo nem inspecionada manualmente toda a sequência de600pares. Os testes validaram pausa/consecutivos na fixture; não foram produzidas capturas novas com comandos físicos, pausa do usuário ou combo natural prolongado. Alinhamento espacial do punho, amplitude, transições e sensação de peso ainda pedem avaliação humana.

**Status:** sincronização temporal das Q/W/E nativas `VALIDADA_TECNICAMENTE`, com evidência nas três capturas reais. Game feel não marcado APROVADO. Pendentes: nove falhas históricas reclassificadas e três sensíveis conforme auditoria, equivalência estrita de timers/IA no W sob hitstop real, R/Mutations como escopo separado, geometria do contato e playtest humano. Nenhum boon foi criado/reativado e básicos não foram modificados.


Fechamento 1.3: hashes iniciais/finais de Assets/_Project, Packages e ProjectSettings confirmaram somente BreakerGauntletCombat.cs e BreakerKitEmissionPlayModeTests.cs modificados, nenhum arquivo novo/removido nesse escopo. Cenas, prefabs, clips, ScriptableObjects, metas e configurações preservados; as546 exclusões Git preexistentes permaneceram idênticas. Diff --check do C# passou,41 hashes documentais atualizados e links locais sem destinos ausentes. Editor final: NexusLobby limpa, Edit Mode, timeScale1, captureDelta0, seleção vazia, qualidade5. Console final zero erros, sem limpar histórico. Registros `iteration13/integrity-final.json` e `editor-final.json`. Nenhum commit, restauração de exclusão ou aprovação humana de game feel foi realizado.
## Vertical Slice 1.0 — auditoria e planejamento, 20/10/2026

**Sem implementação de gameplay.** Instância confirmada `Tech-Guy@fccafb2b47cd7671`, projeto `D:/Dev/Personal/My_games/Tech-Guy`, Unity 6000.5.10f1, branch `gameplayOverhaul`, base `3a3f0ee8ad82734104511b63877d38df512ea146`, com alterações locais preservadas. O hash da base não representa sozinho o workspace. Artefatos desta auditoria em `Library/GameplayObservation/vertical-slice-audit/`, ignorados pelo Git; classificação durável em [estado](../02_ESTADO_E_LACUNAS.md) e planejamento em [prioridades](../03_PRIORIDADES.md). Nenhum aceite humano concedido.

### Editor e assets inspecionados

- Início/fim em NexusLobby, Edit Mode, cena sem dirty, seleção vazia, timeScale 1, captureDeltaTime 0, qualidade 5, runInBackground true. Preview scenes abertas/fechadas sem salvar.
- `ManualRouteValidator.TrySnapshot` aceitou `FirstSectorManual` com Manopla. Grafo tem quatro caminhos; nó 5 concede Mutation antes do ramo 7/Elite, boss 10 requer aquisição de Mutation. Randomização preserva grafo e Elite autorado, não fixa quantidade/papéis de cada run.
- Zero missing scripts nos MonoBehaviours das duas preview scenes. Nenhuma referência quebrada detectada nos campos inspecionados de `LobbyArsenal`, `LobbyInteraction`, `ProgressionDirector` e `PlayerHUD`; isso não certifica todas as referências do projeto. Dados registrados em `editor-assets.json`.
- Console consultado durante e ao fechar a verificação: zero erros. Avisos de `AbilityHolder` por array de teclas menor que o kit, com fallback Q/W/E/R; não corrigidos nesta tarefa. Não houve limpeza do Console nem compilação de novos scripts de produção. Duas consultas dinâmicas inicialmente incompatíveis com a API Unity foram corrigidas (EntityId e RewardSeed), sem edição do projeto.

### Testes Unity atuais

Job PlayMode **`492e0db3f65843a58507acbcd80aae3c`**, **8/9 passaram**, uma falha, zero skipped, duração reportada **47,204 s**. Resultado por nome/assert/stack em `integration-tests.json`; logs repetitivos omitidos. Não é nova execução da suíte ampliada nem aprovação dos 158 testes.

| Casos executados | Resultado | O que efetivamente demonstram |
|---|---|---|
| `ManualPublishedRunPlayModeTests`: `FourPublishedPathsReachActualBossAndExtractToNexusWithFreshRuns`, `PublishedDeathReturnsToNexusAndBowUsesGeneratedRoomsWithCompatibleInventory`, `PublishedMapOwnsInputAndClosingWaitsForClickRelease`, `PublishedSpearUsesGeneratedRoomsWithCompatibleLocalProtocolInventory`, `PublishedWeaponSwapsKeepFamilyInventoryAndOwnedAbilityCopiesWithinOneSession` | **5/5 Passed** | O primeiro percorre quatro caminhos × três armas nas cenas publicadas, com aquisições reais, Mutation, Elite/boss e extração; os demais verificam morte/retorno, input e inventários/cópias. Usa Warp e morte por `TakeDamage(1000000)`: não mede combate, dificuldade ou navegação natural do percurso inteiro. |
| `ManualRunPlayerFeedbackTests.PublishedFloorClickActuallyMovesAfterChoosingDestination` | **Passed** | Clique/raycast de produção e NavMesh realmente movem o jogador após escolha. Não é caminhada de toda a rota. |
| `ManualRunPlayerFeedbackTests.FirstTwoRoomsNativeLethalFeedbackAutomaticallyPresentsRewardAndRespectsDismissal` | **Passed** | Básico confirmado, fronteira/feedback, abertura automática, dismiss/reabertura e ordinais respeitam release. Fixture reduz HP a 1 e agrupa inimigos; não usar como vitória com balanceamento real. |
| `BasicAttackR1PlayModeTests.FourPathsAcquireMutationAtC4AndExecuteItsRealShapeInNextCombat` | **Passed** | Oferta e aquisição de Cadenciada, três impactos/custo único em fixture. Usa recurso/energia/alvo artificiais e stand-ins de rota: não certifica chegada e uso natural no Elite. |
| `BasicAttackR1PlayModeTests.AuthoredEliteSpawnsExistingRankAndTwoActualDefenseAcquisitionsRequireInputRelease` | **Failed**, mesmo assert herdado em `ReachManualReward:16`, chamado em `ManualRunElitePlayModeTests:43` | O setup conclui só ordinal 0 de C1 e tenta avançar. Não chega a verificar rank/recompensa do Elite. |

**Causa do bloqueio da fixture Elite confirmada separadamente:** probe sem cena/efeitos sobre sessão de produção, usando snapshot importado e política real. C1 tem duas aquisições; após ordinal 0, fase `RewardPending` e commit para 2 = false; após ordinal 1, commit = true. Registro `elite-fixture-policy-probe.json`. A fixture precisa consumir toda a sequência vigente, preservando o que pretende testar. Não mudar `ManualRoutePolicy` nem reduzir aquisições para acomodá-la. A passagem atual pelos quatro caminhos publicados impede classificar essa falha como prova de rota de produção quebrada.

### Smoke real e inspeção visual

Nexus em Play Mode → equipar Manopla pelo `WeaponLoadout.Select` → aproximação assistida com Warp à estação → teclado temporário E e polling de `LobbyInteraction.Update` de produção → FirstSector → `ManualRunUI.TryAbortRun` → Nexus. Não se chamou LoadScene diretamente para substituir a estação, não se mudou HP/dano/postura e não se venceu encontro. O teste **não** prova a troca por E no pedestal nem caminhada natural. Keyboard removido e prefs de arma/desbloqueios/moedas restaurados ao snapshot do smoke (saldo 2433); nenhum gasto/concessão de moeda feito pelo smoke.

Seed gerada **4701776757987578804**, C1 com cinco spawns: Spawner, Fragile, Grunt, Heavy e Shooter. A composição foi registrada, sem prometer essa mistura em toda seed. PNGs reais por ScreenCapture, **1160×599** (Game View existente, sem redimensionar), abertos e inspecionados visualmente nesta auditoria:

| Captura, relativa à pasta da auditoria | Observação visual / limite |
|---|---|
| `nexus-gauntlet.png` | Card Gauntlet/Manopla equipado e HUD visíveis. Rótulos grandes dos pedestais se sobrepõem; problema de legibilidade observado nessa câmera/resolução, sem medição de dificuldade de uso. |
| `nexus-incursion.png` | Estação 01/INCURSAO apresenta `[E] Iniciar incursão`. |
| `first-sector-map.png` | Próxima passagem anuncia Technique + Protocol; HUD presente sob overlay. |
| `nexus-return.png` | Capturada ainda em FirstSector: `Run encerrada`, `Aborted`, retorno `Loading`. O nome do arquivo não é prova de Nexus carregado. |
| `nexus-return-final.png` | Nexus efetivamente carregado; não apresenta painel da última run. Consulta runtime confirma ausência de `ProgressionDirector`/`ManualRunUI`; `nexus-return.json` registra cena e saldo. |

`portal-smoke.json` contém snapshot de rota/materialização planejada; `abort-smoke.json` confirma aceitação/outcome/seed. Não há vídeo, comparação antes/depois de mecânica nem nova avaliação de combate neste smoke. As capturas GI1.0–1.3 continuam baseline histórico de combate; não foram substituídas por essas imagens de hub/UI.

### Dívida herdada e gates de entrega

Dos 12 casos herdados, somente o de Elite foi reexecutado nesta auditoria. Os demais mantêm os resultados e limites da seção GI1.3; não declarar nova contagem nove/três ou que foram resolvidos. Trilha separada de correção em [prioridades](../03_PRIORIDADES.md):

- **Evidência bloqueada por fixture:** Elite (causa confirmada acima), stale inventory, esgotamento/fallback, pausa/aborto indisponível. Requerir cobertura vigente equivalente para entregar esses contratos; não confundir setup obsoleto com funcionalidade ausente.
- **Risco a isolar se fizer parte da build:** FlurryEcho retido nas duas fixtures com PressureField/Shockwave fora da oferta vigente. Não habilitar conteúdo aposentado nem encobrir o suporte retido.
- **Dívida investigada sem bloqueio de produção demonstrado:** `focus` aposentado, HUD/colisão de Mutations Q, contagem global de DamagePopup, DemolishingAdvance e release/repetição sensíveis. Sem causa completa para os dois últimos; nova falha natural pode promover a prioridade. Não enfraquecer asserções nem alterar Mutation para obter verde.

**Bloqueios reais do objetivo completo:** consulta de resultado da run no Nexus ausente; combate natural com build transformada→Elite ainda sem validação integrada; escopo de vitória/abandono, resultados e acabamento visual por aprovar. O relatório de dano por habilidade permanece lacuna de produto, sem adiamento aprovado. Manopla GI1.0–1.3 preservada; ausência de mudança runtime dispensa repetir toda sua suíte nesta auditoria documental, mas não dispensa regressão quando a próxima fatia tocar gameplay.

**Integridade da auditoria:** hashes antes/depois mostram somente cinco documentos canônicos e `MANIFEST.json` alterados nesta tarefa; código, cenas, prefabs, assets, metas e configurações preservados. As 546 exclusões pendentes permanecem idênticas; nenhum arquivo previamente existente foi removido. Links Markdown locais de AGENTS/README/Docs/skills verificados e hashes das 41 entradas do inventário atualizados. Registros `hashes-before.json`, `deletions-before.txt` e `integrity-final.json` na pasta da auditoria. Sem commit/stage, restauração de documentos antigos ou aceite implícito do escopo proposto.

Após a retomada da conversa, uma releitura de `mcpforunity://editor/state` falhou por transporte HTTP em `127.0.0.1:8080/mcp`. O último estado confirmado é o cleanup acima, fora de Play Mode; não foi declarada nova consulta bem-sucedida nessa retomada. Os testes, capturas e inspeções anteriores possuem respostas efetivas do Editor.


## VS-01 — Mutation adquirida e combate Elite, 20–21/10/2026

**Resultado:** fixture corrigida; integração assistida de aquisição→executor transformado→dano na Elite→fim do encontro→abandono/Nexus demonstrada. Não é partida natural, comparação controlada de balanceamento ou aceite humano de game feel. Projeto `D:/Dev/Personal/My_games/Tech-Guy`, instância `Tech-Guy@fccafb2b47cd7671`, Unity 6000.5.10f1, branch `gameplayOverhaul`, base `3a3f0ee8ad82734104511b63877d38df512ea146` com alterações locais. Skills locais de aceitação Unity, combate e boons consultadas. Registros locais em `Library/GameplayObservation/vs01/`; não são dependências documentais nem arquivos garantidos em outro checkout.

### Correções limitadas a fixture e captura

- `ManualRunElitePlayModeTests.cs`: os nós prévios agora consomem `TotalAcquisitions`/`RewardAt(ordinal)` antes de avançar. C1 tem duas aquisições. Após isso, surgiu outra expectativa obsoleta da mesma fixture: esgotar técnicas e exigir uma única oferta `bulwark`/Armor. Como esse boon está aposentado, o setup agora adquire duas ofertas vigentes, verifica inventário/contagem efetiva e mantém as asserções de release, sem reativar conteúdo. O nome histórico do método foi preservado.
- Asserções executadas de Elite: cinco inimigos, exatamente uma Elite e quatro normais; perfil com multiplicador de vida >1 e postura >0; fechamento real da fronteira; dois tickets aceitos uma única vez, segundo bloqueado enquanto a tecla continua pressionada; liberação habilita escolha; nó 8 disponível e 6 excluído. A fixture ainda usa stand-ins de rota, Warp e morte determinística: seus resultados não foram usados como prova de vitória natural.
- Vermelho intermediário importado `ed1ae166dc7b469aa31b9455c1b58cb9`: após corrigir ordinais, `Choices.Single()` falhou nas ofertas antigas. Verde completo `3797d929bceb4d27aece9c556225d67a`: **1/1**, depois incluído na regressão 95/95. O primeiro job `3f449a66b1f64e1eb93fb6252bc7afec` ainda executou assembly anterior ao import; não foi atribuído à revisão corrigida.
- `CombatPolishPreview.Observation.cs`: `current` falhava antes de capturar, porque `SaveObservationState` chamava `EditorSceneManager.GetSceneManagerSetup` em Play Mode. Agora só obtém setup em Edit Mode; mantém a restauração de cenas existente. Correção de ferramenta necessária para observar o percurso, sem editar gameplay. Capturas completas/restauradas e 10/10 testes de observação após import/compilação.

`ManualRoutePolicy`, ranks, HP, dano, postura, custos, recursos, resistência, cancelamentos, boons e autoria não foram modificados.

### Execução real final e assistência explícita

Nexus→estação por caminhada assistida/E temporário→FirstSector→**0→1/C1→2/C2→3/reparo→5/C4→7/Elite**. Entrada em `attempt-09`; percurso e combate final em `attempt-10/driver.jsonl`. Seed gerada **5247271230365345558**, sessão **e8725309-2923-49b6-8662-cdf5cc8239bf**. O próximo combate depois da Mutation nesse ramo já é Elite; não houve encontro intermediário inventado.

| Trecho | Executado / assistência / limite |
|---|---|
| Nexus e Manopla | Loadout real equipado; caminhada por NavMesh e E temporário/polling da interação existente. Seleção por API foi verificada na preparação. Não valida descoberta e troca manual pelo pedestal. |
| Travessia e rota | `ManualRunUI.TryChooseDestination` e entrada reais; `NavMeshAgent.SetDestination` assiste deslocamento, sem Warp no percurso observado. APIs/UI dirigidas não equivalem a teclado/mouse humanos. |
| Combate | `AbilityHolder.TryUseAbility`, raycast/seleção de `CharControlScript`, cursor atualizado em cada frame do player loop. Aproximação por movimento de chão existente antes de selecionar/castar. Inimigos e skills usam executores de produção; nenhum dano/HP/postura/energia/mana foi injetado. |
| Recompensas | Tickets/ofertas/custos/requisitos reais, todos os ordinais, `RunBoons.Choose` seleciona opções oferecidas. Não há Mutation adicionada ao inventário por fora da aquisição. Reparo do nó 3 recuperou HP a 1000 pela regra existente. |
| Desfecho | Elite e comuns derrotados, nó 7 `RewardPending`, zero inimigos vivos, jogador **940,12 HP**. As duas recompensas posteriores foram adquiridas e `ManualRunUI.TryAbortRun` retornou ao Nexus com **Aborted**, não Victory. Boss/extração não executados nesse percurso. |

Build antes da Elite: `weapon_FlurryEcho`, `ignite`, `conductor`, `augment_gauntlet_echoing_fist`, **`mutation_gauntlet_eclipse_uppercut`**. Mutation oferecida em C4 junto de `gauntlet_mutation_CadencedAsura` e adquirida no frame **9226**, ordinal 0. Frost e TemporalEcho foram adquiridos **depois** da vitória no encontro, portanto não explicam seu resultado. ViolentFall não fazia parte dessa build.

**Correção da mira da automação após observação do desenvolvedor:** o driver anterior escrevia no update do Editor e perdia o cursor, usando ponto fixo; `MakeCurrent` sozinho não corrigiu. Driver temporário passou a operar em coroutine do jogador, selecionar o inimigo pelo raycast real e seguir sua posição durante casts, isolando/restaurando os outros mouses. Em 69 amostras da tentativa final, erro máximo de projeção no chão **0,004674 unidade**; seleção ainda pode ser vazia na aproximação. Amostra 9981 confirma `Magic_Haste(Clone)` selecionado durante cast; recibo 9972 confirma o acerto. Isso não muda targeting de produção nem prova precisão em todos os frames. Script temporário `drive-production.cs.txt` em Library, sem novo componente importado.

### Transformação, contato, recursos e resistência

| Fato | Evidência e limite |
|---|---|
| Q anterior | Snapshot real da Q nativa: custo 56, dois passos diretos, avanço 2,8 / 0,16 s, active 0,6 s. C4 registrou impactos 14 e 22 no mesmo executor em 9190/9199. |
| Q transformada | Snapshot `EclipseUppercut`: custo 56, **um passo direto**, **avanço 0**, active 0,55 s. Valores padrão de tuning no código: alcance frontal 2,4 e largura 2,8; golpe Heavy e fallback de controle por perfil. A geometria é leitura de código, não medição visual de alcance. |
| Uso contra Elite | Q aceita em **9966**, mana **1500→1444**, delta 56 confirmado por `ManaChanged`. Impacto em **9972**, tempo escalado **150,97995 s**, execução/root **57**, slot Q, índice 0, origem Direct, alvo **568105589213661194**, geração 1. Intervalo desde aceitação: seis frames / cerca de 0,11815 s escalados nessa execução livre. |
| Dano confirmado | `Magic_Haste(Clone)`, rank Elite: **120→84 HP**, dano factual **36**. Recibo primitivo liga snapshot/Mutation/alvo; `ActorDamageReceived` é a fonte de total. Não somar hooks/recibos diagnósticos duplicados. Outros recibos sob contexto Q podem incluir procs aninhados; não atribuir todo o dano causal a golpe nativo. |
| Reação/ameaça | Recibo pós-dano mostra postura 76 e `airborne=false`; telemetria da Elite teve zero amostras airborne/stunned e nenhuma quebra de postura. Duas transições de windup `DoublePunch`, máximo de um impacto resolvido. Não foi forçado launch/stagger nem declarada quebra natural inexistente. Testes específicos cobrem fallback/mark em Heavy/Elite/boss. |
| Budget | Snapshots consultados: `LiveRoots` máximo 1, `MaximumQueue` 2, todos os contadores `Dropped*` 0. Isso é amostragem, não medição do pico absoluto entre frames. Testes de reserva negada, caps, cleanup e raiz compartilhada complementam a observação. |

A mudança de forma tem evidência no snapshot e na sequência renderizada: Q estacionária, contato e redução da barra da Elite. A apreciação de peso, clareza da marca e distinção em swarm permanece humana. Não há alegação de que a Mutation sozinha venceu o encontro; básicos, W/E e Protocols também atuaram.

### Capturas verificadas e realmente abertas

Três execuções `current`, todas `complete` e restauradas; **1800 PNGs** 1280×720, cadência de três frames, **43693 eventos**. Integridade de cada PNG (hash/CRC/dimensões) e dos eventos verificada em `attempt-10-evidence.json`. Sem vídeo exportado. Diretórios sob `Library/GameplayObservation/`:

| Run | PNGs / eventos |
|---|---|
| `20261021T031420596-5143c100` | 600 / 18555 |
| `20261021T031512240-65a275f3` | 600 / 9702 |
| `20261021T031600726-0016cacc` | 600 / 15436; aquisição C4 e combate Elite |

PNG originais da terceira run abertos, também reunidos em `vs01/attempt10-contact-sheet.png` para inspeção:

- `frames/frame-000126.png`: Q nativa contra Heavy, um frame após o recibo 8947. Condições/alvo diferentes da Elite; não é antes/depois estritamente equivalente.
- `frames/frame-000402.png`: overlay de recompensa antes da aquisição. **Não mostra o card da Mutation**; escolha/ID comprovados pelo ticket/log, sem análise de leitura humana desse card.
- `frames/frame-001143.png`, `001146`, `001149`: aproximação/início/progressão; `001149` corresponde a 9971, um frame antes do dano.
- `frames/frame-001152.png`: 9974, **dois frames depois do dano**, barra reduzida, feedback e pose de contato. Não fingir captura exatamente no frame 9972.
- `frames/frame-001155.png`, `001158`, `001164`, `001170`, `001182`, `001200`: progressão/retomada, telegraphs e ações seguintes. Dois atores próximos se sobrepõem; atribuição de reação depende da telemetria, não só da pose. Não declarar qualidade de animação aprovada.
- `vs01/attempt10-elite-completed.png`: ofertas de recompensa do nó 7 após vitória, confirmado por `attempt10-elite-result.json`. `attempt10-nexus-return.png`: Nexus carregado, sem painel de resultado consultável; `attempt10-abort.json` e `attempt10-return.json` confirmam desfecho/cena.

`current` usa simulação livre, sem reseed da IA e sem comparação estrita de condições. Nativa em C4 versus Mutation na Elite demonstra formas em contexto, não ganho comparativo de DPS, dificuldade ou feel. Não se alterou o manifesto para satisfazer o comparador seeded.

### Testes Unity efetivamente executados

| Job / arquivo sob vs01/ | Resultado | Cobertura efetiva |
|---|---|---|
| `3797d929bceb4d27aece9c556225d67a` / `vs01fixtureFixed.json` | Play **1/1** | Fixture Elite corrigida; repetida no lote seguinte, sem somar caso duas vezes. |
| `66b5e688445148b5b96a78a482700b7e` / `vs01regression.json` | Play **95/95** | Elite, cinco PublishedRun (quatro caminhos×três armas incluídos), dois PlayerFeedback, 19 PostureReaction, 11 BreakerKitEmission, dez HitFeedbackR7, cinco CancelNoDuplication, oito DashCancelResolver, 34 ProtocolRuntimeDamage. GI1.0–1.3/cancel/pausa/hitstop/recursos/dano/resistência/retorno cobertos nos casos filtrados. |
| `decec33e06424e7c9445b5d1798d1eb4` / `vs01extraPlay.json` | Play **12/12** | Oito RangedFluidity e quatro Eclipse: aquisição real, cancelamento antes do contato, passo grosseiro, morte/cleanup. |
| `3ef002646ff74855a10b1985df714ee8` / `vs01eclipse.json` | Play **6/6** | Heavy/Elite/boss sem virtual launch, raízes compartilhadas 8/32/64 alvos, pausa/hitstop, fim de encontro/troca de arma, Q estacionária/empty cast, W/E e marcas. |
| `66602dfad9df440db48766438bd2ba54` / `vs01edit.json` | Edit **67/67** | Eclipse, compatibilidade/capability graph, randomização de rota e seed de recompensa. |
| `b099985320754f949e97704c8043fe31` / `vs01obsEdit.json` | Edit **10/10** | ObservationTelemetry, após correção/import da captura current. |

Agregação dos filtros: **113 PlayMode + 77 EditMode distintos aprovados**. Não é uma única suíte de 190 nem a reexecução dos 159 casos ampliados. Testes usam suas fixtures próprias; não confundir com percurso sem injeção. Console final do percurso fresco e após sair de Play: **zero erros**, sem limpar histórico; avisos persistentes de fallback Q/W/E/R em AbilityHolder continuam fora do escopo.

### Tentativas descartadas, falhas reais e pendências

- Tentativas iniciais: driver consultou componentes antes de prontos; captura current falhou como descrito; mira inadequada impediu acertos confiáveis. Não foram contabilizadas como prova da transformação na Elite.
- Um diagnóstico temporário tentou serializar contexto contendo Actor/Unity Object e causou StackOverflow/crash do Editor. Foi erro da instrumentação, não derrota legítima nem defeito atribuído ao combate. Editor reaberto; instrumentação substituída por payloads primitivos e callbacks protegidos. Não há listener desse diagnóstico incorporado ao projeto.
- `attempt-05` percorreu e venceu Elite (907,42 HP finais), mas Q transformada acertou apenas comuns; Elite morreu por outros ataques. As 2400 imagens dessa tentativa não comprovam impacto direto da Mutation na Elite. Tentativas seguintes de correção de mira também não substituem a prova final 9972.
- Na **segunda run da mesma sessão Play**, cinco `NullReferenceException` em `EnemyProjectile.Spawn:373`, chamados por AimedShot/SniperShot; raw `repeat-session-console.json`. Possível relação com pool/objetos após retorno ao Nexus, **causa ainda não confirmada**. Ataques podem ter sido perdidos; não chamar essa run de validação limpa. Não corrigido por estar fora da fixture/observação solicitada. A run final em Play novo não reproduziu o erro, mas não comprova ciclo repetido estável.
- Das 12 falhas históricas, **somente a fixture Elite é declarada resolvida**. Demais 11 preservam classificação/limites GI1.3; alguns contratos passaram em filtros atuais, sem apagar resultados antigos de outros casos. Não reativados focus/bulwark/presets aposentados, não alteradas regras de Mutation nem enfraquecidas asserções de produção.
- Faltam playtest humano com input físico, avaliação de diversão/legibilidade da transformação e estabilidade de múltiplas runs na mesma sessão. Resultado consultável no Nexus e escopo de boss/extração seguem gates separados, sem implementação autorizada por este teste.

**Fechamento:** Play Mode encerrado, NexusLobby limpa, seleção vazia, timeScale 1, captureDelta 0, qualidade 5, targetFrameRate -1, runInBackground true. Preferências restauradas ao snapshot pré-percurso (saldo 2468); dispositivos temporários removidos. Hashes pré/pós confirmaram mudanças somente nos dois C# descritos, quatro documentos canônicos e MANIFEST; nenhum arquivo não ignorado novo/removido, cenas/assets/configurações preservados, **546 exclusões Git preexistentes idênticas**, 41 hashes documentais atualizados e zero links locais quebrados. Sem commit/stage ou reversão de exclusões. Registros `vs01/integrity-final.json`, `test-aggregation.json` e `editor-final.json`; aceitação humana continua pendente.

## VS-01.1 — estabilidade de múltiplas runs, 21/10/2026

**Status: VALIDADO_TECNICAMENTE no escopo descrito; aceite humano pendente.** Instância/projeto reconfirmados por Unity MCP: `Tech-Guy@fccafb2b47cd7671`, `D:/Dev/Personal/My_games/Tech-Guy`, Unity 6000.5.10f1. Branch `gameplayOverhaul`, base `3a3f0ee8ad82734104511b63877d38df512ea146`, com alterações locais preexistentes. Logs/solicitações/JSON de apoio em `Library/GameplayObservation/vs011/`, ignorados pelo Git. Esta seção sucede o risco aberto da VS-01; não apaga os resultados históricos.

### Causa confirmada e reprodução anterior

`EnemyProjectile.Spawn:373` acessava `projectile.transform` após `Pool.Acquire()`. A origem nula era o retorno da aquisição, não owner/target/prefab. `ComponentPool<T>` contém um `SimpleObjectPool<T>` comum e uma raiz Unity da cena; a raiz e os filhos são destruídos ao descarregar FirstSector, mas o pool estático e os wrappers C# permanecem. `SimpleObjectPool` não conhece Unity-null. A aquisição retirava um wrapper destruído e `ComponentPool` o convertia em retorno null; o caller falhava antes de configurar/emitir o tiro. `PoolResetRegistry.BeforeSceneLoad` inicializa no começo de Play/domínio, não em cada run. Não foi constatada duplicação de inscrição como causa.

Reprodução de produção em **uma sessão Play**, `before-v2/events.jsonl`: Nexus→estação→FirstSector→0→C1→abandono→Nexus, repetido três vezes, sem LoadScene/Warp/dano artificial no driver. Leitura privada apenas de estado primitivo; nenhuma referência inválida foi injetada.

| Run anterior | Seed gerada / sessão | Emissões observadas / erro / resultado |
|---|---|---|
| 1 | `4695929019020262075` / `fbd6153b-8195-406f-be9e-431182f7217c` | 1 LobShot; zero erros; `Aborted`, retorno. Pool tinha 1 item livre válido antes da saída; no Nexus, mesma identidade do pool, raiz destruída e 1 wrapper Unity-null. |
| 2 | `5007001447347920661` / `049a4d6e-92d2-45a0-8091-503369161410` | 1 NRE em LobShot/Spawn:373 no frame 1930; 50 projéteis SpreadShot observados depois. O tiro que falhou retirou o item destruído sem criar outro. `Aborted`, retorno. |
| 3 | `5203392361176119759` / `ad8b98f8-879d-4d9d-a465-a3ecc9aec836` | 1 NRE em LobShot/Spawn:373; zero projéteis observados na janela; `Aborted`, retorno. Restaram quatro wrappers destruídos na fila. |

O log original VS-01 (`vs01/repeat-session-console.json`) tinha cinco exceções AimedShot/SniperShot. A reprodução nova ocorreu em LobShot, que usa o mesmo Spawn; os callers AimedShot e SniperShot foram exercitados especificamente pelos testes novos. `ResolvedImpacts` incrementa antes de Spawn: contador de ataque sozinho não comprova emissão. A ausência de projétil na terceira run foi correlacionada com erro e estado do pool, não deduzida apenas de PNG.

Uma preparação anterior (`before/`) não exercitou ranged na primeira run porque o jogador ficou fora da percepção do Sniper; foi interrompida e a segunda captura cancelada. A navegação assistida foi ajustada para aproximar-se de Shooter/Sniper/Bomber. Esse preparativo não conta como validação bem-sucedida.

### Menor correção e responsabilidades preservadas

Somente código de produção alterado: [ComponentPool.cs](../../Assets/_Project/Scripts/Core/Pooling/ComponentPool.cs). Antes de Acquire, se havia instâncias criadas e a raiz Unity foi destruída, o adaptador substitui seu armazenamento pelo `SimpleObjectPool<T>` já existente. A factory permanece a mesma; reuso na cena continua, raiz/filhos são recriados na cena ativa. Não foi criado pool, manager ou lifetime paralelo. Contagens valem para a raiz vigente.

O descarte é **lazy no próximo Acquire**, portanto um snapshot no Nexus ainda pode mostrar wrappers destruídos até o próximo tiro; isso não representa um item utilizável entregue ao caller. Não se tornou a raiz DontDestroyOnLoad nem se resetaram todos os sistemas a cada cena. EnemyProjectile, EnemyCombatActions, EnemyAttackExecution, dano, velocidade, cooldown, telegraphs, IA, boons e assets não foram modificados. Contrato principal em [inimigos](../systems/03_INIMIGOS.md#projéteis-entre-cenas--vs-011-21102026).

### Teste vermelho→verde e regressões Unity

Novo arquivo [EnemyProjectileLifecyclePlayModeTests.cs](../../Assets/_Project/Scripts/Tests/PlayMode/EnemyProjectileLifecyclePlayModeTests.cs), com meta gerado pelo Unity. As cenas aditivas da fixture são carregadas/descarregadas realmente; o pool é isolado apenas no início do caso, **nunca entre seus três ciclos**. Morte por TakeDamage aparece somente no teste de cascade, não nas runs observadas. Não se reduziram asserções existentes.

| Job / JSON sob vs011/ | Resultado | Cobertura e limite |
|---|---|---|
| `247086b2c92c49b793efc84f8067c352` / `lifecycle-red.json` | **0/1**, falha esperada antes da correção | `StaticPoolEmitsAcrossThreeUnloadedScenesWithActiveAndParkedShots` falhou em Spawn:373 na aquisição após descarregar a primeira cena. Mesmo teste foi mantido para o verde. |
| `bd0f4c59bbc14e838512921ddbda13a1` / `lifecycle-green.json` | **5/5**, 9,406 s | Três lifetimes com projéteis ativos e livres; AimedShot/SniperShot emitindo e viajando em cada ciclo; morte do dono sem dano posterior; reuso com novo dono/alvo sem listener antigo; cancel pré-emissão não dispara e pós-emissão preserva voo/dano único. |
| `f230ddd7f3ff489db2ace2decf7e65c1` / `regression.json` | **62/62**, 65,833 s | PublishedRun (quatro caminhos×três armas incluídos), RangedFluidity, EnemyAttackExecution, ProtocolProjectileContext, BreakerKitEmission, HitFeedbackR7, CancelNoDuplication e DashCancelResolver. Preserva regressões GI1.0–1.3, pausas, cancelamentos, dano/recursos e retorno nos filtros escolhidos. |
| `a1f8e05a00d34a8f8db4d90ecd8ba187` / `arsenal-regression.json` | **8/8**, 402,533 s | ArsenalNoDoubleHit, PierceFirstOrdering e RicochetDecay com 100 casos gerados cada; dois TerminalDestroy; três TrainingDummy, incluindo ausência de moedas. Verificam outros caminhos do adaptador compartilhado; não equivalem a playtest humano. |

**75 casos PlayMode distintos aprovados**, conforme `test-aggregation.json`. Compilação/importação pelo Unity MCP sem erros após mudança. Não foi executada nova suíte EditMode nem toda a suíte ampliada; as 11 falhas históricas restantes não foram corrigidas/reativadas para obter verde. O teste completo de coleta de moedas entre três runs não foi acrescentado: a correção é compartilhada, mas a prova principal é de projéteis.

### Três runs publicadas após a correção

Uma única entrada em Play Mode, três sessões de produção distintas e três retornos ao Nexus, sem reset manual de pools. `after/events.jsonl` registra seeds, sessões, frames, origem, dono/geração, alvo, cena, posição e identidade de cada motion; `after-analysis.json` e `runtime-checks.json` cruzam as evidências. Não se serializou Actor/GameObject/Component/UnityEngine.Object inteiro.

| Run após | Seed / sessão | Emissões observadas / Console / HP ao abandonar |
|---|---|---|
| 1 | `5069542931677737923` / `36c4f83e-a4cb-4dd8-abdd-a02fee01d74a` | **8** (6 LobShot, 2 SniperShot); **0 erros**; 812,800049 HP. |
| 2 | `4656190078205607662` / `1d595aaa-13ae-45d1-ba87-a2bcbad8062e` | **54** (45 SpreadShot, 9 AimedShot); **0 erros**; 376,000824 HP. Seis tiros ativos no momento anterior ao descarregamento. |
| 3 | `5144664225502072356` / `ddc45012-cb42-41f4-82e4-8c5e097f1b61` | **5 SniperShot**; **0 erros**; 892,0001 HP. |

Nas **67 emissões observadas**, raiz válida em FirstSector e zero Unity-null na fila após aquisição. Todos os alvos ativos amostrados corresponderam ao ID do jogador daquela run. Movimentos sucessivos da mesma identidade de motion registrados em 6/54/3 trajetórias amostradas; as demais são mais curtas que a janela de sampling, não foram declaradas paradas. O registro do pool manteve identidade externa e quatro callbacks de reset durante os três ciclos. Contagem de listeners de morte 1/0, ausência de assinatura no dono anterior e alvo novo foram assertadas na fixture. Após o terceiro retorno, consulta real ao Nexus confirmou **zero EnemyProjectile de cena**. Não é prova de ausência de todo listener obsoleto de qualquer sistema do jogo.

**Assistência:** NavMesh.SetDestination para andar até estação/C1/ranged, E temporário e polling de LobbyInteraction para entrar, ManualRunUI.TryChooseDestination/TryAbortRun para rota/saída. Nenhum ataque do jogador foi comandado nessa sequência; a finalidade foi receber/observar ataques ranged e exercitar unload. HP/dano/postura/recursos/perfis não foram alterados. São runs assistidas e interrompidas deliberadamente com `Aborted`, não partidas naturais, vitórias de encontro ou validação de build/Mutation adicional.

### Imagens verificadas e analisadas

Todas as seis capturas principais têm `complete` e restauração confirmada, PNGs **1280×720**, hashes/CRC/dimensões e hash de eventos verificados. Antes: 300 PNGs por run, a cada dois frames. Depois: 200 por run, a cada três frames, reduzindo armazenamento. Simulação livre `current`, sem reseed da IA. Seeds/composições/cadência diferem: **não há comparação pixel a pixel ou conclusão de DPS/timing equivalente**. A regressão controlada antes/depois usa o mesmo teste de lifecycle, não a igualdade das partidas aleatórias.

| Captura | PNGs / eventos |
|---|---|
| Antes `20261021T214712030-ff2d5b2e` | 300 / 15096 |
| Antes `20261021T214752740-fe242891` | 300 / 9099 |
| Antes `20261021T214832715-9c3ffbb2` | 300 / 11199 |
| Após `20261021T220913457-407460a2` | 200 / 9065 |
| Após `20261021T220948807-388b7b33` | 200 / 10964 |
| Após `20261021T221026506-5ca6eeab` | 200 / 10741 |

Total **1500 PNGs e 66164 eventos** principais, sem contar o preparativo interrompido. Imagens realmente abertas: antes run 1 `514/518/524/534`, run 2 `42/48/54`, run 3 `60`; pranchas `before-contacts.png` e `after-trajectories.png`. A prancha após contém três sequências: run 1 Sniper `129/132/138/147`, run 2 Aimed `132/135/141/150`, run 3 Sniper `78/81/87/96`. Cruzamento com emissões 788/2256/3635: imagens mostram -1, +2, +8 e +17 frames, respectivamente. Vistos telegraph, projétil ciano/laranja, progressão e desaparecimento; atribuição de dono/alvo depende também do log. Não foi analisado vídeo nem cada um dos 1500 PNGs individualmente.

**Localização física:** o Unity foi bloqueado por diálogo de D: abaixo de 1 GB durante a importação. A skill de controle de aplicativos confirmou o diálogo; ele foi fechado após liberar espaço. Somente capturas geradas nesta tarefa foram movidas para `C:/Users/gabri/.codex/visualizations/2026/10/21/tech-guy-vs011/raw-runs/<run_id>/`, mantendo junções nos caminhos originais `Library/GameplayObservation/<run_id>/`. Pranchas estão no mesmo diretório de C:, fora de raw-runs. `capture-relocation.json` registra o mapeamento; validadores operaram sobre o destino físico resolvido, sem alterar o comparador ou os manifestos. Arquivos antigos da VS-01 e do projeto não foram removidos. Recomendável reservar mais espaço antes de novas capturas.

### Fechamento e prontidão VS-02

Editor final em NexusLobby limpa, Edit Mode, seleção vazia, timeScale 1, captureDelta 0, qualidade 5, targetFrameRate -1, runInBackground true. Preferências restauradas ao snapshot inicial, saldo 2468; sem dispositivos temporários remanescentes. Console pós-sequência: zero erros; não foi chamado Clear e erros anteriores estão preservados nos JSONs. Avisos herdados de AbilityHolder/fallback não foram corrigidos. `editor-final.json` registra o cleanup.

O defeito confirmado está corrigido no escopo testado e deixa de bloquear o avanço técnico à próxima fatia aprovada da VS-02. Permanecem playtest humano/aceite de game feel, resultado consultável no Nexus, decisão de desfecho e dívida histórica separada. Não declarar estabilidade ilimitada de todas as combinações, modos aditivos ou destruição isolada de componentes com raiz ainda viva; esses casos não motivaram a correção. Integridade final em `vs011/integrity-final.json`; sem commit/stage, restauração de exclusões ou alteração de gameplay fora do lifecycle investigado.


## VS-02A — Run Analytics e atribuição causal, 21/10/2026

**Implementado e validado tecnicamente no escopo abaixo; sem UI final e sem aceite humano de game feel.** Unity MCP `Tech-Guy@fccafb2b47cd7671`, Unity 6000.5.10f1, projeto `D:/Dev/Personal/My_games/Tech-Guy`, branch/base já descritas, com alterações locais preservadas. Evidências sob `Library/GameplayObservation/vs02a/`, ignoradas pelo Git. Contrato principal em [telemetria](03_TELEMETRIA_E_GAME_FEEL.md#run-analytics--contrato-vs-02a-21102026); lifecycle/API em [run](../systems/05_RUN_E_PROGRESSAO.md#lifecycle-do-runsummary--vs-02a-21102026).

### Auditoria e implementação

A base já tinha confirmação final em Actor/CaptureNextDamage, ledger/snapshot da Manopla, raízes/filhos e budgets de Protocols, callbacks de morte/uso, tickets resolvidos e outcomes da sessão. O coletor GameplayObservation era exclusivo do Editor; não havia RunSummary persistido. A extensão transporta proveniência primitiva nesses donos e observa o callback final existente, sem novo event bus, executor, manager ou fonte de dano.

Novos arquivos de produção em `Assets/_Project/Scripts/Core/Run/`: `RunDamageSource.cs`, `RunSummary.cs`, `RunStatistics.cs`, `RunSummaryStore.cs` e `ProgressionDirector.Statistics.cs`. Testes novos: `Tests/PlayMode/RunStatisticsPlayModeTests.cs` e `BasicAttackR1RunStatisticsTests.cs` (partial da fixture existente). Metas gerados pelo Unity.

Integrações somente de instrumentação: `Actor.cs`, `PlayerActor.cs`, `AbilityHolder.cs`, `CharControlScript.cs`, `HitboxDamage.cs`; `ProtocolBudget.cs`, `ProtocolElementStatus.cs`, `ProtocolRuntime.cs`, `.Scope`, `.Status`, `.Damage`; `ProgressionDirector.Manual`, `.Encounter`, `.Lifecycle` e `Stages/ProgressionDirector.cs`. Não foram alterados dano, crítico, custos, cooldown, hitstop, cancelamentos, IA, ranks, autoria de skills ou ofertas. Lista exata e hashes no `integrity-final.json` desta etapa.

### Testes Unity MCP

| Job / artefato sob vs02a/ | Resultado | Cobertura e limite |
|---|---|---|
| `1f82f10e146e4e83b68ac6d3926a32bf` / `analytics-tests-initial.json` | **9/9 PlayMode** | HP efetivo/overkill, multialvo, origem desconhecida/antiga, recibos adiados/reversos/repetidos/reentrantes, snapshots defensivos, proc real Overflow, burn misto, três sessões de fixture/outcomes e save inválido. |
| `662ed954b871483fb66cdd7b5de5b510` / `analytics-before-fixture-adjustment.json` | **15/16 PlayMode** | Q não acertou: fixture colocava alvo antes da chegada do avanço. Corrigida somente a posição com `AdvanceDistance + 1.5`; não mudado alcance/dano de produção. Asserção positiva foi preservada. |
| `4d96b11bef014d549473ae15d110a240` / `regression-playmode.json` | **111/112 PlayMode**, 94,883 s | Sete novos casos de executor real (básico, Q/W/E/R, Eclipse e cancel pré-contato) passaram, junto dos nove do coletor. Filtros: ProtocolRuntimeDamage, ProtocolProjectileContext, ManualPublishedRun (quatro caminhos × três armas), EnemyProjectileLifecycle, BreakerKitEmission, HitFeedbackR7, CancelNoDuplication, DashCancelResolver e RangedFluidity. |
| `0aee33770ebb4616aebc2bbd8b054d61` / `burn-isolated.json` | **1/1 PlayMode**, 2,749 s | `SaturatedBurnTicksRotateWithoutCatchupOrStarvationAndDeathTicksDetonate` falhou no conjunto (192/256 alvos servidos na janela), passou isolado (256). Sensibilidade à execução confirmada; causa aberta, sem afirmar que é falha histórica ou corrigida. Não mudados Ignite, budgets ou asserções. |
| `8d4494811d5748bdafa65f748c98297d` / `analytics-tests-final.json` | **13/13 PlayMode**, 0,776 s | Nove casos do coletor repetidos + projétil real atrasado retendo Q sob contexto W/pausa, nativo Persistent distinto de proc, geração reativada e burn entre sessões sem crédito indevido. |
| `3f0c198e314f4d35964fafcca2800968` / `regression-editmode.json` | **47/47 EditMode**, 2,632 s | BoonCapabilityGraph, BoonOfferCompatibility, BoonRewardSeed, BowBoon e SpearBoon. Compatibilidade, limites de aquisição e determinismo nos filtros escolhidos. |

**20 casos novos de analytics aprovados** (7 executores + 13 coletor). Os 116 casos PlayMode distintos selecionados tiveram aprovação em pelo menos um filtro; **a bateria conjunta não ficou verde**. Não é execução de toda a suíte. As 11 falhas históricas restantes de GI1.3/VS-01 não foram corrigidas, reativadas ou reclassificadas por ausência neste filtro.

Victory/Death/Aborted foram finalizados/persistidos em fixture com sessões independentes; Victory nessa fixture prepara a fase de extração, portanto não demonstra vitória natural. O filtro de rotas publicadas atravessa boss/extração nas três armas com sua assistência já documentada. Os percursos observados abaixo encerram por abandono legítimo, não vitória fabricada.

### Três runs consecutivas na mesma sessão Play

Nexus → entrada real em FirstSector → C1 → duas recompensas reais → C2 → abandono pelo contrato de UI → Nexus; três vezes, sem sair/reentrar em Play, sem reset de pools/inventários manual e sem injetar HP, dano, mana, energia, postura, rank ou boons. Movimento por NavMesh.SetDestination e comandos por APIs de input/AbilityHolder; alvo e ponteiro acompanhados a cada player-loop. **Execução assistida, não partida natural humana.** Seeds/composições são as geradas pela produção, não comparações equivalentes de balanceamento.

| Run / pasta | Sessão / seed | HP efetivo / transações únicas / kills | HP recebido | Duração real / escalada (s) | Desfecho |
|---|---|---|---|---|---|
| 1 / `production-02` | `09144e9a-0ae8-455b-82b5-a84e2402f40b` / `4639334207262704557` | 618 / 46 / 10 | 77.999878 | 80.744 / 65.617 | Aborted → Nexus |
| 2 / `production-03` | `c2dcef3e-06ee-4aa2-975a-25f3d67fdf35` / `5085009338933651454` | 528 / 43 / 9 | 126.299805 | 53.692 / 48.129 | Aborted → Nexus |
| 3 / `production-04` | `c7fa8269-0403-4043-b437-25c1b7fcab40` / `5314744361197171405` | 564 / 43 / 9 | 47.999878 | 58.911 / 53.381 | Aborted → Nexus |

`production-01/` guarda o preparativo: E da estação de Nexus iniciou FirstSector, mas a primeira coroutine de assistência terminou junto com o jogador da cena antiga. A condução foi reanexada ao jogador atual em `production-02`, preservando a mesma sessão Play/run. O watchdog posterior só reanexa o loop ao mudar de cena; não executa combate no loop do Editor. O tempo da run 1 inclui esse intervalo; nenhuma duração é benchmark ou avaliação de dificuldade.

- **Run 1:** ofertas reais Flurry Echo e Ignite. Nativos **575,8**, Echo W **24,199999**, BurnTick **18**, total **618**. Destes ticks, **6** têm habilidade ambígua por básico/E misturados; ficam no total do jogador, fora de uma atribuição exclusiva Q/W/E/R. Outros ticks preservam W ou Basic quando demonstrável. W→Echo mantém execução filha e pai. W e R emitiram múltiplas transações.
- **Run 2:** Asura Echo e Detonation adquiridos. **Zero dano secundário confirmado** nesta execução; o inventário não fabrica contribuição. O total permaneceu 528, sem carregar as parcelas de Ignite/Flurry da run anterior.
- **Run 3:** Flurry Echo e Conductor adquiridos. Conductor Q **14,355001**, Conductor Basic **8,795250**, Echo W **12,099998**; os demais HP são nativos. Total **564**, não cumulativo das runs anteriores.

`integrated-analysis.json`/`analyze.py` conciliam os JSONL independentes com os três resumos no Nexus: **46/43/43 transações únicas**, sem IDs duplicados; soma de partições = HP do jogador confirmado, e agrupamentos por habilidade incluindo filhos reconciliados separadamente, tolerância 0,001 para serialização de float. Dano recebido também conciliado. **156/156 callbacks ActorDamageReceived** nas janelas de GameplayObservation encontraram o respectivo recibo independente por frame/entidade/valor. Eles não são somados aos hooks nem aos resumos como outra parcela.

### Capturas realmente produzidas e inspeção visual

**16 observações completas, 320 PNGs 1280×720 e 42348 eventos**, hashes dos 320 PNGs conferidos pelo manifesto. Identidades/listas exatas no `integrated-analysis.json`. Capturas no diretório original `Library/GameplayObservation/<id>/frames/`, sem remover evidências antigas. O pedido de observação conserva o scenario_id legado ENEMY_TELEGRAPHS em `mode=current`; ele não substitui a rota, não autoriza concluir que foi um cenário configurado de laboratório, nem faz a requested_seed informativa sobrescrever a seed real registrada em conditions/driver.

Imagens efetivamente abertas: nativo `20261021T231336607-6799cad7/frames/frame-000045.png`; sequência de Basic/E/Ignite `20261021T231401922-03944c2b/frames/frame-000075.png` e `frame-000090.png`; `vs02a/production-02/nexus-result.png`. As imagens mostram jogador/ataques, inimigo atingido, mudança de barra e números 20/3; a identificação causal do tick 3 depende do recibo de frame3807 e da telemetria, não da aparência isolada. O PNG do Nexus prova o retorno ao hub, **não uma tela de estatísticas**, que não foi criada. A recuperação de dados é demonstrada por `nexus-summary.json`, `nexus_summary` no driver e `nexus-runtime-checks.json`.

### Limites e próxima etapa

Não há contribuição marginal por boon, estimativa de DPS, fonte individual de HP recebido, persistência de run em andamento ou histórico ilimitado. Mutation substituta foi exercitada em teste real do executor Eclipse, não adquirida nos três percursos desta tarefa. Identificação de alguns efeitos legados pode permanecer desconhecida; UI futura deve rotular a lacuna. Acertos são transações HP por alvo, não precisão de mira. Aceite humano e jornada natural completa/boss continuam pendentes.

VS-02B pode ser planejada sobre `RunSummaryStore.TryRead`, com parcelas não aditivas entre visões, rótulos de origem ambígua e ausência de dados. Implementar UI ainda exige a próxima autorização. Manter separadas a falha conjunta de saturação e as 11 históricas; não esconder o conjunto vermelho. Sem nova mecânica, conteúdo ou balanceamento nesta etapa.


Fechamento VS-02A: comparação dos 2696 arquivos iniciais confirmou apenas 15 integrações C# de instrumentação, seis documentos canônicos e MANIFEST alterados; sete C# novos + seus sete metas, nenhum removido. Cenas, prefabs, ScriptableObjects, configurações e GUIDs preexistentes sem alteração nesta etapa; mesmas 546 exclusões Git. Links locais sem destino ausente e documentos UTF-8 válidos. `integrity-final.json`, `test-aggregation.json`, `editor-final.json` e `console-final.json` preservam os checks. NexusLobby limpa, Edit Mode, timeScale1, captureDelta0, qualidade5, sem mouse virtual residual; moedas/loadout/desbloqueios restaurados aos valores prévios de validação, nenhuma escrita no Store por esta etapa. A releitura final encontrou outro resumo válido, sessão `bfc32dfb-3d35-4330-a503-53054d388278`, Victory,4266HP. O protótipo mantém o snapshot real anterior `c7fa8269`,564HP; não é ligação ao vivo e não foi atualizado para aparentar a última run. A origem da mudança entre leituras não foi investigada nem atribuída a esta etapa. A matriz foi recuperada integralmente após falha de codificação na gravação: hash anterior conferido exatamente em `recovery-check.json` antes de reaplicar esta seção. Nenhum commit/stage/restauração de exclusões ou aceite humano.


## VS-02B.1 — estudo de design e UX dos resultados, 21/10/2026

**PROPOSTA PARA APROVAÇÃO HUMANA; sem UI final.** Fonte principal de conceito, mapa completo de campos, estados, reutilização e decisões em [HUD/UI](../systems/06_HUD_E_UI.md#vs-02b1--proposta-de-resultados-para-aprovação-21102026). Protótipo em [Tools/UX/RunResultsPrototype](../../Tools/UX/RunResultsPrototype/index.html); cinco arquivos locais, nenhum C# ou asset Unity novo. A barra de cenários é ferramenta de revisão, não feature entregue no jogo.

Skills locais verificadas/lidas integralmente com referências: game-vision, boon-design e unity-acceptance; unity-mcp-orchestrator global para inspeção. Não encontrada skill local dedicada a UI/UX/tipografia. Critérios e decisões aplicados estão na fonte principal, sem outro relatório Markdown.

**Unity MCP executado:** recursos instances/custom-tools/project/editor; instância `Tech-Guy@fccafb2b47cd7671`, projeto D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, NexusLobby limpa. Inspector do prefab PlayerHUD confirmou uGUI/TMP/LiberationSans SDF/escala1920×1080. Inspeção estática dos menus confirmou IMGUI; no Nexus carregado não havia EventSystem nessa consulta. `RunSummaryStore.TryRead` exportou snapshot real da sessão `c7fa8269-0403-4043-b437-25c1b7fcab40`; readonly, sem escrever PlayerPrefs. Play Mode foi usado somente para baseline Nexus, sem iniciar run ou alterar fluxo. `nexus-baseline.png` tem1280×739 e foi aberto. Leitura intermediária em Edit Mode: NexusLobby sem dirty, Store ainda com sessão/dano564 e Console sem erros; a releitura final diferente está registrada na integridade abaixo. `unity-inspection.json`, `store-summary.json`, `editor-final.json` em `Library/GameplayObservation/vs02b1/`.

**Navegador:** `verify.cjs` executado com Playwright e Edge headless;58 combinações em1280×720/1920×1080, todos os checks aprovados. Cobriu11 presets, três abas quando há dados; soma de barras conciliada ao total; sem overflow horizontal/da página nem corte horizontal dos elementos avaliados; ação principal visível. Tabs/setas, detalhe modal/Escape/restauração de foco, PageDown em build extensa, fechar/reabrir por Enter, dado ausente versus0 e valor positivo muito pequeno sem virar0. Pares de texto avaliados tiveram contraste mínimo8,06386:1 contra o painel. `ux-checks.json` preserva dados e limites. Não é teste de gameplay, Unity Test Runner, leitura humana ou certificação geral de acessibilidade.

As primeiras rodadas identificaram1px de overflow causado pelo elemento de anúncio fora da tela e uma restauração duplicada de foco modal; corrigidos somente no protótipo, mantendo os asserts. Avisos parciais foram compactados após inspeção720p. O build extensivo continua com scroll interno; não se encolheu tipografia para mostrar todo inventário. Nenhuma correção da dívida de combate/burn ou da sobreposição de textos ambientais do hub foi incluída.

**Artefatos de UI, todos sob vs02b1/**: `overview-720.png`, `overview-1080.png`, `victory-720.png`, `death-720.png`, `aborted-720.png`, `sources-720.png`, `partial-720.png`, `empty-720.png`, `zero-720.png`, `skew-720.png`, `build-extensive-720.png`, `localization-720.png`. São renders do navegador, não capturas da nova UI dentro do Unity. Existem12 PNGs finais do protótipo, além do baseline Unity e do render inicial preservado. Imagens abertas durante a revisão: overview inicial720p, overview final720p, overview1080p, estado sem estatísticas, Victory, Death, fontes, parcial, build extensa e baseline Nexus; as imagens não abertas individualmente não recebem declaração de análise visual.

O estado inicial do protótipo é snapshot real do Store (564HP,9 mortes observadas,47,999878HP recebidos,58,910534s reais); variantes de outcome/ambiguidade/build/texto são **fictícias e marcadas**, com ID de exemplo distinto. Nenhum dado fictício foi inserido no runtime ou Store. Fontes secundárias reais: Conductor originado deQ e básico, Echo W “Mil punhos”; seu subtotal já pertence ao total. Não implementados DPS, porcentagem individual por modificador, nome de agressor, equipamento ativo ou novo histórico de saves.

**Limitações/gates:** transposição e foco no Unity não implementados; escolha IMGUI versus uGUI/TMP, abertura automática/manual, termos, acabamento e nomes históricos aguardam decisão autoral. Schema1 não permite identificar toda ausência de escalar numérico individual. Builds sintéticas não comprovam compatibilidade ou ofertas. A VS-02A e a falha conjunta de burn mantêm os resultados/limites anteriores, sem nova suíte Unity nesta etapa. Aprovação visual e de UX depende do desenvolvedor; nenhum aceite presumido.

**Integridade final VS-02B.1:** comparação SHA-256 com2710 arquivos existentes: nenhuma alteração em Assets, código C# ou configuração Unity;546 exclusões Git preexistentes preservadas. Alterados somente HUD/UI, matriz e hashes do manifesto; adicionados os5 arquivos do protótipo em Tools/UX/RunResultsPrototype. Console final consultado sem limpeza:0 erros. Editor em NexusLobby, EditMode e cena limpa; nenhuma escrita no Store por esta etapa. A releitura final encontrou outro resumo válido, sessão `bfc32dfb-3d35-4330-a503-53054d388278`, Victory,4266HP. O protótipo mantém o snapshot real anterior `c7fa8269`,564HP; não é ligação ao vivo e não foi atualizado para aparentar a última run. A origem da mudança entre leituras não foi investigada nem atribuída a esta etapa.


## VS-02B — revisão, investigação do Store e preparação, 22/10/2026

**Somente revisão/plano; UI definitiva não implementada nem aprovada.** [Direção e plano B2](../systems/06_HUD_E_UI.md#vs-02b--revisão-e-preparação-da-implementação-22102026) e [investigação de persistência](../systems/05_RUN_E_PROGRESSAO.md#investigação-do-último-resumo--vs-02b-22102026) são fontes canônicas, sem relatório paralelo.

- **Unity MCP atual:** instância única Tech-Guy@fccafb2b47cd7671, projeto D:/Dev/Personal/My_games/Tech-Guy,6000.5.10f1; NexusLobby limpa, EditMode sem entrar em Play.0 EventSystems carregados; prefab PlayerHUD com27 TMP_Text, ScaleWithScreenSize1920×1080 e sem GraphicRaycaster. Assemblies InputSystem/uGUI/TMP carregadas; input Both constatado no arquivo. Console consultado sem limpar:0 erros e1 warning de depreciação do Input Manager. Não foi executada nova suíte EditMode/PlayMode nem reinício de build.
- **Store:** bfc/Victory/4266 lido pelo MCP e registro persistido Windows. Duas leituras independentes; mutação de DTO local não afeta o Store. Nenhuma escrita ou restauração do resumo. A origem específica de bfc não pôde ser atribuída; foi demonstrada a possibilidade de fixtures de rotas substituírem a chave sem backup. Nenhuma correção preventiva do Store. `persisted-registry.json`, `history-evidence.json`, `snapshot-probe.json` em `Library/GameplayObservation/vs02b-review/` preservam os limites da investigação.
- **Navegador atual:** mesmos58 checks reexecutados com Playwright/Edge, aprovados em720p/1080p, sem erros JS; pares de contraste avaliados mínimo8,06386:1. `ux-checks.json` e12 renders novos no diretório review; capturas VS-02B.1 preservadas. Protótipo/código/fixtures não foram alterados. As sete imagens selecionadas originais foram abertas individualmente (overview, Victory, Death, Aborted, fontes, build extensa, sem estatísticas); overview1080p também foi aberto. A entrega usa as imagens originais inspecionadas, iguais à proposta em revisão.
- **UX:** pontos fortes de hierarquia/causalidade e problemas de identidade ainda genérica, densidade, scroll pouco evidente, nomes por slot e cancel principal ausente registrados em HUD/UI. “Último resumo real” na barra do estudo significa o snapshot histórico c7fa, não o Store atual; corrigir nomenclatura no próximo ajuste do protótipo/transposição, sem alterar dado para aparentar atualização. Dados fictícios continuam identificados.58 checks não dão aceite estético/humano.
- **Gates B2:** aprovação visual e fluxo; isolamento das fixtures; snapshot por abertura; handoff do ID esperado, EventSystem único, foco, cancel/pausa e bloqueio de comandos de mundo. Preservar retry, dano/boons/progressão e HUD de combate. Reinício/build, dados ausentes e múltiplas runs são aceite futuro, não validação alegada agora.


## VS-02B.2 — implementação dos resultados, 22/10/2026

**Implementado e validado tecnicamente nos filtros/cenários abaixo; direção visual provisoriamente aprovada pelo pedido, sem aceite humano da implementação Unity.** Fonte funcional em [HUD/UI](../systems/06_HUD_E_UI.md#vs-02b2--resultados-no-unity-22102026), handoff/test isolation em [run](../systems/05_RUN_E_PROGRESSAO.md#integração-de-apresentação--vs-02b2-22102026). Sem relatório paralelo. Unity MCP `Tech-Guy@fccafb2b47cd7671`,6000.5.10f1, projeto D:/Dev/Personal/My_games/Tech-Guy; workspace local preservado.

### Testes efetivamente executados

| Job / artefato em Library/GameplayObservation/vs02b2/ | Resultado | Cobertura/limites |
|---|---|---|
| `65080c3995db47c8a8a84cab4ff96be4` |2/2 PlayMode | Primeira atividade: restauração exata de valor existente e chave ausente. |
| `1de5822cc8d44a0ebc299bea034490f2` / results-tests-initial.json |24/24 PlayMode | UI inicial, analytics13 e isolamento2; três retornos de abandono reais. |
| `3c69a70f1b124cf38f826511996cd9d7` / regression-playmode.json |45/45 PlayMode | UI/analytics, seis executores reais de Manopla, ManualPublishedRun (12 combinações de rota/arma na fixture principal), pausa/mapa e três lifecycle cases inclusive load failure/retry. |
| `caed0caa60e64eafbe4315514f318ad6` / guard-regression-playmode.json |45/45 PlayMode | Regressão após recuperação de testes interrompidos, com verificação externa do Store original/restauração e Pending=false. |
| `175ae32bc5f84214b77b21aa48dc9788` / keyboard-open-before-fix.json |27/28 PlayMode | Novo teste de teclado revelou Enter abrir e fechar no mesmo frame; não relaxado. |
| `5172071bdf454861946faea8d65f8ca2` / results-playmode-final.json |28/28 PlayMode |13 de UI,13 analytics,2 isolamento; tecla de abertura não confirma fechamento, snapshotA/escritaB/reaberturaB, mouse, modal/foco, cursor/agente, listas,35 itens e três runs. |
| `76cce9054dda4a4883289e7778179924` / model-editmode-font-final.json |6/6 EditMode | Três outcomes independentes de kills/dano, cópia profunda, desconhecido/zero e bucket0 sem gráfico, ID esperado, valor positivo pequeno e duração. |
| `3a905b29aea14bd19c9443beb8e4d6c4` / burn-isolated.json |1/1 PlayMode | SaturatedBurnTicks passou isolado,2,807754s. Não resolve sensibilidade conjunta histórica nem equivale a suíte global verde. |

As baterias intermediárias repetidas serviram às alterações de densidade, foco e cleanup; não somar repetições como testes novos. Um job `de7c140cf4a844f88630c4100fba0001` não inicializou durante compilação com Outline de namespace incorreto; `test-initialization-failed.json` preserva o timeout. Corrigido usando UnityEngine.UI.Outline explicitamente, import/recompile e nova execução bem-sucedida. Nenhum Console foi limpo. O runner tardio deixou fixture no Store, identificada/restaurada; recuperação test-only foi implementada e demonstrada por `interruption-recovery.json` (Stop de Play sem Dispose restaura original). Não esconder esse incidente nem confundi-lo com defeito do Store.

### Evidência real da UI e dados

Capturas finais **ScreenCapture/Game View**, não navegador: `results-overview-720.png`, `results-sources-720.png`, `results-build-720.png` (1280×720); `results-overview-1080.png`, `results-sources-1080.png`, `results-build-1080.png`, `results-uncertain-1080.png`, `results-source-total-1080.png`, `results-facts-1080.png`, `nexus-last-result-1080.png` (1920×1080). Todas10 foram abertas individualmente. Screenshots iniciais permanecem separados. Fonte/fundo/foco/scroll inspecionados; não houve avaliação humana de estética ou compreensão.

Fonte real é `bfc32dfb-3d35-4330-a503-53054d388278`, preservada byte a byte desde o início; não foram substituídos dados do desenvolvedor para screenshots. Comparação `ui-numeric-comparison.json`: total4266 = Basic2075 + W901,098633 + Q451 + R354,901367 + E255 + incerto229. As20 parcelas de fonte também somam4266; HP recebido341,999512 aparece342, kills44, tempo218,749395 aparece03:38, maior impacto66. DTO consumido e texto renderizado preservados. A Victory histórica não foi reclassificada como partida natural nesta etapa.

### Critérios e limites

- Victory/Death/Aborted: Store e handoff testados em fixture, com chegada real de cena; extração e morte também exercitadas nas rotas/lifecycle existentes. Dados sintéticos dos testes não viraram capturas de gameplay.
- Vazio/inválido/zero, nativo/secundário/incerto, build extensa/scroll e snapshotA enquanto B é salvo: testes positivos, sem inventar DPS ou contribuições marginais.
- Três runs consecutivas: FirstSector real → TryAbortManualRun → Nexus real, mesma sessão Play, ID/outcome/autoabertura/EventSystem único por run. Sem injeção de HP/dano/recursos; abandonos por API, não combate natural humano.
- Mouse/Enter/Tab, Escape detalhe→painel, tecla segurada, exclusão de pausa/estação, foco visível, restore de cursor/movimento e reativação com detalhe: verificados pelos casos de UI; consulta real também terminou com worldGate=false e foco no acesso manual. Controller tem ações do Input Module e back, mas não houve playtest completo de gamepad.
- Persistência: Save/leitura e bytes Windows, recarga de cena e ciclos Stop/Play com runtime novo; consulta manual após reinício Play mostrou mesmo ID/4266 e não autoabriu. **Reinício de processo standalone/build não executado**, por isso não certificá-lo. O disco D começou com~1,15GB; capturas mantidas pequenas, sem build extenso.
- Retry: loader de fixture falha duas vezes, terceira tentativa confirma sucesso; resultado/teardown/ID do handoff preservados. Não simular falha do sistema operacional como se fosse teste real de disco/rede.
- As11 falhas históricas restantes e a sensibilidade conjunta de BurnTick permanecem em trilha separada. Nenhum boon aposentado/Mutation/regra/balanceamento foi alterado para tornar filtros verdes.

**Prontidão:** B2 funcional para avaliação no Unity, com aceite de apresentação/legibilidade e validação standalone pendentes. B3 recomendada: playtest humano do loop completo e polimento orientado pelos problemas observados, sem expandir mecânicas por esta entrega. Editor final fora de Play, Nexus limpa e Game View restaurada; Console final consultado:0 erros e warning preexistente de bindings insuficientes em AbilityHolder. Integridade/diff e hashes das capturas no diretório de evidências.

**Integridade da fonte:** a primeira captura de fases adicionou U+2192 ao fallback dinâmico TMP compartilhado. O hash inicial desse asset era idêntico ao HEAD; foram restaurados somente os bytes alterados nesta etapa e usado separador ASCII no texto novo. Import/recompilação,6/6 EditMode e nova inspeção Play do histórico confirmaram fallback com0 caracteres dinâmicos. `font-restoration.json` registra causa/hashes; captura de fatos foi refeita. Nenhuma mudança de fonte/asset de terceiros é entregue.

## VS-03.1 — Nexus, usabilidade e interação (22/10/2026)

**Implementado e validado nos cenários descritos; sem aceite humano inferido.** Unity6000.5.10f1/MCP Tech-Guy@fccafb2b47cd7671, projeto real `D:/Dev/Personal/My_games/Tech-Guy`, base3a3f0ee com alterações extensas preexistentes. O worktree do chat em C: continha versão anterior, sem os sistemas do pedido; foi preservado. A execução trabalhou na cópia aberta no Editor. Espaço inicial:52,62GB em C:,1,06GB em D:; capturas selecionadas ocupam poucos MB. Nenhuma evidência antiga foi removida.

### Skills e decisões

Leituras integrais das skills pertinentes e referências: locais `tech-guy-game-vision` e `tech-guy-unity-acceptance`; globais `tech-guy-world-identity`, `tech-guy-world-journey`, `tech-guy-level-layout` e `unity-mcp-orchestrator`. AGENTS, visão/pilares, estado, direção visual, run/progressão, HUD/UI e procedimentos de validação orientaram o trabalho. Não foi encontrada uma skill local separada de UI nem um documento canônico independente de Nexus/mundos: o papel do hub consta da visão e de run/progressão. Nenhuma skill foi inventada.

| Decisão | Orientação aplicada |
|---|---|
| Manter hub seguro e espacial; não acrescentar atividades aos portais informativos | game-vision + world-journey: preparação e síntese entre incursões; separar implementado de intenção |
| Corrigir sinalização com assets atuais, sem novo estilo/materiais | world-identity: invariantes de leitura e identidade digital; visão/HUD: legibilidade antes de ornamento |
| Conservar disposição e percorrer caminhos de produção | level-layout: chegada, caminhos, métricas e câmera reais; não reconstruir para obter capturas |
| Corrigir gates usando donos existentes e referências serializadas | AGENTS + Unity orchestrator; sem alterar responsabilidades de loadout, combate ou resultados |
| Separar testes, API, input simulado, capturas e aprovação | unity-acceptance e suas referências; evidências atuais, não transferência do aceite VS-02B.2 |

### Mapa funcional encontrado e verificado

Coordenadas aproximadas Unity, extraídas da cena; não são novo layout.

| Local/componente | Função existente |
|---|---|
| Chegada (-3,0,-8,5), PlayerActor/CharControlScript/NavMeshAgent | Movimento por clique, câmera top-down com follow; agente raio0,5/velocidade5 |
| Pedestais oeste, âncoras (-12,11/-8,50/-4,89;0;-3,56) | Manopla/Arco/Lança; LobbyArsenal resolve o mais próximo em3,5 unidades e usa WeaponLoadout; preços0/15/30 preservados |
| Treino (-11,5;0;4,5), TrainingDummy | Alvo existente para experimentar o kit; HUD de combate visível nas proximidades |
| Incursão, âncora (9,5;0;5,58) | LobbyInteraction, alcance2,2; binding de Skill3 inicia FirstSector |
| Fragmento (-2;0;9,51) e Origem (4,5;0;9,51) | Informação/indisponibilidade; sem destinos jogáveis |
| Arquivo (9;0;-4,02) | Terminal informativo sobre origem/Terra; não é loja, NPC ou progressão nova |
| Lobby Guide | Dono de estações/prompts IMGUI; fica separado do jogador e do arsenal |
| Run Results UI + Nexus EventSystem | Consulta manual uGUI/TMP, autoabertura pelo handoff real e barreira de soltura;1 EventSystem/InputSystemUIInputModule |
| PlayerHUD / PauseMenuUI | HUD uGUI e pausa IMGUI existentes; menus não migrados |

Inspeção Unity:0 missing scripts,86 colliders,0 triggers no Nexus; proximidade é calculada pelos componentes. Caminhos NavMesh completos do spawn até os8 pontos de interação amostrados, incluindo arquivo, portais, pedestais e treino. Não se infere cobertura exaustiva de todo o piso. Cena/prefab/câmera/serviços foram inspecionados; não houve novo bake, mudança de movimento ou de câmera. `scene-validation.json` preserva dados.

### Cinco problemas comprovados e prioridade

Frequência/impacto são julgamento de priorização a partir dos cenários reproduzidos, não estatística de jogadores. Complexidade/risco avaliam a correção localizada.

| Prioridade / tipo | Evidência antes | Frequência; compreensão/ação | Complexidade; risco | Tratamento |
|---|---|---|---|---|
|1 — bug funcional: equipar através dos resultados | Perto da Lança, abrir resultado por clique e pressionar E mudou Gauntlet para Spear com modal ainda aberto; teste vermelho confirmou | Condicional a modal junto ao arsenal; alta/alta | Baixa; baixo | Ligação serializada do arsenal ao guia/modal |
|2 — bug funcional: E do pedestal também lança habilidade | Em Arco/Lança, E manteve Gauntlet e ativou cooldown de Skill3; troca recusada por IsCasting. Teste de Arco falhou antes | Toda tentativa reproduzida com skill pronta; alta/alta | Baixa; baixo | Reserva do binding do arsenal no gate já consultado por AbilityHolder |
|3 — usabilidade: textos ambientais sobrepostos | Captura de chegada e de Manopla; bounds originais dos nomes8,45/5,64/6,64 para espaçamento3,61 | Toda passagem pelo arsenal; alta/média | Baixa; baixo | Nomes curtos/dimensões menores, sem mover pedestais |
|4 — usabilidade: HUD de combate concorre com o hub | Capturas originais mostram vida/mana/Asura/QWER/dash na chegada e seleção | Sempre fora do treino; média/média | Baixa; baixo, com regressão FirstSector | HUD contextual por referência exclusiva do Nexus |
|5 — usabilidade: orientação e nome equipado dependem de descoberta local | Portal fora do enquadramento inicial; não havia indicação persistente da arma fora do card, embora o card já tivesse EQUIPADA | Chegada e afastamento do pedestal; alta/média | Baixa; baixo | Guia compacto com direção, comandos reais e arma equipada |

**Três melhorias implementadas:** (1) ownership de interação, cobrindo os dois bugs; (2) sinalização; (3) apresentação contextual do Nexus, cobrindo HUD/orientação/equipamento. Não são cinco sistemas novos. Fonte funcional em [HUD/UI](../systems/06_HUD_E_UI.md#vs-031--nexus-circulação-e-interação-22102026).

### Jornada A–J e método efetivamente executado

- **A:** chegada/atividades inspecionadas em Game View720p/1080p, confrontadas com componentes; identificação por jogador humano continua pendente.
- **B/C:** cliques virtuais no chão atravessaram o arsenal; E equipou Arco, Lança e Manopla. O teste seleciona inicialmente Manopla por API e libera armas só na fixture, restaurando preferências; as trocas posteriores são input. Na observação livre, ambas as armas já estavam desbloqueadas.
- **D:** modelos reais nas mãos, card EQUIPADA e nome persistente inspecionados; nenhuma alteração de prefab de arma.
- **E:** percurso por mouse virtual até a estação, E de produção carregou FirstSector. Sem Warp, SetDestination de teste ou teleporte.
- **F:** teste de integração usa API TryAbortManualRun, explicitamente separado. A observação adicional da sessão8114c0d2 usou E para entrar, Esc para pausa e eventos IMGUI de mouse em Abandonar/Confirmar; retornou com Aborted e a mesma Lança. Não houve vitória natural nesta execução.
- **G/H:** último resumo real bfc32dfb consultado por clique; novo resumo Aborted autoabriu após o retorno. Escape fechou e cliques voltaram a mover o personagem. Resumos de teste não foram apresentados como runs naturais.
- **I:** E no modal não alterou arma/persistência; fechamento com E mantido bloqueou ações até soltura. Pausa também bloqueou troca.
- **J:** rotas de chegada→pedestais→treino→plaza→incursão e retorno→pedestal percorridas por input simulado. Os8 caminhos calculados por API são evidência adicional, distinta do percurso.

Mouse/teclado: dispositivos virtuais InputSystem/QueueStateEvent; para botões IMGUI, EditorGUIUtility.QueueGameViewInputEvent com foco da Game View. Não houve input físico humano. Roteamento temporário IgnoreFocus/AllDeviceInputAlwaysGoesToGameView foi usado e restaurado; não integra o produto. Snapshots/inspeção/abertura de setup dos testes e cálculo de paths são APIs, não alegação de gameplay por input.

### Testes e incidentes da execução

| Job / arquivo em Library/GameplayObservation/vs031 | Resultado | Escopo |
|---|---|---|
|0de78f298a9547bd8c9b69bdebfc7ade / baseline-reproduction-tests.json |0/2 PlayMode, ambas falhas esperadas ANTES | Reproduções de troca impedida e troca oculta no modal |
|f0eabef867ea4b899d0e40e4d0aab905 |2/2 PlayMode | Mesmas asserções após correção |
|86f6dd541dc843009d478b1113a6cb35 / nexus-1080-tests.json |4/4 PlayMode,1080p | Três armas, hold sem repetição, persistência, treino/pausa, navegação e FirstSector/retorno/resultados |
|421a24b445a241c7b0a81785bbae47ec / regression-720-final.json |35/35 PlayMode,720p |4 novos +13 UI/resultados +13 analytics +2 isolamento +3 pausa/mapa |
|7f3d81151f1d44a38fd5ac80ad4874a4 / lifecycle-pause-regression.json |8/9 PlayMode | Extração/retry, morte, teardown, páginas de pausa/clock e gamepad |
|f070b0f1cf8849f39cfd0206db5e9931 / model-editmode.json |6/6 EditMode | Cópia de DTO, outcomes, ausência/zero, identidade, número pequeno/duração |

**Falha herdada mantida:** PauseConfirmedAbortWaitsForResumeFrameAndReturnsFromUnavailableMutationWithoutVictory falha em PauseInspectionPlayModeTests:178, ao tentar selecionar todas as Mutations incompatíveis, antes de executar pausa/abandono. Mesma falha documentada anteriormente nesta matriz. Não foi corrigida, excluída do resultado8/9 ou enfraquecida. As demais falhas históricas/burn não foram declaradas resolvidas nem a suíte global verde.

Baterias intermediárias preservadas: b804537d teve33/35, com dois erros no novo roteiro (clique fora do campo de visão e comparação pelo nome Gauntlet(Clone)); corrigidos com waypoint intermediário e identificação pela família, mantendo verificações de tela/path/arma. A execução272aae82 teve27/35 por roteamento de input sem foco; fixture agora configura/restaura explicitamente o roteamento. O primeiro job70fc3730 teve0 casos por filtro de classe incorreto; não foi contado como aprovação. Repetições não foram somadas como casos únicos.

### Capturas reais selecionadas

`capture-index.json` registra16 PNGs selecionados, dimensões e SHA-256; todos Game View, sem imagem sintética. Conjunto também copiado para a pasta de artefatos do chat em C:. Arquivos de desenvolvimento/mal nomeados foram preservados e ficaram fora do índice; especialmente after-gauntlet-1080.png tem720p, after-spear-equipped-720.png já mostra resultados, e observed-abort-result-720.png é a tentativa inicial ainda pausada. Não são evidência final desses estados.

| Cenário | Antes | Depois |
|---|---|---|
| Chegada720p, mesmo spawn/câmera |[antes](../../Library/GameplayObservation/vs031/before-entry-720.png) |[depois](../../Library/GameplayObservation/vs031/after-entry-720.png), [1080p](../../Library/GameplayObservation/vs031/after-entry-1080.png) |
| Manopla/pedestais720p, posição(-11,8;0,06;-4,8) |[antes](../../Library/GameplayObservation/vs031/before-gauntlet-screen-720.png) |[depois](../../Library/GameplayObservation/vs031/after-gauntlet-720.png) |
| Arma equipada | Trocas impedidas nos testes anteriores |[Arco720p](../../Library/GameplayObservation/vs031/after-bow-equipped-720.png), [Lança1080p](../../Library/GameplayObservation/vs031/after-spear-1080.png) |
| Incursão | Sem baseline equivalente da estação nas duas resoluções |[720p](../../Library/GameplayObservation/vs031/after-incursion-720.png), [1080p](../../Library/GameplayObservation/vs031/after-incursion-1080.png) |
| Resultado bfc32dfb, mesma origem/dados |[1080p antes](../../Library/GameplayObservation/vs031/before-results-1080.png) |[720p](../../Library/GameplayObservation/vs031/after-results-720.png), [1080p](../../Library/GameplayObservation/vs031/after-results-1080.png) |
| Abandono por input, sessão8114c0d2 | Não aplicável |[720p](../../Library/GameplayObservation/vs031/observed-abort-final-720.png), [1080p](../../Library/GameplayObservation/vs031/observed-abort-final-1080.png) |
| Movimento depois de fechar resultado/retornar | Regressão modal vermelha antes |[720p](../../Library/GameplayObservation/vs031/after-return-navigation-720.png), [1080p](../../Library/GameplayObservation/vs031/after-return-navigation-1080.png) |

Pares comparáveis demonstram separação de rótulos/remoção da competição do HUD; renders de resultados preservam o conteúdo. Não há baseline de todo o hub em1080p nem alegação de comparação pixel a pixel de cada local. Portal e equipamento foram avaliados nas resoluções indicadas; os cards ainda podem cobrir rótulos adjacentes e o personagem em algumas aproximações. Não foi alegado acabamento definitivo.

### Arquivos, integridade e fechamento

Alterados nesta execução: NexusLobby.unity, LobbyInteraction.cs, LobbyArsenal.cs, PlayerHUD.cs e os dois documentos canônicos HUD/UI e esta matriz. Adicionados NexusUsabilityPlayModeTests.cs e seu .meta gerado pelo Unity. A comparação com hashes anteriores distingue essas mudanças das centenas de alterações existentes. Referências cruzadas salvas/recarregadas;0 missing scripts/1 EventSystem. Metas/GUIDs preexistentes, prefab compartilhado de HUD, NavMesh, câmera e cenas externas preservados. Mesmas546 exclusões Git intencionais.

Não houve alteração no código/regra de dano, preços/desbloqueios, recompensas, progressão, RunSummary/Store ou save. A run observada gravou seu resultado pelo fluxo normal; seu JSON foi arquivado antes de restaurar o resumo bfc32dfb original para não substituir o dado do desenvolvedor com validação. Loadout0 e saldo2597 restaurados; o saldo chegou a2598 durante validação, com cleanup registrado, sem atribuir a origem exata a um teste. Fixtures restauraram desbloqueios. `editor-final.json` confere igualdade do resumo/loadout/saldo.

Editor final: NexusLobby limpa, Edit Mode, timeScale1, áudio sem pausa, nenhuma entrada virtual residual; tamanho da Game View e roteamento restaurados, preset temporário removido. Console consultado sem limpeza manual: warning herdado de bindings insuficientes; nenhum erro de compilação/gameplay novo. Uma remoção inicial do preset por índice relativo gerou o erro de ferramenta `Invalid index 0 7 1`; repetida com índice total correto e confirmada a remoção. Esse log foi preservado e não é apresentado como Console sem erros. Erros do compilador Roslyn da ferramenta execute_code foram contornados usando CodeDom; o projeto compilou no Unity.

### Pendências de design e VS-03.2

- Avaliar com jogador novo se o guia orienta sem leitura excessiva; medir busca pela incursão e reconhecimento da arma. Julgamento humano continua pendente.
- Decidir composição/câmera de chegada e sinalização espacial mais permanente: a saída ainda não aparece inteira no enquadramento inicial. Isso exigiria proposta antes de mudar topologia/câmera.
- Revisar densidade/posição dos cards, oclusão dos rótulos por cards/personagem e uma eventual marca física do pedestal equipado; não expandir arte automaticamente.
- Se houver futura regeneração do Nexus por builders antigos, preservar explicitamente essas referências e os resultados; os builders históricos não foram executados nem ampliados nesta fase.
- Fazer partida natural completa/boss, playtest humano de compreensão e reinício standalone. Corrigir incompatibilidade da fixture de Mutations em tarefa própria, preservando asserções corretas.

**Estado da matriz:** critérios de fluxo técnico e gameplay por input simulado demonstrados nos cenários A–J acima; aceitação humana/arte e cobertura natural completa continuam pendentes. Não declarar APROVADO_PARA_ENTREGA nem suíte global totalmente verde.

## VS-03.2 — organização espacial do Nexus (22/10/2026)

**Ajustes locais implementados; validação técnica e observação assistida, sem aceite de jogador novo.** Mesmo projeto/Editor Unity6000.5.10f1 em D:/Dev/Personal/My_games/Tech-Guy; worktree antigo do chat preservado. Continuação da VS-03.1, sem outra auditoria de combate/progressão. Espaço antes das capturas:56,76GB em C: e1,13GB em D: (bytes decimais). Evidências em `Library/GameplayObservation/vs032/`;24 PNGs finais pareados e2 de proposta de câmera, com cópia nos artefatos do chat.

### Skills aplicadas e efeito nas decisões

Caminhos confirmados: `.agents/skills/tech-guy-game-vision/SKILL.md` e `.agents/skills/tech-guy-unity-acceptance/SKILL.md`; globais em `C:/Users/gabri/.codex/skills/tech-guy-world-identity/SKILL.md`, `tech-guy-world-journey/SKILL.md`, `tech-guy-level-layout/SKILL.md` e `unity-mcp-skill/SKILL.md` sob a mesma raiz. Instruções e referências pertinentes consultadas nesta conversa, com reconfirmação do projeto e releitura das fontes afetadas. Skills de combate/boons não foram aplicadas a esta tarefa espacial.

| Skill | Decisão orientada |
|---|---|
| game-vision | Manter hub explorável, três armas existentes, controles e preparação entre runs; nenhum tutorial/atividade nova |
| world-identity | Usar sinalização e material existentes; EQUIPADA comunica estado por texto e posição, sem depender apenas da cor |
| world-journey | Retorno devolve o jogador à mesma preparação; consulta de resultados continua opcional e simples |
| level-layout | Inspecionar spawn, landmarks, câmera e trajetos reais; liberar a faixa do personagem e separar sinalização de cards sem mover estações |
| unity-acceptance | Parear posição/câmera/resolução; distinguir input virtual, API de abandono e leitura humana |
| unity-mcp-orchestrator | Confirmar instância, importar/compilar, salvar/reabrir cena, executar testes e restaurar Editor |

### Jornada e problemas comprovados

Chegada ao sul → arsenal oeste → treino atrás do arsenal → plaza livre → incursão nordeste → FirstSector → retorno ao mesmo spawn → resultados → preparação. Fragmento/Origem e arquivo continuam informativos; nenhuma função de NPC/loja foi inferida.

- O card do Arco cobria o rótulo da Lança e parte da leitura dos pedestais; antes/depois do arsenal mostram sua remoção da faixa central.
- O estado equipado existia no card/guia, mas não no pedestal físico. A placa agora distingue o equipamento ativo sem ler a lista de skills.
- O texto TREINO no chão atravessava a posição de aproximação do avatar; o rótulo foi colocado ao lado do alvo e permanece legível em720p/1080p.
- A incursão continua fora da vista inicial. A indicação direcional usa a projeção do único destino jogável e ajuda a orientar sem deslocar câmera/estação.
- A região oeste concentra pedestais/treino e seus textos; o centro funciona como circulação livre. Não foi demonstrado bloqueio que justificasse preencher o centro, rebake ou reorganização das estações.

O percurso por clique foi completado antes e depois; não se afirma ganho de velocidade, distância ou compreensão humana. Caminhos completos do spawn aos8 anchors foram reconferidos. Comprimentos calculados por NavMesh: Manopla10,36; Arco7,39; Lança5,28; treino14,33; incursão18,83 unidades. São caminhos de API, distintos dos deslocamentos por input. O roteiro com troca das três armas usa12 ordens de movimento; sua distância amostrada não foi usada como métrica precisa, pois omite trechos entre amostras.

### Implementação e comparação funcional

| Ajuste | Depois / limites |
|---|---|
| Cards em margem | Personagem, guia, botão de resultados e nomes dos três pedestais livres nos cenários capturados. Tamanhos/dados de skills mantidos; não é certificação de todos os aspectos de tela |
| Placa EQUIPADA |3 placas TextMesh reaproveitam fonte/material existentes; somente a arma real fica marcada, sem polling de save para inventar estado. Placa não invade a faixa inferior do HUD |
| Treino e guia | Texto ao lado do alvo; informação de incursão integrada ao painel existente, sem um segundo painel cobrindo ARSENAL; bindings continuam configuráveis |
| Fluxo funcional | Mesma circulação, preços, desbloqueios, seleção persistente, HUD contextual, gates e resultados.1 EventSystem e0 missing scripts |

Arquivos de produto alterados nesta etapa: `WeaponCardView.cs`, `LobbyArsenal.cs`, `LobbyInteraction.cs`, `NexusLobby.unity`. Adicionados dois arquivos de teste e respectivos metas: `NexusSpatialJourneyPlayModeTests.cs`, `NexusCardLayoutTests.cs`. HUD/UI e esta matriz receberam a documentação. PlayerHUD, backend de resultados, armas, economia, dano, progressão, NavMesh e cenas externas não foram alterados.

### Antes/depois real, nas mesmas condições

`captures-final.json`:24 PNGs com dimensões e SHA-256. `comparison.json`: posição do jogador, posição da câmera, arma e resolução conferidas por par. Mesmo cenário/câmera40°; animações decorativas/idle não foram sincronizadas para diferença pixel a pixel. Resultados pertencem a sessões reais de abandono distintas, com IDs/durações diferentes; não são dados sintéticos nem comparação de desempenho entre runs.

| Estado |720p antes → depois |1080p antes → depois |
|---|---|---|
| Chegada |[antes](../../Library/GameplayObservation/vs032/before-final-720/arrival.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/arrival.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/arrival.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/arrival.png) |
| Arsenal/arma equipada |[antes](../../Library/GameplayObservation/vs032/before-final-720/arsenal.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/arsenal.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/arsenal.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/arsenal.png) |
| Treino/HUD |[antes](../../Library/GameplayObservation/vs032/before-final-720/training.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/training.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/training.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/training.png) |
| Incursão |[antes](../../Library/GameplayObservation/vs032/before-final-720/incursion.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/incursion.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/incursion.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/incursion.png) |
| Resultado |[antes](../../Library/GameplayObservation/vs032/before-final-720/results.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/results.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/results.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/results.png) |
| Preparação retomada |[antes](../../Library/GameplayObservation/vs032/before-final-720/resumed.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-720/resumed.png) |[antes](../../Library/GameplayObservation/vs032/before-final-1080/resumed.png) → [depois](../../Library/GameplayObservation/vs032/after-delivery-1080/resumed.png) |

As imagens de desenvolvimento em before/before-v2/after/after-final foram preservadas e não integram essa seleção. Na primeira revisão, um painel adicional cobriu o título ARSENAL; foi retirado. A placa vista sob o HUD durante treino também recebeu limite de projeção. Os renders finais são after-delivery.

### Testes executados e limites

| Job | Resultado | Evidência |
|---|---|---|
|39f5110607ec4771abfa4a441616c695 |2/2 PlayMode antes |`journey-baseline-final.json`, duas resoluções, seis estados por roteiro |
|9c42473436be4f6e82ed70e649426f8c |37/37 PlayMode final |`regression-final.json`:2 roteiros espaciais +35 regressões VS-03.1/resultados/analytics/isolamento/pausa |
|72e33bba50764e3dbc349b1399af2f3c |10/10 EditMode |4 casos de posicionamento do card +6 casos do modelo de resultados; `editmode-final.json` |

Roteiro assistido: spawn, caminhar por mouse virtual, equipar Arco/Lança/Manopla por E, alcançar treino com HUD, caminhar até incursão, entrar por E, pausar por Esc, retornar, conferir resultado Aborted, fechar por Esc, reabrir pelo botão com mouse, fechar, caminhar e equipar Lança novamente. Assertions verificam card fora do avatar/guia e uma única placa ativa no pedestal correto. Não há Warp, SetDestination de teste ou alteração de estação.

**Assistência explícita:** desbloqueios são liberados somente na fixture e restaurados; abandono usa `PauseMenuUI.TryRequestRunAbort` por API depois de pausar por input. Duas tentativas iniciais de clicar o IMGUI pelo runner não entregaram os eventos ao botão; jobs0a526cdf/25ed70a3 falharam e foram preservados. Isso não foi atribuído a bug do produto, nem contado como abandono por mouse aprovado. O roteiro foi delimitado como integração com assistência API nessa etapa. Nenhum teste herdado foi enfraquecido. O abandono manual simulado da VS-03.1 continua evidência histórica separada.

**Primeira visita por tarefas:** roteiro completou todas as atividades sem bloqueio de navegação ou interação, antes e depois. Ele já conhece posições e bindings; não mede descoberta, hesitação, agrado ou aprendizagem de um jogador novo. Avaliação humana permanece necessária. Falhas herdadas de Mutations/burn da matriz anterior não foram alteradas nem declaradas resolvidas; não se executou suíte global.

### Propostas maiores não implementadas e VS-03.3

Comparação real de câmera, somente prévia temporária: [40° atual](../../Library/GameplayObservation/vs032/proposal-current-fov40-720.png) e [52° propostos](../../Library/GameplayObservation/vs032/proposal-only-fov52-720.png). Mesma posição/rotação/spawn720p.52° revela mais praça, arsenal e alvo, mas diminui o avatar e ainda corta o portal no alto. A mudança foi revertida para40° antes de sair de Play; não há alteração serializada de câmera. Não recomendo adotar52° isoladamente como solução concluída.

Antes de mudar composição maior: comparar uma chegada com câmera mirando mais à frente, preservando leitura do avatar e evitando que o guia cubra o arsenal. Mover incursão/estações ou redesenhar a praça teria custo de NavMesh/circulação e depende de proposta aprovada; não foi implementado. Não há proposta de preencher áreas livres com atividades novas.

**Prontidão VS-03.3:** base técnica pronta para playtest humano e decisão de composição. Pedir tarefas de primeira visita sem apontar os locais, observar busca por arsenal/treino/incursão e distinguir identificação da arma de leitura do card. Decidir câmera e futura direção artística a partir dessa observação; não inferir aprovação do teste automatizado.

### Integridade e encerramento

Hashes anteriores preservam as mudanças VS-03.1 e exclusões Git existentes; mesmas546 exclusões. Metas/GUIDs preexistentes mantidos, referências novas salvas/reabertas, nenhuma cena externa ou NavMesh alterado. `preferences-final.json` confirma igualdade de resumo, seleção, moedas e desbloqueios antes/depois das regressões; o resumo de cada abandono de teste é restaurado pelo escopo existente. Não houve nova política de persistência nem gravação fictícia para screenshots.

Editor encerrado em NexusLobby limpa, fora de Play, câmera40°, Game View e roteamento restaurados, sem dispositivos/presets temporários. Console final consultado sem limpeza manual: sem erros novos de compilação/runtime; warning herdado de bindings insuficientes. O erro de ferramenta de remoção de preset que estava no Console inicial pertence à VS-03.1 e não foi tratado como regressão de gameplay.

**Conferência de fechamento em24/10/2026:** nova sessão MCP reconfirmou o mesmo projeto/Editor, NexusLobby limpa em Edit Mode, câmera40°,0 missing scripts,1 EventSystem,86 colliders e três referências de placa válidas. Ownership e câmera do guia continuam vinculados. Resumo, seleção e moedas ainda coincidem com o backup da validação; nenhuma restauração foi necessária nesta retomada. Console atual:0 erros e1 aviso de depreciação do Input Manager. Os testes e capturas acima são da execução de22/10; os hashes da retomada confirmaram somente as alterações previstas, sem mudança adicional nos sistemas validados. `editor-final.json`, `console-final.json` e `integrity-final.json` registram o fechamento atual.
## VS-03.3 — preparação de playtest humano (24/10/2026)

[Roteiro curto e ficha de observação](04_CHECKLIST_PLAYTEST.md#vs-033--sessão-humana-do-nexus) disponíveis:8 tarefas, com campos de hesitação, ajuda, ação inesperada, legibilidade e decisão. No fechamento técnico inicial ainda não havia relato humano; **o feedback autoral recebido posteriormente está registrado abaixo e muda a recomendação de encerramento.** Não houve bug novo do Nexus reproduzido nem correção de produto. Visão/jornada orientaram a preparação entre runs; level-layout, tarefas de descoberta/circulação; unity-acceptance, a separação entre observação humana e prova técnica. Não houve mudança de câmera, layout, cards, placas, HUD, input ownership ou resultados.

Regressões atuais via MCP no projeto D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1:

- 35/35 PlayMode — Nexus/arsenal, tecla mantida, pausa, movimento por clique, armas/loadout, retorno por API e resultados/foco/soltura, analytics e isolamento; job8c9356b28e174593b8283c2f2a6f1aef.
- 8/8 PlayMode — lifecycle de extração/retry/morte e inspeção/clock de pausa; job317433b7afbb4aca807d87af777d315c.
- 10/10 EditMode — layout do card e modelo de resultados; job1a3f8aecf98b47b3bb337ebb9bab200a.

São testes com dispositivos virtuais e fixtures/API; os testes de lifecycle incluem Warp/atores assistidos e não comprovam navegação natural. A navegação por mouse é coberta separadamente pelos testes do Nexus. Nenhuma captura nova foi necessária: UI/cena permaneceram iguais. Mutations incompatíveis e BurnTick permanecem na trilha histórica, sem reexecução nem declaração de resolução/suíte global verde.

Evidências brutas em `Library/GameplayObservation/vs033/`. Avisos de referências ausentes ocorreram em atores mínimos de fixtures; a cena real tem câmera/dash/clickEffect vinculados.0 erros novos de compilação/runtime. A bateria deixou saldo2598 em vez de2597; restauração exata registrada em currency-cleanup.json, sem mudar regras ou atribuir uma causa específica sem isolar o teste. Resumo, seleção, saldo e desbloqueios foram conferidos iguais ao backup no fechamento.

Integridade: somente roteiro e esta nota foram alterados; código/assets/GUIDs/metas e546 exclusões Git preservados. Editor final NexusLobby limpa, Edit Mode,40°,1 EventSystem,0 missing scripts,3 placas referenciadas; roteamento restaurado e sem dispositivo virtual residual.

**Recomendação inicial, superada pelo feedback abaixo:** encerrar novas alterações técnicas do Nexus e realizar o roteiro humano para fechar o aceite de compreensão. Abrir correção pontual somente se a sessão demonstrar bloqueio/bug ou pequeno problema recuperável; câmera/layout maior continuam decisão autoral, sem adoção de52°.

### Feedback autoral recebido — revisão da direção (24/10/2026)

Origem: resposta direta do desenvolvedor ao pedido de observações humanas. Relata ambiente concentrado/poluído, cards de armas grandes e próximos, falta de salas e funções legíveis, modelos de armas gigantes/provisórios e desconexão com a motivação de voltar à Terra. Não equivale a um teste independente de jogador novo nem fornece um percurso/tempos. É evidência autoral suficiente para manter o aceite de experiência aberto;43/43 e10/10 continuam resultados técnicos, não aprovação do lobby.

Implementação confirma que o card atual entra por proximidade, com raio3,5 e pedestais separados por cerca de3,61 unidades: regiões de foco se sobrepõem. A crítica não foi reduzida a esse ajuste pequeno, porque também exige composição e função narrativa. Nenhum bloqueio mecânico novo foi alegado ou inventado.

[Proposta concisa de experiência](../art/03_NEXUS_PROPOSTA_DE_EXPERIENCIA.md): Reconexão → Oficina/Arsenal → Treino e Travessia, com rota rápida opcional, cards resumidos e proporção de armas em estudo. Comparação em `Library/GameplayObservation/vs033-direction/nexus-direction.png`; lado atual é captura Unity histórica identificada, lado proposto é planta esquemática, não render/implementação. Inclui reutilização dos serviços atuais e limites para evitar prometer progressão narrativa inexistente.

**Nova recomendação:** revisar/aprovar a direção e depois construir blockout isolado comparável; não encerrar a experiência atual nem continuar polindo a praça como substituto dessa decisão. Sem mudanças em NexusLobby, câmera, assets, bindings, saves ou sistemas nesta proposta. Testes anteriores preservados; nenhuma nova execução de gameplay foi necessária para os documentos/visual. Alterações maiores permanecem condicionadas à proposta e aprovação solicitadas pelo desenvolvedor na VS-03.3.

## VS-03.4 — blockout isolado jogável (24/10/2026)

Direção aprovada provisoriamente pelo pedido desta etapa. [Entrega curta e como jogar](../art/04_NEXUS_BLOCKOUT_FUNCIONAL.md). NexusBlockout tem quatro áreas conectadas, rota direta Reconexão → Travessia e desvio Oficina → Treino; reutiliza player, arsenais, alvo, HUD, resultados e FirstSector. Câmera40°. Não substitui NexusLobby; não encerra VS-03 nem estabelece aceite humano.

| Execução MCP | Resultado | Evidência |
|---|---|---|
| PlayMode: NexusBlockout + NexusUsability + RunResults + RunStatistics + RunSummaryIsolation + PauseMapVisibility + TrainingDummyPassive | 40/40 | Job `1c529c299e8345c0830852fb19a39295`; `vs034/playmode-main.json` |
| Lifecycle/progressão/pausa: os8 casos já delimitados da VS-03.3 | 8/8 | Job `0c6b6817bc7046c2a01e2d7a7c58651b`; `playmode-lifecycle.json` |
| EditMode: NexusCardLayout + RunResultsModel | 10/10 | Job `4ccb7998c17b4845b51471931c91e1c5`; `editmode.json` |
| Repetição final dos dois percursos após orientar a silhueta do Arco, sem colliders | 2/2 | Job `3bf0c419caf04ff7963d67ede44165d3`; `playmode-final-journeys.json`; capturas `final-720` / `final-1080` |

As48 verificações PlayMode distintas incluem os dois percursos, repetidos no fechamento. Mouse/teclado **virtuais**, com picking/collision/NavMesh de produção; sem Warp/SetDestination na jornada. Chegada → saída direta → retorno pela abertura → três armas/E sem conjuração → detalhes pelo binding real de Skill4 (F11 neste perfil), reserva do ponteiro → bloqueios de modal/pausa → Treino/Q da Lança → Travessia/E → FirstSector → abandono → Reconexão/resultados com ID correspondente → mouse para reabrir/Escape para fechar → nova escolha de arma. No perfil usado, as armas já estavam liberadas: a fixture não concedeu moedas nem desbloqueios para preparar o blockout.

Assistências explícitas: modal aberto por API na regressão de ownership; confirmação de abandono por `PauseMenuUI.TryRequestRunAbort`; destino de retorno ajustado na instância pelo teste. Lifecycle adicional usa suas fixtures/APIs históricas, não jornada natural. A sessão de revisão do Editor foi verificada separadamente: Play em NexusBlockout ativou backup, entrada de FirstSector via API acionou o hook real (sem injeção da fixture), abandono via API retornou à variante e abriu o ID correto; Stop restaurou o snapshot. `editor-review-return.json` registra a prova. Não houve vitória natural, sessão humana ou validação standalone.

Falhas de desenvolvimento preservadas: primeiro roteiro tentou clicar fora da tela no retorno; roteiro passou a clicar progressivamente no chão visível. Depois foram reproduzidas intercepções por rack e pedestal, corrigidas somente na variante. Clique que cruzava a silhueta do avatar foi ignorado pelo contrato de picking existente; o roteiro passou a usar chão lateral, sem enfraquecer esse contrato. Jobs iniciais `23d08e3da5654ee5942f9fcbfb30dafb`, `573902ab8d7e42e282907d4d5f6d6973`, `d0aac07139a646539bbebc2908fe7f65`, `365e96a86f484317abca964dddabc0af` não são aprovações. Tentativas de ferramenta/builder (fonte legada e nomes com barra) foram corrigidas antes de salvar a variante final. As falhas históricas de Mutations e BurnTick permanecem separadas, fora deste lote e sem ajustes de assertions/catálogo.

Inspeção MCP após salvar/reabrir:1 EventSystem,0 missing scripts,55 colliders, NavMesh independente e caminho direto completo de14,88 unidades; agente raio0,5. Escalas reais da exposição (bounds): Manopla0,406×0,227×0,55; Arco0,342×1,30×0,082; Lança0,310×1,85×0,066. Arma na mão/hitbox inalterados. Console sem erros na consulta final; warning herdado de array curto de bindings continuou usando fallback. Portal cortado no topo perto da interação e escala da Lança na mão ficam como julgamento visual, não bugs resolvidos.

Evidências em `Library/GameplayObservation/vs034/`: estados JSON por captura, integridade, comparação e vistas gerais reais do Unity. Vistas gerais usam câmera temporária igual nas duas cenas, identificada nos artefatos; não são proposta conceitual nem câmera de gameplay. Composições não alteram os PNGs originais. Espaço D: conferido antes das capturas (cerca de1GB livre).

Proteção: hashes dos assets preexistentes comparados ao início; apenas4 scripts compartilhados receberam a opção experimental/ponteiro, com comportamento antigo como padrão. NexusLobby, FirstSector, prefabs, NavMesh original, GUIDs/metas e546 exclusões Git preservados; build settings apenas acrescenta a variante. O lote histórico de lifecycle deixou +1 moeda, já observado na VS-03.3: delta2597→2598 registrado e restaurado exatamente do backup, sem mudar economia. Resumo/seleção/desbloqueios mantiveram igualdade; fechamento confere novamente os dados e o roteamento de input. Nenhum resumo fictício foi criado para screenshots.

**Próximo passo:** desenvolvedor jogar a variante e avaliar conexão entre ambientes, descoberta da saída, escala e legibilidade. Só depois decidir substituição ou ajuste pontual; arte e disposição ainda provisórias.

### VS-03.4 — revisão da planta após rejeição autoral (24/10/2026)

O blockout anterior foi rejeitado por manter um conjunto retangular subdividido. A revisão reconstruiu somente a geometria de NexusBlockout: volumes deslocados SE/oeste/NW/NE, pisos recortados, espaços vazios reais, bancada de equipamentos, Treino aberto e terminal de Travessia. A planta original de vs033-direction foi inspecionada e comparada diretamente com uma vista vertical real, sem UI/rótulos. Proporções e ligações adaptadas ao espaço de combate; não apenas nomes de salas. Entrega atual em Docs/art/04_NEXUS_BLOCKOUT_FUNCIONAL.md.

- PlayMode27/27: NexusBlockout2, NexusUsability4, RunResults13, RunSummaryIsolation2, PauseMapVisibility3 e TrainingDummyPassive3. Job cd270fa9828e494fa76e67c885a2ef4c. O runner reportou sucesso e27/27; a consulta arquivada após recompilar preserva status/progresso, mas não o detalhamento de cada caso.
- EditMode10/10: NexusCardLayout/RunResultsModel, job3705211b457246dcaded45ca711fb1a3. Repetição final das jornadas720p/1080p2/2 após corrigir apenas o texto de orientação da variante, job02a95e541eff42bba153a5de4f04f7ec; relatório completo em final-journeys.json.
- Input virtual de mouse/teclado; picking, colisão e NavMesh reais. Percurso direto, ida/retorno entre volumes, três armas/E, foco único, card resumido/detalhes intencionais, ponteiro bloqueado, pausa, dano efetivo da Q da Lança no TrainingDummy, FirstSector real, retorno à variante, ID de resultado correspondente, reabertura por mouse e nova preparação. Sem Warp/SetDestination na jornada. Modal de ownership aberto por API, confirmação de abandono por PauseMenuUI.TryRequestRunAbort e destino de retorno controlado por reflexão na instância: não são input humano nem vitória natural.
- Contrato arquitetural verificado: dois vazios entre volumes sem NavMesh nem collider de chão oculto; seis estações com PathComplete. Rota direta à incursão23,47 unidades, sem visitar Arsenal/Treino; não foi alegado ganho de velocidade sobre a planta rejeitada. Câmera40° preservada, sem experimento de lente.
- Salvar/reabrir via MCP:1 EventSystem,0 missing scripts,35 colliders, referências mantidas e mesmos GUIDs da variante/NavMesh. Console final sem erros; warnings herdados de bindings continuam separados. Nenhuma falha histórica de Mutations/BurnTick foi alterada ou absorvida pelo resultado deste lote.
- Snapshot de seleção, moeda, desbloqueios e resumo: todos iguais ao início, inclusive presença/ausência e bytes do resumo. Sem grants para preparar a cena; perfil já liberado usado. Routing de input restaurado, dispositivos virtuais removidos, sandbox fechado e Editor final em NexusBlockout limpa/Edit Mode. Espaço em disco conferido antes das capturas (~1GB livre emD:).

Evidência em Library/GameplayObservation/vs034-revision: comparação com referência original, footprint-only/topdown-no-labels, four-areas, connections, estados por captura720p/1080p, resultados de teste e final-integrity. Vistas gerais são câmeras temporárias de inspeção, não alterações da câmera de gameplay. Assets preexistentes alterados nesta revisão: cena/NavMesh isolados, fixture de percurso e uma linha de guia opt-in em LobbyInteraction. Mesh de pisos, dois materiais neutros e ferramenta de reconstrução são novos. Produção NexusLobby/FirstSector/prefabs/metas permanecem iguais ao início;546 exclusões Git preservadas.

Aceite humano permanece aberto. Avaliar bifurcação inicial e segurança percebida das bordas abertas; estrutura superior do portal pode cortar perto da interação, e escala da Lança na mão continua questão autoral. Não substituir NexusLobby nem declarar acabamento artístico concluído.

## Gameplay Intelligence 1.4 — básico da Manopla no contato (24–25/10/2026)

**Causa:** CharControlScript resolvia a área na aceitação do comando e não consumia o ImpactEvent do BasicProfile. A janela busy/cadência real é 0,20 s, mas os clips de soco de aproximadamente 1 s rodavam em velocidade 1. **Decisão explícita do desenvolvedor:** preservar cadência e encaixar a apresentação na janela existente por SkillAnimationPlayer, mecanismo já usado em Q/W/E. Não houve edição de clips, assets, dano, alcance ou velocidade de ataque.

Owner continua CharControlScript/BasicAttackDriver. O partial BasicImpact usa ActionImpactScheduler e ledger existentes, contexto/energy policy da Manopla, área/recibos de PlayerActor, Protocols e feedback existentes. O comando aceita imediatamente; o evento a 40% emite HP na área atual. Alvo não é travado nem perseguido pelo soco. Cancelamento anterior elimina contato pendente; cancelamento reentrante não revoga a área já comprometida. Movimento, skill/dash aceitos, troca, disable e teardown conservam seus pontos existentes de cancelamento; não se adicionaram regras novas de cancel ou buffer.

### Evidência já executada

- Regressão antes de produção: `GIBasicAcceptedAttackWaitsForAuthoredContact`, job `0b107a3cd3c04b51a1d19f0d5e12deca`, **0/1**: esperado HP1000 no frame de aceitação, encontrado980. Primeiro verde após correção: job `2e1212d70f3f42acbfe3b61baccd010a`, **1/1**. JSONs preservados em `Library/GameplayObservation/gi14/regression-before.json` e `regression-first-after.json`.
- Lote focado posterior, job `ba532fcbb7cb42728198e3b970b42476`, **13/13 PlayMode**: aceitação/contato; movimento pré-impacto; área multi-alvo com cancel reentrante; alvo saindo da área; troca de alvo; alvo destruído/desabilitado; morte do alvo; troca de arma/disable do dono; pausa; cinco socos/recuperação/hitstop; avanço grosseiro+sinal duplicado; crítico/Protocol/energia numa raiz; Lança com emissão imediata. Resultado detalhado preservado em `gi14/regression-after.json`. Fixtures usam atores/HP sintéticos e perfil/controller/avatar/clips reais; não são gameplay humana. Warnings de configuração mínima e Animator da fixture ficam registrados.
- A tentativa `9a7e34d6eaae437aa5dcc23609f1f48c` não iniciou testes por compilação do coletor; não conta como execução. Acessores somente-leitura foram expostos ao assembly Editor, recompilação permitiu o lote acima. Não se apagou Console.
- Capturas de produção concluídas: `20261024T195806687-f7313a34` antes e `20261024T201227563-200cac75` depois, ambas FirstSector/nó1, seed1701, cinco inimigos, 1280×720, 60 fps de simulação, 180 PNGs, comandos básicos nos frames30/75/120. Comparação de todos os campos `conditions` foi idêntica, inclusive fingerprint inicial `68e78044…`, câmera e render. Sem injeção de HP/dano/recursos; comandos são APIs públicas simuladas, não mouse/teclado físico ou aprovação humana. NavMesh/física e consumo de RNG não são garantidos bit a bit.

| Momento | Antes | Depois |
|---|---|---|
| Aceitação dos três comandos | frames30/75/120 | frames30/75/120; apresentação inicia no mesmo frame |
| HP confirmado | frames30/75/120; 30/20/10 HP | frames35/81/126; 20/20/20 HP |
| Primeiro contato | síncrono, antes do movimento de soco | t0,58333, cerca de83 ms após t0,5; progresso mecânico0,4 e pose PunchRight0,3333 |
| Segundo/terceiro contato | síncronos | cerca de87/93 ms escalados após aceitação, quantizados pelo FixedUpdate |
| Recuperação do primeiro soco | busy existente | frame44/t0,70000 encerra graph/ação; sem extensão da janela de0,20 s |
| Totais factuais | 3 recibos,60 HP,9 Asura | 3 recibos,60 HP,9 Asura; mana1500 preservada |

Crítico aparece no primeiro golpe antes, não depois: o atraso altera a ordem do consumo de RNG, embora as condições iniciais sejam iguais. Não se interpretou igualdade do total como determinismo de críticos nem como teste de balanceamento. `HpContactTiming` registra os três recibos/execuções e pose na confirmação; não é outro total. HookHit/BasicHit/ProtocolImpact são callbacks sobrepostos. Cinco socos da fixture confirmaram cinco transações, cinco hitstops e15 Asura; área de dois alvos manteve somente3 Asura na raiz e cleanup sem raízes retidas.

Inspeção visual dos PNGs: antes frame30 já mostra HP/Asura com corpo ainda sem contato; depois frames30/33 mostram aceitação/antecipação,35 contato+HP,39 follow-through e46 retorno à postura. O hitstop preserva contato nos frames35–36. Isso sustenta sincronização e recuperação observáveis; não certifica peso agradável ou alinhamento geométrico do punho em todo inimigo. O trecho de cinco inimigos não teve HP perdido pelo jogador: não valida resposta sob pressão prolongada nem swarm. A cadência rápida foi exercitada em fixture, não nesse trecho espaçado.

### Fechamento complementar — 25/10

MCP reconectado e projeto/instância confirmados novamente: `D:/Dev/Personal/My_games/Tech-Guy`, `Tech-Guy@fccafb2b47cd7671`, Unity6000.5.10f1. O cliente de ferramentas reteve falha de handshake; a continuação usou o mesmo servidor MCP local por HTTP, com descoberta real de recursos/tools e execução no Editor aberto, sem batchmode concorrente.

- **16/16 PlayMode focados**, job `b9c29e333ce1481d81b78777685d7849`, `final-focused-green.json`: os13 anteriores, skill aceita antes do contato, morte do dono e disparo normal do Arco. A regressão de contato agora também exige graph ativo e pose autorada no callback de HP. O primeiro lote ampliado foi15/16, job `7a9a8db4768d48dc9cc64b0bb487eda5`, `final-focused.json`: a fixture consultava GetComponent após a morte real destruir o jogador. Corrigiu-se somente a inspeção para reter a policy gerenciada e exigir zero ganhos/raízes, sem acesso ao wrapper destruído. Um lote13/13 antes do refresh, job `de58de6bde714e47920385619eca90f8`, ainda usava a assembly anterior; não foi contado como cobertura dos casos novos.
- **Regressões PlayMode:59/60**, job `8d6dc3321b9f4da0987a2d8b6a98f659`, `regressions-initial.json`. Filtro exato em `regression-filter.json`: ActionImpactScheduler5, BreakerKitEmission10, CancelNoDuplication5, DashCancelResolver8, HitFeedbackR7 8, RangedFluidity8, CursorFluidity8 e oito casos de tap/hold/movimento/skill/dash/pausa/hitstop de BasicAttackR1. Q/W/E nativas e pose no render, R nativo, dedup de impactos, projétil já emitido após cancel e recursos/iframes permaneceram aprovados.
- A única falha foi `CursorFluidity_PrimaryClickWithoutShiftHitsAtZeroManaWithoutResourceCost`: a assertion antiga exigia HP no momento em que contava a aceitação. Atualizada para aguardar HP confirmado, com deadline, preservando clique sem Shift, uma aceitação, mana zero e parada ao soltar. **8/8 CursorFluidity** passaram após a alteração, job `90fc781928cc4229a5aee0b9b0e56388`, `cursor-final.json`. São **76 casos PlayMode distintos aprovados** nos filtros desta revisão (16 focados +60 de regressão, com a correção/repetição descrita), não uma alegação de suíte completa ou de um único lote76/76.
- **22/22 EditMode**, job `ff9a7a9eb7b142feb1fe37beb556cc9a`, `editmode-final.json`: ObservationTelemetryTests e RangedFluidityContractTests. Mouse/teclado foram dispositivos virtuais do Input System nas jornadas de cursor/rebind/hold; os testes focados usam APIs públicas e manipulação explícita de fixture. Não houve input físico humano. Arco confirmou emissão imediata de um projétil e um recibo após viagem; Lança confirmou HP imediato e alcance autorado. Carga/kit completo dessas armas não foi revalidado integralmente.

**Proteção final:** o resumo Aborted de sessão `86987e44…` foi identificado no JSONL da captura depois e substituído pelos bytes exatos do resumo original, sem criar outro resultado. Consulta final MCP confirmou igualdade de presença/valor nas cinco chaves do snapshot: seleção, moeda, dois desbloqueios e resumo. `InspectPreferences-final.json` e `summary-restored.json` preservam a verificação. A limitação do capturador, que não restaura RunSummaryStore sozinho, foi tratada no fechamento desta execução; não se ampliou a ferramenta de captura nesta tarefa.

Editor final em NexusBlockout limpa/Edit Mode, mesmo GUID, zero missing scripts e um EventSystem (`FinalIntegrity-final.json`). Console final consultado sem limpar: zero erros (`console-final.json`); warnings de fixture permanecem nos resultados. Comparação SHA256 final preserva cenas, prefabs, clips, GUIDs/metas existentes: somente CharControlScript, ObservationTelemetry e a fixture CursorFluidity preexistentes mudaram; partial/fixture de impacto e seus metas são novos. As546 exclusões Git são idênticas ao snapshot inicial. Nenhum builder ou alteração do Nexus nesta tarefa.

**Limites e próximo recorte:** Mutations/BurnTick não foram alterados nem incluídos como falhas deste lote. Não houve validação humana de peso/fluidez, contato geométrico em todos os alvos, pressão prolongada ou swarm. Recomenda-se GI1.5 como playtest curto com input físico, aproximação, cinco socos consecutivos, esquiva/skill e retomada contra alvos móveis; registrar hesitação/ameaça/legibilidade antes de propor ajuste pontual. A correção1.4 está tecnicamente validada nos filtros acima, sem declarar acabamento ou game feel aprovados.

## Gameplay Intelligence 1.5 — resposta e sessão humana (25/10/2026)

Preparação com AGENTS, skills locais game-vision/combat-unity/unity-acceptance e guia Unity MCP; observação segue os documentos canônicos, sem inventar uma skill adicional. Instância/projeto reconfirmados via MCP: Tech-Guy@fccafb2b47cd7671, D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1. A coleta reutiliza CombatPolishPreview e o cliente existente Tools/GameplayObservation/observe_gameplay.py.

**Mudanças restritas a diagnóstico/preparação:** InputEdgeObserved passivo com bindings/Shift/pointer/dispositivos/foco e NavigationSnapshot; nenhum consumo/reenvio de input ou novo bus. `observe_seconds` opcional de0..30, somente current, dá duração real previsível ao trecho humano sem impor FPS/timeScale; comportamento por frames antigo continua padrão. Roteiro/preset em04_CHECKLIST_PLAYTEST.md e Tools/GameplayObservation/gi15-human-current.json. Sem mudança de dano, cadência0,20 s, animação, cancelamento, buffering, IA, mapa, boons ou Nexus.

### Verificação técnica

- **4/4 diagnósticos PlayMode**, job `c2b4ad3c37084dcbae1d4c170cea007a`, `Library/GameplayObservation/gi15/diagnostic-final-result.json`: Q→E cedo na recuperação publica Busy uma vez e não executa E depois sem nova pressão; Q→E na janela final aceita/cobra uma vez e apresenta no primeiro render; Q aceita durante hitstop de básico conserva uma utilização e retoma apresentação após freeze; observação de tecla remapeada respeita binding, não consome flags e não duplica comando. Fixture sintética com perfil/avatar reais e reserva de mana explícita; não é sessão humana. Traces JSONL com prefixos early-recovery, late-recovery, skill-during-hitstop e virtual-input na pasta gi15. Em um trace final, Q recovery/progresso global0,6805433 rejeitou E com Busy; nova E após fim foi aceita. Não há buffer geral nessa passagem.
- Tentativas anteriores dos diagnósticos:1/3 e2/3, preservadas em diagnostic-result e diagnostic-repeat-result. Corrigida reserva da fixture que era limitada pelo maxMana mínimo, e inspeção visual movida da fronteira de coroutine para depois do LateUpdate no mesmo frame. Esses erros da fixture não foram classificados como bugs de gameplay nem geraram alteração de contratos.
- **Lote relevante79/80 PlayMode**, job `928deaa2aee94d2b892d3b25caa9602e`, regression-result.json; filtro exato em regression-tests-params.json. Inclui16 casos GI1.4, quatro diagnósticos GI1.5 e60 regressões de scheduler, Q/W/E/R, recursos/múltiplos contatos, dash/cancel/iframes, pausa/hitstop, input compartilhado, Arco/Lança e movimento. A falha foi `Hold_RepeatsAtAttackInterval`, contagem1 onde se esperava>1; nenhum teste foi enfraquecido. Repetição isolada **1/1**, job `6f5b1e772cd84f54bc16855390e13f3d`, hold-result.json. Sensibilidade ao contexto já relatada historicamente permanece sem causa fechada: não declarar suíte completa verde nem perda de hold comprovada no jogo humano.
- **25/25 EditMode**, job `536d6d5fd09f408b9cadd6db348a6f62`, edit-result.json: ObservationTelemetryTests/RangedFluidityContractTests, incluindo validação do novo prazo de observação. Mutations/BurnTick históricos não foram reexecutados ou corrigidos como parte deste lote.

### Sessão física e evidência

O desenvolvedor escolheu jogar agora e operou mouse/teclado reais. Só a entrada em Play Mode foi acionada pelo MCP; portal, percurso, combate e recompensas foram humanos. As duas capturas usam current, agenda vazia, RNG não controlado,1280×720, PNG a cada60 frames e JSONL por frame. Não houve ação de combate, Warp, stats, energia, inimigo ou boon injetados para a coleta. Não são comparações controladas antes/depois.

| Captura completa em Library/GameplayObservation | Encontro inicial real | Janela e evidência |
|---|---|---|
|20261025T180351641-6e6ca54e|FirstSector/nó2,7 inimigos; seed5522680045370970550|30,018 s reais;73 PNGs;12 básicos aceitos; Q/W/E e dash;420 HP inimigo e12 HP jogador|
|20261025T180525631-7d1c577f|FirstSector/nó5,6 inimigos; mesma run/seed|30,017 s reais;52 PNGs;5 básicos aceitos; Q/W/E e dash;312 HP inimigo e67,2 HP jogador|

Totais vêm apenas de ActorDamageReceived. Metadados de61 recibos de HP não apresentaram duplicação de alvo+geração+sequência por trecho; hooks/ProtocolImpact não foram somados. Houve17 básicos aceitos e17 recibos Basic contra inimigos; não se afirma série física isolada de cinco contatos, nem total esperado de Asura sem considerar cap/boons. Imagens reais inspecionadas mostram grupo, telegraphs, ação e HUD; PNG esparso não aprova todos os contatos ou recovery. Não houve vídeo contínuo ou A/B formal sem coletor.

Não ocorreu AbilityRejected nos dois trechos; portanto as tentativas físicas específicas de recovery cedo/tarde não foram certificadas. Esses casos estão demonstrados em fixture, não preenchidos automaticamente no checklist humano. Pressões de dash excederam ativações; sem recibo universal de gate/rejeição, não foram rotuladas como input perdido. Asura Cadenciada foi encontrada no inventário da run, junto de Asura reverberante e Frost; não houve input Skill4/F11 nos clipes. A terceira coleta foi somente armada e não iniciou, pois a run ficou em RewardPending; não há execução registrada da transformação.

**Feedback humano direto:** “O jogo está legal, a sensação dos golpes estão legais”; o desenvolvedor apontou como incômodos atuais os inimigos, a interação dos golpes com eles, o mapa e os boons. Avaliação geral positiva dos golpes, sem aceite individual de todas as tarefas. Nenhum caso concreto de golpe ignorado ou animação/HP divergente foi relatado. Não converter esse comentário em aprovação de cada cancelamento, build, pressão ou encontro.

### Riscos e decisão

1. **Maior prioridade percebida: inimigos e resposta aos acertos**, por relato humano. É desconforto confirmado; causa técnica/ação específica ainda não localizada. GI1.6 deve isolar resposta/retomada/ameaça de um inimigo real antes de propor correção. Mapa e boons seguem como recortes posteriores, sem reformulação em lote.
2. **Rejeição temporal pode parecer comando ignorado:** evidência técnica Busy antes da janela e ausência de fila geral, sem reclamação humana específica nesta sessão. Preservar contrato; usar o roteiro caso o incômodo apareça. Q/W/E têm janelas Skill diferentes e dash/Cadenciada/movimento não são prova de buffering geral.
3. **Repetição de básico merece acompanhamento:** falha79/80 apenas em lote, isolado verde; repetição real e17 acertos Basic foram coletados. Não há causa suficiente para mudar ataque/cadência.
4. **Legibilidade sob pressão/build:** houve combate real com dano recebido, mas ainda falta incidente humano localizado e execução registrada de Cadenciada. Não substituir julgamento por contagens, notas artificiais ou imagem estática.

**Buffer geral: não implementar/recomendar entrada agora.** O feedback atual não o pede. Se surgirem incidentes repetidos, decidir separadamente uma intenção elegível no fim da recuperação, duração curta medida a partir desses incidentes, execução só quando o contrato abrir, revalidação de custo/alvo, descarte em pausa/UI/morte/troca/fim de encontro e feedback de fila/rejeição. Não ampliar janela de cancel nem cobrar recurso ao apenas guardar intenção. Não se fixou duração ou código nesta tarefa.

**Proteção/encerramento:** antes de liberar a sessão física, as cinco chaves do perfil eram iguais ao snapshot (preferences-before-human.json). Depois, progresso humano foi preservado, sem restaurar backup antigo sobre moedas/boons/run novos. Cena final FirstSector, jogador vivo em RewardPending, Play Mode mantido para o desenvolvedor; coletores fechados e sem mouse virtual de observação (final-state.json). Console consultado sem limpar: zero erros. Hashes preservam gameplay, cenas, prefabs e metas existentes; mudanças preexistentes limitadas a dois scripts Editor e testes de observação, com nova fixture/preset/documentação. As546 exclusões Git permanecem idênticas. Nenhuma correção de gameplay especulativa foi implementada.

## Gameplay Intelligence 1.6 — interação com inimigos (25/10/2026)

**Proteção da sessão humana:** a primeira leitura MCP encontrou FirstSector em Play Mode; não houve teste, recompilação, reload, descarte de recompensa ou stop nesse período. Inspeção de código/docs/registros continuou sem interferência. Nova leitura encontrou NexusBlockout em Edit Mode e o desenvolvedor confirmou “pode seguir, já encerrei”. Só então foram salvos os dados atuais e iniciadas validações. O snapshot desta etapa é posterior à sessão humana; não foi reutilizado o backup antigo da GI1.5. Instância/projeto reconfirmados: Tech-Guy@fccafb2b47cd7671, D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1.

Skills aplicadas: tech-guy-game-vision, tech-guy-combat-unity, tech-guy-unity-acceptance, unity-mcp-orchestrator e critérios de papéis de tech-guy-enemy-encounters. Sem criação de arquétipo, alteração de grupo/spawn ou novo sistema. Arquitetura e alternativas estão em [inimigos](../systems/03_INIMIGOS.md#interação-e-apresentação--gi16-25102026).

### Três achados por impacto

| Achado/reprodução e sintoma | Evidência e causa | Dono, melhoria esperada, risco e decisão |
|---|---|---|
|1. Grunt não transforma aproximação em ataque contra jogador parado, FirstSector/nó1/seed1701,12 s.|Zero amostras de windup/controle; navegou em718 amostras e permaneceu a1,227–2,287 m. Inspector/prefab: escala0,5, elegibilidade melee1,075 m; preferência2,20 m e tolerância0,50 m. Não é IA congelada ou stun. Conflito de banda e sintoma confirmados; contribuição de separação/root motion/rotação ainda requer isolamento causal.|EnemyAI/PreferredDistanceLayer/Profile. Esperado: chegar a uma posição realmente apta a atacar, preservando resistências/dano. Risco médio, altera ameaça/prioridade: **não corrigido**, exige aprovação. Recomenda-se reconciliar aproximação/tolerância com alcance efetivo; aumentar alcance/geometria é alternativa de balanceamento, não correção implícita.|
|2. Grunt/Shooter perdem a pose ofensiva antes do render.|Regressão com prefabs/avatar/library reais mediu desvio de20,97°/21,04° no braço em windup. Graph manual foi avaliado antes de o AnimatorController sobrescrever a pose. A amostra declarada pelo graph não garantia o corpo renderizado.|EnemyCombatActions/SkillAnimationPlayer. **Única correção implementada:** LateUpdate reaplica a amostra já selecionada enquanto `_busy`; sem avançar relógio, trocar motion, aplicar hit ou criar prioridade nova. Risco visual baixo, cancel/complete liberam graph como antes. Vermelho→verde, erro de pose<0,1° no teste.|
|3. Sniper anuncia uma direção e pode disparar em outra quando o alvo se move.|Fixture com Sniper real, receptor sintético movido de+Z para+X durante windup: aviso permaneceu em+Z, projétil em+X, divergência90,000°. Um contato de10 HP, sem duplicação. O código constrói a lane no começo e usa Face(target) novamente na emissão.|EnemyCombatActions.SniperShot/EnemyAttackExecution. Esperado: aviso coerente com o tiro. **Não corrigido:** decidir entre aviso acompanhando a mira final ou tiro travado na direção anunciada. A segunda muda targeting/contrajogo; ambas exigem revisão autoral antes da próxima implementação.|

O Shooter também usa aviso sem área espacial (AimedShot, preparação0,25 s); sua legibilidade depende do corpo/projétil. Isso é contexto adicional, não um quarto bug corrigido. A diferença entre flinch, cancelamento e hard CC foi preservada; Elite/Boss continuam respeitando resistência e proteção de retomada.

### Regressão e testes Unity

- Fonte nova: `EnemyInteractionPresentationTests.cs`. Primeiro ensaio de pose **1/2**, job `b1ba4c693526460ba3edb5c775b62e38`, pose-before-result.json. Ampliado antes da correção: **1/3**, job `4deb8adb7b5246e38513e1e990c56192`, pose-expanded-before-result.json; Grunt/Shooter falharam por divergência de pose, cancelamento passou.
- Após o único patch, **4/4**, job `7c5440209f8044f785bcab246de23e01`, pose-after-result.json: pose de Grunt/Shooter, um contato correto, cancel/cleanup/root motion e pausa. A fixture desliga IA/Nav/EnemyVariant.Start para isolar a fronteira de render; receptor tem HP sintético. Não é observação natural.
- Lote amplo relevante: **84/86**, job `8b3cbd4c7e044b33a431fe2f4fc02e25`, regression-result.json. As duas falhas foram novas fixtures de reação sem CombatReactionController, normalmente instalado por EnemyVariant.Start que estava desativado. Corrigiu-se apenas o preparo para instalar o componente/perfil de produção, sem mudar assertions ou mecânica.
- Repetição dos novos casos: **6/6**, job `edf3ee3d3ce74383995bad5892606754`, pose-final-result.json: acrescenta interrupção elegível em Grunt real, GetHit próprio após cancel, nova ofensiva e preservação do impacto já comprometido/recuperação fora de windup. Solicitação de reação é explícita na fixture; não foi fabricada uma interrupção natural na produção.
- **80 regressões existentes aprovadas** no lote acima: PostureReaction (reinício/retorno GetHit, Normal/Elite/Boss, quebra, fallback, proteção e retomada), EnemyAttackExecution/Physical/Variety, projéteis entre cenas, hitstop/pausa, navegação/kiting, Q/W/E/R e básicos da Manopla, múltiplos contatos, recursos e básicos Arco/Lança. Filtro exato em regression-tests-params.json. Não é a suíte global.
- Diagnóstico geométrico adicional **1/1**, job `b4435e7001b2487ea3bcfdf65b3cc916`, sniper-diagnostic-result.json. O teste verifica captura/um contato e grava as direções em sniper-warning-*.json; seu resultado verde não aprova a política de mira divergente.
- **25/25 EditMode**, job `d55265c3ef5f43f2a6eb37aa9e278eae`, edit-result.json: ObservationTelemetryTests/RangedFluidityContractTests. Falhas históricas de Mutations, BurnTick e hold sensível ao contexto não foram reclassificadas como corrigidas nesta etapa.

### Observações reais e comparação

Artefatos relativos a Library/GameplayObservation; todas as novas capturas usam FirstSector/nó1, seed1701, Manopla de produção, cinco inimigos Grunt/Heavy/Charger/dois Snipers,1280×720. Setup assistido explícito do pipeline: seed em memória antes de Awake, RNG de IA controlado, seleção pública de rota e Warp inicial do jogador. Não houve alteração de HP, postura, resistências, dano, mana, IA habilitada ou grupo para fabricar resultado; input automático não é humano.

| Captura | Agenda e propósito |
|---|---|
|20261025T182607865-aaded41b antes /20261025T183542922-49306f9e depois|Sem comandos,360 frames/120 PNGs por lado. Comparador em gi16/passive-comparison:120 pares, zero lacunas/diferenças de condições ou overrides. Mesmo HP recebido21,59998 no frame180; mesmas transições do Sniper e recursos. PNG138 antes/depois inspecionado: muda a pose ofensiva do Sniper, preservando aviso/cenário.|
|20261025T183702968-36b8850f|Passivo prolongado720 frames/120 PNGs: Grunt não inicia ataque; Sniper005 emite novamente, com dano ao jogador nos frames180/449/718. Demonstra continuidade ofensiva ranged e ausência de pressão do melee neste cenário, não paralisia global da IA.|
|20261025T183834908-063df3bd|Dois básicos nos frames30/50,240 frames/120 PNGs; foco no Grunt real. PNGs36/56/104 inspecionados junto dos recibos/reação/Animator.|
|20261025T185449017-0860c283|Q aceita no frame130 durante windup do Sniper,300 frames/150 PNGs. Não houve contato de HP com inimigo: a Q errou; Sniper emitiu e causou dano. Não conta como skill acertando/interrompendo windup. A interrupção legítima foi demonstrada somente na fixture identificada.|

**Timeline real do Grunt, sem hard CC criado:**

| Frame/tempo de gameplay | Fato |
|---|---|
|30/t0,500|Básico aceito; HP ainda60.|
|35/t0,583|Recibo20 HP,60→40; postura100→88; GetHit reinicia em0. Sem stun, airborne, break ou ação inimiga em windup.|
|50/t0,800 /55/t0,883|Segundo básico aceito e contato posterior: HP40→20, postura88→76; GetHit reinicia. Asura final6, mana1500, sem morte/recompensa inimiga.|
|98/t1,567 /102/t1,633|Clip concluído entra em blend e retorna à raiz de locomoção Idle/Speed; não significa agente parado. Postura recupera depois do delay existente.|
|Até239/t3,917|Grunt continua vivo/navegando, sem nova ofensiva; o problema de aproximação é independente do retorno visual correto.|

**Continuidade real do Sniper:** windup105/t1,750 → emissão166/t2,767 → HP do jogador180/t3,000 → recuperação concluída193/t3,217; no trecho longo há novos contatos449/718. Interrupção fora da janela e ação protegida Elite/Boss foram verificadas em testes, não apresentadas como eventos naturais dessas capturas. Não se obteve quebra natural sem alterar autoria, e não se alterou autoria para produzi-la.

**Encerramento:** único arquivo de runtime preexistente alterado é EnemyCombatActions (reaplicação visual); fixture e meta são novos. Nenhuma alteração de EnemyAI, resistência, thresholds, assets/Animator/clips, kits, mapa, boons, NexusBlockout ou save. As cinco chaves de seleção/moedas/desbloqueios/resumo foram restauradas exatamente do snapshot posterior ao encerramento humano, com guarda contra sessão desconhecida. Editor final NexusBlockout limpa/Edit Mode, mesmo GUID, zero missing scripts, um EventSystem e Console sem erros consultado sem limpar. Conferência SHA256 de1730 arquivos preexistentes: somente EnemyCombatActions mudou;546 exclusões Git idênticas. Evidências em gi16/final-file-integrity.json, final-integrity.json e preferences-restored.json.

**Próximo passo:** GI1.7, sob decisão autoral, reconciliar aproximação melee com alcance e depois a política aviso/mira do Sniper. A correção visual está tecnicamente validada, não humanamente aprovada. Bow2.0/Spear2.0 devem reutilizar HP/postura/resistências e enfrentar ameaças legíveis/coerentes; não compensar esses problemas com buffs de arma, CC indiscriminado ou buffer geral.

## Gameplay Intelligence 1.7 — aproximação e alcance melee (25/10/2026)

Autorização: corrigir aproximação→ataque sem vantagens de alcance/dano/velocidade ou redesign de IA. AGENTS, canônicos de estado/prioridades/combate/inimigos e GI1.5/1.6 consultados; skills reais combat-unity, game-vision, unity-acceptance, enemy-encounters e unity-mcp-orchestrator aplicadas. MCP confirmou Tech-Guy@fccafb2b47cd7671, Unity6000.5.10f1, D:/Dev/Personal/My_games/Tech-Guy. Estado inicial NexusBlockout em Edit Mode; nenhuma sessão humana interrompida. Servidor MCP existente acessado pelo cliente do projeto com descoberta real, sem batchmode concorrente ou servidor substituto.

### Causa, regressão vermelha e correção

O Grunt usa centros: gate melee3D1,075 m, cone1,20 m, enquanto a preferência2,20 m e tolerância0,50 m operam no plano. No piso do cenário, diferença vertical entre atores é zero. Os raios de collider não são somados ao gate/cone; agent.radius0,50 m é parâmetro de navegação, não extensão de ataque. A captura diagnóstica anterior mostrou323 sugestões a2,20 m, caminhos PathComplete, stoppingDistance0 e distância mínima1,227 m, sem elegibilidade/ataque em12 s. Há alternância entre sugestão de afastamento e chase com offset; não se trata de uma IA parada nem de facing bloqueando um ataque já elegível.

Ablação independente de60 Hz, mesmo prefab/escala/perfil/seed/local inicial e receptor sintético com NavMeshAgent: original zero emissões em6 s; desabilitar somente sugestões permite três; desabilitar também offset mantém três. Avoidance, colisores, root motion e rotação continuam ligados. Logo, o conflito de posicionamento é causa suficiente do bloqueio reproduzido, sem atribuir universalmente toda oscilação a um único efeito físico.

Regressão final anterior ao patch habilitado: job `834b572f7e0a445ab48a0fd0ec22dd6b`, **1/2**, red-result-production-composition.json: diagnóstico de ablação passa, `ProductionGruntApproachesHitsRecoversAndAttacksAgain` falha porque não entra em alcance em12 s. Composição inclui o mesmo CombatReactionController/rank que ProgressionDirector instala antes de ativar o prefab. A propriedade limitante ficou explicitamente infinita nesse ensaio, equivalente à política anterior; nenhuma sugestão era restringida.

Única correção mecânica: EnemyAI fornece o limite do gate existente somente para Grunt Normal/SmallMob; PreferredDistanceLayer deriva orçamento planar pela diferença vertical, limita preferência a90% desse orçamento, restringe a sugestão após separação e reduz tolerância à margem restante. Plano: destino máximo0,9675 m/tolerância0,1075 m, sem mudar elegibilidade1,075 m/cone1,20 m. Profile, velocidade, damage12, cadência, windup0,40 s, recuperação corporal0,30 s e recuperação IA0,35 s preservados. Rota completa continua validada/aplicada pelo EnemyAI; não houve warp do inimigo, alteração de collider/avoidance ou novo controlador.

Arquivos: EnemyAI.cs e PreferredDistanceLayer.cs no runtime; CombatPolishPreview.Observation.cs acrescenta somente comando assistido `move`, encaminhado ao gate real TryRequestGroundMovement (sem SetDestination direto); ObservationTelemetry acrescenta diagnóstico passivo de distância, sugestão, navegação, facing, busy e cooldown; MeleeEngagementPlayModeTests.cs/meta são novos. O flag attack_eligible é a última percepção da IA e pode estar desatualizado durante sua rotina ocupada; distância é amostrada no frame. Não interpretar flag antigo como aceitação de ataque distante.

### Antes/depois de produção

Pasta base Library/GameplayObservation. FirstSector/nó1, seed1701, Manopla, cinco inimigos originais Grunt/Heavy/Charger/dois Snipers,1280×720,60 Hz,720 frames e60 PNGs por captura. Espaço verificado antes: D:~33 GB livres. Setup assistido existente: seed antes de Awake, RNG de IA controlado, rota pública e Warp inicial somente do jogador. Nenhum HP/postura/resistência/recurso/roster alterado. Não é sessão física humana.

| Cenário | Antes | Depois | Evidência |
|---|---|---|---|
|Parado|20261025T234239127-489a1bcb|20261025T235007519-7ac00b90|Antes1,227–2,287 m/zero windup; depois aproxima2,279→0,936 m, cinco contatos de12 HP. Sniper mantém contatos21,59998 nos frames180/449/718.|
|Move +4X no frame100 e −4X no300|20261025T234410468-122b9e24|20261025T235036949-c140fefc|Os dois comandos passam pelo gate real e são aceitos em ambos. Antes uma emissão melee sem HP; depois quatro contatos melee de12 HP, com retomada após deslocamento. Saída do cone durante windup foi verificada separadamente em fixture.|

Comparador existente: stationary-comparison e moving-comparison, ambos **comparable**,60 pares cada, sem override de condições. Mesmas posições iniciais/perfis/seed/câmera/resolução/agenda por par. Captura inicial adicional20261025T233536964-a31d6178 preserva o sintoma antes de ampliar a telemetria; não foi usada como substituta da comparação diagnóstica. PNGs72 antes/depois,84 depois e228 do movimento foram abertos: aproximação, preparo, follow-through/HP e perseguição apresentados com a correção visual GI1.6 preservada. PNGs espaçados não aprovam todos os frames ou game feel.

| Frame/tempo relativo — parado depois | Resultado observado |
|---|---|
|0/t0|Grunt2,279 m do jogador, fora do alcance.|
|52/t0,867|Elegível a0,936 m; windup pelo executor existente.|
|76/t1,267|Uma emissão e um recibo de12 HP; jogador1000→988.|
|94/t1,567|Recuperação corporal termina; rotina da IA ainda ocupada.|
|115/t1,917|Rotina liberada, sem busy permanente; espera a cadência existente.|
|185/t3,083 →209/t3,483 →248/t4,133|Novo windup, segundo contato12 HP e nova liberação de recuperação.|
|341/473/605|Terceiro/quarto/quinto contato12 HP. Sexto windup713 ainda dentro da janela gravada.|

Depois em movimento: windups52/207/450/582, contatos76/231/474/606, busy liberado115/270/513/645. Mana1500/Asura0 preservados; todos os inimigos vivos, sem recompensa/morte. HP final parado875,20007 =60 melee+64,79993 ranged removidos; movimento865,60010 =48 melee+86,39990 ranged. São recibos ActorDamageReceived, sem somar hooks sobrepostos. Fixture de alvo único exige uma transação por soco conectado e total igual ao dano autorado.

### Testes Unity e limites

- **11/11 focados**, job `445ff65a0a1045f0bc0a50e61556f5da`, focused-result-final.json: dez casos funcionais e um diagnóstico de ablação. Cobre aproximação, dois ciclos completos, moving target escapando do cone comprometido e sendo perseguido novamente, pelo menos dois de três melees emitindo, grupo Grunt/Shooter, rota completa contornando obstáculo, alvo desconectado sem dano/bypass, restrição Normal/Elite/Legendary/Boss/Heavy, GetHit repetido, perda e morte do alvo. Prefab/estatísticas inimigas reais, terreno NavMesh e receptor sintéticos declarados; reações/dano de preparo explícitos na fixture GetHit, não em captura de produção.
- **87/87 regressões PlayMode**, job `2876b685e6ba421086563cc4809a758c`, regression-result.json:86 casos funcionais existentes e diagnóstico geométrico Sniper. Inclui GI1.6 apresentação/cleanup/pausa, GetHit/recovery, postura/quebra/Normal/Elite/Boss e proteção, windup/compromisso, hitstop, navegação/kiting ranged, Q/W/E/R e básicos Manopla, recursos/múltiplos contatos, básicos Arco/Lança e lifecycle de projéteis por três cenas descarregadas. Filtro exato regression-tests.json. O teste Sniper verde registra divergência, não aprova ou corrige sua mira.
- **25/25 EditMode**, job `b36b44e093a44a709525c57efdb156ce`, edit-result.json: ObservationTelemetryTests e Fluidity.EditMode.RangedFluidityContractTests.
- Tentativas preservadas: primeiro job retornou zero testes antes da importação, outro falhou ao inicializar com dependência de serialização inexistente na fixture (substituída por JsonUtility). Não contam como aprovação. Primeira fixture sem agente do jogador não reproduzia fielmente; a versão com agente e60 Hz reproduziu. Primeiro candidato2/9 tinha materialização de reação ausente; após instalar o mesmo componente do spawner,9/11 passaram. Os dois restantes estavam fora do sightRange autorado7 m (Shooter8 m/obstáculo10 m); corrigidas apenas posições da fixture para6 m, mantendo assertions e estatísticas. Último lote11/11 acima é o válido. Nenhum contrato foi relaxado para passar.

Não se executou suíte global, partida humana, swarm8/32/64 ou nova run natural Elite/Boss. A política está restrita ao Grunt; não declarar corrigido o posicionamento de Heavy/Charger/Rush. Históricos Mutations/BurnTick/hold sensível ao contexto continuam separados e não foram declarados resolvidos.

**Integridade final:** SHA256 de1558 fontes/metas/cenas/prefabs/assets/clips/controllers preexistentes encontrou apenas os quatro scripts esperados acima modificados. EnemyCombatActions/GI1.6 e assets/metas existentes preservados; novo teste/meta importados pelo Unity.546 exclusões Git idênticas. Cinco chaves do snapshot atual (arma/moeda/dois desbloqueios/RunSummary) restauradas com igualdade exata e guarda de sessão; não reutilizado backup antigo. NexusBlockout final limpa/Edit Mode, mesmo GUID, zero missing scripts, um EventSystem; Console consultado sem limpar, zero erros. Evidências: gi17/final-file-integrity.json, final-integrity.json, preferences-restored.json e console-final.json.

**Recomendação:** fechar tecnicamente GI1.7 no escopo Grunt comum; aprovação de sensação/pressão continua humana. Propor GI1.8 para Sniper: tracking cedo, instante explícito de aim-lock e direção de aviso/projétil consistente após compromisso, com alternativas/janela por decidir antes de implementar. Não houve mudança no Sniper, armas, boons, mapa, economia ou Nexus.

## Gameplay Intelligence 1.8 — aviso e trajetória do Sniper (25/10/2026)

Projeto confirmado via MCP: D:/Dev/Personal/My_games/Tech-Guy, Tech-Guy@fccafb2b47cd7671, Unity6000.5.10f1; NexusBlockout inicial em Edit Mode, sem sessão humana ativa. AGENTS, canônicos e GI1.6/1.7 consultados; skills reais de combate Unity, game vision, enemy encounters e Unity acceptance/orquestração MCP aplicadas. Acesso pelo cliente existente Tools/GameplayObservation/observe_gameplay.py ao servidor MCP descoberto, sem Editor batchmode concorrente. D:~33 GB livres antes de coletar; somente duas capturas de60 PNGs nesta etapa.

### Causa e contrato implementado

Dois pontos de aquisição divergiam: SniperShot criava o aviso com origin/forward iniciais e recalculava Face/MuzzleOrigin no fim; EnemyProjectile.SetupStraight também apontava novamente para o alvo, ignorando a intenção do facing recebido por Spawn. Corrigir somente o segundo Face não bastaria.

O compromisso agora ocorre na fronteira existente de criação do aviso visível: mesma origem/direção para lane e emissão após o windup. SniperShot reutiliza os locals e passa lockDirection:true à factory/configuração do projétil. Default false conserva os outros consumidores; a opção é argumento de configuração, sem modo persistente no pool. Não há nova fase de tracking, lock percentual, espera, bus ou gerenciador. Movimento anterior à ação participa da aquisição inicial; movimento após o aviso não redireciona o tiro. LoS/controle/validade continuam podendo cancelar antes da emissão, sem trocar a trajetória.

Preservados: dano/profile, windup1 s, recuperação corporal0,45 s e cadência existentes, velocidade24 m/s, range26 m, raio0,35 m, clipping contra geometria, transação única e lifecycle. AimedShot/SpreadShot/LobShot mantêm seus caminhos anteriores. Correção visual GI1.6 e aproximação GI1.7 não foram removidas nem reescritas.

### Regressão antes/depois e testes Unity

- **Antes:0/1**, job `463f8cd52aaf4637b2c681fc901a4167`, red-result.json. Novo `MovingAfterVisibleWarningCannotRedirectTheActualFlight`, Sniper/perfil/dano reais: aviso+Z, receptor sintético movido para+X após0,30 s, voo medido pelo deslocamento real. Divergência90°, uma emissão, um recibo21,59998 HP; assertion de coerência<0,1° falhou. Artefato lateral-d63ae4d3e81e44a0af8ea907f65b5637.json, sem alterar implementação para produzir o vermelho.
- **Depois:11/11 focados**, job `0fa653acfd4f4c67ad1d61769e55d032`, focused-result.json. O mesmo cenário passa com0° e zero contatos; lateral-409e0f8a5efc46e4944a251240bd1965.json. Cobre alvo parado com uma transação/dano autorado, movimento antes da ação capturado pelo primeiro aviso, movimento posterior sem snap, stagger elegível pré-emissão removendo aviso/tiro, cancelamento pós-emissão mantendo um único projétil e contato, pausa/hitstop conservando mira, morte/destruição do alvo durante preparação, dois Snipers com snapshots independentes e reconfiguração explícita do mesmo projétil restaurando o default anterior. IA/Nav desativados para isolar execução; receptor/posições e controle na fixture são assistidos, não uma partida natural.
- **98/98 regressões PlayMode**, job `e2f6bdcf510945388b49406c4272a671`, regression-result.json. Filtro exato regression-tests.json:87 casos do lote GI1.7 e11 de MeleeEngagement, incluindo seu diagnóstico de ablação. Cobre GI1.6 pose/cleanup, GI1.7 aproximação/obstáculos/continuidade, Normal/Elite/Boss, postura/proteção/interrupção, hitstop/pausa, ranged/kiting, ataques das três armas e projéteis por repetidos unloads/loads. O diagnóstico Sniper antigo foi atualizado para exigir o contrato aprovado, medir a direção efetiva `_direction` antes de qualquer contato e aceitar esquiva sem HP; evidência histórica GI1.6 não foi substituída. Não se manteve uma assertion de hit obrigatório quando a nova trajetória deve errar.
- **25/25 EditMode**, job `e4d8a2a4994842ce8c1a96d7140eae0e`, edit-result.json: ObservationTelemetryTests e Fluidity.EditMode.RangedFluidityContractTests. Nenhuma alegação de suíte global; Mutation/BurnTick/hold históricos não foram reclassificados como resolvidos.

Fontes alteradas: EnemyCombatActions.cs/SniperShot e EnemyProjectile.cs/Configure/Spawn/SetupStraight no runtime; CombatPolishPreview.ObservationTelemetry.cs acrescenta leitura passiva de aviso renderizado, direção configurada, viagem e identidade dono/emissão; EnemyInteractionPresentationTests.cs continua o diagnóstico com o contrato atual; SniperAimCommitmentPlayModeTests.cs/meta são novos. Não foi criado outro pipeline de eventos. Eventos FirstObserved significam primeiro frame amostrado, não um callback universal de aquisição/Spawn.

### Comparação FirstSector real assistida

Artefatos em Library/GameplayObservation: antes **20261026T003214641-46b6a13b**, depois **20261026T003704336-aef38a00** (nomes UTC; execução em25/10 local). FirstSector/nó1/seed1701, cinco prefabs/perfis originais, Manopla, mesmas posições iniciais, câmera/resolução1280×720,60 Hz,360 frames/60 PNGs. Setup existente: seed em memória antes de Awake, RNG de IA controlado, escolha pública de rota e Warp inicial somente do jogador. Movimento pelo gate real TryRequestGroundMovement: +4X no frame130, −4X no240, aceitos nos dois lados. Nenhum HP, dano, resistência, profile ou recurso alterado.

Comparador existente em gi18/comparison: **comparable**,60 pares, zero lacunas, zero diferenças de identidade/condições, nenhum override. Não se alinhou por impacto para esconder diferença temporal.

| Ataque/etapa | Antes | Depois |
|---|---|---|
|Sniper005 aquisição da mira/aviso e compromisso|105/t1,750: direção(0,360991;0;−0,932569)|Mesmo frame, origem e direção; dados conservados até a emissão.|
|Pedido de deslocamento|130/t2,167; alvo sai lateralmente da linha|Mesmo comando/percurso.|
|Sniper005 emissão/primeiro voo observado|166/t2,767: direção(0,671000;0;−0,741458), desvio20,983°|166/t2,767: direção(0,360991;0;−0,932569), desvio0°. Uma emissão por lado.|
|Sniper005 viagem/resultado|Voo registrado166–216; já erra o alvo que continua em movimento|Voo registrado166–208 na direção anunciada; zero HP ranged. Desaparecimento por lifecycle/geometria não foi rotulado como hit.|
|Sniper005 recuperação corporal|193/t3,217, Completed|Mesmo frame/estado.|
|Sniper004 aviso → emissão → recuperação corporal|171/t2,850 →232/t3,867 →259/t4,317; desvio5,406°|Mesmos frames; desvio0°, uma emissão.|
|Resultado Sniper004|Um recibo21,59998 HP em246/t4,100|Zero HP ranged: deslocamento evita a trajetória anunciada.|

As duas emissões reutilizaram o mesmo item de pool sucessivamente, com donos diferentes, sem mistura de direção. Configuração observada preserva24 m/s/0,35 m. Os dois contatos de12 HP do Grunt, frames76/243, permaneceram iguais; HP final954,40002 antes/976 depois, mana1500/Asura0, todos os inimigos vivos e nenhuma recompensa. Não confundir contagem de emissão com hit: o resultado factual vem somente de ActorDamageReceived; hooks sobrepostos não foram somados. Timeline detalhada/direções/posições em gi18/production-summary.json e flight-summary.json. A aquisição descrita na tabela é a aquisição da mira pelo executor, não uma alegação de ter registrado o primeiro instante de percepção da IA.

Inspeção visual realizada em PNG126 (aviso),156 depois (alvo fora da linha),168 antes/depois (trajetórias distintas) e246 depois (voo/HP preservado pela esquiva). Links locais: [aviso](../../Library/GameplayObservation/20261026T003214641-46b6a13b/frames/frame-000126.png), [movimento](../../Library/GameplayObservation/20261026T003704336-aef38a00/frames/frame-000156.png), [tiro antes](../../Library/GameplayObservation/20261026T003214641-46b6a13b/frames/frame-000168.png), [tiro depois](../../Library/GameplayObservation/20261026T003704336-aef38a00/frames/frame-000168.png). PNG espaçado e teste automatizado não aprovam pressão, legibilidade sob swarm ou diversão humana.

### Integridade, limites e próximo passo

SHA256 de1560 fontes/metas/cenas/prefabs/assets/clips/controllers preexistentes: apenas os quatro scripts esperados acima modificados; teste/meta novos importados. EnemyAI/PreferredDistanceLayer/GI1.7, clips/Animator, GUIDs/metas existentes e546 exclusões Git preservados. Diff de runtime limita EnemyCombatActions ao SniperShot e mantém LateUpdate/GI1.6. As cinco chaves do snapshot atual foram restauradas com igualdade exata, incluindo RunSummary, com guarda para reconhecer somente as duas sessões de observação próprias. Editor final NexusBlockout limpa/Edit Mode, mesmo GUID, zero missing scripts, um EventSystem; Console consultado sem limpar, zero erros. Evidências em gi18/final-file-integrity.json, preferences-restored.json, final-integrity.json e console-final.json.

Fechamento técnico recomendado para GI1.8 no contrato de trajetória; sem aceite humano novo. Compromisso é no primeiro aviso, sem tracking adicional dentro do windup. LoS atual continua podendo cancelar; nenhum balanceamento foi ajustado para compensar a esquiva válida. Recomenda-se GI1.9 como sessão humana curta de pressão/legibilidade com Grunt/Sniper, escolhendo a próxima melhoria por incidente concreto antes de alterar timings, outros arquétipos, encontros, mapas, boons ou armas.

## Gameplay Intelligence 1.9 — preparação do playtest misto (25/10/2026)

**Estado: PREPARADO; AVALIAÇÃO HUMANA PENDENTE.** O desenvolvedor escolheu explicitamente “Deixar o roteiro pronto para depois”. Nenhum cenário A–D foi jogado/observado nesta etapa; zero capturas novas, zero input físico/virtual/API de gameplay e nenhuma correção. Não selecionar três defeitos como se houvesse sessão nova. O relato GI1.5 (“sensação dos golpes está legal”, incômodo com inimigos/mapa/boons) permanece histórico e anterior às correções GI1.6–1.8.

Preparação: AGENTS, visão/pilares, combate/inimigos, run/encontros, prioridades e GI1.5–1.8 consultados; skills existentes game-vision, combat-unity, enemy-encounters e unity-acceptance/orquestração MCP aplicadas. MCP confirmou D:/Dev/Personal/My_games/Tech-Guy, Tech-Guy@fccafb2b47cd7671, Unity6000.5.10f1, NexusBlockout em Edit Mode sem sessão ativa. Branch gameplayOverhaul, HEAD3a3f0ee com alterações locais preservadas. D:~33 GB livres verificados. Leituras MCP não abriram outra cena nem iniciaram Play.

**Entregue:** [checklist GI1.9](04_CHECKLIST_PLAYTEST.md#gi19--encontros-mistos-sessão-física-de-812-minutos), quatro cenários em8–12 min sugeridos, oito perguntas após cada cenário e registro de incidente. Entrada normal pelo Nexus/FirstSector, Manopla física e escolhas naturais. Três encontros ao longo da rota escolhida, sem exigir boss ou impor ramo. Grupo melee ou Sniper ausente deve ser marcado NÃO ENCONTRADO. O asset tem quatro caminhos válidos, mas FirstSector serializa randomizeRooms/randomizeEachRun ativos; o randomizador altera roster/layout por seed. Não prometer os quatro spawns autorados ou o roster seed1701 para a próxima run. A capacidade melee3 existente não foi alterada.

Preset Tools/GameplayObservation/gi19-human-current.json: current,20 segundos reais, agenda vazia, RNG não controlado, PNG a cada60 frames,1280×720. No máximo dois trechos opcionais representativos, sem iniciar o coletor nesta preparação. Game View é temporariamente configurada/restaurada pelo capturador; modo current não impõe simulation_fps/timeScale nem executa comandos. Dados reais do manifesto prevalecem sobre o rótulo GAUNTLET_SMALL_GROUP e campos de setup. O intervalo120 inicialmente preparado foi rejeitado pelo validador com simulation_fps60 e foi ajustado para60; readiness-final.json registra o preset final válido. Isso é validação de configuração, não defeito de combate.

**Checagens efetivamente executadas, sem Test Framework:** ManualRouteValidator.TrySnapshot do FirstSectorManual com Manopla carregada retornou true e quatro caminhos; CombatPolishPreview.TryValidateObservationRequest do preset final retornou true. Esses resultados em gi19/readiness-final.json verificam dados/schema em leitura Unity, não navegação, vitória, gameplay ou diversão. Nenhuma suíte EditMode/PlayMode foi reexecutada: não houve alteração de runtime. Os11/11 focados,98/98 PlayMode e25/25 EditMode acima continuam resultados históricos da GI1.8. Históricos Mutation/BurnTick/hold permanecem separados.

| Pergunta prioritária para a sessão futura, sem classificar como defeito confirmado | Evidência que falta | Origem a investigar se houver incidente |
|---|---|---|
|A pressão de vários melees cria decisões ou deixa inimigos ineficazes?|A jogado: aproximação/ataques reais, retomada sob golpes, deslocamento escolhido e comentário humano.|IA/nav, gates de ataque/controle e composição; ainda sem causa atribuída.|
|É possível compreender e evitar ranged sob pressão melee?|B/C jogados: aviso visível, direção/voo, resultado de HP/esquiva e motivo da resposta do jogador.|Apresentação, ataque, simultaneidade/composição; correção isolada do Sniper não aprova o encontro.|
|Ritmo e espaço sustentam encontros diferentes?|D jogado: três lutas/transições, decisões espaciais, tempo sem ameaça e frustração concreta.|Geometria, navegação ou composição/ritmo; não há prova nova de mapa inadequado.|

**Positivos a preservar:** apresentação ofensiva GI1.6, entrada do Grunt em alcance/recuperação GI1.7 e trajetória anunciada do Sniper GI1.8 têm evidência técnica já registrada. Não são respostas humanas ao encontro misto. Próxima ação por maior impacto: realizar a sessão, localizar/reproduzir incidentes e só então escolher até três problemas. Uma correção pequena exige contrato existente, reprodução e regressão; mudanças em dano/velocidade/controle/telegraphs/composição/mapa exigem decisão do desenvolvedor. Mapas/boons/Bow2.0/Spear2.0 ficam aguardando evidência de espaço, pressão e escolhas transformadas antes de receber alterações propostas.

**Preservação:** somente checklist, prioridades, canônico de inimigos, esta matriz e preset JSON fora de Assets foram preparados. Nenhum script/asset/meta/GUID, Nexus, kit, progressão ou economia alterado. Preferências e RunSummary foram apenas lidos; não houve restauração sobre dados humanos. O snapshot da preparação não deve substituir dados de uma sessão futura. Integridade final/read-only, hashes e546 exclusões Git preservadas em gi19/final-integrity.json, final-file-integrity.json e preferences-unchanged.json. Console inicial de erros vazio; warnings preexistentes preservados em console-initial.json. Aceite e três maiores problemas de gameplay continuam pendentes até haver experiência física e respostas do desenvolvedor.

### GI1.9 — retorno humano posterior à preparação

O desenvolvedor respondeu às oito perguntas e aos esclarecimentos em25/10/2026. São relatos humanos novos, sem captura Codex, identificação de seed/nós, duração, dispositivos ou lista de boons; não certificar retrospectivamente execução completa de A–D. O relato substitui a pendência de feedback, mas não concede aceite final.

**Preservar:** spawner foi o inimigo mais interessante por sustentar a luta; ranged em geral agradaram; a maioria dos ataques fez sentido e pôde ser evitada intencionalmente. A distribuição espalhada não foi considerada ruim isoladamente. A sugestão de reviver mortos pelo spawner é proposta de design, sem autorização/implementação de ressurreição.

| Três problemas de encontro relatados | Evidência humana | Origem e situação técnica |
|---|---|---|
|Básicos pouco ameaçadores|Disse que demoram para atacar e que o alcance de atração não provoca sensação de ameaça.|Investigar percepção/aproximação, preparação/cadência e composição. Não é prova de regressão GI1.7 nem autorização de tuning.|
|Efeitos ranged sem consequência e atacante indistinguível|Relata ataque vindo de cima, permaneceu dentro da área e não perdeu HP; não sabe o autor porque todos parecem iguais. Alguns outros efeitos também pareceram inócuos.|Suspeita de bug de ataque/efeito merece reprodução; diferenciação de papel/origem também é um problema de apresentação percebido. Executor ainda desconhecido.|
|Espaço sem identidade/propósito|Sala considerada precária e conexões grandes sem motivo, sem decisões úteis percebidas no posicionamento inimigo.|Problema humano de geometria/ritmo/identidade; nenhuma falha técnica de NavMesh ou composição específica foi demonstrada. Exige proposta autoral para mudanças espaciais.|

**Frustrações transversais:** boons foram citados como sem graça/pouco transformadores, sem nomes/slots/build informados; dash/rolamento foi preferido para viajar por parecer mais rápido que andar. Esses relatos indicam problemas de experiência, não ausência comprovada de executores de boons ou defeito comprovado de cooldown. Rework de boons, mobilidade, alcance de atração e tempos inimigos permanecem decisões separadas; não aplicar nerfs/buffs automaticamente.

**Inspeção não intrusiva posterior:** MCP reconfirmou o mesmo projeto/instância e NexusBlockout em Edit Mode; sem entrar em Play ou mudar a sessão. Leitura de SpawnerBehavior confirma produção de novos Swarms, não ressurreição. LobShot/StepArced e HazardCasterBehavior/HazardZone são caminhos candidatos: um resolve área ao aterrissar, outro ativa zonas de dano/slow após preparação. Isso não identifica o incidente e não comprova que seus componentes estavam corretos/incorretos naquela run. Nenhuma suíte/teste/captura nova, mudança de gameplay, valor de balanceamento ou escrita/restauração de save foi executada.

**Recomendação por recorte:** primeiro identificar e reproduzir o efeito aéreo sem consequência, sem presumir seu arquétipo; depois medir a pressão dos básicos e a relação andar/dash. Boons merecem investigação própria com as escolhas realmente adquiridas, e conexões de sala uma proposta espacial específica. Manter interesse do spawner e legibilidade já reconhecida, evitando uniformizar inimigos. Para Bow2.0/Spear2.0, essas lacunas de ameaça/papéis/espaço são contexto para validação futura, sem compensá-las com mudanças especulativas de arma. A GI1.9 possui agora avaliação humana acionável, com causas técnicas e decisões de implementação ainda abertas.

## GPR-00 — auditoria e desenho (26/10/2026)

Somente documentação; nenhum runtime/asset/scene/save autorizado. Projeto D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, branch gameplayOverhaul/3a3f0ee com alterações locais; o worktree C: do chat é anterior. Evidências em artifacts/gameplay-rework/gpr00; [ledger](../decisions/GAMEPLAY_REWORK_PROGRESS.md).

| Atividade | Evidência atual | Limite/status | Relatório |
|---|---|---|---|
| GPR-00.1 | MCP confirma NexusBlockout limpa em Edit Mode; bindings Q/W/E/F11 e F; 15/15 AsuraFinishControlTests, job d7511749935c444badec8449dc1c0170 | PARTIAL: visão original ausente; sem PlayMode/humano novo | [Inventário](../decisions/GPR-00.1_RELATORIO.md) |
| GPR-00.2 | Fluxos comando/HP/derivados/resumo e tickets/clones/cleanup inspecionados | COMPLETED de auditoria estática; swap ainda não implementado | [Ownership](../decisions/GPR-00.2_RELATORIO.md) |
| GPR-00.3 | 14 decisões com alternativas; 4/4 PlayMode básicos, job304278d7295e4fbc8660565529a44206 | Proposta sem implementação; warnings de fixture; sem feel/humano | [Decisões](../decisions/GPR-00.3_RELATORIO.md) |
| GPR-00.4 | Pacote rastreável R01–17/tarefas/V00–16 e riscos K01–10 | COMPLETED documental; baseline físico/visual futuro pendente antes do código | [Pacote](../decisions/GPR-00.4_RELATORIO.md) |
| GPR-00.5 | Matriz de ações/slots/conflitos; agente/rootMotion lidos no MCP | COMPLETED de design; QERX/WASD/RMB e remap futuros sem implementação | [Controles](../decisions/GPR-00.5_RELATORIO.md) |

**Fechamento GPR-00:** o bloqueio inicial da visão em00.1 foi resolvido pelo arquivo fornecido em Docs/gameplay com sufixo(1).md, lido integralmente. As cinco entregas documentais estão no ledger; fase AWAITING_HUMAN_APPROVAL. Testes atuais:15/15 EditMode +4/4 PlayMode nos dois jobs acima, sem suíte global.1597 hashes iguais, status fora de Docs sem delta e510 exclusões preexistentes preservadas. Moedas/loadout/unlocks/tutorial/RunSummary consultados iguais; resumo42387 caracteres, SHA2569751067523EABB7557FFEC504779DB853925D6665AE876DAF8C0C489D52189B3. Editor terminou NexusBlockout limpa/EditMode; consulta final de erros vazia, warnings de fixtures preservados. Baseline visual/física nova NOT RUN, prevista em C01.1 antes de código; dívidas Mutation/BurnTick/hold não reexecutadas. [Consolidado](../decisions/GPR-00_PHASE_SUMMARY.md) / [pacote](../gameplay/README.md). Nenhum runtime/asset/scene/meta alterado.

## GPR-C01.1a — baseline atual,26/10/2026

GPR-00 aprovada documentalmente com ajustes; somente C01.1a/b autorizadas. [Baseline](../gameplay/GPR_C01_BASELINE.md) / [relatório individual](../decisions/GPR-C01.1a_RELATORIO.md).21/21 PlayMode job6881b001422847d1974a22f9a82046da e15/15 EditMode jobb582bf9d07d14eac87006387f430ec9f. Capturas current1280×720 de Nexus/treino/Asura/UI:5 de comandos/UI e1 descartada para input; todas complete/restored,8PNG selecionados abertos/copiados. Scripts de diagnóstico fora de Assets dirigem apenas input legado virtual/API; nenhum novo controle.

Medição API legada:4,99857m/s planar cardinal e4,99989m/s diagonal; não comprova WASD/Agent.Move novo nem aceleração subframe. Medidor100 por contatos naturais no dummy; F11 Busy durante Q e depois1 ativação livre de Asura,100→0/custo147. UI resultados bloqueou básico no probe API; Controls exibiu bindings atuais sem Apply. Humano físico/feel e especial Bow/Spear não testados. Warnings de fixtures e tentativas de diagnóstico preservados, erros temporários de compilação in-memory separados de runtime.

1778 hashes preexistentes sem mudança;546 exclusões totais coincidem com o snapshot antigo,510 são fora deDocs (36 emDocs). Moedas/loadout/unlocks/tutorial/bindings/summary consultados iguais; Editor NexusBlockout limpa/EditMode. Nenhum asset/runtime/.meta/save/schema de produção modificado. Evidência em artifacts/gameplay-rework/c01-baseline. Não declarar suíte global/aceite humano.

### GPR-C01.1b — prontidão e revisão de decisões

[Relatório](../decisions/GPR-C01.1b_RELATORIO.md) / [consolidado](../decisions/GPR-C01.1_BASELINE_E_PRONTIDAO.md). ADR, requisitos, design, tarefas e validação atualizados: LMB clique=um pedido, RMB=especial/Asura, quatro slotsQERX independentes; terminalRMB e AsuraX superados. X inicial/hold/ChargedShot/especiaisBow-Spear/Interact/dash/motor continuam decisões explícitas. Agent.Move não aprovado por baseline de path legado; piloto de rebind/migração em memória planejado, sem Settings implementado.

Regressões adicionais nessa execução:4/4 PASS no jobd825ebde8d0943a09f3243c69e11fdd0 (BowQ/E movimento/restauração, Rremapeadohold/release, Spearretorno sem pagar novamente e alcance do básico autorado). Fixtures com recursos de teste, sem nova run natural. Somados aos filtrosC01.1a foram40 casos distintos em3 jobs, não suite global. Estado histórico de C01.1b: elaboração/AWAITING_APPROVAL. Aprovação posterior e experimento C01.2a abaixo substituem somente o gate, não ampliam a evidência humana.

## GPR-C01.2a — experimento isolado de movimento, 27/10/2026

C01.1a/b aceitas pelo desenvolvedor no escopo documentado; somente C01.2a autorizada. [Relatório obrigatório](../decisions/GPR-C01.2a_RELATORIO.md) / [protocolo](../gameplay/GPR_VALIDATION_PLAN.md). CONCLUÍDA como experimento, aguardando revisão; NÃO INTEGRADA ao player. Asura/X/especiais Bow-Spear não bloquearam o motor e continuam abertos.

| Critério / filtro | Resultado atual | Prova / limite |
|---|---|---|
| V01/02 — direção/mira/câmera/mundo/colisão/slope |10/10 PlayMode PASS, job13ca6c65d3114119b5347044bf22e09f | Assembly TechGuy.Tests.GprMovement; CSV,4 Game View PNG1160×600 abertos, teclado/mouse virtuais; dt30/60/144 sintético |
| V07/08 — gates/autoridade | PASS na fixture | root/cast/hitstop/forced/dash/pausa/foco/disable, writer único; mapping para executores reais ainda não integrado |
| Motor candidato | Move cru REJEITADO; sweep/raycast candidato técnico | Parede não baked foi atravessada pelo cru; protegida bloqueia. CC bloqueia parede mas sai da borda; nenhum controlador simultâneo ativo no comparativo |
| GI/legado/Asura |26/26 PlayMode PASS, job66c52554cb4640a6ab03ebea21280dbc |21 filtros de baseline+5 impacto/cancel/causa/cadência/hitstop; não suíte global |
| Dash cancel/i-frames |8/8 PlayMode PASS, job149ce43f84fc4383941881425886db4a | Produção intacta, fixtures existentes; não integração do motor |
| Identidade/controles Asura |15/15 EditMode PASS, jobbfd1eb5b51b34d26a07574905b96d8af | Total final59 casos distintos em4 jobs; iteração inicial7/8 e correção float arquivadas |
| V00 — integridade |1778 hashes preexistentes iguais, sem ausências nesse conjunto | Prefs/bindings/RunSummary iguais; zero dispositivo virtual/objeto C01/NavMesh temporário; NexusBlockout limpa/Edit Mode. Status raiz registra Docs.zip não versionado desaparecido, sem operação desta atividade sobre ele; origem não determinada, fora do inventário de hashes |
| Importação/GUIDs | Unity importou4 scripts/classes;5 GUIDs novos sem duplicata nos1348 metas enumerados | Checker global da skill FALHOU por relpath C:/D: de Tripo3D, não aprovado; alternativa focada documentada |
| V16 / rig / mapa real / performance de build | PENDENTE | Sem humano físico, rig/integração/crowd/mapas reais; CPU Step~11–15µs mediana apenas amostra Editor, não frame budget |

Cardinal4,998779/diagonal4,997739m/s, parada0; Agent.velocity0 levou a parâmetro Animator por deslocamento. Valores5m/s e dash20m/s×0,25s preservados. Erro de resíduo float do dash corrigido na fixture sem mudar o teste; consulta preliminar0 casos não conta. Warnings legados de keys/bridge preservados em evidência. Dados em artifacts/gameplay-rework/c01-2a. Esse foi o STOP histórico antes da aprovação posterior para C01.2b; não atesta físico/feel.

## GPR-C01.2b — motor integrado e teclas mínimas,27/10/2026

[Relatório obrigatório](../decisions/GPR-C01.2b_RELATORIO.md) / [ledger](../decisions/GAMEPLAY_REWORK_PROGRESS.md). Autorização direta para implementar após2a, mais confirmação explícita para migrar teclas de skills. IMPLEMENTADO, validação técnica executada; físico pendente. Não concluir C01/GPR-01 por esse recorte.

| Critério | Resultado | Evidência / limite |
|---|---|---|
| V01/02/08 — integração real |13/13 PASS, jobdc00aa748d8a48988c64a7dca22ea13c | Native player/agente/executores, root/contato/pose, três armas, Kiting Step, parede, pull sobre dash/hitstop; não humano |
| GI/legado/Bow/Spear |30/30 PASS final, job19d32a503db54ed68852da004c3c4c45 | Primeira execução29/30 falhou MovementDuringRecovery; isolado1/1 e grupo repetido passaram sem alteração. Causa não confirmada, risco mantido |
| Dash cancel/imunidade |8/8 PASS, job99692b45fad74704ae6fb209c4858993 | Timing/cleanup/cooldown existentes; sem alterar balanceamento |
| Finalização/identidade |15/15 EditMode PASS, job2791be3cfd27430ab28ac13cdf2a2fcb |66 casos distintos finais em4 jobs, não suíte global |
| Migração mínima de teclas | PASS pureza/round-trip/rejeição atômica | Perfil Q/E/R + especialF11 + finisherF no baseline; legado intacto, Apply DirectBinding.v2; sem AsuraX/RMB ou slots novos |
| Cenas/serialização |4 cenas habilitadas com modo direto | Somente1 flag por cena no diff contra backup; sem mapa/NavMesh/prefab rebuilt; rig real sem Rigidbody concorrente na Nexus |
| Visual/runtime |3 Game View PNG1160×600 abertos;461 amostras virtuais | Nexus livre mediana4,999993m/s, máximo5,000063, speed5; soltura0; W+D na borda bloqueou. Input/foco virtuais,0 AbilityUsed; não diagonal livre ou aceitação física |
| V00 — integridade |1789 arquivos verificados:1767 iguais,22 intencionais,0 ausências |3 GUIDs novos sem duplicata no inventário; checker global incompatível C:/D: NÃO PASS. Prefs/coins/summary consultados iguais, Editor limpo/Edit Mode |
| V07/V16 e próximos gates | PENDENTE no escopo completo | UI/reward/results, humano físico, crowd/overlap/móveis/escadas/plataformas e remap WASD. LMB/hold ainda legado, X sem conteúdo; C01.3–5/GPR-01 não iniciadas |

MCP real via cliente HTTP de diagnóstico após o usuário iniciar serviço; conector desktop retinha erro de handshake. Não foi usado um simulador para substituir validação Unity. Compilações de iteração e warnings de fixture preservados/descritos no relatório; Console runtime final sem erros, sem executar Clear. Evidências em artifacts/gameplay-rework/c01-2b.


## GPR-C01.2c — fechamento técnico,27/10/2026

[Relatório obrigatório](../decisions/GPR-C01.2c_RELATORIO.md) / [ledger](../decisions/GAMEPLAY_REWORK_PROGRESS.md). C01.2a/2b aceitas no escopo técnico; C01.2c concluída, aguardando revisão. Nenhum aceite físico desta revisão inferido.

| Critério | Resultado | Evidência / limite |
|---|---|---|
| V01/02/08 — parede/quina/borda/rampa/writer/root/impacto |23/23 PASS, jobd772c22bd06f4cf89e8b99b1e00238a3 |Sliding de locomoção: parede Z1,635858→3,0; X bloqueado; quina estável, rampa15°. Mesmo Agent; speed/dash preservados. Inclui input/TTL e testes anteriores de integração. |
| V07 — contextos UI/reload |7/7 PASS, jobd0f7ba4fcc934474ac1855dff1afbf72 |Resultados com W+Q, reload NexusLobby, pausa/reward/disable; soltura+frame, sem replay. Contextos adicionais também na bateria23. |
| GI/legado/Asura/Bow/Spear/recovery |30/30 PASS, jobba0c658bd8fb41d19ad06a9f4d6d0a03 |Preservados impacto/causa/recursos/cancel/contato; não suíte global. |
| Dash cancel/imunidade/cleanup |8/8 PASS, job3b37d6f6bc4948bfb2ba1ba79a29e4a8 |Valores e comportamento de cursor existentes. |
| EditMode identidade/bindings/finalização |15/15 PASS, jobcab379521ae54d54b644e795f93d3ad0 |Total normal final83 casos distintos, zero ignorados;12 casos novos entre os grupos. |
| Recovery intermitente |Investigado, não declarado definitivamente corrigido |Original isolado baseline1/1; grupos30 passam; hitch1501/1; hitch5000/1 reproduzido deliberadamente: unscaled0,5160198/clock0,9262705 >expiração0,7602507. Asserts/TTL/tolerância inalterados; negativo de expiração passa. Causa histórica ainda aberta. |
| R/Interact/Asura |Ownership legado testado; design Interact separado |Sem tecla definitiva; Q/E/R/F11 mantidos. Sem LMB novo/RMB Asura/X/Settings completo. |
| V00 — integridade |1795 hashes:1783 iguais/12 intencionais/0 ausências |Nenhuma cena/prefab/asset/meta/GUID alterado nesta atividade;3 GUIDs de movimento preservados no inventário. Bindings legados/coins2597/resumo consultados iguais; cena limpa/Edit Mode. |
| Unity/Console/checker |Compilação e execução MCP reais; Console final0 erros/1 warning legado |Checker global FALHOU por C:/D:, não PASS; alternativa focada documentada. Sem Clear. |
| Visual e performance observada |7 PNG Game View1160×600;757 amostras virtuais |W4,999984m/s, borda diagonal3,535536; retomada mantida0. W frame mediana5,338ms/p957,232ms; sem inferir CPU/GC/build budget. |
| V16/humano e limites |PENDENTE |Feedback físico anterior: walking/facing estranho, LMB sem alvo desejado, dash mouse/WASD configurável. Sem aprovação física desta revisão; crowd/overlap/móveis/mapas completos/TTLs sob hitch abertos. |

Artefatos: `artifacts/gameplay-rework/c01-2c/`. A captura inicial em background de pausa não mostrou IMGUI; captura complementar com Game View focada mostra overlay e estado TimeScale0. Resultado exibido é resumo histórico consultado, não run nova. Primeira fixture de quina3/4 corrigida por reposicionar origem antes de criar parede; intermediários preservados. STOP e revisão antes de C01.3.


## GPR-C01.3a — básico direcional e RMB Asura,27/10/2026

[Relatório obrigatório](../decisions/GPR-C01.3a_RELATORIO.md) / [ledger](../decisions/GAMEPLAY_REWORK_PROGRESS.md). C01.2c aceita tecnicamente;3a ampliada por autorização direta para Asura no RMB. Concluída no recorte técnico, revisão/humano pendentes.

| Critério | Resultado | Evidência / limite |
|---|---|---|
| V03/04/07/08 — novo input/Asura |19/19 PASS, job74fc13a0f30a481ebe6e5a340439bb0c |LMB single press/whiff/combo0-1-2/contact/pose/buffer único/expiração/hitstop/contextos/skill/swap; Bow exceçãoChargedShot aprovada; Spear. RMB único owner/custo/sem básico/UI/pausa/Cadenciada3presses/F. |
| Movimento integrado |23/23 PASS, jobd8786721fb814feba945e93fff8820d1 |Motor/sliding e contratos anteriores preservados. Scope virtual agora isola/restaura dispositivos/políticas; artefatos separados por fase. |
| UI/results/reload |7/7 PASS, job2704c2c356d4436ca9aaa5e81948ad42 |Não equivale a playtest físico. |
| GI/legado/Asura/Bow/Spear/recovery |30/30 PASS, jobbc1d6be6defa460987ab35809f787aab |Sem reescrever impacto/cancel/dano; incidente histórico TTL não declarado corrigido. |
| Dash |8/8 PASS, jobbf16eff1e41b43ce9980383195a0e245 |Valores/direção compatíveis; opção mouse/WASD não implementada. |
| EditMode |15/15 PASS, job043ed5373ff74f11bb99f4d7dcb70d24 |IDs/prefs/compat preservados; total final102 casos distintos,0 ignorados, não suíte global. |
| Game View |4 PNG1160×600 e1372 amostras |Player real/Nexus;3 LMB→0/1/2,1 RMB→Asura; X6,5/Z−6,8 constantes. Input/foco virtuais, energia100 em memória explicitamente concedida; não humano. |
| Custos/runtime |PASS nas fixtures sem regen; nativo mostra mínimo1353 de1500 para custo147 |Native regen ativa; não usar diferença final como custo líquido. Mesma referência/estado Asura, causa/index3 histórico intactos; F11 compatível. |
| Integridade/Console |1795 protegidos:1789 iguais/6 intencionais/0 ausências;2 GUIDs novos sem duplicata |Nenhuma cena/prefab/asset/clip/controller/meta antiga alterada; snapshots consultados iguais. Console final0 erros/1 warning legado; checker genérico FALHOU C:/D:, alternativa focada. |
| Limites/STOP |PENDENTE físico/3b |Animação directional/strafe/backpedal, dash configurável, Interact/Settings/QERX/extração completa, outros especiais e GPR-01 fora.3b não iniciada. |

Intermediários de compilação/fixture estão arquivados em artifacts/gameplay-rework/c01-3a; não contar como PASS. Primeiras falhas vieram de input virtual concorrendo com dispositivos físicos e de helper de clique escalado atravessando hitstop; isolamento/restauração e relógio real de input corrigidos sem alterar assert/TTL/cadência de produção. MCP nativo e cliente HTTP com IDs RPC únicos usados. Git/push de todo o acumulado solicitado não significa validação global das alterações preexistentes.

## GPR-01.1 — fundação,28/10/2026

[Relatório](../decisions/GPR-01.1_RELATORIO.md) / [pedidos exatos](artifacts/gameplay-rework/gpr-01-1/test-requests.json) / [resumo verificado](artifacts/gameplay-rework/gpr-01-1/verification-summary.json).

| Critério | Resultado observado | Limite |
|---|---|---|
| EditMode grafo/compatibilidade/ofertas/seeds/Asura |90/90, job1e87e68d7b7e47cc817df8f05a495354 | Resultado recuperado após desconexão durante simulação longa |
| PlayMode fundação+GI/Protocols/recursos/tickets/analytics |120/120, jobb13bdf682a7247a7a9d6fc74005e9d23 |15 focados já incluídos,210 nomes distintos com EditMode; não suíte global |
| Vermelho→verde |0/2 no job386bebd3…; Basic inválido e cooldown2→0 | Mesmas condições centrais passaram depois; falhas intermediárias arquivadas |
| Aquisição/fixtures |ID desconhecido rejeitado; timers mantidos; duas fixtures legadas preparadas pelo pool atual | Não ativar conteúdo aposentado nem flexibilizar asserts para obter verde |
| Serialização/importação |Basic4/flag16, Any15; JSON antigo + asset temporário save/unload/reload | Scratch e meta removidos; não prova save/resume de run inteira |
| Causalidade/cleanup |Receipt real, reserva única, cap negado, geração, reentrância, pausa, disable e cena; parent/HP reconciliados | Filhos técnicos injetados, sem onda de produção; políticaNone por padrão |
| Integridade/Editor |1596/1613 hashes protegidos iguais;17 fontes intencionais; sem asset/scene/meta/config preexistente alterado | NexusBlockout limpa/Edit Mode; prefs consultadas iguais; Tripo3D pendente |
| Humano/conteúdo |C01 já aceita; mutação/visual novos NÃO IMPLEMENTADOS/NÃO ACEITOS | Somente01.1 concluída; STOP antes01.2 |

RF-TROCA Q/E/R continua NÃO VALIDADO em GPR-03.1. Q/Avanço para aproximar é compatibilidade, não uma nova sinergia skill↔básico. Comparações A/B/C e economia natural exigem as etapas futuras aprovadas.
