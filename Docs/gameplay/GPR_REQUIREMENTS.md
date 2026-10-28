# Gameplay Rework — requisitos da migração e da primeira fatia

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

## Entrega e exceções vigentes — C01.3a

GPR-R03/04/08: Primary edge → direção do cursor no plano do player → pedido individual, inclusive whiff; sem enemy assist/target lock/click-to-move. Buffer compartilhado existente de120ms/single slot, direção capturada e clocks preservados. Manopla/Spear e Bow sem ChargedShot não repetem por hold. Com ChargedShot, hold legado foi preservado por resposta explícita do desenvolvedor; release não cria ataque.

Ampliação autorizada: RMB/canal secundário da Manopla ativa/continua a mesma AsuraBurst histórica, sem básico concorrente ou custo duplicado. F11 permanece compatibilidade; F prioritário; UI/foco/pausa pedem soltura. Fonte/estado index3R e prefs preservados; GetAbilityKey apresenta o secundário. GPR-R05/07 completos continuam pendentes: não há X novo, extração modular concluída ou Settings completo. Arco/Lança mantêm secundário legado até design próprio. Feedback físico de animação e opção de dash permanece aberto.

## Critérios verificados nesta revisão

- V01/02/08: sliding tangencial somente de locomoção, parede/quina/borda/rampa em fixtures reais, sem aumentar diagonal ou mudar speed/dash; mesmo motor e writer.
- V07: gate de entrada cobre pausa, recompensa, resultados, reload, foco, disable e teclas mantidas; requer soltura e frame posterior. Cast já aceito mantém impacto/cooldown.
- R contextual continua reservado; Interact separado sem tecla definida; F11/Asura e Q/E/R preservados.
- Incidente recovery: execução normal passa, expiração sob hitch500 reproduzida; causa histórica e política de TTL sob hitch continuam abertas. Não flexibilizar assert/tolerância para fechar o gate.
- Na revisão histórica abaixo o aceite físico ainda era pendente. C01.5 encerrou essa pendência para controles atuais; animação direcional continua refinamento futuro não bloqueador.

| ID | Requisito e condição observável | Visão/decisão | Fase/tarefa | Validação |
|---|---|---|---|---|
| GPR-R01 | Nenhum runtime/asset/scene/save alterado em GPR-00; hashes e status preservados | Pedido/escopo | 00.1–00.5 | V00 integridade |
| GPR-R02 | WASD direto relativo à câmera, diagonal normalizada, motor protegido único; sem destino de clique/perseguição | IMPLEMENTADO / C01 humano aceito | C01.2/5 concluídas | V01, filtros de movimento existentes; cobertura extrema delimitada |
| GPR-R03 | Mira independente do deslocamento; swing mantém compromisso de direção; contato/cadência preservados | IMPLEMENTADO / C01 humano aceito | C01.2/3/5 concluídas | V02, input/combate existentes + aceite físico |
| GPR-R04 | LMB por clique avança um básico; RMB só especial. Hold ChargedShot do arco é exceção preservada por decisão explícita | IMPLEMENTADO / C01 humano aceito | C01.3/5 concluídas | V03 tap/hold/whiff/combo e custo único |
| GPR-R05 | Três slots modulares Q/E/R + especial permanente RMB separado. Sem X/quarta modular. Índices/arrays legados preservados; Asura única em index3 | Revisão finalC01.4/D15 superada | C01.4d | V04 três skills/especial/sem aliasF11/X |
| GPR-R06 | InteractG dedicado/configurável, finisherF distinto, contexto/UI priorizados; sem cast ao interagir | RevisãoC01.4 | C01.4c | V05 estações/objetos/close/release |
| GPR-R07 | Settings remapeia movimento/ações no painel existente; teclado/Mouse0–4, conflitos, draft/Apply/Cancel/Defaults e persistência versionada | IMPLEMENTADO; humano aceitou configuração experimentada | C01.4/5 concluídas | V06 contratos/reload; restartStandalone não certificado |
| GPR-R08 | Pausa/reward/resultados/rebind/foco perdido bloqueiam mundo; soltura evita replay; menu permanece utilizável | RF14; D14 | C01.2–5 | V07 teclas presas/fechamento/click-through |
| GPR-R09 | Motor/clocks de hitstop/cast/dash/pull/CC preservados; dashCursor ou Movimento; sem vetorMovimento usa facing | IMPLEMENTADO / C01 humano aceito | C01.2/4/5 concluídas | V08 contratos/cancel/dash existentes; sem retuning |
| GPR-R10 | A transforma apenas terminal LMB com HP confirmado; uma onda frontal em novas vítimas, sem tuning do básico | Proposta v1, parâmetros pendentes | 01.2 | V09 / contratoA da fatia |
| GPR-R11 | A+B=onda+Conductor; A+B+C=Detonation na morte confirmada. Política limitada à nova linhagem, todas as ordens equivalentes | Pedido atual de três vias; conteúdo proposto | 01.3/4 | V10 / ramificação/caps/ordens |
| GPR-R12 | Basic é alvo de modificação, não quarta modular; preservar IDs/grafo/limites, aquisição não reseta timers de skills/especial | IMPLEMENTADO na fundação; swap não validado | 01.1 concluída /01.5 futura | V11 / restore antigo-novo, no-reset, rollback |
| GPR-R13 | Attachment visual da Manopla + onda/assinaturas existentes; base/A/A+B/A+B+C/reset legíveis e reversíveis | Manifestação mínima proposta | 01.2/5 | V12 / câmera real, cleanup e humano do delta |
| GPR-R14 | Tickets Mutation/Protocol atuais, A conta como Mutation; rota/seed legal, sem drop/ordinal extra; Q nativo integra aproximação | Sem swap obrigatório nesta proposta | 01.5 | V13 / seed, tickets e duas runs |
| GPR-R15 | Total/subcategorias reconciliam HP; bindings não renomeiam IDs históricos; fixtures protegem RunSummary | Preservar owners atuais | C01.4 entregue +01.5 planejada | V14 receipts/causalidade/duas runs |
| GPR-R16 | Regressões afetadas incluem três armas, recursos/cancel/reação e cleanup; não declarar todo alvo/catálogo certificado | C01 tem filtros entregues; delta01 exige provas próprias | C01.5 concluída +01.1–5 planejadas | V15 seleção por owner/risco e limitações explícitas |
| GPR-R17 | C01: aceite humano recebido. Nova mutação/sinergia/visual: aceite próprio após implementação autorizada | Separar humano de automação | C01.5 completa / 01.6 futura | V16 / nenhum aceite futuro inferido |

