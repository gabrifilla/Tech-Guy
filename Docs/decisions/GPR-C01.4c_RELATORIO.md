# GPR-C01.4c — Interact independente

28/10/2026 · CONCLUÍDA TECNICAMENTE; cumulativa/visuais finais em4d.

Auditoria de padrões: F já pertence ao finisher/retorno; G não colide com as outras ações padrão. Interact=G configurável; migração mantém finisher personalizado e resolve apenas conflitos em memória. Em modo direto LobbyInteraction e LobbyArsenal usam Interact, não Skill3; legacy permanece adaptado. UI/modal/pausa/results prevalecem, contexto válido reserva a entrada antes do cast; abrir/fechar interação exige soltura e não repete. Detalhes compactos do arsenal continuam pelo botão clicável, sem depender de Skill4/F11 no modo direto. Índices de skills e finisher intactos.

Objetos Item sob cursor recebem Interact por raycast/oclusão existentes e alcance configurável3,5m, sem navegação/ataque automático; inimigos não são tratados como pickup. Nenhum NPC novo ou cenário reconstruído. Prioridade de básico respeita intenção contextual de estação/arsenal.

Arquivos: GamePreferences.cs (fachada InteractionAction); LobbyInteraction.cs; LobbyArsenal.cs; AbilityHolder.cs (consulta de prioridade); CharControlScript.cs e DirectBasicInput.cs (interação de objetos); DirectMovementIntegrationPlayModeTests.cs (contrato histórico chamado ContextualR atualizado para G e skillR livre); DirectBasicInputPlayModeTests.cs (objeto/remap sem básico/mover).

2/2 PlayMode aprovados, job52b2d56a042149c3bee57b0d6e2b41fb: GPRSafetyContextualRHasOneOwnerAndCannotFallThroughOnClose e GPRInteractRebindCollectsOnlyNearbyItemWithoutBasicOrMovement. Requests/start/results em c01-4c. Job inicial1ac96f7bce5443e5b99d9bc3c782fcab perdeu a conexão no reload; recuperação indica succeeded/completed1, mas resultnull. Preservado e não somado como resultado detalhado; bateria final posterior2/2 é a evidência completa.

MCP real no projeto/Editor correto, compilação e testes reais. Skills locais visão/combate/aceitação aplicadas; gamedev ausentes no diretório atual. Sem Clear ou regravação de saves reais; preferências do teste de remap isoladas/restauradas. Sem novo playtest físico; capturas cumulativas registradas ao fim de4d. Próxima4d: consolidar Q/E/R+especial no holder/HUD, sem X, preservando quatro referências serializadas. Sem C01.5/GPR-01.

## Fechamento cumulativo posterior

As quatro subatividades foram concluídas tecnicamente.116 casos distintos finais aprovados por filtros cumulativos agregados; captura final1280x720 de Settings/menu/HUD/interação inspecionada. Pausa também é remapeável mantendoEscape seguro; detalhes/limites/falhas no [consolidado](GPR-C01.4_RELATORIO.md). Sem novo aceite físico, restart standalone ou C01.5/GPR-01.
