# Arsenal do Nexus

Na cena `NexusLobby`, aproxime-se da bancada **ARSENAL / ARMAS**, pressione **E**, escolha uma arma e clique em **Equipar**. **Esc** fecha o painel. A manopla é a arma padrão; a seleção fica salva entre cenas e sessões. As habilidades usam **Q / W / E / R** e o cursor define a direção. A chuva de flechas usa a posição do cursor, limitada a 12m.

| Arma | Q | W | E | R |
| --- | --- | --- | --- | --- |
| Manopla | Avanço Relâmpago | Punhos Relâmpago | Impacto de Choque | Rajada Asura |
| Arco e flecha | Disparo duplo | Tiro concentrado | Leque de flechas | Chuva de flechas |
| Lança | Estocada do dragão | Lua crescente | Rajada espiral | Dragão vermelho |

Arco: projéteis com colisão varrida, obstáculos sólidos bloqueiam os tiros e o tiro concentrado atravessa inimigos. Chuva: quatro pulsos em um ponto fixo. Lança: estocadas estreitas e varredura circular. Todos os golpes usam os atributos e passivas existentes; o painel mostra mana e recarga. Trocas durante uma habilidade são bloqueadas. O painel bloqueia movimento por clique, ataques e habilidades; Asura só aparece com a manopla equipada.

As inspirações são [Breaker / Asura](https://www.playlostark.com/en-gb/news/articles/breaker-breakdown), [Sharpshooter](https://www.playlostark.com/en-gb/news/articles/august-2023-wield-the-storm-release-notes) e [Glaivier](https://www.playlostark.com/en-gb/news/articles/lost-ark-academy-glaivier?tag=academy). São kits iniciais adaptados ao jogo: não incluem a ave do Sharpshooter, troca de posturas da Glaivier nem árvores de talentos. O tiro concentrado tem preparação automática, sem exigir segurar a tecla.

Os modelos geométricos e efeitos de arco e lança são provisórios. As animações reutilizam o personagem atual e precisam de animações próprias em uma etapa de acabamento. Dano, tempos, mana, alcance e dimensões podem ser ajustados nos assets em `Assets/_Project/ScriptableObjects/Arsenal`. A manopla e seus assets permanecem preservados.

`Tools > Tech Guy > Arsenal > Build Arsenal` recria os valores iniciais dos assets e instala a estação na bancada existente; não é necessário executá-lo para jogar a cena entregue. Não executar após ajustes de balanceamento sem intenção de restaurar os valores base. O builder preserva GUIDs e não altera o NavMesh: os novos displays não têm colisores.

Validação automatizada em uma cópia isolada, sem `-quit`:

```text
Unity.exe -batchmode -nographics -projectPath <copia> -executeMethod ArsenalValidation.Run -logFile <log>
```

O teste verifica seleção, quatro habilidades por arma, consumo de mana, recargas, bloqueio de ações simultâneas, colisão de flecha, dano de lança, persistência até `FirstSector` e retorno ao kit Breaker. Restaura a preferência anterior ao terminar.

Validação executada em 25/09/2026, Unity 6000.5.10f1: **81 verificações passaram em Play Mode**, em cópia isolada com `-batchmode -nographics`. Compilação sem erros C#. A execução sem gráficos não valida o acabamento visual nem substitui uma revisão manual das animações. O teste aguarda a inicialização do Editor para separar os testes de gameplay da indexação de busca do Unity.
