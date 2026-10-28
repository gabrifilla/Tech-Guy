# GPR-C01.4a — rebinding e persistência

28/10/2026 · CONCLUÍDA TECNICAMENTE no recorte4a; regressão cumulativa ao fim de4d.

Layout aprovado: três skills Q/E/R e especial separado; X removido do design, índice histórico3 preservado. Baseline: checkout D:/Projetos/Gabriel/Tech-Guy, HEAD7950fd0 com mudanças locais3b preservadas, Docs.zip excluído e EditorBuildSettings modificado anteriores. MCP Tech-Guy@ec9dce4ca709f3be/Unity6000.5.10f1 confirmado, NexusLobby limpa/Edit Mode; Console inicial arquivada, erro Tripo3D preexistente sem Clear.

GamePreferences estende os nomes de ações da persistência v2, sem renumerar os oito índices antigos, sem gravar na leitura e sem substituir preferências legadas. Novas direções configuráveis e Interact; index6/Skill4 continua dado de compatibilidade, fora da lista de ações configuráveis. Leitura repara apenas valores inválidos/conflitantes em memória; Apply valida o conjunto inteiro antes de escrever. Migração determinística, sem reset de prefs/saves reais. Restore Defaults modifica apenas draft até Apply.

Painel existente ganhou lista rolável, labels efetivas, conflito explicado, teclado/Mouse0–4, Escape reservado, cancel e cancelamento por foco/troca de dispositivo. Menus cancelam captura ao perder foco; pausa já bloqueia gameplay. Apply não executa durante captura. Movimento direto consulta as quatro bindings usando o motor existente, sem novo backend/buffer.

Arquivos desta subatividade: GamePreferences.cs; ControlRemappingPanel.cs; MainMenuUI.cs; PauseMenuUI.cs; CharControlScript.DirectMovement.cs; DirectBasicInputPlayModeTests.cs (três novos contratos); DirectMovementIntegrationPlayModeTests.cs (round-trip isola novos nomes e assert legado limita-se aos oito índices antigos).

4/4 PlayMode aprovados, job bc7cff1765d74a8f8c03ac166fd9c96e: GPRConfigMigrationKeepsPersonalizedSecondaryAndHasThreeSkills, GPRConfigRoundTripConflictCaptureAndDefaults, GPRConfigReboundMovementAndMousePrimaryUseExistingOwners, GPRDirectSettingsRoundTripPreservesLegacyBindings. Requests/start/results em ../validation/artifacts/gameplay-rework/c01-4a. Compilação Unity solicitada/realizada. Preferências de testes isoladas/restauradas. settings.png real aberta/inspecionada: scroll dos controles; resolução875x199 limita avaliação visual. GameView maximizada temporariamente e restaurada; sem novo playtest físico. Editor final Edit Mode/NexusLobby.

Skills locais visão/combate/aceitação consultadas e aplicadas; referências previamente lidas nesta conversa. Skills gamedev da máquina antiga não encontradas no diretório atual. Sem novas dependências ou assets. Defaults/ownership de execução completos serão concluídos em4c/4d; Interact ainda sem consumidor nesta subatividade. Próxima:4b dash configurável, já autorizada. Não iniciar C01.5/GPR-01.

## Fechamento cumulativo posterior

As quatro subatividades foram concluídas tecnicamente.116 casos distintos finais aprovados por filtros cumulativos agregados; captura final1280x720 de Settings/menu/HUD/interação inspecionada. Pausa também é remapeável mantendoEscape seguro; detalhes/limites/falhas no [consolidado](GPR-C01.4_RELATORIO.md). Sem novo aceite físico, restart standalone ou C01.5/GPR-01.
