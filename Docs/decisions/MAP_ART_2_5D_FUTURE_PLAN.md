# Mapas híbridos 2.5D — preparação para a próxima revisão

02/11/2026. Pedido do desenvolvedor: considerar imagens2D que aparentem volume na câmera do jogo, permitindo aprovar primeiro as imagens e aplicá-las depois no cenário.

**Decisão:** incluir essa abordagem como candidata explícita na próxima frente de mapas. Esta autorização cobre planejamento; não converte os mapas atuais nem implanta um novo renderer agora. As imagens específicas e a adoção técnica dependem de um protótipo comparativo.

## Abordagem e integração

Combinar imagens pintadas/renderizadas com planos recortados ou volumes3D simples. A técnica2.5D pode usar cartões fixos, impostores e texturas projetadas. A arquitetura não precisa girar para encarar a câmera: um plano fixo alinhado pode ser melhor; billboard é uma opção para elementos adequados.

| Elemento | Primeira opção a experimentar |
|---|---|
| Fundo distante e estruturas fora da área jogável | Imagem em plano fixo, com perspectiva/escala correspondentes à câmera |
| Paredes e fachadas | Volume simples com imagens nas faces ou planos em camadas de profundidade |
| Prop sem interação | Cartão recortado ou impostor, com apoio/pivot coerentes |
| Cobertura que bloqueia tiro e circulação | Volume3D simples mantém colisão/LOS; imagem corresponde à sua silhueta |
| Piso | Superfície simples texturizada; evitar detalhes que imitem ameaça ou degrau físico |
| Personagens, armas e objetos vistos de muitos lados | Manter3D como referência inicial; sua conversão não integra esta proposta de mapas |

Inspeção local: `TG_TopDown_Camera` segue o jogador com altura, distância e ângulo configuráveis. Registrar a projeção efetiva e os limites de zoom/ângulo por cena; não presumir uma câmera imutável para todo o projeto. Se a câmera variar, a ilusão deve continuar aceitável ou usar uma alternativa definida, como outra imagem ou malha simples.

`FirstSectorRoomPresentation` e `FirstSectorEnvironmentArt` já separam apresentação da geometria nativa. São pontos possíveis de integração, **não um sistema2.5D implementado**. Não criar campos vazios, dependências, singletons ou um framework especulativo nesta preparação.

## Contrato da implementação futura

- Colisão, NavMesh, cobertura, bloqueio de projéteis, spawns e dash continuam na geometria física nativa. Transparência não abre passagem física automaticamente.
- Pivot no ponto de apoio, escala em metros e imagem correspondente ao obstáculo real: sem cobertura invisível ou espaço aparentemente livre que bloqueie o jogador.
- Validar jogador/inimigos/VFX à frente e atrás, recorte, profundidade e interação com `CameraOcclusionFader` em toda a área jogável.
- Definir o papel da iluminação pintada, sombra de contato e sombra em tempo real, evitando duplicação. Normal/depth maps são alternativas, não exigências universais.
- Dimensionar atlas, margens transparentes, mipmaps, compressão e materiais pelo tamanho real na câmera. Evitar planos transparentes grandes sobrepostos sem necessidade.
- Preservar a identidade de cada mundo e a leitura do combate; não impor uma estética única a todos os mapas.

## Fluxo de aprovação e protótipo

1. Escolher uma família pequena e uma câmera de autoria; capturar bloco cinza com jogador e ameaças para escala.
2. Apresentar imagens de estilo/elementos ao desenvolvedor. Aprovar silhueta, materiais e perspectiva antes de produzir o lote.
3. Aplicar a imagem aprovada ao plano ou volume simples, com física independente. Aprovação da imagem não equivale ao aceite automático no jogo.
4. Revisar naUnity com movimento, combate, sombras e extremos de câmera/zoom permitidos.
5. Comparar com3D na mesma sala, câmera, resolução, luz e população ativa. Decidir por família antes de converter outros ambientes.

Primeiro recorte proposto: uma fachada periférica e uma cobertura já aprovada. A cobertura comprova que a ilusão não rompe circulação/LOS. Não começar convertendo todas as salas.

Brief reutilizável: **imagem/fonte aprovada; câmera/projeção; ângulos/zoom válidos; dimensões/pivot; papel físico; camadas de profundidade; material/atlas/resolução; origem da luz/sombra; fonte editável/proveniência; evidênciaGameView e custo**.

Menos triângulos não garante maisFPS: texturas, transparência, overdraw e sombras também custam. Medir CPU/GPU, draw calls/batches, triângulos, overdraw e memória de texturas em condições equivalentes, incluindo8/32/64 inimigos quando relevante. Ganho na facilidade de autoria é um benefício separado de desempenho. Não mudar globalmente oURP para testar uma família.

## Estado e referências

**DONE:** intenção registrada, ponto de integração identificado e fluxo imagem→aprovação→aplicação→revisão definido. **NOT DONE:** renderer2.5D, novos assets, bake de impostores e conversão de mapas. **NOT TESTED:** economia real e compatibilidade por família. **PENDING DEVELOPER APPROVAL:** imagens específicas, aparência no jogo e adoção após protótipo.

Skills já aplicadas na frente atual: TechGuyGameVision e referências locais, create-game-assets/art-direction/three-d-pipeline, level-design/tech-guy-level-layout e performance-optimization. Mantidos câmera real, aprovação humana, física independente e medição antes de otimização.

Referências primárias: [UnityBillboardRenderer](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/billboardrenderer), sem escolher esse componente como implementação obrigatória; [UnityURP — blending modes](https://docs.unity.com/en-us/engine/6000.7/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/blending-modes), sobre a relevância de overdraw; [AndroidDevelopers — materiais e shaders](https://developer.android.com/games/optimize/materials), sobre transparências sobrepostas. Referências de técnica não são benchmark deTech-Guy nem prova de suporte de todo recurso na versão6000.5 atual.
