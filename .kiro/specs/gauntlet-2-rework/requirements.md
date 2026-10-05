# Requirements Document

## Introduction

Esta especificação inicia a **Fase 3 — Manopla 2.0** do Tech-Guy (Unity 3D, C#), com base em `Docs/Tech-Guy_Gameplay_Rework.md`, especialmente **§12 e §46**, e nas definições de Techniques/Mutations de **§16, §17 e §39**. O objetivo é consolidar a identidade **PRESSÃO → POSTURA → ASURA**: entrar no grupo, manter contato, alternar golpes, quebrar postura, executar R e reiniciar a ofensiva.

O escopo inclui identidade de pressão, geração de Asura revisada, ciclo de R, avaliação da necessidade de momentum nativo, **8–12 Techniques** e **3–4 Mutations específicas da Manopla**. A arma base deve funcionar sem boons; o conjunto da Fase 3 deve funcionar sem Protocols universais (§46). Esses são dois critérios distintos de aceitação.

**Status: requisitos e design sincronizados conforme os pareceres de 03/10/2026; atualização técnica em 04/10/2026.** Tasks 1–16 concluídas com checks dirigidos via Unity MCP, registrados no audit com cobertura P1–P16. O usuário confirmou a aceitação da Fase 2. O usuário aprovou expressamente a Fase 3 em 04/10/2026; tasks 17–18 concluídas, fase encerrada e handoff em `validation.md`. Não foram fornecidas medições individuais do playtest.

### Dependências e limites de escopo

- Reutilizar a Fase 1 (`combat-foundation-rework`): ação por input, startup/active/recovery, compromisso, cancel windows, buffer, dash e identificação de execução.
- Reutilizar a Fase 2 (`posture-enemy-interaction`): canais independentes, resistência binária, categorias físicas, abertura, retomada real de Elite/Legendary/Boss e feedback agrupado. O usuário confirmou sua aceitação em 03/10/2026; tasks 17–18 e handoff consolidados, liberando a dependência da Fase 3.
- Evoluir os consumidores existentes de `impactful-weapon-boons`, `gauntlet-boon-playstyle-overhaul` e `modifier-synergies-theme17`, registrando adaptações de contrato antes de implementá-las.
- Não incluir reworks de Arco/Lança, Protocol System, estrutura/rotas da run, Repair, HP persistente, meta progressão, mapas ou procedural generation.
- A definição de Techniques/Mutations aplica-se ao conteúdo da Manopla entregue nesta fase. Reclassificar todo o catálogo e construir a infraestrutura global de ofertas/raridade são trabalhos separados; reutilizar os pontos existentes de recompensa sem antecipar a Fase 4.
- O experimento de remoção de mana universal (§8) permanece separado. Preservar custo, débito, cooldown e disponibilidade atuais até uma decisão explícita de escopo.
- Perfect dodge que mantém combo e perda de momentum ao sofrer dano são sugestões de §12.2, não entregas automáticas desta fase. Somente entram se escolhidas na decisão D2.

### Sistemas existentes reutilizados — inspeção estática

| Sistema | Fundação observada e lacuna para a Fase 3 |
| --- | --- |
| `Abilities/Weapon/AsuraMomentum.cs` | Energia por personagem, teto 100, alternância stamina/shock: 25 ao trocar categoria e 10 ao repetir/primeiro uso; R cheio consome/reset. Não confundir o nome da classe com um sistema nativo separado de pilhas de pressão. |
| `Abilities/Weapon/BreakerGauntletCombat.cs` | Executa Q/W/E/R, chama RegisterSkill ao usar Q/W/E, aplica Momentum (+10 por rank/uso) e AsuraReserve (+20 por rank após consumo), já possui scheduler, agrupamento de feedback, cancelamento e cleanup. A geração atual por uso aceita casts sem acerto; revisar esse contrato é decisão declarada. |
| `Core/AsuraSurge.cs` | Asura Fist gera +2 por rank por evento de básico confirmado. É bônus por-run; uma fonte nativa de energia não deve duplicar sua assinatura nem apagar seu benefício silenciosamente. |
| `Characters/Player/PlayerActor.cs`, `Core/HookBus.cs` | Canais de dano/básico/quebra e confirmação de dano; eventos de run não são a única fonte possível do comportamento base. A arma deve funcionar sem depender de adquirir um boon ou possuir RunBoons. |
| `Weapons/WeaponRunModifiers.cs`, `Core/RunBoons.cs` | Catálogo, ranks, ofertas por família, snapshots por-cast e teardown. Reaproveitar efeitos existentes e adaptar apenas o conteúdo selecionado. |
| `Abilities/AbilityHolder.cs`, `BreakerGauntletAbility.cs` | Disponibilidade, recursos, cooldown, slots e entrada de habilidade. R já exige Asura cheia; mudar esse gate ou o custo exige decisão explícita. |
| `Characters/Combat/CombatReactionController.cs` e helpers | Autoridade de postura/reação e causa da quebra; Techniques/Mutations continuam respeitando imunidades e retomada, sem criar controle paralelo. |
| `Characters/Combat/MomentumStacks.cs`, `Core/HungryComboTracker.cs`, `Core/ImpactGuardTracker.cs`, `Core/AdaptiveCadenceTracker.cs` | Estado e consumidores de boons já existentes; auditar sobreposição antes de adicionar momentum nativo ou novos gatilhos. |

Os caminhos de scripts da tabela são relativos a `Assets/_Project/Scripts`. As observações são de código-fonte, não nova validação Unity/runtime.

## Glossary

- **Manopla_Base**: básico, Q/W/E/R, dash e geração nativa da arma, sem boons/Protocols.
- **Pressão**: ofensiva sustentada por input e contato, alternando básico/habilidades e aproveitando aberturas; não significa auto-combate nem controle infinito.
- **Energia_Asura**: recurso por personagem que habilita R; separado de HP, mana, postura do alvo e eventuais pilhas de momentum.
- **Fonte_Nativa**: geração disponível na Manopla_Base, sem recompensa de run.
- **Acerto_Confirmado**: impacto que efetivamente causa dano de HP. Golpe no vazio, tentativa rejeitada e contato cujo dano de HP seja totalmente impedido não o satisfazem. Imunidade física ou a stagger não invalida dano de HP confirmado.
- **Quebra_Confirmada**: quebra de postura reconhecida pela autoridade da Fase 2. Sua confirmação independe de dano de HP, deslocamento físico ou interrupção do alvo. Elegibilidade para energia é uma decisão separada, sujeita à origem, limites e bloqueios da política autorada.
- **Categoria_de_Golpe**: stamina/shock usada pela alternância de Asura; diferente da Categoria_de_Reação do inimigo, do tipo de dano e do compromisso Fluid/Committed/Channel.
- **Execução**: uma ação aceita com identidade própria; pode conter vários impactos e alvos. Repetição de callback não cria nova execução.
- **Ciclo_R**: carregar Asura, aceitar e consumir R, concluir/cancelar conforme regras, voltar à pressão e carregar novamente.
- **Momentum_Nativo**: possível mecânica base adicional de pressão, sujeita à decisão D2; distinto de `WeaponBoon.Momentum` e de `MomentumStrike`.
- **Technique**: especialização da linguagem da Manopla que muda decisões/gatilhos de pressão, postura, combo ou Asura; não apenas um aumento passivo genérico de atributo.
- **Mutation**: transformação específica da Manopla que altera o verbo, trajetória, área, sequência ou modo de executar uma ação; não apenas multiplicador numérico.
- **Snapshot_de_Cast**: cópia de dados de execução/boons/mutation, isolada dos assets fonte e estável durante aquela execução.
- **Protocol_Universal**: interação sistêmica entre efeitos prevista para a Fase 5, que não pode ser requisito para a Manopla funcionar nesta fase.

## Requirements

### Requirement 1: Identidade de pressão e kit base

**User Story:** Como jogador de Manopla, quero manter contato e combinar golpes com controle do personagem, para que pressionar seja a forma natural de jogar mesmo sem boons.

#### Acceptance Criteria

1. THE Sistema_da_Manopla SHALL permitir executar o ciclo entrar → pressionar → quebrar postura → gerar Asura → R → reiniciar pressão sem Techniques, Mutations ou Protocols.
2. WHEN o jogador encerra o input ofensivo, THE Sistema_da_Manopla SHALL preservar o encerramento da ordem de ataque da Fase 1, sem iniciar ataques persistentes por conta própria.
3. THE Sistema_da_Manopla SHALL manter papéis reconhecíveis para básico, Q/entrada, W/sequência de pressão, E/quebra e R/burst Asura; alterações desses papéis por Mutation SHALL ser explicitadas em sua descrição.
4. WHEN uma ação encadeia outra, THE Sistema_da_Manopla SHALL usar as fases, autorizações de cancelamento, buffer e disponibilidade existentes, sem criar um segundo controlador de compromisso.
5. WHEN um golpe inclui avanço para manter contato, THE Sistema_da_Manopla SHALL limitar o avanço por autoria e terreno, sem atravessar obstáculos, perseguir indefinidamente ou conceder dano por proximidade sem impacto.
6. WHILE um inimigo está sob pressão, THE Sistema_da_Manopla SHALL respeitar a política de postura/reação e a oportunidade real de ação protegida pela Fase 2.

### Requirement 2: Geração nativa de Asura e alternância

**User Story:** Como jogador, quero que ofensiva variada e quebras carreguem Asura, para que o recurso recompense o combate que a Manopla pretende incentivar.

*Proposta sujeita a D1:* geração base ligada a contribuição confirmada, com básico nativo, bônus de alternância por execução e bônus de quebra. O documento fonte não fixa taxas, granularidade multi-alvo nem geração no vazio; o design deve fechar esses pontos antes da implementação.

#### Acceptance Criteria

1. THE Sistema_da_Manopla SHALL disponibilizar geração nativa de Energia_Asura sem exigir o boon AsuraFist ou outro componente de recompensa.
2. WHEN uma execução elegível de Q/W/E contribui para o combate, THE Sistema_da_Manopla SHALL distinguir repetição de categoria de alternância stamina/shock e recompensar alternância mais que repetição, conforme parâmetros explicitamente autorados.
3. WHEN uma quebra de postura é confirmada e sua origem atende à política de geração autorada da Manopla, THE Sistema_da_Manopla SHALL conceder a contribuição nativa no máximo uma vez por quebra causal, respeitando os limites por raiz e o bloqueio durante R definidos em D1. Imunidade à reação física SHALL não invalidar sua elegibilidade. Quebras além do limite ou de origem inelegível SHALL permanecer mecanicamente válidas e preservar reação, abertura, feedback e eventos próprios.
4. THE Sistema_da_Manopla SHALL declarar separadamente as contribuições de básico, habilidade, alternância, quebra e bônus de boon, incluindo unidade por impacto/alvo/execução e limites de lote, antes de implementá-las.
5. WHEN callbacks duplicados, reentrantes ou efeitos secundários representam o mesmo gatilho, THE Sistema_da_Manopla SHALL evitar conceder novamente a mesma contribuição.
6. IF uma ação é rejeitada, um golpe não produz a confirmação exigida pela fonte ou uma execução é cancelada antes do gatilho elegível, THEN THE Sistema_da_Manopla SHALL não conceder energia desse gatilho. Ausência de dano de HP SHALL bloquear somente fontes que exigem Acerto_Confirmado, sem bloquear uma Quebra_Confirmada elegível. Imunidade física ou ausência de interrupção SHALL não invalidar essas confirmações. Cancelamento posterior SHALL não retirar ganhos já confirmados.
7. THE Sistema_da_Manopla SHALL manter Energia_Asura finita dentro de [0, capacidade], rejeitar entradas inválidas e impedir overflow; capacidade e taxas SHALL ser dados validados, sem mutação dos assets durante a run.
8. WHEN fontes nativas e Techniques contribuem no mesmo lote, THE Sistema_da_Manopla SHALL compor apenas os bônus explicitamente previstos e aplicar seus limites, sem dupla assinatura do mesmo evento.

### Requirement 3: Ciclo de R e recuperação da pressão

**User Story:** Como jogador, quero perceber quando R está pronto e voltar ao combate após usá-lo, para que Asura seja um pico do ciclo da arma e não um estado permanente.

#### Acceptance Criteria

1. WHEN a Energia_Asura não satisfaz o gate de R, THE Sistema_da_Manopla SHALL rejeitar a ativação preservando energia, cooldown, mana e ação em curso conforme a disponibilidade da Fase 1.
2. WHEN R é aceito, THE Sistema_da_Manopla SHALL consumir a energia uma única vez e aplicar separadamente eventual reserva autorada de Technique, sem conceder a geração comum de Q/W/E pelo simples início de R.
3. WHILE R executa, THE Sistema_da_Manopla SHALL preservar compromisso, fases, janelas de cancelamento e limites de impactos; uma Mutation de R SHALL declarar explicitamente seu contrato substituto.
4. WHEN R conclui, é cancelado, o jogador morre ou a arma é desequipada, THE Sistema_da_Manopla SHALL liberar somente seus próprios modificadores de movimento, dano recebido, animação, agrupamento e controle.
5. WHEN R termina e a disponibilidade permite nova ação, THE Sistema_da_Manopla SHALL permitir reiniciar a pressão pelo input/buffer vigente, sem executar automaticamente outro R.
6. IF energia/reserva atinge novamente a capacidade, THEN THE Sistema_da_Manopla SHALL continuar exigindo uma nova ativação válida e respeitar cooldown/cancelamento, sem criar auto-loop.
7. THE Sistema_da_Manopla SHALL declarar a política de cancelamento após consumo e eventual reembolso antes de implementar mudanças; não SHALL alterar silenciosamente o consumo atual no início da ação aceita.

### Requirement 4: Decisão explícita sobre momentum nativo

**User Story:** Como designer, quero acrescentar momentum somente se ele resolver uma necessidade do combate, para que a arma tenha profundidade sem recursos redundantes.

#### Acceptance Criteria

1. THE Especificação_da_Manopla SHALL registrar uma decisão verificável entre entregar pressão sem medidor adicional ou adicionar Momentum_Nativo, com justificativa e cenário de comparação.
2. IF Momentum_Nativo for selecionado, THEN THE Sistema_da_Manopla SHALL definir ganho, manutenção, limite, perda, relógio, reset e benefício próprios, distinguindo-os de Energia_Asura, Momentum e MomentumStrike.
3. IF dano recebido altera Momentum_Nativo, THEN THE Sistema_da_Manopla SHALL distinguir dano efetivo de imunidade/absorção total e evitar perdas duplicadas por callbacks do mesmo impacto.
4. IF perfect dodge mantém/completa combo, THEN THE Sistema_da_Manopla SHALL definir a confirmação de esquiva e a transição autorizada, sem introduzir iframe/cancelamento universal ou ataque sem input.
5. IF Momentum_Nativo não for selecionado, THEN THE Sistema_da_Manopla SHALL não exigir um novo medidor ou mecânica equivalente para concluir a fase; a decisão negativa justificada satisfaz este requisito.

### Requirement 5: Techniques da Manopla

**User Story:** Como jogador, quero especializações que mudem como mantenho pressão, para que escolhas de run produzam estilos distintos usando o kit da Manopla.

#### Acceptance Criteria

1. THE Conteúdo_da_Manopla SHALL entregar entre **8 e 12 Techniques distintas**, acessíveis pelo fluxo existente de recompensa/equipamento da família Gauntlet.
2. THE Conteúdo_da_Manopla SHALL revisar e reutilizar candidatos existentes antes de criar efeitos equivalentes; conteúdo reaproveitado pode contar para o total somente após cumprir os critérios desta fase.
3. THE Conteúdo_da_Manopla SHALL definir para cada Technique identidade estável, gatilho, efeito comportamental, ranks/limite, fontes de energia afetadas, interação com R, feedback e cleanup.
4. THE Conteúdo_da_Manopla SHALL cobrir estilos de pressão/combo, postura/quebra e Asura, sem exigir Protocols universais para ativar esses estilos.
5. WHEN uma Technique é escolhida repetidamente, THE Sistema_de_Recompensas SHALL respeitar seus limites e reconfigurar seus consumidores sem duplicar handlers, impactos ou custos.
6. WHEN o jogador possui Techniques em ordens diferentes com o mesmo conjunto final de ranks, THE Sistema_da_Manopla SHALL produzir o mesmo comportamento definido, exceto se uma dependência de ordem for explicitamente aprovada e apresentada.
7. IF uma revisão promove o efeito de um boon para comportamento nativo ou reclassifica seu efeito como Mutation, THEN THE Conteúdo_da_Manopla SHALL registrar compensação/substituição e compatibilidade de dados antigos, sem contar o mesmo efeito em duas entradas para atingir a quantidade.
8. THE Conteúdo_da_Manopla SHALL preservar a aplicação dos efeitos em estado runtime/snapshots, sem escrever em armas, habilidades ou perfis fonte.

### Requirement 6: Mutations específicas da Manopla

**User Story:** Como jogador, quero transformações que mudem o verbo de uma ação da Manopla, para que uma Mutation seja uma decisão de estilo reconhecível.

#### Acceptance Criteria

1. THE Conteúdo_da_Manopla SHALL entregar entre **3 e 4 Mutations distintas**, específicas da família Gauntlet e acessíveis por um caminho existente de aquisição/seleção verificável.
2. THE Conteúdo_da_Manopla SHALL definir para cada Mutation a ação afetada, comportamento antes/depois, input, fases, compromisso, cancelamento, custo, cooldown, geração de Asura, postura/reação, feedback e regras de combinação.
3. THE Conteúdo_da_Manopla SHALL exigir alteração de verbo/comportamento observável; um aumento de dano, raio ou velocidade isolado não SHALL contar como Mutation.
4. WHEN Mutations disputam o mesmo comportamento, THE Sistema_da_Manopla SHALL aplicar uma política declarada de exclusividade/substituição ou composição determinística, apresentada antes da escolha.
5. WHEN uma Mutation é aplicada, THE Sistema_da_Manopla SHALL usar snapshots/definições runtime e manter estável a execução já iniciada, sem alterar seus impactos ou cobrar novamente recursos.
6. WHILE uma Mutation executa, THE Sistema_da_Manopla SHALL respeitar imunidades de Boss, limites físicos, agrupamento de feedback e causalidade de quebra da Fase 2.
7. WHEN a arma é trocada ou a run termina, THE Sistema_da_Manopla SHALL remover suas transformações e subscriptions próprias, sem deixar alterações nos assets ou na arma seguinte.
8. THE Conteúdo_da_Manopla SHALL integrar as Mutations ao fluxo disponível sem exigir a criação de Mutation rooms, raridade global ou novas rotas nesta fase.

### Requirement 7: Feedback, autoria e lifecycle

**User Story:** Como jogador, quero entender o recurso e as transformações enquanto luto, para que minhas decisões não dependam de ler números durante a horda.

#### Acceptance Criteria

1. WHILE a Manopla está equipada, THE HUD SHALL comunicar Energia_Asura atual, capacidade e disponibilidade de R, distinguindo energia suficiente de indisponibilidade por cooldown/recurso/estado.
2. WHEN a Manopla é desequipada, THE HUD SHALL não apresentar Asura como recurso ativo da outra arma.
3. WHEN alternância, quebra ou Technique concede energia, THE Feedback_da_Manopla SHALL comunicar o gatilho sem publicar novos eventos mecânicos ou reaplicar dano/postura/hit-stop.
4. WHEN uma Mutation é selecionada, THE Apresentação_de_Recompensa SHALL mostrar ação afetada, mudança comportamental e incompatibilidades; a ação transformada SHALL ser reconhecível em combate.
5. THE Autoria_da_Manopla SHALL validar números brutos, NaN/infinito, limites de energia, tempos, ranks e incompatibilidades, distinguindo ausência de override de zero intencional.
6. WHILE o jogo está pausado ou em hit-stop, THE Sistema_da_Manopla SHALL usar o relógio mecânico definido para combo/decadência/execução e o relógio cosmético definido para apresentação, sem conceder energia por avanço de tempo congelado.
7. WHEN ocorre morte, desequipamento, fim de run, disable ou reuso, THE Sistema_da_Manopla SHALL aplicar a política declarada de reset/persistência de energia, categoria anterior e momentum, sem subscriptions/modificadores residuais.
8. THE Autoria_da_Manopla SHALL garantir que os valores autorados persistam após save/reload, preservando referências e GUIDs existentes em qualquer migração necessária.

### Requirement 8: Compatibilidade e integração das fases

**User Story:** Como desenvolvedor, quero evoluir a Manopla sem quebrar os sistemas compartilhados, para que a Fase 3 se apoie no trabalho já validado.

#### Acceptance Criteria

1. THE Implementação_da_Manopla SHALL preservar assinaturas, payloads, contagem e ordem observável dos eventos consumidos por sistemas existentes, ou registrar antecipadamente cada adaptação deliberada.
2. THE Implementação_da_Manopla SHALL manter Arco/Lança funcionais e preservar seu dano, custo, projéteis, timing, habilidades e dash fora das mudanças explicitamente aprovadas.
3. THE Implementação_da_Manopla SHALL manter um caminho único de decisão de postura/reação e um escopo de feedback por lote, sem controle paralelo de inimigos ou hit-stop por alvo.
4. WHEN a revisão de Asura muda geração por uso para geração por contribuição, THE Implementação_da_Manopla SHALL adaptar os testes e descrições do contrato substituído, preservando testes corretos dos contratos não alterados.
5. THE Implementação_da_Manopla SHALL separar ganho nativo de bônus por-run, preservar o reset de boons e habilitar o kit base sem adquirir boons ou possuir RunBoons. A implementação MAY reutilizar HookBus ou outros eventos existentes se os sinais necessários forem emitidos sem boons e tiverem origem, granularidade, ordem e ownership adequados. IF esses sinais forem insuficientes, THEN a implementação SHALL adaptá-los de forma compatível, sem duplicar eventos ou subscriptions.
6. THE Implementação_da_Manopla SHALL preservar dados de enum, referências e carregamentos runtime; retirar/reclassificar uma oferta não SHALL invalidar silenciosamente loadouts existentes.

### Requirement 9: Validação técnica e aceitação de gameplay

**User Story:** Como responsável pelo jogo, quero evidência de comportamento e de diversão, para que a conclusão da fase não dependa apenas de um checklist implementado.

#### Acceptance Criteria

1. THE Validação_da_Manopla SHALL executar apenas os testes relevantes para cada alteração e suas dependências diretas; uma suíte mais ampla SHALL ser justificada por alcance compartilhado, falha observada ou risco concreto de integração.
2. THE Validação_da_Manopla SHALL usar Unity MCP para compilação, Console, import, dados serializados, testes e runtime pertinentes, registrando execução real e limitações sem limpar o Console para ocultar problemas.
3. THE Validação_da_Manopla SHALL cobrir básico/skill/alternância/quebra, multi-alvo e callbacks reentrantes, R cheio/incompleto, consumo e reserva, cancelamento antes/depois do gatilho, pausa/hit-stop e cleanup.
4. THE Validação_da_Manopla SHALL cobrir Techniques/Mutations selecionadas, exclusividades, snapshots, limites de proc, causalidade, teardown e regressões diretamente afetadas de boons/Arco/Lança.
5. THE Aceitação_de_Gameplay SHALL registrar um playtest humano de aproximadamente dez minutos com Manopla_Base sem boons, avaliando pressão, contato, utilidade do básico, papéis de Q/W/E/R e ciclo de Asura (§43/§57).
6. THE Aceitação_de_Gameplay SHALL registrar pelo menos dois conjuntos distintos de Techniques/Mutations sem Protocols universais, avaliando decisões diferentes e reconhecimento das transformações, incluindo alvo comum, Heavy, Elite e Boss.
7. THE Handoff_da_Fase SHALL separar compilação, testes automatizados, runtime e aceitação humana; pendências da Fase 2 e da Fase 3 SHALL permanecer explícitas até serem resolvidas.

## Registro de decisões para o design

As linhas abaixo preservam as questões levantadas nos requisitos. Suas resoluções propostas constam em `design.md`, na seção Decisões e handoff; não representam pendências deixadas sem definição no design.

| ID | Decisão | Proposta/base para revisão |
| --- | --- | --- |
| D1 | Fontes e granularidade de Asura | Proposta: básicos confirmados geram energia nativa; alternância conta uma vez por execução Q/W/E elegível; quebra causal concede bônus. Definir taxas, limites por lote/multi-alvo, efeito de casts vazios e geração durante R. Atual: Q/W/E geram no uso; básico depende de AsuraFist. Essa mudança precisa ser aprovada como adaptação de contrato. |
| D2 | Momentum nativo necessário? | Começar avaliando o loop pressão/postura/Asura sem novo medidor. Selecionar momentum somente com benefício demonstrável e sem duplicar MomentumStrike; perfect dodge/penalidade ao sofrer dano não são requisitos fechados. |
| D3 | Catálogo de 8–12 Techniques | Revisar candidatos de §12.3: Momentum, AsuraReserve, AsuraFist, GuardBreaker, HungryCombo, MomentumStrike, FlurryEcho, ShockRing e AsuraEcho. Lista de candidatos não é seleção final nem compromisso de manter todos os efeitos atuais. Registrar keep/rework/merge/reclassify e benefício após revisão do kit base. |
| D4 | Catálogo de 3–4 Mutations | Exemplos da fonte: Q atravessa/termina em uppercut; W deixa zona; E carregável; R com sequência manual. São referências, não quatro Mutations aprovadas. Fechar conteúdo, exclusividades, geração de energia e seleção no fluxo existente. |
| D5 | Parâmetros e consumo de R | Preservar provisoriamente capacidade 100, gate cheio, consumo no início e ausência de reembolso automático; definir rates e reserva após decidir D1. Não presumir que valores atuais foram aprovados em playtest. |
| D6 | Mana e persistência | Mana permanece até autorização específica para o experimento §8. Preservar resets atuais de troca de arma/fim de run como baseline; fechar persistência entre salas, morte e reuso sem alterar estrutura da run. |
| D7 | Reconciliação entre specs | Confirmar contratos substituídos nas specs anteriores de geração/boons antes de atualizar seu design/tasks/testes; respeitar o gate humano aberto da Fase 2. |

## Requirements Coverage Map

| Fonte | Requisitos |
| --- | --- |
| §12 — pressão e loop da arma | R1, R2, R3, R4 |
| §46 — geração revisada, R, momentum condicional | R2, R3, R4; decisões D1/D2/D5 |
| §46 — 8–12 Techniques e 3–4 Mutations | R5, R6; decisões D3/D4 |
| §16/§17/§39 — transformação e especialização | R5, R6, R7 |
| §43/§46/§57 — arma base divertida e independente de Protocols | R1, R9 |
| Fundação das Fases 1–2 e AGENTS.md | R7, R8, R9 |

**Verificação documental:** requisitos/design sincronizados com os dois pareceres e tasks incrementais elaboradas por inspeção estática. Nenhum script ou asset foi alterado nesta revisão. Não foram executados testes, compilação ou playtest; esta entrega não faz novas afirmações de validação Unity.

