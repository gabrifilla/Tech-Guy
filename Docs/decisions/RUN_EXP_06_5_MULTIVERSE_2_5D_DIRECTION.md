# RUN-EXP-06.5 — universos com linguagens próprias e cenários híbridos

02–03/11/2026. **Design e preparação técnica concluídos; nenhuma conversão de mapa implementada.** Ponto de partida: [plano inicial](MAP_ART_2_5D_FUTURE_PLAN.md). O aceite do pedido06 cobre a integração01–05; não aprova estas propostas de mundo, suas imagens ou uma técnica2.5D ainda não testada. O [brief07](RUN_EXP_07_2_5D_PROTOTYPE_BRIEF.md) tem um recorte separado.

## O que a câmera e o projeto permitem afirmar

A fonte e a cena FirstSector usam `TG_TopDown_Camera`, perspectiva, FOV40°, altura16m, distância16m e ângulo0°. Nexus usa10m/10m/0°, também FOV40°. O follow acompanha a posição do personagem; não foi encontrado controle normal de órbita ou zoom. A orientação configurável no Inspector não é um compromisso de câmera imutável para todo mapa. A inspeção serializada viaMCP acompanha a validação06.

Mesmo sem órbita, a câmera perspectiva vê um objeto por raios diferentes quando ele está no centro, na borda ou quando o personagem passa por ele. Uma fachada pintada que funciona numa captura central pode perder volume durante o percurso. Por isso o teste07 inclui deslocamento do jogador e posições extremas, além de registrar a faixa de autoria realmente aceita. Não transformar uma limitação da imagem em alteração silenciosa da câmera.

`FirstSectorRoomPresentation`/`FirstSectorEnvironmentArt` já separam decoração e arena física. São pontos de autoria possíveis, não um renderer2.5D pronto. `CameraOcclusionFader.ResolveRenderers` procura **MeshRenderer** no collider/filhos, pai ou Rigidbody associado; não resolve SpriteRenderer/BillboardRenderer genericamente. Um plano MeshRenderer sob a raiz física é a primeira hipótese de encaixe. Material e transição de opacidade ainda precisam ser testados; nem todos os grupos decorativos atuais estão sob colliders de cobertura. Nenhuma compatibilidade universal foi presumida ou corrigida especulativamente06.

## Contratos compartilhados

| Invariante | Contrato verificável |
|---|---|
| Controle e identidade | Mesmo movimento, binding, latência, kit e silhueta do protagonista/arma. Estilo não muda dano, alcance, input ou autoridade. |
| Câmera e chão | Caminho livre, apoio dos pés, bordas físicas e destinos reconhecíveis na escala da GameView. |
| Inimigos | Fragmento baixo/quadrúpede e Emissor de casco aberto continuam distinguíveis por forma, postura e ritmo, inclusive sem cor. Não redesenhar estes inimigos nesta preparação. |
| Perigo e VFX | Telegraphs e projéteis mantêm prioridade de contraste/movimento; decoração não usa seus sinais como ornamento. VFX ativos passam antes/depois dos planos corretamente. |
| Física | Colisão, NavMesh, dash, spawns, LOS e cobertura continuam em3D, com os mesmos limites do objeto de referência. Transparência visual não abre passagem. |
| Interface | HUD, posse, recompensa, cancelamento e confirmação têm os mesmos significados entre mundos. O mundo pode mudar o entorno, sem mascarar estados. |
| Custo | Mesmo procedimento de CPU/GPU/GC/texturas/renderers. Triângulos menores não bastam para aprovar desempenho; nenhuma meta de FPS nova foi inventada. |

## Três propostas originais — nomes de trabalho, sem IDs de campanha

Os cartões abaixo são hipóteses de direção. Não estabelecem ordem da jornada, novos poderes, sociedade canônica ou novos inimigos. A conexão possível à busca pela Terra é uma pergunta de narrativa, não uma pista já implementada. Não há imagem autorada nova nem aprovação visual destes universos06.

### Arquivo Estratificado

**Tese:** um arquivo digital empilha registros em plataformas legíveis, enquanto sua indexação se desfaz nas margens. A regra visual é **camada identificável, ruptura localizada**. Contraste com FirstSector: preservar sua arquitetura de dados como ponto de partida, mas substituir repetição de racks por páginas estruturais, lombadas e placas escalonadas.

Formas retangulares com uma diagonal de quebra, cerâmica fosca e metal escuro; bordas pintadas com hachura curta somente nas faces verticais. Tons frios no miolo, papel quente nas superfícies de consulta, emissivo contido nas junções. Piso de baixo contraste e sem hachuras que imitem hitboxes. Fundo em planos fixos; cobertura em volumes simples com faces pintadas e topo inequívoco.

