# GPR-01.2c — ciclo de vida do tornado após concluir sala

29/10/2026 · **VALIDADO TECNICAMENTE NO ESCOPO FOCADO.** Sem regressão bloqueadora nos 32 casos executados; gate técnico para iniciar GPR-01.3 satisfeito. Aceite humano de GPR-01.2b recebido no pedido desta atividade: funcional e divertido. Tuning de velocidade, tamanho, mira, alcance, dano e cadência explicitamente diferido.

## Preparação e diagnóstico confirmado

Projeto real confirmado pelo Unity MCP: `D:/Dev/Personal/My_games/Tech-Guy`, Unity `6000.5.10f1`, instância `Tech-Guy@fccafb2b47cd7671`; branch `gameplayOverhaul`, HEAD `653db8ba0874f53141c70e88227af76ba0dd07ae`, árvore inicialmente limpa. O worktree do chat em C: estava no HEAD antigo `67d8065` e não foi usado para implementar. Cena inicial NexusBlockout limpa, Edit Mode. Assets/cenas/prefabs/configurações/metas e documentação histórica inventariados em `gpr-01-2c/baseline-hashes.json`. Nenhum save, preferência ou progresso humano foi intencionalmente regravado.

Lidos AGENTS.md, mapa canônico, visão/pilares, combate/boons, requisitos/design/tasks, fatia vertical, matriz, relatórios01.1/01.2/01.2b e contratos atuais de root/budget/receipt/fila. Skills aplicadas: `router`, `unity-csharp-scripting`, `unity-mcp-orchestrator` e as quatro locais `tech-guy-game-vision`, `tech-guy-combat-unity`, `tech-guy-boon-design`, `tech-guy-unity-acceptance`, com referências. Nenhuma delegação.

Reprodução usa FirstSector, ProgressionDirector, inimigos e tickets de produção; aquisição de A é assistida e explicitamente não demonstra oferta natural. O encontro ativo emite. A última morte aciona a fronteira normal, revoga raízes e abre recompensa. Enquanto o mapa/recompensa está aberto, **a entrada está indisponível por regra legítima de UI**. Depois de escolher a recompensa e comprometer o destino seguinte, permanecendo fisicamente na sala concluída, os três ataques seguintes são aceitos e chegam ao scheduler. O terminal não emite porque `CommitTitanicTerminalContact` exige `IsOpen`; em Traversal não há raiz. Não era falha do pool nem ausência do finalizador.

## Correção restrita e garantias

O callback real após o batch de contato oferece uma apresentação de vento quando o contato não é de combate, exclusivamente em Traversal de uma sessão viva cujo nó atual já esteja Completed. Valida snapshot Basic/A/terminal, owner, arma, execução viva, pose finita e deduplica a execução. Não modifica o requisito global de HP confirmado nem o scheduler. Reutiliza TitanicWork, constantes de movimento e pool/lease de TitanicFistsVisual; não duplica o runtime de dano.

Trabalho cosmético não tem ProtocolExecution, não cria raiz, reserva, impacto, receipt, estatística, mana, Asura ou reward. Nunca entra no sweep de combate/fila de dano. LOS visual existente impede atravessar sólidos no segmento. Mantém sessão/nó/destino/revisão; entrar no próximo encontro, reconfigurar, encerrar, morrer/disable, trocar arma ou mudar de cena invalida a apresentação. CloseEncounter agora libera tornado e VFX imediatamente, além das raízes. Pausa/hitstop suspendem o movimento. Nenhuma mudança de arquitetura, ownership de combate, persistência ou tuning.

Fontes modificados em `Assets/_Project/Scripts`: `Characters/Player/Actions/CharControlScript.BasicImpact.cs`, `Characters/Combat/Protocols/ProtocolRuntime.TitanicFists.cs`, `ProtocolRuntime.Status.cs`. Novo `Tests/PlayMode/TitanicRoomClearPlayModeTests.cs` e meta gerada no Unity. Nenhum asset de cena/prefab ou dado autorado modificado.

## Testes, tentativas e evidência

