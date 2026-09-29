# Implementation Plan: Weapon Gameplay Swarm Rework

## Overview

A abordagem é **aditiva, incremental e orientada a dados**, exatamente como o design determina: estender a infraestrutura existente (`CombatReactionController`, `ArsenalCombat`, `BreakerGauntletCombat`, `AsuraMomentum`, `WeaponRunModifiers`, `EnemyAI`, `PriorityTargetMarker`, `EnemyProfile`/`EnemyVariant`) e **nunca recriá-la** (R1). A ordem das tarefas respeita as dependências reais:

1. **Inspeção primeiro** (R1) — registrar o que já existe antes de tocar em qualquer arquivo.
2. **Contratos compartilhados** — `DisplacementTier` e `VulnerabilityWindow`, dos quais todas as armas dependem.
3. **Identidade por arma** — configuração de dados + extensões pontuais (Sweet Spot / Flow da Lança, Mark do Arco, Manoplas).
4. **Serviços de enxame** desacoplados — slots, distância preferida, agrupamento suave.
5. **Integração de modificadores de run** — imutabilidade, ordem determinística, monotonicidade.
6. **Matriz Arma × Inimigo** (dados) e, por fim, **preservação/validação** de `.meta`/GUID e referências.

Toda lógica testável vive em **classes C# planas** extraídas dos `MonoBehaviour` (AGENTS.md), e as 44 Correctness Properties do design viram testes baseados em propriedades (PBT) com FsCheck/CsCheck (≥100 iterações cada), em EditMode, complementados por EditMode por exemplo e PlayMode de integração.

> **Ressalva de execução (R1.7 / R13.9):** o Unity_MCP instalado não expõe ferramentas e a execução de testes EditMode/PlayMode e a validação de referências em `.unity`/`.prefab`/`.asset` podem não rodar via CLI neste ambiente. Quando não for possível executar, valide por inspeção de estrutura, busca de referências e `git diff`, e **relate explicitamente** cada verificação não executada, sem marcar como validada.

## Tasks

- [x] 1. Inspeção e registro do estado atual do projeto (pré-requisito de qualquer alteração)
  - [x] 1.1 Inspecionar os sistemas de combate/arma/inimigo existentes e produzir o registro de inspeção
    - Tentar inspeção via Unity_MCP; como ele não expõe ferramentas, usar o fallback: análise de estrutura de `Assets/_Project` e busca de referências em `.cs`/`.unity`/`.prefab`/`.asset`/`.meta`
    - Registrar, em um documento de notas de inspeção, os componentes e assets inspecionados: `CombatReactionController`, `HitReactionRequest`/`HitReactionType`/`StanceBreakEffect`/`HitStrength`, `EnemyRank`/`EnemyProfile`/`EnemyVariant`, `ArsenalCombat`/`ArsenalAbility`/`ArsenalSkillKind`/`AreaHitStep`, `BreakerGauntletCombat`/`AsuraMomentum`, `WeaponRunModifiers`/`ArsenalCastPlan`/`GauntletSteps`, `WeaponScript`/`WeaponLoadout`, `EnemyAI`, `PriorityTargetMarker`, `PlayerActor.TryApplyAreaDamage`
    - Confirmar por busca de referências (`AttackSlot|PreferredDistance|SoftGroup`) que os sistemas de enxame ainda não existem
    - Relatar explicitamente que a inspeção via Unity_MCP e a validação de referências em cena/prefab/`.asset` não puderam ser executadas por essa via, sem marcá-las como validadas
    - _Requirements: 1.1, 1.2, 1.7_

