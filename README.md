# Tech-Guy (Title Subject to Change)

## Sinopse / Synopsis

## EN-US

In **Tech-Guy**, a software developer is unexpectedly pulled into his own computer and trapped inside a vast digital universe made up of interconnected worlds.

To find a way back to Earth, the player must explore these different realities, face enemies and bosses, unlock new abilities, and uncover the secrets behind the system that brought him there.

At the center of this universe lies a technological hub that acts as a safe zone between expeditions. There, the protagonist discovers his first weapon: a high-tech gauntlet capable of connecting directly to his nervous system and granting him combat abilities far beyond anything he possessed in the real world.

But weapons in **Tech-Guy** are more than equipment. They are intelligent, expressive companions with their own personalities, opinions, humor, and combat styles, inspired by the character-driven weapon interactions of **High on Life**.

The gameplay combines the action RPG foundations of **Diablo** and **Lost Ark** with roguelike elements inspired by **Hades**, while its visual identity blends futuristic technology with the stylistic diversity and dynamic presentation of **Spider-Verse**.

Every expedition reveals more about this strange digital reality - and brings the protagonist one step closer to answering the most important question:

**How do you escape a system when you don't have access to its source code?**

---

## PT-BR

Em **Tech-Guy**, um desenvolvedor de software é inesperadamente sugado para dentro do próprio computador e acaba preso em um vasto universo digital formado por diferentes mundos interconectados.

Para encontrar um caminho de volta para a Terra, o jogador deverá explorar essas diferentes realidades, enfrentar inimigos e chefes, desbloquear novas habilidades e descobrir os segredos por trás do sistema que o levou até ali.

No centro desse universo existe um grande hub tecnológico que funciona como uma zona segura entre as incursões. É nesse lugar que o protagonista encontra sua primeira arma: uma manopla tecnológica capaz de se conectar diretamente ao seu sistema nervoso e conceder habilidades de combate muito além daquelas que possuía no mundo real.

Mas as armas de **Tech-Guy** são mais do que simples equipamentos. Elas são companheiras inteligentes e expressivas, com suas próprias personalidades, opiniões, humor e estilos de combate, inspiradas nas interações entre armas e jogador de **High on Life**.

A jogabilidade combina as bases de action RPG de **Diablo** e **Lost Ark** com elementos roguelike inspirados em **Hades**, enquanto sua identidade visual mistura tecnologia futurista com a diversidade artística e o dinamismo de **Aranhaverso**.

Cada incursão revela um pouco mais sobre essa estranha realidade digital - e aproxima o protagonista da resposta para a pergunta mais importante de sua jornada:

**Como escapar de um sistema quando você não tem acesso ao código-fonte?**

---

## Características do Jogo

* **Universo Digital Interconectado**
  Explore diferentes mundos digitais, cada um com sua própria identidade, desafios, inimigos e segredos.

* **Combate Action RPG**
  Sistema de combate inspirado em jogos como **Diablo** e **Lost Ark**, com foco em habilidades, movimentação e diferentes estilos de jogo.

* **Elementos Roguelike**
  Incursões inspiradas em **Hades**, com progressão, desafios e novas possibilidades a cada tentativa.

* **Armas com Personalidade**
  Cada arma funciona também como um personagem, possuindo comportamento, diálogos, opiniões e uma identidade própria.

* **Manopla Tecnológica**
  A primeira arma do protagonista se conecta ao seu sistema nervoso, permitindo que ele utilize técnicas e habilidades que jamais aprendeu no mundo real.

* **Hub Central**
  Um ambiente tecnológico funciona como ponto de descanso, progressão e preparação entre as incursões pelos diferentes mundos.

* **Identidade Visual Variada**
  Uma mistura de tecnologia futurista e neon com diferentes estilos artísticos, permitindo que cada universo tenha uma aparência própria.

* **Mistério e Narrativa**
  Descubra por que o protagonista foi levado para esse mundo e quem - ou o quê - está por trás do sistema que conecta todas essas realidades.

---

## Gameplay Atual

> Esta seção descreve o estado atual da jogabilidade implementada e projetada.
> Itens ainda não implementados estão marcados como protótipo ou proposta.

### Fluxo de jogo

