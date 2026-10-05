# Implementation Plan: Run Structure (Fase 4)

## Overview

Este plano deriva [requirements.md](requirements.md) e [design.md](design.md), incluindo os ajustes do parecer de design de 04/10/2026. Ordem: auditoria/baseline → dados/sessão/policy → autoria → recompensas/Repair/HP → Elite/Mutation → fronteira de encontro/Boss/lifecycle → UI/integração → checkpoint técnico → aceitação humana → handoff.

**Status: execução iniciada em 04/10/2026.** Tasks 1–14 concluídas; implementação incremental em andamento, com evidências em contract-audit.md e validation.md. Fases 2–3 já aceitas expressamente; não reabrir esses gates. Repair 35%, composição de Elite e alternativa Armor são propostas iniciais do design, não tuning aprovado por gameplay.

Preservar grafo D1 com quatro percursos de 6/7/7/8 combates; catálogos/semântica D2–D8; modo procedural legado, GUIDs, enum RoomType e contratos das Fases 1–3. Não antecipar Protocol System, procedural novo, meta progressão ou expansão de armas/inimigos.

P1–P16 referem-se a Correctness Properties do design; `R7.6`, por exemplo, ao critério 6 do Requirement 7. Nenhum critério/teste listado é opcional. Usar harness de propriedades existente quando útil; testes de helpers não substituem integração, navegação, pipeline e input reais.

**Validação incremental:** antes do trabalho Unity, descobrir capacidades MCP e confirmar projeto/Editor, cena/dirty state e Console inicial. Após alterações relevantes, refresh/import quando necessário, aguardar compilação e inspecionar Console sem limpar. Executar apenas testes referentes às alterações e dependências afetadas; ampliar somente por mudança, falha ou risco concreto. Usar Unity MCP obrigatoriamente nos checks necessários que ele suporta: dados/referências/save-reload, testes e runtime. Registrar limitações e manter checks pendentes se indisponível; CLI e inspeção estática não substituem evidência Unity. Sair de PlayMode preservando a cena do usuário.

Antes de editar, verificar git status e preservar alterações existentes. Novos scripts/ativos seguem AGENTS.md e os caminhos do design; não reorganizar Stages nem terceiros. Marcar cada subtarefa após implementação e validações exigidas. Checkpoints exigem evidência registrada, não apenas caixas anteriores. Durante incrementos incompletos, testar em fixtures/ambiente isolado; não trocar o fluxo publicado de FirstSector/Nexus para o manual antes da integração da task 13.

## Tasks

- [x] 1. Auditar contratos e capturar baseline
  - [x] 1.1 Conferir documentos e dependências
    - Ler specs atuais de run procedural/jogável, Fases 1–3 e sistemas de recompensa afetados; registrar aceitação anterior e scope D1–D8 em `contract-audit.md` desta spec.
    - Registrar adaptações locais manual/legado, arquivos/callers/testes atingidos e conflitos reais; não inferir estado runtime de documentação histórica.
    - _Requisitos: R9.3–R9.6, R10.7_
  - [x] 1.2 Auditar progressão, recursos e callbacks
    - Mapear ProgressionDirector, builders/ambientes, portal Nexus/ScenePortal, RewardTrophy/RunBoons/UI, RoomGraph/RoomType, gates/NavMesh, fragmentos e teardown.
    - Conferir efeitos de RefreshLoadout/equip/reconfiguração em cooldown, Asura/categoria/epoch, snapshots e consumers; inventariar cura de aquisição versus reconstrução, inclusive Iron Heart.
    - Mapear morte do último inimigo, fronteira do lote e fechamento do grupo de feedback/hit-stop; definir integração sem cancelar dentro do primeiro callback letal.
    - Confirmar aplicação real de `bulwark`, elegibilidade dos quatro Protocols e passagem do modo antes de Awake sem global novo.
    - _Requisitos: R3.4–R3.7, R4.1–R4.7, R8.7–R8.9, R9.5–R9.8_
  - [x] 1.3 Inspecionar baseline Unity e testes pertinentes
    - MCP/instância/projeto/capacidades confirmados; Console preservado; cenas/prefabs/arma/HUD/NavMesh inspecionados sem salvar mudanças do usuário.
    - Teste de energia PlayMode 1/1 e FirstSectorProceduralTests EditMode 9/9 passaram. Baseline e avisos documentados em contract-audit.md. Registro externo de 488 testes não é evidência executada nesta sessão.
    - _Requisitos: R9.4, R10.2, R10.4_

