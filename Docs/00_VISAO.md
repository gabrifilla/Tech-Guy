# Visão de produto — Tech Guy

**Status:** direção de produto; não é uma lista de funcionalidades entregues. Implementação e lacunas ficam em [02_ESTADO_E_LACUNAS.md](02_ESTADO_E_LACUNAS.md). Esta consolidação não concede novo aceite de gameplay.

## Conceito

Tech Guy é um **ARPG roguelite top-down em Unity**, de ritmo intenso, protagonizado por um desenvolvedor preso em um universo digital. Há um hub (Nexus), encontros e progressão em runs. O jogo procura combinar a resposta de combate de Lost Ark com a compreensão visual de Diablo e uma estética estilizada com referências artísticas em Hades, sem se converter numa cópia desses jogos.

Na intenção narrativa, o protagonista foi sugado para o próprio computador, procura voltar à Terra e entender o sistema. O Nexus é o espaço seguro de preparação. As armas são imaginadas como companheiras inteligentes e expressivas, com personalidade, humor e diálogos; High on Life orienta esse aspecto. A presença de kits de arma não comprova um sistema de diálogos implementado. Spider-Verse permanece referência de estudo visual, não aprovação de um shader, paleta ou mundo final. O papel de cada referência está em [references/README.md](references/README.md).

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
