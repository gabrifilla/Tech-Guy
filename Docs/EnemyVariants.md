# Variantes de inimigos

O mesmo `Actor` e `EnemyAI` atendem todas as raridades. `EnemyVariant` aplica um
`EnemyProfile` (ScriptableObject) aos valores base do mob. Cada especie pode ter
seus proprios valores base; compartilhar um perfil nao compartilha vida atual.

## Configurar no Unity

1. Selecione o objeto que contem `Actor` e `EnemyAI`.
2. Use **Tools > Tech Guy > Enemies > Add Variant To Selected Enemy**.
3. No componente `EnemyVariant`, escolha `Normal`, `Magic` ou `Rare` em
   `Assets/_Project/ScriptableObjects/Enemies`.
4. Para manter as tres versoes, crie Prefab Variants do prefab original e escolha
   um perfil para cada uma. Nao e necessario duplicar scripts.
5. Em `EnemyRespawnPoint`, o campo `Enemy Profile` opcional sobrescreve o perfil
   do prefab e do `Initial Enemy`. Vazio preserva a configuracao do prefab.

O menu **Create Example Profiles** gera os exemplos sem sobrescrever assets
existentes. Novos perfis: **Create > Tech Guy > Enemies > Profile**.

| Perfil | Vida | Dano | Movimento | Velocidade de ataque | Bonus |
| --- | --- | --- | --- | --- | --- |
| Normal | 1x | 1x | 1x | 1x | Nenhum |
| Magic | 2x | 1,3x | 1,1x | 1,1x | Nenhum |
| Rare | 4x | 1,6x | 1,15x | 1,2x | Configurado por variante |

Valores sao pontos de partida editaveis. Velocidade de ataque divide o intervalo
entre ataques; nao acelera o clip de animacao. Trocar um perfil por
`EnemyVariant.Configure` preserva a porcentagem de vida e recalcula a partir da
base, sem acumular multiplicadores nem reviver inimigos mortos.

`EnemyRank` permanece separado: Normal/Elite/Legendary/Boss controlam resistencia
a stun, launch e ragdoll. A raridade nova nao altera esses ranks existentes.

## Aura de gelo e outras habilidades

`Rare` e a raridade; Frost e apenas um bonus opcional. Para criar um Rare com
gelo, use o perfil `Rare` e adicione o prefab `EnemyFrostAura` em **Additional
Affix Prefabs** do componente `EnemyVariant`. Outros inimigos podem usar o mesmo
perfil Rare com outros bonus ou combinacoes, sem duplicar os status de raridade.
O perfil Rare fornecido nao aplica gelo automaticamente.

O prefab `Assets/_Project/Prefabs/EnemyFrostAura.prefab` tem `EnemyFrostAura`:
raio de 5 unidades, slow de 30%, consulta a cada 0,15 segundo. Ajuste esses valores
e `Target Layers` no prefab. O player precisa de um collider no objeto do
`PlayerActor` ou em um filho. A consulta inclui triggers.

O slow usa `PlayerArpgStats`, respeitando os outros bonus do player. Funciona
com `CharControlScript` e `ThirdPersonController`. Sair do alcance remove o
efeito na proxima consulta; morte/desativacao da aura remove imediatamente.
Duas auras de 30% deixam 49% da velocidade (0,7 x 0,7); remover uma preserva a
outra. Dash continua usando as regras da propria habilidade.

O raio aparece como gizmo ciano ao selecionar a aura na Scene. O exemplo nao
inclui VFX de gelo em jogo; particulas podem ser adicionadas ao prefab.

Para outros bonus, crie prefabs com componentes de habilidade e inclua-os na
lista `Additional Affix Prefabs` do inimigo. Use `Affix Prefabs` do perfil somente
para bonus compartilhados por todos que usam aquele perfil. Cada inimigo recebe instancias proprias como
filhos. Todos os bonus listados sao aplicados; nao ha sorteio de afixos.

## Verificacao manual em Play Mode

- Compare vida maxima, dano, velocidade do NavMeshAgent e intervalo de ataque
  entre os tres perfis, usando o mesmo mob base.
- Entre e saia do raio de um Rare com Frost, inclusive com varios colliders do player.
- Sobreponha duas auras e mate/desative cada inimigo, verificando que o slow
  restante e removido corretamente sem apagar passivas do player.
- Teste respawn com override de perfil e sem override.
- Chame Configure duas vezes com o mesmo perfil: os status nao devem acumular.
