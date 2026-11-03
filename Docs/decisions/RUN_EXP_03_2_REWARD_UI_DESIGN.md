# RUN-EXP-03.2 — duas direções de recompensa

Escopo: substituir a sensação visual genérica/densa por dois conceitos reais para revisão. A UI publicada `RunRewardUI.cs` permanece intacta. STOP antes da escolha humana de direção; nenhum conceito é instalado automaticamente em cenas produtivas.

## Propostas e recomendação

| Critério | A — Artefatos | B — Reconfiguração |
|---|---|---|
| Composição | três glifos grandes, espaços vazios e formas facetadas suspensas | trilho vertical de escolhas e painel amplo da transformação selecionada |
| Leitura inicial | compara três efeitos comportamentais de uma vez | lê um efeito por vez, com foco no kit atual |
| Contexto | sinergia real da seleção no rodapé | sinergia real junto ao efeito selecionado |
| Densidade | três frases curtas, detalhes opcionais | três nomes no trilho, uma explicação principal |
| Custo | IMGUI simples; ícones por família | mesmo modelo, composição/foco adicionais |

RecomendoA para a próxima revisão: apresenta cada ganho como artefato e permite comparar transformações sem abrir uma ficha para cada opção. B é alternativa mais orientada à compreensão do kit. São recomendações de design, não aceite do jogador. As imagens reais de[REVIEW.html](../validation/artifacts/run-exp-03/REVIEW.html) incluem original/A/B com as mesmas ofertas, três armas e duas proporções1160×600/1280×800.

## Arquitetura e assets

Arquivos novos `Scripts/UI/Review/RunExp03Reward{Model,View,Lab}.cs`, `Scripts/Editor/RunExp03RewardsAuthoring.cs`, cena isolada `Scenes/Review/RunExp03Rewards.unity`, quatro PNGs em `Resources/UI/RunExp03`. Usa IMGUI, a tecnologia real do presenter existente, sem instalar pacote. O Lab equipa cópias runtime e solicita ofertas reais; não tem diretor/save. A/B também podem ser anexados temporariamente a uma run real pelos menus; sair do Play restaura a cena publicada.

Modelo editorial lê IDs/descrições/inventário reais, mantém dados numéricos nos detalhes, mostra restrições essenciais e converte escopos Q/W/E/R para teclas atualmente configuradas, inclusive escopos múltiplos. Protocolo adquirido aparece como evolução de nível. Contexto só consulta aquisições; cooferta não é posse. Não há promessa de completar Titanic+Conductor+Detonation. As descrições novas são apresentação, não regras ou boons novos.

Glifos originais foram desenhados por polígonos/rasterização no builder: fragmento facetado, descarga ramificada, lâminas e moldura losangular. São ilustrações geométricas intermediárias por família, não arte profissional exclusiva para cada boon. PNG512² RGBA/sRGB, alpha, bilinear, sem mipmap/compressão. Origem/hashes/importação em[asset-manifest.json](../validation/artifacts/run-exp-03/asset-manifest.json); sem gerador externo.

`RunBoons.Choose` segue como commit único. View valida revisão/IDs do ticket, bloqueia confirmação duplicada no quadro e usa visibilidade/modal do presenter nativo. Mouse seleciona; confirmar é ação deliberada. Setas/dígitos/dpad navegam, Enter/botão sul confirmam; Tab/oeste abre detalhes. Input gameplay não foi remapeado. Pausa tem prioridade. Destruir a View restaura o presenter; rebind da mesma View preserva seu estado anterior.

## Unity, testes e falhas intermediárias

Projeto/instância MCP confirmado em03.0, cena autorada/importada/reaberta na Unity. Primeiro job`29f96ccb128f442a9dfd95aa5e6627bc`:2/2,3,396s. Comparou ofertas reais sem aquisições e contexto realmente adquirido, seguido de confirmação nativa uma única vez. O caso de sinergia usa aplicação assistida de Titanic e lista de ofertas explicitamente montada; não é evidência de RNG natural. Capturas iniciais com moldura rotacionada ruim foram preservadas e substituídas por PNG próprio; não são a proposta final.

Bateria integrada inicial`70751110b2fb4982976caf3d6e0a4b15`:todos os4 casos UI passaram (o único vermelho foi pausa do inimigo,03.3). Inclui teclado/gamepad virtuais no loop real, pausa impedindo aquisição, preview sem commit, lista vazia desabilitada, nível1→2 e teclaF9 temporária restaurada. A edição editorial posterior melhora Mil Punhos e escopos múltiplos; job final e teste físico de mouse estão em03.4. Capturas de variantes curtas/longas e painel de detalhes não concedem boons.

## Skills e limites

Aplicadas: tech-guy-game-vision/boon-design/unity-acceptance e referências; game-ui-ux; create-game-assets; unity-mcp/C#/ScriptableObjects. Direção original digital, hierarquia progressiva, propriedade real do inventário e um único fluxo de aquisição foram as regras centrais. Não houve Figma/Blender ou imagem externa alegada.

GateC **aguarda revisão humana**. O teste automatizado valida regras e estados, não entusiasmo, contraste percebido ou compreensão por jogador. Navegação UI tem convenções fixas distintas dos controles rebindáveis do combate; escopos exibem o binding real. Detalhes preservam texto legado do catálogo. Leitor de tela, remapeamento próprio de navegação UI e ilustração única por boon ficam para etapa aprovada após escolha. Não publicar A/B silenciosamente.
