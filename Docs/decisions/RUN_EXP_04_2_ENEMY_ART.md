# RUN-EXP-04.2 — revisão intermediária de geometria e articulação

Escopo: somenteFragmento/Emissor, preservando silhuetas/papéis aprovados, executores e timing. Arte implementada em cópias próprias e integrada por referências doFirstSectorContent após verificações. **Intermediária, não produção artística final e não aceita fisicamente pelo desenvolvedor.**

## Autoria e decisão

Inspecionados modelos/meshes/pivôs03, FirstSectorEnemyPose/Despawn, fontes02, Actor/EnemyVariant/AI/CombatActions/reactions e pipeline de materiais. CriadosRunExp04EnemyArtAuthoring.cs, seisMeshes comUV/normals emArt/RunExp04/Enemies e doisPrefabs/FirstSector/RunExp04; fontes03/02 não substituídas. Materiais compartilhados03 preservam comparação de geometria sem depender de nova cor/glow. Fragmento: encaixe da coxa, joelho, apoio mais largo e mandíbula laminada. Emissor: carcaça mais longa/estruturada, seção controlada e cavidade de emissão preservada.

Cada mesh tem origem/pivô compatível com o transform já animado. Nenhum renderer novo:18Fragmento/7Emissor. Mesmoscollider/cápsula, profile, arquétipo,HP/dano/cadência/windup. Não adicionarRootMotion, Animatorhumanoide, eventos de dano ou fontes deVFX ofensivo. Referências novas doContent são a única alteração de dados publicados desta etapa.

Flagserialized `_weightedLocomotion` emFirstSectorEnemyPose éfalse nos assets03, true somente nas cópias04. Acrescenta peso vertical leve na sustentação, abertura gradual da preparação e flinch visual das carcaças. Canal/body doEmissor continua na origem nativa; efeito não altera elegibilidade ou instante de tiro. Derrota04 ativa desagregação de partes exclusivamente no ghost semActor/collider/AI; duração0,28s e relógio escalado/cleanup do encontro preservados. FirstSectorEnemyDespawn continua compatível com03 sem ativar essa opção.

BlenderMCP status falhou ao conectar. Pipeline disponível: Mesh authoringC# noEditor, hierarquia rígida existente/IK e materiaisURP. Sem rig/pintura/unwrap final/atlas/LOD ouDCC externo alegados. UVplanar simples; articulações rígidas ainda exigem julgamento humano e eventual rig final. Mais polígonos não são evidência de qualidade.

## Unity, comparação e testes

MCP no projetoD:/Dev/Personal/My_games/Tech-Guy6000.5.10f1. Authoring e reabertura dePrefab/AssetDatabase realizados. JobEdit`180e47065a964a638617bc88999f339c`:1/1,0,695s, geometria alterada/UV/volume positivo/valores finitos/missing0, footprint e executores iguais. Nenhuma falha de código/importação nessa autoria; checker geral será encerrado em04.5.

Play`88fc9eb896f441578413fc8a618e6c9d`:1/1,8,305s. Comparação03/04 no mesmo ator/sessão, pose pausada, câmera/escala/luz idênticas; close-ups deambos e câmera normal. Em seguida nativeAI/poses reais ativas e morte/despawn/cleanup. Fixture explicitamente injeta par04 pelo diretor e imuniza jogador apenas para comparação; não prova distribuição natural ou sensação final. Arquivos04-art* no diretório único`../validation/artifacts/run-exp-03/firstsector/20261102-195202-0479818/`, preservados e destinados ao pacote04.

Imagens antes/depois e close-ups foram abertas/inspecionadas: encaixes e bases deFragmento mais legíveis; carcaçaEmissor mais definida. Formas continuam quadrúpede baixo versus corpo suspenso/canal. Sem labels ou círculo novo como identificação. Observação em câmera normal e fases temporais complementa close-up; aprovação subjetiva não foi inferida. Integração salva e GUIDs em`../validation/artifacts/run-exp-04/art-integrated.json`.

## Skills, segurança e limites

AplicadasTechGuyvision/combat/acceptance e referências, create-game-assets/three-d-pipeline, UnityC#/MCP/animation, game-feel e método deperformance. Regras: silhouettes e causação nativas, geometria antes de brilho, sem retiming de combate, origem/footprint controlados e fontes preservadas. Estado/prefs estão sob isolamento04;32chaves eram iguais após correção da fixture, comparação final em04.5. Custo completo8/32/64 ainda será repetido com a revisão integrada, sem alegar orçamento.

Gate técnico04.2 permite prosseguir ao polimento limitado de salas; arte final exige revisão humana. Regressões de lifecycle/básicos/Protocols e rotas com essas cópias serão repetidas na integração final. Próximos ajustes artísticos dependem de direção aprovada; não expandir roster.

## Fechamento da sequência04

Revisão de evidência no fechamento: a comparação normal em cobertura ocultava Fragmento. A fixture passou para sala aberta seed0 e gerou pares reais03/04 durante preparação e ataque, no mesmo instante pausado/câmera. Job `1c11f19931944dde863eb6dc73da0aba`:1/1,11,760s; ambos visíveis na captura normal e oito imagens adicionais no pacote. Somente código de teste/captura mudou; timing e execução produtivos não foram alterados. Poses atuais comuns nos dois lados isolam a diferença geométrica; não alegar reconstrução do movimento antigo.

Esta seção não altera os resultados intermediários acima. A integração final foi encerrada em04.5:15/15 Play integrados,1/1 matriz ampliada com12 cenários,33/33 regressões,10/10 efeitos de habilidades e28/28 Edit selecionados.1562/1574 hashes iguais e12 alterações previstas;32 preferências TechGuy atuais iguais, sem restaurar03. Fontes/cenas/metas antigas e UI publicada preservadas, Console0 erros; checker mantém dois diagnósticos históricos. Performance, GUIDs, jobs completos, limites de persistência e aprovação física estão no [relatório de validação](RUN_EXP_04_5_VALIDATION.md). [Entrega e pacote visual](RUN_EXP_04_RELATORIO.md). STOP04.5: arte final, direção de UI e julgamento físico permanecem pendentes.
