# Requirements Document

## Introduction

Esta feature reestrutura dois espaços de interface do Tech Guy para ficarem mais coesos com o
resto do jogo e com referências consagradas do gênero:

- **Nexus Lobby** — hoje as três armas ficam escondidas atrás de um único painel de menu
  (`ARSENAL / ARMAS`). A proposta é expô-las como pedestais/altares físicos espalhados pelo
  salão, no estilo do hub de Hades: o jogador caminha até uma arma, vê um cartão de detalhes
  no estilo do Hades, e pode equipá-la ali mesmo. O painel único ``ARSENAL / ARMAS`` atual é
  substituído por essa disposição (não é mantido como alternativa). Um único Training Dummy no
  lobby permite testar as habilidades da arma equipada sem sair; para treinar à distância o
  jogador simplesmente se afasta do dummy.
- **Menu Inicial** — hoje usa um fundo procedural abstrato (grid/anel ciano). A proposta é
  aproximá-lo de um menu estilo Portal 2: uma lista de opções alinhada à esquerda sobre um
  cenário de fundo do próprio mundo do jogo (uma vista do Nexus), com destaque visual claro na
  opção em foco. O menu continua sendo desenhado no IMGUI (``OnGUI``) atual; a reorganização é
  visual e de layout, não uma migração de tecnologia de UI.

O escopo cobre a disposição das armas como estações físicas, o tooltip de proximidade, o
equipar por proximidade, o Training Dummy de treino, e a reorganização visual do menu inicial.
A feature reaproveita os sistemas existentes (`Actor`, `WeaponLoadout`, `WeaponScript`,
`LobbyInteraction`, `MainMenuUI`, geração de cena por scripts de Editor) e não introduz nova
progressão, loja, nem transferência de inventário entre cenas.

## Glossary

- **Pedestal de arma**: uma estação física no lobby que representa uma arma (Manopla, Arco,
  Lança), com um modelo decorativo, uma âncora de interação e um raio de proximidade.
- **Cartão de arma (estilo Hades)**: painel informativo ancorado junto ao pedestal, exibido ao
  aproximar-se, mostrando nome, estilo e habilidades da arma, sem exigir uma ação de menu — no
  estilo dos cartões do hub de Hades.
- **Training Dummy**: alvo de treino no lobby, que recebe dano e feedback de combate mas não
  morre permanentemente nem concede recompensas, permitindo testar as habilidades da arma.
- **Estação bloqueada**: pedestal de uma arma ainda não desbloqueada; mostra o custo e não
  permite equipar até o desbloqueio.

## Requirements

### Requisito 1 — Armas como pedestais físicos no lobby (estilo Hades)

**História de usuário:** Como jogador no Nexus, quero ver as armas dispostas como pedestais
físicos pelo salão, para escolher minha arma andando até ela em vez de abrir um menu único.

#### Critérios de aceitação

1. QUANDO a cena do Nexus Lobby é carregada, ENTÃO o sistema DEVE posicionar um pedestal físico
   distinto para cada arma do `WeaponLoadout` (Manopla, Arco e Flecha, Lança).
2. ENQUANTO o lobby está ativo, o sistema DEVE dispor os pedestais de forma espaçada ao redor de
   uma área central livre, seguindo a referência de layout do hub de Hades (chegada ao sul,
   estações nas laterais, centro navegável).
3. QUANDO um pedestal é posicionado, ENTÃO o sistema DEVE incluir um modelo/marcador visual da
   arma correspondente e uma âncora de interação alcançável pelo NavMesh a partir do ponto de
   chegada do jogador.
4. O sistema NÃO DEVE exigir a abertura de um painel de menu para o jogador identificar qual
   arma corresponde a cada pedestal.
5. QUANDO o jogador equipa uma arma em um pedestal, ENTÃO o sistema DEVE persistir a seleção via
   o mecanismo existente do `WeaponLoadout` para as próximas incursões.

### Requisito 2 — Cartão de arma estilo Hades por proximidade

**História de usuário:** Como jogador, quero que ao me aproximar de um pedestal apareça um
cartão de detalhes da arma no estilo do Hades, para avaliar o equipamento sem abrir menus.

