# Inimigos e boss do FirstSector

| Tipo | Nome | Tamanho relativo |
| --- | --- | --- |
| Normal | Branco | 1× |
| Mágico | Azul | 1× |
| Raro | Amarelo | 1,4× |
| Boss | Laranja, com nome próprio | 2,1× |

As cores aparecem sobre os inimigos e no painel de alvo selecionado. A escala usa o tamanho original do prefab, sem multiplicar novamente quando o perfil é reaplicado. A altura da barra acompanha o tamanho; o boss também tem uma barra mais larga. Os afixos de inimigos comuns continuam aparecendo no nome.

O **Guardião do Núcleo** ocupa o lugar do antigo raro na última sala, acompanhado dos dois inimigos normais existentes. Usa o modelo atual ampliado, 1.800 de vida e classificação de reação Boss. Não herda os afixos de pressa/defesa do antigo raro. Sua IA substitui os ataques comuns:

- **Impacto do núcleo:** círculo de 4,5m ao redor do boss, aviso de 1,2s e 30 de dano.
- **Bombardeio:** fixa um círculo de 2,6m na posição do jogador, avisa por 1,1s e causa 22 de dano. Sair do círculo evita o golpe.
- **Fúria (50% de vida):** avisos 20% mais rápidos, intervalos menores e dois bombardeios por ciclo.

O círculo visível usa o mesmo centro e raio da verificação de dano. Morte/desativação cancelam os ataques e removem os avisos. A saída e a recompensa final continuam exigindo a morte de todos os inimigos da sala, incluindo o boss.

`SectorBoss` é instalado pelo diretor na ativação da cena; `EnemyVariant` aplica a aparência de raridade em todas as cenas. Ajustes de cores/tamanho ficam em `EnemyVisualStyle`, e os valores iniciais de combate em `SectorBoss`.