- [x] 2. Checkpoint — contratos reconciliados
  - Auditoria corrigida conforme task-1-review.md. Seleção pré-Awake concretizada via fonte existente WeaponLoadout, sem global novo; Asura/timers, HP, tickets, fronteira letal e retorno reconciliados em contract-audit.md.
  - Quatro Protocols manuais, exclusões legadas e quatro percursos mantidos. Fases aceitas preservadas; tuning continua provisório.
  - _Requisitos: R1.4, R3.4, R9.6, R10.7_

- [x] 3. Implementar autoria, snapshot e validator manual
  - [x] 3.1 Definir modelos serializáveis
    - ManualRunDefinition, funções/reward kinds/modo separado, nós/conexões/grupos exclusivos, encontros/Repair e bindings de cena conforme Data Models.
    - Manter ProceduralLegacy=0 e valores existentes de RoomType; listas no Inspector, cópias imutáveis runtime, sem dicionário como fonte de autoria.
    - _Requisitos: R1.1, R3.1, R9.1–R9.4_
  - [x] 3.2 Validar configuração antes de iniciar
    - Rejeitar IDs/referências/conexões/enums inválidos, ciclos, NaN/infinito/negativos/limites e Mutation inicial impossível; não materializar parcialmente nem alterar sessão válida.
    - Enumerar D1: quatro percursos 6/7/7/8, Boss único/alcançável, Elite opcional, RP disponível e C4 em todos os percursos. Distinguir visitas de combates e zero bruto de ausência.
    - _Requisitos: R1.4–R1.6, R7.1, R7.6, R9.2_
  - [x] 3.3 Verificar modelos e persistência
    - EditMode P1/P8/P12: grafos válidos/inválidos, parâmetros e snapshot não alterado por mudanças na autoria/cópias retornadas.
    - MCP salvar/recarregar definição e referências reais; verificar diagnóstico sem execução/mutação em OnValidate.
    - _Requisitos: R9.1–R9.2, R10.1–R10.3_

- [x] 4. Implementar sessão e policy de rota
  - [x] 4.1 Criar identidade e estados locais
    - RunSessionState/ManualRoutePolicy, fases e estados de nós/tickets/resultados; nova identidade por sessão e deduplicação de pedidos/eventos.
    - Preflight sem efeito colateral; sessão inválida/encerrada ou pedido rejeitado não altera ramo, recursos ou progresso. Inventário temporário distinto do arsenal.
    - _Requisitos: R1.3, R2.3, R8.5–R8.6, R9.7–R9.8_
  - [x] 4.2 Aplicar compromisso/reconexão
    - RP exclui C3 e vice-versa; C5 exclui E1/E2, E1 exclui C5. Concluir/revisitar/reconectar não restaura alternativas ou recompensas.
    - Separar nó visitado fisicamente da fronteira lógica; entrada somente no destino confirmado e sem dois encontros ativos.
    - _Requisitos: R1.2–R1.7, R2.5_
  - [x] 4.3 Testar transições de estado
    - EditMode P1/P2/P4/P10: pedidos duplicados/reentrantes, conexões fora de ordem, excluídos, reconexão, snapshot, sessionId antigo e rejeições sem mutação.
    - _Requisitos: R1.3–R1.7, R9.7–R9.8, R10.1–R10.3_

