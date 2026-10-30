# Gameplay Rework — progresso mestre

## RUN-EXP-01 — entrega das etapas0–2,30/10/2026

**Escopo autorizado entregue para revisão; STOP após2.** Fixes localizados de pull/fogo, auditoria/especificação e cinco estudos geométricos com dois layouts isolados. Não foram publicados inimigos/salas/economia novos. [Plano integrado](../gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md) · [Etapa0](RUN_STAGE_0_CRITICAL_FIXES.md) · [Etapa1](RUN_STAGE_1_AUDIT_AND_DESIGN.md) · [Etapa2](RUN_STAGE_2_PROTOTYPES.md).

10/10 PlayMode finais (`58493f83f2144543bdc90e15ebb9a22a`),37/37 EditMode (`07f6c50e43d8432c8009e25f4d44a4a6`),1/1 protótipo (`e191ce3934834e69ba91e45d49a13dbc`). Regressão ampliada continua322/353; baseline dos31 e reexecuções no relatório0, sem somar seletivamente resultados. Aceite físico de fogo/tripla e dos designs permanece pendente. Propostas futuras, desempenho de64 agentes reais e arte final não estão completos. Assistência A+B encontrada ligada no Editor e preservada; desligar explicitamente para avaliar economia natural.

Automação de retomada excluída após disparar. Commit/push conjunto de todas as alterações preexistentes e desta frente autorizado pelo usuário; desligamento somente após push confirmado e Editor fora de Play. O checkpoint abaixo é histórico.

## Frente independente RUN-EXP-01 — checkpoint de 29/10/2026

Pedido direto abriu três etapas sequenciais para experiência de incursão, sem renumerar fases GPR. **Etapa0 em andamento; Etapas1/2 ainda não iniciadas. Pausa de cota solicitada, retomada agendada para30/10 às01h45 Brasília.** Pull/HookLine agora não reinicia um pull ativo e permite0,6s contra reaplicação após liberação; fogo ganhou silhueta geométrica animada, sem tuning mecânico. Nove testes novos aprovados; quatro comparadores publicados aprovados em cada rodada visual. Regressão ampliada322/353,31 falhas abertas — não declarar regressão verde ou GPR01 encerrada. Aceite humano do fogo/tripla permanece pendente.

[Relatório em execução](RUN_STAGE_0_CRITICAL_FIXES.md), [checkpoint de retomada](RUN_RESUME_CHECKPOINT.md) e [resultado353 casos](../validation/artifacts/run-exp-01/stage0/regression-243f6a6d.json). Restam diagnóstico das falhas, captura do Hooker na cena publicada/custos, auditoria e protótipos isolados. STOP após2; sem autorização para publicar novo sistema de salas, economia, modelos ou bosses. Commit/push de todo o projeto e desligamento foram autorizados para **depois de concluir tudo**.

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

[Relatório consolidado e evidências](GPR-01.5_RELATORIO.md). Relatórios01.5a/b/c/d preservam comandos, jobs, falhas e limites.

## Histórico anterior — não sobrepõe GPR-01.5


## Estado vigente — GPR-01.4, 29/10/2026

O pedido direto aceita fisicamente01.2c/01.3 e autoriza sequencialmente01.4a–e, incluindo transformação visual obrigatória. **GPR-01.4 implementada, com validação integrada e ressalva legada; STOP após01.4.** GPR-01.5, Singularity, novos especiais, troca modular, tuning, inimigos e salas não foram executados. Todo estado anterior abaixo é histórico onde divergir deste registro.

A=mutation_gauntlet_titan_fists; B=conductor; C=detonation, mesmos IDs/catálogos/tickets. Base/B/C/B+C mantêm execução nativa e não criam tornado. A viaja como vento; A+B acrescenta identidade elétrica e uma oportunidade Conductor; A+C acrescenta fogo e uma oportunidade Detonation em morte confirmada; A+B+C compõe vento, eletricidade e fogo, permitindo morte pelo Conductor derivado autorizar Detonation. Fogo visual não aplica burn/DoT.

Raiz e budget32 compartilhados; uma emissão Conductor e uma Detonation por tornado. Sem Conductor→Conductor, Detonation→Detonation/Conductor ou tornado→tornado no ramo novo. Native B+C preservado, inclusive suas cascatas e dedupe: não aplicar a whitelist nova globalmente. Soco e vento podem conduzir separadamente quando existem alvos elegíveis e capacidade.

