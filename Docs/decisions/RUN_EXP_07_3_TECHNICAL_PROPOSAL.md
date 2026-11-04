# RUN-EXP-07.3 — proposta técnica de duas peças, sem implementação

**DONE:** proposta compatível com owners e configuração encontrados. **NOT DONE / NOT TESTED:** qualquer método2.5D em runtime, custo A/B e assets de produção. **PENDING DEVELOPER APPROVAL:** direção/recorte antes de07.5. Não alterar URP global, câmeras, shaders de combate, física ou catálogo de boons.

## Recorte e associação física

ComparadorA é a apresentação05 atual. ComparadorB futuro muda apenas: uma cobertura existente, preferencialmente Cobertura deslocada0; um único módulo de fachada periférica equivalente a um rack existente. Não retirar a segunda cobertura, o piso, as outras paredes nem o cenário inteiro. Overviews ilustram a linguagem geral; implementação futura permanece duas peças.

Cobertura: envelope4,2×2,2×4,2m, pivot de autoria no apoio inferior; centro físico local(-3;1,1;-1,5), pé(-3;0;-1,5). Origem do futuro filho visual deve compensar pivot para reproduzir bounds nativos, sem duplicar escala dos pais. Fachada: módulo visual de referência2,45×3,8×0,48m (fonte FirstSectorEnvironmentArt); recuar/deformar somente dentro da parede física existente. Confirmar medida instanciada antes da autoria final. Sem collider novo.

FirstSectorRoomPresentation e FirstSectorEnvironmentArt já controlam meshes/materiais; adaptar apenas a família aprovada ou um prefab visual de revisão local. SetVisible atual troca o conjunto inteiro e **não é toggle por peça**. Comparador futuro deve preservar A e alternar somente as duas peças, sem chamar toggles globais que mudem outras superfícies. Racks são combinados: isolar uma instância de módulo visual no comparador é trabalho local, não sinal de que existe API pronta. Não criar WorldStyleManager, renderer por universo ou hierarquia especulativa de ScriptableObjects.

## Comparação dos métodos

| Método | Compatibilidade e custo | Uso proposto / fallback |
|---|---|---|
| Plano ilustrado fixo | MeshRenderer/URP existente; pouca geometria, vista lateral revela ausência de espessura | Fachada periférica em primeiro teste, com laterais se necessário |
| Cards de profundidade | Ordenação3D real, mais renderers/pixels/overdraw; alpha suave é risco | Máximo poucas camadas só onde silhueta justificar; começar com uma |
| Mesh recortado / alpha clip | Depth mais previsível que blend; bordas/mips e fade precisam prova | Silhueta de fachada, cortar margens em geometria |
| Volume simples / faces pintadas | Topo/lados/sombra correspondem à massa física, exige UVs coerentes | Primeira opção de cobertura, sem billboard |
| Projeção de textura | Estica nas faces oblíquas e pode depender do ponto de projeção | Não necessária às duas peças; UV convencional primeiro |
| Impostor único/multivista | Bake/atlas, memória e transição de vistas são custos novos | Adiar; não justificado para dois objetos próximos |
| Mesh3D + ilustração | Mantém volume/sombra, pode preservar triângulos e somar custo de textura | Fallback se plano falhar; não prometer otimização |
| Cartão orientado à câmera | Rotação visual diverge do collider fixo | Não usar em cobertura; só ornamento distante se caso concreto justificar |