- [x] 5. Integrar modo e coordenador de mundo
  - [x] 5.1 Selecionar manual/legado antes da inicialização
    - Resolver contexto local Nexus → FirstSector antes de Awake; ManualSlice apenas para Manopla elegível, outras famílias conservam modo legado com indicação explícita.
    - Um ProgressionDirector; manual não chama gerador/discovery/multistage legado. Roots, triggers, colisões e NavMeshes mutuamente exclusivos; sem singleton/global novo.
    - _Requisitos: R9.3–R9.5, R7.6_
  - [x] 5.2 Adaptar entrada e fases
    - Consumir policy para abrir conexão confirmada/armar encontro no volume; gates de fase e referências explícitas.
    - Última morte registra candidata, sem transição/cleanup/reward imediatos; integrar finalização completa na task 11. Manter testes/fixtures explícitos para fase ainda incompleta.
    - _Requisitos: R1.3, R2.3, R8.9, R9.5_
  - [x] 5.3 Verificar seleção e compatibilidade
    - PlayMode P9/P14: dois modos, contexto ausente/incompatível, nenhum gerador/ambiente duplicado; referências serializadas e root/NavMesh real via MCP.
    - Rodar regressões pertinentes de seleção/gerador, sem alterar seus asserts corretos.
    - _Requisitos: R9.3–R9.4, R10.2–R10.4_

- [x] 6. Autorar rota fixa e navegação
  - [x] 6.1 Criar definição D1 e bindings do ambiente
    - Autorar S/C1/C2/RP/C3/C4/C5/E1/E2/C6/B, grupos A/B, categorias e quantidades; adapter ManualStageEnvironment e arenas/corredores fixos.
    - RoomGraph como projeção geométrica, sem supor grid adjacente; impedir passagens RP↔C3 e C5↔E1/E2. Preservar GUIDs/cena/assets e builder legado.
    - _Requisitos: R1.1–R1.7, R3.1, R9.1–R9.4_
  - [x] 6.2 Configurar portas/spawns/NavMesh no Editor
    - Reutilizar assets de câmera/HUD/jogador/inimigos; conectar anchors/volumes/gates. Assar e salvar navegação do ambiente manual com referências estáveis.
    - Validar fisicamente quatro percursos, retornos permitidos e atalhos bloqueados, incluindo dash/avanço; validator puro não comprova cena.
    - _Requisitos: R1.3–R1.5, R2.5, R9.4–R9.5_
  - [x] 6.3 Verificar persistência/referências
    - MCP save/reload e inspeção de componentes, portas/spawns e NavMesh; verificar ausência de ambiente legado sobreposto e missing references.
    - Registrar autoria técnica sem ativar antecipadamente o fluxo manual publicado; preservar cena do usuário após validação isolada.
    - _Requisitos: R9.1, R9.4, R10.1–R10.2_

- [x] 7. Implementar recompensas tipadas e tickets
  - [x] 7.1 Integrar policy com aplicador existente
    - RunRewardRequest/Ticket com sessão/nó/ordinal, Pending/Committing/Resolved; API contextual e conclusão identificada, mantendo OfferReward(int)/RewardChosen legados.
    - Claim antes do efeito/publicação; aquisição única, rejeição sem commit, exceção parcial com diagnóstico/aborto em vez de retry cego. Troféu reclamado não apaga ticket pendente.
    - _Requisitos: R3.5–R3.7, R9.8_
  - [x] 7.2 Implementar catálogos, alternativas e ofertas estáveis
    - Dez Techniques e quatro Mutations elegíveis; Protocols apenas ignite/frost/conductor/detonation, sem Reactor/Resonance/Overflow manual ou roll Rewrite para Mutation.
    - Até três opções distintas; ranks/aliases/slot respeitados. Fallback `bulwark` +40 Armor real e identificado; Mutation vazia sem garantia satisfeita segue recuperação da task 10.
    - Congelar oferta por ticket; reabrir não resorteia/consome RNG. Mudança legítima usa revisão de inventário, ordem estável/seed derivada e diagnóstico; escolha revalida.
    - _Requisitos: R3.2–R3.7, R7.2–R7.6_
  - [x] 7.3 Validar aquisição e alcançabilidade
    - EditMode/PlayMode P3/P4/P15: sequência válida para cada um dos quatro Protocols em C5/E2, exclusão dos inalcançáveis e preservação de efeitos/pool legados.
    - Testar catálogo esgotado, limite de rank, efeito real Armor, duplo clique/reentrância, cancel/reabrir, inventário alterado e assets fonte intactos. Regressões apenas de RunBoons/callers afetados.
    - _Requisitos: R3.2–R3.7, R10.1–R10.4_