| Execução Unity Test Framework via MCP | Resultado | Job |
|---|---|---|
| Primeiro pedido antes de importar arquivo novo | 0 testes; não é PASS válido | a0bcbf927f354b7c9245ce4375313952 |
| Duas preparações iniciais pararam na UI legítima de recompensa | 0/1 cada; diagnóstico de fixture, sem alteração da produção | 21405a3f1f954ba8a5c399746a5d81c4 / 99d9b0c5031f4389b750c1d08f1f0769 |
| Vermelho válido, terminal aceito fora de combate | 0/1, ausência de vento confirmada | 61d5594468b447afa0698d0b9b747865 |
| Verde inicial, mesma asserção central | 1/1 | 9325ace81b9c4de48f8c969e67688720 |
| Focados finais: 27 comparadores existentes + 5 lifecycle de produção | **32/32, zero ignorados**, 53,8347903s | 402623b4c6e44ca4a4b6cf8f957c8e17 |

[Seletores](../validation/artifacts/gameplay-rework/gpr-01-2c/focused.request.json), [vermelho](../validation/artifacts/gameplay-rework/gpr-01-2c/red-terminal.json), [resultado detalhado](../validation/artifacts/gameplay-rework/gpr-01-2c/focused-final.json). Comparadores ativos cobrem dano, whiff, dedupe/colliders/gerações, cap, LOS, direção/pacote capturado, pausa/hitstop, cancelamento, swap, morte, run/cena, HP/statistics, A isolada e política None. Cinco cenários novos passam pela conclusão real, terminal cosmético, alvo com membership antigo permanecendo sem dano, pausa e limpeza por entrada na próxima sala/disable/swap/encerramento/cena; tornado combatente volta no encontro seguinte. Zero raízes remanescentes. Não é aprovação da suíte global.

Importação/compilação e Console consultadas no MCP. `refresh_unity` com scope scripts não importou o arquivo novo na primeira tentativa; scope all importou e gerou meta. Uma chamada exploratória execute_code sem return e outra com falha interna de Roslyn foram rejeitadas, sem alteração de gameplay; validação dos testes não dependeu delas. Console registrou aviso legado de keys e erro do Package Manager “Operation cancelled”; não foi limpa manualmente.

## Limites e próximo gate

Automação técnica não substitui teste físico. O cenário de produção usa dano assistido para concluir sala e Warp para testar transição; não afirma uma run humana completa ou frequência de rewards. Desempenho em8/32/64 inimigos e save/resume integral não medidos. Regressões finais compartilhadas estão no [relatório01.3](GPR-01.3_RELATORIO.md):196/196 ampliados,90/90 EditMode e329 casos distintos aprovados no conjunto das seleções, sem selo global.

## Complemento de Game View e fechamento

Depois da composição, a correção foi novamente exercitada com A+B na produção: `RoomClearGameViewRecordsCosmeticAndNextEncounter`, **1/1**,5,3392371s, job `a5b1eabdeb0a4e33a43dc116764cb860`. [Resultado](../validation/artifacts/gameplay-rework/gpr-01-2c/game-view-final.json). Capturas reais, sem editar pixels: [vento cosmético após clear](../validation/artifacts/gameplay-rework/gpr-01-2c/captures/20261029-232506-1068978/post-clear-cosmetic-no-root.png) e [tornado na próxima sala](../validation/artifacts/gameplay-rework/gpr-01-2c/captures/20261029-232508-4198314/next-room-combat-resumed.png). O alvo com barra restante na primeira imagem é a fixture de membership antigo, que permanece com1000HP; não é encontro reaberto. Ouro exibido no chão vem das mortes assistidas anteriores, não do vento cosmético. Asserts comprovam zero raiz/eletricidade/progresso nesse vento; imagem sozinha não certifica isso.

Também passaram os cenários de última morte pelo próprio tornado e de limpezaA+B na regressão final. O caso de última morte precisou usarWarp do NavMeshAgent na preparação, em vez de apenas moverTransform; nenhum comportamento de produção ou tolerância foi alterado para obter o resultado. Estado final do Editor, Console, integridade e limitações da ferramenta genérica estão no relatório01.3 e artefatos finais.

**O gate técnico foi satisfeito antes de iniciar GPR-01.3**, já autorizada sequencialmente pelo pedido. A composição Titanic Fists + Conductor foi então executada e está documentada separadamente. **STOP após01.3 para revisão do desenvolvedor**; Detonation, Singularity, redesign, novos especiais e substituição modular continuam sem autorização.
