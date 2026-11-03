# RUN-EXP-03.4 — validação integrada e entrega para revisão

Escopo executado até03.4: integração limitada na FirstSector, dois conceitos isolados de recompensa, refino intermediário de dois candidatos e pacote de evidências. STOP nesta etapa. Sem substituição permanente da UI ou expansão do catálogo.

## Ambiente, métodos e resultados

Unity MCP verificou repetidamente o projetoD:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, instânciaTech-Guy@fccafb2b47cd7671. UTF executado no Editor pelo servidor MCP já existente; `Tools/RunExp02/mcp_call.py` apenas usa esse cliente. Sem runner alternativo ou dependências instaladas. Resultado exato, casos/saídas/duração em[test-index.json](../validation/artifacts/run-exp-03/test-index.json).

| Verificação | Job | Resultado |
|---|---|---|
| Baseline | 173d618819be47b3901d72f65c4ac2d4 | 5/5 PlayMode |
| Distribuição/config | 7e0a8c2973e44491a9bbf583fb3a5eb6 | 1/1 EditMode |
| Primeiras rotas | 02d5b164cdf242e4999df014a6bb0d8a | 3/3 PlayMode; rótulos Bow/Spear corrigidos depois |
| Arte/config | b7c1fd8f2fbf408f8898489fb5c7b1f8 | 2/2 EditMode |
| UI inicial | 29f96ccb128f442a9dfd95aa5e6627bc | 2/2 PlayMode |
| Contato três armas/cobertura | 3086d727e4e1457c889e69e87179b59b | 1/1 PlayMode |
| Rotas refinadas | 267fbd214fcb4651ac279dd80b116b6b | 3/3 PlayMode |
| Integrado intermediário | 70751110b2fb4982976caf3d6e0a4b15 | 11/12; pausa visual nova falhou |
| Correção pausa isolada | d663395b664a4850b645e6b1fffd2c91 | 1/1 PlayMode |
| Regressões críticas | e59091e659fb4cd1bb1a03cd6b676a9f | 24/24 PlayMode,36,486s |
| Integrado final | 37282363f9d443efa8ff6dc5c4c7d6a4 | 12/12 PlayMode,117,246s |
| Config/grafo/arte final | 4366e480686840c7a4d9f16a77955d22 | 26/26 EditMode,1,609s |

Integrado final: três rotas0/1/2 até boss/tickets/Nexus; contato básico das três armas;8/32/64 com autoridade nativa; lifecycle refinado;4 casos UI. Regressões: hooks repetidos/sobrepostos/produção, recuperação/morte/unload, pausa/hitstop, movimento/dash/colisão, fechamento de recompensa/release, tornado vento/elétrico/fogo/triplo, duas aquisições naturais e contexto/leases de projéteis/frost. Os registros identificam assistência visual nas composições; não se confundem com aquisição natural. Não foi executada a suíte global;31 falhas históricas de RUNEXP01 permanecem históricas e não foram ocultadas.

Mouse nativo foi exercitado no Lab usando a skill computer-use e a janela Unity observada. Logs`mouse-*.json`:0 aquisições ao selecionarA, abrir detalhes e clicar sob modal; seleção bloqueada sob detalhes; trocaB/trilho também sem aquisição; confirmar Detonation resultou1 aquisição e choices vazias. Só inventário runtime isolado, sem save. Teclado/gamepad virtuais, pausa, confirmação única, lista vazia, evolução e binding temporário fazem parte do UTF. Isso não equivale a aprovação humana do design.

## Correções e achados intermediários

Patas excediam o alcance em curvas (até0,532m): apoio adaptativo passou a levantar antes de arrastar. Máximo observado na execução final≈4,1e-8m durante sustentação, tolerância4cm mantida. A pausa revelou1,713mm de movimento em um quadro; guarda timeScale nas apresentações novas corrigiu, mesma tolerância1mm passou. Durante instrumentação, `UnityStats.batches` não existe nesta versão: erroCS0117 corrigido removendo essa leitura; campo-1 indica indisponível. Draw calls usam propriedade real suportada. Console final MCP0 erros, sem limpeza artificial.

Checker da skill C# falhou primeiro ao formatar caminho de pacote externo C: relativo ao projeto D:. Wrapper local só ajustou essa formatação, sem mudar regras. Resultado[project-check.json](../validation/artifacts/run-exp-03/project-check.json):2710 arquivos, um missing-script histórico em PlaygroundGUID9d54880da6396da4c8cb3d4f4d1289a3 e um aviso histórico de TitanicCompositionFrameSampler/filename. Nenhum achado03. Não foram removidos componentes ou corrigidos legados fora do escopo.

## Performance observada

Fixture controlada na FirstSector seed0/nó1: snapshot apenas da sessão recebe população1:1:1 Fragmento/Emissor/Grunt; o diretor real faz todo spawn/membership/coordinator/estatísticas. Nenhum novo spawner.1s warmup e5s de amostra; AI, dano, navegação e apresentação ativos, contagem ativa mínima igual ao alvo. Jogador imune para sustentar a medição, moedas removidas apenas na fixture. Não é distribuição natural de uma sala com64.

| População | Frames | Quadro p50/p95/p99 ms | MeshRenderer + Skinned | Triângulos mesh + skin | Draw calls Editor p95 |
|---|---:|---|---|---|---:|
| 8 | 1174 | 3,79 / 5,28 / 6,85 | 75 +14 | 3300 +54944 | 499 |
| 32 | 647 | 6,82 / 9,65 / 11,73 | 275 +70 | 12100 +274720 | 1562 |
| 64 | 436 | 10,44 / 13,31 / 14,57 | 543 +147 | 23820 +576912 | 3171 |

