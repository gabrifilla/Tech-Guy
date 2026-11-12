# Tech Guy — Roteiro das próximas etapas (passagem de contexto)

**Gerado em:** 11/11/2026 · **Branch:** `gameplayOverhaul` · **Para:** começar uma conversa nova com contexto limpo.

> **Como usar numa conversa nova:** peça para ler, nesta ordem, `CLAUDE.md` (regras vigentes), este arquivo, `Docs/STATUS.md` (histórico detalhado e números) e `Docs/plano/TECH_GUY_PLANO_RESET_DE_LOOP.md` (visão das fases). A narrativa está em `Docs/00_VISAO.md` → Narrativa. Depois, ataque **uma etapa por vez**, na ordem abaixo.

---

## 1. Como trabalhamos (regras que valem para todas as etapas)
- **Uma frente por vez.** Cada etapa termina com algo **jogável**; o desenvolvedor joga antes de seguir.
- **Relatório de uma página, em PT-BR:** Feito / Não feito / Não testado / O que jogar para validar. Atualizar `Docs/STATUS.md` e o plano; não criar arquivos em `Docs/decisions/`.
- **Decisões do desenvolvedor:** quando houver escolha relevante (design, escopo, prioridade, balanceamento), perguntar **com opções clicáveis**, a recomendação em primeiro e o trade-off de cada uma. Detalhes óbvios: decidir e avisar.
- **Testes:** rodar só os testes focados na mudança, pelo Unity MCP. A suíte completa é do desenvolvedor.
- **Commit e push só quando o desenvolvedor pedir.**
- **Preservar:** combo Titanic Fists → Conductor → Detonation; salas RUN-EXP-09/09B; tela de recompensas C05; controles (WASD, mouse mira, botão esquerdo básico, botão direito especial/Asura, Q/E/R, dash remapeável); GUIDs/.meta, saves, PlayerPrefs e IDs de boons.
- **Pausados:** Lança, Arco, arte 2.5D/pictórica, prólogo, protótipos G1/G2/G3.

## 2. Onde estamos (11/11/2026)
- **Aprovado em playtest:** Fase 1a (vitória do Universo 1), 1b/1b.1 (Fragmentos, NPC Desconhecido, terminal, economia), passe de inimigos (aggro, recuo limitado, contorno de cobertura), salas-pacote, frente de ameaça (passes 1 a 5), mana/HUD/dash.
- **Commits:** `e5ffb3b7` (ameaça) é o último. **Não foi feito push dele.**
- **Ainda sem commit (no disco):**
  - frente de mana/HUD/dash: `playerManaEnabled` desligado, orbe de mana escondido, dash 2,0 s, `ManaSystemTestDefaults` e `PlayerManaSwitchPlayModeTests`;
  - caminhos mais curtos: `ManualRunRandomizer.Compact`.
  Pedir ao desenvolvedor se quer commitar antes de começar.
- **Números atuais do combate** (detalhes no STATUS):
  - **Jogador:** vida 500 (700 com Vida reforçada), velocidade 5,0, dash com cooldown de 2,0 s, mana desligada.
  - **Dano:** base 12 × perfil. Rush, Grunt e Mirror 24; Heavy e Charger 45; Swarm 6; Fragile 18; Shooter 22; Sniper 36; Bomber 30.
  - **Vida:** Grunt 150; Rush 90; Charger 165; Heavy 600; Mirror 180; atiradores e controle 120; Swarm 45.
  - **Preparação dos golpes comuns:** 0,25–0,35 s. Tiros diretos: aviso de 0,8 s.
  - **Até 5 atacantes melee ao mesmo tempo;** Swarm e Rush fora desse limite nas salas-pacote.
  - **Salas-pacote em rodízio:** Fornalha (Heavy + 9 Swarm + HazardCaster), Anzol (Charger + Bomber + 4 Rush), Mira (3 Grunt + 2 Shooter + 1 SpreadShooter). Elite e boss não mudam.
- **Ferramentas de Editor:** menu **Tech Guy ▸ Protótipo ▸ Salas-pacote** (ligado; desligar para comparar) e **Nomes dos inimigos** (ligado: rótulo com o nome do arquétipo, colorido por papel). F9 pula para o boss.

