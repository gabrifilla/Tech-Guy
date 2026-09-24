# Combate legível e referência visual

Abra `Assets/_Project/Scenes/CombatStudy.unity` e use Play. É uma cena isolada
para experimentar encontros, sem portais de retorno ao lobby. A regra de design
é seguir a run até derrotar o boss, conforme registrado no README.

## Leitura do combate

- Gelo: anel azul de 5 unidades acompanha o inimigo. O player ganha um anel nos
  pés e indicação de porcentagem de slow. Sobreposições continuam multiplicativas.
- Ataque inimigo: limite laranja fixado no início da preparação e anel amarelo
  crescente durante 0,7 segundo. O dano acontece uma vez no fim desse aviso.
  Sair da área evita o dano. O inimigo tem 0,35 segundo de recuperação.
- A velocidade de ataque altera o intervalo entre ataques, preservando o tempo
  mínimo de leitura do aviso. Desativação e parada do NavMeshAgent interrompem
  a preparação. Os eventos de animação de hitbox não causam um segundo impacto.
- O raio usado no aviso é o mesmo do cálculo do impacto. O ataque é circular
  nesta primeira versão; não é um cone. A tolerância vertical do impacto é 2m.
- Raridade e afixos aparecem sobre os inimigos da cena de estudo.

O aviso de ataque está no EnemyAI compartilhado, portanto também funciona no
Playground. O indicador do player é `CombatReadabilityUI`, instalado no player
do Playground e no estudo. Prefabs novos de player precisam desse componente.

## Combinações manuais

Os perfis continuam sendo Normal, Magic e Rare. Os bônus ficam em
`Additional Affix Prefabs` do EnemyVariant. Não há sorteio automático.

| Afixo | Efeito |
| --- | --- |
| Gelo | Aura com 30% de redução de movimento |
| Acelerado | Movimento x1,35; frequência de ataque x1,2 |
| Resistente | Recebe 70% do dano, ou seja, redução de 30% |

`Assets/_Project/Prefabs/EnemyVariants` contém os afixos e cinco exemplos:
Normal, Magic_Haste, Rare_Frost, Rare_Haste_Guard e Rare_Frost_Haste_Guard.
Frost no nome de um prefab descreve sua combinação, não uma nova raridade.
Um mesmo prefab de afixo listado duas vezes é aplicado apenas uma vez.
Reaplicar um perfil não acumula os multiplicadores. Resistência afeta o dano,
não imuniza contra slow, stun ou lançamento.

Na cena aparecem Normal, Magic com Acelerado e Rare com os três afixos. Os
valores são iniciais para comparação, ainda sem balanceamento definitivo.

## Referência de arte

O pátio de manutenção usa grafite, aço fosco e cerâmica âmbar. Armários com
venezianas, travas e estruturas ficam nas bordas; o centro fica livre para a
silhueta do personagem e os avisos do combate. O conjunto foi salvo como
`CombatReferenceDeck.prefab`, com NavMesh próprio.

A bancada do Nexus recebeu gavetas, puxadores, trilhos e módulos de ferramentas
usando a mesma família de materiais. Os detalhes são decorativos e preservam os
colisores existentes. São modelos geométricos editáveis, uma referência inicial
de direção visual, não assets finais de produção.

## Checagens

`CombatStudyValidation.Run` executa checagens em Play Mode de entrada/saída da
aura, sobreposição, remoção por desativação e morte, preservação de passivas,
reaplicação de perfil, resistência, preparação de ataque, esquiva e impacto único.
O teste encerra a instância do Editor; execute por CLI em uma instância separada.

Confira manualmente o ritmo das animações e a leitura dos efeitos durante combate
com vários mobs. A cena é um laboratório; não implementa a progressão da run.
