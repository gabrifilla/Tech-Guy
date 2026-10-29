# Gameplay Rework — progresso mestre

## Estado vigente — GPR-01.2b, 28/10/2026

O desenvolvedor testou fisicamente a onda instantânea anterior: funcionalidade técnica aceita, resultado de gameplay considerado pouco transformador e rejeitado como design final. A revisão autorizada substitui essa onda por um **tornado de vento móvel**, mantendo o ID da Mutation, aquisição, controles e combate nativo. A evidência de GPR-01.2 abaixo permanece histórica.

Cada terminal realmente executado no contato do scheduler, com A no snapshot aceito, autoriza no máximo um tornado **inclusive em whiff**. Pedido/cancelamento pré-contato e estágios intermediários não emitem. Contrato específico de emissão separado de `ReserveConfirmedBasicChild`, cujo requisito de HP confirmado permanece intacto. Emissão não produz HP/estatística/recurso nem nova raiz; impactos posteriores usam a fila e os receipts existentes, mesma raiz/parent e caps. Política do ramo: `ProtocolBasicEffects.None`.

Definição experimental única: deslocamento de 5,5 m a 3,5 m/s, vida máxima escalada de 2 s, raio 0,65 m, altura 1,8 m e offset inicial 0,75 m. Direção comprometida não acompanha mouse/rotação posterior. Dano = 0,60 × `CaptureAttackDamage` não crítico capturado na aceitação do terminal (dano authored + stats/Momentum elegíveis naquela aceitação); não depende de receipt, não sorteia crítico nem reaplica multiplicadores do atacante. Mitigação/HP seguem o alvo. Uma vítima por handle+generation, mantendo a exclusão das vítimas nativas já confirmadas.

Overlap inicial + capsule sweep por passo; sólidos e LOS bloqueiam. Reservas negadas/saturação encerram com segurança. Pausa/hitstop congelam; troca de arma/contexto, fim da run, disable/morte e cena invalidam/limpam reservas e apresentação. VFX reversível, pool limitado e leases por geração, sem Rigidbody/segundo executor/alteração permanente de prefab.

Implementação e validação desta revisão registradas no [relatório GPR-01.2b](GPR-01.2b_RELATORIO.md). **Aceite físico do tornado novo pendente. STOP antes de GPR-01.3**: somente recomendar revisão da composição com Conductor após avaliação do desenvolvedor; nenhum grant novo de Conductor/Detonation, swap modular, tuning nativo ou quarta skill implementado.

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

GPR-01.1 aceita tecnicamente pelo desenvolvedor; GPR-C01 continua encerrada. Autorizada somente a primeira Mutation jogável **Punhos Titânicos**. Implementados catálogo Basic da Manopla, onda na raiz nativa com HP confirmado e grants `ProtocolBasicEffects.None`, sobrepeça reversível e testes focados. Implementação concluída com ressalva de regressão:18/18 focados e90/90 EditMode;187/187 na última bateria ampliada.295 casos distintos finais, com ocorrências intermitentes anteriores de hitstop/burn registradas e não declaradas corrigidas. Zero ignorados; não confundir automação com aceite humano. [Relatório da atividade](GPR-01.2_RELATORIO.md).

Parâmetros experimentais: caixa frontal de 1,5× o alcance/largura efetivos do soco, altura preservada; 0,60× o maior dano pré-mitigação entre receipts nativos positivos do terminal, coeficiente aplicado uma vez. Sem tuning do básico/Asura/QER, nova ação, recurso, postura ou grant Conductor/Detonation. Nova aquisição usa os tickets/catálogos existentes; nenhuma política de recompensa ampliada.

**STOP após GPR-01.2. GPR-01.3 e etapas seguintes não autorizadas.** Revisão humana da mutação e de seu visual permanece pendente; moonwalk/cadência e swap modular continuam diferidos. As seções anteriores preservadas abaixo são históricas onde divergirem deste estado.



