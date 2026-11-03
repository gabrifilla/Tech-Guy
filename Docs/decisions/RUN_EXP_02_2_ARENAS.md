# RUN-EXP-02.2 — duas arenas jogáveis

## Escopo e condições controladas

`Assets/_Project/Prototypes/RunExp02/ArenaA.unity` e `ArenaB.unity`, com NavA/NavB próprios e entrada pelo menu Tools/Tech Guy/RUN-EXP-02. Jogador/controlador/câmera copiados da FirstSector real, HUD existente, Manopla equipada somente em memória; sem carregar seu ProgressionDirector. Piso 24×20m, mesma câmera, player inicial (0,0,-6), dois Fragmentos e um Emissor, tuning e conclusão por três mortes iguais. Spawns pedidos em (-4,0,4), (4,0,4), (0,0,7), ajustados ao piso navegável quando há cobertura.

| Aspecto | Arena A | Arena B |
|---|---|---|
| Espaço | Piso aberto e limites físicos | Duas massas sólidas deslocadas, 4,2m de largura × 4,2m de profundidade × 2,2m de altura |
| Decisão | Espaçar, circular e escolher entre pressão próxima e tiro | Entrar em sombra de tiro, contornar pela esquerda/direita e escolher por qual borda aproximar |
| Circulação verificada | Rotas laterais livres | Duas rotas laterais x=-7/x=7 completas; sem spawn sobre o topo |
| LOS/projétil | Linha escolhida livre | A mesma linha x=-2,3 cruza massa opaca e bloqueia projétil nativo |
| Melee | Alcance e esquiva | Profundidade separa os volumes dos básicos Manopla/Lança; exige contorno |

O corredor visual estreito central não é uma terceira rota de navegação. A geometria não torna todo ataque impossível: os mesmos básicos são disparados em área livre e os caminhos levam aos inimigos. O layout não garante vitória, segurança permanente ou que toda habilidade de área obedeça LOS.

## Correção guiada por teste

Cobertura inicial de 1,4m permitia ao básico legado de Lança (3,6m, volume sem LOS) atingir o outro lado. Reprodução: job `7fd066ef6b754ac699edb2b896b4146a`, 0/1, HP1000→978. Não se alterou o executor publicado para mascarar isso: a massa do blockout ganhou profundidade suficiente para separar os volumes dos básicos. Isso é proteção por métricas espaciais, não uma nova regra universal de oclusão. Áreas grandes dos executores publicados continuam com suas próprias regras; essa compatibilidade ampla exige revisão posterior antes de publicação.

O rebake seguinte revelou PathPartial porque o topo espesso podia receber spawn como ilha. NavMeshModifier Not Walkable nas coberturas corrigiu a autoria; NavB foi rebakeado preservando seu GUID. O teste continuou exigindo PathComplete e duas rotas, sem relaxamento. Foi corrigido também o sinal de uma asserção do limite sul no teste de dash.

## Evidência, arquivos e skills

[Arena B em combate](../validation/artifacts/run-exp-02/captures/arena-b-gauntlet-current-1.png) · [pair-start-normal-camera.png](../validation/artifacts/run-exp-02/integration/20261030-220628-8278188-7de59e16f6f845a1983b5148df31c738/pair-start-normal-camera.png). A fixture também captura A em `native-combat-open.png`/`combat-open.png`. São capturas da câmera real, com combate automatizado, não declaração de pacing divertido. Diferença observada mecanicamente: o mesmo raio/projétil deixa de cruzar a cobertura, caminhos desviam e básicos separados pela massa não atingem o alvo. Tempo de reação humano, prioridade percebida e obstrução de câmera em movimento ainda precisam de playtest.

Arquivos: cenas, NavMeshData e materiais novos; RunExp02Builder, RunExp02Review e testes RunExp02IntegrationTests. Skills: level-design, tech-guy-level-layout, tech-guy-enemy-encounters, unity-navmesh, unity-mcp-orchestrator, tech-guy-combat-unity e tech-guy-unity-acceptance. Nenhuma sala foi adicionada ao grafo publicado.

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

Capturas adicionais de Play via MCP, sem imunidade e com Manopla: [Arena A](../validation/artifacts/run-exp-02/captures/arena-a-gauntlet-current.png) / [Arena B](../validation/artifacts/run-exp-02/captures/arena-b-gauntlet-current-1.png). Probes `arena-a-native-combat.json` e `arena-b-native-combat.json` confirmam dano ao jogador e agentes válidos; tempos de observação diferentes, não usar HP como comparação causal de dificuldade. `combat-cover.png` da fixture termina com Arco após a checagem das três armas.

Conferência final dos arquivos do persistentDataPath: [save-files-final.json](../validation/artifacts/run-exp-02/save-files-final.json). Quatro dos cinco hashes permanecem iguais; somente TestResults.xml mudou pela execução UTF. Isso não é alteração de save de gameplay. Console final sem erros;306/306 hashes protegidos e três preferências conferidas, com os limites de cobertura já registrados.