- [x] 2. Contratos compartilhados de reação e deslocamento (base de todas as armas)
  - [x] 2.1 Implementar `DisplacementTier` como validador puro compartilhado
    - Criar a classe estática `DisplacementTier` em `Assets/_Project/Scripts/Characters/Combat`, classificando um `HitReactionRequest` em `Micro`/`Push`/`Launch` a partir de `PushDistance`/`KnockUpHeight`/`KnockbackDistance`, sem criar campos novos nem canal paralelo
    - Validar as faixas (Micro 0–0,3; Push 0,31–2,0; Launch Knockback 2,01–8,0 ou KnockUp 0,5–4,0) e rebaixar para `Micro` com indicação de erro em declaração ausente/zero/fora de faixa, sem alterar os demais efeitos do acerto
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6; DisplacementTier (Components and Interfaces)_

  - [x] 2.2 Escrever teste de propriedade para os limites por nível de deslocamento
    - **Property 12: Limites por nível de deslocamento**
    - **Validates: Requirements 4.1, 4.2, 4.3**

  - [x] 2.3 Escrever teste de propriedade para rebaixamento de deslocamento inválido
    - **Property 13: Declaração de deslocamento inválida rebaixa para Micro sem afetar outros efeitos**
    - **Validates: Requirements 4.6**

  - [x] 2.4 Implementar `VulnerabilityWindow` como estado interno puro do controlador
    - Criar a classe plana `VulnerabilityWindow` (`IsOpen`, `ClosesAt`, `EffectiveRank`) em `Characters/Combat`; abre na quebra de postura e fecha por tempo (duração > 0)
    - _Requirements: 3.3; VulnerabilityWindow (Components and Interfaces / Data Models)_

- [x] 3. Estender o `CombatReactionController` com os quatro canais legíveis, imunidade e vulnerabilidade
  - [x] 3.1 Preservar independência dos canais e clamps de legibilidade da reação imediata
    - Estender `ApplyReaction` mantendo os quatro canais independentes (vida, reação imediata, dano de postura, quebra); nenhum canal lê o estado do outro
    - Aplicar clamp de legibilidade na reação imediata Push/Stagger: deslocamento ≤ 0,5 m e rotação ≤ 15° por acerto; canais em None não produzem deslocamento/rotação/interrupção
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 3.2 Escrever teste de propriedade para independência dos quatro canais
    - **Property 1: Independência dos quatro canais de reação**
    - **Validates: Requirements 2.1, 2.3**

  - [x] 3.3 Escrever teste de propriedade para o limite de legibilidade da reação imediata
    - **Property 2: Limite de legibilidade da reação imediata**
    - **Validates: Requirements 2.2**

  - [x] 3.4 Preservar subtração de postura, quebra no mesmo quadro, imunidade e recuperação
    - Garantir subtração `max(0, atual − StanceDamage × multiplicador)` com multiplicador em [0,0; 5,0]; quebra resolvida no mesmo passo em que a reserva zera, aplicando o `BreakEffect` sujeito às resistências
    - Preservar a janela de imunidade pós-quebra (0,1–10,0s) ignorando dano de postura adicional; na quebra, restaurar a reserva ao máximo e adiar a recuperação (0,0–10,0s)
    - _Requirements: 2.4, 2.5, 2.6, 2.7_

  - [x] 3.5 Escrever teste de propriedade para subtração de postura limitada
    - **Property 3: Subtração de postura limitada e não-negativa**
    - **Validates: Requirements 2.4**

  - [x] 3.6 Escrever teste de propriedade para quebra no mesmo quadro em que a reserva zera
    - **Property 4: Quebra de postura no mesmo quadro em que a reserva zera**
    - **Validates: Requirements 2.5**

  - [x] 3.7 Escrever teste de propriedade para imunidade pós-quebra ignorar dano de postura
    - **Property 5: Imunidade pós-quebra ignora dano de postura adicional**
    - **Validates: Requirements 2.6**

  - [x] 3.8 Escrever teste de propriedade para restauração de postura e adiamento da recuperação
    - **Property 6: Quebra restaura postura ao máximo e adia a recuperação**
    - **Validates: Requirements 2.7**

  - [x] 3.9 Implementar o degradê determinístico de efeito por resistência (escada KnockUp→Stun)
    - Aplicar o efeito solicitado quando não houver imunidade total (1,0); caso contrário, o efeito de menor severidade não totalmente imune segundo a escada KnockUp→Stun, ou None
    - _Requirements: 2.8_

  - [x] 3.10 Escrever teste de propriedade para o degradê determinístico de efeito
    - **Property 7: Degradê determinístico de efeito por imunidade**
    - **Validates: Requirements 2.8**

  - [x] 3.11 Suprimir deslocamento adicional durante travamento de controle
    - Estender o controlador para suprimir Micro_Displacement e Push adicionais enquanto o inimigo está atordoado ou no ar, e voltar a permiti-los ao fim do travamento
    - _Requirements: 4.5_

  - [x] 3.12 Escrever teste de propriedade para supressão de deslocamento em travamento
    - **Property 14: Deslocamento suprimido durante travamento de controle**
    - **Validates: Requirements 4.5**

