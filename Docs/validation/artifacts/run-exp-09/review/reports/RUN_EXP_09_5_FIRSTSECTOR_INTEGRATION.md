# RUN-EXP-09.5 — Integração FirstSector

## Objetivo e implementação produtiva

DONE: _useSpatialLayouts está habilitado por override em Assets/_Project/Scenes/FirstSector.unity. ManualRunRandomizer.Generate recebe opção vinda do ambiente atual. A seleção usa SplitMix64(seed XOR nodeId XOR salt espacial) módulo5: quatro novas famílias e retângulo fallback, apenas em Combat com conteúdo compatível. Boss/Elite/especiais conservam caminho anterior. Os streams existentes de recompensas/inimigos/layout executam na ordem anterior; a nova escolha ocorre depois e não consome esses RNGs.

ManualNodeDefinition guarda o plano; ProgressionDirector passa a opção, sem novo diretor/framework. Review09 é uma cópia para comparar. Review08 e seu flag padrãofalse ficam no caminho antigo. Não há runtime painting08 nas salas produtivas.

## Reprodução e validação

Abra NexusBlockout e Play para seed normal. O portal normal carrega FirstSector com seleção espacial antes do chefe. Para comparar uma forma no primeiro Combat, fora de Play use Tools → Tech Guy → RUN-EXP-09 → Next run → Courtyard / L Chamber / Gallery / Ring e entre no portal. O override de seed é SessionState de uso único, não save. Tools → Tech Guy → RUN-EXP-09 → Open spatial comparison abre a cena isolada09; a08 continua disponível para comparação histórica.

DONE: três rotas fixas, uma por arma, e uma aleatória via Nexus/portal, roster/rewards nativos, C05 com cancel/reabrir/confirmar uma vez, chefe/extraction e retornoNexus. Mais um teste de foco/modal/Enter remapeado sem vazar comando para combate. Os logs06 com timestamp2345 são execução desta09, copiados em production do pacote; o nome histórico do helper não muda a origem das capturas. Imunidade, acesso temporário a loadout, Warps e kills são somente da automação.

Arquivos: ManualRunRandomizer.cs, ManualRunDefinition.cs, ProgressionDirector.Manual.cs, ambiente/planos e override de cena. RunExp09Authoring.cs fornece comparação/reprodução, sem alteração de prefs persistentes. Chance por família é regra do protótipo, sujeita a aceite; não há pesos novos de loot.

## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.
