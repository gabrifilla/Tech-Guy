# Gameplay Rework — fontes e pacote GPR-00

## Estado vigente — GPR-01.1,28/10/2026

GPR-C01 permanece **COMPLETA e aceita pelo desenvolvedor**; GPR-01.P1 aceita como planejamento. LMB básico, RMB especial permanente e exatamente três modulares Q/E/R, sem X. Moonwalk/cadência continuam refinamentos futuros não bloqueadores, sem tuning nesta etapa.

Direção experimental A→A+B→A+B+C aceita, parâmetros1,5×/0,60× provisórios; autorizada **exclusivamente GPR-01.1**. [Relatório da fundação](../decisions/GPR-01.1_RELATORIO.md) / [fatia e próximo escopo](GPR_01_VERTICAL_SLICE.md). Sem asset/boonTitanic Fists habilitado, onda ou transformação visual de produção. STOP para revisão antes de01.2;01.3–6 também exigem autorização posterior.

Q/Avanço para aproximação é compatibilidade de ações, não sinergia mecânica nova. SwapAvanço→Uppercut adiado por redução aprovada de escopo; aquisição/substituição modular Q/E/R permanece requisito **NÃO IMPLEMENTADO/NÃO VALIDADO**, rastreado em GPR-03.1/RF-TROCA. Gates e decisões históricos abaixo não sobrepõem esta autorização.

## Histórico aprovado C01.4 — três skills + especial,28/10/2026

A decisão direta mais recente **substitui todo modelo de quatro slots modulares Q/E/R/X**: exatamente três slots modulares Q/E/R e um especial permanente separado no RMB. X não tem ação/placeholder; index3 e arrays de quatro referências permanecem compatibilidade interna, nunca uma quarta skill modular. Asura mantém executor, custos, duração, cooldown, animação, danos e continuação; F11 não é alias direto. Bow/Spear reutilizam sua quarta ação existente no canal especial, sem mecânica/asset novo. A futura aquisição/substituição é restrita aos três slots e continua fora de C01.4.

WASD e demais ações configuráveis no Settings; InteractG dedicado e finisherF distintos por padrão, sem sobrescrever personalizações. Dash padrãoCursor; opçãoMovimento usa vetor configurado/câmera normalizado e **facing atual quando não há direção pressionada**, conforme esclarecimento posterior do desenvolvedor. A sugestão histórica de fallbackcursor no modoMovimento está superada. Escape reservado à pausa/cancel seguro. Migração só lê/repara em memória, Apply explícito grava nomes de ações; sem reset de progresso.

Registro histórico de C01.4: implementação e116 casos cumulativos concluídos antes do aceite físico. A pendência humana foi encerrada por C01.5; o estado vigente e o novo STOP de GPR-01 estão no início deste documento.



27/10/2026. **C01.2b e migração mínima de teclas implementadas após aprovação direta do protótipo.** WASD/mira independente no player das4 cenas habilitadas;66/66 testes finais e smoke virtual na Nexus. Aceite físico pendente; C01.2c/C01.3–5/GPR-01 não iniciadas. [Relatório atual](../decisions/GPR-C01.2b_RELATORIO.md), [C01.2a histórica](../decisions/GPR-C01.2a_RELATORIO.md), [baseline](GPR_C01_BASELINE.md), [migração](GPR_CONTROL_MIGRATION.md). Perfil provisório Q/E/R e especialF11 preservado; sem AsuraX/RMB ou remap completo de WASD.

1. [Visão original fornecida pelo desenvolvedor](TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md): fonte principal da nova experiência. Nome real contém `(1)`; preservado sem renomear a fonte recebida.
2. [Plano WASD v1.1](TECH_GUY_GAMEPLAY_REWORK_EXECUTION_PLAN_v1.1_WASD.md): plano atualizado de execução/gates, com trechos históricos preservados e supersessão explícita pelo layout de três modulares + RMB.
3. [Ownership/lacunas](GPR_OWNERSHIP.md): ponte com o código existente, sem arquiteturas paralelas.
4. [Decisões e alternativas](../decisions/ADR-0006-GPR-DIRECAO-E-GATES.md): D01–14, propostas concretas para aprovação.
5. [Requisitos](GPR_REQUIREMENTS.md), [design técnico](GPR_TECHNICAL_DESIGN.md), [tarefas](GPR_TASKS.md), [validação planejada](GPR_VALIDATION_PLAN.md), [migração de controles](GPR_CONTROL_MIGRATION.md).
6. [Ledger mestre](../decisions/GAMEPLAY_REWORK_PROGRESS.md), [consolidado de fase](../decisions/GPR-00_PHASE_SUMMARY.md), [matriz canônica de resultados](../validation/01_MATRIZ_DE_VALIDACAO.md).

Os canônicos de sistemas, visão estável/ficção, arquitetura, boons, combate, run e HUD continuam em Docs; este pacote descreve apenas o delta proposto. GPR-C01 deve ser aprovado antes de implementar controles, e aprovado em feel antes de GPR-01. Nenhum plano futuro autoriza mapa, inimigos, save novo ou catálogo inteiro.
