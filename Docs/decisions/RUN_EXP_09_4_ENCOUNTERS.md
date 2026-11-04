# RUN-EXP-09.4 — Encontros e ocupação espacial

## Objetivo e estado atual

DONE: os mesmos prefabs/ranks e ordem do roster são redistribuídos em posições seguras. Arrange reserva centro com raio4 m, sockets com raio5 m, clearance1,25 m e separação2,4 m. Backline usa a classificação existente; scores por família favorecem braço superior do L, eixo/bolsões da galeria e lado oposto do anel. Não altera pesos, ondas, budget, HP, dano, rewards ou inimigos.

Doze fixtures usam Fragmento, Emissor, Grunt, Heavy, Hooker e Healer reais, seis por sala. IA/nav/poses ficam ativos na observação, circulação e LOS. O alvo do teste de básico tem HP/posição/IA temporariamente controlados; habilidades usam mana1500 e Asura para exercitar executores nativos. Isso verifica compatibilidade e legibilidade capturada, não dificuldade natural.

## Testes e correções

Ataque básico causa HP nas três armas; Q/E/R e especial são usados e encerram casting; dash recupera NavMesh. O Emissor não resolve impacto atrás da cobertura física e readquire após mudança de ângulo. A asserção identifica o collider obstrutor. Fragmento/Emissor registram poses/pés. O Spawner e Hooker são exercitados separadamente junto aos contornos, com fixtures declaradas.

A tripla TitanicFists+Conductor+Detonation foi aplicada em memória e produziu recibos nativos de dano e telegraph HeavySlam junto à massa do anel. A primeira posição copiada do08 estava atrás de uma cobertura e falhou corretamente; mudou-se a fixture para uma faixa livre, sem modificar protocolos. O caso negativo usa a massa real como bloqueio. NOT DONE: balanceamento novo, nova IA, novos arquétipos e campanha de dificuldade. Arquivos de executor e catálogos permanecem intactos.

## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.