Ownership visual no GauntletExecutionSnapshot aceito; ranks mecânicos na raiz aceita. A reserva intersecta ownership/snapshot e inventário, e o visual recebe essa mesma política. Aquisição durante antecipação/voo não altera projétil anterior. Cosmetic Completed conserva identidade aceita, sem root, HP, arco externo ou explosão. Pool/leases limpam fogo e eletricidade em release/clear/swap/disable/run/cena/contexto. Geometria, velocidade3,5m/s, alcance5,5m, TTL2s, raio0,65m e dano0,60× preservados.

Validação real Unity6000.5.10f1/MCP no checkoutD:/Dev/Personal/My_games/Tech-Guy:98/98 focados,12/12 visual/lifecycle,7/7 integração e90/90 EditMode. Regressão ampliada199/203; três falhas de diretório corrigidas na preparação e4/4 no diagnóstico isolado. Uma ocorrência de Asura cadenciada permanece sem causa confirmada; diagnóstico verde não é correção ou bateria ampliada inteiramente verde.368 nomes distintos com resultado aprovado observado (278 PlayMode+90 EditMode), sem ignorados nas seleções finais; não suíte global nem novo aceite humano.

Tickets reais A+B seed0 e A+C seed1; quatro ordens triplas assistidas explicitamente. Rota normal exercitada oferece somente uma oportunidade Protocol: não foi ampliada para fabricar aquisição tripla natural. Capturas Game View, receipts separados, desempenho controlado8/32/64 e todas as tentativas no consolidado e artefatosGPR-01.4. Playtest físico de leitura/feel desta revisão continua pendente.

[Consolidado](GPR-01.4_RELATORIO.md) · relatórios separados01.4a/b/c/d/e e evidências novas vinculadas nele.

## Histórico anterior — não sobrepõe o estado GPR-01.4


Apoio ao playtest29/10/2026: modo temporário autorizado pelo desenvolvedor oferece Mutação + Protocolo por sala Combat/Elite da Manopla, garantindo A+B na primeira sala. Menu Tools > Tech Guy > Testar Tornado + Conductor; ligado nesta sessão do Editor para novas incursões. 7/7 testes focados aprovados, job266ffdae522c4ec08129075f01e33776. Sem tuning ou avanço a01.4. Detalhes no canônico de boons; não é alteração permanente da economia.

Fechamento técnico29/10/2026: **329 casos distintos aprovados nas seleções (239 PlayMode +90 EditMode)**; zero ignorados, sem certificação da suíte global. Inclui196/196 regressões e correção de preparação do caso de cancelamento, com falhas intermediárias preservadas. Evidência e limites no relatórioGPR-01.3. Revisão física da composição pendente; STOP após01.3.

## Estado vigente — GPR-01.2c /01.3, 29/10/2026

GPR-01.2b aceita fisicamente pelo desenvolvedor: tornado funcional e divertido; velocidade/tamanho/mira e conteúdo futuro diferidos.01.2c executada primeiro e validada32/32 antes de iniciar01.3. Diagnóstico confirmado: scheduler aceita terminais em Traversal após clear, mas ProtocolRuntime não autoriza raiz fora de Encounter; agora há vento cosmético sem combate.

01.3 implementa somente a composição Titanic Fists + Conductor. Um evento elétrico por tornado/raiz a partir do primeiro dano efetivo, com orçamento32, linha de visão, membership, dedupe/geração e cancelamento existentes. None default, sem Detonation ou recursos extras. Aquisição por tickets reais na rota de fixture seed0/nós5–6; reverse order mecânica assistida, distinguida de recompensa natural.

Resultados finais e ressalvas são os dos [relatórios01.2c](GPR-01.2c_RELATORIO.md) e [01.3](GPR-01.3_RELATORIO.md). Automação/Game View não representam novo playtest físico. Backlog de design em [tasks](../gameplay/GPR_TASKS.md). **STOP após01.3**; revisão do desenvolvedor antes da próxima etapa. Histórico abaixo não representa autorização atual.

## Histórico — GPR-01.2b, 28/10/2026

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
