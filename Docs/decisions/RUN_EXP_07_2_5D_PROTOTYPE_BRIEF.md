# RUN-EXP-07 — brief do primeiro experimento híbrido2.5D

03/11/2026. **Proposta futura; não iniciada e não autorizada pela execução06.** O desenvolvedor escolhe a linguagem e revisa imagens antes da conversão. Referências: [direção06.5](RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION.md) e [plano inicial](MAP_ART_2_5D_FUTURE_PLAN.md).

## Pergunta e recorte

Uma imagem aprovada, aplicada sobre representação simples, consegue manter volume convincente e leitura do combate na câmera real, com autoria mais conveniente e custo aceitável?

Comparar exatamente **uma fachada periférica** e **uma cobertura física existente**. VersãoA é o3D atual; B usa fachada em plano fixo/poucas camadas e cobertura em volume simples com faces pintadas. Manter A disponível. Não converter salas inteiras, personagens ou inimigos. Não construir framework, novos mundos, nova câmera, bosses, boons ou física.

## Preparação e aprovação das imagens

1. Abrir o projeto real/Editor correto; registrar branch, estado das cenas, prefs e Console. Capturar referência A com medidas/pivots, luz, materiais e posição de câmera.
2. Escolher com o desenvolvedor uma das linguagens propostas ou outra direção explícita. Produzir somente as imagens originais necessárias às duas peças, com arquivo editável, proveniência, paleta funcional e transparência intencional. Incluir topo/lados da cobertura; uma foto frontal não define seu volume.
3. Mostrar as imagens na escala de leitura prevista, junto ao jogador e ao chão de referência. **Aguardar revisão das imagens antes de converter.** Registrar a versão aceita; ajustes posteriores de perspectiva não podem trocar silenciosamente a direção.
4. Só então criar B em cena/cópia isolada ou toggle exclusivamente de desenvolvimento. Usar AssetDatabase para mover assets com suas metas; preservar A e a geometria física. Não publicar B por padrão.

Ficha mínima por peça: dimensões em metros, ponto de apoio/pivot, textura e fonte, ocupação em pixels na GameView, faixa de câmera, modo de alpha, material, UV, faces/camadas, iluminação pintada/dinâmica, sombra de contato, renderer associado ao fader e objeto físico correspondente.

## Condições comparáveis

Mesma sala FirstSector de cobertura, seed/rota registradas, câmera perspectiva FOV40°, altura16/distância16/ângulo0, luz, URP, qualidade, resolução, jogador, armas, população e ordem de ações. Registrar transforms e configurações de A/B, sem compensar um cartão incorreto com outra câmera/luz. Fazer capturas em1160×600,1280×800 e1920×1080.

Percurso: posição central, aproximação, ambos os lados, ida/volta, bordas próximas do enquadramento e pontos onde a câmera cruza a fachada. Não existe controle normal de órbita/zoom encontrado06; portanto validar o follow atual. Se for proposta variação futura de altura/FOV/ângulo, testar e declarar uma faixa própria em vez de afirmar suporte ilimitado. Fora da faixa, preferir3D ou outra peça; não restringir o jogador sem decisão explícita.

## Provas emUnity

