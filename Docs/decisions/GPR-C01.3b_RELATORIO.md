# GPR-C01.3b — integração de input, RMB e correções de validação

> Histórico de3b: qualquer orientação futura sobre quatro slots/X foi superada pela revisão finalC01.4 — trêsQ/E/R + especial separado. Evidências3b abaixo permanecem intactas. Ver [consolidado4](GPR-C01.4_RELATORIO.md).


28/10/2026 · America/Sao_Paulo · **CONCLUÍDA TECNICAMENTE — gate de C01.4 satisfeito.** Aceite humano anterior às últimas correções registrado separadamente. Não é aprovação da suíte global nem de features C01.4.

## Escopo e aceite humano

C01.3a foi aceita tecnicamente. O desenvolvedor depois testou fisicamente WASD, básicos, Q/E/R e Asura/RMB, aprovando o funcionamento/feel. Relatou básicos ligeiramente rápidos: refinamento futuro, sem autorização para mudar cadência. Especial/Ultimate atual preservado; nenhum especial novo Bow/Spear, X, boon ou balanceamento introduzido.

Autorização posterior permitiu C01.4a–4d somente após resolver o gate técnico. O pedido mais recente foi corrigir as falhas. Esta entrega encerra essas correções de C01.3b; C01.4 não foi implementada, C01.5/GPR-01 não iniciadas. A autorização condicional de C01.4 já existe; não exigir nova autorização para iniciar4 depois deste gate.

## Ambiente e integridade

Checkout `D:/Projetos/Gabriel/Tech-Guy`, HEAD base `7950fd06d858a9c03e01b0c34983d127d5243c3b`, com mudanças locais. MCP HTTP real confirmou instância `Tech-Guy@ec9dce4ca709f3be`, Editor6000.5.10f1, NexusLobby limpa/Edit Mode no início e no fim. A tentativa inicial histórica falhou WinError10061; o usuário iniciou a ponte e a conexão foi restabelecida. Pacote instalado nunca foi usado como prova de conexão.

Git inicial da atividade tinha EditorBuildSettings.asset modificado e Docs (2).zip não rastreado. Durante as revisões Docs.zip passou a constar excluído e Docs (2).zip deixou de constar; nenhum comando desta atividade removeu/restaurou esses arquivos. Preservados. As alterações de AbilityHolder/testes/docs anteriores à rodada de correções também foram preservadas. Não foram alterados assets autorados, cenas, prefabs, GUIDs, rigs, animações, dano ou parâmetros de combate. Nenhum reset de preferências/progresso foi executado; nenhuma aplicação do Settings foi feita.

## Diagnóstico RMB/F11 e decisão vigente

`WeaponSpecialBinding` lê o canal secundário existente; Mouse1 traduz corretamente para `Mouse.current.rightButton`. O fluxo exige modo direto, arma Gauntlet e AsuraBurst equipada e usa somente o holder/executor histórico index3. Estado/cooldown/mana/energia/continuações continuam únicos. A barreira UI impede RMB sobre HUD/gráficos, com prioridade contextual preservada.

O usuário confirmou que no incidente original estava com Manopla, energia cheia e cursor sobre inimigo. A causa física daquele incidente **não pôde ser confirmada retrospectivamente**. O aceite físico posterior inclui RMB funcional. Não atribuir a falha original ao HUD, à arma Spear encontrada na inspeção ou aos dispositivos virtuais dos testes sem evidência.

O desenvolvedor autorizou retirar F11. `AbilityHolder.IsAbilityKeyPressed(3)` agora aceita somente o canal secundário no modo direto de Manopla/Asura, preservando a preferência legada gravada e os caminhos das demais armas. Não há executor novo nem duplicação em X. O teste verifica F11 sozinho sem ativação/consumo e RMB+LMB+F11 com uma única ativação/custo.

Leitura histórica deste perfil: secundário legado324/Mouse1; Skill4 legado114/R; sem overrides diretos. A migração em memória oferece Skill4F11 e especialMouse1; F11 deixou de ser alias executável da Asura. Snapshot final repete os oito valores de binding legada e ausência de overrides, sem regravação. Arma selecionada final0; a correção não grava WeaponLoadout.Select. Não há dump completo antes/depois de todos os saves, portanto não declarar auditoria total de persistência.

