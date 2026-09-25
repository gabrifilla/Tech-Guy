# HUD compartilhada e mana

Playground, NexusLobby, CombatStudy e FirstSector usam instancias do mesmo prefab:
`Assets/_Project/Prefabs/PlayerHUD.prefab`. Cada instancia referencia o jogador
da propria cena. A HUD antiga do Playground permanece desativada na hierarquia,
para preservar a composicao original. Os sprites dos orbes foram reaproveitados
do pacote ja usado no Playground, sem modificar os arquivos de terceiros.

A referencia de composicao e a [barra de acoes de Diablo IV](https://www.gamepressure.com/diablo-iv/interface/z0109a5):
orbes de vida e recurso nas extremidades, skills entre eles, fundo escuro e
detalhes metalicos. Os simbolos das skills sao desenhos vetoriais do projeto.

## Informacoes visiveis

- Vida e mana atuais/maximas, com preenchimento dos orbes.
- Q/W/E/R da arma equipada e Space para esquiva, com icone, nome e custo.
- Flash dourado ao usar, estado EM USO, cobertura radial e contagem de recarga.
- Flash vermelho e mensagem ao tentar usar sem mana, em recarga ou durante outro golpe.
- Energia Asura na mesma interface, sem a barra IMGUI antiga sobreposta.
- Nome completo e custo ao passar o mouse sobre um slot.

O painel bloqueia comandos de movimento por clique na sua area. No lobby, E
perto dos terminais pertence a interacao; nao dispara a skill nem consome mana.
O painel narrativo aparece acima da barra, e as instrucoes do lobby ficam no alto.

## Mana

| Habilidade | Custo |
| --- | ---: |
| Avanco Relampago (Q) | 80 |
| Punhos Relampago (W) | 150 |
| Impacto de Choque (E) | 140 |
| Rajada Asura (R) | 280, alem de 100 de energia Asura |
| Esquiva (Space) | 0 (gratuita) |
| Front Area Strike | 100 |
| Sword Slash | 120 |

Os jogadores existentes mantem seus 1500 de mana maxima. A regeneracao padrao
e 4% da mana maxima por segundo, iniciando 1,25 s apos o ultimo gasto. Ambos os
valores podem ser editados no PlayerActor. Nao ha regeneracao depois da morte.
Ataques basicos e dash padrao sao gratuitos. Um dash modificado por item/boon pode usar outro asset DashScript com Mana Cost maior que zero; o fluxo existente valida e cobra esse custo. O asset base nao deve ser alterado para todos os jogadores. A HUD mostra GRATIS para habilidades sem custo.

`Mana Cost`, nome curto, simbolo e cor ficam no asset de cada Ability. O custo
e cobrado uma unica vez, antes da execucao, depois de validar os requisitos.
Falhas por mana, recarga, energia Asura ou bloqueio de controle nao cobram mana.
Repetir a tecla durante a execucao nao dispara nem cobra outro golpe.

## Manutencao e testes

O menu **Tools > Tech Guy > UI > Build and Install Shared HUD** reconstrui o
prefab, aplica os custos padrao acima e instala a HUD nas cenas com jogador em
`Assets/_Project/Scenes`. Use-o para reaplicar o preset; ele substitui ajustes
manuais do prefab e dos custos. Os geradores de lobby e arena tambem conectam o
prefab quando ele existe. Novas skills equipadas ocupam os quatro slots da arma.

Smoke test em copia isolada (o comando encerra o Editor):

```text
Unity.exe -batchmode -projectPath <projeto> -executeMethod SharedHudValidation.Run -logFile <log>
```

O teste percorre as tres cenas e verifica binding de vida/mana, custo unico,
rejeicao por mana e recarga, regeneracao e limite, Asura sem energia, esquiva,
troca de arma e prioridade do E no lobby. Com dispositivo grafico, tambem gera
`Docs/HUD-Playground.png`, `Docs/HUD-NexusLobby.png`, `Docs/HUD-CombatStudy.png`,
`Docs/HUD-casting.png` e `Docs/HUD-no-mana.png` para revisao visual.

O shader RealToon ja apresenta uma referencia de include ausente no projeto;
isso pode deixar objetos do Playground magenta. A validacao registra essa falha
especifica separadamente como `SHARED_HUD_EXTERNAL_SHADER_ERRORS`, e continua
falhando para qualquer outro erro. Nenhum codigo de terceiros foi alterado.
