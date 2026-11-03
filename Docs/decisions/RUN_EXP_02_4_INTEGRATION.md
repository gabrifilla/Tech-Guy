# RUN-EXP-02.4 — pacote de revisão integrado

## Escopo entregue e reprodução

1. No Editor correto, em Edit Mode: **Tools → Tech Guy → RUN-EXP-02 → 1 - Abrir arena aberta**; entre em Play.
2. Use WASD/mira e o kit atual conforme HUD/rebindings. Há dois Fragmentos e um Emissor, com dano, navegação, antecipação e morte reais. Não existe imunidade no fluxo normal.
3. Use **Arena B / reiniciar** para o encontro equivalente com massas deslocadas. Reiniciar recarrega somente a cena isolada; não altera Build Settings.
4. **Só Fragmento** / **Só Emissor** permitem inspeção independente. Esses botões substituem o encontro corrente; não curam o jogador. Reiniciar a arena repõe seu estado.
5. **Recompensas** pausa combate e abre consulta sem aquisição. Troque a arma da consulta, compare os textos e volte ao combate com Manopla.
6. Saia de Play. **Tools → Tech Guy → RUN-EXP-02 → 3 - Retornar ao Nexus** volta à cena corrente do projeto. As cenas de revisão ficam salvas no projeto.

Arquivos de integração: RunExp02Review/RewardReview/EnemyPose, RunExp02Builder, duas cenas, dois prefabs, dados de navegação, materiais/malhas próprios; três arquivos de testes novos. Não houve integração com o gerador publicado ou novo singleton. Cada encontro possui seus atores e estatísticas locais, descarta subscriptions e não chama gravação de run.

## Validação mecânica e jobs

Asserções finais cobrem os dois executores navegando e removendo HP real, pausa sem mudança de posição/HP, morte/conclusão sem projéteis residuais, nova geração após reativação, CC limitado com pausa e recuperação, recibo com atribuição Basic, rotas completas A/B, LOS físico, projétil nativo bloqueado, dash nos limites, básicos Manopla/Lança/Arco disparados em área livre e separação dos básicos melee através da massa da cobertura. Loop de controle indefinido não foi observado no recorte; feel humano continua pendente.

O job de contratos existentes verifica WASD virtual/diagonal sem conjurar W, foco/pausa/release, motor/pull/dash em parede, hitstop, sliding, retorno de modal, interrupção de ataque, raiz/ranks de projétil e limpeza/expiração de Protocol. Inclui a terceira fixture histórica que colidia em diretório de evidência, agora aprovada. Não substitui a suíte histórica completa nem aceita todas as combinações de habilidades contra toda geometria.

## Falhas intermediárias conservadas

- Compilação inicial dos testes: chamada de EnemyProjectile.Spawn com assinatura incorreta e Newtonsoft indisponível na assembly de testes; corrigidos argumento e serialização JsonUtility, sem editar dependências.
- Primeiro job de integração `c30238930ab448e981e07ce564f4cff4`: 1/8, sete casos barrados pela asserção CoinDrop. Correção local do spawner.
- `526dd98343844f35868024249ad078df`: 7/8; sinal invertido na asserção do limite de cobertura.
- `fa368184c9c84dc6abbcaa17aaf73bf0`: 8/9; Source nulo sem observador de estatísticas na revisão. Adicionado o observador real, sem persistência.
- `3c4d953598664f078128d9fd72ddbc8e` e `3835cf4de6af4aa7a6e0a0e351a8110f`: 9/9 antes do caso novo de cobertura. O último já usa faces corrigidas e marcadores somados por frame.
- `7fd066ef6b754ac699edb2b896b4146a`: 0/1 para Lança através da cobertura fina. Espessura corrigida; `4ee825ae1d794a989fd68b23d1c009bc`: 9/10, revelou ilha navegável no topo. NavMeshModifier e rebake corrigiram a autoria, sem retirar PathComplete da asserção.

Os JSONs e imagens intermediários permanecem. As figuras curadas no consolidado apontam à execução final, não aos estados preliminares.

## Custos observados, sem aprovação de orçamento

Editor Windows, Ryzen 5 5600X/12 threads e RTX3060Ti, Game View1160×600. Arena A, aproximadamente 2/3 Fragmentos e 1/3 Emissores, 1s de aquecimento e 5s medidos por cenário; todos os agentes têm EnemyAI habilitada, NavMesh válida, executores, reações e poses. Jogador imune apenas para sustentar a fixture. Não se reduziu a população nem se desligou IA. Sem reescrita de pooling.

| Inimigos ativos | CPU mediana/p95 ms | Behaviour mediana ms | Nav mediana ms | Pose mediana ms | GC mediana bytes/frame | Renderers / triângulos | Windups máx. |
|---|---|---|---|---|---|---|---|
| 8/8 | 3.094/4.613 | 0.492 | 0.057 | 0.013 | 297648 | 62 / 2400 | 5 |
| 32/32 | 4.994/6.998 | 0.668 | 0.180 | 0.049 | 501608 | 246 / 9568 | 10 |
| 64/64 | 6.986/10.245 | 0.922 | 0.277 | 0.099 | 741704 | 491 / 19120 | 22 |


Fonte: [performance-summary.json](../validation/artifacts/run-exp-02/performance-summary.json) e séries cruas ali referenciadas. Main Thread inclui o Editor, UI, testes e instrumentação; não é tempo exclusivo do jogo ou medida de GPU. BehaviourUpdate é agregado e **não isola EnemyAI**. NavMeshManager é agregado; marcador Components.NavMeshAgent.State retornou zero e não sustenta uma medida isolada. Pose usa marcador próprio somado por frame. Candidatos têm zero skinning; os marcadores de skinning globais incluem o jogador. Contagens de renderers/triângulos são da geometria dos candidatos, não incluem todos os VFX/player. Duas materiais compartilhadas; telegraphs medidos por ações em windup e projéteis, não inventário exaustivo de todas as rings/fills.

GC é alto no Editor e inclui alocações da coleta LINQ, UTF e UI; não atribuir toda a diferença aos inimigos nem declarar otimização de produção. Os recortes de cinco segundos não substituem build, amostra longa, picos de spawn/despawn ou profiling em hardware alvo. A limpeza é verificada mecanicamente; não há distribuição dedicada do seu custo. Os primeiros dumps com muitos marcadores não somavam todos os samples; somente a série final resumida usa SumAllSamplesInFrame. Skills: performance-optimization, unity-mcp-orchestrator, unity-navmesh, tech-guy-unity-acceptance e as skills de implementação citadas nos relatórios1–3.

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