- [x] 4. Diferenciação de reação por raridade (dados + vulnerabilidade efetiva)
  - [x] 4.1 Integrar a `VulnerabilityWindow` ao controlador rebaixando a raridade efetiva
    - Abrir a janela em `TriggerStanceBreak`; enquanto aberta, tratar a raridade efetiva como inferior (Elite→Normal etc.) por uma duração configurável > 0
    - _Requirements: 3.3, 3.4, 3.5_

  - [x] 4.2 Escrever teste de propriedade para o rebaixamento de raridade efetiva
    - **Property 9: Janela de vulnerabilidade rebaixa a raridade efetiva (Elite→Normal)**
    - **Validates: Requirements 3.3**

  - [x] 4.2b Aplicar as regras de quebra por raridade (Normal/Elite/Legendary/Boss)
    - Normal quebra ao esgotar a reserva; Elite exige acúmulo, força Heavy/Breaker ou dano de postura, respeitando resistências em [0,0; 1,0]; Legendary não fica em stagger contínuo; Boss suprime reações convencionais, permitindo só dano de postura, stagger de interrupção e vulnerabilidade
    - _Requirements: 3.1, 3.2, 3.4, 3.5_

  - [x] 4.3 Escrever teste de propriedade para quebra de Elite por acúmulo ou força alta
    - **Property 8: Elite exige acúmulo ou força alta para quebrar**
    - **Validates: Requirements 3.2**

  - [x] 4.4 Escrever teste de propriedade para Legendary sem stagger contínuo
    - **Property 10: Legendary não permanece em stagger contínuo**
    - **Validates: Requirements 3.4**

  - [x] 4.5 Escrever teste de propriedade para Boss suprimir reações convencionais
    - **Property 11: Boss suprime reações convencionais de acerto**
    - **Validates: Requirements 3.5**

  - [x] 4.6 Criar `RankReactionDefaults` e o fallback de configuração ausente por raridade
    - Adicionar defaults por `EnemyRank` (via `ScriptableObject` ou tabela análoga a `ApplyRankDefaults`) com postura/resistências + `vulnerabilityWindowSeconds` (>0) + `breakImmunitySeconds` (0,1–10,0s); derivar valores de dados, sem números por arma
    - Quando faltar `EnemyProfile`, aplicar defaults por `EnemyRank` e registrar indicação de configuração ausente (identificador estável) sem interromper o acerto
    - _Requirements: 3.6, 3.7; RankReactionDefaults (Data Models)_

  - [x] 4.7 Escrever teste (exemplo) para o fallback de configuração ausente por raridade
    - Verificar que sem `EnemyProfile` os defaults por `EnemyRank` são aplicados e a ausência é logada, sem interromper o processamento
    - _Requirements: 3.7_