- [x] 8. Implementar HP persistente e Repair
  - [x] 8.1 Preservar recursos entre nós
    - HP sem cura de transição; reconstrução resolve maxHP final antes de HP atual, sem simular pickup/recurar Iron Heart.
    - Preservar mana regen/Asura/categoria e cooldown fora de aquisição; RefreshLoadout aceito conserva contrato auditado de timers sem reset acidental de energia/epoch/consumers.
    - Recursos iniciais aplicados uma vez por sessão; reativação não inicia run nova. Atualizar HUD pelos canais existentes.
    - _Requisitos: R4.1–R4.7, R8.5, R9.5, R9.7_
  - [x] 8.2 Implementar oportunidade de Repair
    - 35% maxHP limitado ao déficit, um uso, sem moeda/mana/Asura. Uso/dispensa identificados; dispensa consome oportunidade, revisita não recupera uso.
    - HP cheio: uso indisponível, saída por dispensa; rejeição por estado/morte/sessão não cobra/cura/conclui. UI apresenta quantidade efetiva, limite e custo de oportunidade.
    - _Requisitos: R5.1–R5.8_
  - [x] 8.3 Validar persistência/recuperação
    - EditMode/PlayMode P5/P6/P13: HP cheio/baixo, maxHP alterado/reconstrução, zero/inválidos, callbacks repetidos e rejeição, reabrir/revisitar/reativar.
    - Demonstrar nenhuma cura/mana/Asura cosmética ou por transição e preservar snapshots/consumers. MCP autoria Repair/save-reload e runtime HUD.
    - _Requisitos: R4.1–R4.7, R5.1–R5.8, R10.2–R10.4_

- [x] 9. Integrar Elite e recompensa adicional
  - [x] 9.1 Autorar composição e dois ordinais
    - E1: um Elite existente e quatro comuns, perfis/resistência/abertura/retomada atuais; E2 combate adicional/Protocol, sem novos inimigos.
    - Duas aquisições Technique sequenciais; inventário do ordinal 0 informa oferta 1. Catálogo vazio conserva duas aplicações efetivas de alternativa Armor.
    - _Requisitos: R6.1–R6.7_
  - [x] 9.2 Validar vantagem e cleanup
    - PlayMode P7/P14: risco/composição e benefício reais, dois ordinais, tecla/release não seleciona dois de uma vez, callbacks duplicados e morte entre escolhas.
    - Conferir vantagem anunciada versus entrega, fallback +80 Armor quando ambos esgotados e caminho sem Elite válido; MCP referências dos perfis existentes.
    - _Requisitos: R6.1–R6.7, R10.1–R10.4_

