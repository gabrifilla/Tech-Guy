# Gameplay Rework — tarefas propostas e gates

## Estado vigente — auditoria GPR-01.5, 29/10/2026

**GPR-01.5 concluída como auditoria, com ressalva de estabilidade e aceite físico pendente. STOP após01.5.** O desenvolvedor aceita01.4 como implementada/demonstrada no foco documentado; não aceitou fisicamente as novas camadas fogo/eletricidade ou a tripla. Gameplay01.3 já foi aceito. O histórico199/203 +4/4 isolados de01.4 não foi reclassificado como203/203.

A+B+C foi obtida por tickets reais, sem grants de boons, nos quatro percursos publicados deFirstSector, com assistênciaA+B desligada/restaurada no scope dos testes. Combate/conclusão dos encontros foram automatizados, não humanos. A rota atual usa geração e tem umaMutation e2–7 oportunidadesProtocol na amostra; a antiga conclusão de uma única oportunidade limitava-se à fixture. Nenhum peso/frequência/gate/sala/economia foi alterado.

Modelo determinístico64 seeds×4 caminhos×3 políticas, sorteio/commit de produção:ABC253/256 priorizandoB,252/256 priorizandoC,42/256 escolhendo uniformemente entre cards sorteados. Não são probabilidades universais nem garantia de sobrevivência/diversão. Quatro comparadores reais na câmera normal, com vários inimigos/telegraphs, mostram vento/fogo/eletricidade e feedback causal separado; leitura física continua pendente.

Validação desta atividade:98/98 focados;3/3 EditMode de amostra;8/8 produção (4rotas+4visuais);5/5 correções de ambiente/probe;10/10 checks finais;90/90 EditMode final. Regressão ampliada **203/204**: caso de pausa/recovery falhou emIsCasting após0,4s reais de pausa, passando inalterado no diagnóstico posterior. Causa ainda não confirmada, risco temporal aberto. Não declarar a bateria totalmente verde ouGPR-01 encerrada.

Somente testes/documentação mudaram: diretórios de evidência criados pelos writers; helperAsura exige recovery ainda estacionária, porque a espera móvel também reportaRecovery/progress1 e era admitida antes. Probe reproduziu a premissa incorreta; não prova retrospectivamente toda falha histórica. Nenhum timer/TTL/buffer/cadência/animação ou owner de produção alterado. Medições8/32/64 mantêm dois tornados/pool2 e cap32, com limites de Editor/GC agregado.

Antes de encerrarGPR-01 ou autorizar01.6: diagnosticar pausa/recovery e obter revisão física das camadas/tripla/telegraphs. Auditoria futura dedicada de recompensas/ritmo, layouts, identidade visual de inimigos e bosses é apenas proposta. Singularity, tuning, economia nova, novasMutations/especiais/skills e redesign não foram executados.

| Subatividade | Estado |
|---|---|
|01.5a |Concluída:98/98; oito builds e causalidade |
|01.5b |Concluída:ABC acessível na produção, amostra768 cenários e4rotas |
|01.5c |Concluída técnica; aceite físico pendente |
|01.5d |Concluída com ressalva:regressão203/204, pausa/recovery aberta |
|GPR-01 |Não encerrar como totalmente aceita |
|01.6/expansão |Não iniciada; requer autorização |

[Relatório consolidado e evidências](../decisions/GPR-01.5_RELATORIO.md). Relatórios01.5a/b/c/d preservam comandos, jobs, falhas e limites.

## Histórico anterior — não sobrepõe GPR-01.5


## Estado vigente — GPR-01.4, 29/10/2026

O pedido direto aceita fisicamente01.2c/01.3 e autoriza sequencialmente01.4a–e, incluindo transformação visual obrigatória. **GPR-01.4 implementada, com validação integrada e ressalva legada; STOP após01.4.** GPR-01.5, Singularity, novos especiais, troca modular, tuning, inimigos e salas não foram executados. Todo estado anterior abaixo é histórico onde divergir deste registro.

