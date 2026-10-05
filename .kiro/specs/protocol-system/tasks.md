# Implementation Plan: Sistema de Protocolos (Fase 5)

## Overview

Este plano deriva [requirements.md](requirements.md), [design.md](design.md) e o parecer de requirements da Fase 5. Ordem: auditoria/baseline → catálogo/inventário → tickets/autoria → contexto/orçamento → produtores → status/efeitos → lifecycle/UI → integração → performance → gameplay/handoff.

**Status: execução iniciada; tasks 1–12 concluídas; task 13 em implementação/validação, 14–16 pendentes.** Evidências incrementais em `contract-audit.md` e `validation.md`. Fases 2–3 aceitas; a aprovação da apresentação/avanço da Fase 4 não substitui seu registro separado de duas runs humanas.

Preservar os sete Protocolos e decisões D1–D8. A expansão para quatro aquisições normais/cinco Elite altera explicitamente recompensas, sem alterar topologia, Techniques existentes, Repair, HP persistente ou Mutation obrigatória. Caps, fórmulas e metas são valores iniciais do design, sujeitos a evidência; não reduzir critérios silenciosamente para aprovar um resultado.

P1–P12 referem-se às Correctness Properties do design; `R2.3`, por exemplo, ao critério 3 do Requirement 2. Nenhum critério listado é opcional. Testar comportamento/contratos; helpers não substituem pipeline real, aquisição publicada, performance ou gameplay.

**Validação incremental:** conferir `git status` antes de editar e preservar alterações existentes. No início do trabalho Unity, verificar MCP, instância/projeto, capacidades e Console sem limpar. Após C#/assets afetados, importar, aguardar compilação e inspecionar Console. Executar pelo MCP somente testes relevantes ao incremento e suas dependências; ampliar por falha ou alteração adicional justificada. Verificar referências, Inspector/save/reload e runtime quando pertinentes, encerrando Play Mode e preservando cena. MCP indisponível deixa validação Unity pendente, com tentativa e limitação registradas. Marcar cada task somente após implementação e checks exigidos; não usar evidência histórica como resultado atual.

Código/assets novos seguem AGENTS.md; testes em `Assets/_Project/Scripts/Tests/EditMode/Editor` e `Tests/PlayMode`. Não alterar terceiros, GUIDs, loaders Resources, roadmap de armas/inimigos ou contratos fora do escopo. Registrar decisões impactantes em `contract-audit.md` e evidências incrementais em `validation.md`, sem relatório adicional por correção pequena.

## Tasks

- [x] 1. Auditar contratos e capturar baseline
  - [x] 1.1 Mapear produtores e consumidores afetados
    - Ler specs atuais pertinentes; mapear básico, Q/W/E/R, Guard Impact, Techniques/Mutations, ecos, zonas, projéteis/salvas, DoT e dano ambiental para D4. Distinguir modificação de golpe nativo de impacto derivado.
    - Inventariar receipts, energia, HookBus, cleanup de status/morte, gerações, cancelamento e passagem por `PlayerActor.ResolveAttackDamage`. Registrar arquivo, origem, raiz, evento e adapter necessário; nenhum produtor fica elegível só pela fachada utilizada.
    - Registrar contratos antigos/novos: rank 3, primeiro tick, Frost por rank, choque sem dano duplicado, orçamento por execução e RewardSteps. Não enfraquecer testes corretos nem desativar hooks globalmente.
    - _Requisitos: R1.2–R1.4, R6.1–R6.7, R9.5; P5–P10; D2–D7_
  - [x] 1.2 Conferir baseline Unity e referências
    - Confirmar projeto/Editor MCP e capacidades, estado da cena/Play, Console preexistente, catálogos, FirstSector, definição manual, RunBoons e UI.
    - Identificar testes relevantes existentes e selecionar baseline dirigido ao pipeline que será alterado. Registrar resultados efetivos, falhas preexistentes e limitações no audit.
    - _Requisitos: R9.6, R10.1, R10.6, R10.9_