- [x] 5. Checkpoint — garantir que reação, deslocamento e raridade estão sólidos
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Serviço de slots de ataque do enxame (novo, desacoplado)
  - [x] 6.1 Implementar `AttackSlotPool` (classe plana) e o `AttackSlotConfig`
    - Criar `AttackSlotPool` em `Characters/Enemy` com limite configurável > 0 (`AttackSlotConfig.maxConcurrentMelee`), aquisição/devolução de tokens e fila; excedentes permanecem em espera; sem token elegível, o slot fica livre sem forçar inimigo para fora da distância preferida
    - _Requirements: 5.1, 5.2, 5.3, 5.5; AttackSlotConfig (Data Models)_

  - [x] 6.2 Escrever teste de propriedade para o limite de atacantes simultâneos
    - **Property 15: Limite de atacantes simultâneos do enxame**
    - **Validates: Requirements 5.1, 5.2**

  - [x] 6.3 Escrever teste de propriedade para devolução de slot restaurar capacidade
    - **Property 16: Devolução de slot restaura capacidade**
    - **Validates: Requirements 5.3**

  - [x] 6.4 Escrever teste (exemplo) para slot livre sem inimigo elegível
    - Verificar que o slot permanece livre e nenhum atacante é forçado para fora da distância preferida
    - _Requirements: 5.5_

  - [x] 6.5 Adicionar o `SwarmAttackCoordinator` (MonoBehaviour fino) e conectá-lo ao `EnemyAI`
    - Criar o coordenador por sala/encontro que hospeda o `AttackSlotPool`; `EnemyAI` pede token antes de `TelegraphedAttack` e devolve o token em interrupção/stun/quebra via os hooks de `InterruptAttack`/`CombatReactionController`, sem duplicar percepção nem locomoção
    - _Requirements: 5.2, 5.3, 5.4; SwarmAttackCoordinator (Components and Interfaces)_

- [x] 7. Distância preferida e formações emergentes (extensão do `EnemyAI`)
  - [x] 7.1 Implementar `PreferredDistanceProfile` e `PreferredDistanceLayer`
    - Criar o `PreferredDistanceProfile` (`ScriptableObject`) por `ArchetypeId` (`preferredDistance` > 0, `separationRadius`) e a camada que faz o inimigo tender à distância preferida quando não ataca, reaproveitando `EnemyAI.EngagementRange`/`StandoffDistance` e mantendo separação de colisão
    - Fallback: sem valor declarado, usar o default do `CombatRole` (sempre > 0) e registrar ausência
    - _Requirements: 6.1, 6.2, 6.4; PreferredDistanceLayer (Components and Interfaces)_

  - [x] 7.2 Escrever teste de propriedade para distância preferida positiva e alvo de posicionamento
    - **Property 17: Distância preferida positiva e alvo de posicionamento**
    - **Validates: Requirements 6.1**

  - [x] 7.3 Escrever teste (exemplo) para o fallback de distância por `CombatRole`
    - Verificar que sem `Preferred_Distance` o default do `CombatRole` (>0) é aplicado e a ausência é logada
    - _Requirements: 6.4_

- [x] 8. Agrupamento suave e regras de interrupção (novo serviço + reuso de interrupção)
  - [x] 8.1 Implementar o núcleo de cálculo do `SoftGroupingService` e o `SoftGroupingConfig`
    - Criar a classe plana de cálculo (com `MonoBehaviour` fino) que desloca inimigos rumo ao centro do grupo: ≤1,0 m/s, raio 3,0 m, ≤0,5 m por aplicação; movimento pelos meios de locomoção existentes (`NavMeshAgent`/`CharacterController`)
    - No-op quando a arma equipada é o Arco
    - _Requirements: 7.1, 7.5; SoftGroupingService / SoftGroupingConfig_

  - [x] 8.2 Escrever teste de propriedade para os limites do agrupamento suave
    - **Property 18: Limites do agrupamento suave**
    - **Validates: Requirements 7.1**

  - [x] 8.3 Escrever teste de propriedade para o Arco não aplicar agrupamento
    - **Property 22: Arco não aplica agrupamento**
    - **Validates: Requirements 7.5**

  - [x] 8.4 Estender a interrupção condicionada à janela interrompível e o acúmulo por raridade
    - Reusar `EnemyAI.InterruptAttack`/`CombatReactionController`: dentro da janela interrompível, cancelar o ataque e suprimir o beat pendente no mesmo quadro; fora dela, preservar o ataque, não suprimir o beat e sinalizar ausência de interrupção
    - Acumular stagger de interrupção por resistência de raridade, aplicando stagger/quebra só ao atingir o limiar; abaixo dele, preservar o estado
    - _Requirements: 7.6, 7.7, 7.8_

  - [x] 8.5 Escrever teste de propriedade para a interrupção condicionada à janela
    - **Property 23: Interrupção condicionada à janela interrompível**
    - **Validates: Requirements 7.6, 7.7**

  - [x] 8.6 Escrever teste de propriedade para o acúmulo de stagger por raridade
    - **Property 24: Acúmulo de stagger por resistência de raridade**
    - **Validates: Requirements 7.8**