#### Critérios de aceitação

1. QUANDO o jogador entra no raio de proximidade de um pedestal de arma, ENTÃO o sistema DEVE
   exibir um cartão de arma com o nome da arma, o estilo/família e a lista de habilidades.
2. QUANDO o cartão é exibido, ENTÃO o sistema DEVE mostrar, para cada habilidade da arma, o
   nome (`DisplayName`), o custo de mana e o tempo de recarga, mais a descrição quando disponível.
3. QUANDO o cartão é exibido, ENTÃO ele DEVE ser posicionado ancorado junto ao pedestal
   correspondente (estilo Hades), e não como um painel central genérico.
4. QUANDO o jogador sai do raio de proximidade de um pedestal, ENTÃO o sistema DEVE ocultar o
   cartão daquele pedestal.
5. QUANDO o jogador está no raio de um pedestal cuja arma ainda não foi desbloqueada, ENTÃO o
   cartão DEVE indicar o estado bloqueado e o custo de desbloqueio.
6. QUANDO o jogador está no raio de um pedestal cuja arma está atualmente equipada, ENTÃO o
   cartão DEVE indicar o estado "equipada".
7. O cartão NÃO DEVE bloquear o movimento do jogador nem exigir uma tecla para ser exibido
   (aparece por proximidade); a ação de equipar/desbloquear permanece explícita.

### Requisito 3 — Equipar e desbloquear armas por proximidade

**História de usuário:** Como jogador, quero equipar (ou desbloquear) uma arma diretamente no
pedestal, para preparar minha incursão sem passar por um menu central.

#### Critérios de aceitação

1. QUANDO o jogador está no raio de um pedestal de arma desbloqueada e aciona a interação, ENTÃO
   o sistema DEVE equipar aquela arma no jogador via `WeaponLoadout.Select`.
2. QUANDO o jogador aciona a interação em um pedestal de arma bloqueada E possui moedas
   suficientes, ENTÃO o sistema DEVE desbloquear a arma via `WeaponLoadout.TryUnlock` e equipá-la.
3. QUANDO o jogador aciona a interação em um pedestal de arma bloqueada E NÃO possui moedas
   suficientes, ENTÃO o sistema NÃO DEVE desbloquear a arma e DEVE comunicar a falta de moedas.
4. QUANDO uma arma é equipada em um pedestal, ENTÃO o sistema DEVE atualizar imediatamente o
   estado exibido (o pedestal atual e os demais) para refletir qual arma está equipada.
5. QUANDO a arma equipada muda, ENTÃO o sistema DEVE atualizar o carregamento de habilidades do
   jogador para que o Training Dummy e o HUD reflitam a nova arma.
6. O sistema DEVE usar a mesma tecla de interação já configurada no lobby (`GameControl.Skill3`).

### Requisito 4 — Training Dummy para testar habilidades

**História de usuário:** Como jogador, quero um boneco de treino perto dos pedestais, para
testar as habilidades da minha arma equipada antes de iniciar a incursão.

#### Critérios de aceitação

1. QUANDO a cena do Nexus Lobby é carregada, ENTÃO o sistema DEVE posicionar exatamente um
   Training Dummy próximo à área dos pedestais, alcançável pelo jogador. Para treinar ataques à
   distância, o jogador simplesmente se afasta do dummy — não há necessidade de múltiplos alvos.
2. QUANDO o jogador atinge o Training Dummy com um ataque ou habilidade, ENTÃO o dummy DEVE
   receber o dano e exibir o feedback de combate padrão (número de dano/barra), reaproveitando o
   sistema existente.
3. QUANDO a vida do Training Dummy chega a zero, ENTÃO o dummy NÃO DEVE ser destruído
   permanentemente; ele DEVE restaurar a vida para poder continuar sendo usado como alvo.
4. O Training Dummy NÃO DEVE conceder moedas nem recompensas ao ser atingido ou "derrotado".
5. O Training Dummy NÃO DEVE atacar o jogador nem se mover para persegui-lo (é um alvo passivo).
6. QUANDO o jogador troca de arma em um pedestal, ENTÃO o Training Dummy DEVE permanecer
   utilizável para testar as habilidades da nova arma.

