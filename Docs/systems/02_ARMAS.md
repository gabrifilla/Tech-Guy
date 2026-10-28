# Identidade das armas — três estilos, não três ranges

**Direção de design vigente:** preservar identidades distintas e kit base divertido sem boons. **Implementação:** inspeção estática em 18/10/2026, `gameplayOverhaul`, HEAD `3a3f0ee` com alterações locais; não houve teste Unity nem aceite de gameplay nesta consolidação. As seções de runtime são `IMPLEMENTADO_NAO_VALIDADO` para esta revisão. Resultados técnicos históricos não aprovam feel; consultar a [matriz](../validation/01_MATRIZ_DE_VALIDACAO.md).

| | Manopla | Lança | Arco |
|---|---|---|---|
| Fantasia | boxeador que se aproxima e domina | combatente que desenha espaço | caçador que escolhe alvo/distância |
| Decisão central | quando entrar, pressionar e quebrar postura | onde alinhar e quando reposicionar | o que marcar, quando carregar e manter distância |
| Controle de swarm | stagger, pressão, juggle elegível | sweep, linha, zona, sweet spot | marca, projétil, armadilha, kiting |
| Risco | proximidade e janelas de compromisso | precisão do espaço e ângulo | charge/exposição e ameaça aproximando |
| Ganho perceptível | combo, força do hit e Asura | alcance útil e geometria | prioridade de alvo e controle espacial |

## Autoria e custos atuais

[WeaponScript](../../Assets/_Project/Scripts/Weapons/WeaponScript.cs) mantém arma, básico e quatro referências de habilidade. Os assets [Gauntlet](../../Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset), [Bow](../../Assets/_Project/Resources/Weapons/Ranged/Bow_arrow/Bow.asset) e [Spear](../../Assets/_Project/Resources/Weapons/Melee/Spear/Spear.asset) fixam a ordem Q/W/E/R. [WeaponLoadout](../../Assets/_Project/Scripts/Weapons/WeaponLoadout.cs) guarda seleção/desbloqueios; persistência e condições de acesso ficam em [run](05_RUN_E_PROGRESSAO.md).

Valores lidos dos assets de habilidade em 18/10/2026, antes de modificadores de run. A coluna de recarga é `cooldownTime` nominal, sem somar duração ativa ou aplicar redução de recarga.

| Arma / fonte | Q: mana / recarga (s) | W: mana / recarga (s) | E: mana / recarga (s) | R: mana / recarga (s) |
|---|---|---|---|---|
| Manopla, autoria bruta | 80 / 3 | 110 / 5 | 140 / 6 | 210 / 16 |
| Manopla, custo efetivo base com config atual | 56 / 3 | 77 / 5 | 98 / 6 | 147 / 16 |
| Arco | 80 / 4 | 150 / 8 | 180 / 10 | 240 / 14 |
| Lança | 80 / 4 | 120 / 7 | 180 / 10 | 240 / 14 |

Fontes: [BreakerAdvance](../../Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerAdvance.asset), [BreakerFlurry](../../Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerFlurry.asset), [BreakerShock](../../Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerShock.asset), [BreakerAsura](../../Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerAsura.asset) e [assets Arsenal](../../Assets/_Project/ScriptableObjects/Arsenal/). O [CombatBalanceConfig](../../Assets/_Project/Resources/CombatBalanceConfig.asset) aplica `skillManaCostMultiplier: 0.7` exclusivamente através de [BreakerGauntletAbility.ManaCost](../../Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletAbility.cs). Sem config, o multiplicador cai para 1. Arco/lança não recebem essa redução. HUD, preflight e débito leem o custo efetivo; não copiar a tabela bruta para UI. Esses números descrevem a configuração, não comprovam aprovação de balanceamento.

## Manopla

**Direção:** Breaker (Lost Ark) e feel de boxe/Steve Fox (Tekken) para pressão, evasão e execução, sem copiar movimentos, assets ou frame data. Fluxo: aproximar → conectar hits → pressionar stance → gerar oportunidade → finalizar com Asura/skill adequada. Básicos podem causar micro-stagger elegível; launch depende de skill/quebra/perfil autorizado.

