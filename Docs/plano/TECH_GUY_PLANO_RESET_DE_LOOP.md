# Tech Guy — Plano "Reset de Loop"

**Data:** 09/11/2026 · **Status:** direção recomendada; arma, artefatos, duração, sala de descanso e NPCs decididos (seção 7).

## 1. Diagnóstico (do desenvolvedor)
- Jogar é divertido, mas as runs ficam repetitivas.
- Não há progressão: falta motivo para jogar a próxima run.
- Não há identidade visual estabelecida, embora a direção esteja clara na cabeça do autor.
- A visão do jogo continua intacta.

Conclusão: o núcleo (combate) funciona. O que falta é o **loop ao redor**: meta-progressão, variedade entre runs e identidade visual.

## 2. Estrutura de progressão proposta pelo desenvolvedor

1. **Universos = fases.** Cada universo tem estilo de arte próprio. Termina-se o Universo 1 para liberar o 2; a run seguinte passa por 1 → 2 e, ao vencer o 2, libera o 3, e assim por diante (modelo Dead Cells/Isaac).
2. **Cada arma pertence a um universo** e tem personalidade própria.
3. **Rapport com as armas** (inspirado em Lost Ark): a afinidade sobe ao longo das runs; cada nível libera algo, e no nível máximo a arma entrega um **artefato**.
4. **Itens e NPCs ao estilo Hades:** itens obtidos nas runs, interações que liberam conteúdo novo.
5. **Universos planejados:** (1) tecnológico, (2) cartoonizado, (3) noir em preto e branco. O **protagonista mantém o mesmo estilo** em todos.

## 3. Avaliação

| Ideia | Ponto forte | Risco | Recomendação |
|---|---|---|---|
| Armas como NPCs com rapport | Une personalidade das armas, meta e motivo para trocar de arma. | Escrita e diálogos custam; risco de virar só +X% | **Adotar.** Diálogo em texto curto no início, sem voz. Haverá também NPCs resgatados nas runs (seção 7). |
| Artefatos do rapport | Recompensa de longo prazo clara | "Melhorar dano" contraria o pilar anti +X% | **DECIDIDO:** coleção estilo Keepsakes (seção 7). Efeitos direcionam a build, não só números. |
| Arma ligada ao universo | Lore forte, e a arma vira recompensa de progressão | Prender a arma ao universo mata a variedade | **DECIDIDO:** a arma vem do seu universo, mas é escolhida no Nexus e usada em todos os universos até o fim da run. |
| Universos sequenciais | Meta clara, sensação de avanço | Repetir o Universo 1 em toda run é justamente a queixa atual | **DECIDIDO:** até ~10 min por universo, variando com a build. Variações de rota/sala por universo e, mais tarde, modificadores de dificuldade opcionais. |
| Arte diferente por universo | Identidade fortíssima (conceito tipo Spider-Verse, sem copiar) | Custo de 3 conjuntos de arte + inimigos | Comportamentos de inimigo **reaproveitados** e reskinados por universo. Arte de cada universo feita só quando ele for jogável. |
| Player constante | Âncora visual e de legibilidade | Precisa se destacar em fundos muito diferentes | Contorno/rim light e paleta do player fixos e testados contra os três estilos. No noir, o player em cor é um gancho visual forte. |

## 4. Modelo de meta-progressão recomendado (MVP)

Começar com **duas** moedas, não mais:
- **Fragmentos de Dados** (comum): ganhos em toda run, inclusive na morte. Gastos num terminal do Nexus.
- **Rapport por arma:** sobe ao usar a arma (salas vencidas, boss, tempo de uso) e com **presentes** raros achados nas runs.

**Rapport — níveis por arma:** ver o esboço na seção 7.

**Terminal do Nexus (Fragmentos) — 6 a 8 itens iniciais:** +1 reroll de oferta; começar a run com 1 boon; sala-loja passa a aparecer; +1 cura entre universos; revelar o tipo de recompensa nas portas; +1 chance de oferta rara. Nada que seja só "+10% dano" em sequência.

**Liberação de armas:** vencer o boss do Universo 1 libera o Universo 2 **e** a Lança. Vencer o boss do 2 libera o 3 **e** o Arco.

**Fim de run:** mostra Fragmentos ganhos, rapport ganho e o próximo desbloqueio mais próximo ("faltam 40 para…"). Isso é o que cria o "só mais uma run".

## 5. Fases de execução

**Fase 0 — Arrumar a casa.** Commit/estado do git; isolar os testes falhos; pausar Lança/Arco/2.5D/prólogo; relatórios de uma página; **auditoria do que já existe** de save, Nexus, desbloqueios, loadout, boss e resultado de run. *(Prompt pronto.)*

