# Ataques dos inimigos — revisão física

O repertório foi reconstruído para o modelo humanoide desarmado que já existe no jogo. O dano acompanha o contato da animação, o deslocamento ou a passagem do projétil. A direção é comprometida durante a preparação; o inimigo não gira atrás do jogador no instante do impacto.

| Variante | Comportamento | Contra-jogo |
|---|---|---|
| Normal | Aproxima-se até o alcance do braço e desfere um soco frontal curto | Flanquear durante a preparação |
| Magic_Haste | Investe quando distante; perto, intercala soco e combo de dois socos | Desviar lateralmente da corrida e aproveitar a recuperação |
| Rare_Frost | Carrega uma esfera na mão e dispara um estilhaço reto quando distante; perto, usa socos | Sair da trajetória depois do disparo; cenário sólido bloqueia o projétil |
| Rare_Haste_Guard | Investida para fechar distância, socos e pancada pesada com as duas mãos | Evitar a preparação lenta e punir a recuperação longa |
| Rare_Frost_Haste_Guard | Seleciona projétil ou investida conforme a distância; corpo a corpo usa os golpes pesados e o combo | Ler a preparação corporal; não há sobreposição automática dos especiais |

## Guardião do Núcleo

O boss aproxima-se antes de usar golpes curtos. Longe, fecha a distância com uma investida real. Perto, alterna pancada pesada, um-dois e uma pancada que libera uma onda de choque expansiva. A onda causa dano quando sua borda alcança o jogador, uma única vez por onda.

Abaixo de 50% de vida, pode completar a investida ou o um-dois com uma pancada se o jogador permanecer perto. Também pode soltar uma segunda onda, com uma nova preparação completa. Quebrar sua postura cancela o ataque e os complementos daquela sequência.

Foram removidos os bombardeios teleportados, cruzes, contracortes gigantes e a alternância abstrata entre círculo e anel.

## Animação e manutenção

- `EnemyCombatActions`: preparação, contato, deslocamento, projétil, onda e recuperação.
- `EnemyAttackPatterns`: escolha do golpe por afixo e distância.
- `EnemyAttackExecution`: aviso cancelável; o dano instantâneo é usado apenas pelos contatos corpo a corpo.
- `EnemyMotions.asset`: biblioteca própria, com socos copiados sem eventos de dano e pancada de duas mãos criada para os inimigos.
- Menu `Tools > Tech Guy > Enemies > Install Physical Attacks`: liga a biblioteca aos prefabs e cenas existentes, preservando geometria e assets de origem.

Os testes cobrem colisão contínua da investida, projétil bloqueado por parede, tempo de viagem, cancelamento, seleção de golpes e progressão do boss.
