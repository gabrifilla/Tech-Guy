---
name: tech-guy-unity-acceptance
description: Use to validate Tech-Guy Unity gameplay, C# changes, boons, scenes, ScriptableObjects, prefabs, input, HUD, performance, or MCP/editor connectivity. Provide evidence from Unity Test Runner, Inspector, Console, and Play Mode and clearly distinguish CLI compilation from runtime validation.
---

# Tech-Guy: validação baseada em evidência Unity

1. Leia `AGENTS.md` na raiz, [o procedimento de Editor](references/editor-validation.md) e a matriz canônica antes da execução.
2. Confirme as ferramentas MCP realmente expostas, conexão, instância e projeto. Pacote instalado não comprova ponte ativa; não invente tool names nem fixe um resultado histórico de conexão como atual.
3. Inspecione estado inicial e execute somente verificações pertinentes à tarefa autorizada. Use filtros e cenas existentes; registre comando, revisão, resultados e limitações.
4. Separe código encontrado, CLI, importação Unity, EditMode, PlayMode, round-trip/Inspector, inspeção visual, gameplay e aprovação humana. Uma etapa não substitui as demais.
5. Se o Editor estiver inacessível, entregue a evidência estática e as verificações pendentes. Nunca declare execução Unity ou aceitação visual sem registro observado.
