# Pausa, animações e Asura

## Pausa

Esc pausa e retoma nas cenas com jogador. Tempo de combate, recargas, projéteis, movimentação, animações e áudio param; os comandos e a seleção de recompensas ficam bloqueados. Voltar ao menu inicial e sair do jogo têm confirmação, pois encerram a incursão atual. O tutorial também oferece a opção de pular para o Nexus. Ao retomar, é preciso soltar o botão de ataque antes de iniciar outro básico, evitando ataques acidentais ao clicar em Continuar.

`PauseMenuUI` pertence ao jogador da cena. Não há objeto persistente nem singleton. O estado anterior do relógio e do áudio é restaurado ao continuar, desabilitar o componente ou trocar de cena.

## Animações das habilidades

`SkillAnimationPlayer` usa Playables e acompanha o relógio real da habilidade: a pose de contato coincide com o dano, e a recuperação acontece depois. Cadência e repetições modificadas durante a run usam o mesmo cronograma. O Animator volta à locomoção quando a execução termina ou é cancelada.

`SkillAnimationLibrary` contém socos esquerdo/direito, impacto descendente, disparo de arco, disparo elevado, estocada e varredura. Os socos e o arco são derivados de clipes já disponíveis; estocada, varredura e impacto descendente usam curvas próprias de pose humanoide. Os assets originais e os arquivos de terceiros são preservados. Eventos de hitbox foram removidos das cópias de skill: dano é aplicado exclusivamente pelo combate, sem ataques básicos extras disparados pela animação.

## Asura

| Parâmetro | Antes | Agora |
|---|---:|---:|
| Energia | 100 | 100 |
| Mana | 280 | 0 |
| Execução base | 2,9 s | 1,46 s aproximadamente |
| Recarga após execução | 16 s | 8 s |
| Golpes rápidos | 16 × 0,42 | 10 × 0,9 |
| Finalizador | 3,8 ×, frontal | 6 ×, circular, raio de 4 m |
| Multiplicador total base | 10,52 × | 15 × |
| Proteção durante execução | Nenhuma | 85% de redução do dano recebido |

Os multiplicadores se aplicam ao dano da arma antes dos demais modificadores da build. A rajada acompanha o cursor com velocidade angular limitada. Após 0,25 s, uma esquiva válida aciona o finalizador principal imediatamente e interrompe o restante da sequência. Réplicas de modificadores ainda não executadas são perdidas ao cancelar. Esquiva indisponível ou sem recursos não interrompe a skill. A proteção termina ao finalizar, esquivar, trocar de arma, morrer ou desabilitar o componente.

O finalizador causa 100 de dano de postura e atordoa por 1,1 s quando quebra a postura, respeitando as regras de reação dos inimigos. Socos rápidos não empurram os alvos para fora da sequência.

## Ferramentas

- `CombatPolishBuilder.Build`: gera/atualiza os clipes próprios, biblioteca e dados do Asura via APIs do Editor, preservando GUIDs existentes.
- `CombatPolishValidation.Run`: integração de pausa, poses humanoides, dano sem duplicação, proteção/cancelamento/limpeza do Asura, recompensa e retorno ao menu.
- `CombatPolishPreview.Run` com `-combatPreview`: capturas reais da pausa e das poses em Editor isolado.
- `WeaponModifierValidation.Run`: regressão de dano das 12 skills com todos os modificadores no máximo.

As validações e capturas encerram o Editor ao terminar; execute em uma cópia isolada do projeto.

## Indicadores de impacto

As áreas de básicos e skills são contornos no chão criados no instante em que o dano é consultado. Caixas usam o mesmo centro, orientação, largura e profundidade de `OverlapBox`; esferas usam um círculo no centro X/Z de `OverlapSphere`, com o mesmo raio. O desenho considera offsets e os tamanhos finais após modificadores, incluindo réplicas, chuva e nova circular. A altura de apresentação usa o plano do jogador.

Os contornos desaparecem sem aumentar ou diminuir a área. Rastros de soco continuam como detalhe de impacto; os antigos anéis decorativos que sugeriam outra área foram removidos. Flechas mantêm seus projéteis visíveis. Os clipes existentes continuam sincronizados pelo cronograma das habilidades.

`ControlsCombatValidation.Run` verifica persistência e entrada remapeada, rascunhos independentes, geometria dos indicadores e bônus de movimento. Executar em batch sem `-quit`; encerra automaticamente. `RunLoopValidation.Run` inclui rejeição de ataques no vazio e fora de alcance.

Validação desta revisão (Unity 6000.5.10f1, batch, sem renderização):

- Compilação concluída; `ControlsCombatValidation.Run` passou.
- `WeaponModifierValidation.Run`: 251 verificações aprovadas, incluindo as 12 skills com níveis máximos; Asura: 15 verificações aprovadas.
- `RunLoopValidation.Run`: atravessou os básicos das três armas e as rejeições por vazio/alcance, mas a suíte completa falhou em `Only current room enemies active`. O fluxo completo das salas não está validado nesta revisão.
- Aparência e legibilidade dos efeitos e do remapeamento ainda precisam de conferência visual em Game view; os testes acima não renderizam a cena.
