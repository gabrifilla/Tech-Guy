# Prólogo, menu inicial e configurações

O jogo abre em `MainMenu`. Na primeira jornada, **Iniciar jornada** abre `PrologueTutorial`; concluir ou pular o tutorial leva ao `NexusLobby` e libera **Continuar no Nexus** nas próximas entradas. **Repetir prólogo** permite praticar novamente sem apagar moedas, armas ou desbloqueios. O lobby tem um botão **Menu inicial** para voltar às configurações e às demais opções.

## Tutorial

1. Caminhar até o marcador azul com o botão direito.
2. Executar uma esquiva com Espaço.
3. Acertar três ataques básicos no alvo de treinamento.
4. Acertar a habilidade Q no alvo.
5. Derrotar dois drones de treinamento.
6. Escolher um modificador real usando a mesma interface das runs.
7. Testar a escolha no alvo, se desejar, e atravessar a saída dourada até o Nexus.

Esc abre o menu de pausa, onde é possível continuar, voltar ao menu inicial, sair do jogo ou pular o tutorial para o Nexus. Morrer reinicia o tutorial. A manopla de treinamento e o modificador escolhido são locais à cena: o equipamento salvo permanece intacto. Os drones não concedem moedas.

## Preparação para a história

No objeto **Tutorial flow**, o componente `TutorialDirector` tem sete entradas em **Beats**, com título, controle, instrução, transmissão e evento **On Enter** editáveis no Inspector. Use esses eventos para acionar diálogos, animações, efeitos ou sequências de história. **On Completed** ocorre somente ao concluir normalmente; pular não dispara esse evento. Há também o evento C# `StageChanged`.

A calibração atual é uma base jogável para o prólogo; os textos não estabelecem uma história definitiva.

## Configurações

- Vídeo: modo janela/tela cheia sem bordas, resolução, níveis gráficos existentes no projeto e VSync.
- Áudio: volume geral, incluindo silêncio em 0%.
- **Aplicar** salva a seleção em PlayerPrefs. **Voltar** descarta alterações não aplicadas.
- **Restaurar padrões** preenche os valores padrão; é necessário aplicar para salvá-los.
- No jogo compilado, alterações de resolução/modo de tela exigem confirmação em 15 segundos; cancelar, pressionar Esc ou esgotar o tempo restaura os valores anteriores.
- No Editor, resolução e modo de tela não redimensionam a Game view. As demais configurações são aplicadas normalmente.
- A página **Controles** mostra os comandos atuais; não implementa remapeamento.

As preferências usam `TechGuy.Settings.*`. A passagem pelo tutorial usa `TechGuy.Progress.TutorialSeen`; não representa um salvamento de run em andamento. Continuar inicia no Nexus.

## Abrir e validar

Use **Tools → Tech Guy → Scenes → Open Main Menu** e dê Play para testar o fluxo completo. **Open Prologue Tutorial** abre diretamente a fase. Os geradores **Create Main Menu** e **Tutorial → Create Tutorial Scene** preservam cenas existentes.

`TutorialValidation.BuildAndRun` executa a validação de integração em um Editor isolado: navegação, golpes reais, Q, recompensa, morte, pulo, destinos do menu e persistência das configurações. `TitleTutorialPreview.Run`, iniciado com `-titlePreview`, captura as telas renderizadas. Essas ferramentas encerram o Editor ao concluir e devem ser executadas em uma cópia de validação do projeto.