- [x] 9. Checkpoint — garantir que os serviços de enxame estão íntegros
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Identidade das Manoplas (configuração + reuso de Avanço/Flurry/Asura)
  - [x] 10.1 Configurar o loop e os deslocamentos das Manoplas em `BreakerGauntletCombat`/`AreaHitStep`
    - Configurar `AreaHitStep`/reação para o loop ENTRAR→INTERROMPER→PRESSIONAR→QUEBRAR→JUGGLE→ASURA→FINALIZAR sem exigir a etapa seguinte; Q (Avanço Relâmpago) clampado à NavMesh (reuso de `agent.Raycast`/`NavMesh.SamplePosition`); E com força Heavy/Breaker; W (Flurry) mantendo golpes e travamento pela duração
    - Manter o juggle (controle bloqueado) enquanto no ar em quebra com KnockUp e `KnockUpResistance` < 1; restaurar controle ao fim da duração aérea
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.8, 8.9_

  - [x] 10.2 Escrever teste de propriedade para o Avanço Relâmpago ficar na NavMesh
    - **Property 25: Avanço Relâmpago fica na NavMesh**
    - **Validates: Requirements 8.2**

  - [x] 10.3 Escrever teste de propriedade para o Flurry manter golpes e travamento
    - **Property 26: Flurry mantém golpes e travamento pela duração**
    - **Validates: Requirements 8.4**

  - [x] 10.4 Escrever teste de propriedade para o KnockUp manter juggle e restaurar controle
    - **Property 29: KnockUp mantém juggle e restaura controle ao pousar**
    - **Validates: Requirements 8.8, 8.9**

  - [x] 10.5 Preservar as regras de ativação/ganho de Asura em `AsuraMomentum`/`BreakerGauntletCombat`
    - R com energia cheia entra em Asura e zera a energia; R com energia < 100 nega a entrada e preserva a energia; registro adiciona 25 quando a categoria difere da anterior e 10 quando é igual, com teto 100
    - _Requirements: 8.6, 8.7, 8.10_

  - [x] 10.6 Escrever teste de propriedade para R negar Asura quando a energia não está cheia
    - **Property 28: R nega Asura quando a energia não está cheia**
    - **Validates: Requirements 8.7**

  - [x] 10.7 Escrever teste de propriedade para o ganho de Asura por alternância de categoria
    - **Property 30: Ganho de Asura por alternância de categoria**
    - **Validates: Requirements 8.10**

- [x] 11. Identidade da Lança (Sweet Spot + Flow opcional + configuração de estocada/varredura)
  - [x] 11.1 Implementar `SweetSpot` (classe plana) e o `SweetSpotConfig`
    - Criar `SweetSpot` em `Abilities/Weapon`: dado o ponto de acerto e a geometria da estocada, decide se caiu na ponta e devolve os multiplicadores de bônus de postura/recurso (estritamente > 1 dentro); injetada no `ArsenalCombat` sem engordar o `MonoBehaviour`
    - _Requirements: 9.1, 9.2; SweetSpot / SweetSpotConfig_

  - [x] 11.2 Escrever teste de propriedade para o bônus do Sweet Spot ser estritamente maior
    - **Property 31: Bônus do Sweet Spot estritamente maior**
    - **Validates: Requirements 9.2**

  - [x] 11.3 Implementar `SpearFlow` (recurso opcional) sem interferir em quem não acumula
    - Criar `SpearFlow`: acumula por acerto no Sweet Spot e por alternância estocada↔varredura, sem alterar dano/custo/recarga das habilidades de quem não acumula Flow
    - _Requirements: 9.7; SpearFlow_

  - [x] 11.4 Escrever teste de propriedade para Flow não interferir em quem não acumula
    - **Property 32: Flow não interfere em quem não acumula**
    - **Validates: Requirements 9.7**

  - [x] 11.5 Configurar as habilidades Q/W/E/R da Lança via dados de `ArsenalAbility`/`AreaHitStep`
    - Q estocada/avanço (contra Charger); W varredura orbital anti-enxame com deslocamento limitado; E perfuração pesada com força Heavy/Breaker apta a quebrar Heavy; R Onda do Dragão
    - _Requirements: 9.1, 9.3, 9.4, 9.5, 9.6, 7.4_

  - [x] 11.6 Escrever teste de propriedade para o deslocamento limitado da varredura
    - **Property 21: Deslocamento da varredura da Lança limitado**
    - **Validates: Requirements 7.4**

