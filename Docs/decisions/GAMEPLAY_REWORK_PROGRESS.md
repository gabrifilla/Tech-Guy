# Gameplay Rework — progresso mestre

27/10/2026. **GPR-C01.3a concluída tecnicamente, aguardando revisão.** C01.2c aceita no escopo técnico. LMB direcional por clique implementado; hold do básico somente com ChargedShot por decisão explícita; Asura no RMB da Manopla incluída por autorização adicional.102/102 testes normais; aceite físico pendente. C01.3b/GPR-01 não iniciadas. [Relatório C01.3a](GPR-C01.3a_RELATORIO.md).

## GPR-C01.3a — fechamento vigente

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
