# RUN-EXP-09.0 — auditoria estrutural anterior à implementação

03/11/2026, America/Sao_Paulo. **DONE: auditoria de dependências. Implementação e validação09 ainda NOT DONE neste checkpoint.** O pedido09 autoriza alterar piso, colisão, navegação e posições espaciais, preservando combate, economia, grafo e o protótipo08.

## Estado de entrada e fontes

Unity MCP confirmou D:/Dev/Personal/My_games/Tech-Guy, instância Tech-Guy@fccafb2b47cd7671, Unity6000.5.10f1/URP17.5. Branch gameplayOverhaul, HEAD766afa66d93a58afc1b9de1f2eafa15ed8a5504b. NexusBlockout limpa/Edit. Alterações07/08 emDocs/Tools e arquivos08 não rastreados são trabalho anterior preservado. Snapshot09:2393 arquivos existentes dos escopos Assets/_Project/ProjectSettings/Packages/SourceArt e47 preferências TechGuy. Sem reset, commit ou push.

AGENTS e skills locais reais de visão/combate/aceitação/recompensas e referências consultados; README, visão, direção/pipeline de arte, documentação de inimigos, run/recompensas, matriz/ledger/masterplan, relatórios06/07/08 e pacote08 disponíveis. Plano2.5D permanece referência separada. Busca deAGENTS aninhados/07B não encontrou fonte adicional; nenhuma inventada. Câmera e código deoclusão já auditados07/08, continuam noHEAD atual e foram preservados. Este documento registra dependências antes da primeira edição de geometria09.

## Mapa de dependências real

| Autoridade existente | Contrato / hipótese retangular encontrada | Alteração limitada proposta |
|---|---|---|
| ManualRunRandomizer.Generate | dimensões22–30×18–24, centro por níveis38–50m e lanes48–60m; SpawnPositions cria grade retangular | Plano espacial independente deRNG de recompensas, com footprint e redistribuição somente das posições dos mesmos prefabs/ranks |
| ManualNodeDefinition.Copy / ManualRouteSnapshot | centro eArenaSize são o envelope de todos os espaços | Acrescentar plano espacial imutável opcional ao snapshot e preservar sua referência na cópia; null mantém legado |
| ProceduralRunGeometryPlan.Create | sockets nas metades do retângulo; conexões ortogonais com branches/merges | Resolver sockets sobre limites reais mantendo lados/adjacências do grafo; ajustar cotovelos sem novas arestas |
| ManualStageEnvironment.BuildGenerated | umCubefloor por nó, GeneratedArenaWalls somente4lados | Materializar footprint em retângulos sem sobreposição, paredes nos contornos externos/internos e aberturas somente nos sockets |
| NavMeshSurface | bake único do deck inteiro, PhysicsColliders; coberturas NotWalkable | Continuar um bake por run; pisos ausentes não entram noNavMesh, estruturas não caminháveis mantêmModifier |
| ContainsPlayer / EntryVolume | umBoxTrigger inset cobre qualquer ponto do envelope | Conservar binding/trigger e acrescentar teste do footprint físico, excluindo vazios e massas centrais |
| ProgressionDirector.Manual | commit→traversal→entrada física observada0,15s→spawn; centro+offset; AI/spawners recebem bounds retangulares | Mesmas autoridades. NavMesh passa a delimitar áreas reais; verificar reforços e destinos junto às reentrâncias |
| EnemyAI / SpawnerBehavior | bounds retangulares são limite externo, candidatos passam por amostragemNavMesh | Inspecionar se amostragem pode escapar da sala/vazio; restringir pelo footprint apenas se caso real exigir |
| FirstSectorRoomPresentation | procura umfloor e desenha trilhas retangulares | Não aplicar trilhas antigas aos novos footprints; preservar apresentação05 compatível dos materiais/massas, sem pintura08 global |
| FirstSectorEnvironmentArt | percorre floors/walls/covers; racks supõem paredes ortogonais | Manter paredes ortogonais e materiais05; não introduzir nova linguagem artística |
| EncounterGates / ApplyGateState | barreira poraresta,abre somente commit/backtrack nas fases nativas | Mesmo binding/collider/carving e estados; nova moldura visual não decide progressão |
| ManualRoomProjection / ManualRunUI | projeta grafo eâncoras, não o polígono dopiso | Preservar IDs, rotas e escolha existente; sem portas com destinos fictícios |
| Reward/boons/boss/Nexus | frontier de encontro, ticketsC05 e grafo dependem deIDs/ordinais, não textura | Não modificar RNG/catálogo/pesos/economia nem layoutBoss; verificar sequência completa |

## Métricas e câmera

MCP confirmou Fragmento/Emissor comNavMeshAgent raio0,5m/altura2m. Demais agentes e jogador serão registrados noartefato de métricas; corredores produtivos já têm4m livres. O desenho manterá corredores4m, braços largos e faixas deanel de pelo menos5m, superiores ao diâmetro dos agentes encontrados; dash deve respeitar NavMesh e sólidos, não exige um corredor do comprimento inteiro dodash. Não alterar raio/câmera para encaixar a proposta. Follow perspectivaFOV40,16m/16m, semcontrole normal dezoom/órbita localizado. Estruturas altas nafrente exigem fader e captura real.