RF04 (três modificadores) entra na fatia proposta GPR-01 pelo pedido atual; RF06 (camadas cumulativas completas) e awakening continuam em GPR-02; RF18 (duas builds deliberadas): GPR-03; meta lateral: GPR-04; expansão de identidade Bow/Spear: GPR-05. Não desaparecem do produto por estarem fora da primeira entrega.

Não funcionais: desempenho medido no hardware/cena real antes de fixar meta; nenhuma mudança nos GUIDs/shared assets; sem alteração em terceiros ou schema de moeda/unlocks/RunSummary; sem managers/event buses/damage pipelines paralelos. Tratamento de NaN/Infinity/ausência de câmera/dispositivo deve falhar sem mover/atacar em direção inválida. Manter a execução Asura existente: consumo100/mana/cooldown/estágios/continuações/F/cancel/pose/HP/cleanup, nunca clone de executor paraRMB.

## Delta autorizado e rastreabilidade01.1

GPR-R10/11/13: conteúdo/forma/visual ainda NÃO IMPLEMENTADOS; direção experimental aceita, números iniciais não são balanceamento final. GPR-R12: contratosBasic e aquisição sem reset implementados e testados; isso não valida troca de skill. GPR-R14: infraestrutura/tickets existentes regredidos; oportunidade real de A/B/C não provada porque A não está no catálogo. Q/Avanço é compatibilidade de ações, não nova sinergia mecânica.

| ID | Requisito preservado | Fase/status | Aceite futuro |
|---|---|---|---|
| GPR-R18 / RF-TROCA | Adquirir/substituir habilidades modulares de Q/E/R com compatibilidade, investimentos, timers/recursos e rollback; RMB especial preservado | GPR-03.1, NÃO IMPLEMENTADO/NÃO VALIDADO; adiado nesta primeira fatia | Ticket/seed legal e troca seletiva real, UIantes/depois/cancel e regressões |
| GPR-R19 | Interação mecânica skill↔básico precisa mudar decisão/regra e ter comparador; aproximaçãoQ isoladamente não basta | Hipótese futura GPR-02; não iniciada nem autorizada | Proposta própria e efeito causal observado |