O jogo abre no **Menu inicial** (`MainMenu`). Na primeira vez, **Iniciar jornada**
leva ao **Prólogo / Tutorial**; concluí-lo ou pulá-lo abre o **Nexus** e libera
a opção **Continuar no Nexus** nas próximas entradas. **Repetir prólogo** permite
praticar o tutorial novamente sem apagar moedas, armas ou desbloqueios.

O **Nexus** é a zona segura de preparação antes de uma incursão. Dele, o portal
**01 / Incursão** leva ao **Setor 01**. Durante uma incursão, o jogador avança
pelos encontros sem voltar ao lobby entre as etapas. O retorno ao Nexus ocorre
ao concluir a incursão ou ao morrer. A arma escolhida no arsenal permanece salva
entre incursões; bênçãos e transformações duram apenas a incursão atual.

As configurações oferecem opções de vídeo (modo de tela, resolução, nível
gráfico, VSync), áudio e remapeamento de controles (seleção/movimento, movimento
alternativo, esquiva e as quatro habilidades), tanto no menu inicial quanto na
pausa durante a run.

### Controles

* **Botão esquerdo:** seleciona um inimigo, aproxima-se e ataca automaticamente
  dentro do alcance e com linha de visão. Clicar no chão move o personagem e
  cancela o alvo.
* **Botão direito:** também move e interage. Ambos os comandos são remapeáveis.
* **Shift + botão esquerdo/direito:** executa um ataque básico parado na direção
  do cursor, sem selecionar inimigos nem criar ordem de movimento.
* **Q / W / E / R:** habilidades da arma equipada; o cursor define a direção.
* **Espaço:** esquiva (dash). O dash padrão é gratuito, mesmo sem mana.
* **E:** interage com terminais e estações no Nexus. **Esc:** pausa o jogo (e
  fecha textos de terminal). Teclas duplicadas são rejeitadas; Esc fica reservado
  para a pausa.

Ataques básicos não gastam mana e funcionam durante a recarga das habilidades.
A pausa interrompe tempo de combate, recargas, projéteis, movimento, animações
e áudio.

### HUD

Uma HUD compartilhada (inspirada na barra de ações de **Diablo IV**) aparece nas
cenas de jogo:

* Orbes de **vida** e **mana** atuais/máximas nas extremidades.
* Slots **Q / W / E / R** da arma equipada e **Espaço** para esquiva, com ícone,
  nome e custo.
* Feedback de uso (flash dourado, estado "em uso", cobertura radial e contagem
  de recarga) e aviso ao tentar usar sem mana, em recarga ou durante outro golpe.
* **Energia Asura** integrada à mesma interface quando a manopla está equipada.

O jogador tem **1500** de mana máxima, com regeneração de 4% por segundo iniciando
1,25 s após o último gasto. Não há regeneração após a morte.

### Arsenal e armas

Na bancada **Arsenal** do Nexus, o jogador escolhe e equipa uma arma; a seleção
fica salva entre cenas e sessões. A **manopla** é a arma padrão. Cada arma tem
quatro habilidades em **Q / W / E / R**:

| Arma | Q | W | E | R |
| --- | --- | --- | --- | --- |
| Manopla | Avanço Relâmpago | Punhos Relâmpago | Impacto de Choque | Rajada Asura |
| Arco e flecha | Disparo duplo | Tiro concentrado | Leque de flechas | Chuva de flechas |
| Lança | Estocada do dragão | Lua crescente | Rajada espiral | Dragão vermelho |

As habilidades da manopla custam, respectivamente, **80 / 150 / 140 / 280** de
mana; o **R** (Rajada Asura) também exige energia Asura cheia. A manopla gera
energia Asura ao alternar entre categorias de golpe (Impulso e Choque) e durante
a Rajada ganha proteção de dano, acompanha o cursor e pode ser finalizada mais
cedo com uma esquiva.

Os kits de arco e lança são inspirados em arquétipos de **Lost Ark** (Breaker/Asura,
Sharpshooter e Glaivier), adaptados ao jogo. Os modelos e efeitos de arco e lança,
além de parte das animações, ainda são provisórios e aguardam uma etapa de acabamento.

### A manopla (estilo Asura)

| Tecla | Habilidade | Comportamento |
| --- | --- | --- |
| Q | Avanço Relâmpago | Avanço curto limitado pelo NavMesh e dois socos |
| W | Punhos Relâmpago | Sequência de socos curtos com um finalizador |
| E | Impacto de Choque | Dois impactos; o segundo causa dano de postura e stun |
| R | Rajada Asura | Consome energia Asura: rajada de socos e finalizador circular |