- [x] 2. Implementar catálogo validado e inventário local
  - Dependência: 1.
  - [x] 2.1 Implementar autoria, validação e snapshot
    - Criar catálogo dos sete IDs com fórmulas D2, parâmetros D6, relógios, pré-requisitos e rank máximo 3. Listas serializáveis; nenhuma mutação de gameplay em OnValidate.
    - Rejeitar duplicatas/ausências, enums desconhecidos, NaN/infinito, negativos, chances/tempos/caps inválidos antes de normalizar. Snapshot imutável no começo da run.
    - MCP: importar, conferir Inspector, salvar/recarregar asset e referências. EditMode: P1 e valores de fronteira.
    - _Requisitos: R1.1–R1.6, R7.1, R9.6; P1_
  - [x] 2.2 Integrar inventário e aquisição única
    - `RunBoons` permanece aplicador único; preflight de sessão/revisão/pré-requisito/cap antes do commit de um ID/rank. Atualizar chave de inventário com sete ranks e revisão do catálogo.
    - Preservar fachadas necessárias e modo sem RunBoons por dependências locais explícitas. Aplicar cap/pré-requisitos aos sete IDs nas ofertas legadas, sem alterar outros boons.
    - Testar duplicação/reentrada, rank 0/1/3, rejeição no cap, revisão obsoleta, nova sessão e falha sem ganho parcial.
    - _Requisitos: R1.2–R1.4, R2.1–R2.2, R2.6, R9.3, R9.5; P1/P2_

- [x] 3. Evoluir tickets para RewardSteps e seleção de Protocol
  - Dependências: 1–2.
  - [x] 3.1 Implementar passos de recompensa e migração compatível
    - Introduzir lista ordenada de categoria/quantidade; nós sem lista conservam Reward/Choices antigos. Snapshot/validator/policy usam categoria do passo e ordinal crescente por nó.
    - Concluir nó somente após todos os tickets; preservar garantia de Mutation, alternativa, fault/recovery, revisão e notificações contextualizadas. Não conceder pacote de IDs por escolha.
    - EditMode e PlayMode dirigidos: categorias mistas, dois tickets iguais, fechamento/reabertura, release entre tickets, callbacks antigos, reentrada/falha e conclusão única.
    - _Requisitos: R2.4–R2.7, R6.5, R9.5–R9.6; P2/P3_
  - [x] 3.2 Implementar seleção determinística D1
    - Prioridades das oportunidades globais 1–5, filtros e preenchimento estável até três cartões; primeira somente Ignite/Frost. Contar oportunidades reais separadamente de categoria/ordinal local.
    - Provar por testes de policy as cinco sequências de combinação D1, alcance dos sete IDs, caps, dependências, igualdade ao reabrir e ausência de inventário residual.
    - Seleção legada mantém sua forma genérica com elegibilidade/caps declarados; não usar pool manual para substituí-la silenciosamente.
    - _Requisitos: R2.1–R2.6, R5.6–R5.7; P2/P3_

- [x] 4. Migrar autoria da rota publicada
  - Dependência: 3.
  - Migrar explicitamente C1/C2/C6 para Technique×1 → Protocol×1 e E2 para Protocol×2. Preservar E1, C4, RP, demais nós, geometria/NavMesh e GUIDs.
  - MCP: salvar/recarregar definição, inspecionar referências e snapshot; enumerar quatro percursos, confirmar quatro/cinco Protocols, Techniques anteriores, Mutation e 6–8 combates.
  - Atualizar builder de autoria e documentação de integração afetada, mantendo histórico dos contratos da Fase 4. Não executar mutação de assets em OnValidate.
  - _Requisitos: R2.3–R2.4, R2.7, R8.6, R9.6; P1/P3; D1_

- [x] 5. Implementar raízes, reservas e orçamento compartilhado
  - Dependência: 2.
  - [x] 5.1 Implementar contexto e identidade
    - Criar raízes por execução, ranks congelados, receipts, handles de alvo/geração/aplicação e reservas para descendentes. Selar produtor sem invalidar filhos legítimos reservados.
    - Pool/reuso só após fila/reservas/lote fechados; replay não recria raiz. Tick legítimo possui ApplicationId/ordinal próprio, sem permitir novo root a descendente recusado.
    - EditMode: multialvo, etapas, filho atrasado, ranks adquiridos em curso, selamento, cancelamento, replay e geração nova.
    - _Requisitos: R6.1–R6.3, R6.7, R9.4, R9.7; P7/P10; D3_
  - [x] 5.2 Implementar caps locais/agregados e telemetria bounded
    - Aplicar todos os tetos D6: 32 tentativas/root, profundidade 4, fila 64, reservas 32, roots 64, 256 tentativas/frame, 64 consultas/frame e buffers 128; integrar contagem de aplicações/ticks/VFX nas tasks seguintes.
    - Reservar tentativa antes do dano, inclusive mitigado; profundidade 4 aplica sem filhos. Descartar excedente sem dívida, nova raiz, alocação ilimitada ou bloqueio do dano nativo.
    - EditMode: fronteiras exatas/um além, reentrada, múltiplas raízes/frame, saturação de fila/buffer, TTL escalado 10s, ausência de starvation e liberação em finally.
    - _Requisitos: R6.2–R6.3, R7.1–R7.5; P7_

