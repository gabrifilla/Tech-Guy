# GPR-C01.4b — direção configurável do dash

28/10/2026 · CONCLUÍDA TECNICAMENTE, cumulativa final em4d.

Dois modos no Settings existente: Cursor (padrão) e Movimento. No segundo, lê o vetor normalizado das bindings direcionais no referencial da câmera de movimento. **Sem direção pressionada, usa o facing atual**, conforme correção explícita do desenvolvedor durante a atividade; a sugestão anterior de fallback cursor está superada. Direção capturada na ativação; velocidade/duração/cooldown/i-frames/mana/animação/colisão/containment/owner e buffer existentes não foram retunados.

GamePreferences.Options/Save/Read acrescentam enum validado e chave própria TechGuy.Settings.DashDirection; leitura inválida cai em Cursor. Apply grava, Cancel/Defaults usam draft. Pausa e menu compartilham toggle e persistência. CharControlScript expõe direção configurada ao DashScript; nenhum motor novo.

Arquivos: GamePreferences.cs; Dash/DashScript.cs; CharControlScript.DirectMovement.cs; ControlRemappingPanel.cs; PauseMenuUI.cs; MainMenuUI.cs; DirectBasicInputPlayModeTests.cs (teste de política/persistência/fallback e isolamento da chave).

MCP real no projeto/Editor6000.5.10f1.1/1 focado aprovado, job33fcf5173f0e4ad1b4fb21ed12bc54b9 (GPRDashModesUseNormalizedMovementAndFacingFallback);8/8 regressões de dash joba5b6d66f2db34fe9a49fd47069c4bd99. Artefatos c01-4b. Primeira consulta focada retornou0 testes antes da importação do novo caso: NÃO conta como PASS/cobertura. Resultado/request preservados. Nova compilação/filtro posterior executou1 caso real. Uma edição CLI intermediária teve erro de encoding, corrigida antes do filtro válido; não houve alteração de balanceamento. Console mantém dependência Tripo3D preexistente; sem Clear.

Skills locais visão/combate/aceitação aplicadas; gamedev solicitadas ausentes no diretório atual. Configuração validada por input virtual/reflexão da política e regressões do executor real; não teste físico novo. Capturas cumulativas de Settings serão registradas em4d, somadas à captura real do painel4a. Limites: restart standalone não executado. Próxima4c: Interact independente; F permanece finisher e G candidato sem conflito padrão. Sem C01.5/GPR-01.

## Fechamento cumulativo posterior

As quatro subatividades foram concluídas tecnicamente.116 casos distintos finais aprovados por filtros cumulativos agregados; captura final1280x720 de Settings/menu/HUD/interação inspecionada. Pausa também é remapeável mantendoEscape seguro; detalhes/limites/falhas no [consolidado](GPR-C01.4_RELATORIO.md). Sem novo aceite físico, restart standalone ou C01.5/GPR-01.