Geografia proposta: laços curtos em torno de depósitos, dois acessos visíveis a cada área de cobertura e uma lombada monumental como orientação. O jogador escolhe contornar a pilha para quebrar LOS ou cruzar a faixa aberta para alcançar o ranged. Essa diferença é composição com contratos existentes, sem mecânica nova de plataforma ou destruição. Na chegada, ver uma pilha íntegra ao lado de uma quebrada explica a organização espacial antes do primeiro encontro. Eventuais habitantes seriam mantenedores do arquivo; nenhuma nova família é autorizada. Vestígio candidato: índices incompletos de um endereço externo, a desenvolver somente em escopo narrativo próprio.

Som/movimento propostos: pequenos estalos de relé, folhas metálicas que assentam lentamente fora da área de combate; nunca tremor contínuo do piso. Produção **baixa a média** por reutilizar volumes modulares e atlas de bordas. Replayabilidade vem de recombinar massas/rotas e distribuição de vazios, não trocar apenas a cor. Risco: detalhes finos cintilarem na perspectiva. Fatia futura: chegada, laço de circulação e encontro com cobertura; teste em cinza e sem nomes deve continuar parecendo um arquivo estratificado.

### Metrópole de Letreiros Recortados

**Tese:** uma cidade de serviços tenta orientar seus habitantes por anúncios sobrepostos, enquanto as vias reais ficam entre as fachadas. Regra visual: **silhueta arquitetônica forte, luz como cartaz**. Não copiar prédios, personagens ou composição de filme algum.

Massas verticais assimétricas, pontes de serviço visuais, marquises largas e esquinas recortadas. Pintura chapada em três valores, manchas de luz de borda, retícula restrita ao fundo e a painéis grandes. Paleta limitada por quarteirão; néon não pulsante como foco de navegação, nunca igual ao sinal de ataque. Fachadas distantes podem usar cartões em profundidade; marquises próximas precisam de volume/topo coerente. Silhuetas do skyline devem distinguir este mundo mesmo em preto e branco.

Geografia proposta: avenidas abertas conectadas por travessas curtas e bolsões de recuperação visíveis. Decisão de combate: cruzar a avenida exposta ou usar esquina física para interromper o Emissor e avançar pelo lado. Nada de verticalidade jogável, trânsito ou letreiro que dê buff. Na chegada, um enquadramento de avenida revela o destino e a travessa alternativa. Habitantes e conflito podem se apoiar em serviços disputando atenção; isso é proposta, sem facções canônicas. Pergunta ligada à jornada: os sinais apontam para a Terra ou apenas recirculam pedidos?

Som/movimento: zumbido de transformador e anúncios abstratos espaçados, com animação ambiental lenta longe dos telegraphs. Produção **média**: exige skyline, cartazes originais e coerência de iluminação; cartões reaproveitáveis por fachada. Replayabilidade pela alternância avenida/travessa e marquises, com cuidado para não esconder o protagonista. Risco: excesso de emissivo, cartões transparentes grandes e retícula competindo com VFX. Fatia futura: uma esquina, uma avenida e um encontro; reconhecer rota e perigo sem ler anúncios é condição de continuidade.

### Palimpsesto Corrompido

**Tese:** um território pintado tenta recompor sua superfície sobre versões antigas que continuam aparecendo. Regra visual: **pincelada orienta a matéria; falha aparece em áreas delimitadas**. O aspecto orgânico vem de superfície e contorno, sem converter o jogo em outro sistema físico.

Massas arredondadas cortadas por estratos de tinta seca, volumes baixos com faces pintadas, planos distantes quebrados e sobreposições escassas. Cores minerais desaturadas, uma cor expressiva de corrupção reservada a áreas de fundo; chão de valores simples e bordas físicas sem distorção. Não aplicar jitter em toda a tela, ondular colisores ou pintar falso buraco em piso caminhável.

Geografia proposta: clareiras conectadas por rotas curvas, pilares assimétricos baixos e bordas fragmentadas exclusivamente fora do caminho. Decisão de combate: preservar distância na clareira ou contornar massa curta para interromper linha de tiro e reabrir alcance. É uma diferença de ritmo/layout com AI atual; atravessar planos ou terreno mutável exigiria outra aprovação. Na chegada, uma superfície recém-pintada deixa à mostra a estrutura antiga e demonstra o limite entre chão sólido e fundo. Ecologia e vestígios são candidatos de memória sobrescrita, sem inimigo ou regra local nova. Pergunta narrativa: uma assinatura apagada pode conter o caminho de retorno?

Som/movimento: raspagem e ressonância grave, pequenas alterações de tinta em fundo, com intensidade ajustável se vierem a existir. Produção **média a alta**, pois cada transição de massa pode exigir mais pintura e correção de perspectiva. Replayabilidade por módulos de contorno/clareira e contraste entre superfície preservada e apagada; evitar arte única por sala. Risco: fronteira visual ambígua, manchas parecendo chão perigoso e transparência em excesso. Fatia futura: uma clareira e um contorno; teste sem paleta deve mostrar uma geografia curva, distinta dos outros dois.

## Comparação de métodos para o protótipo