A=mutation_gauntlet_titan_fists; B=conductor; C=detonation, mesmos IDs/catálogos/tickets. Base/B/C/B+C mantêm execução nativa e não criam tornado. A viaja como vento; A+B acrescenta identidade elétrica e uma oportunidade Conductor; A+C acrescenta fogo e uma oportunidade Detonation em morte confirmada; A+B+C compõe vento, eletricidade e fogo, permitindo morte pelo Conductor derivado autorizar Detonation. Fogo visual não aplica burn/DoT.

Raiz e budget32 compartilhados; uma emissão Conductor e uma Detonation por tornado. Sem Conductor→Conductor, Detonation→Detonation/Conductor ou tornado→tornado no ramo novo. Native B+C preservado, inclusive suas cascatas e dedupe: não aplicar a whitelist nova globalmente. Soco e vento podem conduzir separadamente quando existem alvos elegíveis e capacidade.

Ownership visual no GauntletExecutionSnapshot aceito; ranks mecânicos na raiz aceita. A reserva intersecta ownership/snapshot e inventário, e o visual recebe essa mesma política. Aquisição durante antecipação/voo não altera projétil anterior. Cosmetic Completed conserva identidade aceita, sem root, HP, arco externo ou explosão. Pool/leases limpam fogo e eletricidade em release/clear/swap/disable/run/cena/contexto. Geometria, velocidade3,5m/s, alcance5,5m, TTL2s, raio0,65m e dano0,60× preservados.

Validação real Unity6000.5.10f1/MCP no checkoutD:/Dev/Personal/My_games/Tech-Guy:98/98 focados,12/12 visual/lifecycle,7/7 integração e90/90 EditMode. Regressão ampliada199/203; três falhas de diretório corrigidas na preparação e4/4 no diagnóstico isolado. Uma ocorrência de Asura cadenciada permanece sem causa confirmada; diagnóstico verde não é correção ou bateria ampliada inteiramente verde.368 nomes distintos com resultado aprovado observado (278 PlayMode+90 EditMode), sem ignorados nas seleções finais; não suíte global nem novo aceite humano.

Tickets reais A+B seed0 e A+C seed1; quatro ordens triplas assistidas explicitamente. Rota normal exercitada oferece somente uma oportunidade Protocol: não foi ampliada para fabricar aquisição tripla natural. Capturas Game View, receipts separados, desempenho controlado8/32/64 e todas as tentativas no consolidado e artefatosGPR-01.4. Playtest físico de leitura/feel desta revisão continua pendente.

| Atividade | Estado | Próximo gate |
|---|---|---|
|01.4a |8/8, produtores independentes; sem correção de produção |01.4b executada após gate |
|01.4b |6/6, vento letal + Detonation |01.4c executada após gate |
|01.4c |7/7, morte elétrica + Detonation e quatro ordens |01.4d executada após gate |
|01.4d |12/12 final; tint e lifecycle corrigidos |01.4e executada após gate |
|01.4e |Integração/documentação concluídas com ressalva Asura |Revisão física; nenhuma01.5 iniciada |

- [ ] Revisar fisicamente quatro aparências, impacto/arco/explosão e telegraphs.
- [ ] Investigar ocorrência legada da Asura sem retuning/relaxamento de assertions.
- [ ] Aprovar separadamente eventual01.5 de validação integrada de runs; não implementar Singularity.

[Consolidado](../decisions/GPR-01.4_RELATORIO.md) · relatórios separados01.4a/b/c/d/e e evidências novas vinculadas nele.

## Histórico anterior — não sobrepõe o estado GPR-01.4


## Execução vigente — 29/10/2026

- [x] Registrar aceite humano de01.2b e feedback futuro, sem tuning.
- [x]01.2c: reproduzir FirstSector, distinguir bloqueio de UI de terminal aceito sem raiz; corrigir apresentação não combatente e validar32/32 antes de01.3.
- [x]01.3: compor Titanic Fists + Conductor com grant específico, mesma raiz/budget, receipt efetivo, sem Detonation.
- [x] Comparar base/A/B/A+B, whiff/impacto tardio, imunidade, ausência de impacto, cap32, LOS, colliders, cancelamento, aquisição e transição; detalhes nos [relatórios](../decisions/GPR-01.3_RELATORIO.md).
- [ ] Revisão física do desenvolvedor da composição01.3. Aprovação técnica é restrita aos testes/evidências registrados.

