# Direção visual — regras sem presumir paleta aprovada

**Status:** diretriz macro de arte estabelecida; paleta exata, mundo e shader final ainda exigem decisão baseada em estudos no jogo real.

## Direção

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
