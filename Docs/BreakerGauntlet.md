# Manopla — estilo Asura

A manopla padrao ja equipa as quatro habilidades. Abra CombatStudy pelo menu
Tools > Tech Guy > Scenes > Open Combat Study e entre em Play Mode.
Mire com o mouse; as teclas padrao sao Q/W/E/R. A barra inferior mostra energia
e recargas e acompanha os bindings configurados no AbilityHolder.
Essa barra agora faz parte da [HUD compartilhada](PlayerHUD.md). Q/W/E/R
custam, respectivamente, 80/150/140/280 de mana; R tambem exige a energia Asura.

| Tecla | Habilidade | Comportamento | Recarga apos execucao |
| --- | --- | --- | --- |
| Q | Avanco Relampago | Avanco de ate 2,8 m limitado pelo NavMesh e dois socos | 3 s |
| W | Punhos Relampago | Seis socos curtos e um finalizador | 5 s |
| E | Impacto de Choque | Dois impactos; o segundo causa dano de postura e stun | 6 s |
| R | Rajada Asura | Consome 100 de energia: 16 socos e finalizador com knockback | 16 s |

Q e W sao Impulso; E e Choque. Cada uso gera 10 de energia; alternar entre
categorias gera 25. A energia e gerada ao usar, mesmo sem acertar, limitada a 100.
R sem energia nao executa nem entra em recarga. A rajada dura 2,9 s, trava a
direcao escolhida e precisa ser posicionada antes do uso. Recargas respeitam
os modificadores de atributos existentes; os valores da tabela sao os basicos.

Durante a execucao, movimento, ataque basico, dash e outras skills ficam
bloqueados. Morte, desativacao ou troca de arma encerram a sequencia e restauram
o controle; morte e troca de arma tambem zeram a energia. Socos curtos nao
empurram os alvos para fora da rajada. As reacoes continuam sujeitas a postura
e resistencias do inimigo; nao ha garantia de stun em elites.

Inspiracao: [guia oficial do Breaker](https://www.playlostark.com/en-gb/news/articles/breaker-breakdown).
Esta e uma adaptacao do ritmo de socos e da energia por alternancia do Asura ao
sistema de quatro skills do projeto. A identidade foi condensada no R; nao
implementa os dois recursos Stamina/Shock, o escudo X ou todos os sistemas do
Lost Ark. Usa as animacoes Attack/Attack2/Attack3 e efeitos procedurais do
projeto, com cadencia e cores proprias; nao inclui animacoes novas de captura.

## Ajustes

Os quatro assets `Breaker*.asset` ficam em
`Assets/_Project/ScriptableObjects/Abilities/Weapon`. Cada golpe permite editar
tempo, dano, alcance e reacao. As referencias estao no asset original
`Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset`.
Nenhuma cena precisa ser regenerada. O estado de combate e a HUD sao criados
no personagem ao equipar essa manopla; nao sao compartilhados pelos assets.

## Validacao

`AsuraMomentumValidation.Run` verifica energia, alternancia, limite, consumo,
reset e isolamento entre personagens. Pode ser executado como C# puro ou via
`Unity.exe -batchmode -nographics -projectPath <projeto> -executeMethod AsuraMomentumValidation.Run -quit`.

Conferir em Play Mode: Q perto da borda; alternancia Q/E/W/E; R antes/depois de
100; impedir sobreposicao ao pressionar varias skills; morte e troca de arma
durante a rajada; retorno de movimento e velocidade da animacao ao terminar.

## Balanceamento inicial do Setor 01

Multiplicadores somados por execucao: Q 1,40x; W 3,38x; E 2,70x; R 10,52x.
Os valores multiplicam o dano do personagem/arma, antes de criticos e resistencias.
Q prioriza mobilidade; W concentra dano em um alvo; E tem stun de 0,65 s no
segundo impacto e recarga maior; R exige energia cheia e posicionamento.

A manopla usa rastros estreitos na altura dos punhos e pequenos aneis de impacto.
E termina com uma onda no chao; R combina os socos rapidos com um impacto de
aneis duplos. Nao desenha mais o triangulo generico por baixo de cada golpe.
O indicador generico das outras armas agora e uma faixa curva estreita.
