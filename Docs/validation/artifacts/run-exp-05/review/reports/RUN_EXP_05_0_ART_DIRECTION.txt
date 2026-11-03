# RUN-EXP-05.0 — direção da fatia visual

02/11/2026. Escopo autorizado: uma revisão visual jogável de recompensa, Fragmento prioritário, Emissor consistente e sala coberta, sem expansão mecânica. A direção abaixo é uma escolha de trabalho delegada pela tarefa, **PENDING DEVELOPER APPROVAL**, não arte aprovada ou publicação de C.

## Auditoria observada

Projeto real confirmado por Unity MCP: `D:/Dev/Personal/My_games/Tech-Guy`, instância `Tech-Guy@fccafb2b47cd7671`, Unity6000.5.10f1. Inicial NexusBlockout limpa/Edit, assistência desligada, Console0 erros. BranchgameplayOverhaul, HEAD314d28ec e mudanças02–04/exclusão anteriorDocs.zip preservadas.2058 hashes emAssets/_Project/ProjectSettings/Packages e63 preferências de produto capturados antes de edição produtiva; valores privados só emLibrary. O estado atual é a autoridade, sem restauração03/04.

Vistas inspecionadas: Fragmento04 próximo e câmera normal; Emissor04; sala aberta/coberta, jogador/HUD; original e C04 nas três armas. Novas capturas de baseline geradas pelo UTF no job `2873b6f5b7b34fab8188ed7f969066dc`:10/10Play,67,644s. Inclui rota0 até boss/tickets/Nexus, comparações de arte/salas/UI e8/32/64 ativos. Os helpers legados gravam destinos03 únicos; os arquivos desta execução serão copiados ao pacote05, preservando os históricos. Não confundir conclusão automatizada com sobrevivência humana.

| Área atual | Diagnóstico visual | Decisão para05 |
|---|---|---|
| Fragmento | Quadrúpede e direção legíveis, corpo/apoios facetados mas conexões e superfícies lembram protótipo; mandíbula laranja compete com o material | Remodelar superfícies anatômicas conectadas, materiais em camadas e UVs; manter quatro apoios e footprint |
| Emissor | Carcaças separadas e canal claros, pouco contraste estrutural e acabamento plano | Mesmo vocabulário cerâmica/grafite/cobre, carcaças e núcleo distintos; passe secundário limitado |
| Sala coberta | Duas massas/circulações e LOS funcionais; paredes e coberturas são caixas, trilhas insuficientes como identidade | Câmara de armazenamento estratificado: massas como arquivos laminados e infraestrutura periférica, sem mudar física |
| C04 | Três alternativas e foco bons; símbolos abstratos repetidos, pouco ritual e confirmação genérica | Preservar arquitetura; ilustração própria de efeitos reais, enquadramento material e transições curtas; C04 comparável |
| Jogador/HUD | Traços/contrastes fortes, orbes e teclas no rodapé; ameaça vermelha prioritária | Conectar valores marfim/grafite e cobre aoHUD; não redesenhar jogador ou HUD |

## Receita de trabalho: arquivo estratificado