### Backlog de design — sem autorização de implementação

- Tornado: velocidade, largura, alcance, leitura e precisão de mira; sensação atual rápida/pequena, semelhante a projétil, registrada sem mudar números.
- Cadência do básico e animações direcionais/moonwalk.
- Frequência/qualidade de recompensas, economia de aquisição e coerência de builds.
- Layouts de salas/fases, ritmo de exploração e encontros.
- Modelos/meshes e diversidade visual de inimigos; semelhança atual é preocupação conhecida.
- Identidade, mecânicas e apresentação de elites e bosses.
- Singularity futura: tornado móvel + atração gravitacional + dano limitado + colapso/launch final; consultar imunidade aCC, stagger e ownership de deslocamento.

**STOP após01.3.** Não iniciar01.4/Detonation, Singularity, novo especial, troca modular, redesign de inimigos ou implementação de level design. [01.2c](../decisions/GPR-01.2c_RELATORIO.md) / [01.3](../decisions/GPR-01.3_RELATORIO.md).

## Histórico — GPR-01.2b, 28/10/2026

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

## Histórico — estado GPR-01.2 anterior à revisão

### Registro histórico — GPR-01.2, 28/10/2026

GPR-01.1 aceita tecnicamente pelo desenvolvedor; GPR-C01 continua encerrada. Autorizada somente a primeira Mutation jogável **Punhos Titânicos**. Implementados catálogo Basic da Manopla, onda na raiz nativa com HP confirmado e grants `ProtocolBasicEffects.None`, sobrepeça reversível e testes focados. Implementação concluída com ressalva de regressão:18/18 focados e90/90 EditMode;187/187 na última bateria ampliada.295 casos distintos finais, com ocorrências intermitentes anteriores de hitstop/burn registradas e não declaradas corrigidas. Zero ignorados; não confundir automação com aceite humano. [Relatório da atividade](../decisions/GPR-01.2_RELATORIO.md).

Parâmetros experimentais: caixa frontal de 1,5× o alcance/largura efetivos do soco, altura preservada; 0,60× o maior dano pré-mitigação entre receipts nativos positivos do terminal, coeficiente aplicado uma vez. Sem tuning do básico/Asura/QER, nova ação, recurso, postura ou grant Conductor/Detonation. Nova aquisição usa os tickets/catálogos existentes; nenhuma política de recompensa ampliada.

**STOP após GPR-01.2. GPR-01.3 e etapas seguintes não autorizadas.** Revisão humana da mutação e de seu visual permanece pendente; moonwalk/cadência e swap modular continuam diferidos. As seções anteriores preservadas abaixo são históricas onde divergirem deste estado.



## Histórico da entrega GPR-01.1 — superado pelo estado vigente acima

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



## Histórico C01.3b — fechamento na data,28/10/2026

**CONCLUÍDA TECNICAMENTE:105/105 casos distintos finais, zero ignorados**, via Unity MCP no checkout correto/Editor6000.5.10f1. Aceite humano anterior das últimas correções recebido para WASD, básicos, Q/E/R e Asura/RMB; básicos rápidos ficam como refinamento futuro, sem mudar cadência. [Relatório](../decisions/GPR-C01.3b_RELATORIO.md).

Corrigidas fixtures que criavam dispositivos virtuais antes da política de foco, e o gate de soltura que descartava o primeiro comando novo no frame seguinte. Dois testes falharam0/2 antes da correção de produção;22 básicos/input+23 movimento+7 contextos+30 combate+8 dash+15 EditMode passaram após as correções. Ajustado teste Cadenciada para agir no frame de recovery observado, mantendo asserções/tempos. Outputs e falhas anteriores preservados em c01-3b/correcao e gate; não suíte global. Runtime publicado com input virtual:3 básicos0/1/2 e1 Asura por RMB,4 PNG; não novo teste físico.

