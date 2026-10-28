# GPR-C01.4 — fechamento consolidado

28/10/2026 · **CONCLUÍDA TECNICAMENTE; aguardando playtest/aceite humano da fase.** STOP após C01.4. C01.5/GPR-01 não iniciadas nem autorizadas.

## Resultado

Implementado o modelo final **3 skills modulares Q/E/R +1 especial permanente separado RMB**, substituindo expressamente todo planejamento anterior de quatro slots Q/E/R/X. Arrays/GUIDs/índices/executores antigos não foram removidos/renumerados. Asura, Q/E/R, básicos, dano, cadência, recovery, hitstop, cancel e balanceamento preservados. Não há skillX nem aquisição/substituição modular nova.

| Ação | Padrão | Configuração |
|---|---|---|
| Movimento | W/A/S/D | Cada direção remapeável; vetor normalizado/câmera |
| Mira | Cursor | Independente do movimento |
| Básico | Mouse esquerdo | Um pedido por clique; hold ChargedShot preservado |
| Especial da arma | Mouse direito | Separado dos três slots; Asura única na Manopla |
| Skills modulares1/2/3 | Q/E/R | Remapeáveis; referências/identidades internas preservadas |
| Dash | Espaço | Cursor padrão ou Movimento; sem vetor no modoMovimento usa facing |
| Interact | G | Independente, remapeável, prioridades de contexto/UI |
| Finisher/retorno | F | Distinto de Interact; contrato atual preservado |
| Pausa | Escape | Remapeável; Escape continua saída segura |
| X | Nenhuma ação padrão | Nenhuma quarta skill ou placeholder |

Os padrões não sobrescrevem personalizações. No perfil lido ao final, o modo salvo já eraMovimento; o padrão de perfis novos permaneceCursor. Pausa pode receber outra tecla/botão; Escape nunca deixa o jogador preso no Settings. X é uma tecla livre, sem ação/quarta skill atribuída pelo layout.

## Entregas sequenciais

- [4a — rebinding/persistência](GPR-C01.4a_RELATORIO.md): painel existente com scroll, conflitos explicados, draft/Apply/Cancel/Defaults, captura segura, migração sem gravação na leitura.4/4 focados inicialmente; cumulativa posterior cobre os ajustes de segurança.
- [4b — dash](GPR-C01.4b_RELATORIO.md): Cursor/Movimento, diagonal normalizada e fallback por facing determinado diretamente pelo desenvolvedor durante a atividade.1/1 política+8/8 dash.
- [4c — Interact](GPR-C01.4c_RELATORIO.md): G padrão auditado sem conflito padrão; F finisher preservado; estações/pedestais/objetos, prioridades e soltura.2/2 focados.
- [4d — ownership/HUD](GPR-C01.4d_RELATORIO.md): especial em um único holder index3, três skills, nenhuma quarta modular; botões laterais, pausa e textura UI.2/2 kits/HUD,3/3 segurança e cumulativa abaixo.

## Arquitetura e persistência

GamePreferences continua a fachada única. Enum de ações foi estendido por anexação (0–7 preservados); Skill4/index6 continua dado histórico oculto, não ação modular configurável. Chaves DirectBinding.v2 por nome foram estendidas deliberadamente para novas ações, sem regravar legado ou salvar ao ler; não há backend paralelo. Valores personalizados válidos têm prioridade, conflitos/ausências são reparados determinística e idempotentemente em memória. Apply valida antes de gravar. BotõesMouse0–4 e teclas suportadas pelo Input System são aceitos; Escape reservado para fechar/cancelar com segurança. A aba Controles restaura só bindings/dash do draft.

Movimento/dash usam o motor e clocks existentes. Interact lê contexto antes de consumir, invalida comandos não comprometidos ao abrir/fechar e não compartilha uma binding com skills/finisher. Bow/Spear apenas encaminham a quarta habilidade que já existia ao canal especial; nenhuma mecânica nova foi inventada. Referência históricaR/índice3 mantém recursos, cooldown, snapshots/boons/estatísticas. Nenhum asset foi reescrito por causa da decisão de três slots.