**Implementado:** [BreakerGauntletCombat](../../Assets/_Project/Scripts/Abilities/Weapon/BreakerGauntletCombat.cs) e [GauntletExecutionSnapshot](../../Assets/_Project/Scripts/Abilities/Weapon/GauntletExecutionSnapshot.cs) fixam dados por execução e emitem impactos por scheduler/ledger. Q (`Avanco`) avança com limites de navegação/sólidos; W (`Punhos`) executa sequência; E (`Choque`) aplica impactos e postura; R (`Asura`) exige energia cheia e mana. As categorias Impulso/Choque vêm de `ShockSkill` nos assets.

O medidor único [AsuraMomentum](../../Assets/_Project/Scripts/Abilities/Weapon/AsuraMomentum.cs) tem capacidade 100. O runtime atual passa por [GauntletEnergyPolicy](../../Assets/_Project/Scripts/Abilities/Weapon/GauntletEnergyPolicy.cs), não deve ser descrito apenas pelo helper antigo `RegisterSkill`:

- Ganho depende de dano de HP/quebra confirmados em contexto vivo e elegível, com deduplicação por raiz/execução.
- A autoria padrão [GauntletEnergyAuthoring](../../Assets/_Project/Scripts/Abilities/Weapon/GauntletAuthoring.cs) concede 3 por básico confirmado, 10 por skill Q/W/E confirmada, mais 15 quando alterna categoria e 20 por quebra, até duas quebras por raiz. Técnicas podem adicionar parcelas específicas. Esses são defaults; overrides devem ser lidos antes de alterar tuning.
- R consome medidor cheio uma vez. Durante Asura, confirmações não geram energia; reserva de técnica é parcela própria na entrada. Morte, disable e troca de arma invalidam contextos/resetam energia pelo executor.
- R nativo aplica multiplicador de dano recebido `0.15` durante a execução, acompanha o cursor e respeita as janelas de cancelamento. Não prometer que uma esquiva sempre dispara o finalizador: `FinishForDodge` possui caminhos condicionais e o contexto atual pode apenas encerrar a execução.

**Transformações implementadas, condicionais à run:** mutações são descritas no [authoring](../../Assets/_Project/Scripts/Abilities/Weapon/GauntletAuthoring.cs), com compatibilidade/oferta em [boons](04_BOONS.md). Asura Cadenciada muda R para três pressionamentos, custo único; segurar R não continua. Durante a espera é possível mover sem proteção; F antecipa o golpe final. Após o primeiro golpe omite a parcela intermediária (75% do total nativo); após o segundo preserva o total. Isso não é o comportamento do R base sem essa mutação.

## Lança

**Direção:** Glaivier/arma longa de Lost Ark; alinhamento, ponta/sweet spot, varreduras, linhas e reposicionamento devem produzir decisões diferentes da manopla. Órbita/retorno dependem do conteúdo e contrato real, não são uma ação universal de toda lança.

**Implementado:** [ArsenalCombat](../../Assets/_Project/Scripts/Abilities/Weapon/ArsenalCombat.cs) aplica identidades de [SpearLoopSteps](../../Assets/_Project/Scripts/Abilities/Weapon/SpearLoopSteps.cs): Q estocada/contra-ataque, W varredura, E golpe de postura Heavy/Breaker e R onda/pressão. Os casts nativos param a navegação e restauram estado ao terminar; o deslocamento controlado de inimigos usa `SoftGroupingService`, não um impulso global em todo alvo.

[SpearBoonCombat](../../Assets/_Project/Scripts/Abilities/Weapon/SpearBoonCombat.cs) atende mutações de Q linha persistente, E estacas e R órbita. Projeções têm ciclo/identidade próprios; F solicita o retorno quando uma órbita está ativa e elegível. Esse executor reutiliza ativação/custos do holder; presença de projeção persistente não significa uma segunda cobrança.

## Arco

**Direção:** marcação, prioridade de alvo, posicionamento/disparo/fuga e controle de distância. Carga, projéteis, minas e LOS precisam de targeting e cancelamento claros, preservando utilidade do kit sem boons.

**Implementado:** [BowLoopSteps](../../Assets/_Project/Scripts/Abilities/Weapon/BowLoopSteps.cs) e `ArsenalCombat` distinguem Q disparos, W tiro pesado com contribuição de postura, E leque com recuo limitado pelo NavMesh e R chuva em ponto do cursor limitado ao alcance. [BowFireMovement](../../Assets/_Project/Scripts/Abilities/Weapon/BowFireMovement.cs) reduz velocidade durante casts nativos em vez de prender o jogador; isso não libera automaticamente outros ataques fora de suas janelas. W base usa windup autorado, sem carga variável por segurar a tecla.