URP/Lit, Simple Lit e Unlit foram encontrados via Shader.Find; materiais ativos usam Lit. Não foi criada feature nova. Fonte instalada/fader é autoridade de integração. Referência técnica externa: [URP Unlit — Unity6000.5](https://docs.unity.com/en-us/engine/6000.5/manual/materials-and-shaders/built-in/shaders-in-universalrp/reference/unlit-shader), consultada nesta tarefa; alpha clip/blend são opções disponíveis, não prova de resultado no jogo.

## Profundidade, câmera e oclusão

Câmera perspectiva FOV40 e follow16/16/ângulo0 são baseline, não câmera fixa universal. Mesmo sem órbita, a vista relativa muda quando o objeto atravessa centro/borda. Primeiro suportar o percurso real atual: centro, extremos laterais, aproximação, frente/trás e ida/volta. Nenhuma faixa extra está certificada. QA exploratória pode testar FOV35/40/45 e yaw±10/altura14/16/18 **somente na cópia de teste**, sem publicar controles ou aceitar automaticamente toda combinação. Se falhar fora do follow aprovado, manter3D naquela peça e registrar faixa; não limitar controle do jogador silenciosamente.

Usar depth test nativo, evitar sortingOrder para desenhar tudo na frente. Conferir jogador/Fragmento/Emissor, flechas, lança, tornado, eletricidade e fogo à frente/atrás/interseção. Não deixar margens de textura cobrirem perigo. Fachada não deve projetar ornamento sobre área de circulação. Cutout e blend durante fade têm comportamentos distintos.

CameraOcclusionFader procura MeshRenderer nos filhos do collider, pai ou attachedRigidbody, sem subir pela raiz inteira. Usa probes0,5m, opacidade0,12, fadeout0,12s/fadein0,25s/restoredelay0,12s, pathreveal2,5m. Material com _OcclusionOpacity segue caminho dither; Lit/Simple Lit/Unlit conhecidos recebem clone transparente temporário, ZWrite0, DepthOnly/DepthNormals desligados. _ALPHATEST_ON é desligado no caminho transparente: **um plano recortado pode revelar seu retângulo/margens ao desvanecer**. Não presumir suporte alpha clip perfeito. Começar com mesh recortada sem grandes margens; testar restore material/alpha/renderqueue e custo. Shader novo só se falha concreta justificar e dentro de autorização específica. Collider e LOS nunca somem com o fade.

## Iluminação e sombra

Cobertura: começar com URP/Lit opaco e pintura de albedo material; manter normais geométricas e luz real. Pintar microvariação/oclusão de fendas, evitar sombra direcional dura sobre a textura. Fachada distante: Unlit pintado é alternativa, com sombra projetada desligada se ela duplicar sombra desenhada; Lit simples é controle se player parecer desconectado. Unlit não responde à luz como Lit; comparação deve declarar essa diferença deliberada.

Contato no chão preferencialmente pela massa3D nativa/render volume da comparação, sem novo collider ou blob duplicando sombra existente. Uma única fonte de sombra forte. Não criar normal/depth map por rotina; usar normal somente se uma prova de relevo justificar ganho visual/custo. Sem baked lightmaps novos para arena gerada em runtime nesta proposta. Não alterar exposição/bloom/LUT global para salvar uma imagem incorreta.

## Asset e custo — estimativas, não benchmark

Proposta inicial: cobertura atlas albedo1024²; fachada1024×2048, dependendo da ocupação medida no quadro1920×1080. PNG de conceito não define orçamento final. sRGB para albedo; mapas de dados, se existirem, lineares. Mipmaps ligados para minificação perspectiva; Read/Write desligado, wrapClamp em fachada, atlas com8–16px de extrusão no mip base e teste de bleeding em mips. Esses valores são ponto de partida, não preset aplicado. Ajustar pelo tamanho real e teste de bordas. Transparência só para silhueta necessária, margens cortadas; cobertura opaca.

Windows alvo: avaliar BC7 para preservar pintura/alpha, fallback de compressão conforme suporte da build; verificar resultado no Inspector/build. Estimativa matemática BC7 8bpp com cadeia completa de mips≈4/3:1024²≈1,33MiB;1024×2048≈2,67MiB; total≈4MiB somente albedo. RGBA32 sem compressão≈16MiB para ambas com mips. Cabeçalhos, alinhamento, streaming, cópiasCPU, textura residente e VRAM real não estão incluídos. Normal opcional adicionaria custo; não está exigida. Atlas compartilhado economiza switches potenciais, mas clones de fade podem aumentar materiais e quebrar batches.

Menos triângulos não garante menos tempo de GPU: pixel coverage, blend, shadowpass e texturas podem custar mais. Medir CPUmain/render, GPU quando marcador válido, GC, batches/drawcalls, renderers, tris e memória total/texturas; VRAM somente com ferramenta apropriada. Não rodar stress novo de desempenho nesta fase07.0–4. Benefício principal pode ser autoria e diversidade visual.

## Matriz futura de validação07.5/07.6 — ainda não executada

| Área | Método e condição observável |
|---|---|
| Comparabilidade | Mesma seed63/nó1, sala/roster, arma, posições e agenda de ações; registrar transform por frame de captura, resolução/qualidade/backend/hardware/versão; A/B alternados |
| Geometria | Mesma contagem/bounds/flags/layers de colliders e NavMesh;0 novos obstáculos,0passagens visuais falsas; overlay físico além da imagem limpa |
| Navegação | Paths completos pelas duas laterais, player e Fragmento realmente percorrem ambos; mesmo destino válido antes/depois |
| LOS/tiro | Emissor bloqueado atrás da mesma cobertura e readquire após contorno; nenhum projétil atravessa por alpha; mesma autoridade nativa |
| Dash e contato | Dash contra cada face/canto, sem penetração/teleporte; básicos e especiaisGauntlet/Spear/Bow preservam resultados |
| Telegraph/VFX | Windup/ataque/recovery visíveis em todos os pontos;0ocultações que impeçam reagir; observar vento/fogo/eletricidade da tripla sem mudar efeitos |
| Profundidade/fader | Cruzar frente/trás dos dois objetos e probes;0margens/retângulos visíveis no fade, restore completo dos materiais e sombras; ameaças legíveis |
| Câmera | Percurso follow atual e resoluções1160×600,1280×800,1920×1080; faixa adicional exploratória registrada separadamente |
| Texturas | Bordas/cantos/mips/compressão sem halo/cintilação que afete leitura; topo/lados contínuos, nenhum pé flutuante |
| CPU/GPU | Warmup, blocos A/B/BA iguais repetidos, mediana/p95 e amostras brutas; cena real,8/32/64 somente se relevante; Editor separado de release |
| Memória/batches | Comparar texturas residentes, material instances, drawcalls/shadowpass e overdraw no FrameDebugger; registrar indisponibilidades |
| Autoria | Cronometrar pintura/UV/import/revisão/correção por peça, comparar iterações e aceitar custo conscientemente |
| Integridade | Hash/prefs atuais antes/depois, sem restauração antiga; sair dePlay e voltar à cena/seleção de entrada |

Sem meta universal de FPS ou ganho percentual inventado. Resultados devem ser aceitáveis no orçamento acordado a partir do baseline e variabilidade medida; piora precisa de decisão consciente em troca de autoria/arte. Fachada e cobertura podem receber decisões diferentes. Implementação das duas peças exige escolha explícita do desenvolvedor; publicação global exige nova autorização depois do protótipo.
