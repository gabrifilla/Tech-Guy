# Direção visual — regras sem presumir paleta aprovada

<!-- RUNEXP06 CURRENT -->
## RUN-EXP-06 — direção05 aceita; universos futuros exploratórios

03/11/2026. Fragmento quadrúpede, Emissor de conchas e arquivoFirstSector05 foram aceitos para produção; acabamento final continua limitado. Arquivo Estratificado, Metrópole de Letreiros Recortados e Palimpsesto Corrompido são hipóteses originais de linguagem/layout, sem IDs/lore/ordem de campanha aprovados. Física/leitura/telegraphs comuns.

Estado06 prevalece sobre gates pendentes/restrições de fases anteriores abaixo, preservados como histórico. [Evidência e limites](../decisions/RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md).
<!-- /RUNEXP06 CURRENT -->

<!-- RUNEXP05 CURRENT -->
## RUN-EXP-05 — estudo Arquivo estratificado

02/11/2026. Escolha de trabalho para esta fatia: cerâmica marfim, estrutura grafite, cobre nas junções, turquesa pontual. Planos largos/encaixes e piso quieto conectamUI, inimigos e coberturas/racks. Vermelho de ameaça mantém prioridade. ImagensGameView eguia portátil permitem comparar; conceitoimage_gen não é prova do modelo. PENDING DEVELOPER APPROVAL; não vira paleta/shader global aprovado.

[Entrega e limites](../decisions/RUN_EXP_05_0_ART_DIRECTION.md).
<!-- /RUNEXP05 CURRENT -->

**Status:** diretriz macro de arte estabelecida; paleta exata, mundo e shader final ainda exigem decisão baseada em estudos no jogo real.

## Direção

Na próxima revisão dos mapas, avaliar cenários híbridos2.5D: imagens aprovadas pelo desenvolvedor aplicadas em planos ou volumes simples alinhados à câmera. Preservar identidade por mundo, silhuetas e legibilidade. Ver [plano e fluxo de aprovação](../decisions/MAP_ART_2_5D_FUTURE_PLAN.md). Nenhum mapa foi convertido por esta decisão.

Arte estilizada com cor, contraste, silhuetas e composição tomando Hades como uma **referência visual**, com densidade espacial que também pode beber de Lost Ark. HUD e leitura de combate usam Diablo como referência funcional. O jogo deve manter identidade própria, com tema digital/tecnológico alinhado à sinopse, **sem definir** automaticamente laboratório, ruína ou instalação industrial como estética final.

## Receita visual consistente

O Codex/artista deve registrar uma vez, após estudos aprovados:

| Item | Parâmetro a decidir/testar | Proibição |
|---|---|---|
| Paleta | cores de chão, personagem, threats e VFX | inimigo se funde com piso |
| Materiais | rugosidade, iluminação, normal/textura, shader, contorno | asset importado com shader aleatório |
| Silhuetas | escala comparativa jogador/comum/elite/boss | FX cobre pose/ataque |
| Ambiente | detalhe concentrado nas bordas e áreas não críticas | arena com ruído em toda superfície |
| Iluminação | direção e contraste calibrados na câmera gameplay | highlights escondem tell |
| Pós-processamento | exposição, bloom, grade e anti-aliasing por preset | valores inventados antes de medir |
| Tipografia/UI | hierarquia, contraste e tamanho na resolução-alvo | cada tela um estilo diferente |

## Processo de decisão artística

1. Capturar baseline atual em arena, menu, reward e UI.
2. Selecionar 2–3 referências por **propriedade concreta** (paleta, material, hierarquia, iluminação), documentando limites de uso e direitos.
3. Fazer estudos comparáveis **na mesma câmera, cena e resolução**; renderizar contact sheet quando útil.
4. Usuário escolhe a direção. Agente implementa receita reutilizável, não presets arbitrários asset a asset.
5. Máximo duas rodadas com correções objetivas nos dois maiores defeitos por vez; depois exigir referência adicional em vez de tentativa aleatória.

## Estado

Nenhum color grade/LUT, família de shader, densidade final ou preset de iluminação é declarado aprovado aqui. A inspeção de arquivos de 18/10/2026 confirma Unity 6000.5.10f1, URP 17.5.0 e o asset URP vinculado em GraphicsSettings; isso é implementação, não aceite artístico. A fonte de ambiente e limitações está em [../02_ESTADO_E_LACUNAS.md](../02_ESTADO_E_LACUNAS.md).
