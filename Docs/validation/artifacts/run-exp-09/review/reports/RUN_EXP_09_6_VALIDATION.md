# RUN-EXP-09.6 — Validação técnica

## Resultado e escopo

DONE: auditoria anterior; 1/1 EditMode de50 seeds; 1/1 PlayMode de50 decks; 12/12 famílias×armas; 5/5 produção (três armas, rota aleatória e foco/C05); custo Editor e tripla com recibos. DONE: 1/1 borda/Spawner/HookLine nas quatro famílias, 1/1 tripla com caso negativo da massa e 1/1 inspeção Scene View. As regressões estão nos jobs finais anexados, sem transformar uma execução que não iniciou em “passou”. O pacote contém resultados iniciais e finais para auditoria.

Falhas: foco da fixture no arco exigiu restabelecer foco e release no próprio teste; não se afrouxou gameplay. Tripla inicial bloqueada pela cobertura exigiu posição livre. Um erro local de ordem de variável impediu compilar a fixture negativa; corrigido, aguardou-se compilação limpa. Jobs sem inicialização foram descartados, sem contagem de testes aprovados. A amostra de50 decks não cobre todas as seeds possíveis.

## Performance observada

As condições e amostras estão em performance/frames.csv e conditions.txt. Cinco decks com seeds distintas, 3s por família após0,6s; inimigos e geometria do deck variam. Timer de bake mede somente NavMeshSurface.BuildNavMesh. Load+bake inclui carregamento e frames iniciais. Frame/CPU/draws medidos no Editor; coleta tem overhead e não é A/B de densidade igual. Alocação total é memória residente, não GC/frame. Sem alegação de otimização ou FPS standalone.

| Família | Inimigos | Bake deck ms | Load+bake ms | Frame med/p95 ms | CPU med ms | Draws | Renderers / colliders / triângulos deck |
|---|---|---|---|---|---|---|---|
| Fallback retangular | [6] | 28.422 | 933.569 | 5.651 / 7.443 | 5.678 | 383.0 | 307.0 / 281.0 / 371.0 |
| Pátio assimétrico | [4] | 14.959 | 672.158 | 4.587 / 6.982 | 4.596 | 327.0 | 318.0 / 294.0 / 406.0 |
| Câmara em L | [5] | 17.294 | 634.906 | 4.783 / 6.47 | 4.785 | 351.0 | 336.0 / 309.0 / 501.0 |
| Galeria com bolsões | [6] | 16.183 | 598.232 | 6.709 / 8.47 | 6.68 | 399.0 | 289.0 / 267.0 / 362.0 |
| Anel fragmentado | [5] | 18.402 | 775.871 | 5.041 / 6.815 | 5.056 | 332.0 | 337.0 / 308.0 / 394.0 |

O agrupamento de pisos evita um collider por metro. Nenhuma grande regressão pode ser excluída em GPU/campanha por estes dados curtos. NOT TESTED: GPU, GC/frame e custo exclusivo de agentes/fader. Browser do HTML: BLOCKED pelo file:// no ambiente; QA estática de referências locais, SHA256, PNG/CRC, sintaxeJS e ZIP. Sem contorno dessa política por servidor local.

## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.

## Integridade final e encerramento

DONE: comparação com o snapshot anterior09. Nenhum arquivo protegido removido. As únicas diferenças em arquivos existentes são os seis fontes da integração e FirstSector.unity. Prefs TechGuy:47 antes/47 depois, sem valor alterado. HEAD preservado, sem commit/push. Cena final NexusBlockout limpa em Edit Mode, Time.timeScale1, sem seed/assistência pendentes. Arte/scene/scripts08 e prefabs/kits/boons/settings preexistentes preservados.

O pacote inclui evidence/integrity-comparison.json com lista exata. Browser rendering continua NOT TESTED por bloqueio file://; a validação do ZIP é estática. O aceite humano permanece PENDING DEVELOPER APPROVAL.
