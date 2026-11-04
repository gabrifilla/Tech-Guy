# RUN-EXP-09 — FirstSector com variação estrutural

DONE: quatro famílias físicas foram implementadas e integradas na seleção normal de Combat do FirstSector: pátio assimétrico, L, galeria com bolsões e anel. Cada uma altera piso, paredes, navegação, sockets e ocupação, mantendo retângulo como fallback e preservando chefe/Elite/especiais. _useSpatialLayouts é um override produtivo; o tratamento painterly08 continua isolado.

## O que está comprovado

50 seeds de dados e50 decks com NavMesh real; 12 combinações de armas×famílias; três rotas fixas e uma aleatória Nexus→salas→chefe→Nexus; C05/foco/remapeamento; ataque básico com HP, quatro habilidades, dash, bloqueio/readquisição do Emissor e travessia física do trecho de porta. Há testes de Spawner/Hook e tripla com fixtures declaradas e jobs finais anexados. Ver RUN_EXP_09_6_VALIDATION.md e evidence/*.json para resultados exatos e tentativas iniciais.

Plantas SVG derivam de BoxColliders, anchors e triangulação real exportados em PlayMode. Game View normal e vista ortográfica técnica são categorias separadas. Scene View aparece como captura técnica do Editor. A forma só é considerada publicada porque a cena produtiva foi habilitada e as rotas normais foram executadas; screenshots da comparação permanecem identificados como comparação.

## Como revisar

Use o HTML portátil em Docs/validation/artifacts/run-exp-09/review/index.html ou extraia RUN_EXP_09_REVIEW.zip e abra review/index.html. No Unity, rode NexusBlockout para distribuição normal ou escolha Tools → Tech Guy → RUN-EXP-09 → Next run para uma forma determinística no primeiro Combat. Open spatial comparison abre Review09; Review08 preserva a apresentação painterly histórica.

O avanço aqui é estrutural e mensurável. “Mais bonita”, ritmo, dificuldade, conforto da câmera e identidade artística continuam PENDING DEVELOPER APPROVAL em playtest físico. NOT DONE: novos mundos/inimigos/boons, rework de economia/UI, pintura global, chefe novo e etapa posterior. STOP após09; sem commit/push.

## Relatórios

Auditoria0; famílias1; geometria2; portas3; encontros4; integração5; validação6. Cada relatório registra fontes, mudanças, decisões, trade-offs, evidência e limites. Performance tem medidas curtas de Editor e não prova de GPU/standalone. Integridade é fechada por hashes/prefs comparados ao snapshot anterior.

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