| Área | Procedimento e condição de continuidade |
|---|---|
| Perspectiva e escala | Vídeo/percurso e capturas A/B nos mesmos pontos. Sem verso exposto, pé flutuante ou colapso evidente de volume na faixa aceita. |
| Física/visual | Overlay de colliders/NavMesh mais captura normal. Nenhuma passagem aparentemente aberta bloqueada, nem cobertura invisível. Manter duas circulações, dash e clique no chão. |
| LOS e projéteis | Emissor perde tiro/LOS atrás da mesma cobertura e readquire ao contornar. Fragmento faz caminho completo pelos dois lados; tiros não atravessam a cobertura porque a imagem tem alpha. |
| Profundidade | Jogador, Fragmento, Emissor, projéteis, hitVFX, tornado, eletricidade e fogo antes/depois de cada peça, incluindo recortes entre camadas. Sem sorting que esconda perigo. |
| Oclusão | Atravessar posição que aciona CameraOcclusionFader, esperar retorno e repetir. Verificar MeshRenderer associado à raiz física e material em opacidade parcial; collider/LOS permanecem ativos. SpriteRenderer/BillboardRenderer exigiriam trabalho extra, não suporte presumido. |
| Bordas | Zoom de QA mais escala real, fundo claro/escuro, mips e compressão alvo. Sem halo, franja, cintilação ou corte grosseiro que comprometa silhueta. Não confundir esse zoom de QA com controle normal do jogo. |
| Luz e sombra | A/B com luz constante; contato no chão e lado de sombra coerentes. Verificar sombra duplicada/pintada e sombra projetada do plano. Se necessário, solução local simples medida, sem trocar pipeline. |
| Gameplay | Básicos, especial, Q/E/R, dash, pausa/retorno, mortes/cleanup; mesmas autoridades e resultados. Não aceitar arte que esconda telegraph ou alvo. |

Iniciar com MeshRenderer e materiaisURP existentes. Plano fixo é hipótese da fachada; volume pintado é hipótese da cobertura. Se cartões falharem em profundidade/oclusão, comparar malha3D com overlay antes de inventar impostor multivista, shader de profundidade ou billboard global.

## Medição e decisão

Medir A/B em blocos alternados na mesma sessão/máquina após warmup, primeiro com a população real da sala, depois8/32/64 ativos se a alteração afetar esse cenário. Mesmos AI, navegação, dano, animações e VFX; imunidade somente do jogador da fixture. Registrar resolução, backend, hardware, vsync/framecap, duração, amostras e versão. Separar Editor de standalone.

Coletar mediana/p95 de frame CPU e GPU quando disponíveis, GC, draw calls/batches, renderers, triângulos, materiais, memória total alocada e memória de textura carregada; VRAM só com ferramenta que realmente a meça. Frame Debugger/overdraw para explicar camadas. Se marcador GPU não produzir amostras válidas, declarar indisponível e não converter FPS em GPU. Medir tempo de autoria de cada etapa e quantidade de revisões de A/B. Não chamar variação de uma execução de otimização.

Critérios de decisão por peça:

- **Manter hipótese:** leitura/física/LOS/fader passam, desenvolvedor aceita o resultado no jogo e custo/autoria justificam a adoção. Registrar limitações e faixa de câmera.
- **Revisar localmente:** problema tem correção restrita de pintura, corte, UV, material ou volume, sem alterar controles/física/pipeline; repetir comparação afetada.
- **Rejeitar método naquela família:** perde leitura/volume, exige sistema desproporcional ou piora custo sem benefício de autoria/arte aceito. Preservar A; fachada e cobertura podem ter decisões diferentes.

Não exigir um ganho percentual arbitrário sem baseline e orçamento acordados. Uma2.5D mais leve em triângulos pode gastar mais em pixels/texturas; uma solução de custo semelhante ainda pode valer por direção de arte/autoria, se isso for aceito conscientemente.

## Entrega e parada

Entregar cena/toggle de comparação isolado, imagens originais e fontes, ficha de cada peça, walkthrough A/B com mesmos pontos, resultados/artefatos brutos, tempos de autoria, limites conhecidos e parecer separado para fachada/cobertura. Verificar referências, GUIDs, prefs atuais, Editor fora dePlay e cenas salvas.

**Parar após as duas peças e a revisão.** Aceite das imagens não é aceite da integração; aceite do protótipo não autoriza conversão global. Só avaliar um pequeno perfil de arte se conteúdo real revelar repetição que o justifique. Nenhum WorldStyleManager, renderer por universo ou hierarquia antecipada de ScriptableObjects faz parte deste brief.
