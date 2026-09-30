# Requirements Document

## Introduction

Esta feature trata dos **stutters grandões** (picos grandes de tempo de frame) sentidos ao rodar o
Tech Guy pelo **Unity Editor**, junto de uma passada de **limpeza de projeto** e **otimização de
performance**. A investigação ao vivo (Unity Profiler + inspeção de código e de assets) apontou
causas concretas e evidências mensuráveis.

**Sintoma principal — pressão de GC.** O heap gerenciado está anormalmente grande: **GC Reserved
~1,77 GB** e **GC Used ~1,41 GB**. Coletas de lixo grandes sobre um heap desse tamanho são a causa
provável dos picos grandes de frame. Há **alocação por frame mesmo quase-parado** (~997 B em ~15
allocs num frame ocioso), ou seja, código de hot path está gerando lixo continuamente. A App
Committed Memory está em ~5 GB (residente ~271 MB).

**Fontes confirmadas de alocação por frame (arquivos verificados):**

- **`PlayerHUD.Update`** (`Assets/_Project/Scripts/UI/PlayerHUD.cs`): monta 3–5 strings interpoladas
  (`$"{...}"`) **todo frame** (vida/mana/asura/tooltip/feedback), mesmo sem mudança de valor.
- **`RunRewardUI`** (`Assets/_Project/Scripts/UI/RunRewardUI.cs`): desenho em estilo IMGUI (`OnGUI`)
  com montagem de string por frame enquanto a tela de recompensa está ativa — IMGUI aloca muito.
- **`CombatReadabilityUI.LateUpdate`**: monta strings + `WorldToScreenPoint` por frame.
- **`OutlineScript.Update`** (`Assets/_Project/Scripts/OutlineScript.cs`): `Camera.main` +
  `Physics.Raycast` + vários `GetComponent<Outline>()` **todo frame**.
- **`Actor.Update`** (`Assets/_Project/Scripts/Characters/Actor.cs`): chama `UpdateHealthBar()` todo
  frame para **cada** actor.
- **Instantiate/Destroy em runtime sem pooling**: projéteis do player (`ArsenalProjectile` cria
  GameObject + LineRenderer + Material por tiro e destrói material no `OnDestroy`), projéteis de
  inimigo (`EnemyProjectile`) e moedas (`CoinPickup`, Instantiate/Destroy por moeda). Já existe um
  **pool de efeitos de hit** (`HitboxDamage.effectPools`) que serve de precedente a espelhar.
- **`Resources.Load` em runtime**: `WeaponLoadout.Current/Equip` (`Resources.Load<WeaponScript>` na
  troca de arma) e carga de efeitos em `HitboxDamage` — primeiro acesso pode gerar hitch.
- Vários `MonoBehaviour` com `Update`/`LateUpdate` que fazem `GetComponent` por frame (`EnemyAI`,
  `PreferredDistanceLayer`, `FrontalReflector`, `CombatReactionController`, `ProtectionIndicator`,
  `Shield`, `PriorityTargetMarker`, `StatusEffect`, etc.).

**Baseline de assets (Profiler, ao vivo):** Asset Count 13.382; Texture Count 1.100; Texture Memory
~91 MB; Material Count 163; Mesh Count 30; Object Count 16.428; Scene Object Count 3.046; GameObject
Count 579.

**Fontes de peso e limpeza:**

