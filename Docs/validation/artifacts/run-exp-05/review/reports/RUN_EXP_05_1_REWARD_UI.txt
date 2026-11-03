# RUN-EXP-05.1 — Síntese C05 em revisão

Escopo: evoluir a personalidade visual de C sem publicar uma nova UI de produção. **DONE** para implementação e verificações abaixo; **PENDING DEVELOPER APPROVAL** para direção artística. RunRewardUI, tickets, pesos, definições e controles produtivos não foram substituídos. C04/A/B continuam comparáveis.

## Diagnóstico e decisão

C04 tinha boa estrutura de três opções, mas usava o mesmo símbolo por família e confirmação com pouco destaque. C05 mantém alternativas compactas e um efeito em foco; acrescenta ilustrações de ações reais, superfícies grafite/cerâmica/cobre, encaixes angulares contidos, contraste de foco e confirmação clara. O texto de Asura reverberante foi convertido de uma frase numérica para a réplica do golpe final; números permanecem nos detalhes. Não há escolha ótima marcada ou promessa de parceiro futuro.

Alternativas consideradas: três cards inteiramente ilustrados aumentariam densidade/competição; um carrossel esconderia as alternativas; mais badges/estatísticas repetiriam o problema04. Mantido o presenterIMGUI atual com canvas virtual ajustado por menor escala e margens fixas relativas à referência, sem migrar tecnologia ou instalar pacote.

## Arte e pipeline demonstrados

Oito vignettes originais: tornado/Punhos Titânicos, eco, Conductor, Geada, Ignição, Órbita, Juízo e Perfuração. Produzidas com image_gen integrado, atlas4×2 e uma correção de padding; fontes emTools/RunExp05/Source e prompt emTools/RunExp05/reward-art-prompt.txt. O primeiro resultado apertado foi preservado. Dimensões reais1774×887; corte com bordas arredondadas ao pixel, sem presumir grade divisível. Normalização apenascrop/resize512, sem repintar/gerar porPython. Verificados alpha real, margens e ausência de contato entre células. Pillow13 isolado emvenvLibrary para QA, sem dependência do jogo.

Assets emResources/UI/RunExp05:512²,sRGB,alpha,bilinear,sem mipmap/readwrite,compressãoHQ. Importação/round-trip/Guids e memória observada emreward-art-import.json. Oito texturas mediram cerca de2,3MB somadas na inspeção, não orçamento final universal. Fonte e parâmetros emreward-art-manifest.json. Não usados assets de jogos/terceiros ou modelosTripo. A pintura comunica fantasia, não afirma acabamento final aprovado.

Mapeamento visual porID emRunExp05RewardArt: oito artes para nove IDs; EcoTemporal e PunhoEcoante compartilham o motivo de impacto atrasado, com contratos distintos no texto. Demais itens continuam com glyph03; **NOT DONE** para ilustração exclusiva do catálogo inteiro. Rank usa o mesmo símbolo doProtocol e números reais, nunca números pintados. Esses limites são documentados fora do fluxo normal do jogador.

## Sistemas e arquivos

Inspecionados Model/View/Lab03, RunRewardUI, RunBoons/Choose/ApplyOffer, apresentaçãoProtocol/catálogos/boons e bindings. Modificados Model (ID para arte e frase deAsuraEcho), View (layoutSynthesis05 e feedback compartilhado), Lab (marcador de mostruário assistido). Criados partialView.Synthesis05, RunExp05RewardArt, RunExp05RewardAuthoring e partialTests.Visual05. Não duplicado executor/rewardauthority.

Transições: foco/hero até0,13s, detalhes até0,1s; confirmação aplicaChoose imediatamente e mostra retorno curto0,8s, sem bloquear avanço. Movimento reduzido é opção de revisão sem preferência persistente. Modal bloqueia confirmação/subclique; troca deliberada por teclado mantém foco. Pausa tem precedência, previewreadonly e revisão/IDs inválidos não adquirem. Restaurar/destroçar preview devolve presenter nativo conforme estado capturado. UI permanece opt-in.