F11 deixou de ativar Asura direta por decisão explícita; binding salva/executor/custos/estágios preservados. Frame de soltura continua bloqueado; held não atravessa UI/foco/pausa. Editor final NexusLobby limpa/Edit Mode; permanece erro de pacote Tripo3D preexistente. Causa física do incidente RMB antigo não confirmada retrospectivamente; aceite humano posterior inclui RMB funcional.

**Gate de C01.4 satisfeito;4a–4d já autorizadas, ainda não implementadas.** Não iniciar C01.5/GPR-01. Interact separado precisa respeitar F/finisher; X pode ficar vazio, nunca duplicar Asura. Autorizações e evidências3a abaixo são históricas onde conflitam com este fechamento.

| Tarefa | Dependência | Trabalho mínimo e owner | Aceite/rollback |
|---|---|---|---|
| C01.1a | Autorização explícita recebida | Baseline técnica, virtual/API e capturas atuais; distinguir humano ausente; inspeção três armas | COMPLETED técnico;21Play+15Edit; baseline física pendente, não inventada |
| C01.1b | 1a | Revisar ADR/requisitos/design/tasks para RMB Asura, cliqueLMB, quatro slots e compatibilidade; fechar plano/gates | COMPLETED de elaboração; STOP para decisões pendentes |
| C01.2a | Autorização explícita recebida; Asura/X/Bow-Spear não são bloqueios | CONCLUÍDA E ACEITA para implementar: motor/arena/10 testes isolados, câmera/mundo/dash comparados, Move cru falha e CC avaliado separadamente |59/59 históricos;4 PNG e CSV; autorização posterior levou a2b, sem atestar físico |
| C01.2b | Autorização direta após revisão2a; teclas incluídas por resposta explícita | IMPLEMENTADO: controlador/motor único, fontes reais, animação por deslocamento,13 testes de integração e cutover em4 cenas; facade v2 para liberarW com Q/E/R e especialF11 |66/66 finais incluindo regressões;3PNG Nexus/CSV; físico, crowd e bordas pendentes. Asura/X/Bow-Spear separados; próxima proposta2c |
| C01.2c | Autorização específica recebida após2b | CONCLUÍDA: sliding protegido de locomoção; barreira compartilhada de soltura; UI/reward/results/reload/foco/disable; R contextual; investigação de recovery |83/83 normais; hitch500 negativo reproduzido/causa histórica aberta; relatório e revisão pendente. STOP antes de3 |
| C01.3a | C01.2c aceita; LMB autorizado; RMB acrescentado diretamente; ChargedShot decidido | CONCLUÍDA: básico primário por clique/cursor/whiff; buffer único120ms; hold apenas ChargedShot; RMB Manopla no owner Asura atual, sem básico/custo duplicado; F11 compat/F prioritário |102/102 normais;4 Game View PNG/1372 amostras. X/extração completa não implementados. STOP e revisão antes3b |
| C01.3b | Aceite humano + correções solicitadas | CONCLUÍDA técnica: fixture/foco, primeiro input após soltura, RMB único/F11 removido; cadência e owner preservados |105/105 finais, falhas históricas preservadas; gate liberado para C01.4 autorizada |
| C01.4a | 3b aceita/revisão final | Rebinding no painel existente, ações estáveis, migração/persistência/conflitos/defaults/capture segura |4/4 focados; incluída nos116 cumulativos |
| C01.4b | 4a | DashCursor/Movimento; Movimento sem input=facing; snapshot, clocks e física existentes |1/1 política+8/8 dash; incluída nos116 cumulativos |
| C01.4c | 4b | InteractG independente/finisherF; estações, arsenal e objetos; prioridade/modal/release |2/2 focados; incluída nos116 cumulativos |
| C01.4d | 4c | Q/E/R + especial RMB único, labels dinâmicas, sem X, legadoindex3 preservado | 2/2 kits/HUD e116 cumulativos distintos; STOP após4 |
| C01.5a | Evidência4a–d existente | CONCLUÍDA: revisão dos seis JSON/116 nomes distintos; sem reteste desnecessário | Nenhuma regressão bloqueadora nova demonstrada; relatório5 |
| C01.5b | Aceite físico explícito recebido | CONCLUÍDA: controle/feel/configuração/interação aprovados; moonwalk/cadência diferidos | GPR-C01 COMPLETA; não repetir aceite global |
| 01.P1 | Aceite recebido | ACEITA como planejamento; direção experimental aprovada | Não é autorização de conteúdo |
| 01.1 | Autorização recebida | CONCLUÍDA E ACEITA tecnicamente pelo desenvolvedor; fundação Basic/receipt/linhagem/aquisição | Gate liberado exclusivamente para01.2 |
| 01.2 | Autorização explícita recebida após aceite01.1 | IMPLEMENTADA: Punhos Titânicos/onda/card/visual reversível, grantsNone;18 focados+90 EditMode+187 regressões aprovados nas seleções finais; intermitências anteriores registradas | STOP para revisão; ver GPR-01.2_RELATORIO.md |
| 01.3 | 01.2 | PLANEJADA: Conductor existente compõe com onda; ordem e limites específicos | V10/14; sem energia/mana/raiz nova |
| 01.4 | 01.3 | PLANEJADA: Detonation existente como terceira camada on-kill | V10/14; morte única/cap/posição nova, sem cascata extra do ramo |
| 01.5 | 01.4 | PLANEJADA: tickets/seed real, todas ordens, duas runs/reset, timers e8/32/64 alvos se relevante | V11–15; resultados reais e assistência diferenciados |
| 01.6 | 01.5 | PLANEJADA: relatório técnico e revisão humana somente da nova mutação/sinergia/visual | V16; STOP antes de expansão |

