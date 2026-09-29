# Requirements Document

## Introduction

Esta funcionalidade reformula a **direção de gameplay das três armas** de Tech-Guy — **Manoplas** (punhos), **Lança** e **Arco** — para que cada arma resolva o **mesmo enxame de inimigos** de um jeito próprio e reconhecível, e não apenas como "corpo a corpo curto / corpo a corpo longo / à distância". A reformulação está ancorada nos sistemas de combate já existentes no projeto Unity 3D (C#): `CombatReactionController` (postura/stagger/quebra de postura), `HitReactionRequest` + `HitReactionType` + `StanceBreakEffect` + `HitStrength`, `EnemyRank`, os arquétipos de inimigo (`ArchetypeId`, `CombatRole`, `EnemyProfile`, `EnemyVariant`, `EnemyAI`), as habilidades de arma (`ArsenalAbility` / `ArsenalCombat` / `ArsenalSkillKind`, `AreaHitStep`, `BreakerGauntletAbility`, `AsuraMomentum`) e os modificadores de run (`WeaponRunModifiers`, `WeaponBoon`, `ArsenalCastPlan`).

A intenção de design é que cada arma tenha uma **identidade tática** distinta:

- **Manoplas** — brigão agressivo que entra no grupo, interrompe, quebra postura, mantém pressão, faz juggle e acumula **Asura**.
- **Lança** — controladora de espaço que manipula distância, linhas, varreduras e posicionamento, com a mecânica central de **Sweet Spot** (ponto ideal perto da ponta).
- **Arco** — caçadora tática que prioriza alvos, se posiciona, controla trajetória e mobilidade, com a mecânica exclusiva de **Marca** (Mark).

A reformulação também define um **Sistema Universal de Reação de Inimigos** (separando dano, stagger, dano de postura, quebra de postura, empurrão, lançamento, atordoamento e knockback), três **níveis de intensidade de deslocamento**, **sistemas de comportamento de enxame** (slots de ataque, distância preferida, agrupamento suave, regras de interrupção, resistência de postura por raridade), a **matriz Arma × Inimigo** e a **integração progressiva dos modificadores de run**.

### Restrição crítica de implementação (contexto Unity)

Este é um projeto Unity 3D em C#. **Antes** de implementar ou alterar qualquer mecânica desta funcionalidade, o estado atual do projeto (scripts, componentes, prefabs, ScriptableObjects, habilidades, sistema de atributos, sistema de postura, reações a acerto, movimento, sistema de armas, modificadores de run, inimigos, animações/estados e arquitetura) **deve ser inspecionado** — via **Unity MCP** quando disponível, e, quando o Unity MCP não estiver disponível, via inspeção de estrutura do projeto e busca de referências, conforme as regras do projeto (`AGENTS.md`). A prioridade é **reutilizar e estender** a infraestrutura existente, manter componentes desacoplados e comportamento configurável por dados, e **não recriar** sistemas nem construir uma implementação paralela sem confirmar a arquitetura atual.

### Escopo

**Dentro do escopo:** identidade e loop de cada arma (Manoplas, Lança, Arco) e suas habilidades Q/W/E/R; o Sistema Universal de Reação com diferenciação por raridade; os três níveis de deslocamento; os sistemas de comportamento de enxame que dão suporte às identidades; a matriz Arma × Inimigo para os arquétipos existentes; a integração dos modificadores de run já catalogados; e as restrições de reutilização/inspeção via Unity MCP.

**Fora do escopo (explicitamente excluído):** arte, modelos definitivos e animações próprias (o acabamento visual é etapa posterior); criação de novos arquétipos de inimigo além dos já existentes; balanceamento numérico final; e ferramentas de autoria de composição de encontros. Valores de dano, tempos, alcance e raio permanecem ajustáveis nos assets.

## Glossary

- **Weapon_System**: Conjunto de componentes e assets de arma existentes (`ArsenalCombat`, `ArsenalAbility`, `ArsenalCastPlan`, `BreakerGauntletCombat`, `WeaponScript`, `WeaponLoadout`) e suas extensões, responsável por executar ataques básicos e habilidades.
- **Fists** / **Manoplas**: A arma de punhos, cujas habilidades usam `BreakerGauntletAbility` e o recurso `AsuraMomentum`.
- **Spear** / **Lança**: A arma de haste, identificada como `RunWeaponFamily.Spear`, com estocadas (`ArsenalSkillKind.Thrust`) e varreduras (`ArsenalSkillKind.Sweep`).
- **Bow** / **Arco**: A arma de projéteis, identificada por `WeaponScript.FiresArrows` (`RunWeaponFamily.Bow`).
- **Reaction_Controller**: O `CombatReactionController` existente, que gerencia postura, quebra de postura e os efeitos Push / Stagger / Stun / KnockUp / Knockback.
- **Hit_Request**: A struct `HitReactionRequest` que descreve o efeito de um acerto (reação imediata, dano de postura, efeito de quebra, força).
- **Reaction_Type**: O enum `HitReactionType` (None, Push, Stagger) — a reação imediata garantida de um acerto.
- **Break_Effect**: O enum `StanceBreakEffect` (None, Stun, KnockUp, Knockback) — o controle de multidão aplicado ao quebrar a postura.
- **Hit_Strength**: O enum `HitStrength` (Light, Medium, Heavy, Breaker) — a força relativa de um acerto.
- **Enemy_Rarity** / **Enemy_Rank**: O enum `EnemyRank` (Normal, Elite, Legendary, Boss) que define quão reativo é o inimigo.
- **Stance** / **Postura**: A reserva de postura de um inimigo (`maxStance` / `currentStance` no Reaction_Controller); quando esgotada, ocorre a **Stance_Break**.
- **Stance_Break** / **Quebra de postura**: O evento em que a postura chega a zero e o Break_Effect do acerto é aplicado, seguido de uma janela de imunidade a nova quebra.
- **Vulnerability_Window** / **Janela de vulnerabilidade**: Intervalo, após uma quebra de postura, em que o inimigo está mais exposto a dano e/ou reage como um inimigo de raridade inferior.
- **Displacement** / **Deslocamento**: Movimento físico imposto ao inimigo, em três intensidades: Micro_Displacement, Push e Launch.
- **Micro_Displacement**: Deslocamento mínimo de legibilidade aplicado por ataques básicos, sem remover controle.
- **Push**: Deslocamento moderado aplicado por habilidades específicas, sem remover controle.
- **Launch**: Deslocamento forte (empurrão longo ou lançamento vertical) associado a quebra de postura e habilidades/modificadores específicos.
- **Archetype**: Um dos arquétipos de inimigo já definidos por `ArchetypeId` (Rush, Grunt, Heavy, Charger, Shooter, SpreadShooter, Sniper, Bomber, HazardCaster, Hooker, Healer, ShieldSupport, Swarm, Spawner, Fragile, Mirror).
- **Combat_Role**: A função tática de um arquétipo, conforme o enum `CombatRole`.
- **Attack_Slots** / **Tokens de ataque**: Limite de quantos inimigos corpo a corpo podem atacar o jogador simultaneamente dentro de um enxame.
- **Preferred_Distance** / **Distância preferida**: A distância de combate que cada arquétipo tenta manter, gerando formações emergentes.
- **Soft_Grouping** / **Agrupamento suave**: Assistência sutil que mantém inimigos agrupados sem criar um efeito de vácuo perceptível.
- **Sweet_Spot**: Região próxima à ponta da Lança que concede bônus (dano de postura, recurso e/ou modificadores) quando o acerto direto ocorre nela.
- **Flow** / **Fluxo**: Recurso opcional da Lança que recompensa boa execução (encadeamento de acertos no Sweet_Spot / alternância estocada↔varredura).
- **Asura**: O modo de poder das Manoplas, alimentado pelo recurso `AsuraMomentum` e liberado pela habilidade R.
- **Mark** / **Marca**: A mecânica exclusiva do Arco que designa um alvo prioritário e reforça comportamentos como homing, ricochete e prioridade de Heavy Bolt.
- **Run_Modifier**: Um `WeaponBoon` catalogado em `WeaponRunModifiers`, aplicado por run à `ArsenalCastPlan`/`GauntletSteps` sem escrever nos assets de arma.
- **Cast_Plan**: O `ArsenalCastPlan`, snapshot por conjuração que os modificadores mutam sem alterar o asset de origem.
- **Unity_MCP**: A interface de inspeção/automação do projeto Unity (power `kiro-unity-accelerator`) usada para inspecionar scripts, cenas, prefabs e assets antes da implementação.

## Requirements

### Requisito 1: Restrição de inspeção e reutilização via Unity MCP

**User Story:** Como desenvolvedor do projeto, quero que qualquer alteração de mecânica seja precedida de inspeção do projeto real, para reutilizar e estender os sistemas existentes em vez de recriá-los ou duplicá-los.

#### Acceptance Criteria

1. WHERE o Unity_MCP estiver disponível, WHEN uma alteração em scripts, componentes, prefabs ou ScriptableObjects relacionados a arma, postura, reação, movimento ou modificadores for iniciada, THE Weapon_System SHALL ser inspecionado através do Unity_MCP antes de qualquer modificação de arquivo, produzindo um registro que liste os assets e componentes inspecionados.
2. IF o Unity_MCP não estiver disponível quando uma alteração for iniciada, THEN THE Weapon_System SHALL ser inspecionado por análise de estrutura do projeto e por busca de referências em todos os arquivos `.cs`, `.unity`, `.prefab`, `.asset` e `.meta` do diretório `Assets/_Project` antes de qualquer modificação de arquivo, produzindo um registro dos arquivos inspecionados.
3. WHEN uma mecânica desta funcionalidade for implementada, THE Weapon_System SHALL estender os componentes e assets existentes (`ArsenalCombat`, `ArsenalAbility`, `BreakerGauntletAbility`, `CombatReactionController`, `WeaponRunModifiers`) sem criar novo script, componente ou ScriptableObject cuja responsabilidade seja equivalente à de um dos itens existentes listados.
4. WHERE um comportamento puder ser expresso de forma genérica e configurável por dados (parâmetros em ScriptableObject ou campos serializados), THE Weapon_System SHALL implementar esse comportamento em um único ponto de código compartilhado por todas as armas, sem repetir a mesma lógica por arma individual.
5. WHEN um asset for movido, renomeado ou reorganizado, THE Weapon_System SHALL preservar inalterados o GUID, o nome de asset referenciado e o arquivo `.meta` correspondente, mantendo o arquivo `.meta` emparelhado com seu asset.
6. IF uma alteração for concluída sem que a resolução de referências resulte em zero referências quebradas em arquivos `.unity`, `.prefab` e `.asset`, THEN THE Weapon_System SHALL rejeitar a conclusão e indicar quais referências permanecem não resolvidas.
7. IF uma inspeção via Unity_MCP ou uma validação de referências não puder ser executada, THEN THE processo de implementação SHALL relatar explicitamente qual verificação não foi realizada e o motivo da não execução, sem marcar a alteração como validada.

### Requisito 2: Sistema Universal de Reação de Inimigos

**User Story:** Como jogador, quero que dano, stagger, dano de postura, quebra de postura, empurrão, lançamento, atordoamento e knockback sejam conceitos separados e legíveis, para entender exatamente como cada golpe afeta cada inimigo.

#### Acceptance Criteria

1. THE Reaction_Controller SHALL processar cada Hit_Request em quatro canais independentes — dano à vida, Reaction_Type imediato, dano de postura e Break_Effect — de modo que a ausência ou a supressão de um canal não altere o resultado observável dos demais canais.
2. WHEN um acerto é aplicado, THE Reaction_Controller SHALL aplicar a reação imediata definida em Reaction_Type (None, Push ou Stagger), limitando qualquer deslocamento resultante a no máximo 0,5 metro e qualquer rotação resultante a no máximo 15 graus por acerto.
3. IF um acerto possui Reaction_Type igual a None ou Break_Effect igual a None, THEN THE Reaction_Controller SHALL não aplicar deslocamento, rotação nem interrupção de animação para o canal correspondente e SHALL preservar o estado atual do inimigo nesse canal.
4. WHEN um acerto possui dano de postura maior que zero, THE Reaction_Controller SHALL subtrair da reserva de postura do inimigo o valor (dano de postura × multiplicador de postura do inimigo), onde o multiplicador está no intervalo de 0,0 a 5,0, sem permitir que a reserva de postura fique abaixo de 0.
5. WHEN a reserva de postura de um inimigo chega a 0, THE Reaction_Controller SHALL aplicar o Break_Effect do acerto (None, Stun, KnockUp ou Knockback), sujeito às resistências do inimigo, no mesmo quadro em que a reserva atinge 0.
6. WHILE um inimigo estiver dentro da janela de imunidade a quebra após uma Stance_Break, THE Reaction_Controller SHALL ignorar todo dano de postura adicional, sendo a duração dessa janela um valor configurável no intervalo de 0,1 a 10,0 segundos.
7. WHEN uma Stance_Break ocorre, THE Reaction_Controller SHALL restaurar a reserva de postura ao valor máximo do inimigo e SHALL iniciar a recuperação de postura após um atraso de recuperação configurável no intervalo de 0,0 a 10,0 segundos.
8. WHERE um inimigo declara resistência total (valor 1,0) a um Break_Effect, THE Reaction_Controller SHALL suprimir esse efeito e, quando existir um degradê definido, SHALL aplicar o efeito de menor severidade ao qual o inimigo não seja totalmente imune (por exemplo, KnockUp imune recai em Stun quando o inimigo não é imune a Stun), ou None quando não houver efeito aplicável.

### Requisito 3: Reação diferenciada por raridade do inimigo

**User Story:** Como jogador, quero que inimigos comuns, intermediários, raros/elite e chefes reajam de formas distintas aos meus golpes, para que o combate contra grupos varie conforme a composição.

#### Acceptance Criteria

1. WHERE um inimigo é de raridade Normal, WHEN um acerto básico o atinge, THE Reaction_Controller SHALL aplicar Stagger e permitir quebra de postura ao atingir a reserva de postura configurada para Normal.
2. WHERE um inimigo é de raridade intermediária (Elite), THE Reaction_Controller SHALL exigir múltiplos acertos, acertos de força Heavy ou Breaker, ou habilidades de dano de postura para quebrar sua postura, aplicando as resistências de stagger e de controle configuradas (cada uma no intervalo de 0,0 a 1,0).
3. WHEN a postura de um inimigo Elite é quebrada, THE Reaction_Controller SHALL abrir uma Vulnerability_Window durante a qual o inimigo reage como um inimigo Normal, por uma duração configurável maior que 0 segundos.
4. WHERE um inimigo é de raridade Legendary, THE Reaction_Controller SHALL impedir que ele permaneça em stagger contínuo e permitir que continue atacando, reservando a maior exposição para a Vulnerability_Window após a quebra de postura.
5. WHERE um inimigo é um chefe (Boss), THE Reaction_Controller SHALL suprimir reações convencionais de acerto e permitir interação apenas por dano de postura, stagger de interrupção e janelas de vulnerabilidade após quebra de postura.
6. THE Reaction_Controller SHALL derivar os valores de postura e resistências de raridade a partir de configuração de dados (defaults por `EnemyRank` ou de um `EnemyProfile`), sem valores codificados por arma.
7. IF um inimigo não possui configuração de postura ou resistências definida para sua raridade, THEN THE Reaction_Controller SHALL aplicar os defaults por `EnemyRank` e registrar uma indicação de configuração ausente, sem interromper o processamento do acerto.

### Requisito 4: Níveis de intensidade de deslocamento

**User Story:** Como jogador, quero que o deslocamento imposto aos inimigos venha em intensidades claras, para que golpes básicos preservem a legibilidade e apenas habilidades específicas movam ou lancem inimigos de forma marcante.

#### Acceptance Criteria

1. WHEN um ataque básico acerta um inimigo, THE Weapon_System SHALL aplicar no máximo um Micro_Displacement com `PushDistance` entre 0 e 0,3 unidades a partir da posição do inimigo no momento do acerto, mantendo `ReactionType` como reação leve e `KnockUpHeight` e `KnockbackDistance` iguais a 0.
2. WHERE uma habilidade declara deslocamento moderado, THE Weapon_System SHALL aplicar um Push por meio do Reaction_Controller com `PushDistance` entre 0,31 e 2,0 unidades, `KnockUpHeight` igual a 0 e `KnockbackDistance` igual a 0, sem remover o controle do inimigo.
3. WHERE um efeito declara deslocamento forte por quebra de postura, uppercut, ou habilidade/modificador que declare explicitamente esse efeito, THE Weapon_System SHALL aplicar um Launch definindo `KnockbackDistance` entre 2,01 e 8,0 unidades para empurrão longo ou `KnockUpHeight` entre 0,5 e 4,0 unidades para lançamento vertical, com `ReactionType` indicando perda de controle.
4. THE Weapon_System SHALL expressar cada nível de deslocamento exclusivamente por meio dos campos existentes do Hit_Request (`ReactionType`, `PushDistance`, `BreakEffect`, `KnockUpHeight`, `KnockbackDistance`), sem criar um canal de deslocamento paralelo.
5. WHILE um inimigo está com o controle travado (atordoado ou no ar), THE Weapon_System SHALL suprimir Micro_Displacement e Push adicionais sobre esse inimigo, e WHEN o travamento termina, THE Weapon_System SHALL voltar a permitir Micro_Displacement e Push em acertos subsequentes.
6. IF um Hit_Request declara um nível de deslocamento cujos campos correspondentes (`PushDistance`, `KnockUpHeight` ou `KnockbackDistance`) estão ausentes, iguais a 0 ou fora das faixas definidas nos critérios 1 a 3, THEN THE Weapon_System SHALL rejeitar o deslocamento, tratar o acerto como Micro_Displacement e registrar uma indicação de erro sinalizando a declaração inválida, sem alterar demais efeitos do acerto.

### Requisito 5: Sistema de slots de ataque do enxame

**User Story:** Como jogador, quero que apenas um número limitado de inimigos corpo a corpo me ataque ao mesmo tempo, para que enxames sejam desafiadores mas justos e legíveis.

#### Acceptance Criteria

1. THE Weapon_System SHALL cooperar com um sistema de Attack_Slots que limita quantos inimigos corpo a corpo executam um ataque contra o jogador simultaneamente, sendo o limite um valor configurável maior que 0.
2. WHEN o número de atacantes corpo a corpo ativos atinge o limite de Attack_Slots, THE Weapon_System SHALL manter os inimigos excedentes em posicionamento de espera em vez de permitir que ataquem.
3. WHEN um atacante corpo a corpo tem seu ataque interrompido, é atordoado ou tem a postura quebrada, THE Weapon_System SHALL liberar o slot de Attack_Slots ocupado por esse inimigo para outro inimigo elegível.
4. WHERE o sistema de Attack_Slots já existir no projeto, THE Weapon_System SHALL estendê-lo em vez de recriá-lo; WHERE não existir, THE Weapon_System SHALL implementá-lo de forma desacoplada da lógica de cada arma.
5. IF nenhum inimigo elegível estiver disponível para ocupar um slot liberado, THEN THE Weapon_System SHALL manter o slot livre até que um inimigo elegível se torne disponível, sem forçar um atacante fora de sua Preferred_Distance.

### Requisito 6: Distância preferida e formações emergentes

**User Story:** Como jogador, quero que cada tipo de inimigo mantenha sua distância de combate característica, para que o enxame forme naturalmente camadas que cada arma explora de forma diferente.

#### Acceptance Criteria

1. THE Weapon_System SHALL respeitar uma Preferred_Distance declarada por arquétipo, com valor maior que 0, ao posicionar inimigos em relação ao jogador.
2. WHILE um inimigo não está executando um ataque, THE Weapon_System SHALL fazer o inimigo tender à sua Preferred_Distance, mantendo separação em relação aos demais inimigos que evite sobreposição de seus volumes de colisão.
3. THE Weapon_System SHALL produzir formações em camadas emergentes (corpo a corpo à frente, à distância atrás) a partir das distâncias preferidas individuais dos arquétipos presentes no enxame, sem coreografia codificada por encontro.
4. IF um arquétipo não declara Preferred_Distance, THEN THE Weapon_System SHALL aplicar a distância padrão do `CombatRole` correspondente e registrar uma indicação de configuração ausente.

### Requisito 7: Agrupamento suave e regras de interrupção

**User Story:** Como jogador, quero que as armas consigam manter inimigos agrupados de forma sutil e interrompê-los de forma previsível, para que cada arma tenha ferramentas de controle sem puxões abruptos.

#### Acceptance Criteria

1. THE Weapon_System SHALL fornecer Soft_Grouping como uma assistência que desloca inimigos em direção ao centro do grupo a uma velocidade de no máximo 1,0 m/s, aplicada somente a inimigos dentro de um raio de 3,0 m do ponto de agrupamento, sem exceder um deslocamento total de 0,5 m por aplicação.
2. WHILE a Manopla executa Flurry (W), THE Weapon_System SHALL reaplicar Soft_Grouping ao alvo atingido em intervalos de no máximo 0,25 s durante toda a duração da habilidade, mantendo o alvo dentro de um raio de 2,0 m do ponto de encadeamento.
3. WHEN a Manopla ativa Asura (R), THE Weapon_System SHALL aplicar uma atração dos inimigos dentro de um raio de 4,0 m em direção ao ponto central, a uma velocidade de no máximo 2,0 m/s, sem exceder um deslocamento total de 1,5 m por inimigo.
4. WHEN a Lança executa a varredura (Sweep, W) e atinge um inimigo, THE Weapon_System SHALL deslocar o inimigo atingido na direção da varredura a uma velocidade de no máximo 1,5 m/s, limitado a um deslocamento total de 0,75 m, sem aplicar impulso instantâneo maior que esse limite.
5. IF a arma equipada é o Arco, THEN THE Weapon_System SHALL não aplicar nenhum deslocamento de Soft_Grouping, mantendo a posição dos inimigos inalterada por efeito de agrupamento do Arco.
6. WHEN uma habilidade de interrupção acerta um inimigo cujo ataque está em janela interrompível, THE Reaction_Controller SHALL cancelar o ataque e suprimir seu beat de dano pendente no mesmo frame de simulação.
7. IF uma habilidade de interrupção acerta um inimigo cujo ataque não está em janela interrompível, THEN THE Reaction_Controller SHALL preservar o ataque em curso e não suprimir o beat de dano, indicando ao jogador uma reação de ausência de interrupção.
8. WHERE um inimigo declara resistência de postura por raridade, THE Reaction_Controller SHALL acumular o valor de stagger da interrupção e aplicar stagger ou quebra somente quando o acúmulo atingir ou exceder o limiar de resistência definido para aquela raridade, preservando o estado do inimigo abaixo desse limiar.

### Requisito 8: Identidade e loop das Manoplas (brigão)

**User Story:** Como jogador de Manoplas, quero um kit agressivo que entra no grupo, interrompe, quebra postura, mantém pressão e faz juggle acumulando Asura, para dominar o enxame de perto.

#### Acceptance Criteria

1. THE Weapon_System SHALL suportar o loop das Manoplas na sequência ENTRAR → INTERROMPER → PRESSIONAR → QUEBRAR POSTURA → JUGGLE/BURST → ASURA → FINALIZAR, permitindo executar cada etapa sem exigir a etapa seguinte.
2. WHEN o jogador ativa Q (Avanço Relâmpago), THE Weapon_System SHALL deslocar o jogador em direção ao grupo de inimigos alvo até o destino navegável mais próximo do ponto de mira, sem ultrapassar os limites da NavMesh.
3. IF o destino do Avanço Relâmpago estiver fora da área navegável, THEN THE Weapon_System SHALL encerrar o avanço no ponto navegável válido mais próximo, preservando o estado do jogador.
4. WHILE o jogador executa W (Punhos Relâmpago / Flurry), THE Weapon_System SHALL aplicar golpes repetidos no alvo travado durante toda a duração declarada da habilidade, mantendo o travamento até o término da duração.
5. WHEN o jogador usa E (Impacto de Choque / Stance Breaker), THE Weapon_System SHALL aplicar dano de postura com força Heavy ou Breaker (Hit_Strength).
6. WHEN o jogador ativa R com `AsuraMomentum` cheio (Energy = 100, IsReady verdadeiro), THE Weapon_System SHALL entrar no modo de poder Asura e zerar `AsuraMomentum` para 0.
7. IF o jogador ativa R com `AsuraMomentum` abaixo de 100, THEN THE Weapon_System SHALL negar a entrada no modo Asura e preservar o valor atual de `AsuraMomentum` sem alteração.
8. WHERE uma quebra de postura ocorre por golpe da Manopla que declara KnockUp e o inimigo tem KnockUpResistance abaixo de 1, THE Weapon_System SHALL manter o inimigo em juggle (controle bloqueado) enquanto ele estiver no ar.
9. WHEN a duração aérea do juggle termina, THE Weapon_System SHALL restaurar o controle do inimigo.
10. WHEN uma habilidade das Manoplas é registrada no `AsuraMomentum`, THE Weapon_System SHALL adicionar 25 de energia caso a categoria (stamina/shock) difira da habilidade anterior e 10 caso seja igual, limitando o total a 100.

### Requisito 9: Identidade da Lança (controladora de espaço)

**User Story:** Como jogador de Lança, quero manipular distância, linhas e área com estocadas e varreduras, mirando o Sweet Spot, para controlar o espaço do enxame e ser recompensado por boa execução.

#### Acceptance Criteria

1. THE Weapon_System SHALL fornecer duas linguagens de ataque para a Lança: estocadas (`Thrust`) focadas em linha, precisão e dano de postura, e varreduras (`Sweep`) focadas em área e limpeza de enxame.
2. WHEN um acerto direto de estocada ocorre na região do Sweet_Spot próxima à ponta, THE Weapon_System SHALL conceder bônus de dano de postura e de recurso estritamente maior do que o concedido por um acerto fora do Sweet_Spot.
3. WHEN o jogador ativa Q (estocada/avanço), THE Weapon_System SHALL executar uma estocada com avanço e suportar um contra-ataque eficaz contra o arquétipo Charger.
4. WHEN o jogador ativa W (varredura orbital), THE Weapon_System SHALL aplicar dano em área ao redor do jogador como ferramenta anti-enxame.
5. WHEN o jogador ativa E (perfuração pesada), THE Weapon_System SHALL aplicar dano de postura com força Heavy ou Breaker apto a quebrar a postura de inimigos Heavy.
6. WHEN o jogador ativa R (Onda do Dragão), THE Weapon_System SHALL projetar uma onda perfurante de controle de espaço.
7. WHERE o recurso opcional Flow estiver habilitado, THE Weapon_System SHALL recompensar boa execução (acertos no Sweet_Spot e alternância estocada↔varredura) acumulando Flow, sem alterar dano, custo ou recarga das habilidades do jogador que não acumula Flow.

### Requisito 10: Identidade do Arco (caçador tático)

**User Story:** Como jogador de Arco, quero priorizar alvos, me posicionar, controlar trajetórias e me mover enquanto disparo, usando a Marca, para caçar ameaças-chave dentro do enxame.

#### Acceptance Criteria

1. WHILE o jogador dispara com o Arco, THE Weapon_System SHALL permitir que o jogador se mova a uma velocidade reduzida menor que a velocidade de movimento plena e maior que 0, sem exigir parada total.
2. THE Weapon_System SHALL fornecer a mecânica exclusiva de Marca (Mark) que designa um alvo prioritário para o Arco.
3. WHERE um alvo está marcado, THE Weapon_System SHALL reforçar comportamentos de prioridade (homing, ricochete e prioridade de Heavy Bolt) em direção ao alvo marcado.
4. WHEN o jogador ativa Q (Rajada Rápida), THE Weapon_System SHALL executar DPS móvel com múltiplos disparos.
5. WHEN o jogador ativa W (Flecha Pesada / Tiro Concentrado), THE Weapon_System SHALL suportar um disparo carregável com dano de postura apto a quebrar inimigos Heavy.
6. WHEN o jogador ativa E (Leque Amplo), THE Weapon_System SHALL disparar um cone de emergência e conceder um recuo (backstep).
7. WHEN o jogador ativa R (Chuva de Flechas), THE Weapon_System SHALL criar controle territorial em uma área definida pela posição do cursor, respeitando o alcance existente.
8. IF o jogador ativa uma habilidade de prioridade sem alvo marcado, THEN THE Weapon_System SHALL executar a habilidade com o comportamento de alvo padrão (sem reforço de homing, ricochete ou prioridade de Heavy Bolt), preservando o estado da Marca inalterado.

### Requisito 11: Matriz Arma × Inimigo

**User Story:** Como jogador, quero que cada arma resolva cada arquétipo de inimigo de um jeito próprio, para que a escolha de arma altere significativamente a estratégia contra cada composição.

#### Acceptance Criteria

1. THE Weapon_System SHALL definir, para cada arquétipo existente (Swarm, Rush, Heavy, Shooter, Sniper, Charger, Leaper, Healer, ShieldSupport, Hooker, Mirror, Fragile e inimigos estáveis), uma resposta distinta por arma (Manoplas, Lança, Arco).
2. WHERE o arquétipo é Heavy, THE Weapon_System SHALL prover, em cada arma, uma ferramenta de dano de postura com força Heavy ou Breaker (Manopla E, Lança E, Arco W) capaz de quebrar sua postura.
3. WHERE o arquétipo é Charger, THE Weapon_System SHALL prover um contra-ataque de estocada (Lança Q) eficaz contra a investida.
4. WHERE o arquétipo é de prioridade (Healer, ShieldSupport, Spawner), THE Weapon_System SHALL permitir que o Arco alcance e priorize esse alvo por meio da Marca, e que Manoplas e Lança alcancem o alvo por avanço/estocada.
5. WHERE o arquétipo é Swarm, THE Weapon_System SHALL prover limpeza em área por varredura (Lança W), por impacto em círculo (Manopla E) e por cone/chuva (Arco E/R).
6. THE Weapon_System SHALL expressar a matriz Arma × Inimigo por dados de habilidade e reação configuráveis, e não por ramificações codificadas específicas de par arma-inimigo espalhadas pelo código.
7. IF um arquétipo existente não possui resposta definida para uma das três armas, THEN THE Weapon_System SHALL aplicar a resposta padrão da arma para o `CombatRole` correspondente e registrar uma indicação de cobertura ausente na matriz.

### Requisito 12: Integração progressiva dos modificadores de run

**User Story:** Como jogador em uma run, quero que os modificadores quebrem progressivamente as regras base de cada arma, para que a mesma arma jogue de forma diferente conforme os modificadores adquiridos.

#### Acceptance Criteria

1. THE Weapon_System SHALL aplicar cada Run_Modifier catalogado em `WeaponRunModifiers` sobre um Cast_Plan (snapshot por conjuração), sem escrever nos assets de arma ou habilidade de origem, mantendo os valores originais desses assets inalterados após a aplicação.
2. WHERE modificadores de Manopla estão ativos (LongFists, ComboNova, FlurryEcho, ShockRing, StanceCrusher, AsuraEcho, Momentum, entre os catalogados), THE Weapon_System SHALL alterar alcance, ecos, área, dano de postura e ganho de Asura de forma monotonicamente crescente com o rank do modificador, dentro do intervalo de rank 1 até o rank máximo catalogado para o modificador.
3. WHERE modificadores de Lança estão ativos (TripleMoon, Orbit, LongReach, MoonShard, SpearTip, EchoThrust, Trident, DragonWave, entre os catalogados), THE Weapon_System SHALL alterar quantidade de varreduras, raio, viagem, alcance, bônus de Sweet_Spot e comportamento de estocada de forma monotonicamente crescente com o rank do modificador, dentro do intervalo de rank 1 até o rank máximo catalogado para o modificador.
4. WHERE modificadores de Arco estão ativos (RapidBurst, TwinShot, Homing, WideVolley, GuidedRain, LongRain, HeavyBolt, Piercing, Ricochet, Sniper, entre os catalogados), THE Weapon_System SHALL alterar número de disparos, cadência, curvatura, largura do leque, pulsos e prioridade da Marca de forma monotonicamente crescente com o rank do modificador, dentro do intervalo de rank 1 até o rank máximo catalogado para o modificador.
5. WHEN um modificador transformador é aplicado, THE Weapon_System SHALL usá-lo apenas em skills criadas em runtime e de propriedade da run, mantendo os assets compartilhados imutáveis.
6. THE Weapon_System SHALL conectar os modificadores existentes às novas regras de reação, deslocamento e agrupamento, sem introduzir um catálogo paralelo de modificadores.
7. WHEN dois ou mais Run_Modifier estão ativos sobre o mesmo Cast_Plan, THE Weapon_System SHALL aplicá-los em ordem determinística e produzir o mesmo Cast_Plan resultante para o mesmo conjunto de modificadores e ranks, independentemente da ordem de aquisição na run.
8. IF um Run_Modifier possui rank fora do intervalo de 1 até o rank máximo catalogado, ou não está catalogado em `WeaponRunModifiers`, THEN THE Weapon_System SHALL ignorar esse modificador ao montar o Cast_Plan, registrar uma indicação de erro identificando o modificador e o rank recusados, e preservar o Cast_Plan e os assets de origem sem alteração por esse modificador.

### Requisito 13: Preservação do comportamento e dos assets existentes

**User Story:** Como desenvolvedor, quero que a reformulação não quebre o que já funciona, para que armas, kits e assets atuais continuem válidos após as mudanças.

#### Acceptance Criteria

1. THE Weapon_System SHALL manter exatamente três armas jogáveis, cada uma com exatamente quatro habilidades atribuídas às teclas Q, W, E e R, totalizando doze habilidades disponíveis.
2. WHEN uma habilidade é ativada, THE Weapon_System SHALL deduzir o custo de mana definido para essa habilidade e iniciar sua recarga, tornando a habilidade indisponível para nova ativação enquanto a recarga não expirar.
3. WHILE uma habilidade está em execução, THE Weapon_System SHALL bloquear a ativação de qualquer outra habilidade das teclas Q, W, E e R até a conclusão da habilidade em execução.
4. IF uma habilidade é ativada quando a mana disponível é menor que o custo dessa habilidade ou quando sua recarga ainda não expirou, THEN THE Weapon_System SHALL rejeitar a ativação, preservar a mana atual sem dedução e manter o estado inalterado.
5. WHILE a Manopla está equipada, THE Weapon_System SHALL preservar o kit e os assets das Manoplas e exibir Asura.
6. IF a Manopla não está equipada, THEN THE Weapon_System SHALL não exibir Asura.
7. WHEN um asset é movido ou reorganizado, THE Weapon_System SHALL mover o asset junto com seu arquivo `.meta` correspondente, mantendo o GUID e os caminhos especiais do Unity inalterados.
8. IF o movimento ou a reorganização de um asset exigir alteração de GUID ou de caminho especial do Unity, THEN THE Weapon_System SHALL verificar previamente todas as cenas (`.unity`), prefabs (`.prefab`) e ScriptableObjects (`.asset`) dependentes e não aplicar a alteração enquanto houver referência dependente não verificada.
9. WHERE existir teste EditMode ou PlayMode relevante à mudança, THE processo de implementação SHALL executá-lo; IF os testes não puderem ser executados via CLI, THEN THE processo SHALL validar por inspeção de estrutura de projeto, busca de referências e `git diff`, e relatar explicitamente cada teste ou validação que não foi executado.
