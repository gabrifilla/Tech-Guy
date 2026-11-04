# RUN-EXP-07.2 — artes comparáveis da mesma arena

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
