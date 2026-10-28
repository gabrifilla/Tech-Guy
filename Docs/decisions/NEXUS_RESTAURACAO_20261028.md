# Restauração do Nexus e auditoria de cenas — 28/10/2026

Pedido: voltar ao Nexus separado por salas e excluir cenas sem uso, explicando previamente quando a exclusão não fosse válida.

## Decisão e implementação

O layout desejado permanecia íntegro em `Assets/_Project/Scenes/NexusBlockout.unity`. Menu, prólogo e retorno de FirstSector ainda apontavam para NexusLobby: não houve reconstrução ou perda do layout.

NexusBlockout passa a ser o destino normal do menu, conclusão/skip do prólogo e retorno de run. Atualizados campos serializados pelo Editor, defaults dos diretores, gerador FirstSector e atalho Open Nexus Lobby. Geometria, câmera, NavMesh, prefabs e GUID do Nexus com salas foram preservados. Os quatro arquivos de cena alterados têm somente o destino atualizado; MainMenu também remove um componente decorativo indevido.

O helper de revisão não restaura mais o progresso automaticamente ao parar uma sessão normal iniciada no Nexus. A proteção continua disponível por opção explícita: Tools → Tech Guy → Nexus Blockout → Protect Next Review Session Save. Isso evita que o hub agora normal desfaça progresso ao sair de Play.

O smoke revelou um LobbyArsenal sem jogador na cópia decorativa do menu. Removido somente dessa instância; gerador corrigido para não recriar o defeito, com teste de regressão. O arsenal do Nexus continua referenciando seu jogador.

## Limpeza: nenhuma exclusão

A justificativa foi apresentada antes de concluir a limpeza. Há oito cenas, com usos identificados:

| Cena | Uso que impede exclusão direta |
|---|---|
| MainMenu | Entrada e configurações |
| PrologueTutorial | Prólogo e replay |
| NexusBlockout | Hub com Reconexão, Oficina, Treino e Travessia; destino atual |
| FirstSector | Expedição publicada |
| NexusLobby | Fonte de NexusBlockoutBuilder e fixtures espaciais legadas |
| Playground | Fonte de LobbySceneBuilder e CombatStudyBuilder; validação de rig/HUD |
| CombatStudy | Fonte de TutorialSceneBuilder e FirstSectorBuilder; laboratório de combate |
| ProceduralStageAuthoring | Cena de authoring legada, ainda coberta pela integração do rig; sem rota de produção encontrada |

NexusLobby permanece habilitada no Build Settings porque fixtures legadas ainda a carregam por SceneManager. Estar habilitada não significa que o menu ou a run a utilizem. Playground/CombatStudy continuam desabilitadas, ProceduralStageAuthoring fora da lista. Build Settings preexistente preservado.

Não se afirma que essas dependências sejam uma arquitetura ideal: uma consolidação futura deve retirar os geradores da dependência de cenas antigas e decidir aposentar fixtures de layouts substituídos. ProceduralStageAuthoring tem uso apenas de validação encontrado nesta auditoria. Não foram apagados cenas, metas ou dados de terceiros para produzir uma contagem artificial de limpeza.

Inventário e referências: [auditoria Unity](../validation/artifacts/nexus-restoration-20261028/scene-audit.json) e [referências estáticas](../validation/artifacts/nexus-restoration-20261028/static-scene-references.json).

## Validação realmente executada

Unity 6000.5.10f1, MCP Tech-Guy@ec9dce4ca709f3be, projeto D:/Projetos/Gabriel/Tech-Guy.

- **12/12 EditMode**: rig de sete cenas, quatro destinos serializados e ausência de arsenal interativo no diorama. Job `cda0ae0d22d24855a10cb5c03c6d1d69`. A rodada inicial 11/11 foi repetida após corrigir o menu, sem somar casos duplicados.
- **17/17 PlayMode**: RunResults e morte publicada/retorno; incluem três abandonos reais consecutivos com apresentação do snapshot correto. Job `71ecf329a4734418923daa79435c30e2`.
- **29 casos distintos**; filtros focados, não suíte completa. Fixtures históricas de navegação por clique não foram executadas; seu destino de retorno foi explicitado para o próprio layout antigo, sem alterar a rota de produção.
- Smoke real em Play: MainMenuUI.StartJourney pelo MCP, usando perfil existente com tutorial visto, chegou a NexusBlockout. Arsenal do Nexus com referência válida, controle direto habilitado e agente sobre NavMesh. Smoke repetido após correção do diorama: zero arsenais no menu, um arsenal válido no hub.
- Inspeção dos arquivos reabertos pelo Editor: zero scripts ausentes no Nexus e nas quatro cenas de fluxo/authoring alteradas. Playground tem **um script ausente preexistente**, pendente no laboratório; não foi removido sem identificar a função.
- Console final mantém o erro preexistente de pacote Tripo3D com caminho local inexistente e aviso de keys incompletas de AbilityHolder. Não houve limpeza manual do Console. O erro de arsenal do menu não reapareceu no smoke final.
- Moedas, arma selecionada, tutorial e hash do resumo iguais antes/depois dos testes e smoke; preferências de controles não foram editadas. Não é dump de todo PlayerPrefs.
- Diff revisado e git diff --check aprovado. Alterações anteriores do usuário/fase C01.4 e exclusão preexistente de Docs.zip preservadas.

O cliente HTTP antigo retornou 404 de sessão; os testes e inspeções acima usaram o MCP nativo funcional. Evidências em [nexus-restoration-20261028](../validation/artifacts/nexus-restoration-20261028/). Capturas: arrival.png é Game View na dimensão corrente; overview.png usa câmera temporária vertical, sem UI, para conferir as quatro áreas, sem mudar a câmera de gameplay.

Estado final: **Edit Mode, NexusBlockout aberta e limpa, sem compilação ativa**. Layout observado em render real; restauração solicitada implementada; nova aprovação humana de gameplay/arte não foi inferida. Nenhuma etapa GPR futura iniciada.