## Validação executada

MCP real confirmou D:/Projetos/Gabriel/Tech-Guy, instância Tech-Guy@ec9dce4ca709f3be, Unity6000.5.10f1. Testes via ponte MCP HTTP com requests/start/results; ferramentas MCP nativas também usadas no fechamento para reconfirmar projeto, Console e Edit Mode. Nenhum CLI build foi tratado como prova Unity. Console nunca foi limpa por comando.

Baseline Git: HEAD7950fd0 com alterações locais3b, Docs.zip excluído e EditorBuildSettings.asset modificado, preservados. Nenhuma cena, prefab, ScriptableObject, meta ou GUID preexistente alterado pela fase. C# recompilado/importado no Editor; novos arquivos de evidência/relatórios ficam fora de Assets. Editor terminou NexusLobby limpa/Edit Mode, sem compilação; tamanho customizado de captura removido e Game View voltou ao índice0/Free Aspect.

Skills aplicadas: tech-guy-game-vision, tech-guy-combat-unity e tech-guy-unity-acceptance, com suas referências. As skills gamedev solicitadas não estavam disponíveis no diretório de skills deste usuário; não foram instalados pacotes nem declarado seu uso. Arquitetura real e contrato aprovado governaram a implementação.

**116 casos distintos aprovados na seleção cumulativa final**, sem ignorados.115 na rodada completa e9 UI após correção final, sendo1 caso novo: não apresentar como uma única suíte global116/116.

| Filtro final selecionado | Resultado | Duração | Job MCP |
|---|---|---|---|
| final-input | 31/31 | 25.557344s | `0fae980b7d714c92a71d7986eea389e2` |
| final-ui | 9/9 | 6.897800s | `de0fc68fa4854306b1332ef0ef5dbc32` |
| final-movement-regression | 23/23 | 8.885509s | `fb119a887508473e808aafc55833587d` |
| final-combat-regression | 30/30 | 25.073967s | `a503bb6ff5ca4979b068af69919faca2` |
| final-dash | 8/8 | 1.846354s | `141ee207ae734529804824e65c0310f0` |
| final-edit | 15/15 | 0.894549s | `47faa33cc37e43ec9e927e599f829141` |

Reprodução: `python Tools/GameplayObservation/c01_2c_validation.py <request.json> <resultado.json>`, um job por vez; requests/start/results em [c01-4d](../validation/artifacts/gameplay-rework/c01-4d). Mapeamento exato de nomes e asserts está nos resultados. Cobertura: bindings atuais/customizadas/defaults/conflitos/cancel/dispositivo, três skills/especialF11/X, persistência/reload, dash/facing, Interact/finisher, LMB/ChargedShot/Spear, modal/foco/pausa e contratosGI/dano.

Resultados intermediários preservados: consulta4b retornou0 testes antes da importação do caso (não conta como PASS); conexão perdida na primeira consulta4c e recuperação sem result detalhado; primeiro focado4d1/2 por preparação de mana máxima inválida na fixture; novo teste de Mouse4 nos resultados0/1 antes da correção. Não houve redução de assert para esconder falhas. O erro transitório de encoding de uma edição CLI foi corrigido antes dos filtros válidos.

## Gameplay e evidência visual

Capturas reais abertas/inspecionadas em1280x720:

- [Settings da pausa — topo](../validation/artifacts/gameplay-rework/c01-4d/runtime/settings-top.png).
- [Settings da pausa — fim da lista](../validation/artifacts/gameplay-rework/c01-4d/runtime/settings-bottom.png).
- [Settings do menu](../validation/artifacts/gameplay-rework/c01-4d/runtime/main-menu-settings.png).
- [HUD Q/E/R + Asura separada](../validation/artifacts/gameplay-rework/c01-4d/runtime/whiff/nexus-asura-rmb.png).
- [Interação G após reconstrução da UI](../validation/artifacts/gameplay-rework/c01-4d/runtime/final-interact/interaction.png).

