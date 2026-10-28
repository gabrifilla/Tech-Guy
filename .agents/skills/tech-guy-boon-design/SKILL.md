---
name: tech-guy-boon-design
description: Use when auditing, creating, changing, balancing, cataloging, testing, or implementing Tech-Guy run boons, Mutations, Augments, Protocols, Resonances/Duos, Mythics, reward offers, combat synergies, or Tech-Guy boon ScriptableObject authoring in Unity. Preserve the existing rework and avoid invalid combinations.
---

# Tech-Guy: boons e builds emergentes

1. Leia `AGENTS.md` na raiz e [as fontes de catálogo e gates](references/catalog-and-gates.md). O contrato e o estado documental vivem em `Docs/systems/04_BOONS.md`; não recrie catálogos de texto paralelos.
2. Inspecione definições, asset, executores, input, oferta e consumidores reais antes de alterar. Item serializado, conteúdo habilitado e opção elegível numa run são fatos distintos.
3. Preserve IDs, limites, requisitos/capabilities, escopo, causal root, budget, recibos, RNG de recompensa e teardown existentes. Não habilite conteúdo desligado ou Mythics sem os gates e a autorização aplicáveis.
4. Ao propor transformação, descreva substituição, comando, decisão nova, risco, custo/cooldown e consequência por alvo; confirme o texto do card contra o executor. Não substituir uma mudança de comportamento por atributo puro.
5. Registre a mudança e evidências nas seções existentes de boons/estado/validação. Separe implementação, testes Unity observados, visual, gameplay e aprovação humana. Propostas futuras não autorizam implementação.