## Plano limitado e fallback

Uma camada de dados de footprint opcional, consumida pelo randomizador/materializador atuais. Quatro famílias alvo: pátio assimétrico com recortes reais; L largo com duas zonas; galeria com eixo e bolsões laterais; anel com estrutura e piso ausente no centro. Pelo menos três precisam passar os mesmos gates; não publicar uma família insegura.

Seleção somenteCombat compatível, por stream próprio e seed, com retângulo como fallback; Boss/Elite/salas especiais preservados. Centros0 e1 devem continuar pontos válidos de apoio quando usados pelo fluxo atual. Sockets precisam de abertura4m e aproximação livre. Remapear posições sem alterar roster, pesos, rewards ou seed stream existente. Rodar50seeds para geometria/determinismo e amostra real deNavMesh; exercitar cada família nas três armas e retorno ao fluxo/Boss/Nexus. Contagens e limites observados serão registrados, não inferidos deumSVG.

Uma opção serializada noowner da cenaFirstSector habilitará a nova seleção; cenas antigas/review08 conservam valor padrãofalse e geometria anterior. Não editar a cena08 nem suas texturas. Nenhum segundo diretor, gerador concorrente ou framework global.

## Skills e aplicação

Visão local: distinguir autorização09 do conceito08. Combate local: manter autoridades deHP/LOS e efeitos. Aceitação local/UnityMCP: instância real, hashes e testes executados. Boon-design: preservar stream/IDs/tickets sem rework. Level-design/tech-guy-level-layout: piso real, métricas, adjacências e rotas alternativas. Tech-guy-enemy-encounters/encounter-spec: posicionar mesmos papéis conforme família. Procedural-gen: plano de dados antes de render, RNG independente, validação de conectividade. UnityNavMesh: bake porrun, paths reais eWarp validado. Câmera/oclusão: volumes físicos e visuais correspondentes. Performance: medir bake/render/AI sem alegar otimização. Skills deasset/identidade/guias já lidos nesta conversa sustentam legibilidade; nenhuma imagem nova é necessária para demonstrar footprint.

## Evidência, riscos e próximo gate

Capturas baseline08/07 preservadas; baseline09 será capturado novamente peloUTF antes da integração. Console e resultado exato serão anexados. **NOT TESTED neste checkpoint:** famílias09,50seeds, GPU/standalone e aceitehumano. Riscos: sockets em reentrâncias, entryfalsepositive, spawns em buracos, carving de portas, efeito que cruza sólido e bake maior. Resolver por dados/colisão/owner existente; não mascarar comtuning global. Aprovação física permanece **PENDING DEVELOPER APPROVAL** após entrega09. Atualizações devalidação complementarão este checkpoint sem apagar a cronologia.

## Fechamento RUN-EXP-09 — 03/11/2026

DONE: quatro famílias físicas (pátio assimétrico, L, galeria com bolsões e anel) integradas à seleção normal de Combat do FirstSector por override _useSpatialLayouts. Seleção espacial independente do RNG de rewards/inimigos, com retângulo fallback; Boss/Elite/especiais preservados. Pisos ausentes não têm NavMesh, paredes/sockets seguem o contorno e spawns nativos são reposicionados sem alterar roster/rank/pesos. Tratamento painterly08 continua isolado.

DONE técnico: 50 seeds em dados, 50 decks materializados (277 salas espaciais:72 pátios,70 L,70 galerias,65 anéis), 12/12 famílias×armas, 5/5 produção (rotas Nexus/chefe/retorno nas três armas, rota aleatória e foco/C05), Spawner/bordas/HookLine, tripla com caso negativo da massa e Scene View real das quatro formas. Regressão09: 9/9 (cinco ataques físicos/projétil, morte/retorno e três armas na cena08) e2/2 protocolos Titanic/Conductor. Fixtures de posições, imunidade/HP, mana/Asura, boons em memória, Warps e kills estão declaradas; não são aceite humano nem aquisição natural de sinergia.

Custo medido no Editor: bake por deck, frame/CPU/draws e contagens; amostras curtas, seeds/rosters diferentes. GPU/standalone/GC por frame e custo exclusivo de nav/fader NOT TESTED. Integridade por snapshot2393 arquivos/47 prefs, comparação final em artefatos09. Sem commit/push. PENDING DEVELOPER APPROVAL: fluxo, ritmo, câmera, beleza e dificuldade em controle físico. STOP após09.

[Relatório09](RUN_EXP_09_RELATORIO.md) · [Validação09](RUN_EXP_09_6_VALIDATION.md). Pacote: Docs/validation/artifacts/run-exp-09/review/index.html e RUN_EXP_09_REVIEW.zip.

O checkpoint acima foi escrito antes da primeira edição de geometria; seus NOT TESTED são históricos. Foram criados plano, materializador parcial, opção/override produtivo, cena de comparação, autoria de seed, testes e pacote09. Timers de bake são observação, não tuning. A massa central inicialmente usava o material de blockout e passou a usar o ArchiveWall05; não alterou sua colisão. Scene View inicial via Camera.Render saiu vazia e foi rejeitada; MCP capturou as quatro formas no viewport real com overlay de navegação. Foco, cooldown e alvos mortos foram corrigidos nas fixtures; executores de combate ficaram intactos.