## Causas corrigidas com reprodução

1. **Fixture criava dispositivos desabilitados.** DirectInputScope tinha AddDevice em inicializadores de campos, antes de configurar IgnoreFocus/roteamento de Editor. A implementação instalada do Input System aplica a política de background imediatamente em AddDevice. Em segundo plano os virtuais podiam nascer desabilitados. Baseline20 reproduziu18 falhas, incluindo `Virtual keyboard must be enabled before injection`/enabledFalse. A criação foi movida para depois da política; as asserções de gameplay não foram reduzidas. RunResults não configurava essa política; agora configura antes de criar dispositivos e restaura política/dispositivos/current no teardown. A prova nova exige enabled, W/botão/posição persistentes após updates reais.
2. **Primeiro comando novo após soltura era descartado.** GameplayInputReady reconhecia todos os controles soltos, mas mantinha `_waitForAttackRelease` e `_directMovementRelease`. Um press no frame seguinte podia exigir uma segunda soltura. Dois novos testes reproduziram0/2: zero ataques e zero deslocamento. A produção agora libera ambos os bloqueios locais no momento da soltura global; o frame observado continua bloqueado por `_gameplayReleaseFrame`. Não permite replay de input mantido, não altera clocks/buffers/cadência.
3. **Fixture agia depois da recuperação observada.** No teste CadencedRContinuation, retornar de coroutine aninhada podia atravessar a transição para espera. Nesse estado o movimento correto é imediato, contrariando a expectativa de intenção pendente. A ação e todas as mesmas asserções imediatas agora executam por callback no frame observado de recovery>=.85. A observação posterior da continuação permanece no runner real; nenhum estado/tempo de combate é atribuído pelo teste.
4. **Destino de evidência de combo fixo em3a.** O teste agora usa SessionState de fase. A medição nova que atingiu o caminho antigo foi copiada para correcao e o arquivo histórico, comprovadamente sem mudança anterior, foi restaurado ao conteúdo Git original. Diff de artefatos3a vazio no fechamento.

## Testes realmente executados

Todos pelo Unity Test Framework via MCP, sequencialmente, com requests/start/results preservados. Runner: `python Tools/GameplayObservation/c01_2c_validation.py <request.json> <resultado.json>`. Compilação/importação solicitadas por refresh_unity e estado ready/is_compilingFalse confirmado. Evidências finais em [correcao](../validation/artifacts/gameplay-rework/c01-3b/correcao).

**105 casos distintos finais aprovados; zero falhas/ignorados nos seis filtros finais.**

| Filtro | Resultado | Duração | Job MCP |
|---|---|---|---|
| final22 | 22/22 | 20.695194s | `78cc6893b9fa428d8c34bbd6b8d27909` |
| movement-regression-final | 23/23 | 9.821366s | `190984b4654540ee8b42e7cfb8951c4f` |
| contexts-final | 7/7 | 6.986580s | `60e25a30d6cd458eac63de7bbaefa885` |
| combat-regression-final | 30/30 | 24.382800s | `cd431aa8455c4ee0882a256536a06d77` |
| dash-final | 8/8 | 1.769948s | `d24d0a56810245b785f823146f25dcc1` |
| edit-final | 15/15 | 0.849245s | `77277d7ab901459594c54686b2985011` |

22 cobre19 anteriores mais entrega virtual e dois casos de primeiro input após soltura. Demais filtros cobrem movimento, UI/results/reload/pausa/foco, GI/legado/Asura/Bow/Spear/recovery, dash/cancel/imunidade e contratos EditMode. Não suíte global.

Falhas preservadas: revisões iniciais de19 em12/19,1/19,8/19; gate20 com2/20 e teclado desabilitado; release-antes0/2; primeira correção20 com19/20 e combate29/30; demais históricos em gate/ e raizc01-3b. O isolado restantes-diagnostico2/2 antes da correção de soltura não foi usado para encerrar a causa. Gate anterior: básicos1/19, movimento23/23, contextos4/7. Resultados verdes finais decorrem das correções e dos casos negativos reproduzidos, não de apagar resultados ou repetir até passar.