- Forma: planos largos quebrados e camadas interligadas; o Fragmento comprime essa linguagem num corpo baixo, o Emissor a abre em duas conchas suspensas. Não reproduzir anatomia de personagens existentes.
- Materiais: cerâmica marfim desaturada nas placas; grafite rugoso no tecido mecânico; cobre nas bordas de encaixe, não dourado em tudo. Piso mais escuro e silencioso que inimigos.
- Superfície: marcas grandes dirigidas pela anatomia, poucos canais/gravuras; textura de desgaste concentrada nas quinas, sem ruído branco em toda face. UVs e atlas compartilhado, não uma instância de material por membro.
- Emissivo: pequenos canais/núcleos turquesa. Vermelho continua ameaça nativa; não adicionar nova luz ou bloom global. Cor não substitui silhueta.
- Bordas: bevels e variações de plano na fonteDCC; usar o pipelineURP existente, sem outline global ou feature nova. Contornos do jogador permanecem.
- Luz: manter direção/exposição do gameplay nas comparações; superfícies claras e rugosas respondem à luz atual. Close-up neutro é evidência adicional, não substituto da câmera normal.
- Ambiente: duas coberturas viram casulos/arquivos estratificados; relevos e encaixes dentro dos volumes atuais. Paredes recebem infraestrutura estática periférica; centro e duas rotas não recebem props. Nada móvel que pareça inimigo, nenhuma decoração vermelha.
- UI: três opções compactas com contraste de foco; uma ilustração selecionada maior e uma frase comportamental. Nome/família e limitações essenciais em texto nativo. Detalhes secundários conservados. Transições curtas em tempo deUI, sem atrasar input/confirmar ou furar pausa.
- Iconografia: oito regiões ilustradas originais para tornado, eco, descarga, gelo, fogo, órbita, flecha carregada e perfuração. Rank/posse são dados reais por código, não números pintados. Itens fora da amostra mantêm fallback honesto documentado, sem fingir catálogo ilustrado completo.
- Tipografia: hierarquia existente marfim, título forte, corpo legível e pouco texto; ajustar comprimentos e foco em1160×600,1280×800 e16:9. Controles mostram bindings atuais, não os slots legados como comandos.

Alternativas rejeitadas: neon em todos os contornos; trocar oHUD por outra estrutura; aumentar detalhes/labels nos três cards; novos obstáculos e arquitetura de geração; mais peças primitivas para simular evolução artística. A prova será comparação Unity, não descrição.

## Ferramentas e pipeline verificados

BlenderMCP `get_addon_status` não conectou nesta etapa. Localizado Blender5.2.2LTS instalado e executado com `--background --factory-startup`; bpy e exportadoresFBX/glTF disponíveis. Portanto há um caminhoDCC real porCLI, sem afirmar conexãoMCPBlender. Fontes.blend ficarão fora deAssets para impedir importação automática concorrente; FBX/UV/texturas serão importados e reabertos naUnity. Rig rígido/IK atual é contrato de pivôs, não um humanoide a substituir. Não instalar pacote de rendering nem alterar arquivos externos.

Skillimagegen lida/aplicada: ferramenta integrada gerou atlas original e correção de espaçamento, além de conceitoFragmento a partir da silhueta04. Fontes preservadas emTools/RunExp05/Source; prompt emreward-art-prompt.txt. O primeiro atlas tinha recortes apertados e foi preservado; segunda chamada corrigiu padding. Normalização/crop/alpha/importação ainda precisam ser verificados. Conceito é **mockup de direção, não screenshot/runtime ou modelo final**. Não foi usado modelo externo, Tripo ou download de personagens.

## Skills, regras e gate

Descobertas/lidas: quatro skills locais atuais `tech-guy-game-vision`, `tech-guy-combat-unity`, `tech-guy-boon-design`, `tech-guy-unity-acceptance`, com quatro referências e documentos canônicos vinculados. LidosAGENTS/README, visão/pilares/arte, contratos combate/armas/inimigos/boons/run/HUD, requisitos/desenho/tasks/masterplan/matriz/ledger/estado e relatórios03/04 pertinentes. Não foi encontrada skill localUI ou dedicada a modelagem/rig.

Instaladas lidas/aplicadas: router direcionouUnity e disciplinas; unity-mcp/C# orientam importação/round-trip e contratos; unity-animation mantém dano fora da animação; create-game-assets e referências art-direction/raster/three-d/provenance definem família/manifest/UV; imagegen para ilustrações; game-ui-ux para foco/escala; game-feel para feedback sem latência; level-layout/NavMesh para duas rotas e physicscolliders; performance-optimization para comparar condições atuais e medir antes de otimizar. As escolhas acima derivam dessas regras, não somente de nomes listados.

Gate05.0: **DONE** para auditoria, baseline e receita delimitada compatível com pipeline atual. Implementações05.1–4 ainda não verificadas por este relatório. A arte final e todo julgamento humano permanecem pendentes; nenhum teste técnico é aceite visual.
