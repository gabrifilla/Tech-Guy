# GPR-C01.4d — Q/E/R, especial separado e integração HUD

28/10/2026 · **CONCLUÍDA TECNICAMENTE**. Aceite físico de C01.4 pendente; STOP antes de C01.5/GPR-01.

## Objetivo e decisão vigente

Exatamente três skills modulares Q/E/R, especial permanente separado RMB, sem X/quarta modular/placeholder. As quatro referências e index3 históricos continuam intactos: trata-se de adaptação de ação/UI, não migração de assets/boons/analytics. O uso de quatro entradas internas não define quatro skills modulares.

Holder index3 é encaminhado ao canal especial em todos os kits diretos. Gauntlet mantém a única Asura; Bow/Spear reutilizam a quarta ação nativa já existente sem criar executor, mecânica ou asset. HUD mostra prefixo Especial e binding efetiva; três skills e dash permanecem separados. F11 não é alias direto. Se o jogador remapear explicitamente uma ação para uma tecla livre, essa é sua escolha, não um alias legado automático.

Menu de controles e tutorial usam labels efetivas; quatro direções, básico, especial, três skills, dash, Interact, finisher e pausa são configuráveis. Escape continua saída segura mesmo com pausa remapeada. O clique que termina captura não pode também aplicar Settings no mesmo frame; captura cancela por Escape/foco/mudança de dispositivo. Restore na aba Controles modifica somente bindings/direção de dash do draft, sem resetar vídeo/áudio/progresso.

## Alterações desta subatividade

AbilityHolder.cs (ResolveKey/GetAbilityKey/ownership especial); CharControlScript.DirectBasicInput.cs (sem secundário legado concorrente); PlayerHUD.cs (especial identificado); MainMenuUI.cs/TutorialHUD.cs (prompts); ControlRemappingPanel.cs/PauseMenuUI.cs/GamePreferences.cs (segurança de captura, pausa remapeável/labels); RunResultsPresenter.cs (soltura inclui botões remapeados); LobbyInteraction.cs (reconstrói textura de UI perdida); DirectBasicInputPlayModeTests.cs e RunResultsPlayModeTests.cs (contratos de kits, Q/E/R, HUD e segurança).

## Evidência e correções demonstradas

Focados de kits/HUD:2/2, job7e8611917b524c35b002aa9257fae583. Primeira execução1/2 preservada: fixture atribuía1500 de mana atual com máximo0; corrigida preparação do máximo e mantida a asserção de débito exato. F11/X não ativam, RMB remapeado usa uma habilidade/consumo por arma, Q/E/R preservam referência/custo e labels atualizam.

Novo teste de resultados com Mouse4 reproduziu0/1 no jobebb7bc144b814504b1196d7412b41c29: barreira própria ignorava botões laterais. Corrigida para consultar as ações efetivas; teste valida soltura e binding após reload da Nexus. Segurança focada posterior3/3, job075c180f023c4113b96bfa7dde60a234, antes da adição de pausa remapeável à cumulativa.

Ao observar a transição real, Console registrou null texture em LobbyInteraction.OnGUI. O guard de estilos passou a verificar também a textura nativa: uma GUIStyle sobrevivente não impede reconstrução do recurso perdido. Novo teste destrói a textura em memória, espera o loop real recriá-la e verifica contexto intacto. Bateria posterior UI9/9, jobde0fc68fa4854306b1332ef0ef5dbc32. Captura posterior final-interact/interaction.png aberta/inspecionada, com painel/labels legíveis. Nenhuma asserção ou tempo de combate foi afrouxado.

Cumulativa agregada:116 casos distintos aprovados, zero ignorados nos resultados finais selecionados. Não foi uma única execução global;115 passaram na rodada completa e a correção visual final teve9 casos UI reexecutados, acrescentando1 caso distinto. Tabela exata no consolidado e JSON c01-4d.

## Game View, Console e integridade

MCP real confirmou D:/Projetos/Gabriel/Tech-Guy, instância Tech-Guy@ec9dce4ca709f3be, Unity6000.5.10f1. Testes via ponte MCP HTTP com requests/start/results; ferramentas MCP nativas também usadas no fechamento para reconfirmar projeto, Console e Edit Mode. Nenhum CLI build foi tratado como prova Unity. Console nunca foi limpa por comando.

Baseline Git: HEAD7950fd0 com alterações locais3b, Docs.zip excluído e EditorBuildSettings.asset modificado, preservados. Nenhuma cena, prefab, ScriptableObject, meta ou GUID preexistente alterado pela fase. C# recompilado/importado no Editor; novos arquivos de evidência/relatórios ficam fora de Assets. Editor terminou NexusLobby limpa/Edit Mode, sem compilação; tamanho customizado de captura removido e Game View voltou ao índice0/Free Aspect.

Skills aplicadas: tech-guy-game-vision, tech-guy-combat-unity e tech-guy-unity-acceptance, com suas referências. As skills gamedev solicitadas não estavam disponíveis no diretório de skills deste usuário; não foram instalados pacotes nem declarado seu uso. Arquitetura real e contrato aprovado governaram a implementação.

Capturas reais1280x720 inspecionadas: Settings da pausa (topo/rodapé), Settings do menu, HUD Asura e interação. Driver realNexus com input virtual G+LMB abriu/fechou estação02/FRAGMENTO com0 básicos/0 casts. Whiff separado gerou3 básicos0/1/2 e1 Asura,5,390s, interval/busy0,2s. Primeiro driver junto ao treino registrou1 básico/1 Asura, não3; os arquivos de frames não foram tratados como três aceitações. Energia100 concedida apenas em memória nos drivers de combate; nunca persistida. Teleporte de posicionamento apenas durante observação automatizada; não prova navegação/playtest humano.

Snapshots antes/depois da janela automatizada eram iguais (bindings, modo, moedas, seleção e hash/comprimento do resumo). Leitura nativa posterior encontrou bindingsv2 salvas, modoMovimento, moedas14219 em vez de14200 e resumo novo; autoria dessas alterações posteriores não foi verificada. Estado atual foi preservado, sem restaurar snapshot antigo nem atribuir a mudança à migração. Testes de preferências usam scopes com restauração. Não houve Apply de Settings pelos drivers de captura.

Console mantém erro de dependência Tripo3D/caminho da máquina antiga e avisos de keys incompletas, já existentes. Houve erro de reflexão na preparação da captura (Open é público; flags corrigidos) e erro Invalid index na primeira remoção de tamanho customizado (API usa índice absoluto; limpeza corrigida, removidos apenas os dois tamanhosC01.4, total7/índice0). Logs preservados; não são bugs de execução das habilidades. Não declarar Console globalmente sem erros.

## Limitações e próximo gate

Sem restart standalone, teste físico novo de hotplug ou aprovação humana de C01.4. Aceite físico recebido pertence ao combate anterior; as novas preferências/políticas precisam do checklist do consolidado. Sem benchmark/crowd novo. Não há regressão automatizada pendente nos filtros executados, mas isso não aprova a suíte global. Próxima proposta: C01.5 integrado/feel/validação humana, SOMENTE após autorização explícita. Nenhuma aquisição/substituição de skills/boon novo implementada.