- [x] 6. Integrar produtores, receipts e snapshots de morte
  - Dependências: 1, 5.
  - [x] 6.1 Conectar adapters ao pipeline real
    - Básico multialvo/habilidade multi-impacto compartilham raiz; Techniques/Mutations derivadas, Guard Impact, ecos/zonas e projéteis legados carregam contexto correto, incluindo reservas futuras.
    - D4 controla aplicação/propagação/amplificação; energia continua exclusivamente na política causal da Fase 3. Não rerrolar críticos/reaplicar multiplicadores diretos a Protocol.
    - PlayMode: produtores auditados, irmãos de salva, impactos cancelados/atrasados, energia/epoch/categoria/hitstop e hooks existentes com/sem modificadores.
    - _Requisitos: R6.1–R6.4, R6.6–R6.8, R9.5; P7/P8_
  - [x] 6.2 Capturar morte antes do cleanup
    - Snapshot imutável de status/posição/geração antes de TakeDamage/limpeza; DeathReceipt usa receipt letal confirmado e reclama morte uma vez. Não aplicar elemento em cadáver ou reconstruir dados no transform reutilizado.
    - Testar dano zero, invulnerabilidade, overkill, morte síncrona com limpeza, pooling, reentrada e morte por burst; registrar discrepâncias encontradas no adapter.
    - _Requisitos: R4.3–R4.6, R6.1, R6.7–R6.8, R9.4; P6/P10; D5_

- [x] 7. Implementar Ignite, Frost e Reactor
  - Dependências: 2, 5–6.
  - [x] 7.1 Burn e Reactor com relógio/aplicação próprios
    - Fórmulas D2, primeiro tick após 0,5s, duração 4s, renovação mantendo maior DPS e relógio único; ranks capturados, sem recálculo retroativo ou mínimo acidental do componente antigo.
    - Até 256 aplicações/tipo e 64 ticks/frame, round-robin, um tick/aplicação/frame; pausa/hitstop sem ticks, stall sem catch-up. Morte por tick autoriza Detonation no root do tick.
    - Testar dano medido, renovação, rank novo, expirado, delta zero, saturação, nova geração e cleanup. Regressões `DetonationReactorBurnScalingTests` e `DetonationIsolatedBurnTickTests`, com migrações documentadas.
    - _Requisitos: R3.1, R3.3, R3.5, R3.7, R5.1, R7.2–R7.4; P4/P7/P10_
  - [x] 7.2 Frost e propriedade de controle
    - Chance/rank/duração D2 e sorteio injetável; aplicar política Fragile/Grunt, Heavy, Elite e Boss. Sem freeze nos três últimos; conservar resistências e proprietários de velocidade/stop.
    - PlayMode: cinco ranks, reaplicação/expiração, controle simultâneo, morte, disable/reuso, pause/hitstop. Confirmar restauração sem liberar stop alheio.
    - _Requisitos: R3.2–R3.3, R3.5–R3.7, R9.7; P4_

- [x] 8. Implementar Resonance, choque térmico e Overflow
  - Dependências: 6–7.
  - D5: Resonance uma vez antes do dano a partir de status prévios; choque por cada interação permitida, até dois por receipt; Overflow somente produtor elegível e status posteriores, sem consumir/renovar.
  - Toda tentativa usa orçamento, mesmo sem Resonance; choque não causa dano base seguido de uma segunda aplicação via ReportImpact. Bursts não reaplicam elementos nem ativam outros bursts.
  - EditMode/PlayMode: zero/um/dois status, rank 1–3, pré-requisito, elemento do próprio hit, timers preservados, morte no primeiro burst, reentrada, cap e NaN/infinito.
  - Regressões pertinentes: `ElementalOverflowGatePropertyTests`, `ThermalShockBurstMagnitudeTests`, `ThermalShockResonanceAmplifiedTests`, `ThermalShockAtMostOncePerApplicationTests`, `ThermalShockStatusPreservationTests`, `ThermalShockNoOppositeElementTests`.
  - _Requisitos: R3.4, R5.2–R5.6, R6.3–R6.4, R6.7–R6.8, R7.2; P5/P7/P8; D2/D4/D5_

