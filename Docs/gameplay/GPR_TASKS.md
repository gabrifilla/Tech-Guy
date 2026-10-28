# Gameplay Rework — tarefas propostas e gates

## Estado vigente — GPR-01.1,28/10/2026

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
| 01.1 | Autorização exclusiva recebida | CONCLUÍDA TECNICAMENTE: Basic/receipt/linhagem opt-in/aquisição;210 casos distintos selecionados, relatório técnico | STOP para revisão antes de01.2 |
| 01.2 | Nova aprovação explícita após revisão01.1 | NÃO INICIADA: A/onda/card/visual mínimo, permissões derivadasNone; nenhum B/C novo | Escopo exato em GPR_01_VERTICAL_SLICE.md |
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
