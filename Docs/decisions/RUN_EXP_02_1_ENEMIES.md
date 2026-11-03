# RUN-EXP-02.1 — Fragmento e Emissor

## Escopo e entrega

Dois candidatos 3D autorados e funcionais, separados dos 16 prefabs publicados. Blender MCP não conectou; não houve produção no Blender nem download de terceiros. A entrega é uma prova geométrica intermediária com malhas facetadas próprias geradas por estações e seções poligonais, não uma composição de cubos ou arte AAA final.

| Candidato | Forma e movimento | Combate real | Geometria |
|---|---|---|---|
| Fragmento | Corpo baixo segmentado, cabeça frontal, mandíbulas e quatro membros angulares com pares alternados | Actor/EnemyVariant Rush, aproximação por NavMesh, Punch nativo, antecipação, contato e recuperação; 36 HP após perfil, velocidade nominal 5,25 m/s | 8 renderers, 304 triângulos |
| Emissor | Estrutura suspensa bifurcada, núcleo separado, duas carcaças que abrem e canal de emissão alinhado ao muzzle | Actor/EnemyVariant Sniper; windup nativo de 1s, direção comprometida, projétil com colisão e recuperação; 54 HP após perfil, velocidade nominal 2,975 m/s | 7 renderers, 288 triângulos |

Ambos: pivô no solo, Y para cima, frente +Z, escala de raiz 0,5 preservada e visual compensado; duas materiais compartilhadas (carcaça/núcleo), UV planar simples, normais facetadas, cápsula própria, nenhum SkinnedMeshRenderer. Os HP/velocidades de perfil diferem do valor-base serializado de 60 HP/3,5 m/s. [Inspeção de import](../validation/artifacts/run-exp-02/enemies-final-import.json).

## Arquitetura e arquivos

`Assets/_Project/Prototypes/RunExp02/Fragmento.prefab`, `Emissor.prefab`, `Meshes/*`, materiais e `DistanciaEmissor.asset`; `Scripts/Prototypes/RunExp02/RunExp02EnemyPose.cs`; `Scripts/Editor/RunExp02/RunExp02Builder.cs`.

EnemyAI, EnemyCombatActions, AttackExecution, Actor, receipts e CombatReactionController continuam donos de movimento/dano/CC/vida. A apresentação lê velocidade, windup e contagem de impactos; não aplica dano nem desloca a raiz de navegação. Reações usam um visualRoot separado da animação local. Há idle/balanço, marcha, antecipação, compromisso, recuperação e flinch. Morte usa o despawn nativo imediato, sem clip final. Desabilitar/reativar inicia nova geração; estados visuais transitórios reiniciam.

Fragmento remove PreferredDistanceLayer apenas na cópia, para chegar ao alcance curto nativo. Emissor recebe um perfil próprio de distância de 6m para a revisão; o perfil de produção não mudou. SwarmAttackCoordinator nativo limita melee no encontro; agentes e executores continuam ativos também nos benchmarks. Estatísticas locais dão atribuição Basic/causa aos recibos e são descartadas sem gravar resumo.

## Falhas intermediárias e evidência

O jogador copiado de FirstSector estava inativo aguardando seu fluxo. Ativação corrigida somente nas cenas novas. Actor.Awake adicionava CoinDrop automaticamente; a asserção o detectou e a revisão passou a removê-lo antes do primeiro frame de combate. O primeiro conjunto de malhas tinha winding invertido: faces corrigidas, normais recalculadas, teste de volume orientado/UV/normais aprovado. Não confundir os PNGs preliminares `arena-a-first*` com a entrega atual.

Capturas de detalhe são suplementares, com câmera aproximada: [Fragmento](../validation/artifacts/run-exp-02/captures/fragmento-close.png), [Emissor](../validation/artifacts/run-exp-02/captures/emissor-close.png).

Sequência real na câmera normal: [fragmento-idle.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/fragmento-idle.png), [fragmento-move.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/fragmento-move.png), [fragmento-windup.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/fragmento-windup.png), [fragmento-attack.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/fragmento-attack.png); [emissor-idle.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/emissor-idle.png), [emissor-move.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/emissor-move.png), [emissor-windup.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/emissor-windup.png), [emissor-projectile.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/emissor-projectile.png). As imagens foram abertas e inspecionadas; frames de fases próximas podem parecer similares. Emissor inicia distante e sua forma exige revisão física na escala normal; close-up não resolve esse gate.

## Comparação, skills e gate de arte

