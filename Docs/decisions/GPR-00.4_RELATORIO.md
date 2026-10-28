# GPR-00.4 — Pacote de planejamento rastreável

- Data26/10/2026 (America/Sao_Paulo), fase GPR-00/atividade00.4.
- Status COMPLETED documental; design AWAITING_HUMAN_APPROVAL; evidência NOT_APPLICABLE.
- Ambiente/base/escopo: [00.1](GPR-00.1_RELATORIO.md). Entradas: visão integral, plano WASD, ADR0006 e ownership. Fora: runtime/assets/scenes/saves.

## Objetivo e aceite

Entregar requisitos/design/tarefas/risco/validação da fatia pequena, com GPR-C01 antes de GPR-01. Critério satisfeito: GPR-R01–17 ligam decisões, tarefas e V00–16; cada tarefa tem dependência e aceite/rollback. Não declarar pronta a implementação antes de aprovação dos parâmetros/alternativas.

## Arquivos e realizado

Criados `Docs/gameplay/GPR_REQUIREMENTS.md`, `GPR_TECHNICAL_DESIGN.md`, `GPR_TASKS.md`, `GPR_VALIDATION_PLAN.md`, este relatório; ledger/matriz atualizados. A convenção Docs/gameplay foi fornecida pelo desenvolvedor durante a auditoria; sistemas existentes continuam canônicos e são referenciados, sem duplicar todo o catálogo. GPR_CONTROL_MIGRATION.md é dependência a finalizar em00.5.

Incluídos escopo de delta futuro por owner, estratégia transacional e rollback seletivo, composição Basic/Conductor, desenho visual reversível, gates/ordinais reais e registro de riscos K01–10. Sem12 boons, meta, mapa ou rework inimigo.

Skills aplicadas: quatro skills locais Tech Guy; unity-csharp-scripting, unity-scriptableobjects, unity-input-system, input-systems, unity-animation, unity-navmesh, game-feel, game-ui-ux e unity-mcp-orchestrator. Aplicadas como critérios de arquitetura/validação e preservação, sem usar exemplos como APIs aprovadas.

## Evidências e pendências

| Checagem | Resultado | Limite |
|---|---|---|
| Rastreabilidade requisito→tarefa→cenário | PASS de elaboração | Checagem de links/IDs no fechamento da fase |
| Baseline técnico | 15/15 EditMode e4/4 PlayMode | Filtros limitados, não suite global |
| Baseline visual/físico pré-mudança | NOT RUN | Obrigatório C01.1 antes de código; pipeline e condições definidos; nenhuma imagem nova inspecionada |
| Feel/visual/performance do delta | NOT RUN | Sem implementação e sem números de tuning inventados |

Não há mudança comportamental demonstrada, apenas design e contratos atuais testados. D01–14 aguardam desenvolvedor; risco de reset de recarga e diferença finisher/Asura documentados. Problemas externos e dívidas históricas permanecem separados.

## Passagem

Próxima atividade GPR-00.5: fechar matriz de ações/conflitos, arquitetura de input, migração e roteiro humano; atualizar referências do pacote. Fase ainda não fechada. Somente essa continuação documental está autorizada.