---

## 3. Etapas programadas, em ordem

### Etapa 1 — Playtest dos caminhos mais curtos *(pendente; nada a implementar)*
- O que mudou: corredores com média de 58 → 36 m (mediana 68 → 41 m).
- Perguntas: a caminhada ficou curta o bastante? Alguma sala ou corredor ficou sobreposto, apertado ou estranho, principalmente nas bifurcações?
- **Pendência de validação:** os testes visuais 09/09B falham com "Failed to capture screen shot" também no último commit, sem as mudanças. A causa provável é o Game view num segundo monitor. Com o Game view no monitor principal, rodar `Distribution09B*Gauntlet` e `Spatial09*Gauntlet`. As variantes L e Ring recusaram uma skill da Manopla na linha 84 de `RunExp03FirstSectorTests.Spatial09Gameplay.cs`; confirmar se foi foco perdido ou efeito da compactação.

### Etapa 2 — Hooker refeito *(pedido do desenvolvedor)*
- **Gancho como projétil visível**, no estilo do hook do Thresh (LoL): dá para ver vindo e desviar; lançamento mais rápido; puxão mais rápido até o Hooker, de preferência para perto dos aliados ou de zona de perigo. Substitui o puxão atual (`EnemyCombatActions.HookLine`, `PlayerActor.BeginExternalPull`).
- **Útil fora do gancho:** enquanto o gancho recarrega, ataques de média distância com a corrente/gancho (golpes, não puxão).
- **Imprevisível:** o gancho não sai assim que recarrega; espera um tempo aleatório.
- **Manter** a janela anti-loop de 2,5 s depois de um puxão (`PlayerActor.ExternalPullRecovery`).
- Testes que tocam no puxão: `RunFrontCriticalPlayModeTests` (RUN00), `Spatial09ReinforcementsConcaveEdgesAndHookRecovery`.

### Etapa 3 — Redesenhos de inimigos já aprovados (revisão de 10/11/2026)
- **Fragile → Volátil:** explode ~0,8 s depois de morrer (área pequena); pune AoE descuidado no meio do grupo.
- **Mirror → Guarda:** frente imune a golpe; abre com quebra de postura ou pelas costas.
- **SpreadShooter → modificador do Shooter** (deixa de ser arquétipo próprio).
- **Emissor e Fragmento → só skins**, com peso único no pool (hoje dobram Sniper e Rush).
- **Shooter "Rajada"** (novo): metralhadora na cauda, laser em rajadas. **Depende do modelo** que o desenvolvedor está fazendo no Blender (exportar com um ponto/bone na ponta da cauda para a origem dos tiros).
- Revisar os pacotes de sala quando esses inimigos existirem.
- **Em aberto do playtest:** ainda dá para fugir andando sem dash. Opções levantadas: investida curta nos comuns, jogador mais lento, ou deixar o dash (2,0 s) resolver.

### Etapa 4 — Fase 1c: rapport da Ico (Manopla)
- Rapport níveis 1–5, subindo com o uso da Manopla (salas, boss, tempo) e com **presentes** raros nas runs.
- Nível 1: primeira conversa; 2: artefato comum + 2–3 boons da Manopla no pool; 3: conversa + mais boons; 4: segundo artefato; 5: artefato assinatura.
- **Artefatos** estilo Keepsakes: 1 slot no Nexus, 4–5 no MVP, efeitos que direcionam a build. Ideias: começar com um tipo de boon; primeira oferta de cada universo com Protocol; elites largam presente; uma revivida por run; oferta de elite com opção extra; Fragmentos por sala sem dano.
- **Texto:** primeiras falas da Ico (smug, sarcástica, solidária; easter egg em homenagem ao *ICO* da Sony, sem assets nem marca) e reescrita das falas do Desconhecido como NPC inacabado (hoje `TODO-TEXTO` em `MetaUpgrades.cs` → `MetaAllies`). Ele ganha uma camada nova a cada retorno ao Nexus.
- Observar: com vida 500, a cura da sala de reparo e do resgate passou a valer mais; pode pedir ajuste de economia.