- [x] 9. Implementar Conductor e Detonation
  - Dependências: 6–8.
  - Fila síncrona FIFO, fórmulas/rank/radius D2, seleção por distância/identidade, equipe/encontro/vida/linha de visão. Visita combinada cadeia/explosão uma vez por vítima/root, distinta de bursts locais.
  - Explosão única por morte, incluindo mortes por secundário/tick; Chill prevalece sobre Burn, Shatter até seis vítimas e traços cosméticos distintos. Receipts mantêm origem e não geram energia nativa.
  - Testar cascata letal, alvos múltiplos/colliders duplicados, fonte reciclada, parede, aliado, zero dano, ranks, profundidade 4 e saturação. Regressões `DetonationClassificationTests`, `DetonationCascadeBoundsTests`, `IntegrationDetonationWithElementsTests`, `GlobalCascadeBoundsTests` e bounds de boons/lança diretamente afetados.
  - _Requisitos: R4.1–R4.7, R6.1–R6.4, R7.2–R7.5; P5–P8_

- [x] 10. Integrar fronteira do encontro e cleanup de sessão
  - Dependências: 5–9.
  - [x] 10.1 Preservar lote confirmado antes da recompensa
    - Runtime participa do candidato de encerramento da Fase 4; aguardar lote/notificações/leases confirmados, não reservas futuras, DoT eterno ou VFX.
    - Fechar admissão, cancelar futuro e liberar reservas após lote; preservar energia/categoria/epoch/hitstop. Morte do jogador tem prioridade; não resetar combate para abrir ticket.
    - PlayMode: última morte por cascata/tick/burst, efeitos atrasados, morte concorrente e duas primeiras salas; recompensas sem duplicação/bloqueio.
    - _Requisitos: R6.5–R6.7, R9.7, R10.5; P9_
  - [x] 10.2 Limpar somente estado owned
    - Invalidação por sessão/encontro/alvo/geração/aplicação; cleanup idempotente por morte, aborto, extração, disable/destruição e reuso. Transição conserva inventário/recursos sem reaplicar ganhos.
    - Testar callback antigo na mesma run e na seguinte, status de outra fonte, raiz viva, aplicação renovada e nova geração. Sincronizar fachadas de Clear sem segundo teardown concorrente.
    - _Requisitos: R3.7, R9.1–R9.5, R9.7; P10_

- [x] 11. Atualizar UI de rota, tickets e feedback
  - Dependências: 3–4, 7–10.
  - Mostrar recompensas ordenadas e progresso dos passos, quatro/cinco oportunidades e dois tickets E2. Explicar nome/categoria/rank, números, dependências, leitura de status e ausência de energia; Overflow sem consumo, Frost sem freeze garantido/indevido.
  - Preservar M, cartões congelados, movimento/input e liberação entre escolhas; nenhuma confirmação duplicada por tecla/clique mantidos.
  - Implementar feedback distinto por forma/movimento/cor, com 24 VFX ativos, 16 novos/frame e TTL 0,6s escalado. Telegraphs prioritários; partículas não reservam combate nem criam impactos.
  - MCP: inspeção em runtime nas resoluções disponíveis e save/reload de referências. PlayMode: release/tickets múltiplos e caps visuais; clareza humana continua no gate 15.
  - _Requisitos: R7.4–R7.5, R8.1–R8.6, R9.6; P2/P11; D8_

- [x] 12. Validar integração e alcançabilidade publicadas
  - Dependências: 4, 9–11.
  - Executar as cinco sequências D1 por ofertas/UI/tickets reais no fluxo Nexus/FirstSector; comprovar sete IDs e ativação das combinações sem injetar inventário. Enumerar os quatro percursos e verificar contagem 4/5, Mutation, HP, E1/E2, Boss e encerramento.
  - Fixtures determinísticas de inimigos devem ser declaradas; nenhuma chamada direta a ApplyOffer substitui prova de aquisição. Inspecionar efeitos reais e segunda sala, não apenas helper ou caixas.
  - Verificar modos legados Bow/Spear, caps dos sete IDs e classificação de projéteis/filhos sem antecipar rework de armas. Executar regressões pertinentes de energia/input/reward afetadas desde o último check.
  - MCP: testes/PlayMode, Console, referências publicadas e cena preservada. Registrar jobs, contagens, falhas e limitações atuais.
  - _Requisitos: R2.3, R5.7, R9.5–R9.6, R10.1–R10.6; P3/P8–P11_