- [x] 10. Integrar garantia efetiva de Mutation
  - [x] 10.1 Resolver C4 em todos os percursos
    - Exigir aquisição elegível nesta sessão antes do Boss; nenhum roll de raridade/Elite obrigatório. HUD apresenta ação transformada, com contratos Fase 3 preservados.
    - Cancelar UI mantém ticket/saídas e permite reabrir; sem dispensa da primeira Mutation. Oferta/fallback/import antigo não incrementa garantia.
    - _Requisitos: R7.1–R7.5_
  - [x] 10.2 Tratar elegibilidade/impossibilidade
    - Loadout/família inicial validado; troca incompatível durante sessão recusada antes de equipar. Aquisição anterior legítima da sessão satisfaz garantia; oportunidade excedente usa alternativa.
    - Sem opções e sem aquisição: diagnosticar, manter Boss bloqueado e permitir aborto/Nexus; não apagar slots/inventário, criar ranks ou quebrar exclusividade.
    - _Requisitos: R7.2, R7.4, R7.6, R9.7–R9.8_
  - [x] 10.3 Validar garantia/combate após transformação
    - EditMode/PlayMode P8/P10: quatro percursos, alias/import, todos slots ocupados, fallback excedente versus primeira garantia, cancel/reabrir, família rejeitada e saída por aborto.
    - Exercitar Mutation adquirida no combate seguinte real; snapshot/custo/energia permanecem conforme Fase 3, sem escrever asset.
    - _Requisitos: R7.1–R7.6, R10.1–R10.4_

- [x] 11. Finalizar encontro na fronteira mecânica correta
  - [x] 11.1 Coordenar conclusão candidata e cleanup futuro
    - Última morte aguarda lote, callbacks síncronos, confirmações e fechamento do grupo de feedback/hit-stop; não cancelar dentro do primeiro callback letal.
    - Revalidar vivo/sessão/inimigos; reclamar encerramento único, cancelar somente ações/filhos futuros pelos owners e emitir ticket. Não reiniciar recurso/categoria/epoch nem esperar animação cosmética indefinidamente.
    - Impedir novo encontro durante cleanup e reabertura por callback tardio. Não usar Configure(null) como atalho de transição.
    - _Requisitos: R3.7, R4.6, R8.7, R9.5, R9.8_
  - [x] 11.2 Testar ordem real do último golpe
    - PlayMode P16/P13/P14: básico/skill letal, multi-alvo e descendente último; energia elegível uma vez, HP/quebra/eventos e hit-stop confirmados preservados, ticket único.
    - Verificar cancelamento futuro sem dano tardio/novo ticket/reset Asura e morte do jogador no mesmo lote. Rodar regressões dirigidas do pipeline/feedback/cleanup alterados.
    - _Requisitos: R3.7, R4.6, R9.5, R10.1–R10.4_

- [x] 12. Implementar Boss, resultado e lifecycle
  - [x] 12.1 Aplicar gate e resolução final
    - B exige C6 concluído, tickets resolvidos e Mutation efetiva; manual sem fragmentos, legado mantém seu gate.
    - Sem recompensa final de build no MVP; manter fronteira extensível para autoria que a prevê. Morte na mesma resolução vence vitória; nenhum portal/reward antes do resultado válido.
    - _Requisitos: R8.1–R8.3, R8.7–R8.9_
  - [x] 12.2 Integrar extração, aborto, morte e retorno
    - ExtractionAvailable distinto de sessão encerrada; revalidar vivo no portal, reclamar Extracted antes da carga e teardown único. ScenePortal com guard/callback opcional, legado preservado.
    - Morte/aborto retornam sem vitória. Retry de load mantém resultado reclamado, sem novo portal/ticket/sessão. Remover apenas estado/locks/assets temporários próprios; preservar arsenal.
    - _Requisitos: R8.3–R8.8, R9.7–R9.8_
  - [x] 12.3 Validar concorrência e replay
    - PlayMode P10/P11/P14: ordem de callbacks morte/Boss, morte antes do portal, contato morto/repetido, falha/retry de carga, aborto e duas sessões com callbacks antigos de encontro/reward/cura/transição.
    - Validar reset de nós/Repair/inventário/resources versus arma persistente; regressões de ScenePortal/extração/fragmentos legado diretamente afetadas.
    - _Requisitos: R8.1–R8.9, R9.7–R9.8, R10.1–R10.4, R10.8_

