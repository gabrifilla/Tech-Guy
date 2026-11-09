---
name: tech-guy-combat-unity
description: Use when implementing or tuning Unity C# combat, hit reactions, poise/stagger, knockup, boss telegraphs, Asura combos, bow projectiles, spear trajectories, input timing, movement, damage, or enemy control in Tech-Guy. Focus on ARPG feel with robust lifecycle and runtime physics.
---

# Tech-Guy: contratos de combate Unity

1. Leia `AGENTS.md` na raiz e [o mapa de contratos](references/combat-matrix.md). Preserve o contrato do sistema afetado e registre divergências sem mudar a intenção de design automaticamente.
2. Inspecione classes, assemblies, cenas, prefabs e consumidores reais antes de implementar. Confirme input, animação, NavMesh e pipeline de impacto existentes; nomes documentais não são API garantida.
3. Estenda a autoridade existente para HP, postura, reação, deslocamento, mana, cooldown e Asura. Preserve pausa/hitstop, identidade da execução, deduplicação, causalidade e cleanup; não simule launch/pull ignorando o controlador do alvo.
4. Verifique input rebindável e comportamento por classe de alvo, paredes/LOS, cancelamento, morte, troca de arma e fim de encontro conforme o escopo. Testes devem provar invariantes relevantes, sem espelhar a implementação.
5. Declare somente a validação executada com evidência. Para importação, testes e gameplay siga a skill `tech-guy-unity-acceptance` e a matriz canônica.
