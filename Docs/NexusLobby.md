# Nexus — Ponto Zero

Cena: `Assets/_Project/Scenes/NexusLobby.unity`.
Abra a cena no Unity e aperte **Play**. Clique no piso para andar; aproxime-se
de um terminal e pressione **E**. **Esc** fecha o texto do terminal.

O lobby interpreta o hub seguro descrito no README: uma praca suspensa dentro
do computador, cercada por blocos de memoria desconectados. Materiais escuros,
circuitos ciano, falhas magenta e detalhes ambar distinguem suas areas.

- **Kernel:** nucleo flutuante animado, no centro da praca.
- **Neural Link:** bancada com modelo geometrico de uma manopla, caixas de
  equipamento e um terminal com a primeira fala da arma. A manopla exposta e
  decorativa; o player usa o equipamento configurado no projeto.
- **Arquivo / 404:** racks de memoria e um registro sobre a origem do jogador.
- **01 / Incursao:** pressione E proximo ao portal para carregar Playground.
- **02 / Fragmento e 03 / Origem:** destinos bloqueados com texto informativo.
- **Reconexao:** circulo de chegada e bancos de descanso.

Os terminais de arquivo e manopla sao interacoes narrativas. Nao implementam
loja, progressao, sorteio de mundos ou melhorias. O portal carrega a cena de
destino com a configuracao existente dela; nao transfere inventario nem inclui
retorno automatico ao lobby.

## Editar

Toda a geometria e formada por objetos 3D editaveis no Unity. A hierarquia
`NEXUS - Modular Environment` separa as areas. O mesmo conjunto foi salvo em
`Assets/_Project/Prefabs/NexusEnvironment.prefab`. A cena contem uma copia
editavel do ambiente; alteracoes nela nao sao automaticamente aplicadas ao prefab.

Materiais, perfil de pos-processamento e NavMesh ficam em
`Assets/_Project/Art/NexusLobby`. Ao mudar obstaculos ou piso, refaca o bake no
`NavMeshSurface` da raiz do ambiente. Elementos decorativos nao possuem collider;
piso, protecoes de borda e obstaculos fisicos possuem.

`Lobby Guide` referencia explicitamente o player e as cinco estacoes. Para
conectar outro mundo, altere `Destination Scene` da estacao e inclua a cena
nas Build Settings. Um destino vazio abre apenas o texto informativo.

O player foi copiado da configuracao do Playground, com camera e movimento
por clique ligados ao lobby. A camera segue o mesmo padrao do Playground:
altura 10, distancia 10 e angulo horizontal 0. Projecao e campo de visao sao
copiados da camera da cena de referencia pelo gerador, mantendo o alvo no player
do lobby. `Docs/NexusLobby-gameplay.png` mostra esse enquadramento; a imagem
`NexusLobby-preview.png` e apenas uma vista geral para apresentar o mapa.
Playground permanece como a primeira cena das Build Settings; NexusLobby foi
adicionada e pode ser aberta diretamente para testar.

O menu **Tools > Tech Guy > Lobby > Create Nexus Lobby** gera a cena apenas
quando ela nao existe, evitando sobrescrever edicoes feitas no Editor.

## Validacao

- Compilacao dos scripts pelo Unity 6000.5.10f1.
- Bake do NavMesh e caminhos completos do spawn ate as cinco estacoes.
- Smoke test em Play Mode: um player, nenhuma EnemyAI, arma e camera presentes,
  player sobre o NavMesh salvo, deslocamento real e destino Playground disponivel.
- Imagem da cena renderizada pelo Unity: `Docs/NexusLobby-preview.png`.

Smoke test via CLI (encerra o Editor ao terminar; execute numa instancia separada):

```text
Unity.exe -batchmode -nographics -projectPath <projeto> -executeMethod LobbySceneValidation.Run -logFile <log>
```

Os comandos E, o painel de texto e a transicao acionada pelo teclado ainda devem
ser conferidos manualmente. O teste automatizado verifica o destino, mas nao
simula a tecla E.