**Fase 1 — Motivo para jogar.**
- 1a: Universo 1 com fim claro (boss vencível → tela de vitória → retorno ao Nexus).
- 1b: Fragmentos + primeiro NPC resgatável no Universo 1, que abre o terminal do Nexus + tela de fim de run com progresso.
- 1c: Rapport da Manopla (níveis 1–5, artefatos nos níveis 2, 4 e 5) + presentes + slot de artefato no Nexus.
- 1d: Universo 2 em **gray-box** (salas atuais, inimigos reaproveitados com outra cor) + **sala de descanso** entre universos (troca de artefato, cura) + Lança liberada ao vencer o Universo 1.

**Fase 2 — Runs diferentes entre si.** ~10 boons "barulhentos" para a Manopla; portas com tipo de recompensa; elite opcional, sala-desafio e sala-loja; elites com modificadores.

**Fase 3 — Identidade visual (em paralelo).** Bíblia de arte escrita para os três universos + regra do player. Depois, **uma sala de referência do Universo 1** (sala + player + 1 inimigo) no padrão final, aprovada pelo autor como régua.

## 6. Regras do processo novo
- Toda etapa termina com um **build jogável** e o autor joga antes da próxima.
- Relatório de **uma página**: feito / não feito / não testado / o que jogar.
- Protótipo de design pode ser descartável; não preservar por padrão o que não foi aprovado.
- Veto de boon: se a mudança não aparece numa gravação de 10 s, não entra.
- Uma frente por vez (a arte da Fase 3 é a única exceção paralela).

## 7. Decisões tomadas (09/11/2026)
1. **Arma:** escolhida no Nexus e usada **em todos os universos até o fim da run**. Troca no meio da run fica para avaliação futura.
2. **Artefatos:** **coleção ao estilo dos Keepsakes de Hades**. Transformar a arma fica como possibilidade futura.
3. **Duração:** média de **até ~10 min por universo**, variando com a força da build.

### Especificação de artefatos (proposta)
- **Fonte:** cada arma entrega artefatos em mais de um nível de rapport; o nível máximo entrega o **artefato assinatura** da arma. Presentes/eventos raros nas runs entregam outros. Assim a coleção tem tamanho suficiente para ser uma escolha real.
- **Uso:** equipar **1 artefato** no Nexus antes da run. **DECIDIDO:** pode ser trocado na **sala de descanso entre universos**, como em Hades.
- **Efeito:** artefatos **direcionam a build** em vez de dar só +X%. Exemplos: começar a run com um boon de certo tipo; a primeira oferta de cada universo sempre traz um Protocol; elites largam presente de rapport; uma revivida por run; ofertas de elite com uma opção a mais; Fragmentos extras por sala sem levar dano. Bônus numérico simples só como item comum e minoritário.
- **MVP:** 1 slot, sem rank de artefato, 4–5 artefatos (os 3 da Manopla + 1–2 de eventos).

### Rapport da Manopla — esboço revisado
| Nível | Libera |
|---|---|
| 1 | Primeira conversa (texto) |
| 2 | Artefato comum + 2–3 boons da Manopla no pool |
| 3 | Conversa + mais boons no pool |
| 4 | Segundo artefato |
| 5 (máx.) | Artefato assinatura da Manopla |

### Duração — implicações de design
- Dimensionar cada universo para ~10 min com build **média**; build forte termina antes (recompensa natural), build fraca passa um pouco.
- Boss de universo: ~1,5–2,5 min desse total.
- Registrar o tempo por universo na tela de fim de run, para calibrar com dados reais.

### Sala de descanso entre universos (DECIDIDO)
Sala curta, sem combate, entre o boss de um universo e o início do próximo. MVP: trocar artefato + cura parcial. Depois: encontro com NPC, conversa com a arma, loja ocasional. Também marca a transição de estilo de arte entre universos.

### NPCs (DECIDIDO: haverá NPCs além das armas)
- Personagens **encontrados ou resgatados durante as runs** passam a morar no Nexus, e cada um traz algo novo: um serviço, uma loja, desbloqueios ou conversas. Referência de princípio: resgates que povoam o hub (Enter the Gungeon) e conversas que avançam a cada retorno (Hades).
- **Cada resgate é uma recompensa de progressão por si só:** o Nexus cresce porque você jogou, sem virar burocracia (cada NPC = uma função clara).
- **MVP recomendado:** **um** NPC resgatado no Universo 1, garantido numa das primeiras runs. Ao chegar ao Nexus, ele **abre o terminal de Fragmentos**. Assim o primeiro serviço do hub nasce de um evento da run, não de um menu que já estava lá.
- Diálogo em texto curto, sem voz no MVP. Rapport fica para as armas; NPCs podem ter progressão mais simples (conversas que avançam por visitas/eventos).

### Ainda em aberto
- Quem é o primeiro NPC (identidade, lore, onde é resgatado) e o tom das conversas.
- O terminal fica fechado até o primeiro resgate ou existe numa versão mínima desde a primeira run? *(Recomendado: resgate garantido na 1ª ou 2ª run, para o terminal abrir cedo.)*