## Histórico da entrega GPR-01.1 — superado pelo estado vigente acima

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](../gameplay/GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Ledger atual

| Atividade | Estado | Evidência/entrega | Próximo gate |
|---|---|---|---|
| GPR-C01.5a | CONCLUÍDA | Revisão dos116 casos finais existentes; nenhum reteste desnecessário | Sem nova regressão bloqueadora demonstrada |
| GPR-C01.5b / GPR-C01 | CONCLUÍDA / HUMANO ACEITO | Declaração física explícita do desenvolvedor; [relatório5](GPR-C01.5_RELATORIO.md), [resumo](GPR-C01_PHASE_SUMMARY.md) | Polimento diferido não bloqueia |
| GPR-01.P1 | ACEITA como planejamento | Direção experimental A/B/C e adiamento do swap aceitos pelo desenvolvedor | Gate01.1 satisfeito; somente01.2 executada |
| GPR-01.1 | CONCLUÍDA E ACEITA TECNICAMENTE | Declaração direta do desenvolvedor;210 casos históricos delimitados | Autorizada somente01.2 |
| GPR-01.2 | TÉCNICA ACEITA / GAMEPLAY REJEITADO COMO FINAL | Punhos Titânicos, aquisição por ticket, geometria/HP/caps e quatro capturas; [relatório](GPR-01.2_RELATORIO.md) | STOP e revisão humana antes de01.3 |
| GPR-01.2b | CONCLUÍDA TECNICAMENTE / HUMANO PENDENTE | Tornado móvel/whiff;305 casos distintos,6 Game View; [relatório](GPR-01.2b_RELATORIO.md) | Revisão física antes de autorizar01.3 |
| GPR-01.3–01.6 | NÃO INICIADAS/NÃO AUTORIZADAS | Sem nova composição Conductor/Detonation | Cada próxima etapa depende de aprovação posterior |



## Histórico do fechamento C01.4,28/10/2026

**CONCLUÍDA TECNICAMENTE; aguardando playtest humano.** Três skillsQ/E/R + especialRMB, semX/aliasF11; InteractG e finisherF, pausa remapeável/Escape seguro. DashCursor padrão ou Movimento com fallbackolhar por decisão direta.116 casos distintos aprovados em filtros agregados (115 na rodada completa +9UI posterior com1 novo), zero ignorados; não suíte global. Snapshots automatizados iguais e mudanças posteriores do perfil preservadas. Editor finalNexusLobby limpa/Edit Mode; Tripo3D preexistente pendente. [Consolidado](GPR-C01.4_RELATORIO.md). Não iniciar C01.5/GPR-01 sem autorização.


## Histórico C01.4 — layout revisado, execução sequencial

C01.3b aceita tecnicamente pelo desenvolvedor. Decisão mais recente substitui quatro slots Q/E/R/X por **três Q/E/R + especial separado RMB**, preservando índices/assets/executores.4a concluída tecnicamente4/4 focados, relatório individual e captura real; cumulativa pendente.4b concluída:1/1 política/persistência e8/8 dash; Movimento sem input usa facing por decisão direta mais recente.4c concluída2/2: InteractG separado de skills/finisherF; contexto e objetos/remap validados.4d concluída tecnicamente: Q/E/R+especial separado, semX;116 casos distintos finais agregados, capturas1280x720, relatórios4a/b/c/d e consolidado. Preservadas falhas intermediárias; sem suíte global/aceite físico novo. STOP após4,5/GPR-01 exigem aprovação. Sem C01.5/GPR-01.


## Histórico C01.3b — fechamento na data,28/10/2026

**CONCLUÍDA TECNICAMENTE:105/105 casos distintos finais, zero ignorados**, via Unity MCP no checkout correto/Editor6000.5.10f1. Aceite humano anterior das últimas correções recebido para WASD, básicos, Q/E/R e Asura/RMB; básicos rápidos ficam como refinamento futuro, sem mudar cadência. [Relatório](GPR-C01.3b_RELATORIO.md).

Corrigidas fixtures que criavam dispositivos virtuais antes da política de foco, e o gate de soltura que descartava o primeiro comando novo no frame seguinte. Dois testes falharam0/2 antes da correção de produção;22 básicos/input+23 movimento+7 contextos+30 combate+8 dash+15 EditMode passaram após as correções. Ajustado teste Cadenciada para agir no frame de recovery observado, mantendo asserções/tempos. Outputs e falhas anteriores preservados em c01-3b/correcao e gate; não suíte global. Runtime publicado com input virtual:3 básicos0/1/2 e1 Asura por RMB,4 PNG; não novo teste físico.

F11 deixou de ativar Asura direta por decisão explícita; binding salva/executor/custos/estágios preservados. Frame de soltura continua bloqueado; held não atravessa UI/foco/pausa. Editor final NexusLobby limpa/Edit Mode; permanece erro de pacote Tripo3D preexistente. Causa física do incidente RMB antigo não confirmada retrospectivamente; aceite humano posterior inclui RMB funcional.

**Gate de C01.4 satisfeito;4a–4d já autorizadas, ainda não implementadas.** Não iniciar C01.5/GPR-01. Interact separado precisa respeitar F/finisher; X pode ficar vazio, nunca duplicar Asura. Autorizações e evidências3a abaixo são históricas onde conflitam com este fechamento.

## Histórico GPR-C01.3a — fechamento na data

| Atividade | Status | Evidência | Próximo gate |
|---|---|---|---|
| C01.2c | ACEITA tecnicamente | Revisão explícita recebida; não aceite físico final | Autorização C01.3a recebida |
| C01.3a + RMB Asura autorizado depois | CONCLUÍDA TÉCNICA / AGUARDANDO REVISÃO |19 novos+23 movimento+7 contextos+30 combate+8 dash+15 EditMode=102/102;4 PNG e1372 amostras do player real com input virtual | Revisão e aprovação específica antes de C01.3b |

LMB individual sem target/aim assist, geometria e impacto existentes, shared buffer120ms. Hold primário só com ChargedShot por decisão expressa. RMB da Manopla aciona a única Asura legada, sem básico concorrente/custo duplo; F11 compat/F prioritário. Não há skillX nem extração modular/Settings completo. Feedback de locomotion e dash configurável continua aberto.1795 hashes:1789 iguais/6 intencionais/0 ausências;2 GUIDs novos únicos; cenas/assets anteriores preservados. Snapshots persistentes consultados iguais; NexusBlockout limpa/Edit Mode. Commit/push integral das alterações acumuladas e desligamento posterior solicitados separadamente pelo usuário. Relatório registra limites de validação do recorte, sem aprovar todo o diff acumulado.

## GPR-C01.2c — histórico aceito tecnicamente

| Atividade | Status | Resultado/evidência | Próximo gate |
|---|---|---|---|
| GPR-C01.2c | CONCLUÍDA TÉCNICA / AGUARDANDO REVISÃO |23 integração+7 contextos+30 GI/legado/Bow/Spear+8 dash+15 EditMode=83/83 normais;7 capturas Game View,757 amostras; sliding de locomoção e soltura compartilhada | Revisão/feel físico antes de C01.3 |

Motor único preservado, sem tuning de speed/dash ou alteração de cenas/assets/GUIDs. W livre4,999984m/s; diagonal na borda3,535536m/s; pausa/retomada mantida sem movimento.1795 hashes:1783 iguais/12 mudanças intencionais/0 ausências. Prefs consultadas/coins/summary iguais; NexusBlockout limpa/Edit Mode. Hitch150 passou; hitch500 reproduziu expiração0,35s e falha da premissa do teste original, sem alterar assert/tolerância. Isso não prova a causa histórica nem encerra o incidente como corrigido. Checker genérico C:/D: falhou; evidência focada e limites no relatório. Interact sem tecla definida, Q/E/R/F11 preservados; dash configurável e LMB sem alvo registrados para etapa futura.

## GPR-C01.2b — fechamento histórico aceito tecnicamente

| Atividade | Status | Resultado/evidência | Próximo passo |
|---|---|---|---|
| C01.2a | ACEITA para implementar; não atesta físico | Aprovação direta do desenvolvedor após relatório/protótipo | C01.2b autorizada |
| C01.2b + teclas necessárias | IMPLEMENTADO / TÉCNICO EXECUTADO / FÍSICO PENDENTE |13 integração+30 GI/legado/Bow/Spear+8 dash+15 EditMode=66/66 finais;3 Game View PNG e461 amostras na Nexus;4 cenas só receberam flag de movimento; perfilQ/E/R e especialF11, legado preservado | Teste físico e proposta C01.2c; não iniciar AsuraRMB/X ou GPR-01 |

Motor comum atende teclado/cast/dash/pull/recoil/Kiting Step; pull interrompe dash, animação usa deslocamento real. Pipeline de dano/IDs/balanceamento preservado. Primeiro grupo legado29/30 falhou buffer; isolado e grupo final passaram sem alterar código/tolerância, causa não confirmada registrada. Na Nexus, livre≈5m/s e soltura0; diagonal contra borda parou.1767 de1789 hashes protegidos iguais,22 mudanças intencionais e0 ausências. Dados persistentes consultados iguais; Editor NexusBlockout limpa/Edit Mode. Ver relatório, manifesto e evidência c01-2b.

## GPR-C01.2a — fechamento histórico antes da autorização C01.2b

| Atividade | Status | Arquivos/evidência | Próximo gate |
|---|---|---|---|
| GPR-C01.1a / C01.1b | ACEITAS pelo desenvolvedor no escopo documentado | Baseline/readiness/ADR recebidos; não significa controles implementados/aceitos | Autorização específica C01.2a recebida |
| GPR-C01.2a | CONCLUÍDA como experimento; AGUARDANDO REVISÃO | Assembly exclusiva de testes, arena temporária,10 novos PlayMode+26 regressões+8 dash+15 EditMode=59/59 finais;4 Game View PNG,CSV,integridade1778 | [Relatório obrigatório](GPR-C01.2a_RELATORIO.md); aprovar/rejeitar candidato antes de C01.2b |

Rejeitar Agent.Move cru por travessia de collider fora do bake. Variante com sweep/raycast é candidata técnica, não integrada. CharacterController testado separadamente: bloqueia parede, mas não respeita limite NavMesh sozinho. Físico/feel, mapa real, crowd e rig pendentes. Extração de Asura/X/Bow-Spear não bloquearam o experimento. Editor restaurado NexusBlockout limpa/Edit Mode; prefs/summary/bindings preservados; sem player/cena/prefab/combate de produção alterado. Evidência em Docs/validation/artifacts/gameplay-rework/c01-2a.

## Histórico de autorização e execução anterior (superado apenas quanto ao gate)

| Atividade | Título | Status | Atualização | Arquivos | Testes/evidência | Relatório | Aprovação/próximo gate |
|---|---|---|---|---|---|---|---|
| GPR-00.1 | Inventário/baseline | COMPLETED | 26/10/2026 | relatório, ledger, matriz, evidências gpr00 | MCP/15 EditMode/hash/visão integral fornecida durante00.2 | [GPR-00.1](GPR-00.1_RELATORIO.md) | Bloqueio da visão resolvido |
| GPR-00.2 | Ownership/lacunas | COMPLETED | 26/10/2026 | mapa, relatório, ledger, matriz | código atual e caminhos de HP/tickets/clones | [GPR-00.2](GPR-00.2_RELATORIO.md) | GPR-00.3: decisões propostas |
| GPR-00.3 | Decisões/fatia | COMPLETED | 26/10/2026 | ADR0006, relatório, ledger, matriz | visão integral e código; 4/4 PlayMode | [GPR-00.3](GPR-00.3_RELATORIO.md) | D01–D14 aguardam revisão |
| GPR-00.4 | Requisitos/design/tarefas/QA | COMPLETED | 26/10/2026 | quatro documentos GPR, relatório, ledger, matriz | GPR-R01–17 / V00–16 / tarefas | [GPR-00.4](GPR-00.4_RELATORIO.md) | 00.5 fecha controles |
| GPR-00.5 | Migração WASD/mouse/QERX | COMPLETED | 26/10/2026 | migração, relatório, ADR/pacote, ledger, matriz | campos MCP e fontes de controles | [GPR-00.5](GPR-00.5_RELATORIO.md) | STOP / aprovação antes de C01.1a |

Fechamento históricoGPR-00: cinco atividades documentadas, então AWAITING_HUMAN_APPROVAL. Aprovação documental posterior recebida; não equivale a aceite de implementação/feel ou parâmetrosGPR-01. Baseline virtual/visual executada emC01.1a abaixo; física continua pendente. [Consolidado históricoGPR-00](GPR-00_PHASE_SUMMARY.md).

Nenhum resultado técnico equivale a aceite humano. GPR-C01 precede GPR-01; os parágrafos seguintes registram a autorização existente na data de C01.1, substituída pelo fechamento atual acima.

## Aprovação recebida e baseline C01.1 —26/10/2026

GPR-00 aprovada documentalmente pelo desenvolvedor com ajustes de controle; somente C01.1a/b autorizadas. CliqueLMB=um pedido, RMB=especial da arma/Asura; propostas anteriores de RMB terminal/AsuraX ficam superadas. Não há aprovação de C01.2–5 ou GPR-01.

| Atividade | Status | Arquivos/evidência | Relatório | Próximo gate |
|---|---|---|---|---|
| GPR-C01.1a | COMPLETED técnico/virtual | Baseline,4 diagnósticos fora de Assets,36 testes,8PNG inspecionados,integridade/prefs | [C01.1a](GPR-C01.1a_RELATORIO.md) | C01.1b autorizada; humano/motor futuro pendentes |
| GPR-C01.1b | AWAITING_APPROVAL (design concluído) | ADR/pacote revisados;4/4 Bow/Spear;sem input/motor novo | [C01.1b](GPR-C01.1b_RELATORIO.md) | STOP; C01.2a isolada exige nova autorização |