- [x] 13. Integrar mapa/UI e publicar o fluxo manual na cena
  - [x] 13.1 Apresentar rota, recompensas e recuperação
    - Mapa de funções/categorias/conexões/estado lógico e físico, risco de Elite/encontro extra/duas aquisições, custo dos ramos, garantia e bloqueios de Boss.
    - UI de Repair mostra cura efetiva/uso/dispensa; ofertas identificam categoria/alternativa/incompatibilidade. UI apenas emite pedidos; cosmetic não progride.
    - _Requisitos: R2.1–R2.5, R3.5–R3.6, R5.2, R6.2, R7.3, R8.9_
  - [x] 13.2 Integrar input/locks e autoria final
    - Click/teclas/release não disparam básico/skill/movimento involuntário; rejeitar confirmação durante combate/cast/dash/pausa/hitstop/transição/morte. Fechar UI libera só lock próprio.
    - Ativar seleção Nexus/FirstSector final após dependências completas; preservar referências/GUIDs e opção legado. MCP save/reload/cena/prefab/componentes/UI e roots/NavMeshes exclusivos.
    - _Requisitos: R2.3–R2.5, R9.1, R9.3–R9.5_
  - [x] 13.3 Exercitar ponta a ponta
    - MCP PlayMode Nexus → quatro percursos → C4 → Boss → extração/Nexus, morte e nova run; navegação/portas/dash/retorno e mapa concordam, nenhum atalho a ramo excluído/Boss.
    - Registrar se fixture matou inimigos deterministicamente; essa evidência não aprova dificuldade/diversão. Verificar energia/HP e UI pendente/reativada, logs e scene state final.
    - _Requisitos: R1.1–R1.7, R2.1–R2.5, R10.1–R10.4, R10.8_

- [x] 14. Checkpoint — validação técnica e revisão final
  - Dependências: 3–13. Mapear R1–R10/P1–P16 à evidência incremental registrada no audit; não repetir checks suficientes sem mudança/falha/risco.
  - Confirmar import/compilação/Console, save-reload/referências/NavMesh e runtime necessários via Unity MCP. Registrar skips/limitações/falhas/retries e corrigir defeitos demonstrados dentro do escopo.
  - Rodar regressões dirigidas de modo legado/RunBoons/portais/resources/combate conforme APIs efetivamente alteradas; não enfraquecer testes corretos.
  - Revisar diff/GUIDs/enums/Resources/.meta e serialização, sem mudanças involuntárias/terceiros; sair de PlayMode e preservar cena. Não marcar completo com checks Unity obrigatórios pendentes; não substitui aceitação humana.
  - _Requisitos: R9.1–R9.8, R10.1–R10.4, R10.7–R10.8_

- [ ] 15. Gate manual — duas runs com a mesma Manopla
  - [ ] 15.1 Avaliar decisões de percurso/build/HP
    - Duas runs consecutivas; registrar HP em A/B, ramo e motivo, aquisições/Mutation, uso ou dispensa Repair, risco/benefício Elite e desfecho.
    - Avaliar diferença motivada por HP/build/risco/benefício, sem forçar caminhos para aprovar nem contar seed/cosmético isolado. Testes técnicos não demonstram diversão.
    - _Requisitos: R10.5–R10.6_
  - [ ] 15.2 Registrar aceitação e ajustes demonstrados
    - Verificar clareza das categorias/incompatibilidades, garantia de transformação e vantagem real; observar Repair/composição/Armor como hipóteses de tuning.
    - Registrar resultado humano e limitações. Se ajustes forem necessários, alterar só escopo atual e repetir checks afetados antes de registrar aceitação; pendência humana fica desmarcada.
    - _Requisitos: R5.2, R6.2, R6.6, R7.3, R10.5–R10.7_

  - [x] 15.3 Corrigir feedback inicial de movimento, conclusão da segunda sala e leitura das escolhas
    - Pisos Ground, reapresentação de reward após hitstop sem contrariar fechamento voluntário, cartões de destino e atalhos protegidos. Quatro testes PlayMode relevantes aprovados via MCP; evidências em `validation.md`.
    - Usuário aprovou a apresentação e autorizou iniciar a Fase 5; registro das duas runs completas da task 15.1 permanece separado e pendente.