- **`Assets/_ThirdParty` = 603 MB ≈ 85% do projeto.** Destaque: **"STYLIZED MALE CHARACTER" 565 MB**
  (de longe o maior — candidato primário a remoção/trim se não usado ou só parcialmente referenciado),
  **FastScriptReload 22 MB** (ferramenta de auto-reload de scripts em tempo de dev que loga "full
  reload will be triggered" — plausível contribuinte de stutter no Editor), Blink 9,6 MB, TextMesh
  Pro 4,9 MB (**MANTER** — usado pela UI), itsmars Health Orb 0,7 MB, Ultimate RPG Items Pack 0,5 MB,
  RPG Characters 0,3 MB, QuickOutline ~0 MB (usado pelo `OutlineScript`).
- `_Project` 110 MB, `Plugins` 12 MB.
- 468 scripts `.cs`, 53 prefabs, 130 materiais, 103 texturas de origem, 8 cenas — provável presença
  de scripts de dev/editor órfãos e assets não referenciados.

**Objetivo, sem alterar gameplay/visual observável:** estabelecer um baseline mensurável; eliminar
alocações por frame no HUD/UI; introduzir object pooling para projéteis e moedas; otimizar lookups
em hot paths; pré-carregar/cachear assets de `Resources`; otimizar import de texturas/assets;
identificar com segurança assets órfãos e pacotes third-party não usados (removendo apenas após
confirmação item-a-item do usuário); e revisar o FastScriptReload como possível causa de stutter no
Editor.

**Realidade de validação (honesta):** as metas de performance são validadas via **Unity Profiler +
playtest manual do usuário**. O Play mode do Unity não pode ser dirigido de forma confiável via CLI
neste ambiente (o test runner de EditMode já sofre timeout). Testes automatizados cobrem **apenas
lógica pura** (ex.: um object pool). Onde a validação não puder ser executada via CLI, a feature
valida por Profiler + inspeção + `git diff` e **relata explicitamente** o que não foi validado.

Esta feature **não** adiciona conteúdo novo de gameplay, não muda balanceamento, não altera GUIDs ou
caminhos especiais sem checar os dependentes, e não modifica código third-party (salvo revisão
explícita do FastScriptReload). Ela move-em-vez-de-recriar e preserva `.meta`/GUID.

## Glossary

- **Stutter / pico de frame**: aumento abrupto e perceptível no tempo de um frame, sentido como
  travada; nesta feature, associado principalmente a coletas de GC grandes.
- **GC (Garbage Collection)**: coleta de memória gerenciada do .NET/Mono. **Pico de GC** é a pausa
  causada por uma coleta; **alocação por frame** é o lixo gerado a cada frame que alimenta essas
  coletas.
- **Heap gerenciado**: memória reservada/usada pelo runtime gerenciado (`GC Reserved`/`GC Used` no
  Profiler).
- **Hot path**: código executado com muita frequência (tipicamente `Update`/`LateUpdate`/`FixedUpdate`
  por frame), onde alocações e lookups custam caro.
- **Object pool (pool de objetos)**: conjunto reutilizável de instâncias pré-criadas, evitando
  `Instantiate`/`Destroy` em runtime; precedente no projeto: `HitboxDamage.effectPools`.
- **Draw call / batch**: comando de desenho enviado à GPU; contagem alta impacta CPU/GPU.
- **Import settings**: configurações de importação de um asset no Unity (ex.: compressão, mipmaps,
  `maxTextureSize`, tipo de textura).
- **Atlas de textura**: agrupamento de várias texturas numa só para reduzir draw calls/estado.
- **Asset órfão (orphan)**: asset presente no projeto que **nenhuma** cena/prefab/ScriptableObject
  referencia (por GUID) e que não é carregado por `Resources.Load`.
- **GUID / `.meta`**: identificador único de asset gerado pelo Unity e armazenado no arquivo `.meta`
  correspondente; referências entre assets são feitas por GUID.
- **`Resources`-managed**: assets sob `Assets/**/Resources` que podem ser carregados por caminho via
  `Resources.Load`, portanto sem referência estática rastreável por GUID.
- **Baseline**: medição de referência capturada antes das mudanças, usada para comparar ganhos.
- **Portão de segurança "identificar → confirmar → remover"**: fluxo em que a remoção de qualquer
  asset só ocorre após confirmação explícita do usuário, item a item.

## Requirements

### Requisito 1 — Baseline de performance mensurável

**User Story:** Como desenvolvedor, quero capturar um baseline de performance antes de otimizar, para
medir ganhos de forma objetiva e saber onde estão os piores custos.

#### Acceptance Criteria

1. WHEN a passada de otimização começa THEN o desenvolvedor SHALL capturar, via Unity Profiler, um
   baseline contendo no mínimo: `GC Reserved` e `GC Used` (heap gerenciado), `GC Allocated In Frame`
   (alocação por frame) em estado ocioso e em combate, tempo de frame (CPU) médio/pico, e as
   contagens de objetos/assets/texturas/materiais relevantes.
2. WHEN o baseline é capturado THEN o desenvolvedor SHALL registrá-lo em um artefato versionado do
   projeto (ex.: documento no diretório da spec), com os valores observados e a data/contexto da
   medição.
3. WHEN as metas de otimização são definidas THEN o sistema SHALL expressá-las como metas
   **relativas** ao baseline (ex.: reduzir `GC Allocated In Frame` em estado ocioso para próximo de
   zero; reduzir a frequência/tamanho dos picos de GC durante combate), evitando números absolutos
   não verificáveis neste ambiente.
4. IF uma métrica-alvo não puder ser medida de forma confiável via CLI THEN o sistema SHALL
   declará-la como validada por Profiler + playtest manual e SHALL registrar essa restrição.
5. WHEN a otimização é concluída THEN o desenvolvedor SHALL capturar uma medição pós-mudança
   comparável ao baseline (mesmas métricas, mesmo roteiro) para evidenciar o ganho.

### Requisito 2 — Eliminar alocações por frame no HUD/UI

**User Story:** Como jogador, quero que a UI não gere lixo continuamente, para o jogo não acumular
pressão de GC e travar durante o gameplay.

#### Acceptance Criteria

1. WHILE nenhum valor exibido mudou THEN o `PlayerHUD` SHALL **não** remontar strings interpoladas
   por frame (vida/mana/asura/tooltip/feedback), reconstruindo o texto somente quando o valor de
   origem muda.
2. WHEN um valor exibido pelo `PlayerHUD` muda THEN o sistema SHALL atualizar apenas o texto afetado,
   preservando o conteúdo e o formato atualmente exibidos.
3. WHEN a tela de recompensa (`RunRewardUI`) está ativa THEN o sistema SHALL evitar montagem de
   string por frame no caminho de desenho, cacheando o texto e reconstruindo-o somente quando os
   dados de recompensa mudam, preservando o layout e as informações mostradas.
4. WHILE o `CombatReadabilityUI` está ativo THEN o sistema SHALL evitar montar strings por frame e
   SHALL só recalcular/atualizar quando os valores de origem mudarem, mantendo o posicionamento em
   tela dos elementos.
5. WHEN as otimizações de UI são aplicadas THEN o sistema SHALL reduzir de forma mensurável o
   `GC Allocated In Frame` atribuível à UI em estado ocioso, comparado ao baseline do Requisito 1.
6. WHEN qualquer texto/indicador de UI é atualizado THEN o sistema SHALL manter o resultado visual
   idêntico ao atual para o mesmo estado de jogo (sem regressão de conteúdo ou formatação).

### Requisito 3 — Object pooling para projéteis e moedas

**User Story:** Como jogador, quero que disparos e moedas não criem e destruam objetos em runtime,
para evitar picos de GC e hitches durante o combate e a coleta.

#### Acceptance Criteria

1. WHEN o player dispara (`ArsenalProjectile`) THEN o sistema SHALL obter a instância de projétil de
   um pool reutilizável em vez de `Instantiate`, e SHALL devolvê-la ao pool em vez de `Destroy`.
2. WHEN um inimigo dispara (`EnemyProjectile`) THEN o sistema SHALL obter e devolver a instância a um
   pool reutilizável, sem `Instantiate`/`Destroy` por disparo.
3. WHEN uma moeda (`CoinPickup`) é gerada ou coletada THEN o sistema SHALL usar um pool reutilizável
   em vez de `Instantiate`/`Destroy` por moeda.
4. WHEN uma instância é devolvida ao pool THEN o sistema SHALL resetar seu estado (posição,
   velocidade, alvos de homing, contadores de pierce, LineRenderer/Material e demais campos) de modo
   que uma reutilização subsequente se comporte como uma instância recém-criada.
5. WHERE já existe o pool de efeitos de hit (`HitboxDamage.effectPools`) THEN o sistema SHALL
   espelhar esse padrão existente para os novos pools, mantendo coesão de implementação.
6. WHEN o pooling é aplicado THEN o sistema SHALL preservar o comportamento observável dos projéteis
   e moedas (dano, pierce, homing, ricochete, alcance, efeitos visuais e valor coletado), sem
   diferença perceptível para o jogador.
7. IF o pool está vazio no momento do uso THEN o sistema SHALL expandir o pool (criar nova instância)
   sem falhar nem descartar o disparo/moeda.
8. WHEN um material/LineRenderer é gerenciado pelo pool THEN o sistema SHALL evitar criar e destruir
   materiais por disparo, reutilizando-os e prevenindo vazamento de material.

### Requisito 4 — Otimizar lookups em hot paths

**User Story:** Como jogador, quero que scripts executados todo frame não façam buscas caras
repetidas, para reduzir custo de CPU e alocações por frame.

#### Acceptance Criteria

1. WHEN `OutlineScript` roda por frame THEN o sistema SHALL evitar `Camera.main` e `GetComponent`
   repetidos por frame, cacheando a câmera e os componentes de `Outline`, e SHALL limitar a
   frequência do raycast (throttle) sem alterar o comportamento de destaque percebido.
2. WHEN a vida de um `Actor` não muda THEN o sistema SHALL **não** chamar `UpdateHealthBar()` naquele
   frame, atualizando a barra de vida somente em resposta à mudança de vida (evento), preservando o
   visual da barra.
3. WHEN um `MonoBehaviour` em hot path precisa de um componente/referência estável THEN o sistema
   SHALL resolver essa referência uma vez (ex.: em `Awake`/`Start`) e reutilizá-la, em vez de chamar
   `GetComponent` por frame, para os casos identificados na investigação (ex.: `EnemyAI`,
   `PreferredDistanceLayer`, `FrontalReflector`, `CombatReactionController`, `ProtectionIndicator`,
   `Shield`, `PriorityTargetMarker`, `StatusEffect`).
4. WHEN as otimizações de hot path são aplicadas THEN o sistema SHALL preservar o comportamento
   observável (mesmos alvos destacados, mesma reação de combate, mesmas barras/indicadores), sem
   regressão funcional.
5. IF uma referência cacheada estiver ausente em runtime THEN o sistema SHALL validar a dependência
   em `Awake`/`OnValidate` e falhar de forma clara (log/aviso) em vez de gerar `NullReferenceException`
   silenciosa por frame.

### Requisito 5 — Preload/cache de assets carregados via `Resources`

**User Story:** Como jogador, quero que a troca de arma e o disparo de efeitos não travem no primeiro
uso, para o combate fluir sem hitch de primeiro acesso.

#### Acceptance Criteria

1. WHEN o `WeaponLoadout` carrega ou troca de arma (`Resources.Load<WeaponScript>`) THEN o sistema
   SHALL evitar `Resources.Load` no caminho de troca em runtime, pré-carregando/cacheando as armas
   necessárias em um ponto de inicialização, preservando qual arma fica equipada.
2. WHEN um efeito referenciado por `HitboxDamage` é carregado via `Resources` THEN o sistema SHALL
   cachear/pré-carregar o efeito de modo que o primeiro uso não dispare uma carga síncrona de disco
   durante o combate.
3. WHEN o preload/cache é aplicado THEN o sistema SHALL preservar o comportamento observável (mesma
   arma equipada, mesmos efeitos, mesmos dados), sem alterar o gameplay.
4. WHERE um asset está sob uma pasta `Resources` THEN o sistema SHALL tratá-lo como Unity-managed e
   SHALL **não** movê-lo/removê-lo sem antes confirmar que nenhum `Resources.Load` depende do caminho.
5. IF um asset esperado não puder ser pré-carregado THEN o sistema SHALL registrar um aviso claro e
   cair no comportamento atual (carga sob demanda), sem quebrar o combate.

### Requisito 6 — Otimização de import de texturas e assets

**User Story:** Como desenvolvedor, quero ajustar as configurações de importação de assets pesados,
para reduzir memória e custo de renderização sem degradar a aparência.

#### Acceptance Criteria

1. WHEN texturas de origem são revisadas THEN o sistema SHALL ajustar import settings (ex.:
   `maxTextureSize`, compressão adequada por plataforma, `mipmaps` conforme o uso 3D/UI) de forma que
   a memória de textura reportada pelo Profiler diminua em relação ao baseline do Requisito 1.
2. WHEN uma textura é usada estritamente como sprite/UI THEN o sistema SHALL aplicar as configurações
   apropriadas de UI (ex.: sem mipmaps quando desnecessário), preservando a nitidez percebida na
   resolução de uso.
3. WHERE agrupar texturas em atlas reduzir draw calls sem perda visual THEN o sistema SHALL considerar
   atlas, aplicando-o apenas onde o ganho for demonstrável.
4. WHEN qualquer import setting é alterado THEN o sistema SHALL preservar a aparência do asset em
   cena/UI (sem artefatos, blur ou banding perceptíveis) em comparação ao estado atual.
5. WHEN import settings são alterados THEN o sistema SHALL preservar o `.meta`/GUID do asset (edição
   de settings de importação, sem recriar o asset), mantendo intactas as referências existentes.
6. WHERE um asset é third-party THEN o sistema SHALL limitar-se a ajustar import settings (dado
   explícito) e SHALL **não** modificar o conteúdo/código do pacote.

### Requisito 7 — Identificação segura de assets órfãos e third-party não usados

**User Story:** Como desenvolvedor, quero identificar com segurança assets e pacotes não usados e
removê-los apenas com minha confirmação, para reduzir o peso do projeto sem quebrar referências.

#### Acceptance Criteria

1. WHEN a análise de uso roda THEN o sistema SHALL identificar candidatos a órfãos buscando
   referências de GUID de cada asset em arquivos `.unity`, `.prefab`, `.asset` e `.meta`, e SHALL
   marcar como candidato apenas os assets sem nenhuma referência encontrada.
2. WHEN um asset candidato está sob uma pasta `Resources` THEN o sistema SHALL tratá-lo como possível
   alvo de `Resources.Load` por caminho e SHALL **não** classificá-lo como órfão sem confirmar a
   ausência de uso por caminho.
3. WHEN a análise termina THEN o sistema SHALL produzir um **relatório** legível listando cada
   candidato (caminho, tamanho, motivo/ausência de referências), incluindo os pacotes third-party
   suspeitos de não uso (com destaque para "STYLIZED MALE CHARACTER" ~565 MB).
4. WHEN a remoção é proposta THEN o sistema SHALL exigir **confirmação item-a-item do usuário** antes
   de excluir qualquer asset (portão "identificar → confirmar → remover"); o sistema SHALL **não**
   remover nada de forma silenciosa ou em lote sem confirmação.
5. IF qualquer referência a um asset for encontrada (em cena, prefab, ScriptableObject ou por caminho
   de `Resources`) THEN o sistema SHALL **rejeitar** a remoção desse asset e SHALL relatar onde a
   referência existe.
6. WHEN um asset é removido após confirmação THEN o sistema SHALL remover o asset **junto do seu
   `.meta`** e SHALL não deixar `.meta` órfão nem GUID pendente.
7. WHEN a lista de mantidos é definida THEN o sistema SHALL preservar pacotes comprovadamente usados
   (ex.: TextMesh Pro pela UI, QuickOutline pelo `OutlineScript`), sem propô-los para remoção.
8. WHEN o relatório é gerado THEN o sistema SHALL distinguir "remoção total do pacote" de "trim
   parcial" (ex.: remover só sub-assets não usados de um pacote grande), sempre sujeito à confirmação
   do usuário.

### Requisito 8 — Revisão do FastScriptReload como possível causa de stutter no Editor

**User Story:** Como desenvolvedor, quero avaliar se o FastScriptReload está contribuindo para os
stutters no Editor, para poder mitigar sem remover uma ferramenta útil por engano.

#### Acceptance Criteria

1. WHEN o FastScriptReload é avaliado THEN o sistema SHALL verificar se o auto-reload em tempo de
   dev (que loga "full reload will be triggered") coincide com picos de frame no Editor, registrando
   a evidência observada.
2. WHERE o auto-reload durante o Play mode é identificado como contribuinte de stutter THEN o sistema
   SHALL propor **desabilitar o auto-reload durante o play** (via configuração da ferramenta), em vez
   de remover a ferramenta.
3. WHEN uma mudança de configuração do FastScriptReload é proposta THEN o sistema SHALL exigir
   confirmação do usuário e SHALL **não** modificar o código-fonte do pacote third-party.
4. IF a avaliação for inconclusiva THEN o sistema SHALL relatar o resultado como inconclusivo e
   SHALL manter a ferramenta no estado atual, sem alteração às cegas.

### Requisito 9 — Sem regressão de gameplay e visual

**User Story:** Como jogador, quero que a limpeza e a otimização não mudem como o jogo se joga nem
como ele parece, para eu só sentir menos travadas.

#### Acceptance Criteria

1. WHEN as otimizações são aplicadas THEN o sistema SHALL preservar o comportamento observável de
   combate, câmera, UI e cenas idêntico ao atual para o mesmo estado de jogo.
2. WHEN um asset é movido, renomeado ou removido THEN o sistema SHALL preservar `.meta`/GUID
   conforme aplicável e SHALL **não** alterar GUIDs, nomes de asset referenciados ou caminhos
   especiais do Unity sem antes checar as cenas, prefabs e ScriptableObjects que dependem deles.
3. WHEN uma mudança poderia afetar referências THEN o sistema SHALL mover-em-vez-de-recriar e
   verificar os dependentes em `.unity`, `.prefab`, `.asset` e `.meta` antes de concluir.
4. WHEN a passada termina THEN o sistema SHALL manter as 8 cenas abríveis e funcionais, sem
   referências quebradas (missing scripts/assets) introduzidas pela mudança.
5. WHERE a mudança toca código third-party THEN o sistema SHALL abster-se, salvo a revisão explícita
   de configuração do FastScriptReload (Requisito 8).
6. WHEN as mudanças são feitas THEN o sistema SHALL manter o escopo restrito a limpeza e performance,
   sem refatorações grandes não relacionadas.

### Requisito 10 — Restrição e transparência de validação

**User Story:** Como desenvolvedor, quero saber exatamente o que foi validado e como, para confiar no
resultado mesmo quando o Play mode não pode ser dirigido via CLI.

#### Acceptance Criteria

1. WHERE o Play mode ou testes de PlayMode não puderem ser executados de forma confiável via CLI
   (o test runner de EditMode já sofre timeout neste ambiente) THEN o sistema SHALL validar por
   Unity Profiler + inspeção de referências + `git diff` e SHALL **relatar explicitamente** o que
   não foi validado automaticamente.
2. WHEN há lógica pura passível de teste automatizado (ex.: um object pool) THEN o sistema SHALL
   adicionar testes automatizados para essa lógica, independentes de cena/Play mode.
3. WHEN as metas de performance são verificadas THEN o sistema SHALL apoiar a verificação em capturas
   do Profiler (antes/depois) e em playtest manual do usuário, documentando o roteiro usado.
4. WHEN a feature é concluída THEN o sistema SHALL entregar um resumo de validação distinguindo
   claramente: o que foi medido no Profiler, o que foi verificado por inspeção/`git diff`, e o que
   depende de playtest manual do usuário.
