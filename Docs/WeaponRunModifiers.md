# 30 modificadores de arma

Uma primeira coleção jogável de dez modificadores para cada arma. Direção de design inspirada em transformações específicas de arma, sinergias entre propriedades e recompensas por condições de combate, usando Hades, The Binding of Isaac e Dead Cells como referências criativas. Os efeitos e números abaixo pertencem ao Tech Guy.

Cada recompensa inclui uma opção exclusiva da arma enquanto houver níveis disponíveis. A maioria chega ao nível 3; propriedades binárias têm apenas um nível. As descrições das ofertas mostram o próximo nível. Tudo dura apenas a run e é aplicado durante a execução, sem alterar assets compartilhados. Os modificadores universais de fogo, gelo, bobina, detonação e ressonância continuam disponíveis.

## Arco

| Modificador | Efeito |
| --- | --- |
| Corda tripla | +2 projéteis por nível para cada flecha básica/Q/W/E; divide dano individual por `1 + 0,5 × nível`. No nível 1 são três flechas a 66,7% cada. |
| Agulhas espectrais | Todas as flechas atravessam inimigos; paredes continuam bloqueando. Um nível. |
| Flecha saltadora | +2 ricochetes por nível, buscando outro alvo a até 6m; cada salto conserva 75% do dano. O alcance total da flecha permanece limitado. |
| Olho caçador | Flechas corrigem gradualmente a trajetória para inimigos num cone frontal. Um nível. |
| Balista portátil | W: +60% de dano e +25% de preparação por nível. |
| Tambor de disparos | Q: +2 disparos por nível; intervalo dividido por `1 + 0,25 × nível`. |
| Pavão de aço | E: +4 flechas por nível, distribuídas num leque de até 100°. Combina com Corda tripla. |
| Monção | R: +3 pulsos e +20% de raio por nível. |
| Nuvem obediente | R recalcula o centro pelo cursor a cada pulso, respeitando alcance. Um nível. |
| Horizonte mortal | Acertos diretos ganham dano com a distância atual entre jogador e inimigo, até +60% por nível aos 12m. Inclui a chuva. |

Exemplo: Pavão + Corda + Agulhas + Flecha saltadora cria um leque de projéteis que atravessam ou saltam entre inimigos. Cada acerto real pode transportar fogo/gelo e iniciar uma cadeia universal. Se há alvo de ricochete, a flecha muda de direção; caso contrário, a perfuração mantém o movimento.

## Lança

| Modificador | Efeito |
| --- | --- |
| Haste impossível | +30% de alcance dos básicos e alcance/raio das habilidades por nível. |
| Estocada ecoante | Habilidades de estocada recebem +1 repetição por nível; dano de cada golpe dividido por `1 + 0,2 × nível`. |
| Órbita das luas | W: +2 varreduras por nível. |
| Tridente espiral | E: três direções por pulso, cada uma a 60% do dano. Alvos na interseção podem receber mais de uma direção. Um nível. |
| Dragão liberto | R acrescenta uma onda perfurante de alcance duplo, com 60% do dano por nível, a cada pulso. |
| Ponta contaminada | +25% de dano direto por fogo/gelo já no inimigo, por nível. |
| Distância perfeita | Acertos diretos a pelo menos 3m ganham +45% de dano por nível. |
| Carrasco | +60% de dano direto por nível contra inimigos abaixo de 30% da vida antes do golpe. |
| Lua viajante | W: +25% de raio por nível e centro avança 1,5m por pulso. Precisa de Órbita das luas para haver pulsos adicionais. |
| Condutor vital | Recupera 2 de mana por nível em cada acerto direto com dano. Descargas/explosões universais não recuperam mana. |

Exemplo: Haste + Eco + Tridente multiplica alcance, repetições e direções de E. Fogo/gelo + Ponta contaminada + Distância perfeita alimentam o dano que será herdado pelas sinergias universais.

## Manoplas

| Modificador | Efeito |
| --- | --- |
| Punhos titânicos | +25% de alcance, largura e raio dos básicos e habilidades por nível. |
| Propulsor de combate | Q original: +70% de avanço por nível, limitado pelo NavMesh. |
| Mil punhos | W: +2 réplicas do golpe final por nível, com 55% de dano/postura, separadas por 0,18s. |
| Epicentro | E: impactos tornam-se esferas centralizadas no jogador, com +20% de raio por nível. |
| Asura reverberante | R: +1 réplica final por nível, com 65% de dano/postura. |
| Dínamo de combate | Habilidades originais Q/W/E geram +10 de energia Asura por nível, além da alternância normal. |
| Reserva divina | R conserva 20 de energia por nível após consumir Asura. Máximo de 60, portanto não permite repetir R sozinho. |
| Demolidor | Habilidades originais: +75% de dano de postura e +30% de empurrão por nível. |
| Terceiro impacto | Cada terceiro ataque básico acrescenta nova de 2,5m com 60% do dano da arma por nível. |
| Motor em pane | Abaixo de 40% da vida do jogador, +50% de dano direto por nível. |

Exemplo: Dínamo + Reserva aceleram a frequência de Asura. Punhos + Demolidor + Asura reverberante ampliam área, dano de postura e número de impactos. Fogo, ressonância e detonação podem transformar esses impactos em reações de grupo.

## Regras e limites

- Os valores percentuais de um mesmo modificador crescem com seus níveis; multiplicadores de condições diferentes se combinam multiplicativamente.
- Flechas têm conjunto de vítimas por projétil: perfurar/ricochetear não acerta repetidamente a mesma vítima. Flechas distintas podem acertar o mesmo alvo.
- Projéteis e conjurações encerram ao morrer ou trocar de arma. Recargas começam depois da duração estendida das habilidades.
- As modificações de número de disparos não se aplicam à chuva, que é uma sequência de áreas e tem seus próprios modificadores.
- A transformação universal de Q continua disponível. Modificadores dependentes de uma mecânica substituída deixam de afetá-la: por exemplo, Propulsor não move a Nova de impacto e Eco não repete uma varredura. Modificadores de slot compatíveis, alcance e condições de dano continuam funcionando.
- Estes modificadores não persistem no Nexus. O teto de níveis limita projéteis e pulsos; as cascatas universais mantêm seus próprios limites de 32 impactos e quatro gerações.
- Uma run atual de três salas oferece apenas três escolhas; a coleção inteira exige múltiplas runs para explorar. O teste automatizado equipa todos os modificadores no máximo para verificar casos extremos.

## Validação

`WeaponModifierValidation.Run` entra em Play Mode e encerra o Editor ao terminar; usar numa cópia isolada com `-batchmode -nographics`, sem `-quit`. Cobre catálogo, isolamento por arma/run, teto de níveis, combinações de planos, energia Asura, sorteio/escolha de recompensa e dano real das 12 habilidades com todos os modificadores no máximo. Compara armas/habilidades originais antes/depois. Os testes não substituem avaliação visual e de balanceamento, especialmente com leques extensos e homing.