| Método | Uso inicial adequado | Custo/limite e decisão proposta |
|---|---|---|
| Plano2D fixo | Fundo periférico fora da circulação | Autoria simples; revela espessura ausente fora do eixo. Usar fachada como hipótese, sem girar tudo para a câmera. |
| Cartões em profundidade | Skyline, estratos e partes de fachada | Parallax real limitado, mais renderers/overdraw e risco de interseção. Poucas camadas, recortes ajustados à silhueta. |
| Plano recortado/cutout | Ornamento de contorno irregular | Alpha clip é candidato para recorte firme; testar serrilhado/mips/fader. Não assumir que resolve sombras e profundidade automaticamente. |
| Volume simples com faces pintadas | Cobertura e marquise próximas | Melhor correspondência entre topo, lados, colisão e sombra. Primeira opção para cobertura07; exige UV e pintura coerentes entre faces. |
| Textura projetada | Pintura sobre volume ou detalhe local | Pode reduzir UV manual em casos específicos; risco de esticar em faces laterais e depender do ponto de projeção. Não criar sistema global de projeção. |
| Impostor de uma/múltiplas vistas | Prop distante repetido com pouca variação de vista | Bake/atlas, troca de vistas, memória e iluminação são custos reais. Adiar multivista até demonstrar falha do plano/volume simples. |
| Malha3D com overlay2D | Preservar volume/sombra de cobertura e enriquecer material | Menor risco de perspectiva; arte/overdraw extras. Controle útil quando o cartão puro falhar, sem prometer menos triângulos. |
| Tratamento dependente da câmera | Somente elemento cuja faixa de vista não pode ser fixa | Billboard pode girar uma borda contra sua colisão. Não usar em cobertura; adiar adaptação dinâmica até o protótipo provar necessidade. |

Escolha inicial proposta: **fachada periférica em plano fixo ou poucas camadas; cobertura em volume simples com faces pintadas**. São duas hipóteses comparadas à referência3D; se não funcionarem na câmera real, manter3D naquela família. Nenhum método é aprovado só por esta recomendação.

## Arquitetura mínima e cadeia de autoria

Primeiro usar uma ficha de arte por família, com paleta funcional, dimensões/pivot, câmera de autoria, conjunto de materiais, texturas/fontes, papel de luz/sombra, layers físicos/visuais e imagens aceitas. Um prefab visual local com referências serializadas existentes é suficiente para duas peças. Não adicionar componentes vazios, loader, renderer, WorldStyleManager ou hierarquia de ScriptableObjects06.

Após07, se **duas ou mais famílias reais** exigirem repetição dos mesmos parâmetros e houver um caso de atualização verificável, avaliar um perfil de arte pequeno e plano para referências de materiais/atlas/paleta/luz. Física, input, navegação, danos, tickets e identidades nunca entram nesse perfil. Manter um URP comum; materiais e composição podem variar por mundo. Postprocess, animação ambiental e VFX específicos são opcionais sujeitos à leitura e custo, não requisitos de uma arquitetura nova.

Fluxo futuro: bloco físico e captura da câmera → imagens originais/fonte editável → revisão de estilo pelo desenvolvedor → aplicação local → validação em movimento/combate → revisão do resultado emUnity → decisão por família. Uma imagem aprovada não aprova o objeto final. Medir tempo de pintura, UV/aplicação, correção e revisão separadamente; facilidade de montagem é um benefício a medir, não uma economia presumida.

## Custos e fontes técnicas

Evitar margens transparentes excessivas, grandes superfícies sobrepostas e texturas dimensionadas pela resolução da fonte em vez da ocupação na tela. Testar mipmaps/compressão na plataforma alvo; memória CPU, memória de textura e VRAM não são a mesma medida. Pintura pode conter luz ambiente, mas não deve duplicar uma sombra dinâmica escura. Topo de cobertura deve corresponder ao volume que bloqueia tiro.

As opções de [material URP Unlit](https://docs.unity.com/en-us/engine/6000.0/manual/materials-and-shaders/built-in/shaders-in-universalrp/prebuilt-shader-graphs-urp/unlit) documentam blend e alpha clip; são alternativas a testar na versão instalada, não um shader já criado. A [documentação de ordem de renderização URP](https://docs.unity.com/en-us/engine/6000.0/manual/cameras/urp/advanced) orienta investigar overdraw com ferramentas de frame. A referência de [TextureImporter](https://docs.unity.com/en-us/engine/6000.7/script-reference/unityeditor/textureimporter) distingue a cópia legível na CPU; confirmar os campos na Unity6000.5 antes de qualquer mudança. Estas fontes não são benchmark de Tech-Guy.

Skills aplicadas: visão local/referências, tech-guy-world-identity e ficha world-card, create-game-assets/direção/proveniência/3D, level-design, shader-programming e performance-optimization, junto da inspeção UnityMCP/C#. **Não testado06:** perspectiva dos novos assets, profundidade/transparência/fader, ganho de GPU/VRAM ou tempo de autoria. Essa ausência é deliberada: a implementação pertence ao futuro07, que continua sem autorização para começar.
