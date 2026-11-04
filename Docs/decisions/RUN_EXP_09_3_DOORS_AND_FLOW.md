# RUN-EXP-09.3 — Portas, conectores e fluxo

## Objetivo e contrato preservado

DONE: adjacências, IDs e destinos continuam os do snapshot de run. ProceduralRunGeometryPlan resolve o socket no contorno externo real, mantendo normal cardinal e projetando a posição lateral solicitada. O socket não pode abrir para a massa central. A projeção mantém separados destinos de branches/merges; usar sempre o meio da borda poderia juntar portas.

Conectores mantêm trechos ortogonais, largura4 m e prolongamento4 m antes de cotovelos. Barreiras físicas, carving e anchors continuam os do materializador existente. Estado vem de ProgressionDirector/ManualSession: fechar no encontro, resolver ordinais de recompensa, commit válido e só então abrir. Ombreiras indicam abertura, sem um segundo dono de progressão.

## Testes e trade-offs

DONE: 50 decks verificaram segmentos ortogonais e caminhos reais por aresta. Doze comparações verificaram gate fechado, ticketsC05, gate aberto, deslocamento real do NavMeshAgent de1 m dentro a3 m fora do socket e entrada no próximo nó. O restante da distância usa Warp na fixture, explicitamente. Quatro rotas de produção atravessaram estados até chefe/extrair/Nexus com o owner atual; também usam Warp e kills controladas.

A grade cardinal restringe curvas e arquitetura, em troca de uma integração limitada. Não se desenhou uma porta de destino fictício. Arquivos alterados: ProceduralRunGeometryPlan.cs, materializador parcial, ManualStageEnvironment e cena FirstSector. O grafo/UI de escolha não foi redesenhado. PENDING DEVELOPER APPROVAL: perceber e escolher a saída com a câmera normal, sobretudo junto à massa do anel.

## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.
