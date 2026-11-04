"""Build the offline visual review and PT-BR handoff; no Unity assets are authored."""
from pathlib import Path
import json,hashlib,struct,html,re,zipfile,shutil
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/validation/artifacts/run-exp-07'
REVIEW=OUT/'review'
DEC=ROOT/'Docs/decisions'
DATE='03/11/2026 — America/Sao_Paulo'
reports={
'RUN_EXP_07_0_BASELINE.md':'''# RUN-EXP-07.0 — baseline real e fronteiras de evidência

03/11/2026. **DONE:** auditoria, capturas Unity e verificação focada do ambiente existente. **NOT DONE:** implementação 2.5D. Este relatório não aprova uma direção artística.

## Projeto correto e segurança

O chat iniciou no worktree C:/Users/gabri/.codex/worktrees/09c7/Tech-Guy, HEAD 67d8065a778c3dd91151718abb4dd3f9ca6e73f9, detached e limpo. Ele contém uma versão antiga, Unity 6000.3.3f1/URP 17.3.0, sem Docs nem .agents. Não serve de baseline para os marcos atuais.

Unity MCP confirmou uma única instância Tech-Guy@fccafb2b47cd7671 em D:/Dev/Personal/My_games/Tech-Guy, Unity 6000.5.10f1. Esse checkout é a fonte e destino desta entrega: branch gameplayOverhaul, HEAD 766afa66d93a58afc1b9de1f2eafa15ed8a5504b. Entrada: NexusBlockout limpa, Edit Mode, sem compilação pendente. Havia modificação em ProjectSettings/ProjectAuditorSettings.asset; nenhuma restauração Git foi executada. O estado posterior desse arquivo é conferido nos hashes, sem presumir autoria de mudanças do Editor.

Antes de testar: 2368 arquivos protegidos de Assets/_Project, ProjectSettings, Packages e SourceArt; 47 preferências TechGuy nos dois escopos de registro. Somente hashes, sem exportação de valores privados. Script Tools/RunExp07/safety_snapshot.py. Não se restaura snapshot antigo de save. Fontes de arte, cenas, prefabs, combate, boons, C05, input e economia são protegidos. Sem commit/push.

## Fontes lidas e aplicação

AGENTS raiz, README atual, skills locais e referências; Docs/00_VISAO, art/01_DIRECAO_VISUAL e 02_PIPELINE_DE_ASSETS; 04_ARQUITETURA_E_INTEGRIDADE; sistemas de inimigos/HUD; seções vigentes do GPR_TECHNICAL_DESIGN, masterplan, ledger e matriz; fluxo canônico de captura; relatórios 05.0/05 consolidado e 06.0/06.3/06 consolidado; MAP_ART_2_5D_FUTURE_PLAN, RUN_EXP_06_5_MULTIVERSE_2_5D_DIRECTION e brief07 completos. Não foram encontradas instruções AGENTS aninhadas em Docs/.agents. Relatórios históricos não certificam a revisão atual.

## Owners encontrados na fonte e assets

| Sistema | Responsável real / evidência |
|---|---|
| Materialização e navegação | ManualStageEnvironment.BuildGenerated, NavMeshData e NavMeshModifier NotWalkable nas coberturas, antes do bake |
| Layout e composição | ManualRunRandomizer, FirstSectorEncounterLayout e FirstSectorContentDefinition; não foram alterados |
| Apresentação | FirstSectorRoomPresentation cria trilhas e bevel; FirstSectorEnvironmentArt troca mesh/material e combina racks visuais |
| Física | BoxCollider original de cada cobertura: 4,2 × 2,2 × 4,2 m. Centros locais (-3;1,1;-1,5) e (3;1,1;1,5) |
| LOS / tiro | EnemyAI e EnemyCombatActions nativos; fixture VerifyEmitterCover observa bloqueio e retomada de impactos |
| Câmera | TG_TopDown_Camera.HandleCamera e CameraOcclusionFader; follow em Start/Update, oclusão em LateUpdate/unscaledDeltaTime |
| UI | RunRewardUI e UI/Rewards; C05 permanece produtiva, sem edição |

FirstSectorEnvironmentArt mantém paredes visuais recuadas e racks combinados num sibling visual. A raiz combinada de racks não é automaticamente resolvida pelo collider da parede. A integração futura deve verificar essa associação por peça; não descombinar todas as paredes nesta etapa.

## Renderização Unity inspecionada

URP 17.5.0 (manifest atual); UniversalRenderPipelineAsset ativo, espaço Linear, qualidade Ultra. Renderer UniversalRenderPipelineAsset_Renderer: zero renderer features via recurso MCP. Asset: HDR ligado, render scale 1, MSAA 1, depth/opaque texture desligados; sombras da luz principal, mapa 2048, quatro cascatas, distância100, soft shadows; sombras de luzes adicionais desligadas. São configurações encontradas, não medições de GPU.

Inspeção aditiva temporária da FirstSector em Edit Mode, fechada sem salvar: luz Directional branca, intensidade1,6, sombras Soft, Euler(52;328;0). Câmera perspectiva FOV40, near0,2/far500. Transform serializado (0;10;-18), Euler(44,7121;0;0), **não confundir com follow em runtime**. Configuração follow16m/16m/ângulo0. Sem binding normal de órbita/zoom encontrado. Variações futuras não estão proibidas nem certificadas.

MCP confirmou URP/Lit em Ceramica grafite cobre (atlas BaseColor/Normal/MetallicSmoothness), Piso de arquivo (ArchiveFloor) e Estrutura de arquivo. Shader.Find confirmou Lit, Simple Lit e Unlit disponíveis; nenhum material novo foi criado. Meshes/fontes05 e prefabs permanecem originais.

## Capturas reais e condições

Teste existente Archive05ComparisonPreservesCoverRoutesLosMovementAndAbilities via Unity Test Framework/MCP, job0c4eed4e5e0042fba8845412ced7466e: **1/1 Passed**, 17,5907712s de seleção, sem skipped/falhas. Uma repetição para tentar coletar câmera: jobbb7e725898674b47a8a286c54feff5e6, **1/1 Passed**,14,8307034s. São duas execuções do mesmo caso, não dois testes distintos. O sampler temporário de Editor não sobreviveu ao reload; nenhum arquivo de telemetria de câmera foi produzido. Reconstrução posterior do planejador executada via CodeDom; tentativa Roslyn falhou por dependência MetadataReader, preservada como falha de ferramenta.

As imagens utilizadas são da primeira execução: pasta original run-exp-03/firstsector/20261103-214151-6130699, copiada integralmente para run-exp-07/review/baseline. Nomes05 pertencem ao helper legado; data, job e execução são atuais07. Segunda execução original:20261103-214738-2761800, preservada. ScreenCapture real,1160×599 pixels; não1920×1080. O helper congelou timeScale0 nas vistas gerais e comparou apresentação04/05; as referências principais07 usam **after**, arte05 produtiva.

| Referência | Arquivo no pacote | Condição |
|---|---|---|
| Arena coberta / base comum A/B/C | baseline/05-room-cover-after-normal-camera.png | Seed63,nó1, jogador no centro; mesma imagem de entrada para os três conceitos |
| Arena aberta | baseline/05-room-open-after-normal-camera.png | Seed0,nó1; referência separada, não A/B de geometria idêntica |
| Jogador junto à cobertura | baseline/05-room-player-circulating.png | Movimento nativo pela rota lateral |
| Fragmento / telegraph | baseline/natural-Fragmento-windup-node-1.png | Fase real, aviso vermelho ao lado do jogador |
| Emissor normal / LOS | baseline/natural-Emissor-idle-node-1.png e emissor-blocked/reacquired | Roster real; fixture posicional explícita |

Fixture: jogador imune, warps para entrada/LOS, AI/nav/dano inimigo ativos, remoção de CoinDrop em memória; preferência de arma/moeda/resumo e SessionState restaurados pelos scopes existentes. Não é gameplay humano sem assistência. Passou igualdade de contagem/bounds dos colliders entre apresentações, dois paths completos, raio de cobertura, movimento real e bloqueio/retomada de Emissor. Não prova método2.5D, pois nenhum foi implementado.

## Escala e câmera reconstruídas, não amostradas no PNG

Executado no Unity o planejador puro com asset FirstSectorManual e seed:0→centro(500;0;38), arena29×19m;63→centro(500;0;43), arena28×24m. Fonte do fixture posiciona jogador no centro antes da captura. Pela fórmula atual do follow: seed63→câmera(500;16;27), Euler(45;0;0); seed0→(500;16;22). São **reconstruções de configuração/código**, não transform medido no mesmo frame. O CSV poses-node-1 registra posições reais dos inimigos após a vista geral, não daquele frame exato. Essa limitação deve ser corrigida no manifesto do futuro A/B implementado.

Faixa observada: follow atual em sala coberta/aberta, FOV40; não há intervalo de zoom certificado. QA futura deve testar center/bordas e frente/trás das duas peças antes de aceitar uma faixa. Fonte de escala humana ~1,8m nas pranchas é referência ilustrativa, não medição do rig.

**NOT TESTED:** desempenho2.5D, outras resoluções nesta rodada, suporte de novas texturas/fader, gameplay dos conceitos. Console inicial0erros/aviso de Input Manager; final sem erros de gameplay observados e warnings existentes de keys/AbilityHolder. Não corrigir tuning/bindings para silenciar warning fora do escopo.
''',
'RUN_EXP_07_1_MULTIVERSE_ART_DIRECTION.md':'''# RUN-EXP-07.1 — três linguagens originais de universo

**DONE:** propostas e seis artes originais. **PENDING DEVELOPER APPROVAL:** linguagem escolhida. Nomes são exploratórios; não estabelecem lore, ordem de campanha ou universos jogáveis.

## Invariantes entre universos

Protagonista/arma mantêm identidade, silhueta e escala. Fragmento quadrúpede baixo e Emissor de conchas abertas continuam com as mesmas funções, modelos e comportamentos. O chão caminhável é contínuo e discreto; colisão visual corresponde à física. Perigo usa sinais nativos de forma, posição, cor e tempo, acima do ornamento. Vida, mana, Asura, recursos, prompts remapeáveis, layout do HUD e C05 mantêm significado e apresentação produtivos.

Hierarquia: jogador/alvos e telegraphs → recursos/HUD → VFX de habilidade → decoração. Não usar vermelho de ameaça, círculos de alvo, chevrons de ataque ou pulsação rápida como detalhe de parede. Contraste em cinza e forma complementam a cor. Ilustração não pode apagar um telegraph, pintar passagem bloqueada nem fazer cobertura sólida parecer atravessável. Nenhuma regra física nova é proposta.

## A — Arquivo Digital Fragmentado

Tese visual: registros empilhados tentam conservar índices enquanto a margem se rompe. Estratos retangulares, diagonal curta de quebra, cerâmica laminada com costuras fortes e cobre estrutural; grafite faz fundo e marfim define quinas. Detalhe e pincel seco ficam nas faces verticais/periferia. Piso amplo azul-carvão, sem circuito luminoso sob os pés. Luz direcional material, highlights pintados amplos e sombra de contato real futura. Profundidade por volumes simples e poucos estratos periféricos.

Paleta funcional proposta: chão #29353D; estrutura #20272D; cerâmica #C9BCA1; cobre #A1683E; acento de indexação #548D8D. Cobre contido para não competir com fogo existente. Tensão espacial vem da interrupção de LOS nas duas massas físicas já presentes; não acrescentar obstáculo. O mundo continua reconhecível sem cores por lombadas/estratos e costuras quebradas. Liga-se visualmente ao tema de informação incompleta e à busca pela Terra sem inventar pista narrativa canônica.

Som/movimento futuro somente como intenção: relés espaçados e acomodação lenta de estratos longe do combate. Autoria por atlas de bordas e faces pintadas sobre volume; reaproveitamento alto de família. Esforço **baixo a médio**, comparativamente às outras opções, sem orçamento fechado.

## B — Metrópole Gráfica Ilustrada

Tese visual: serviços e anúncios organizam uma cidade por planos de informação. Fachadas verticais, pilastras, cornijas angulares e skyline em degraus; desenho de tinta com planos chapados de dois/três valores. Hachura só em sombra vertical; retícula grossa restrita ao skyline/periferia. Piso petrol quieto. Relação de cartaz entre papel ocre, creme e tinta escura, sem néon obrigatório.

Paleta: piso/paredes frias #243B4A; papel #D5B66F; luz #E9DCC0; contorno #19212A; acento #44989A. A escala do traço precisa sobreviver ao minificado. As massas de cobertura viram quiosques técnicos fechados, sem portas que prometam entrada. Rotas ficam iguais; a metáfora de avenida/travessa orienta composição futura, sem redesenhar esta sala. Skyline, planos de fachada e recortes explicam distância, não filtro de pós-processamento.

Em cinza reconhece-se o ritmo urbano/contornos. Som futuro candidato: transformadores e anúncios abstratos espaçados, sem semântica interativa falsa. Effort **médio**: ilustração/recortes originais, ajustes de espessura de linha e integração com personagens tridimensionais. Risco de pôster parecer interação e de contorno do ambiente competir com o do player. Não copiar layouts, quadros, personagens ou motivos de Spider-Verse.

## C — Realidade Corrompida Pictórica

Tese visual: camadas de matéria pintada deixam aparecer versões apagadas. Estratos minerais curvos, contornos internos orgânicos e lacunas digitais retangulares rasas. A geometria física da cobertura permanece retangular; o orgânico está no relevo interno/pintura, não em raízes bloqueando circulação. Gouache, espátula e pincel seco produzem transições de cor e bordas perdidas dentro da superfície; a borda de colisão permanece definida.

Paleta: chão #344553; lilás mineral #776B83; giz #C9C7B4; sálvia #7B8D80; mauve desaturado de fundo. Piso mais silencioso que parede; nenhum buraco pintado no chão. Atmosfera em camadas distantes, sem névoa sobre ameaças. Luz ambiente pictórica ampla e contato dinâmico futuro evitam sombras duplicadas. Em cinza, curvas/impasto se distinguem de racks e cidade gráfica.

Som/movimento proposto: fricção mineral discreta; nenhuma ondulação de collider ou distorção global. Autoria **média a alta**: coerência de pinceladas entre topo/lados, mips e faixas de câmera. Pode contrastar com o player cerâmico sem refazê-lo; contraste de valor precisa proteger seu corpo claro. Não introduzir nova mecânica local nesta exploração.

## Comparação além da paleta

| Eixo | A | B | C |
|---|---|---|---|
| Silhueta periférica | Lombadas/estratos quebrados | Skyline/cornijas escalonadas | Folds minerais curvos |
| Arquitetura | Depósitos e encaixes | Pilastras/quiosques e vãos rasos | Sedimentos comprimidos |
| Superfície | Facetas pintadas/cerâmica | Tinta/chapado/retícula controlada | Impasto/pincelada tátil |
| Luz | Material e direção | Planos gráficos de valor | Atmosfera difusa mineral |
| Profundidade | Camadas estreitas/volumes | Cards de skyline + volumes próximos | Planos atmosféricos + faces pintadas |
| Produção | Reuso05 e atlas | Mais desenho e ajuste de linha | Pintura contínua e mais revisão |
| Custo provável | Menos transparência necessária | Risco de cards/overdraw | Texturas grandes/névoa se excessiva |
| Compatibilidade player | Maior continuidade | Contraste intencional 2D/3D | Player como figura nítida sobre fundo pintado |
| Extensibilidade | Família modular de arquivo | Quarteirões de linguagem urbana | Variações de pigmento/estratos |

São avaliações de design e hipóteses de produção, não benchmarks. A/B/C mantêm topologia nesta comparação; diferenças futuras de percurso/mecânica de uma ficha de mundo não estão autorizadas. A recomendada para primeiro protótipo é **A**; B é a alternativa mais gráfica, C a mais pictórica. Aprovação07.1 não é conversão de FirstSector.
''',
'RUN_EXP_07_2_VISUAL_CONCEPTS.md':'''# RUN-EXP-07.2 — artes comparáveis da mesma arena

**DONE:** três overviews e três pranchas originais raster, comparações individuais com baseline e página offline. **PENDING DEVELOPER APPROVAL:** escolha artística. **NOT DONE:** asset Unity pronto, UV final ou protótipo jogável.

## Artefatos e método

Todos os conceitos foram gerados pelo ImageGen integrado, nesta conversa, sem CLI/API externa. Mesmo PNG real seed63 usado como entrada das três vistas; cada prancha usa sua vista como referência. Prompts exatos em review/prompts; hashes/dimensões/paths em asset-manifest.json. Sem seed/modelo exato retornados pelo sistema, não inventá-los. Originais copiados sem transformação raster nem remoção de metadados. Nenhuma imagem foi produzida no Blender, cuja conexão MCP falhou.

| Direção | Vista completa | Prancha detalhada | Método futuro sugerido |
|---|---|---|---|
| A | images/A-overview.png | images/A-sheet.png | Fachada fixa recortada, cobertura em volume de faces pintadas |
| B | images/B-overview.png | images/B-sheet.png | Fachada/skyline em poucas camadas, quiosque em volume gráfico |
| C | images/C-overview.png | images/C-sheet.png | Fachada com plano pintado, cobertura com pintura contínua em volume |

Cada prancha mostra close-up de cobertura, fachada, linguagem de superfície, paleta e estudos de topo/frente/lado. São referências de autoria: imagens completas incluem luz, perspectiva, títulos e composição; **não são texturas prontas nem atlas/UV certificados**. O gerador não forneceu camadas editáveis/PSD/blend; raster PNG é o original disponível. Prompts/manifest/builder são editáveis; não equivalem a fonte pictórica em camadas. Extração/redesenho/UV e validação só após direção aprovada.

## Controle e deriva observada

As três mantêm duas coberturas na diagonal, corredor central/lateral, jogador entre elas, inimigos de referência e fundo periférico. A e C seguem mais de perto o enquadramento; B desloca parede e alguns limites na imagem. O modelo reinterpretou pixels dos personagens/HUD, texto e acessórios. Não usar essas alterações como proposta de redesign. HUD e inimigos de produção continuam exatamente os existentes.

As dimensões de output ficam no manifesto; proporção aproximadamente igual ao baseline, sem alegar registro pixel a pixel. Não há transform/collider no conceito. A comparação tem composição controlada por referência, **não é um teste A/B implementado**. Em C a porta pintada arredondou o topo e a faixa cobre pode parecer salmão: não promover esse ornamento a navegação/ameaça. Nas pranchas, desenhos de recortes e cotas são ilustrativos; a geometria nativa governa o futuro asset. B precisa devolver cornijas para dentro do envelope e evitar aparência de porta no quiosque antes de integrar.

## Leitura de gameplay — análise explícita por candidato

| Check | A | B | C |
|---|---|---|---|
| Jogador | Claro contra chão grafite; não clarear topo ao mesmo valor | Branco contra petrol; linha de contorno do cenário deve ser mais fina | Claro contra ardósia; fundos giz atrás dele exigem fader |
| Fragmento | Baixo/quadrúpede, contraste no piso; evitar marfim no chão | Silhueta baixa reconhecível, contorno de parede não deve dominar membros | Marfim distinto de piso; reduzir pincelada sob pés/mandíbula |
| Emissor | Teal e duas conchas destacam; não repetir núcleo luminoso em cover | Perfil de conchas distingue de quiosque retangular; sinalização teal discreta | Conchas nítidas contra pintura; mauve não deve contaminar o teal |
| LOS ranged | Costura não equivale a fenda atravessável; volume é sólido | Shutter é placa fechada; cornija não pode sugerir tiro sob ela | Depressão pintada é rasa, nunca túnel; mesma massa bloqueia tiro |
| Rotas | Piso amplo, duas laterais e gap preservados | Planos largos guiam; corrigir deriva de enquadramento antes do teste | Chão contínuo, sem fendas/raízes nem relevo de rota |
| Silhueta física | Bevel só para dentro de4,2×2,2×4,2m | Cornijas/quiosque devem caber no mesmo volume | Curvas internas; bordas externas do obstáculo continuam inequívocas |
| Telegraph vermelho | Bom contraste no chão frio; proibir cobre saturado no piso | Não usar pôster vermelho nem hachura sob sinal | Não usar corrupção vermelha no playfield; reduzir ruído de pincel |
| VFX do player | Cobre dos props discreto para fogo/Titanic existente | Ocre não deve encobrir fogo, tinta não deve simular eletricidade | Evitar névoa clara sobre vento/fogo; manter espaço negativo |
| Oclusão frontal | MeshRenderer sob collider para fade; racks precisam associação local | Marquise/cartão pode cortar personagem; sem decoração sobre corredor | Plano de fundo e volume próximos; névoa nunca cobre ameaças |
| Sensibilidade câmera | Volume pintado tolera mais follow que cartão único | Perspectiva desenhada/linha varia no deslocamento, risco maior | Pincel e luz pintada denunciam mudança de ângulo; testar bordas |

Baseline real contém windup/ataque de Fragmento e LOS de Emissor; os overviews gerados são momentos de leitura, não cenas de stress de VFX. As células acima são análise/proibições, **NOT TESTED em gameplay2.5D**. Não há Burn/DoT novo: preservar fogo da tripla Titanic Fists + Conductor + Detonation exatamente como está.

## Recomendação para uma fachada + uma cobertura

**A** tem maior continuidade com FirstSector05, menor dependência de traço global e permite separar textura expressiva de volume físico confiável. A mudança é localizada e reaproveita família/material. Custo de arte relativo baixo/médio: pintura de faces, união de quinas, um recorte de fachada e correções de câmera; estimativa de planejamento **2–4 dias úteis de artista técnico** após aceite, sem compromisso e sem incluir retrabalho de decisão. B:3–5 dias; C:4–6; validar cronômetro de autoria em07.5. São hipóteses, não horas medidas de pintura manual da geração atual.

Maior risco A: estratos finos cintilarem e fachada perder espessura na borda. Fallback: plano fixo com laterais reais ou manter rack3D, preservando pintura. B e C continuam candidatas; A não é aprovada por ser recomendada. Nenhum vencedor de FPS pode ser escolhido sem comparação real.
''',
'RUN_EXP_07_3_TECHNICAL_PROPOSAL.md':'''# RUN-EXP-07.3 — proposta técnica de duas peças, sem implementação

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
''',
'RUN_EXP_07_RELATORIO.md':'''# RUN-EXP-07 — pacote de revisão visual até07.4

03/11/2026. **DONE:** auditoria da base correta, três direções, seis artes originais, proposta de fachada/cobertura e pacote HTML offline. **PENDING DEVELOPER APPROVAL:** escolha artística. **NOT DONE:**07.5/07.6, protótipo Unity e conversão de FirstSector. **NOT TESTED:** desempenho/leitura dos conceitos em movimento. A entrega distingue arte gerada, capturaUnity e proposta técnica.

## Abrir e decidir

[Revisão offline](../validation/artifacts/run-exp-07/review/index.html) e [ZIP portátil](../validation/artifacts/run-exp-07/RUN_EXP_07_REVIEW.zip). As imagens e relatórios estão incluídos; o HTML não usa rede, bibliotecas externas nem fontes remotas. Comparação individual e conjunto A/B/C, opção visual em cinza, pranchas de faces/materiais, baseline coberta/aberta, telegraph e LOS. Os PNGs completos são os originais raster do ImageGen. Não existem PSD/blend/fontes em camadas fornecidos pelo gerador.

**Recomendação: A — Arquivo Digital Fragmentado**, para reduzir variáveis na primeira comparação técnica e aproveitar continuidade com FirstSector05. B evidencia contraste gráfico urbano; C evidencia linguagem pictórica mineral. A escolha não apaga as alternativas nem estabelece lore. A recomendação não concede aprovação.

## Skills efetivamente aplicadas

| Skill lida | Técnica/regra aplicada | Decisão resultante |
|---|---|---|
| router | Detectar engine/versão real antes de roteiro | Recusar baseline do worktree antigo; usar Unity6000.5/URP17.5 conectados |
| tech-guy-game-vision + vision-and-constraints | Distinguir intenção, implementado, verificado e aceito | Mundos são propostas; preservar Nexus, personagem e armas |
| tech-guy-combat-unity + combat-matrix | Autoridades de dano/controle/LOS já existentes | Nenhuma mudança de combate, boons, input ou Burn |
| tech-guy-unity-acceptance + editor-validation | MCP, filtro proporcional, evidência atual e restauração | Teste existente focado, scopes atuais e hashes/prefs, sem verde global |
| unity-mcp-orchestrator | Resource-first, instância/câmera/pipeline antes de ação | Confirmar raizD:, cena limpa, renderer sem features, leitura aditiva sem salvar |
| unity-csharp-scripting | Versão fixada/propriedade serializada/lifecycle | Proposta usa owners existentes; nenhuma classe runtime nova |
| create-game-assets + art-direction/raster/three-d/provenance | Forma/valor/material, câmera comum, alvo em escala e fonte rastreada |6 imagens com prompts/hashes; piso quieto, pranchas não sãoUV/fontes fictícias |
| imagegen | Ferramenta integrada para gerar/editar, fontes copiadas ao projeto |6 chamadas reais; referências de baseline e família; sem fallbackCLI |
| tech-guy-world-identity + world-card | Tese e contraste sem depender de cor; invariantes |A estratos, B contorno/cidade, C pincel/curvas; sem mecânica/lore extra |
| camera-systems | Follow/depth/oclusão pertencem à câmera | Propor faixa e testar extremos sem billboarding da cobertura |
| level-design | Métricas/circulação antes de dressing | Duas massas e mesmas rotas, sem novo layout ou obstáculos |
| performance-optimization | Medir antes de otimizar, separar Editor/build | Especificar A/B e custos de textura/overdraw; nenhum ganhoFPS alegado |

Skills locais encontradas: quatro em .agents, incluindo boon-design, cujo SKILL.md não foi aplicado porque o catálogo está fora do escopo; seu mapa foi identificado na descoberta. Não há skill local separada de renderURP, pintura2.5D ou Blender. As skills instaladas oferecem câmera, assets, shaders e desempenho; shader-programming foi descoberto no catálogo mas não aplicado porque não há autoria de shader. Fontes técnicas oficiais e shader/assets reais sustentam a proposta, sem instalar skills/ferramentas adicionais. Não foi delegada tarefa a subagentes.

## Auditoria e estado atual

Fonte/entregaD:/Dev/Personal/My_games/Tech-Guy, gameplayOverhaul,HEAD766afa66. Worktree do chat09c7 ficou intacto, por ser anterior aos marcos aprovados. Cena inicial/finalNexusBlockout limpa/Edit. UnityMCP6000.5.10f1 eURP17.5 reais. Presentations atuais já separam arte/física. Fader suporta MeshRenderer e tem limites de hierarquia/alpha; racks combinados exigem associação local. C05, Fragmento eEmissor preservados. BlenderMCP **BLOCKED** por conexão recusada; consequência: nenhum .blend/modelo/UV novo, sem impedir conceitos raster. ImageGen integrado executou as seis artes. Ferramenta execute_code/Roslyn falhou numa leitura de reconstrução por MetadataReader; CodeDom executou a consulta equivalente. Nenhum erro de script de produção foi corrigido ou introduzido por essa leitura.

## Verificações realizadas e limites

Unity UTF: uma seleção focada executada duas vezes,1/1Passed cada; segunda tentou coletar metadata de câmera por callback, perdido no reload. Nenhuma declaração de telemetria não produzida. Reconstituição explícita das seeds emUnity registrada separadamente. Capturas atuais deGameView1160×599 com roster/telegraph/cover/LOS. O fixture tem imunidade/warps e restaura prefs correntes, não reproduz sobrevivência humana. Console inicial avisoInputManager; execução tem warningsAbilityHolder, sem alteração de binding. Não rodou suite global, benchmark2.5D, build novo, playtest humano ou conversão. Validação do pacote e hashes no manifest/QA/integridade.

## Riscos e dependências

Arte raster reinterpreta detalhes do HUD/personagens e enquadramentoB: são limitações de conceito, não mudança aprovada. A prancheta pode sugerir recorte que precisa ser contido no envelope físico. Antes deUnity, redesenhar mapas de face sem títulos/sombras duplicadas, acertarUV/cantos/pivot e validar noMaterialInspector. Custo artístico estimado não é medição. Alpha/fade pode revelar retângulos; plano perde volume sob follow; texturas/materiais/overdraw podem aumentar custo. Conservar3D como fallback por peça.

## Arquivos produzidos e modificados

Produzidos: RUN_EXP_07_0_BASELINE.md,07_1_MULTIVERSE_ART_DIRECTION.md,07_2_VISUAL_CONCEPTS.md,07_3_TECHNICAL_PROPOSAL.md, este relatório e07_SKILL_APPLICATION.md; artefatos emDocs/validation/artifacts/run-exp-07; Tools/RunExp07/safety_snapshot.py ebuild_review.py. Novas evidências dos helpers permanecem nos dois diretóriosrun-exp-03/firstsector datados214151/214738. Manifesto enumera imagens/prompts/fontes e hashes; inventário final lista o pacote.

Modificados intencionalmente somente três documentos mestres: TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md, GAMEPLAY_REWORK_PROGRESS.md e01_MATRIZ_DE_VALIDACAO.md, com bloco07 acima do histórico. Nenhum arquivoUnity de produção/art source foi editado pelo agente; comparação integral dos escopos protegidos acompanha esta afirmação. Se houver divergência de configuração doEditor no relatório de integridade, ela deve ser exposta e não revertida automaticamente.

## Gates e parada

| Gate | Status / limite |
|---|---|
| A skills/projeto | DONE com metadata de câmera reconstruída explicitamente, não amostrada no mesmoPNG |
| B diferenciação | DONE como demonstração artística; reconhecimento humano pendente |
| C leitura | DONE como análise visual por candidato; NOT TESTED em2.5D jogável |
| D artes/proveniência | DONE,6PNG originais/prompts; fonte emcamadas não disponível |
| E técnica | DONE como proposta de duas peças, NOT TESTED implementação |
| F integridade | Conferir evidence/integrity-comparison.json; nenhum commit/push ou restauração de save |
| G aprovação | PENDING DEVELOPER APPROVAL |

Decisões para o desenvolvedor: escolherA/B/C ou pedir revisão objetiva; aceitar a linguagem das duas peças ou indicar ajustes; autorizar expressamente07.5 somente após escolher imagens/recorte. Aprovação da imagem não é aceite da integração; implementação local não autoriza conversão global. **STOP após07.4.**
'''}