### Requisito 5 — Coesão do layout do lobby

**História de usuário:** Como jogador, quero que o lobby fique bem organizado e coeso com o
resto do jogo, para me localizar e circular com clareza.

#### Critérios de aceitação

1. QUANDO os pedestais e o Training Dummy são posicionados, ENTÃO o sistema DEVE manter uma área
   central navegável e livre de obstáculos entre a chegada e a saída principal.
2. O sistema DEVE preservar a identidade visual do Nexus (materiais escuros, circuitos ciano,
   detalhes existentes) descrita na documentação atual do lobby.
3. QUANDO o layout é gerado/aplicado, ENTÃO todas as estações (pedestais, dummy, portais)
   DEVEM ser alcançáveis pelo NavMesh a partir do ponto de chegada do jogador.
4. O sistema DEVE preservar as estações e portais não relacionados a armas já existentes
   (incursão, estações informativas) e sua funcionalidade atual.
5. QUANDO o layout é regerado, ENTÃO o sistema NÃO DEVE quebrar as referências de GUID de cena e
   prefab existentes.

### Requisito 6 — Menu inicial estilo Portal 2

**História de usuário:** Como jogador, quero um menu inicial parecido com o do Portal 2, para
ter uma tela de entrada mais imersiva e organizada.

#### Critérios de aceitação

1. QUANDO o Menu Inicial é exibido, ENTÃO o sistema DEVE apresentar as opções principais em uma
   lista vertical alinhada à esquerda da tela, mantendo a renderização no IMGUI (`OnGUI`) atual.
2. QUANDO o Menu Inicial é exibido, ENTÃO o fundo DEVE mostrar um cenário do mundo do jogo (uma
   vista do Nexus), em vez de um padrão abstrato de grid.
3. QUANDO uma opção do menu está em foco (por teclado ou mouse), ENTÃO o sistema DEVE destacá-la
   visualmente de forma clara e distinta das demais, no estilo do realce do Portal 2.
4. O sistema DEVE preservar as ações existentes do menu (iniciar/continuar, repetir prólogo,
   configurações, controles, sair) e seus destinos de cena.
5. QUANDO o jogador navega com o teclado (setas/enter) ou com o mouse, ENTÃO o foco e a ativação
   DEVEM funcionar de forma consistente com o comportamento atual do menu.
6. O sistema DEVE manter as telas secundárias existentes (configurações, controles, sair,
   confirmação de vídeo) funcionais após a reorganização visual.

### Requisito 7 — Legibilidade e escala em diferentes resoluções

**História de usuário:** Como jogador, quero que o lobby e o menu fiquem legíveis em qualquer
resolução, para jogar confortavelmente em telas diferentes.

#### Critérios de aceitação

1. QUANDO a resolução da tela muda, ENTÃO o cartão de arma e os painéis do lobby DEVEM
   escalar mantendo a legibilidade, como o comportamento de escala já usado hoje.
2. QUANDO a resolução da tela muda, ENTÃO a lista de opções e o realce do menu inicial DEVEM
   permanecer alinhados e legíveis.
3. O sistema NÃO DEVE sobrepor o cartão de arma a outros painéis ativos (por exemplo, o
   menu de pausa) de forma que impeça a leitura ou a interação.

## Fora de escopo

- Loja, economia nova ou progressão além do desbloqueio de armas já existente.
- Transferência de inventário/estado entre lobby e incursões.
- Novos modelos de arte finais para as armas ou para o dummy (marcadores/modelos decorativos
  reaproveitados são suficientes).
- Áudio/trilha nova para o menu ou lobby.
- Migração de IMGUI para UI Toolkit/uGUI: por decisão do projeto, o menu e o lobby continuam em
  IMGUI (`OnGUI`); esta feature reorganiza o layout, não troca a tecnologia de UI.
- Manutenção do painel único `ARSENAL / ARMAS` como alternativa: ele é substituído pelos
  pedestais físicos, não preservado como fallback.





