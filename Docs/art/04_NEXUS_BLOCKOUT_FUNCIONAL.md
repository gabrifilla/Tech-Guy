# Nexus VS-03.4 — revisão espacial para jogar

**24/10/2026 · O primeiro blockout foi REJEITADO. Esta revisão reconstrói sua geometria; direção conceitual mantida, avaliação humana pendente e VS-03 aberta.**

Projeto MCP: D:/Dev/Personal/My_games/Tech-Guy, Unity6000.5.10f1, Tech-Guy@fccafb2b47cd7671. Abra Assets/_Project/Scenes/NexusBlockout.unity e dê Play. NexusLobby, sua câmera, FirstSector e prefabs de produção são preservados.

## Planta construída

Referência principal: planta à direita de Library/GameplayObservation/vs033-direction/nexus-direction.png, indicada na proposta03. Reutiliza suas relações espaciais e vazios; não copia as medidas esquemáticas.

- **Reconexão SE:** plataforma larga e chanfrada; chegada/retorno, arquivo de origem Terra e resultados existentes. Acesso curto ao noroeste.
- **Oficina oeste:** volume recortado e deslocado, bancada contínua e três suportes proporcionais; pedestais/círculos de exposição removidos.
- **Treino NW:** plataforma aberta maior, alvo, linhas de distância e anteparo no fundo; ligação curta à Oficina.
- **Travessia NE:** terminal independente com ábside e portal existente; acesso pelo lado oeste. Apenas FirstSector é jogável.

Rota rápida: **Reconexão → conexão central → Travessia**, sem visitar preparação. Circuito opcional: Reconexão → Oficina → Treino → Travessia; todas as ligações reversíveis. Conectores abertos de3–5,5 unidades de extensão e3–4 de largura, com patamares nas bifurcações. Não há piso retangular sob os ambientes: os vazios são reais, sem collider/NavMesh oculto. Câmera40°, altura10/distância10.

Graybox com dois materiais neutros simples, pisos próprios e limites traseiros baixos. Sem arte final, NPCs, diálogos, missões ou progresso narrativo. Origem Terra fundamenta preparar-se e partir; nenhuma descoberta por run é inventada.

## Jogar

Mover/equipar seguem os bindings atuais; E equipa no perfil usado. Card curto por proximidade; kit completo somente pelo botão ou Skill4 (atualmenteF11). Modal, soltura e reserva do ponteiro seguem nos componentes existentes. Preços, desbloqueios, persistência e hitboxes não mudaram.

O helper Editor-only já existente protege a sessão iniciada com Play nesta cena: usa o perfil atual sem concessões, faz FirstSector retornar à variante e restaura seleção/desbloqueios/moedas/último resultado no Stop. Não é integração standalone. Recuperação na mesma sessão do Editor: Tools → Tech Guy → Nexus Blockout → Restore Interrupted Review Save, fora de Play.

Avalie primeiro a planta sem rótulos; depois siga diretamente à saída, volte à Oficina, equipe as três armas, abra/feche detalhes, atinja o alvo, use a ligação lateral, entre em FirstSector, abandone, consulte/feche resultados e prepare outra tentativa. Registre hesitações, caminhos inesperados, cliques difíceis e oclusão.

## Evidência e limites

Library/GameplayObservation/vs034-revision contém footprint-only.png, topdown-no-labels.png, comparação com a planta original, quatro áreas e quatro conexões na câmera real, capturas720p/1080p e estados JSON. A vista superior é render real do Unity com câmera temporária vertical e UI/rótulos ocultos; a câmera de gameplay é preservada. Composições mantêm os PNGs originais. Jobs/resultados atuais na matriz.

Mouse/teclado virtuais, picking e NavMesh de produção, sem Warp/SetDestination na jornada. Modal de ownership e confirmação de abandono usam APIs; destino de retorno ajustado na instância pela fixture. O teste exige dano efetivo ao TrainingDummy. Perfil já liberado usado sem concessões, com restauração das preferências. Isso não constitui avaliação humana ou vitória natural.

Limites para jogar: estrutura superior do portal ainda pode cortar perto da interação; a câmera mostra o próximo trecho, não todos os destinos simultaneamente; Lança na mão mantém o prefab existente. Exposição mantém dimensões máximas0,55/1,30/1,85, sem alterar combate. Avaliar segurança percebida nas bordas abertas e descoberta da bifurcação antes de adicionar guardas ou arte.

**Arquivos:** NexusBlockoutSpatialRevision.cs; NexusBlockout.unity e seu NavMesh existente; mesh de pisos e dois materiais novos; NexusBlockoutPlayModeTests.cs; uma linha de orientação opt-in em LobbyInteraction.cs; gerador create-spatial-revision-review.cjs; documentos03/04 e matriz. GUIDs preexistentes preservados. Geometria rejeitada removida, não apenas desativada.

**Skills:** game-vision/world-journey fundamentaram retorno à Terra e repetição sem gates; world-identity/level-layout fundamentaram volumes assimétricos, proporções, vazios e conexões curtas; map-storytelling ligou chegada → preparar → partir com elementos existentes; Unity acceptance/MCP separaram cena salva, input virtual, APIs e aceite humano.

Próxima iteração: julgamento humano da planta completa/chegada, ajustes apenas nos acessos que demonstrarem dificuldade, decisão sobre portal e arma na mão antes de arte. Não substituir NexusLobby sem aprovação explícita.