- [ ] 13. Medir performance e limites agregados
  - Dependências: 9–12.
  - [ ] 13.1 Construir e executar harness reproduzível
    - Seed/configuração salva, baseline/com combinação iguais, densidades 8/32/64, build alcançável rank 1 e stress sete rank 3. Inventário injetado somente no stress, não como prova de aquisição.
    - Warm-up 10s, coleta 60s, três repetições; repetir com VFX desligados. Testar separadamente 256 aplicações e 65 roots, pooling e combate sustentado com novas gerações.
    - Registrar hardware real, Unity/backend/build/qualidade/resolução, raw samples, markers CPU/GC, percentis, memória e drops. Editor verifica regressão; Development Player verifica custo quando execução/profiler suportados. Indisponibilidade fica pendente.
    - _Requisitos: R7.1–R7.6, R10.3, R10.8; P7/P12_
  - [ ] 13.2 Avaliar metas e corrigir regressões
    - Metas D8: Protocol CPU p95 ≤1ms/p99 ≤2ms; GC p95 ≤1KB/frame; sem crescimento monotônico; frame p95 ≤16,7ms no hardware de referência e incremento ≤2ms. Baseline acima da meta absoluta não aprova essa meta.
    - Separar saturação controlada de falha de custo; correções repetem somente cenários/tests afetados. Mudança de valor/critério exige rationale e comparação registrada, sem apagar resultado anterior.
    - _Requisitos: R7.6–R7.7, R10.8–R10.9; P12_

- [ ] 14. Checkpoint técnico e revisão de escopo
  - Dependências: 1–13.
  - Revisar cobertura P1–P12 e cada critério técnico dos requirements, compilation/Console/jobs, save/reload e runtime. Caixas concluídas não substituem evidência; resolver defeitos reproduzidos no escopo.
  - Revisar `git diff`, metadados/GUIDs, referências, alterações inesperadas e loaders. Preservar alterações do usuário; documentar migrações D1–D7 e compatibilidade afetada.
  - Não executar toda a suíte por padrão; repetir/broaden somente checks justificados por mudança, falha ou lacuna concreta. Se MCP/performance tiver pendência, checkpoint fica pendente.
  - _Requisitos: R1.2, R9.5–R9.6, R10.1–R10.6, R10.9; AGENTS.md_

- [ ] 15. Gate humano — composição e legibilidade
  - Dependência: 14.
  - Realizar duas runs com mesma Manopla e escolhas diferentes; registrar ramos, tickets/ranks, Mutation, HP, resultado e situações concretas de posicionamento/prioridade/sequência alterados pela combinação.
  - Avaliar em três encontros por build reconhecimento da causa dominante e leitura de telegraphs; registrar acertos/erros, ocultação recorrente, controles indevidos e responsividade. Mais dano/ausência de exceção não prova emergência.
  - Avaliar ritmo da expansão de recompensas e vantagem Elite. Ajustes demonstrados permanecem no escopo; repetir validações afetadas. Sem relato/aceite humano, manter gate desmarcado.
  - _Requisitos: R7.7, R8.4–R8.5, R10.7–R10.9; P11/P12; D8_

- [ ] 16. Consolidar handoff e encerramento
  - Dependências: 14–15.
  - Consolidar `validation.md`: decisões/rationale, alcance, produtores, fórmulas/ranks, migrações, jobs/resultados, performance/hardware, aceitação humana, riscos e pendências separados.
  - Sincronizar status de requirements/design/tasks/audit sem apagar histórico ou reabrir aceites das Fases 2–3. Só declarar fase encerrada após verificações exigidas e gate humano resolvidos; futuras propostas permanecem fora da entrega.
  - _Requisitos: R10.8–R10.9; AGENTS.md — Completion and Handoff_

## Cobertura e ordem

| Área | Tasks principais |
| --- | --- |
| R1 — catálogo/ranks/validação | 1–2 |
| R2 — aquisição/tickets/alcance | 2–4, 11–12 |
| R3 — elementos/relógios/resistência | 5–8, 10 |
| R4 — propagação/morte | 6, 9–10 |
| R5 — amplificadores/Overflow | 2–3, 7–8, 12 |
| R6 — composição/energia/fronteira | 1, 5–6, 8–10, 12 |
| R7 — caps/performance | 5, 7–9, 11, 13–15 |
| R8 — UI/feedback | 11–12, 15 |
| R9 — sessão/compatibilidade/autoria | 1–7, 10, 12, 14 |
| R10 — checks/aceitação/handoff | 1, 12–16, validação incremental em cada incremento |

**Registro de elaboração:** somente documentação. Nenhum código, asset ou cena foi alterado; nenhuma compilação, execução Unity MCP, teste ou medição foi realizada para elaborar este plano.