Produção mantém humanoides, malhas e controller antigos. Os candidatos usam anatomias diferentes e animação customizada, sem substituir seu executor. Skills aplicadas: tech-guy-game-vision, tech-guy-combat-unity, tech-guy-unity-acceptance, router, unity-mcp-orchestrator, unity-csharp-scripting, unity-animation, unity-navmesh, game-ai, game-feel, create-game-assets e performance-optimization.

Faltam retopologia/avaliação de interseções, UV/atlas de produção, texturas, materiais finais, LOD, rig/clips finais ou refinamento do rig customizado, contato dos pés sem deslizamento, animação final de morte e acabamento de silhueta em swarm. Paleta e nomes continuam provisórios. O teste de volume orientado não é auditoria de manifold ou prova de qualidade de topologia. Técnico habilita revisão; visual/gameplay/arte não foram aceitos por humano.

## Verificação, preservação e limites comuns

Instância MCP `Tech-Guy@fccafb2b47cd7671`, Unity `6000.5.10f1`, projeto real `D:/Dev/Personal/My_games/Tech-Guy`, branch `gameplayOverhaul`, HEAD inicial `314d28ec22ae2d242409fbfd10ea7f8368dd76e9`. O checkout antigo do chat não foi usado para implementar. O cliente `Tools/RunExp02/mcp_call.py` chama o Unity MCP existente, seleciona o projeto e conserva respostas; UTF, compilação, Console, prefabs e Play foram verificados no Editor. Não há servidor ou runner substituto.

Resultados integrais, sem somar execuções sobrepostas:

- Estabilidade: `b007fc8e1c3940cd8cc95cc4fd59dc00` — 13/13 PlayMode, 27.966s, 0 ignorados.
- Fatia final: `3862bb8a2c134ad59fab6fe78808319f` — 10/10 PlayMode, 56.621s, 0 ignorados.
- Geometria: `64ba37df12cc45638fba9ea4d14ece5f` — 1/1 EditMode, 0.512s, 0 ignorados.
- Contratos existentes selecionados: `7eb548c0fd354f1da745694aa4ade0b2` — 11/11 PlayMode, 9.650s, 0 ignorados.

Os nomes exatos, asserções, mensagens e saídas constam nos JSONs correspondentes em `Docs/validation/artifacts/run-exp-02/`. As fixtures usam imunidade do jogador somente nas sequências visuais/medições e testes especificados; o playtest normal não a recebe. Conclusão/death automatizada é prova mecânica, não vitória humana. O teste de obstrução dispara um projétil nativo diretamente, em fixture explícita, e não representa uma aquisição de poder.

306 arquivos protegidos com SHA-256 idêntico ao início, incluindo cenas publicadas, prefabs, definições, Resources, pacotes e configurações. `integrity-final.json` registra a comparação. As três chaves reais de PlayerPrefs — arma, resumo de run e tutorial — mantiveram existência, valor inteiro e hash do texto. Não se chamou Finish/Save das estatísticas na revisão, nem existe ProgressionDirector nela. As moedas adicionadas por Actor.Awake são removidas somente nos candidatos instanciados na revisão. As alterações anteriores em `Docs.zip`/`Docs (2).zip` foram preservadas.

Limite da auditoria de saves: o inventário inicial do registro usou separador incorreto e retornou vazio; ele não demonstra a totalidade das preferências. A comparação válida cobre as três chaves lidas por PlayerPrefs e os arquivos disponíveis, majoritariamente logs/testes/analytics. Não se alega um hash integral do perfil humano. RunSummary foi protegido pela fixture de isolamento existente. Não houve reset de preferências.

Arquivos desta frente: [manifesto](../validation/artifacts/run-exp-02/files-manifest.json), acrescido destes seis relatórios e das atualizações no ledger, matriz, masterplan, requisitos, design, tarefas e estado do projeto. Nenhum prefab publicado foi substituído e nenhum GUID histórico foi recriado. O estudo RUN-EXP-01 continua disponível. Editor final em NexusBlockout, Edit Mode, cena limpa; ver `editor-final.json`. Não houve commit/push nesta nova frente.

Gates: **técnico aprovado no recorte testado**; **gameplay e visual aguardam revisão física**; **arte de produção pendente**. Teste verde não aceita feel, leitura sem rótulos ou arte final. Não publicar candidatos/salas/UI na progressão antes dessa revisão. STOP após 02.4.

Conferência final dos arquivos do persistentDataPath: [save-files-final.json](../validation/artifacts/run-exp-02/save-files-final.json). Quatro dos cinco hashes permanecem iguais; somente TestResults.xml mudou pela execução UTF. Isso não é alteração de save de gameplay. Console final sem erros;306/306 hashes protegidos e três preferências conferidas, com os limites de cobertura já registrados.