def esc(t):return html.escape(t,quote=True)
def md(t):
    # Small offline renderer for the report subset, preserving tables and links.
    def inline(s):
        s=esc(s)
        s=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',r'<a href="\2">\1</a>',s)
        return re.sub(r'\*\*(.+?)\*\*',r'<strong>\1</strong>',s)
    result=[];table=False
    for line in t.splitlines():
        if line.startswith('|'):
            if re.match(r'^\|[\s:|\-]+\|$',line):continue
            if not table:result.append('<div class="table-scroll"><table>');table=True
            result.append('<tr>'+''.join('<td>'+inline(x.strip())+'</td>' for x in line.strip('|').split('|'))+'</tr>');continue
        if table:result.append('</table></div>');table=False
        if not line.strip():continue
        if line.startswith('#'):
            level=min(4,len(line)-len(line.lstrip('#')));result.append(f'<h{level}>'+inline(line.lstrip('# ').strip())+f'</h{level}>')
        else:result.append('<p>'+inline(line)+'</p>')
    if table:result.append('</table></div>')
    return '\n'.join(result)

CSS='''*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#10191f;color:#e6e5dd;font:16px/1.65 system-ui,sans-serif}a{color:#acd9d1}main{max-width:1500px;margin:auto;padding:32px}header{padding:38px 0 24px;border-bottom:1px solid #40515b}h1{font-size:clamp(28px,4vw,60px);line-height:1.1;max-width:1100px}h2{font-size:32px;margin-top:56px}h3{font-size:23px}p{max-width:1050px}nav{display:flex;flex-wrap:wrap;gap:18px;margin:24px 0}nav a{font-weight:600;text-decoration:none}figure{margin:0}img{display:block;width:100%;height:auto;background:#19252d}figcaption{color:#b8c8cb;font-size:14px;margin:8px 0 22px}button,select{font:inherit;background:#24343e;color:#fff;border:1px solid #708792;padding:8px 14px;border-radius:5px;cursor:pointer}button[aria-pressed=true]{background:#5b746a}button:focus-visible,a:focus-visible,select:focus-visible{outline:3px solid #f0d283;outline-offset:4px}.controls{display:flex;gap:14px;align-items:center;flex-wrap:wrap;margin:20px 0}.pair,.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:20px}.three{grid-template-columns:repeat(3,minmax(0,1fr))}.card,.note{background:#1b2931;padding:22px;border:1px solid #3a4e58;border-radius:8px}.note{border-left:4px solid #d8b46b}.badge{display:inline-block;font-size:12px;letter-spacing:.09em;font-weight:800;padding:4px 10px;background:#42594f;color:#fff}.pending{background:#725f35}.swatches{display:flex;gap:10px;flex-wrap:wrap}.swatch{height:58px;width:130px;padding:36px 4px 0;font:12px/1.5 ui-monospace;color:#f8f4e8;border:1px solid #718089;margin-bottom:20px}table{border-collapse:collapse;width:100%;font-size:15px}td{padding:12px;border:1px solid #465960;vertical-align:top}tr:first-child{font-weight:700;background:#263a44}.table-scroll{overflow:auto;margin:20px 0}details{background:#18262d;padding:20px;margin:16px 0}summary{cursor:pointer;font-weight:700}.gray .art{filter:grayscale(1)}.comparison img{aspect-ratio:1160/599;object-fit:contain}.muted{color:#b1c2c9}footer{margin-top:48px;padding-top:24px;border-top:1px solid #40515b}@media(max-width:850px){main{padding:18px}.pair,.three{grid-template-columns:1fr}h2{font-size:27px}}@media print{nav,.controls{display:none}body{background:white;color:black}details{break-inside:avoid}img{break-inside:avoid}}'''