[BowBoonCombat](../../Assets/_Project/Scripts/Abilities/Weapon/BowBoonCombat.cs) atende transformações Q vínculo/marca, E jardim de minas e R flecha carregável. Marca verifica alvo vivo/geração/encontro, e o tiro R transformado pode consumir marca; seu canal bloqueia movimento. Não confundir essa carga com W nativo, nem com o modificador legado de básico `ChargedShot`. A habilitação/oferta de cada conteúdo pertence a [boons](04_BOONS.md).

## Contratos de compatibilidade

- Q/W/E/R e F são bindings reais em [GamePreferences](../../Assets/_Project/Scripts/Core/GamePreferences.cs), não exemplos. Respeitar remapeamento e contexto; não rebindar globalmente por causa de uma arma.
- A decisão de cancelamento e o buffer pertencem ao [contrato de combate](01_COMBATE.md); não voltar à regra documental de bloqueio total durante toda skill.
- Boons intensificam/subvertem a identidade conscientemente. Não tornar uma função central dependente de sinergia rara sem decisão de produto.
- Após mudança compartilhada, validar as três armas, incluindo projéteis já emitidos, morte, troca de arma, pausa e fim de encontro.

## Conflitos e lacunas conhecidos

| Evidência da implementação | Direção que permanece vigente / pendência |
|---|---|
| `ArsenalCombat.BuildSpearReaction` avalia `SweetSpot` com uma ponta sintética (`center + direction * Range`) antes de resolver os alvos; não usa o ponto real de contato de cada inimigo. | A direção exige recompensa por espaçamento/alinhamento reais. O helper geométrico existe, mas esse callsite não comprova distinção entre acerto perto do cabo e acerto na ponta. Requer decisão/validação e correção autorizada em tarefa de gameplay. |
| [SpearFlow](../../Assets/_Project/Scripts/Abilities/Weapon/SpearFlow.cs) existe como modelo opcional; não foi localizado consumidor de runtime que o instancie ou habilite. | Não anunciar Flow como recurso jogável. Preservar a decisão de recurso opcional, sem mudar dano/custo/cooldown como efeito colateral. |
| [WeaponMark](../../Assets/_Project/Scripts/Abilities/Weapon/WeaponMark.cs) existe como helper; marcação concreta encontrada está ligada a boons (`BowBoonCombat`/marcas de chuva). W base é temporizado, enquanto descrições de identidade mencionam carga. | Decidir se marca/carga fazem parte do kit sem boons; não declarar identidade base concluída nem instalar esses comportamentos automaticamente. |
| O caminho moderno de `FinishForDodge` pode cancelar sem pagar finalizador; F atua na Cadenciada/órbita. | Não preservar como fato a promessa antiga de finalizador grátis ao esquivar. Se esse cash-out for decisão de produto desejada, reconciliar explicitamente antes de alterar código. |

Nenhum conflito acima autoriza mudança automática do design ou gameplay. Ausência de teste humano também não prova que a intenção foi rejeitada.

## Evidência e aceite

Há testes fontes específicos de [emissão da manopla](../../Assets/_Project/Scripts/Tests/PlayMode/BreakerKitEmissionPlayModeTests.cs), [F/finalização Asura](../../Assets/_Project/Scripts/Tests/PlayMode/AsuraEarlyFinishPlayModeTests.cs), [arco](../../Assets/_Project/Scripts/Tests/PlayMode/BowBoonPlayModeTests.cs), [lança](../../Assets/_Project/Scripts/Tests/PlayMode/SpearBoonPlayModeTests.cs) e [fluidez ranged](../../Assets/_Project/Scripts/Tests/PlayMode/RangedFluidityPlayModeTests.cs). Teste presente não é teste executado nesta revisão; testes de helpers também não comprovam integração por alvo/cena.

Cenários sugeridos, sem resultado medido nesta entrega: manopla sem boons por aproximadamente 10 min com 3–5 inimigos; lança com alvos em posições diferentes do eixo/ponta e perto de paredes; arco mudando alvo/em movimento e cancelando após emissão. Aprovação requer vídeo/observação do desenvolvedor, além dos testes técnicos aplicáveis.
