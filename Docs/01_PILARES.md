# Pilares de gameplay e critérios de veto

| Pilar | Experiência esperada | Sinais de falha |
|---|---|---|
| Controle direto | Input responsivo, compromisso legível, janelas de cancelamento/buffer coerentes | auto-ataque imprevisto; input consumido duas vezes |
| Impacto coerente | Hitstop/feedback, stagger e stance distintos; controle compatível com target | qualquer ataque vira launcher; boss é saco de pancada imóvel |
| Três maneiras de resolver o mesmo swarm | manopla pressiona, lança controla espaço, arco escolhe alvos e se reposiciona | apenas skin, alcance ou DPS separam as armas |
| Ameaças justas | ataques inimigos previsíveis e legíveis, inclusive fora de combos gigantes | hits invisíveis/fora de tela sem aviso; CC infinito |
| Boons transformadores | modificar input, timing/risco, geometria/origem, ordem de skills ou interações | card 'lendário' que só dá bônus de dano |
| Identidade audiovisual consistente | silhueta, telegraph e HUD claros na câmera e resolução reais | efeitos cobrem ataques hostis e personagem |
| Runtime seguro | limite por raiz causal, procs, spawns, VFX e limpeza | explosões recursivas, pools vazando, cenas quebradas |

## Prioridade na dúvida

**Legibilidade → controle/causalidade → game feel → sinergia → espetáculo → volume de conteúdo.**

## Como decidir se um boon é marcante

Heurística **de design, não teste automatizado aprovado**: exigir alteração observável de ao menos duas dimensões entre `input`, `timing/risco`, `geometria/origem`, `sequência` e `interação condicional`; efeitos muito simples só entram como suporte/raridade adequada. A pessoa jogando precisa reconhecer a mudança sem ler código ou logs.

## Veto antes de implementar

Não aprovar proposta que introduza sistema duplicado, altere outra arma sem motivação, confunda reação física com dano, arruíne leitura em swarm, ignore persistência/GUIDs, ou declare diversão comprovada por testes unitários.