| População | BehaviourUpdate p95 ms | NavMeshManager p95 ms | Pose03 total p95 ms | GC p95 bytes/quadro | Spawn síncrono ms |
|---|---:|---:|---:|---:|---:|
| 8 | 0,862 | 0,111 | 0,061 | 471808 | 7,71 |
| 32 | 1,393 | 0,276 | 0,239 | 759806 | 21,60 |
| 64 | 2,015 | 0,431 | 0,485 | 1219106 | 37,52 |

10/32/44 impactos nativos observados; todos os atores, projéteis e ghosts removidos ao concluir. Cleanup430,9/435,8/450,3ms inclui espera deliberada0,4s e frames, **não CPU de destruição**. Três materiais distintos contam somente MeshRenderers dos inimigos; materiais skin/cenário não entram nessa contagem. Skinning.Update p95≈0,071/0,146/0,225ms; Skin e Agent.State registraram0, não prova de custo inexistente. Draw calls são estatística global do Editor, incluem outras vistas/câmeras/telegraphs, não draw calls exclusivos do candidato. Batches não disponível. BehaviourUpdate é agregado, não CPU isolada da AI.

Filas Protocol: MaximumQueue0/LiveRoots0/drops0 nesta fixture sem ataque do jogador/boons; não é teste de saturação de uma build. Contratos causais/budgets foram verificados nos filtros de regressão. O samplador UTF/LINQ, Editor e OnGUI acrescentam CPU/GC; não atribuir toda alocação à pose nova. Há custo relevante de renderers/skin legados e articulações; avaliar batching/rig/LOD após aprovação da arte e perfil standalone. Dados completos em[performance-summary.json](../validation/artifacts/run-exp-03/performance-summary.json). Nenhum orçamento final de lançamento foi aprovado.

## Integridade e evidências

Inspeção final recarregou FirstSector/config/prefabs: pool16 preservado, extras2, missing scripts0, pose03 presente, asset salvo/cena limpa. Distribuição da ordem real128seeds em[native-distribution-128.csv](../validation/artifacts/run-exp-03/native-distribution-128.csv):357 cobertas/539 abertas,373 Fragmento/389 Emissor, mesmos totais da amostra ordenada mas rosters/rotas individuais diferem. Prefabs/radii/contratos em[final-inspector.json](../validation/artifacts/run-exp-03/final-inspector.json).

Inventário inicial1500 hashes permite identificar precisamente cinco arquivos existentes de produção alterados; demais1495 preservados, inclusive Nexus/UI/controles/kits/reward definitions/originais02. Novos assets/scripts estão no inventário final. O teste legado de movimento regravou um JSON histórico que estava limpo no baseline: sua nova execução foi copiada para`regression-movement-current.json` e o histórico restaurado byte a byte deHEAD. Nenhuma alteração do usuário foi descartada.

Na última inspeçãoUnity30/10,32 chaves TechGuy do registro, incluindo moedas, unlocks, arma, tutorial, bindings e resumo, estavam iguais ao baseline. Comparação hash a hash em`integrity-final.json`, campo`unityRecordedGamePreferenceChanges` vazio. Alterações esperadas de sessões Unity e `PT_Run` (Unity Test Framework Performance, fonte Utils.cs) são separadas. Nenhuma alegação de identidade total desses caches de teste. Arquivos persistentDataPath comparados à referência02 têm limite histórico explícito; logs/TestResults não são saves de gameplay. Último estadoUnity verificado: ArenaB original, EditMode, limpa, relógio1, assistênciafalse e override seed vazio. Console0.

**Conferência posterior à interrupção, encerramento documental02/11:** Editor fechado, sem processoUnity; tentativa de nova conexãoMCP recusada em127.0.0.1:8080. Não foi fabricada reexecução/estado atual de Editor. Leitura posterior do registro encontrou quatro diferenças de gameplay em relação ao checkpoint: moedas no Player e no Editor, arma no Player e resumo no Editor. Origem posterior não atribuída; valores atuais foram preservados. As32 igualdades descrevem o checkpoint de30/10, não uma promessa sobre preferências futuras. GateF é PASS no escopo daquele checkpoint auditado; novas sessões devem capturar baseline atual antes de testar. Não é necessário abrirUnity para concluir estas edições documentais.

Capturas de FirstSector normal, salas abertas/cobertas, dupla, LOS bloqueado/recuperado, movimento/ataques/hit/despawn, close-ups e original/A/B organizadas em[REVIEW.html](../validation/artifacts/run-exp-03/REVIEW.html). Foram inspecionadas em imagem e correlacionadas com HP/impactos/nav/rosters. Rotas são assistidas por travessia/abates; lifecycle tem HP real e posicionamento explícito. Nenhum screenshot sozinho foi usado para provar combate.

Skills efetivamente aplicadas: quatro locais TechGuy e referências, router, Unity MCP/C#/ScriptableObjects/NavMesh/animation, level-layout/encounters, game-ui-ux, create-game-assets, game-feel, performance-optimization; computer-use apenas para interação mouse real. Blender não conectou. Arquivos novos/alterados e decisões por domínio estão nos relatórios03.1–3 e no manifesto. Próximo passo: revisão física da UI e arte; parar aqui.