Durante a execução de uma habilidade, o movimento, o ataque básico, o dash e as
outras habilidades ficam bloqueados. Morte ou troca de arma encerram a sequência
e zeram a energia Asura.

### Estrutura da incursão (Setor 01)

O **Setor 01 — Memória Corrompida** é a primeira fase jogável. A incursão atual
tem uma sala inicial, arenas de combate conectadas e termina com um boss. Entrar
em uma arena fecha as portas e ativa apenas os inimigos dela; eliminar todos abre
a saída. A cena entregue encadeia três encontros em ordem:

1. **Acesso** — três inimigos normais.
2. **Relé** — três normais e um acelerado.
3. **Guardião** — um raro resistente/acelerado com dois normais, culminando no
   boss **Guardião do Núcleo**.

A cada grupo eliminado, a vida é restaurada e 20% da mana máxima é recuperada,
suavizando a primeira incursão. Ao limpar a última sala, um **anel dourado de
extração** retorna ao Nexus; morrer também retorna ao lobby após alguns segundos,
permitindo tentar novamente. Entrar novamente reinicia os encontros.

O layout ainda não é procedural e não há ramificações nem múltiplos andares;
ainda não há recompensa persistente entre incursões.

### Bênçãos, modificadores e sinergias

Ao limpar uma sala, o jogador escolhe entre três **bênçãos** (via clique ou teclas
**1 / 2 / 3**). As bênçãos duram apenas a incursão e são aplicadas em runtime, sem
modificar os assets de arma e habilidade. Exemplos atuais:

| Bênção | Efeito durante a incursão |
| --- | --- |
| Núcleo de força | +25% de dano de básicos e habilidades |
| Mãos velozes | +25% de velocidade dos básicos |
| Fluxo arcano | +15 p.p. de redução de recarga |
| Coração de ferro | +100 de vida máxima e cura completa |
| Foco eficiente | Q custa 25% menos mana e recarrega 35% mais rápido |
| Disparo prismático | Substitui Q por cinco flechas perfurantes em leque |
| Nova de impacto | Substitui Q por dano circular (manopla/lança) |

Há também uma coleção de **modificadores específicos por arma** (cerca de dez por
arma, muitos com múltiplos níveis) que transformam o comportamento das habilidades:
multiplicar projéteis, repetir golpes, adicionar ricochetes e ondas, gerar energia
extra, entre outros. Sinergias universais já implementadas incluem **Bobina
encadeada** (descargas em cadeia), **Reator de sucata** (explosão ao matar),
**Combustível instável** (queimadura por fogo) e **Ressonância térmica** (bônus
por fogo/gelo). O objetivo de design é permitir combinações exageradas e
emergentes a cada run. Itens utilizáveis e consumíveis são uma proposta ainda não
implementada.

### Inimigos e boss

Os inimigos usam um sistema de raridade com cor e tamanho próprios:

| Raridade | Cor | Observação |
| --- | --- | --- |
| Normal | Branco | Base |
| Mágico | Azul | Atributos reforçados |
| Raro | Amarelo | Maior, com afixos configuráveis |
| Boss | Laranja | Nome próprio, muito maior |

Afixos como **Gelo** (aura que reduz movimento), **Acelerado** (mais velocidade de
movimento e ataque) e **Resistente** (recebe menos dano) alteram o comportamento.
Os ataques inimigos exibem avisos legíveis (contorno e anel crescente) antes do
dano, permitindo esquivar saindo da área.

O **Guardião do Núcleo** encerra o Setor 01: fecha distância com investidas,
alterna golpes pesados e libera uma onda de choque expansiva. Abaixo de 50% de
vida entra em fúria, com ataques mais rápidos. Quebrar sua postura cancela a
sequência de ataque atual.

---

## Contribuições

**Tech-Guy** ainda está em desenvolvimento.

Ideias, sugestões e contribuições são bem-vindas. Consulte o guia de contribuição do projeto para mais informações.

---

## Créditos

**Desenvolvimento:** Gabriel Filla Camargo
**Game Design:** --
**Arte:** --
**Música e Efeitos Sonoros:** --