- [x] 12. Identidade do Arco (Marca + configuração de disparo/alcance)
  - [x] 12.1 Implementar `WeaponMark` (classe plana) reusando `PriorityTargetMarker`
    - Criar `WeaponMark` em `Abilities/Weapon`: alvo atual + validade; reforça homing/ricochete/prioridade de Heavy Bolt quando presente e é no-op quando ausente, preservando o estado da Marca; reusa `PriorityTargetMarker` para a legibilidade
    - _Requirements: 10.2, 10.3, 10.8; WeaponMark / MarkConfig_

  - [x] 12.2 Escrever teste de propriedade para a Marca reforçar comportamentos de prioridade
    - **Property 34: Marca reforça comportamentos de prioridade**
    - **Validates: Requirements 10.3**

  - [x] 12.3 Escrever teste de propriedade para prioridade sem Marca usar alvo padrão
    - **Property 36: Habilidade de prioridade sem Marca usa alvo padrão**
    - **Validates: Requirements 10.8**

  - [x] 12.4 Configurar movimento reduzido ao disparar e as habilidades Q/W/E/R do Arco
    - Velocidade ao disparar estritamente > 0 e < velocidade plena; Q Rajada Rápida móvel; W Flecha Pesada carregável com força Heavy/Breaker apta a quebrar Heavy; E Leque Amplo + backstep; R Chuva de Flechas clampada ao alcance existente pela posição do cursor
    - _Requirements: 10.1, 10.4, 10.5, 10.6, 10.7_

  - [x] 12.5 Escrever teste de propriedade para a velocidade reduzida ao disparar o Arco
    - **Property 33: Velocidade reduzida ao disparar o Arco**
    - **Validates: Requirements 10.1**

  - [x] 12.6 Escrever teste de propriedade para a Chuva de Flechas respeitar o alcance
    - **Property 35: Chuva de Flechas respeita o alcance**
    - **Validates: Requirements 10.7**

- [x] 13. Agrupamento aplicado pelas armas (Flurry / Asura) e ferramenta anti-Heavy compartilhada
  - [x] 13.1 Conectar a reaplicação de agrupamento pelo Flurry e a atração de Asura
    - Flurry reaplica o agrupamento mantendo o alvo dentro de 2,0 m do ponto de encadeamento, respeitando o teto por aplicação; Asura atrai inimigos dentro de 4,0 m (≤1,5 m por inimigo, ≤2,0 m/s), sem atrair quem está fora do raio
    - _Requirements: 7.2, 7.3_

  - [x] 13.2 Escrever teste de propriedade para a reaplicação de agrupamento pelo Flurry
    - **Property 19: Reaplicação do agrupamento pelo Flurry respeita o teto e o raio**
    - **Validates: Requirements 7.2**

  - [x] 13.3 Escrever teste de propriedade para a atração de Asura limitada
    - **Property 20: Atração de Asura limitada**
    - **Validates: Requirements 7.3**

  - [x] 13.4 Escrever teste de propriedade para a ferramenta anti-Heavy usar força Heavy/Breaker
    - **Property 27: Ferramenta anti-Heavy usa força Heavy ou Breaker**
    - **Validates: Requirements 8.5, 9.5, 11.2**