- [ ] 16. Consolidar handoff e encerramento
  - Dependências: 14–15. Produzir `validation.md` com decisões/rationale, contratos manual/legado, migrações, tuning, compilação/testes/runtime e aceitação humana separados, limitações e próximas ações.
  - Atualizar requirements/design/tasks/audit sem apagar histórico. Declarar fase encerrada somente com checks Unity exigidos e aceitação humana resolvidos; propostas futuras fora da entrega.
  - _Requisitos: R9.6, R10.7; AGENTS.md — Completion and Handoff_

## Dependencies

| Task | Depende de |
| --- | --- |
| 1 | Documentos atuais e acesso ao baseline Unity na 1.3 |
| 2 | 1 |
| 3 | 2 |
| 4 | 3 |
| 5 | 3–4 |
| 6 | 3–5 |
| 7 | 3–6 |
| 8 | 7 |
| 9 | 6–8 |
| 10 | 7–9 |
| 11 | 5–10 |
| 12 | 10–11 |
| 13 | 6–12 |
| 14 | 3–13 |
| 15 | 14 |
| 16 | 14–15 |

Preparar dados/fixtures não autoriza publicar comportamento incompleto ou antecipar fases futuras. Alterações no mesmo pipeline/diretor seguem incrementos coordenados; respeitar dependências antes de marcar checks de integração.

## Requirements Coverage Map

| Requisito | Tasks principais |
| --- | --- |
| R1 — Rota/compromisso | 3–6, 13–14 |
| R2 — Mapa/input | 4–5, 13–14 |
| R3 — Categoria/aquisição | 7, 9–11, 13–14 |
| R4 — HP/recursos | 1, 8, 11, 13–14 |
| R5 — Repair | 6, 8, 13, 15 |
| R6 — Elite | 6, 9, 12–15 |
| R7 — Mutation | 3, 7, 10, 12–15 |
| R8 — Boss/replay | 4–5, 11–14 |
| R9 — Autoria/sessão/compatibilidade | 1–6, 8, 10–14, 16 |
| R10 — Validação/aceitação | Checks incrementais 1–13, checkpoint 14, gate 15 e handoff 16 |

## Correctness and Review Coverage Map

| Propriedade/ponto do parecer | Tasks e evidência esperada |
| --- | --- |
| P1/P2 — Percursos/compromisso | 3–6/13: quatro percursos 6/7/7/8 e exclusões físicas/lógicas |
| P3/P4 — Categorias/tickets | 7/9/13: elegibilidade, alternativa, oferta estável e commit único |
| P5/P6 — HP/Repair | 8/13: max final, transição sem cura, um uso/dispensa/rejeição |
| P7 — Elite | 9: dois benefícios reais/ordinais e caminho opcional |
| P8 — Mutation | 3/10/12: aquisição antes de B, fallback/recusa/import não falsificam garantia |
| P9 — UI/gates | 5–6/13: mapa, volumes, input e NavMesh reais |
| P10/P11 — Sessão/término | 4/12: callbacks antigos, resultado único, morte concorrente e load retry |
| P12 — Autoria | 3/6/13: validação bruta, snapshot e save/reload |
| P13/P14 — Clocks/compatibilidade | 5/8/11–14: pausa/hitstop/locks, assets/modo legado preservados |
| P15 / Ajuste A — Protocols alcançáveis | 1/2/7: quatro IDs, sequência válida; Reactor/Resonance apenas legado |
| P16 / Ajuste B — Fronteira letal | 1/5/11: lote/feedback antes de cleanup, energia/eventos/hit-stop e ticket único |
| Atenções de execução | 1/5/7–8/12–13: RefreshLoadout, modo pré-Awake, reabertura, Armor, navegação/retorno e retry |

**Registro de elaboração:** apenas documentação. Nenhum código/asset/cena foi alterado, nem houve nova compilação, teste, Unity MCP ou playtest para criar este plano.





