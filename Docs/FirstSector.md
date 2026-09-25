# Setor 01 - Memoria Corrompida

Abra **Tools > Tech Guy > Scenes > Open Nexus Lobby**, entre em Play e caminhe
ate o centro do portal **01 / INCURSAO**. Tambem pode usar E perto dele.
Para abrir diretamente: **Tools > Tech Guy > Scenes > Open First Sector**.

A cena `Assets/_Project/Scenes/FirstSector.unity` tem tres arenas conectadas,
iluminacao de orientacao azul, navegacao salva e a mesma HUD das outras cenas.
Os encontros ativam por proximidade e em ordem:

1. Acesso: tres inimigos normais.
2. Rele: tres normais e um acelerado.
3. Guardiao: um raro resistente/acelerado e dois normais.

Os inimigos nao reaparecem. Cada grupo eliminado restaura vida e 20% da mana
maxima para suavizar a primeira incursao. O objetivo no alto informa o grupo
atual e os inimigos restantes. Ao eliminar todos, o anel dourado de extracao
acende; entrar nele retorna ao Nexus. Morrer retorna ao Nexus apos 2,5 segundos.
Entrar novamente reinicia os encontros; ainda nao ha recompensa persistente.

O dash padrao custa zero, inclusive com a mana vazia. As habilidades da manopla
custam 80/150/140/280 de mana; o R tambem exige energia Asura cheia.

## Manutencao

`FirstSectorDirector` concentra a progressao local; seus encontros, inimigos,
jogador, texto e portal sao referencias serializadas na cena. `ScenePortal`
controla a entrada automatica do lobby. A cena esta habilitada no Build Settings.
Os materiais e NavMesh proprios ficam em `Assets/_Project/Art/FirstSector`.

`FirstSectorBuilder.Build` gera apenas se a cena nao existir, preservando edicoes.
O balanceamento fica nos assets `Breaker*.asset`, e os inimigos da fase podem
ser ajustados nas instancias da cena sem mudar os prefabs usados nos testes.

## Validacao

Na copia isolada do projeto:

```text
Unity.exe -batchmode -projectPath <copia> -executeMethod FirstSectorValidation.Run -logFile <log>
```

O teste verifica entrada real por proximidade, HUD, dash gratuito com mana zero,
dash modificado com custo, extracao inicialmente bloqueada, dano da sequencia,
limpeza dos efeitos, ativacao e conclusao dos tres encontros, retorno por vitoria,
reinicio e retorno por morte. A geracao valida 14 caminhos no NavMesh.
As capturas ficam em `Docs/FirstSector-overview.png`, `FirstSector-combat.png`
e `FirstSector-guardian.png`. O comando de teste encerra a instancia do Editor.