- [x] 14. Checkpoint — garantir que as três identidades de arma estão íntegras
  - Ensure all tests pass, ask the user if questions arise.

- [x] 15. Integração progressiva dos modificadores de run (imutabilidade / ordem / monotonicidade)
  - [x] 15.1 Preservar a imutabilidade dos assets ao montar o Cast_Plan/GauntletSteps
    - Garantir que `WeaponRunModifiers.Plan`/`GauntletSteps` só mutam o snapshot por conjuração; os assets de origem (`ArsenalAbility`/`BreakerGauntletAbility`/`WeaponScript`) permanecem inalterados; conectar às novas regras de reação/deslocamento/agrupamento sem catálogo paralelo
    - _Requirements: 12.1, 12.5, 12.6_

  - [x] 15.2 Escrever teste de propriedade para a imutabilidade dos assets ao planejar
    - **Property 37: Imutabilidade dos assets de origem ao planejar**
    - **Validates: Requirements 12.1, 12.5**

  - [x] 15.3 Garantir monotonicidade por rank e ordem determinística de aplicação
    - Aplicar cada modificador catalogado de forma monotonicamente crescente com o rank (1..rank máximo) para as quantidades afetadas; iteração fixa do `Catalog` + `slot` produzindo o mesmo snapshot independente da ordem de aquisição
    - _Requirements: 12.2, 12.3, 12.4, 12.7_

  - [x] 15.4 Escrever teste de propriedade para a monotonicidade dos modificadores por rank
    - **Property 38: Monotonicidade dos modificadores por rank**
    - **Validates: Requirements 12.2, 12.3, 12.4**

  - [x] 15.5 Escrever teste de propriedade para a ordem determinística de aplicação
    - **Property 39: Ordem determinística de aplicação de modificadores**
    - **Validates: Requirements 12.7**

  - [x] 15.6 Ignorar modificador inválido/não catalogado sem efeito colateral
    - Rank fora de [1; máximo] ou não catalogado é ignorado ao montar o Cast_Plan, com log identificando modificador+rank recusados, preservando Cast_Plan e assets
    - _Requirements: 12.8_

  - [x] 15.7 Escrever teste de propriedade para modificador inválido ignorado sem efeito colateral
    - **Property 40: Modificador inválido é ignorado sem efeito colateral**
    - **Validates: Requirements 12.8**

- [x] 16. Matriz Arma × Inimigo (somente dados + fallback)
  - [x] 16.1 Criar o `WeaponEnemyMatrix` (ScriptableObject) mapeando (Arquétipo × Arma) para resposta
    - Criar o asset em `_Project/ScriptableObjects` mapeando `(ArchetypeId × RunWeaponFamily)` para uma referência de resposta (habilidade/`AreaHitStep`/config de reação), sem ramificação por par arma-inimigo no código
    - Cobrir os arquétipos existentes com respostas distintas por arma; incluir anti-Heavy (Manopla E / Lança E / Arco W), contra-Charger (Lança Q), prioridade (Marca do Arco; avanço/estocada para Manopla/Lança) e limpeza de Swarm (Lança W / Manopla E / Arco E-R)
    - Fallback: par sem resposta resolve a resposta padrão da arma para o `CombatRole` e registra cobertura ausente
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7; WeaponEnemyMatrix (Components and Interfaces)_

  - [x] 16.2 Escrever teste (exemplo) de completude e fallback da matriz
    - Verificar que todo arquétipo tem resposta por arma ou cai no fallback do `CombatRole` com log de cobertura ausente, sem ramificações por par no código
    - _Requirements: 11.1, 11.6, 11.7_