def figure(path,caption):return f'<figure><a href="{path}"><img class="art" loading="lazy" src="{path}" alt="{esc(caption)}"></a><figcaption>{esc(caption)} · clique para export completo</figcaption></figure>'

def build():
    (REVIEW/'reports').mkdir(parents=True,exist_ok=True)
    integrity='''
## Integridade final e QA do pacote

**DONE:**47/47 preferências TechGuy existentes idênticas,0adicionadas/removidas.2367/2368 arquivos protegidos idênticos;0adicionados/removidos nesses escopos. Única divergência: ProjectSettings/ProjectAuditorSettings.asset, modificado já na entrada e diferente no hash final; ao final coincide comHEAD. O agente não editou/restaurou esse arquivo. Origem da transição não confirmada; não atribuir causalidade nem restaurar conteúdo antigo por suposição. Não se declara integridade byte a byte desse ajuste doEditor. Evidência:evidence/integrity-comparison.json e snapshots antes/depois.

Editor final NexusBlockout limpa/Edit, timeScale1, assistênciaTitanicConductorfalse, seedoverride vazio e scope de resumo encerrado. Sem commit/push. Capturas e geração são isoladas emDocs/Tools. Revisão das seis artes feita nas imagens retornadas peloImageGen; baseline cover/windup aberta porview_image.

**BLOCKED:** préviaHTML no navegador da ferramenta, por política que recusa file:. Nenhum contorno do bloqueio foi tentado. **NOT TESTED:** renderização visual e interação doHTML em navegador real. Verificação estática deHTML/caminhos, assinatura/dimensões/CRC dePNG, proveniência/hashes e integridade doZIP são independentes e constam empackage-qa.json. Essa limitação permanece explícita em vez de alegar inspeção visual deHTML não executada.
'''
    for key in ('RUN_EXP_07_0_BASELINE.md','RUN_EXP_07_RELATORIO.md'):
        if '## Integridade final e QA do pacote' not in reports[key]:reports[key]+=integrity
    for name,t in reports.items():
        (DEC/name).write_text(t,encoding='utf-8')
        portable=t.replace('../validation/artifacts/run-exp-07/review/index.html','../index.html').replace('[ZIP portátil](../validation/artifacts/run-exp-07/RUN_EXP_07_REVIEW.zip)','ZIP portátil na pasta da entrega original')
        (REVIEW/'reports'/name).write_text(portable,encoding='utf-8')
        (REVIEW/'reports'/name.replace('.md','.html')).write_text('<!doctype html><html lang="pt-BR"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+esc(name)+'</title><style>'+CSS+'</style><main><a href="../index.html">← Voltar à revisão</a>'+md(portable)+'</main></html>',encoding='utf-8')
    skill=reports['RUN_EXP_07_RELATORIO.md'].split('## Skills efetivamente aplicadas')[1].split('## Auditoria e estado atual')[0]
    (DEC/'RUN_EXP_07_SKILL_APPLICATION.md').write_text('# RUN-EXP-07 — aplicação de skills\n'+skill,encoding='utf-8')
    (REVIEW/'reports'/'RUN_EXP_07_SKILL_APPLICATION.md').write_text('# RUN-EXP-07 — aplicação de skills\n'+skill,encoding='utf-8')
    dirs=[('A','Arquivo Digital Fragmentado','Estratos, cerâmica e juntas estruturais. Maior continuidade com FirstSector05.',['#29353D','#20272D','#C9BCA1','#A1683E','#548D8D']),('B','Metrópole Gráfica Ilustrada','Contornos de tinta, fachadas urbanas e retícula nas bordas.',['#243B4A','#D5B66F','#E9DCC0','#19212A','#44989A']),('C','Realidade Corrompida Pictórica','Impasto mineral, pinceladas e curvas internas; física permanece retangular.',['#344553','#776B83','#C9C7B4','#7B8D80','#8C7581'])]
    baseline='baseline/05-room-cover-after-normal-camera.png'
    body='''<header><span class="badge pending">PENDING DEVELOPER APPROVAL</span><p class="muted">TECH GUY · RUN-EXP-07 · 03 NOV 2026</p><h1>Três linguagens.<br>A mesma arena como referência.</h1><p>Exploração visual original para futuros universos digitais e proposta de um teste híbrido 2.5D. Artes conceituais geradas; nenhum cenário convertido.</p></header><nav><a href="#comparar">Comparar</a><a href="#A">A · Arquivo</a><a href="#B">B · Metrópole</a><a href="#C">C · Pictórico</a><a href="#baseline">Capturas Unity</a><a href="#tecnica">Técnica</a><a href="#decisao">Decisão</a><a href="#relatorios">Relatórios</a></nav><aside class="note"><strong>Recomendação provisória: A.</strong> Preserva a continuidade visual e oferece um primeiro teste pequeno. A seleção pertence ao desenvolvedor. O recorte futuro continua uma fachada periférica + uma cobertura existente, mantendo o mundo físico3D.</aside><section id="comparar"><h2>Comparação lado a lado</h2><p>A mesma captura seed63 é a referência das três imagens. O gerador mantém a composição geral, mas reinterpreta pixels do HUD/personagens e desloca parte do enquadramentoB. Essas diferenças não são propostas de gameplay ou UI. Nenhuma imagem gerada é captura Unity.</p><div class="controls"><label for="direction">Direção: </label><select id="direction"><option value="A">A — Arquivo</option><option value="B">B — Metrópole</option><option value="C">C — Pictórico</option></select><button id="gray" aria-pressed="false">Ver em cinza</button></div><div class="pair comparison">'''
    body+=figure(baseline,'Unity baseline capture · FirstSector05 · seed63 ·1160×599')
    body+='<figure><a id="concept-link" href="images/A-overview.png"><img id="concept" class="art" src="images/A-overview.png" alt="Concept artwork — direção A, não implementada"></a><figcaption id="concept-caption">Concept artwork · A · não implementado · clique para export completo</figcaption></figure></div><h3>As três alternativas</h3><div class="grid three">'
    for k,n,s,p in dirs:body+=figure(f'images/{k}-overview.png',f'Concept artwork · {k} — {n}')
    body+='</div></section>'
    for k,n,s,palette in dirs:
        body+=f'<section id="{k}"><h2>{k} — {n}</h2><p>{s}</p>'
        body+='<div class="swatches">'+''.join(f'<div class="swatch" style="background:{c}">{c}</div>' for c in palette)+'</div>'
        body+=figure(f'images/{k}-sheet.png','Concept artwork · close-up, fachada, faces e linguagem de materiais · cotas ilustrativas')
        body+='<details><summary>Conceito contra a referência FirstSector</summary><div class="pair comparison">'+figure(baseline,'Unity baseline capture · física e composição de referência')+figure(f'images/{k}-overview.png',f'Concept artwork · direção{k}, não implementada')+'</div></details></section>'
    body+='''<section id="baseline"><h2>Evidência do jogo real</h2><p>Capturas novas desta tarefa pelo teste existente de ambiente; nomes05 pertencem ao helper histórico. Mesmo projeto atual, arte05 produtiva. Fixture com jogador imune e posições de LOS assistidas; AI e navegação nativas ativas. São evidências de baseline, sem 2.5D implementado.</p><div class="grid">'''
    for path,caption in [('05-room-open-after-normal-camera.png','Unity baseline capture · arena aberta seed0, referência separada'),('05-room-cover-after-normal-camera.png','Unity baseline capture · arena coberta seed63'),('05-room-player-circulating.png','Unity baseline capture · jogador perto da cobertura / circulação'),('natural-Fragmento-windup-node-1.png','Unity baseline capture · Fragmento / telegraph vermelho real'),('natural-Emissor-idle-node-1.png','Unity baseline capture · Emissor, escala normal'),('emissor-blocked-by-cover-node-1.png','Unity baseline capture · Emissor bloqueado por cover'),('emissor-reacquired-los-node-1.png','Unity baseline capture · LOS recuperada'),('natural-Fragmento-attack-node-1.png','Unity baseline capture · ataque de Fragmento')]:body+=figure('baseline/'+path,caption)
    body+='</div><details><summary>Câmera, escala e limites de metadata</summary>'+md(reports['RUN_EXP_07_0_BASELINE.md'].split('## Renderização Unity inspecionada')[1])+'</details></section>'
    body+='<section id="tecnica"><h2>Primeiro teste proposto: duas peças</h2><div class="note">Fachada: plano fixo ou malha recortada com poucas camadas. Cobertura: volume simples com faces pintadas. Collider, NavMesh, LOS e dash permanecem nativos. Toggle somente de revisão após escolha expressa.</div>'+md(reports['RUN_EXP_07_3_TECHNICAL_PROPOSAL.md'])+'</section>'
    body+='''<section id="decisao"><h2>Decisão artística pendente</h2><p>Escolha A, B ou C, ou indique os ajustes visuais necessários antes da escolha. As pranchas são propostas; aprovação de imagem não valida integração ou desempenho.</p><div class="card"><ol><li>Qual linguagem você escolhe para o primeiro protótipo?</li><li>A cobertura e a fachada dessa prancha representam a aparência desejada? Que ajuste é necessário?</li><li>Após escolher, você autoriza expressamente07.5 para somente essas duas peças, com revisão separada antes da conversão de produção?</li></ol></div><p><strong>STOP após07.4.</strong> Sem07.5/07.6, renderer novo, novos universos jogáveis, boons ou alteração de combate. C05 e o fogo existente da tripla permanecem protegidos.</p></section><section id="relatorios"><h2>Relatórios e fontes incluídos</h2><ul>'''
    for name in reports:body+=f'<li><a href="reports/{name.replace(".md",".html")}">{name}</a> · <a href="reports/{name}">Markdown</a></li>'
    body+='</ul><p><a href="asset-manifest.json">Manifesto de imagens, proveniência e hashes</a> · <a href="reports/RUN_EXP_07_SKILL_APPLICATION.md">Aplicação de skills</a></p><details><summary>Análise de leitura por candidato e limitações</summary>'+md(reports['RUN_EXP_07_2_VISUAL_CONCEPTS.md'])+'</details><details><summary>Integridade final e limite de inspeçãoHTML</summary>'+md(integrity)+'</details></section><footer>Pacote local independente · sem dependências externas para exibição · todas as imagens clicáveis em resolução original. Relatórios distinguem DONE, NOT DONE, NOT TESTED, BLOCKED e PENDING DEVELOPER APPROVAL.</footer>'
    script='''const select=document.querySelector('#direction');select.addEventListener('change',()=>{const k=select.value;document.querySelector('#concept').src='images/'+k+'-overview.png';document.querySelector('#concept').alt='Concept artwork — direção '+k+', não implementada';document.querySelector('#concept-link').href='images/'+k+'-overview.png';document.querySelector('#concept-caption').textContent='Concept artwork · '+k+' · não implementado · clique para export completo'});document.querySelector('#gray').addEventListener('click',e=>{const enabled=document.body.classList.toggle('gray');e.currentTarget.setAttribute('aria-pressed',enabled);e.currentTarget.textContent=enabled?'Ver em cores':'Ver em cinza'});'''
    (REVIEW/'index.html').write_text('<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Tech Guy — RUN-EXP-07 | Revisão visual</title><style>'+CSS+'</style></head><body><main>'+body+'</main><script>'+script+'</script></body></html>',encoding='utf-8')
    # Durable source provenance; no fake layer files and no fabricated model/seed.
    generated=['exec-1d6434d1-e973-4f21-ac9a-22a81d3381d6.png','exec-012e7fb1-6dbd-4f95-bbfd-6eeb24bc87cb.png','exec-e40b50d8-3d7f-4d78-a40d-7495cc5b0aca.png','exec-0fdcc886-f7a8-4df6-9073-27bba3bd2f53.png','exec-6c6a7592-f843-46fb-ba88-259499b96d0e.png','exec-857bdb68-cbdb-4d47-8e33-efe68adf6860.png']
    labels=['A-overview','B-overview','C-overview','A-sheet','B-sheet','C-sheet']
    assets=[]
    for label,source in zip(labels,generated):
        p=REVIEW/'images'/(label+'.png');raw=p.read_bytes();w,h=struct.unpack('>II',raw[16:24]);assets.append({'id':label,'file':p.relative_to(REVIEW).as_posix(),'category':'Concept artwork','tool':'image_gen integrated','date':DATE,'model':'not exposed','seed':'not exposed','prompt':'prompts/'+label+'.txt','reference':('baseline/05-room-cover-after-normal-camera.png' if 'overview' in label else 'images/'+label[0]+'-overview.png'),'original':'C:/Users/gabri/.codex/generated_images/01a1a359-eb1d-79b1-9cfd-d4d40cfe27ae/'+source,'width':w,'height':h,'sha256':hashlib.sha256(raw).hexdigest(),'edits':'None; original PNG copied intact','editable_layers':'Not provided by raster generator','approval':'PENDING DEVELOPER APPROVAL','runtime_asset':False,'rights_note':'Generated for this task from project reference. No external art/artist imitation requested. No legal clearance or production license inference claimed.'})
    for p in (REVIEW/'baseline').glob('*.png'):
        raw=p.read_bytes();w,h=struct.unpack('>II',raw[16:24]);assets.append({'id':p.stem,'file':p.relative_to(REVIEW).as_posix(),'category':'Unity baseline capture','tool':'Unity ScreenCapture / existing UTF fixture','width':w,'height':h,'sha256':hashlib.sha256(raw).hexdigest(),'job':'0c4eed4e5e0042fba8845412ced7466e','source':'Docs/validation/artifacts/run-exp-03/firstsector/20261103-214151-6130699/'+p.name,'assistance':'player immunity, fixture warps; native AI/nav','edits':'None'})
    (REVIEW/'asset-manifest.json').write_text(json.dumps({'schema':1,'assets':assets},ensure_ascii=False,indent=2),encoding='utf-8')
    for target,link in [('Docs/gameplay/TECH_GUY_RUN_REWARDS_ROOMS_ENEMIES_MASTERPLAN.md','../decisions/RUN_EXP_07_RELATORIO.md'),('Docs/decisions/GAMEPLAY_REWORK_PROGRESS.md','RUN_EXP_07_RELATORIO.md'),('Docs/validation/01_MATRIZ_DE_VALIDACAO.md','../decisions/RUN_EXP_07_RELATORIO.md')]:
        p=ROOT/target;t=p.read_text(encoding='utf-8');marker='<!-- RUNEXP07 CURRENT -->'
        block=f'''{marker}
## RUN-EXP-07 — exploração visual entregue até07.4; STOP

03/11/2026. AuditoriaUnity6000.5/URP17.5, baseline atual e três linguagens originais A/B/C com seis artes raster e pacoteHTML offline. A recomendada, **PENDING DEVELOPER APPROVAL**. Conceitos propostos; nenhum método2.5D implementado ou benchmark07.6 executado. UTF existente focado1/1Passed em duas execuções do mesmo caso, não suíte global. Camera metadata reconstruída identificada como tal. C05/combate/boons/física/fontes preservados; integridade nos artefatos07.

07.5/07.6 **NOT DONE**, exigem escolha/autorização explícita. Recorte futuro: uma fachada + uma cobertura; sem conversão produtiva. Este bloco atualiza o estado07 sem reescrever a história06. [Pacote, evidência e limites]({link}).
<!-- /RUNEXP07 CURRENT -->

'''
        if marker in t:t=re.sub(r'<!-- RUNEXP07 CURRENT -->.*?<!-- /RUNEXP07 CURRENT -->\s*','',t,flags=re.S)
        split=t.find('\n')+1;p.write_text(t[:split]+'\n'+block+t[split:].lstrip('\n'),encoding='utf-8')
    print('Built',len(assets),'image records,',len(reports),'reports and offline HTML.')

if __name__=='__main__':build()