MenusTools→TechGuy→RUN-EXP-05→Revisão: abrirLab; C05temporário na run; restaurarpublicada; alternar movimento reduzido; mostruário assistido da arma atual emPlay. O mostruário usa definições reais numa lista explícita e éreadonly, identificado como comparação assistida; não prova elegibilidade/RNG de ticket. Escolherarma noLab prepara novamente ofertas normais. Confirmar na prévia de uma run real consome ticket real; oLab não persiste sua aquisição.

## Unity e resultados

MCP confirmado no projeto realD:/Dev/Personal/My_games/Tech-Guy6000.5.10f1. Baseline05.0 foi10/10 antes desta implementação. JobsUI:

| Job | Resultado | Diagnóstico |
|---|---|---|
|67ffd450984d42c79768b456035f5d97|5/6,20,819s|Fixture lia lista apósApplyOffer nativo encerrá-la/limpá-la|
|4e2ee2d2a7a64b11ada186ae0c3e1dd6|6/6,21,189s|Fixture abre próximo pedido e repopula comparação assistida; regra produtiva intacta|
|7fcc50d08c6b497b9dc626ddf2b19da7|6/6,23,018s|Revisão final após corrigir traços da moldura e texto comportamental|

Original/C04/C05 comparados com **mesmas ofertas e zero aquisições** por linha emGauntlet/Bow/Spear×1160×600/1280×800/1920×1080. Estados selecionados/detalhes, nomes curtos/longos, contexto ausente/cooferta, posse realTitanic→Conductor, rank1→2 e mostruários deMutationSpear/Bow. Estes últimos são fixtures assistidasreadonly com definições reais, não novos boons ou prova de sorteio. Teclado/gamepad virtuais, pausa, modal, lista vazia, identidade stale e duplicação passaram.

MouseWindows nativo pela skillcomputer-use: selecionar/abrir detalhes/clique sob modal deixouowned0; confirmarAsuraEcho produziuowned1/choicesvazias. EvidênciaJSONmouse-initial/selected/modal/confirmed. Play encerrado e maximizaçãoGameView anterior restaurada. Esses cliques antecedem a correção puramente visual da moldura; contratos de aquisição foram mantidos no últimoUTF.

Inspeção de PNGs encontrou linhas fora do clip em aspecto largo: rotaçõesGUI.matrix nas arestas verticais deslocavam o recorte. Corrigidas para traçosRect sem alterar a matriz; comparação final aberta confirma ausência das linhas. Capturas e vermelho intermediário preservados. Nenhuma asserção relaxada. Console após compilação0 erros sem limpeza manual. Arte e coesão observadas não equivalem a preferência humana.

## Skills, integridade e limites

QuatroTechGuy locais+refs aplicadas à direção digital, fonte única de requisitos e separação de aceite; boon-design impediu tratar cooferta como posse ou editar a economia. UnityMCP/C# conduziram importação/round-trip e testes. create-game-assets/imagegen orientaram atlas/proveniência/QA; game-ui-ux determinou foco, escala e modal; game-feel limitou feedback sem latência; computer-use validou mouse no aplicativo real. Não existe alegação de Figma ou Blender nesta autoriaUI; BlenderCLI será usado em05.2.

Prefabs/cenas/UI publicada e dados de recompensa não foram editados nesta etapa. Preferências e hashes serão comparados ao baseline atual05 no fechamento05.4, sem restaurar histórico. Testes usamLabisolado e fixture deResumo existente; mostruários não gravam progressão. Leitor de tela/remapeamento próprio da navegação e ilustração de todos os itens **NOT DONE**; arte final e aceitação humana pendentes. Pacote portátil e fechamento de performance/integridade ficam em05.4. Gate técnico permite a próxima etapa limitada deDCC, sem autorizar publicação deC05.