## Runtime e Game View

Após as correções, driver publicado/NexusLobby executado via MCP: `Player - Nexus`, Gauntlet equipada pela API somente em memória; teclado/mouse/foco virtuais e100 de energia concedida só para teste. Em5,154491s:3 básicos0/1/2 e1 Asura RMB, interval/busy0,2s. Mana inicial1500, custo147, final1488,05786 com regeneração; final não representa custo líquido. Nenhuma concessão persistida. Driver e proveniência arquivados em correcao/runtime.

Quatro PNG reais gerados; nexus-asura-rmb.png aberta e inspecionada (460x141, baixa resolução, sem aprovação de arte). São observação do jogador publicado com input virtual, não novo playtest físico nem prova de HP na Nexus vazia; contato/dano/GI vêm dos testes de combate. A correção exata do frame pós-soltura foi verificada nos novos testes com loop real. Políticas/dispositivos restaurados; Play Mode encerrado, NexusLobby dirtyFalse e SessionState temporário removido.

## Console e limitações

Console inicial/final consultada sem Clear. Permanece erro preexistente de dependência Tripo3D em caminho local da máquina antiga, fora do escopo das correções. Console final também tem aviso legado de keys array menor que abilities; durante fixtures houve avisos de câmera/clickEffect/DashScript/controller ausentes. Não declarar Console sem erros nem resolver dependência removendo pacote nesta tarefa. Isso não impediu a compilação e os filtros registrados.

Sem benchmark/crowd novo, auditoria global de referências, build standalone/restart do aplicativo ou novo playtest humano pós-correção. O aceite físico recebido refere-se à versão testada pelo desenvolvedor antes da pequena correção de soltura, não é inferido dos105 testes. A causa física original RMB permanece retrospectivamente não confirmada; causa das falhas virtuais e primeiro input pós-soltura foi demonstrada separadamente.

## Arquivos alterados e finalidade

- `Assets/_Project/Scripts/Abilities/AbilityHolder.cs`: remoção autorizada do alias legado da Asura direta (alteração anterior, preservada).
- `Assets/_Project/Scripts/Characters/Player/Actions/CharControlScript.DirectMovement.cs`: sincroniza bloqueios locais quando o gate global observa soltura.
- `Assets/_Project/Scripts/Tests/PlayMode/DirectMovementIntegrationPlayModeTests.cs`: ordem correta de configuração/criação dos virtuais.
- `Assets/_Project/Scripts/Tests/PlayMode/RunResultsPlayModeTests.cs`: política de foco isolada/restaurada e current devices restaurados.
- `Assets/_Project/Scripts/Tests/PlayMode/DirectBasicInputPlayModeTests.cs`: teste F11 anterior; três regressões de entrega/soltura; diagnósticos de readiness e saída de combo por fase.
- `Assets/_Project/Scripts/Tests/PlayMode/BasicAttackR1MovementFluidityTests.cs`: callback de teste atua na janela observada, preservando asserções.
- Este relatório, ledger, ADR-0006, requisitos/design/tarefas/plano e matriz: estado vigente/aceite/gate e evidência, distinguindo histórico.
- `Docs/validation/artifacts/gameplay-rework/c01-3b/`: requests, resultados, Console, snapshots, trace e capturas. Novos outputs de correção isolados em correcao/; nenhum GUID novo.

## Skills e próximo passo

Consultadas/aplicadas skills locais de visão, combate, aceitação Unity e, nas preparações anteriores da atividade, boon-design, com referências. Gamedev listadas na autorização não foram encontradas no diretório de skills deste usuário; não alegar uso das skills da máquina antiga. Aplicados preservação de identidade/cadência/owners, evidência Unity real e separação humano/técnico.

**Gate técnico3b liberado.** Próxima atividade já autorizada: C01.4a, seguida de4b/4c/4d e reports individuais/consolidado. Nenhuma4 entregue por esta correção. F hoje pertence ao finisher; default de Interact exige decisão conforme autorização, sem sobrescrever. X não pode duplicar Asura ou inventar skill. C01.5/GPR-01 continuam dependentes de aprovação posterior. Cadência, especiais e balanceamento continuam preservados.
