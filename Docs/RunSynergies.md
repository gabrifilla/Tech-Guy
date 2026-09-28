# Sinergias de run

Implementado nas escolhas de bênção, com cópias acumuláveis e duração restrita à run:

| Modificador | Efeito | Combinações |
| --- | --- | --- |
| Bobina encadeada | Cada cópia acrescenta um alvo por salto; descargas herdam 45% do dano aplicado e os elementos. Alcance inicial 3,5 m. | Velocidade gera mais cadeias; crítico fortalece o início; fogo e gelo se espalham. |
| Reator de sucata | Morte por impacto/descarga/explosão causa explosão de 75% do dano aplicado. Cópias acrescentam 25 pontos percentuais e 0,5 m de raio. | A bobina mata um inimigo, a explosão mata outros e gera novas explosões. |
| Combustível instável | Com fogo adquirido, a queimadura recebe DPS adicional de 12% do dano aplicado por cópia. | Um crítico forte vira queimadura forte; descargas e explosões transportam essa propriedade. |
| Ressonância térmica | Descargas/explosões ganham +40% de dano por efeito de fogo/gelo já no alvo, por cópia. | Prepare a horda com elementos e dispare a cadeia. Duas cópias e dois elementos multiplicam cada impacto secundário por 2,6. |

Exemplo: fogo + gelo + duas ressonâncias + bobina fazem um salto de 45% virar 117% do dano anterior contra alvos previamente afetados pelos dois elementos. A propagação pode crescer em vez de diminuir. Adicione detonação para converter mortes em áreas que alimentam a cadeia.

O dano herdado é o efetivamente removido da vida: resistências e excesso de dano sobre inimigos fracos não inflam artificialmente a cadeia. Cada inimigo recebe no máximo um impacto secundário por acerto original; no máximo quatro gerações e 32 impactos secundários. Esses limites controlam custo computacional e ciclos, sem limitar os atributos acumulados. Mortes por ticks isolados de queimadura ainda não detonam. Queimaduras renovam duração e conservam o maior DPS, sem somar um novo DOT por acerto.

Ataques básicos, áreas, hitboxes e flechas usam a mesma entrada de efeitos. As recompensas continuam sendo reclamadas pelo troféu de sala. Os novos modificadores entram no sorteio; não são garantidos em uma run de três salas. Combustível depende de fogo; ressonância depende de elementos e de uma fonte de impactos secundários.

## Itens utilizáveis — propostas, ainda não implementadas

Proposta de inventário: um espaço de equipamento ativo, com recarga por salas concluídas, e dois espaços de consumíveis. Coleta por interação; mochila cheia oferece troca explícita. A tecla deve ser escolhida sem conflitar com Q/W/E/R, ataque, esquiva e interação.

Distribuição sugerida: consumíveis em caixas de suprimentos e como drops raros de inimigos; um equipamento ativo em sala de tesouro; itens de risco/recompensa em salas opcionais de elite; baterias e dados também vendidos por moedas. Evitar premiar itens somente depois da última luta, quando já não há oportunidade de usá-los. Valores e frequências precisam de teste de balanceamento.

| Item | Uso e custo sugerido | Sinergia desejada |
| --- | --- | --- |
| Capacitor de tempestade | Ativo; descarrega energia armazenada pelos seus acertos; recarga de duas salas. | Herda bobina e elementos. Builds rápidas carregam mais depressa. |
| Ampola de nitrogênio | Consumível; congela uma área por dois segundos. | Prepara ressonância e segura alvos dentro das explosões. |
| Óleo de plasma | Consumível; cobre uma área por seis segundos; o próximo fogo incendeia os alvos. | Combustível instável e crítico transformam o primeiro impacto numa ignição forte. |
| Granada de sucata | Consumível; explosão com dano baseado na arma atual. | Recebe modificadores de impacto e pode iniciar detonações em cadeia. |
| Bateria de emergência | Consumível; recupera 40% de mana e reduz pela metade a recarga restante das habilidades. | Permite outra sequência de habilidades elementais. |
| Duplicador defeituoso | Ativo; repete a próxima habilidade com 50% do dano; recarga de três salas. | A cópia herda propriedades, mas não duplica a si mesma. |
| Ímã gravitacional | Ativo; puxa inimigos comuns por dois segundos; recarga de duas salas. | Concentra alvos para bobina e explosões; chefes recebem apenas lentidão. |
| Coração de reserva | Consumível; converte 25% da mana máxima em escudo temporário. | Cria escolha entre sobreviver e gastar mana ofensivamente. |
| Dado de circuito | Consumível; sorteia novamente as três bênçãos antes da escolha. | Ajuda a procurar a peça que falta numa build sem garantir o resultado. |
| Fusível proibido | Consumível; dobra os níveis de bobina, reator, combustível e ressonância por oito segundos, mas impede cura nesse período. | Janela curta para combinações extremas; restaura os níveis originais ao terminar. |

Primeiro conjunto recomendado para implementação: nitrogênio, granada, bateria e dado. Cobrem preparação, dano, recuperação e construção da build sem exigir sistemas extensos de cópia de habilidade ou física de atração.

## Validação

Executar Unity em batch com `-executeMethod RunSynergyValidation.Run` (sem `-quit`, pois o teste entra em Play Mode e encerra sozinho). O teste cobre chance acumulável de gelo, propagação elemental, múltiplos colliders, amplificação por estados, morte explosiva, limpeza e limite de cascata. Conferir visualmente depois no FirstSector, com diferentes armas e combinações de bênçãos.