### Etapa 5 — Fase 1d: Universo 2 em gray-box
- Universo 2 (cartoonizado) com as salas atuais e os inimigos reaproveitados em outra cor.
- **Sala de descanso** entre universos: troca de artefato e cura parcial.
- **Lança liberada** ao vencer o Universo 1 (e Universo 2 liberado). Corrigir junto os testes `ManualPublishedRunPlayModeTests`, que ainda desbloqueiam armas pelo método antigo (flags no PlayerPrefs) e falham desde a 1b.
- Depois da 1d: tela **"Escolha sua próxima jornada"** ao fim de um universo (2–3 percursos lado a lado com rota em miniatura; reaproveitar `RouteStripModel`).

### Etapa 6 — Fase 2: runs diferentes entre si
- ~10 boons "barulhentos" da Manopla (visíveis em 10 s de vídeo).
- **Boons de dash:** cargas extras, golpes básicos que reduzem o cooldown do dash e similares.
- Portas mostrando o tipo de recompensa; elite opcional; sala-desafio; sala-loja.
- **Elite "Comandante":** arquétipo + modificador + aura que fortalece os comuns por perto (hoje a elite é o variante legado `Magic_Haste`).
- **Caminhos com relevância:** Fragmentos, emboscadas ou eventos nos corredores.

### Etapa 7 — Frente de bosses (separada)
- Cada boss com estilo único e épico. Referências: God of War, Phantom Blade Zero e Wukong adaptados ao isométrico, além de Lost Ark e Diablo, sem copiar.
- **Boss do Universo 1:** ponto de partida = direção A+C da revisão (combos sem pausa morta + a cada terço núcleo exposto, enxame e anel de zona; sem stun-lock). Diagnóstico atual: usa golpes do Heavy e do Charger, fica 1,4 s parado entre ciclos, luta sozinho e não tem fase nova.
- **Boss final (conceito):** quer destruir tudo e todos, inclusive a si mesmo ("um preço baixo a se pagar"); golpes que mudam de universo; ondas de choque que fazem o jogador alternar entre universos.

### Etapa 8 — Fase 3: identidade visual (pode correr em paralelo)
- Bíblia de arte dos três universos (tecnológico, cartoonizado, noir P&B) e a regra do protagonista com estilo fixo (contorno e rim light testados nos três).
- Uma sala de referência do Universo 1 no padrão final, aprovada como régua.

---

## 4. Para revisitar depois
- **Mana:** desligada para o jogador, sistema intacto. Revisitar quando Lança e Arco voltarem (cada arma com recurso próprio, como o Asura).
- **Fugir andando** ainda é fácil (ver Etapa 3).
- **Economia** de Fragmentos e cura, com a vida mais baixa.

## 5. Dívida técnica e ambiente
- **Suíte completa** não rodada depois das últimas mudanças. Falhas conhecidas:
  - testes que pedem Arco ou Lança, bloqueados por progressão (ver Etapa 5);
  - variantes do Arco instáveis nas salas 09B (o alvo leva 18 de dano antes da checagem);
  - testes de input, pausa e mapa que dependem da ordem de execução;
  - 44 testes em quarentena (`grep -r QUARENTENA-F0 Assets`).
- **Captura de tela nos testes** falha com o Game view no segundo monitor.
- **O projeto roda com recarga de domínio desligada** (`EnterPlayModeOptions` = 3): estado estático sobrevive entre Plays.
- **Input no Editor:** `Core/EditorInputDeviceGuard.cs` religa teclado e mouse reais no Play e remove dispositivos virtuais vazados de testes. Sintoma antigo: só o clicar-para-mover funcionava.
- **Unity MCP:** se a conversa começar com o Unity fechado, o MCP falha; reconectar pelo `/mcp` depois de abrir o Unity.
- **Fast Script Reload:** às vezes acusa erros de recarga a quente (avisos de API obsoleta) sem afetar a compilação normal.
- Testes mudam artefatos em `Docs/validation/artifacts/`: restaurar com `git checkout -- Docs/validation` antes de commitar.

## 6. Decisões ainda em aberto
- Nome final do Desconhecido, quando ele for "terminado".
- Quem é o Arco (personalidade e universo).
- Detalhes do Hooker novo (alcance, velocidades, quais ataques de média distância).
