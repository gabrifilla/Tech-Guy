# ADR-0005 — Captura e feedback visual antes de polimento por IA

- **Status:** IMPLEMENTAÇÃO DO WORKFLOW AUTORIZADA pelo desenvolvedor em 18/10/2026; demonstração técnica da manopla executada. Game feel e direção artística não aprovados por esta decisão.
- **Contexto:** código/Console não demonstram legibilidade e peso dos impactos. O inventário encontrou previews, APIs públicas de combate e eventos suficientes para uma extensão coesa.
- **Decisão:** estender `CombatPolishPreview` com captura real finita, metadados e observação temporal; reutilizar MCP e Unity Test Framework; comparação offline com condições verificadas. Detalhes e limites na [fonte de captura](../validation/02_CAPTURA_E_INSPECAO_VISUAL.md), resultados na [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md).
- **Consequência:** baseline → inspeção de imagens/eventos → prioridades → correção especificamente autorizada → recaptura equivalente → comparação → decisão humana. Sem mudanças de gameplay durante a infraestrutura.
- **Não fazer:** afirmar visão sem abrir imagens, somar hooks como dano independente, comparar câmeras diferentes sem expor a variável ou interpretar diferença de pixels como melhoria/aprovação. Sequências PNG + JSONL são a evidência temporal inicial; vídeo codificado permanece extensão futura.
