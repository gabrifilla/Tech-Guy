# GPR-01.P1 — planejamento e prontidão da primeira fatia

28/10/2026 · **CONCLUÍDA COMO PLANEJAMENTO. AGUARDANDO APROVAÇÃO DO DESIGN.**

## Trabalho concluído

Revisados AGENTS, visão/pilares, plano de execução, combate/armas/inimigos/boons/HUD/run e código real de básico, grafo, Protocols e aquisição. Preparada a [fatia v1](../gameplay/GPR_01_VERTICAL_SLICE.md): Punhos Titânicos no LMB + Conductor + Detonation, transformação visível e integração com Q/Avanço. Nenhuma alteração de gameplay.

Conflitos explicitados: três vias antes reservadas a GPR-02; swap Uppercut antes obrigatório passa a adiamento proposto; Basic=-1 não é alvo válido do grafo atual; ProtocolEligibility já permite cascatas; RefreshLoadout global pode resetar timers. A proposta descreve extensões limitadas nos owners reais, não um motor paralelo.

## Arquivos criados e modificados nesta rodada

Criados: este relatório, GPR-C01.5_RELATORIO.md, GPR-C01_PHASE_SUMMARY.md e Docs/gameplay/GPR_01_VERTICAL_SLICE.md.

Atualizados em Docs/gameplay: GPR_REQUIREMENTS.md, GPR_TECHNICAL_DESIGN.md, GPR_TASKS.md, GPR_VALIDATION_PLAN.md, README.md, GPR_CONTROL_MIGRATION.md, GPR_OWNERSHIP.md, plano TECH_GUY_GAMEPLAY_REWORK_EXECUTION_PLAN_v1.1_WASD.md e nota de estado na visão TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md.

Atualizados: Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md e ADR-0006-GPR-DIRECAO-E-GATES.md; Docs/validation/01_MATRIZ_DE_VALIDACAO.md; Docs/systems/01_COMBATE.md,02_ARMAS.md,04_BOONS.md; Docs/02_ESTADO_E_LACUNAS.md,03_PRIORIDADES.md e README.md. Relatórios históricos não foram reescritos como se o aceite tivesse ocorrido antes.

## Skills aplicadas

tech-guy-game-vision, tech-guy-combat-unity, tech-guy-boon-design, tech-guy-unity-acceptance, com suas referências locais. Nenhuma skill gamedev adicional disponível foi identificada no catálogo desta sessão/.agents/skills; nenhuma instalação ou uso fictício. Não houve delegação.

## Testes, evidências e limites

Inspeção estática do checkout atual com alterações anteriores preservadas; leitura/verificação dos seis resultados finais de C01.4 confirmou116 nomes distintos aprovados. Fontes e jobs no [fechamento5](GPR-C01.5_RELATORIO.md). Não executados novos testes Unity, compilação, render ou playtest nesta rodada documental. O desenho A/B/C não foi validado em runtime, assets ou ofertas reais.

Verificação final: diff/check de documentação e links;1623 arquivos protegidos de código/cenas/assets/metas/config/skills conservaram SHA256 e existência entre início/fim desta rodada. Não é comparação universal de todo arquivo de terceiros ou perfil. Save não lido/escrito por novas chamadas Unity; nenhum rollback/reset de progresso ou alterações anteriores.

## Problemas e decisões pendentes

Aprovar conteúdo/números de A, limites por linhagem e C on-kill; confirmar adiamento do swap. Somente na implementação autorizada serão provados seam de Basic, serialização, tickets, no-reset de timers, cascatas, perfis de alvo e visual. Sem aprovação desses parâmetros, estado permanece PROPOSTO.

Moonwalk/cadência são refinamentos futuros não bloqueadores de C01; não incorporados à fatia. Tripo3D, keys, Playground e cobertura externa permanecem pendências delimitadas, sem correção incidental.

## Próxima atividade e aprovação

GPR-01.1, **não iniciada**: após aprovação explícita da proposta, preparar extensões mínimas e regressões afetadas. STOP antes de implementar qualquer gameplay/asset/catálogo GPR-01. Aprovar o documento não deve ser confundido com aceite de uma implementação ainda inexistente.