GPR-02: composição/visuais gerais, ecos e expansão além da única sinergia três vias proposta em GPR-01; GPR-03: duas builds e ecossistema modular ampliado; GPR-04: meta; GPR-05: semânticas específicas Bow/Spear. Sem catálogo dos12 exemplos, Mythics, mapa novo ou rework inimigo nas tarefas acima.

Riscos transversais: K01 checkout errado (reconfirmar MCP); K02 resets de cooldown (operação seletiva); K03 input duplicado (um leitor); K04 rebind quebra slots/histórico (IDs estáveis); K05 proc por whiff/recursão (receipt+budget); K06 troca incompatível (preflight/rollback); K07 efeito cobre perigo (hierarquia/prova humana); K08 fixture altera resumo (isolamento e hashes); K09 dívida histórica confundida com regressão (filtro isolado+conjunto); K10 direção aprovada confundida com tuning (D01–14 com aceite explícito).

Exceção autorizada em C01.2b: parcela mínima de C01.4a/c foi antecipada — migração pura, Apply versionado no Settings existente, round-trip e prompts essenciais — para liberar WASD. Não declarar C01.4 concluída: remapeamento dos componentes de movimento, secundários, ação própria Interact, todos os contextos/UI e slots QERX ainda não foram entregues. Primeira regressão legada29/30 foi arquivada; caso isolado e grupo final passaram sem alterar tolerância, com causa de timing/ordem pendente.

## Compromissos futuros preservados

| ID | Fase/status | Gate e aceite |
|---|---|---|
| RF-TROCA / 03.1-MOD | GPR-03.1, NÃO IMPLEMENTADO/NÃO VALIDADO | Adquirir/substituir skill em Q/E/R por ticket real, preflight de investimentos/capabilities, commit seletivo de timers/custos e rollback. Preservar especialRMB; requer aprovação própria |
| 02.2-INT | GPR-02, NÃO INICIADA | Propor e provar uma interação mecânica entre skill e básico; usarQ para aproximar não satisfaz esse critério. Requer desenho/aprovação específicos |