- [x] 17. Preservação do kit/HUD e integridade de referências
  - [x] 17.1 Preservar o kit de 12 habilidades, mana/recarga, exclusão mútua e exibição de Asura
    - Manter exatamente 3 armas × 4 habilidades (12); ativação deduz mana e inicia recarga; execução bloqueia outras Q/W/E/R; ativação inválida preserva mana/estado; Asura exibido só com Manoplas equipadas
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6_

  - [x] 17.2 Escrever teste de propriedade para a dedução de mana e início de recarga
    - **Property 41: Ativação deduz mana e inicia recarga**
    - **Validates: Requirements 13.2**

  - [x] 17.3 Escrever teste de propriedade para a exclusão mútua durante a execução
    - **Property 42: Exclusão mútua durante a execução**
    - **Validates: Requirements 13.3**

  - [x] 17.4 Escrever teste de propriedade para a ativação inválida preservar o estado
    - **Property 43: Ativação inválida preserva o estado**
    - **Validates: Requirements 13.4**

  - [x] 17.5 Escrever teste de propriedade para Asura exibido sse e somente se Manoplas equipadas
    - **Property 44: Asura exibido se e somente se as Manoplas estão equipadas**
    - **Validates: Requirements 13.5, 13.6**

  - [x] 17.6 Validar integridade de referências e preservação de `.meta`/GUID
    - Em qualquer movimento/reorganização de asset, mover o `.meta` junto e preservar GUID/caminhos especiais; antes de qualquer alteração de GUID/caminho, verificar dependentes em `.unity`/`.prefab`/`.asset`; rejeitar a conclusão se houver referência quebrada
    - Onde testes EditMode/PlayMode não puderem rodar via CLI, validar por inspeção de estrutura, busca de referências e `git diff`, e relatar explicitamente cada verificação não executada, sem marcar como validada
    - _Requirements: 1.5, 1.6, 13.7, 13.8, 13.9_

- [x] 18. Checkpoint final — garantir que todos os testes passam e as validações estão relatadas
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tarefas marcadas com `*` são opcionais (testes) e podem ser adiadas para um MVP mais rápido; as tarefas de implementação sem `*` devem ser feitas.
- Cada tarefa referencia critérios de requisito granulares e/ou componentes/propriedades do design para rastreabilidade.
- Cada uma das 44 Correctness Properties do design é coberta por exatamente um teste de propriedade (FsCheck/CsCheck, ≥100 iterações), com a tag "Feature: weapon-gameplay-swarm-rework, Property {N}: {texto}".
- Os checkpoints garantem validação incremental em cortes naturais (contratos → enxame → armas → modificadores/matriz/preservação).
- Toda lógica testável vive em classes C# planas extraídas dos `MonoBehaviour`, conforme AGENTS.md.
- Ressalva de execução via CLI (R1.7 / R13.9): quando testes EditMode/PlayMode ou a validação de referências não puderem rodar, validar por inspeção de estrutura + busca de referências + `git diff` e relatar explicitamente o que não foi executado.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["2.1", "2.4"] },
    { "id": 2, "tasks": ["2.2", "2.3", "3.1"] },
    { "id": 3, "tasks": ["3.2", "3.3", "3.4", "3.11"] },
    { "id": 4, "tasks": ["3.5", "3.6", "3.7", "3.8", "3.9", "3.12", "4.1", "4.2b"] },
    { "id": 5, "tasks": ["3.10", "4.2", "4.3", "4.4", "4.5", "4.6"] },
    { "id": 6, "tasks": ["4.7", "6.1", "7.1", "8.1"] },
    { "id": 7, "tasks": ["6.2", "6.3", "6.4", "6.5", "7.2", "7.3", "8.2", "8.3", "8.4"] },
    { "id": 8, "tasks": ["8.5", "8.6", "10.1", "11.1", "11.3", "11.5", "12.1", "12.4"] },
    { "id": 9, "tasks": ["10.2", "10.3", "10.4", "10.5", "11.2", "11.4", "11.6", "12.2", "12.3", "12.5", "12.6", "13.1"] },
    { "id": 10, "tasks": ["10.6", "10.7", "13.2", "13.3", "13.4", "15.1", "15.3", "15.6"] },
    { "id": 11, "tasks": ["15.2", "15.4", "15.5", "15.7", "16.1", "17.1"] },
    { "id": 12, "tasks": ["16.2", "17.2", "17.3", "17.4", "17.5", "17.6"] }
  ]
}
```
