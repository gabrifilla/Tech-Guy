# Ataques básicos e incursões por salas

- **Segurar o botão esquerdo:** atacar continuamente na direção do cursor, sem gastar mana. Funciona com manopla, arco e lança e enquanto Q/W/E/R estão em recarga.
- **Botão direito:** mover. Shift + direito continua permitindo um ataque parado.
- **Q/W/E/R:** habilidades. Durante a execução de uma habilidade ou dash, o ataque básico aguarda; recarga de habilidade não bloqueia o básico.

O FirstSector é a primeira fase, com **três salas fixas conectadas**: acesso, relé e guardião. Entrar em uma sala fecha as portas e ativa somente seus inimigos. Matar todos abre uma escolha de três bênçãos diferentes. A saída só abre após escolher uma delas, com clique ou teclas **1/2/3**. O portal final só libera após concluir a terceira sala e escolher a recompensa. Morte ou extração levam ao Nexus; os bônus e transformações são descartados, mas a arma escolhida no arsenal permanece salva.

O layout ainda não é procedural e não há ramificações nem múltiplos andares. As ofertas variam a cada incursão; sempre há uma opção que altera habilidade enquanto alguma estiver disponível. Cada bênção pode ser adquirida uma vez nesta fase. As recargas são restauradas ao escolher a recompensa; cada sala recupera 20% de mana, mas não restaura vida automaticamente.

| Bênção | Efeito durante a incursão |
| --- | --- |
| Núcleo de força | +25% de dano de básicos e skills |
| Mãos velozes | +25% de velocidade dos básicos |
| Fluxo arcano | +15 pontos percentuais de redução de recarga |
| Coração de ferro | +100 de vida máxima e cura completa |
| Foco eficiente | Q custa 25% menos mana e recarrega 35% mais rápido |
| Disparo prismático (arco) | Substitui Q por cinco flechas perfurantes em leque |
| Nova de impacto (manopla/lança) | Substitui Q por dano circular em 4m; esse Q não gera energia Asura |

Foco eficiente permanece aplicado se Q for transformado depois. Os assets originais de arma e skills não são modificados: `RunBoons` cria e destrói suas próprias cópias em runtime. A barra de skills acompanha a transformação. Portas temporárias usam obstáculos de NavMesh sobre a navegação existente; dash e avanço respeitam esses limites.

Validação em projeto isolado:

```text
Unity.exe -batchmode -nographics -projectPath <copia> -executeMethod RunLoopValidation.Run -logFile <log>
```

O teste cobre básicos das três armas sem mana e durante recarga, cadência, bloqueio durante cast, três salas, portas, escolha única, transformação do Q, preservação dos assets, extração, replay limpo e morte.

## Feedback de combate

Inimigos ativos exibem barra de vida, vida atual/máxima, categoria e afixos sobre a cabeça, sem precisar selecioná-los. Ao receber dano real, a barra pisca e uma faixa dourada mostra a vida perdida; um marcador de impacto e o número de dano aparecem no alvo. Golpes letais mostram **ELIMINADO**, com o número permanecendo por um instante após a morte. Básicos, projéteis e skills compartilham o evento de dano efetivo do `Actor`, portanto golpes que erram não mostram confirmação de acerto. As anotações de raridade anteriores são substituídas pela apresentação conjunta para evitar sobreposição.

## Resultado da validação

Unity 6000.5.10f1: **86 verificações passaram em Play Mode**, incluindo básico sem mana nas três armas, cadência, básico durante recarga, feedback de vida e dano, portas bloqueando navegação, transformação com dano atrás do personagem, acumulação de bônus, preservação dos assets, extração e reset por morte/replay. Log local: `Logs/run-loop-feedback-tests.log`. Compilação sem erros C# e `git diff --check` aprovado. Execução com `-nographics`: o acabamento visual e a disposição dos elementos não foram revisados por captura de tela.
