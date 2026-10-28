# Gameplay Rework — fontes e pacote GPR-00

27/10/2026. **C01.2b e migração mínima de teclas implementadas após aprovação direta do protótipo.** WASD/mira independente no player das4 cenas habilitadas;66/66 testes finais e smoke virtual na Nexus. Aceite físico pendente; C01.2c/C01.3–5/GPR-01 não iniciadas. [Relatório atual](../decisions/GPR-C01.2b_RELATORIO.md), [C01.2a histórica](../decisions/GPR-C01.2a_RELATORIO.md), [baseline](GPR_C01_BASELINE.md), [migração](GPR_CONTROL_MIGRATION.md). Perfil provisório Q/E/R e especialF11 preservado; sem AsuraX/RMB ou remap completo de WASD.

1. [Visão original fornecida pelo desenvolvedor](TECH_GUY_GAMEPLAY_MUTACOES_SINERGIAS_E_SKILLS_MODULARES(1).md): fonte principal da nova experiência. Nome real contém `(1)`; preservado sem renomear a fonte recebida.
2. [Plano WASD v1.1](TECH_GUY_GAMEPLAY_REWORK_EXECUTION_PLAN_v1.1_WASD.md): cópia integral do arquivo de Downloads, mesmo SHA256; roteiro de execução/gates. Decisão posterior WASD/mouse/QERX prevalece sobre exemplos QWER da visão.
3. [Ownership/lacunas](GPR_OWNERSHIP.md): ponte com o código existente, sem arquiteturas paralelas.
4. [Decisões e alternativas](../decisions/ADR-0006-GPR-DIRECAO-E-GATES.md): D01–14, propostas concretas para aprovação.
5. [Requisitos](GPR_REQUIREMENTS.md), [design técnico](GPR_TECHNICAL_DESIGN.md), [tarefas](GPR_TASKS.md), [validação planejada](GPR_VALIDATION_PLAN.md), [migração de controles](GPR_CONTROL_MIGRATION.md).
6. [Ledger mestre](../decisions/GAMEPLAY_REWORK_PROGRESS.md), [consolidado de fase](../decisions/GPR-00_PHASE_SUMMARY.md), [matriz canônica de resultados](../validation/01_MATRIZ_DE_VALIDACAO.md).

Os canônicos de sistemas, visão estável/ficção, arquitetura, boons, combate, run e HUD continuam em Docs; este pacote descreve apenas o delta proposto. GPR-C01 deve ser aprovado antes de implementar controles, e aprovado em feel antes de GPR-01. Nenhum plano futuro autoriza mapa, inimigos, save novo ou catálogo inteiro.