Inputs/foco virtuais, não playtest físico. Nexus publicada: estação02/FRAGMENTO abre/fecha comG, mesmo comLMB simultâneo,0 básicos/0 casts. Cenário whiff:3 básicos0/1/2 e1 Asura; outro cenário junto ao alvo registrou1 básico/1 Asura, sem inferir outras aceitações dos nomes dos frames. Energia100 somente em memória. Valores de mana finais têm regeneração; prova de custo único vem dos testes sem regen. A leitura original de textura nula foi corrigida no guard e verificada por perda/recriação de recurso e captura posterior legível.

## Integridade, Console e limites

Snapshots de bindings/modo/moedas/seleção/hash do resumo eram iguais antes/depois da janela automatizada, em runtime/prefs-before.json e prefs-after.json. Snapshot posterior nativo mostra bindings salvas/modoMovimento, moedas+19 e resumo diferente; autoria posterior não verificada, estado atual preservado sem rollback. Não alegar imutabilidade de todo o perfil durante uma sessão compartilhada nem auditoria completa de saves.

Console inicial/final arquivada: permanece erro preexistente Tripo3D local e avisos de keys incompletas. Erros de preparação/limpeza da observação foram corrigidos e documentados em4d; nenhum Clear. Cena/prefab/SO/meta/GUID sem diff novo desta fase; mudanças prévias em EditorBuildSettings e Docs.zip permanecem. `git diff --check` executado. Não houve restart standalone/build/export, benchmark/crowd ou novo teste físico de dispositivos. Nenhum aceite humano de C01.4 foi inferido dos testes/capturas.

## Arquivos e documentação

Código alterado por4: GamePreferences.cs, DashScript.cs, AbilityHolder.cs, CharControlScript.cs/DirectMovement.cs/DirectBasicInput.cs, ControlRemappingPanel.cs, PauseMenuUI.cs, MainMenuUI.cs, LobbyInteraction.cs, LobbyArsenal.cs, PlayerHUD.cs, TutorialHUD.cs, RunResultsPresenter.cs. Testes: DirectBasicInputPlayModeTests.cs, DirectMovementIntegrationPlayModeTests.cs, RunResultsPlayModeTests.cs; BasicAttackR1MovementFluidityTests.cs já tinha correção3b preservada. Artefatos c01-4a/b/c/d e cinco relatórios individuais/consolidado.

Atualizados requisitos, desenho, tarefas, validação, ADR-0006, ledger, matriz, ownership/migração, visão/roadmap/índicegameplay e prioridades; fontes históricas são mantidas com nota explícita de superação. Mudanças3b preexistentes não são atribuídas novamente a4.

## Checklist físico curto

1. Conferir Q/E/R, RMB Asura, ausência de X/F11 e finisherF em combate; básicos/cliques e ChargedShot mantêm feel anterior.
2. Remapear uma skill, especial e direção; tentar conflito, cancelar comEsc, perder foco e desconectar/reconectar dispositivo durante captura; aplicar e reabrir Settings.
3. Comparar dashCursor com Movimento; parado e cursor oposto ao corpo, Movimento deve seguir o olhar. Conferir diagonal/parede sem distância extra.
4. UsarG no Nexus/objetos e F na finalização; fechar pausa/results com tecla/botão lateral mantido, depois soltar e dar comando novo: sem replay nem primeiro comando perdido.
5. Restaurar padrões no draft, cancelar sem salvar, depois conferir persistência após fechar/reabrir o aplicativo. Essa reinicialização física ainda não foi executada pela automação.

## Próxima atividade e gate

PropostaC01.5: integração/feel físico, regressões complementares em encontros/duas sessões, revisão visual/prompts e desempenho proporcional; não novas mutações/skills ou retuning automático. Observação de básicos rápidos permanece refinamento futuro. **STOP após C01.4; revisão/aceite e autorização explícita antes de C01.5 ou GPR-01.**
