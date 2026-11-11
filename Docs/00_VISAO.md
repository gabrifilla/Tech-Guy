# Visão de produto — Tech Guy

**Status:** direção de produto; não é uma lista de funcionalidades entregues. Implementação e lacunas ficam em [02_ESTADO_E_LACUNAS.md](02_ESTADO_E_LACUNAS.md). Esta consolidação não concede novo aceite de gameplay.

## Conceito

Tech Guy é um **ARPG roguelite top-down em Unity**, de ritmo intenso, protagonizado por um desenvolvedor preso em um universo digital. Há um hub (Nexus), encontros e progressão em runs. O jogo procura combinar a resposta de combate de Lost Ark com a compreensão visual de Diablo e uma estética estilizada com referências artísticas em Hades, sem se converter numa cópia desses jogos.

A história está na seção [Narrativa](#narrativa) abaixo. O Nexus é o espaço seguro de preparação. As armas são companheiras inteligentes e expressivas, com personalidade, humor e diálogos; High on Life orienta esse aspecto. A presença de kits de arma não comprova um sistema de diálogos implementado. Spider-Verse permanece referência de estudo visual, não aprovação de um shader, paleta ou mundo final. O papel de cada referência está em [references/README.md](references/README.md).

## Narrativa

*Definida pelo desenvolvedor em 10/11/2026. Substitui a sinopse antiga ("sugado para o próprio computador").*

**Premissa.** O protagonista é um desenvolvedor de jogos do mundo real. Numa madrugada de crunch, ele desmaia em frente ao computador enquanto trabalha no próprio jogo. Ao acordar, está **dentro do mundo do jogo que estava construindo**, só que fragmentado e confuso.

**A Manopla.** Ele encontra um objeto desconhecido, que não tem forma de manopla. Ao ser tocado, o objeto se funde à mão dele de forma agressiva e invasiva e se conecta pelos nervos até o cérebro. A fusão causa muita dor, mas transfere todo o conhecimento da arma, principalmente a arte marcial para a qual ela foi "criada": o **boxe**. É assim que o jogador ganha as skills para sobreviver. Ainda atordoado, ele começa a conversar com ela.
- **Nome:** **Ico** (decidido em 10/11/2026). O nome é intencional: em algum momento haverá um easter egg referenciando o jogo *ICO* (Sony, 2001), em forma de homenagem (sem usar assets, personagens ou marca do jogo).
- **Personalidade:** smug e sarcástica, mas solidária e amigável.
- **Motivação:** ela também não sabe como foi parar ali. Os dois buscam juntos o caminho de volta ao mundo real.

**Universos como caminho de volta.** Cada universo deixa o protagonista um pouco mais perto do mundo real. A ordem planejada é (1) tecnológico, (2) cartoonizado e (3) noir em preto e branco, conforme o plano.

**Armas.** Cada arma tem personalidade própria, pertence a um universo diferente e se conecta ao jogador de um jeito diferente para transmitir a arte marcial que domina.
- **Lança:** personalidade mais feminina, refinada, *lady-like*, por causa da leveza dos golpes.
- **Arco:** a decidir.
- **Foco atual:** só a Manopla.

**O Desconhecido (primeiro NPC resgatado).** *Decidido em 10/11/2026.* É um NPC que o protagonista começou a criar para o próprio jogo e nunca terminou. "Desconhecido" é literalmente o nome provisório que ficou no projeto.
- **Ironia dramática:** o jogador sabe que está diante da própria criação; o NPC não sabe.
- **Conhecimento parcial:** ele conhece aquele universo e sabe **um pouco** sobre a "confusão" entre os universos, mas tem lacunas, como um personagem incompleto.
- **Progressão:** a cada retorno ao Nexus, ele pode ganhar uma camada nova (nome, lembrança, função), como se estivesse sendo "terminado". Isso também dá motivo para conversas novas e evita repetir diálogo.

**Boss final (conceito).** Um ser cujo propósito é destruir tudo e todos, inclusive a si mesmo: a destruição de todos os universos é "um preço baixo a se pagar" (uma pegada de vilão convicto à la Thanos, sem copiar). Os golpes **mudam os universos**: ondas de choque fazem o jogador alternar entre universos durante a luta.

## Fantasia de jogo

> Encarar grupos de inimigos variados com armas de identidade forte; reagir a padrões legíveis; construir, ao longo da run, combinações que transformem o jeito de lutar e produzam histórias de gameplay próprias.

O jogador deve identificar a causa de cada resultado: um golpe interrompeu o oponente; outro quebrou a postura; uma combinação de boons alterou geometria/tempo/risco; o boss respondeu de maneira distinta por ter resistência apropriada.

## Camadas da experiência

1. **Segundos:** comando → movimento/ataque → impacto reconhecível → reação inimiga → decisão seguinte.
2. **Encontro:** leitura dos papéis do swarm, posicionamento, priorização de threats e interação com elites/telegraphs.
3. **Run:** escolha de recompensas e boons, mudança de comportamento da build e trade-offs visíveis.
4. **Fim da run:** resumo de desempenho (ex.: dano por skill, fontes de dano, ganhos), sem sacrificar clareza.

## Loop e identidades — direção consolidada

Preparar-se no **Nexus → escolher arma → atravessar encontros e ler ameaças → escolher recompensas → experimentar a build modificada → encerrar por extração, morte ou abandono → retornar ao Nexus**. A progressão da build acontece durante a run; persistência, condições de saída e oportunidades reais pertencem a [run e progressão](systems/05_RUN_E_PROGRESSAO.md). Esse loop é intenção de experiência, não certificação de um percurso integrado já aprovado.

As três armas resolvem os grupos de maneiras distintas: **Manopla** aproxima, pressiona postura e cria oportunidades para Asura; **Lança** recompensa alinhamento e domínio do espaço; **Arco** prioriza alvos, distância e reposicionamento. Kits, exceções e divergências entre essa identidade e o runtime têm fonte única em [armas](systems/02_ARMAS.md). [Boons](systems/04_BOONS.md) devem modificar decisões e comportamento; Mutation não é requisito para o kit base funcionar.

## Decisões e hipóteses

| Condição | Como interpretar |
|---|---|
| Direção consolidada | Comando ativo, identidades distintas, causalidade legível, reação conforme alvo, boons transformadores e recursos visíveis. Critérios de diversão/leitura e vetos estão em [pilares](01_PILARES.md), sem presumir aprovação por testes. |
| Implementação em avaliação | O feel da Manopla, a percepção das builds e o percurso integrado exigem observação e avaliação humana. Resultados técnicos das Gameplay Intelligence 1.0–1.3 estão na [matriz](validation/01_MATRIZ_DE_VALIDACAO.md); não aprovam automaticamente o conjunto. |
| Hipóteses/propostas | Conteúdo desligado, Mythics e candidatos de boons; paleta, shader e ambiente finais; duração e composição ideais dos encontros. Ver [boons](systems/04_BOONS.md) e [arte](art/01_DIRECAO_VISUAL.md). Não são backlog autorizado. |
| Escopo ainda por aprovar | Recorte do Vertical Slice 1.0, desfecho mínimo e conteúdo de resultados, conforme [plano](03_PRIORIDADES.md). O relatório por habilidade continua uma decisão de produto; eventual adiamento exige aprovação explícita. |

Não existe uma segunda Bible a manter: esta visão/sinopse orienta os documentos canônicos por sistema. Os limites rejeitados abaixo continuam vigentes.

## O que o jogo não é

- Auto-combat onde o jogador apenas assiste.
- Armas idênticas com números/alcances diferentes.
- Roguelite em que quase todas as escolhas são `+X%`.
- Reprodução completa do HUD de Hades, da progressão MMO de Lost Ark ou do endgame de Diablo.
- Simulador de física em que qualquer hit empurra/lança indiscriminadamente.

## Declarações a confirmar no repo

As quantidades de salas, boons, inimigos, animações, classes, fases concluídas, direção visual final e durações de encontros não são fixadas aqui: exigem inspeção e decisão autoral específica.
