# Clique e direção visual ilustrada

## Clique próximo ao jogador

`WorldClickResolver` ordena os acertos do raycast e ignora todos os colliders da hierarquia do jogador, incluindo equipamentos. Inimigos e itens continuam selecionáveis, inclusive atrás do corpo do jogador. Obstáculos sólidos continuam bloqueando o que está atrás deles; triggers de área sem interação são ignorados.

Clicar no próprio avatar não emite uma nova ordem de movimento. Cliques de chão dentro de 0,55 m dos pés também são ignorados, preservando combo e seleção. O destino é validado no NavMesh antes de substituir a ordem atual. O raio pode ser ajustado em `CharControlScript > Movement Click Dead Zone`.

## Visual

A referência orientou contornos escuros, sombras em faixas, áreas de luz quentes e sombras frias, com pedra verde-petróleo, âmbar e acentos de energia. Os modelos e a composição dos cenários foram preservados.

- Shader próprio `Tech Guy/Illustrated Toon`, compatível com o URP do projeto: contorno por casca invertida, iluminação em faixas, sombras e variação suave de pigmento.
- Materiais gerados em `Assets/_Project/Art/Toon/Materials`; materiais originais, inclusive de terceiros, permanecem intactos.
- Perfil `IllustratedAtmosphere.asset`: contraste, saturação, bloom moderado e vinheta.
- Iluminação e antialiasing ajustados nas cenas com mundo 3D.
- Menu `Tools > Tech Guy > Art > Apply Illustrated Look`: reaplica às cenas e prefabs do projeto; materiais já ilustrados são preservados para permitir ajustes manuais.

O RealToon instalado tem includes ausentes e não foi usado. Não foram alterados arquivos desse plugin. Esta implementação muda o tratamento visual dos assets existentes; não recria as ilustrações e os modelos da referência.
