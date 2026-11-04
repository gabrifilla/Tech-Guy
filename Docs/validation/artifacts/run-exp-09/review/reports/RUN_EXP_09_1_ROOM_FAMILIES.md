# RUN-EXP-09.1 — Famílias espaciais reais

## Objetivo e estado auditado

DONE: substituir parte das arenas retangulares por quatro footprints distintos, preservando retângulo e arenas especiais. Antes09, variação aberta/coberta mantinha o mesmo envelope retangular; o protótipo08 era apresentação isolada.

| Família | Envelope / piso | Decisão de jogo e papéis |
|---|---|---|
| Pátio assimétrico | Envelope nativo arredondado a metros pares; recortes NW 4×5 e SE 5×3 sem piso | Rotação aberta entre coberturas e recortes. Melee aproxima pelo interior; ranged mantém distância no lado oposto. Mais próximo do baseline para transição. |
| L | 28×26 m, piso 602 m²; quadrante NE 14×9 ausente | Dois braços largos e canto que rompe visão. Ranged valoriza o braço superior; melee pressiona a curva. Jogador pode escolher abrir ângulo ou recolher-se. |
| Galeria | 28×30 m; eixo de 12 m e bolsões laterais desencontrados; piso 504 m² | Avanço longitudinal, cobertura escalonada e refúgios laterais. Ranged tem eixo longo; acesso pelos bolsões quebra a aproximação frontal. |
| Anel | 28×26 m; massa 7×10 m deslocada a leste e piso ausente sob ela; piso 658 m² | Dois contornos reconectam o jogador. Faixa leste 5 m, norte/sul 8 m e oeste 16 m antes do inset. Massa interrompe ataques físicos/LOS; rota oeste é mais ampla. |

São quatro famílias implementadas, não quatro skins. Portas em fronteira externa, centro válido e densidade de roster nativa limitam a autoria. As duas coberturas continuam condicionadas pela seleção existente. Massas e materiais são05; pintura08 não foi distribuída.

## Arquivos, decisões e trade-offs

FirstSectorRoomLayout.cs define células de 1 m, retângulos fundidos, fronteiras, portais e posições. A leitura métrica é intencional, sem DCC nem geometria orgânica. O anel pode dominar o quadro perto da massa: precisa de aceite humano; usa material05 e o fader existente. O termo “fragmentado” descreve circulação desviada e coberturas, sem saltos/ilhas desconectadas. Não há salto novo.

## Escopo, fontes e skills aplicadas

Pedido RUN-EXP-09, auditoria anterior à geometria em RUN_EXP_09_0_AUDIT.md, AGENTS.md e código corrente em D:/Dev/Personal/My_games/Tech-Guy. Skills locais de visão, combate Unity, boon-design e aceitação; instaladas de layout Tech-Guy, encontros, identidade, level-design, procedural-gen, Unity NavMesh, Unity C#, Unity MCP, câmera/oclusão e performance. As referências de assets sustentam materiais existentes; nenhuma arte nova foi gerada nesta etapa. Nenhum agente delegado, commit ou push.

Sistemas inspecionados: ManualRunDefinition/Randomizer/StageEnvironment, ProceduralRunGeometryPlan, ProgressionDirector.Manual, EnemyAI/EnemyNavigation/SpawnerBehavior, FirstSectorContentDefinition/EncounterLayout/EnvironmentArt/RoomPresentation, câmera e fader, dash, tickets RunBoons/C05 e testes de produção06. O owner atual continua responsável pela run e pelo único bake do deck.

Arquivos criados09: FirstSectorRoomLayout.cs, ManualStageEnvironment.Spatial.cs, RunExp09Authoring.cs, Review/RunExp09SpatialReview.unity, teste EditMode RunExp09SpatialTests e cinco partials PlayMode Spatial09/Gameplay/Evidence/Edges/SceneView; respectivos .meta gerados pelo Unity. Alterados09: ManualRunDefinition.cs, ManualRunRandomizer.cs, ManualStageEnvironment.cs/.Procedural.cs, ProceduralRunGeometryPlan.cs, ProgressionDirector.Manual.cs e override de FirstSector.unity. Documentação/Tools/artefatos09 e atualizações canônicas acompanham a entrega. Nenhum prefab ou asset de kit/boon foi alterado.

## Evidência, integridade e limites

Os jobs brutos, resultados iniciais com falha, CSVs e capturas estão em Docs/validation/artifacts/run-exp-09. O HTML portátil separa comparação controlada, produção normal e planta técnica. Capturas não foram retocadas. Conectividade automatizada não é aceite de ritmo/dificuldade. Recursos, HP/imunidade, posições e kills usados nas fixtures estão explicitados nos arquivos passed.txt/fixture.txt e logs de rota. Não houve aquisição natural garantida da tripla.

Hashes de 2393 arquivos existentes e 47 preferências foram obtidos antes da alteração. O fechamento usa integrity-comparison.json; diffs anteriores07/08 ficam preservados. Cenas/prefabs de combate, chefe, Nexus, recursos e GUIDs não são recriados. A cena FirstSector recebe somente o override _useSpatialLayouts; a cena08 mantém o caminho retangular e sua pintura isolada. NOT TESTED: GPU, standalone, custo isolado do fader/nav de agentes, campanha longa e playtest humano. O checker genérico de C# tem limitação de mounts C:/D: no pacote externo Tripo; isso não foi chamado de aprovação.

PENDING DEVELOPER APPROVAL: qualidade espacial, leitura das portas, gosto visual e dificuldade em controle físico. STOP após09, sem expansão automática. A Scene View inicial via Camera.Render foi rejeitada por viewport vazio; as capturas válidas em scene-viewport foram refeitas pelo MCP com source=scene_view e overlay de NavMesh. O ensaio de HookLine passou após observar release antes da nova tecla e respeitar cooldown do dash anterior; a asserção mede início nativo, podendo encerrar ao alcançar um sólido. Não se exige atravessar a parede nem durar0,08s contra ela.
