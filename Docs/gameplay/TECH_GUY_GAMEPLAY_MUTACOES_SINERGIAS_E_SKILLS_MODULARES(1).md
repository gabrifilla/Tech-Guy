# TECH GUY — REFORMA ESTRUTURAL DE GAMEPLAY
## Ataques mutáveis, sinergias emergentes, skills modulares e progressão por run

> **Tipo:** Documento-mestre de visão, design e requisitos para planejamento técnico.
> **Versão:** 1.0 — 26/10/2026.
> **Status:** Proposta de direção para validação. **Não equivale a autorização para reescrever o projeto inteiro.**
> **Público:** Codex, Claude e outros agentes de desenvolvimento, game design, UX, VFX, engenharia e QA.
> **Engine:** Unity 3D / C#.
> **Prioridade:** Preservar o combate existente que funciona; transformar progressão, boons, habilidades e representação visual de maneira incremental e verificável.

---

# 0. INSTRUÇÕES OBRIGATÓRIAS PARA QUALQUER IA QUE RECEBER ESTE DOCUMENTO

## 0.1. Como interpretar esta especificação

Este documento expressa a **intenção de produto** e a experiência desejada pelo criador do jogo. É uma referência de direção — não uma alegação sobre a estrutura atual do repositório.

Use os termos abaixo rigorosamente:

- **DEVE / NÃO DEVE:** requisito obrigatório da direção proposta.
- **DEVERIA:** forte recomendação, que admite alternativas justificadas por análise.
- **PODE:** hipótese/opção para prototipagem.
- **A VALIDAR:** ponto que precisa ser decidido a partir de inspeção do jogo, protótipo ou playtest.
- **EXEMPLO:** conteúdo ilustrativo; **não é** catálogo final, código existente, balanceamento aprovado nem nome obrigatório de classe.

**Não trate os exemplos de boons, skills, níveis, tiers, datas ou nomes de classes deste documento como assets ou APIs já existentes.** Antes de criar qualquer classe, verificar equivalentes no projeto. Não inventar caminhos ou nomes de skills/agentes do repositório.

## 0.2. Ordem de trabalho obrigatória

1. Ler as instruções efetivamente existentes no repositório, incluindo `AGENTS.md` e quaisquer instruções locais aplicáveis.
2. **Descobrir, ler e aplicar as skills de design que o criador já adicionou ao repositório**, especialmente as relevantes a game design, combate, direção de arte, UI/UX, VFX, HUD, telemetria e documentação. Descobrir caminhos reais em vez de supor nomes.
3. Auditar sistemas existentes de armas, básico, combos, skills Q/W/E/R, boons, salas, recompensas, HUD, personagens, VFX, persistência, testes e ferramentas.
4. Identificar decisões já aprovadas ou implementadas que esta proposta deve preservar. Não presumir que o estado atual corresponde à descrição histórica deste arquivo.
5. Produzir **análise de lacunas + requisitos + design técnico + plano de tarefas**, separados ou conectados à estrutura documental já adotada no projeto.
6. Sugerir uma **vertical slice da Gauntlet** antes de migrar Spear, Bow ou todo o catálogo de boons.
7. Implementar apenas após a etapa de design/escopo ser aceita no fluxo de trabalho do projeto.
8. Validar em Unity por meio do MCP da Unity quando disponível e operacional; além de testes automatizados, realizar inspeções visuais, capturas e playtests. Se o MCP falhar, informar o bloqueio claramente, sem afirmar que a validação visual ocorreu.
9. Documentar os resultados de cada avaliação ou marco relevante em `.md`, incluindo evidências, regressões e decisões pendentes; não criar relatórios para perguntas triviais.

Respeitar a organização real do projeto, a separação `_Project`/`_ThirdParty` quando aplicável, e a preservação de `.meta`/GUIDs. Não alterar assets de terceiros sem necessidade e autorização.

## 0.3. Regra de não regressão

Uma mudança não é bem-sucedida se aumentar a variedade de builds ao custo de arruinar o impacto, a legibilidade ou a fluidez do combate já conquistados.

Em particular, verificar o comportamento atual de reações de hit: ataques básicos comuns **não devem interromper sistematicamente a IA** de inimigos e bosses. Na etapa conhecida do projeto, a reação padrão do básico foi ajustada para algo equivalente a **Push / flinch + pequeno deslocamento**, enquanto uma finalização específica da Gauntlet permaneceu com **Stagger**. Preservar a intenção e auditar os nomes/comportamentos reais antes de tocar nisso.

---

# 1. VISÃO DE PRODUTO

## 1.1. Premissa

Construir um **ARPG de ação + roguelite** em que o jogador escolhe uma arma/arquetipo no lobby, entra na run com um núcleo de combate satisfatório e, a cada conjunto de salas, **transforma progressivamente o seu personagem, os seus ataques básicos e as suas skills** por meio de aquisições acumulativas.

Ao terminar uma run, o personagem deve poder:

- **Parecer radicalmente diferente**, apesar de continuar reconhecível como o mesmo herói e a mesma arma-base;
- **Jogar de maneira radicalmente diferente** — não apenas causar mais dano;
- Apresentar **interações inesperadas porém compreensíveis** entre itens, ataques básicos, skills e status;
- Conservar o peso, a habilidade manual, as decisões de posicionamento e a leitura do combate de ação;
- Fazer o jogador desejar iniciar outra run para descobrir novas combinações.

**Frase-guia:**

> A arma define **como o jogador começa a lutar**. Os itens, skills e decisões da run definem **no que esse estilo de luta se transforma**.

## 1.2. Referências e o que aproveitar de cada uma

| Referência | Extrair | Não copiar automaticamente |
|---|---|---|
| **The Binding of Isaac** | Itens com efeitos cumulativos, substituição de comportamento dos ataques, sinergias emergentes, transformações visuais do personagem, sensação de descoberta | Câmera, controle twin-stick, estética grotesca, aleatoriedade sem mecanismos de correção |
| **Shape of Dreams** | Skills que podem ser adquiridas, equipadas, aprimoradas e substituídas durante a run; progressão no lobby para ampliar opções iniciais | Interfaces, árvore específica, proporções numéricas ou economia exata |
| **Lost Ark**, especialmente Breaker | Impacto, timing, hit feedback, fluidez, cancelamentos, reações, telegráficos e identidade de armas | Escala/MMO, excesso de botões e sistemas de progressão persistente |
| **Diablo** | Legibilidade de vida/recurso, linguagem de ARPG, confronto com grupos e leitura de status; HUD com vida e mana/recurso visíveis | Interface inteira, sistemas de itemização endgame e dependência de números |
| **Hades** | Direção artística estilizada, contraste, cores, clareza e caracterização | Tratar Hades como referência obrigatória de todos os sistemas de gameplay ou do layout do HUD |
| **Megabonk** e roguelites de hordas | Sensação de poder crescente, densidade de inimigos, leitura da evolução da run e estatísticas de resultado | Excesso de efeitos opacos, combate exclusivamente automático |

Referências orientam experiência e princípios, não autorizam reprodução de assets, animações, códigos proprietários ou UI protegida.

## 1.3. Pilares de design, em prioridade

1. **Combate divertido no primeiro minuto.** A run não pode começar com um personagem inutilmente fraco aguardando sorte para ganhar uma skill.
2. **Cada aquisição relevante deve ser sentida.** Priorizar alterações de comportamento, forma, animação, feedback e aparência em vez de aumentos percentuais genéricos.
3. **Combinações acumulativas.** Modificadores independentes devem interagir de forma planejada e previsível tecnicamente, mas surpreendente criativamente.
4. **Liberdade de construção.** Skills podem ser adquiridas, melhoradas e trocadas durante a run; ataques básicos devem sustentar builds completas.
5. **Identidade de arma.** Gauntlet, Spear e Bow devem preservar diferenças no ritmo, alcance e modo de interação, mesmo compartilhando propriedades universais.
6. **Poder legível.** O espetáculo visual do jogador não pode esconder inimigos, perigos, bosses, telegráficos, cooldowns nem a própria posição.
7. **Evolução visual da build.** A aparência do personagem e das habilidades precisa contar a história dos itens adquiridos.
8. **Replayability com agência.** RNG cria variedade; escolhas, recompensas e mecanismos de correção evitam runs mortas.

---

# 2. O QUE MUDA E O QUE PERMANECE

## 2.1. Estado desejado

**Antes (modelo a ser reavaliado):** cada arma inicia com um conjunto relativamente fechado de skills; boons modificam partes desse kit, frequentemente em incrementos.

**Depois (modelo desejado):**

- Cada arma possui **núcleo inerente**: ataque básico, identidade de combo, movimentação/defesa essencial e mecânica exclusiva.
- O ataque básico **pode receber mutações profundas**, inclusive substituições parciais de forma/comportamento.
- Skills ativas Q/W/E/R formam um **loadout modular**, construído ao longo da run.
- Boons podem alterar o básico, modificar skills, gerar efeitos em eventos de combate, interligar sistemas e modificar o visual do personagem.
- Alguns itens especiais podem **reescrever uma regra** de ataque e alterar simultaneamente animação, visual e comportamento.
- Lobby/metaprogressão desbloqueia **possibilidades e escolhas iniciais**, não apenas multiplicadores de poder.

## 2.2. Preservar explicitamente

- O jogo continua sendo combate de ação com execução, esquiva, leitura e decisão; **não** se torna idle/autobattler.
- Preservar o *game feel* já aprovado da Gauntlet e as regras corretas de cancelamento/reações.
- Preservar ou migrar com cuidado a semântica atual de Q/W/E/R. **Não mover/remover silenciosamente o finisher existente em E**. Se o design futuro tornar E totalmente intercambiável, decidir primeiro como o finisher característico será acessado (habilidade inicial protegida, comando inerente ao moveset, ou outra solução validada).
- Bosses e elites continuam perigosos, com telegráficos visualmente prioritários.
- HUD deve exibir **vida e mana/recurso claramente**, usando Diablo como referência de informações; a linguagem visual pode ser estilizada.
- O jogo deve manter fluxo de salas, escolhas de rota/recompensas e resumo de fim de run, quando presentes no projeto.

## 2.3. Fora do escopo da primeira entrega

- Refazer todos os inimigos e bosses;
- Reescrever as três armas simultaneamente;
- Criar dezenas/centenas de boons antes de testar o motor de sinergias;
- Fazer uma árvore permanente completa antes de validar o loop de run;
- Construir um editor de habilidades para o jogador;
- Substituir arquitetura estável do projeto sem evidência de limitação;
- Produzir arte final de todas as transformações antes de aprovar seu comportamento.

---

# 3. VOCABULÁRIO / MODELO CONCEITUAL

| Termo | Definição normativa |
|---|---|
| **Weapon Core (Núcleo de arma)** | Ações, animações, timings, alcance-base, ritmo e passiva(s) que tornam a arma reconhecível. Persistem do início ao fim da run, mas podem ser modificados. |
| **Basic Attack (Ataque básico)** | Ação recorrente da arma, com combos e/ou subações. É um sistema de combate completo, não só fonte passiva de recurso. |
| **Active Skill (Skill ativa)** | Habilidade atribuível aos slots Q/W/E/R, sujeita a descoberta, equipagem, substituição e evolução, respeitadas regras do núcleo de combate. |
| **Boon / Item de run** | Aquisição que altera regras, aplica modificadores, desbloqueia efeitos ou muda aparências apenas durante a run, salvo exceção explícita. |
| **Modifier (Modificador)** | Unidade combinável com gatilho, condição, efeito, prioridade e restrições. Pode atingir ataque, skill, status, personagem ou apresentação. |
| **Mutation (Mutação)** | Alteração mecânica perceptível da forma, do comportamento ou das consequências de uma ação. |
| **Transformation (Transformação visual)** | Alteração de aparência do personagem/arma/ataque, ligada à build e ao estado atual dos itens. |
| **Awakening (Transformação maior)** | Mutação rara capaz de redefinir parte importante do moveset ou da skill. Não implica automaticamente números superiores. |
| **Synergy (Sinergia)** | Comportamento emergente da interação de dois ou mais modificadores, por composição genérica ou combinação especial declarada. |
| **Run Build State** | Estado transitório: arma, skills, boons, stacks, variações, mutações, seed, recursos e aparência derivada. |
| **Meta Progression** | Desbloqueios permanentes entre runs: repertório, opções de starter, presets e ramificações de estilo. |

**Distinção crítica:** raridade do item, nível de uma skill e intensidade visual **não são a mesma variável**.

---

# 4. ARQUITETURA DE GAMEPLAY PROPOSTA

## 4.1. Camada A — núcleo fixo da arma

Cada arma começa funcional. Seu núcleo DEVE conter:

- Ataque básico responsivo, com padrão ou sequência própria;
- Mobilidade/defesa necessária para sobreviver e expressar habilidade;
- Mecanismo distintivo (ex.: Momentum, postura, precisão, combo ou recurso);
- Ferramenta inicial mínima para dano de área/controle/finalização, conforme exigência do combate existente;
- Feedback audiovisual suficiente **sem depender de boons**.

**Não tornar o básico apenas um gerador obrigatório de mana/cooldown.** Ele deve poder ser a fonte primária de dano de uma build viável.

### Identidades indicativas

- **Gauntlet:** boxe técnico, aproximação, sequência de socos, counters, Momentum, golpes decisivos e transformações corporais/manoplas.
- **Spear:** alcance e direção, estocadas, varreduras, perfuração, controle espacial, lâminas espectrais e transformações da arma.
- **Bow:** cadência, precisão, projéteis, mobilidade, marcação de alvos, fragmentação, retorno, ricochetes e perseguição.

Essas descrições são conceitos; preservar o que a implementação atual já faz bem.

## 4.2. Camada B — skills ativas modulares

- Manter **quatro slots-alvo Q/W/E/R**, exceto se auditoria e aprovação justificarem adaptação.
- Jogador DEVE poder **obter e substituir** skills durante a run.
- Skills DEVERIAM ter variantes mecânicas, não apenas upgrades de dano.
- Recompensas não podem apresentar repetidamente escolhas impossíveis de equipar/usar.
- Skills substituídas devem ter regras explícitas de descarte, recuperação, transferência ou reembolso parcial de investimento. Não presumir reembolso total.
- Aquisição e substituição são realizadas em interfaces seguras (recompensa, transição de sala, pausa permitida) e não devem pausar combate inadequadamente.
- Quando um slot já estiver preenchido, a UI deve mostrar claramente: **equipar / substituir / cancelar / consequências**.

### Kit inicial — decisão de compatibilidade

**Objetivo de produto:** o jogador inicia com o núcleo divertido + ao menos uma ação ativa útil, podendo montar o restante ao longo da run. Não exigir grind permanente para o personagem ficar funcional.

**Regra de migração:** o finisher atual da Gauntlet em E permanece acessível durante o protótipo, sem regressão de Stagger, enquanto o design define se posteriormente será uma ação inerente ou uma skill modular. A IA NÃO DEVE mover esse finisher por conta própria.

**Garantia de progressão:** desenhar um caminho de recompensas para que o jogador obtenha opções viáveis de preencher o loadout ainda na primeira parte da run. Determinar sala exata após medir duração real; não depender exclusivamente de sorte.

## 4.3. Camada C — boons e mutações

Boons podem cumprir uma ou mais funções:

1. **Forma:** substituir/adaptar hitbox, alcance, padrão de combo, número de golpes, shape/trajectory dos projéteis.
2. **Comportamento:** ricochetear, perfurar, retornar, dividir, perseguir, carregar, repetir.
3. **Impacto:** explosão, onda de choque, deslocamento apropriado, status, propagação.
4. **Gatilho:** ao atacar, acertar, critar, finalizar combo, usar skill, esquivar, eliminar alvo, gastar recurso.
5. **Relação entre sistemas:** básico prepara skill; skill altera próximo básico; status aplicado por um pode ser consumido pelo outro.
6. **Apresentação:** mudança de malha, material, animação, VFX, SFX, aura, silhueta e feedback.
7. **Substituição maior:** itens raros trocam uma regra central do ataque, preservando compatibilidade com modificadores aplicáveis.

**Critério de qualidade:** uma aquisição importante deve gerar algo observável no combate sem abrir um painel de atributos. Bônus numéricos puros podem existir como sustentação, mas não podem dominar o catálogo.

## 4.4. Camada D — progressão no lobby

A árvore/meta-progresso tem como principal função **ampliar escolhas**:

- Desbloquear novas skills que podem aparecer nas runs;
- Desbloquear variantes de ataques básicos e especializações iniciais;
- Permitir escolher uma skill inicial, a partir de opções já desbloqueadas;
- Abrir alternativas de starter boon, presets ou uma configuração inicial limitada;
- Disponibilizar novos arquétipos sem tornar o conteúdo anterior irrelevante.

**Evitar:** progressão permanente baseada sobretudo em multiplicadores massivos de dano/vida, que deixem as primeiras runs frustrantes ou trivializem o combate posterior.

**Equilíbrio:** starter configurável deve aumentar consistência e expressividade, não entregar uma build lendária completa no lobby.

---

# 5. REGRAS DE SINERGIA — A PRINCIPAL INOVAÇÃO

## 5.1. Princípio

O resultado de adquirir A + B + C deve frequentemente ser **qualitativamente diferente** de apenas somar os números de A, B e C.

Exemplo ilustrativo da Gauntlet:

- A: **Punhos Titânicos** — final de combo gera onda de choque e manoplas crescem.
- B: **Condutor de Tempestades** — impactos com área/propriedade compatível propagam eletricidade.
- C: **Ecos do Lutador** — determinados ataques geram repetição espectral controlada.
- A + B: a onda de choque também distribui eletricidade.
- A + C: um eco replica parte do golpe gerador de onda, sob regras de proc.
- A + B + C: ecos e impactos elétricos compõem a sequência, dentro dos limites de geração e recursão.

Esse último resultado precisa ser **implementável e legível**, não uma cascata ilimitada de eventos.

## 5.2. Composição genérica, não milhares de combinações manuais

Adotar — ou adaptar sobre sistemas existentes — um modelo de **tags semânticas + eventos + modificadores composáveis**.

Tags ilustrativas:

- `attack.basic`, `attack.skill`, `attack.finisher`
- `shape.melee`, `shape.projectile`, `shape.area`
- `behavior.piercing`, `behavior.echo`, `behavior.split`
- `element.lightning`, `element.fire`
- `event.on_hit`, `event.on_kill`, `event.on_combo_end`
- `source.primary`, `source.secondary`, `source.proc`

As tags são linguagem conceitual; não copiar diretamente se o projeto já tiver tipos, enums ou identificadores equivalentes.

**Composição genérica** gera o grosso das interações. **Sinergias especiais declaradas** podem existir para resultados memoráveis (ex.: nome próprio, efeito visual raro, transformação corporal), mas não substituem a arquitetura composável.

## 5.3. Ordem semântica de resolução

Propor uma pipeline explícita, testável e determinística:

```text
Input do jogador
  -> estado de arma/skill e recurso
  -> determinação do ataque-base
  -> substituições de forma (com prioridade e exclusão)
  -> modificadores de trajetória/quantidade/área
  -> modificadores de impacto/status
  -> evento de hit confirmado
  -> procs condicionais / efeitos secundários
  -> composição de feedback visual/sonoro
  -> telemetria + atribuição de dano
```

A ordem final deve considerar a arquitetura existente. É obrigatório especificar **quando** cada efeito é aplicado para eliminar ambiguidades como "projetil se divide antes ou depois de perfurar?".

## 5.4. Substituição versus acúmulo

Tratar modificadores em pelo menos três famílias:

- **Aditivos:** adicionam efeito compatível (ex.: choque em onda de impacto).
- **Transformativos:** substituem forma ou regra de ataque (ex.: soco físico vira punho astral à distância).
- **Exclusivos:** não coexistem integralmente no mesmo slot sem resolução explícita (ex.: duas formas mutuamente incompatíveis de projétil principal).

Para conflitos, usar precedência declarada e feedback claro ao jogador. **Não** deixar a ordem casual de aquisição produzir bugs silenciosos. Quando a ordem importar por design, comunicar isso e registrá-la no estado da run.

## 5.5. Proteções de runtime e balanceamento

Eventos de efeitos secundários DEVEM carregar contexto de origem e limites. Especificar:

- Identidade da ação/skill/arma/boon causador, alvo e origem do dano;
- Distinção entre **acerto primário**, **hit secundário** e **proc**;
- Profundidade/geração de cadeia;
- Orçamento máximo de procs por ação/janela para evitar loops, travamentos e explosões de objetos;
- Cooldowns internos quando apropriados;
- Regras explícitas para se procs podem gerar outros procs;
- Uso de pooling e culling visual quando necessário;
- Regras de stacking e caps para multiplicadores/quantidade de instâncias.

**Nunca introduzir loops infinitos, dano aplicado duas vezes pelo mesmo evento, hits fantasmas ou interrupção descontrolada da IA por causa dos efeitos secundários.**

## 5.6. Determinismo e depuração

Sempre que viável, as escolhas de recompensa e resultados relevantes devem ser reproduzíveis por **seed + sequência de decisões**, facilitando teste de sinergias. Logs estruturados devem permitir rastrear o que gerou um ataque, quais modificadores foram aplicados e por que um proc ocorreu ou foi suprimido.

---

# 6. TRANSFORMAÇÃO VISUAL — REQUISITO DE PRIMEIRA CLASSE

## 6.1. Objetivo visual

Em uma comparação de capturas do início e do boss final, uma build transformadora deve ter **silhueta, detalhes, efeitos, animações e apresentação de ataques claramente diferentes**.

O jogador deve reconhecer que:

- Suas manoplas cresceram, racharam, irradiam energia ou ganharam braços auxiliares;
- Sua lança se fragmentou, projetou lâminas adicionais ou virou arma energética;
- Suas flechas deixaram de ser flechas convencionais e se transformaram em múltiplos projéteis, raios ou outros comportamentos;
- O personagem tem uma **história visual da build**, e não apenas ícones no HUD.

## 6.2. Camadas visuais componíveis

O sistema deveria suportar, conforme a estrutura real do personagem:

1. **Malhas/acessórios**: manoplas, peças de armadura, lâminas, elementos orbitais, braços espectrais.
2. **Materiais/shaders**: cores, emissão, rachaduras, glow localizado, padrões.
3. **VFX de movimento**: trails, afterimages, partículas, rastros.
4. **VFX de impacto**: ondas, explosões, marcas no chão, arcos elétricos.
5. **Animações e poses**: substituições pontuais, camadas, alterações de timing coerentes com gameplay.
6. **SFX e feedback tátil/visual**: impacto, bass, áudio adaptado e shake controlado.
7. **Assinatura da transformação**: combinação identificável para itens de maior impacto.

**Não** criar um prefab de personagem completo para cada combinação. Preferir composição modular, reaproveitamento de rigs, slots de acessórios, parâmetros de materiais e perfis de VFX.

## 6.3. Regras de composição visual

- Modificações aditivas devem coexistir quando tecnicamente e artisticamente viáveis.
- Mudanças exclusivas de malha ou material precisam de regra de prioridade e restauração.
- A remoção/substituição de um item na run deve remover adequadamente os efeitos associados, sem resíduos.
- VFX deve refletir a **mecânica real**; não anunciar choque em uma ação que não aplica choque.
- Diferenciar estados permanentes da run de estados temporários (buff, ataque carregado, awakening, transformação por duração).
- Evitar clipping visível, perda de rig, mãos desalinhadas e deformação do personagem.
- Preservar a identidade visual base e a silhueta legível mesmo em estados avançados.

## 6.4. Prioridade de legibilidade

**Hierarquia visual obrigatória:**

1. Perigos críticos, telegráficos de elites e bosses;
2. Posicionamento do jogador e leitura imediata dos ataques;
3. Vida, recurso, cooldowns e estados relevantes;
4. Efeitos de dano e espetáculo da build;
5. Ambientação e efeitos secundários cosméticos.

Implementar mecanismos de redução de ruído quando efeitos se acumularem: budgets de partículas/luzes, transparência contextual, limitação de flashes/shakes, opções de acessibilidade, culling e contraste controlado. O espetáculo não pode esconder janelas defensivas.

## 6.5. Estilo de arte

Seguir a direção vigente do projeto: personagem 3D estilizado com elementos ilustrados/2D, alto apelo de silhueta e efeitos cuidadosamente desenhados. Usar referências artísticas de Hades **sem converter indevidamente todo o jogo num clone de Hades**. A UI funcional e o HUD devem priorizar leitura de ARPG, incluindo vida e mana/recurso visíveis em linguagem próxima a Diablo.

---

# 7. EXEMPLOS DE BOONS TRANSFORMADORES — CATÁLOGO DE PROTÓTIPO

> **IMPORTANTE:** estes 12 exemplos servem para testar a diversidade de **funções** e interações. Não são balanceamento aprovado nem requisito para implementação literal de todos na primeira fase. O agente deve revisar boons existentes e reaproveitar bons conceitos.

| ID conceitual | Item | Efeito mecânico observável | Consequência visual | Interações desejadas |
|---|---|---|---|---|
| B01 | **Punhos Titânicos** | Final de combo ganha onda de choque/área | Manoplas aumentadas, solo fraturado | Choque, eco, gravidade |
| B02 | **Condutor de Tempestades** | Hits compatíveis encadeiam eletricidade | Manoplas com fissuras elétricas e raios | Área, ataques múltiplos, ecos |
| B03 | **Ecos do Lutador** | Parte de ataques cria repetição espectral limitada | Braços fantasma/afterimage | Área, elementos, combos |
| B04 | **Ritmo Frenético** | Sequências rápidas alteram cadência e recompensa de combo | Trilhas curtas e animação mais dinâmica | Acúmulo de status e recursos |
| B05 | **Núcleo Gravitacional** | Golpes selecionados atraem alvos ou agrupam impactos | Distorção/vórtice localizado | Área, explosões, perfuração |
| B06 | **Impacto Fraturado** | Certos hits se fragmentam em impactos secundários | Fendas e fragmentos geométricos | Eco, raio, status |
| B07 | **Reação em Cadeia** | Eliminações por efeitos compatíveis disparam propagação controlada | Pulsos de energia alvo a alvo | Elementos, explosão, agrupamento |
| B08 | **Marcas de Ruptura** | Básico aplica marca consumida por skills ou finalizadores | Símbolos discretos e ruptura no alvo | Build híbrida básico + skill |
| B09 | **Segundo Fôlego** | Ações bem temporizadas modificam próxima janela ofensiva | Feedback rítmico no personagem | Counters, dash, combos |
| B10 | **Avatar Astral** | Parte do básico vira projeção de punhos a curta/média distância | Braços espectrais; pose/traços novos | Eco, fragmentação, elementos |
| B11 | **Sangue de Meteorito** | Finisher cria área persistente breve com consequência tática | Cratera estilizada, material incandescente | Gravidade, status, combos |
| B12 | **Reescrita de Forma** | Escolha rara permite trocar uma característica estrutural do ataque | Nova forma de ataque facilmente reconhecível | Compatibilidade de modificadores |

**Regra de curadoria:** nenhum catálogo inicial deve conter 12 itens que só mudam percentual, cor ou partículas. O protótipo deve incluir diferentes classes de mutação e pelo menos uma substituição real de comportamento.

## 7.1. Exemplo de sinergia A — Gauntlet de impacto pesado

```text
Núcleo: socos / combo / finisher
+ Punhos Titânicos           -> onda de choque no fim do combo
+ Condutor de Tempestades    -> onda de choque eletrificada
+ Ecos do Lutador            -> um eco repete parte do ataque
+ Núcleo Gravitacional       -> alvos são agrupados antes do impacto
= Boxeador de impacto massivo, manoplas grandes, energia elétrica,
  braços espectrais e combate de controle de hordas.
```

## 7.2. Exemplo de sinergia B — Gauntlet de velocidade e preparação

```text
Núcleo: socos curtos / mobilidade / counter
+ Ritmo Frenético            -> cadência e sequências alteradas
+ Marcas de Ruptura          -> hits básicos marcam alvos
+ Segundo Fôlego            -> esquivas/counters criam janela ofensiva
+ Skill de detonação         -> consome marcas em um golpe decisivo
= Boxeador técnico e veloz, com visual leve, afterimages,
  oportunidades de precisão e explosão controlada de dano.
```

## 7.3. Exemplo de sinergia C — variações entre armas

Um boon conceitual universal, **Fragmentação**, pode:

- **Gauntlet:** subdividir a onda de choque em impactos menores;
- **Spear:** criar lâminas secundárias após uma estocada;
- **Bow:** dividir projéteis compatíveis;

Todos compartilham uma **intenção mecânica** (um ataque gera múltiplos elementos), mas não a mesma animação, hitbox ou implementação audiovisual.

Quando um boon universal for incompatível com uma arma/skill, o sistema deve evitar ofertá-lo ou fornecer variação válida explicitamente projetada. Não fingir compatibilidade adicionando bônus numérico arbitrário.

---

# 8. EVOLUÇÃO DAS SKILLS — SEM CONFUNDIR COM RARIDADE

## 8.1. Três eixos separados

1. **Aquisição:** obter uma skill equipável.
2. **Especialização:** alterar o modo como a skill funciona.
3. **Intensificação:** melhorar potência/eficiência/cadência conforme regras de balanceamento.

O mesmo skill pode ter especializações equivalentes em valor estratégico, mas com usos diferentes.

### Exemplo: Uppercut da Gauntlet

| Variante | Mudança mecânica | Caso de uso |
|---|---|---|
| **Base** | Lança o alvo e oferece abertura | Kit geral |
| **Crushing** | Golpe concentrado com recompensa maior contra alvos resistentes | Elites/boss |
| **Cyclone** | Ganha atração/controle em área | Hordas |
| **Thunder** | Aplica propagação elétrica | Sinergias elementais |
| **Awakened (raro)** | Novo comportamento, pose/animação e impacto marcante | Build específica; não deve invalidar todas as variantes |

**Importante:** evolução não significa obrigatoriamente uma escada `Tier III > Tier II > Tier I`. Raridade pode controlar frequência e transformação, mas variantes devem ter escolhas significativas.

## 8.2. Alteração do loadout durante a run

O jogador deve poder:

- Inspecionar rapidamente as skills equipadas e mutações associadas;
- Equipar skill descoberta em slot vazio;
- Substituir skill existente mediante confirmação;
- Entender o destino de aprimoramentos que a skill substituída possuía;
- Reconhecer se uma mutação depende de skill específica ou continua válida no básico;
- Visualizar antes/depois das mudanças quando possível.

**Não permitir troca descontrolada durante execução de animações/combat state** sem regras de segurança. Substituições são transações atômicas: validar compatibilidade, aplicar troca, atualizar recursos/cooldowns conforme política declarada, reconstruir visuais e telemetria.

---

# 9. LOOP DE RUN E SISTEMA DE RECOMPENSAS

## 9.1. Fluxo pretendido

```text
Lobby
  -> escolher arma / preset / opção inicial desbloqueada
  -> entrar na run com núcleo funcional
  -> combate em sala (hordas, elites e encontros especiais)
  -> recompensa com opções significativas
       - nova skill
       - mutação do ataque básico
       - modificador de skill
       - item transformador/sinergia
       - recurso ou rota alternativa
  -> gerenciar loadout em segurança
  -> novas salas + builds emergentes
  -> boss / conclusão ou derrota
  -> relatório de run, incluindo quais sistemas causaram dano e efeitos
  -> lobby com novos desbloqueios possíveis
```

## 9.2. Regras para RNG e agência

- O gerador de recompensas DEVE filtrar opções inválidas, incompatíveis ou impossíveis de aproveitar.
- O jogador não pode depender de um único item raro para o básico ou suas skills passarem a funcionar.
- **Recompensas essenciais** (como opções de acesso às skills) precisam de mecanismo garantido, pool reservado, pity ou outro desenho de progressão verificável.
- Não ofertar três escolhas funcionalmente idênticas com nomes diferentes.
- Ofertas deveriam refletir parcialmente arma, skills equipadas, tags e investimentos anteriores **sem forçar uma build específica**.
- Preservar descoberta: não direcionar 100% das recompensas por sinergia óbvia.
- Pode haver reroll/banish/alternativas, mas o custo e frequência são decisões de balanceamento A VALIDAR.

## 9.3. Ritmo hipotético — não fixar salas sem medição

- **Início:** kit mínimo divertido + primeira oportunidade de direcionar build.
- **Primeiro trecho:** opções suficientes para o jogador sentir autonomia de skills e básico.
- **Trecho intermediário:** sinergias reconhecíveis, alguma transformação visual marcante.
- **Último trecho:** escolhas especializadas, possível awakening e diferença clara em relação ao personagem inicial.
- **Boss:** poder elevado, ainda exigindo posicionamento e leitura.

Não determinar que uma skill obrigatoriamente aparece na sala 2 ou que existe exatamente uma run de 12 salas. Ajustar ao mapa e à duração reais.

---

# 10. LOBBY E META-PROGRESSÃO

## 10.1. Objetivo

Criar um sistema de especializações/desbloqueios permanentes que permita ao jogador escolher seu **ponto de partida**, não definir completamente sua build antes da run.

### Sugestões de desbloqueio

- Nova skill elegível na pool;
- Variante de combo básico inicial;
- Escolha entre duas skills iniciais desbloqueadas;
- Um slot limitado de starter boon/essência, com regras de balanceamento;
- Presets de arma e estilo;
- Possibilidade de uma escolha inicial mais informada sobre a direção da run.

### Antipadrões

- Obrigatoriedade de dezenas de runs para o básico ficar responsivo;
- Bônus permanentes tão fortes que invalidam decisões da run;
- Árvore que desbloqueia sempre a mesma "melhor" build;
- Dependência circular em que é impossível jogar bem sem desbloqueios e impossível desbloquear sem jogar bem.

## 10.2. Proposta de organização

Uma árvore/constelação por arma com áreas conceituais:

- **Fundamentos da arma** — possibilidades do moveset;
- **Arsenal** — skills e variantes elegíveis;
- **Especialização** — estilos iniciais alternativos;
- **Expedição** — mais agência em escolha de recompensas, dentro de limites.

A arquitetura visual exata deverá seguir as skills de design existentes, as referências auditadas e o padrão de UX do projeto.

---

# 11. EXPERIÊNCIA DE UI/UX E FEEDBACK

## 11.1. HUD em combate

DEVE deixar legíveis:

- Vida e mana/recurso (referência de informação: Diablo);
- Slots Q/W/E/R, cooldowns e disponibilidade;
- Identidade do ataque básico/mecânica exclusiva quando relevante;
- Status decisivos da build, sem poluição;
- Informação de evolução visual sem precisar abrir menus.

## 11.2. Cards de recompensa

Um boon transformador deve explicar em linguagem direta:

1. **O que muda de comportamento**;
2. **Que ações são afetadas** (básico, skill, finalizador, projétil etc.);
3. **Como interage com a build atual**, quando relevante;
4. Incompatibilidades/substituições relevantes;
5. Preview visual quando tecnicamente acessível.

Exemplo de redação de card:

> **Punhos Titânicos** — O último soco do combo cria uma onda de choque. As manoplas aumentam de tamanho e os golpes pesados ficam mais amplos. **Sinergia atual:** a onda também poderá aplicar sua eletricidade.

Não escrever cards que ocultem mudança mecânica atrás de `+X% Damage`.

## 11.3. Menu de skills

- Mostrar loadout com slot, skill, variante, modificadores e interação com boons;
- Permitir comparar skill atual e candidata;
- Exibir consequências de troca;
- Ser navegável por teclado/mouse/gamepad de acordo com suporte real do projeto;
- Evitar menus complexos durante combate.

## 11.4. Relatório de fim de run

Conectar transformações a telemetria concreta:

- Dano total e por skill;
- Dano atribuído ao ataque básico;
- Contribuição de efeitos/boons/procs **sem dupla contagem**;
- Skills equipadas, variantes e principais itens;
- Linha do tempo simples dos itens adquiridos e transformações;
- Tempo de run, salas, elites/bosses e outros indicadores já pertinentes ao projeto.

Atribuição deve ser clara: o dano causado por um proc derivado de uma skill pode aparecer como subcategoria dessa skill, mas não pode ser somado duas vezes em totalizações.

---

# 12. DIRETRIZES DE ARQUITETURA PARA UNITY (NÃO PRESCRITIVAS DE NOMES)

## 12.1. Separação de dados, runtime e apresentação

Padrão conceitual sugerido:

```text
Definitions (dados autoráveis)
  WeaponDefinition
  SkillDefinition
  BoonDefinition
  Mutation/ModifierDefinition
  VisualMutationProfile
       |
       v
Run State (estado mutável/transitório)
  Active Weapon + Loadout
  Boons / Stacks / Mutations / Resources
  Build Tags / Seed / RNG / Cooldowns
       |
       v
Combat Resolution
  Attack Context
  Compatibility + Priority Resolution
  Hit Events + Proc Budget
  Damage / Status / Reaction Resolution
       |
       +--> Presentation (character appearance / animation / VFX / SFX / HUD)
       +--> Telemetry (damage attribution / combat log / run report)
```

**Este é um desenho lógico**, não uma exigência de classes com tais nomes. Reutilizar componentes existentes e preservar convenções de arquitetura do projeto.

`ScriptableObject` pode ser apropriado para definições autoráveis/compartilhadas; não colocar estado mutável de uma run dentro de assets compartilhados. Separar definições de instâncias em runtime.

## 12.2. Contratos mínimos de um modificador

Cada modificador deveria expor conceitos equivalentes a:

- ID estável e versão de conteúdo;
- Origem (item, skill, talento, buff);
- Escopo/target (básico, skill, evento, arma, personagem);
- Tags e condições de compatibilidade;
- Trigger/evento;
- Efeito aplicado;
- Modo de combinação e regras de conflito;
- Prioridade de resolução;
- Política de stack/duração/remoção;
- Referência opcional a visual/SFX;
- Metadados para UI, debug e telemetria.

## 12.3. Exemplo de estrutura de dados (pseudo-YAML, **não copiar como API**)

```yaml
id: boon.titanic_fists
name: "Punhos Titânicos"
category: transformation
rarity: rare
scope: weapon.gauntlet
eligibility:
  requires_tags: [attack.basic, combo.finisher]
  excludes_tags: []
modifiers:
  - trigger: on_combo_finisher
    mode: add_secondary_impact
    impact_tags: [shape.area, impact.shockwave]
    proc_policy: primary_only
    stacking: unique
visual_mutations:
  - target: gauntlet_equipment
    layer: replace_or_scale_attachment
    profile: visual.gauntlet_titanic
  - target: attack_impact
    layer: additive_vfx
    profile: visual.shockwave_ground
presentation:
  primary_change: "O último soco produz uma onda de choque."
  secondary_change: "As manoplas aumentam e impactos racham o solo."
telemetry:
  source_category: boon_secondary_impact
```

Esse schema é um **exemplo de domínio**: o agente deve propor formato final alinhado ao que o código já suporta. Evitar criar arquivos/configs extras sem necessidade.

## 12.4. Composição visual e controle de estado

Mudanças visuais devem ser **derivadas da build ativa**, e não aplicadas de maneira irreversível ao prefab original. Recomenda-se reconstrução/reconciliação idempotente a partir do estado atual ao adquirir, substituir, remover ou limpar itens. Evitar vazamento de materiais, instâncias duplicadas e persistência acidental entre runs.

## 12.5. Segurança das operações de loadout

Trocar skill é uma transação:

1. Verificar momento seguro/estado de combate permitido;
2. Verificar validade/compatibilidade;
3. Determinar destino de investimentos e cooldown;
4. Atualizar estado da run atomicamente;
5. Recalcular tags, modificadores, stats derivadas, ações disponíveis;
6. Atualizar visuais, HUD e telemetria;
7. Fazer rollback coerente se uma operação falhar.

## 12.6. Performance

Definir budgets com medições reais e hardware-alvo do projeto, não números inventados neste documento. No protótipo, instrumentar quantidade de hits, procs, instâncias VFX e custos de CPU/GPU em runs com sinergias extremas.

---

# 13. REQUISITOS FUNCIONAIS E CRITÉRIOS DE ACEITE

Os IDs abaixo devem ser rastreáveis em `requirements`, `design`, `tasks`, testes e relatórios. **Não representam IDs já existentes**; se houver convenção real, mapear sem duplicar.

| ID | Requisito | Aceite mínimo verificável |
|---|---|---|
| RF-01 | Núcleo de arma funcional | Gauntlet é divertida e capaz de enfrentar sala inicial sem depender de boon específico. |
| RF-02 | Básico mutável | Ao menos uma aquisição altera hitbox/padrão/comportamento do básico, não apenas atributos. |
| RF-03 | Composição de modificadores | Dois modificadores compatíveis produzem efeito combinado estável. |
| RF-04 | Sinergia emergente | Três modificadores geram combinação distinguível de seus efeitos isolados. |
| RF-05 | Substituição de comportamento | Pelo menos uma mutação troca parte relevante do ataque mantendo controles coerentes. |
| RF-06 | Transformação visual cumulativa | Duas aquisições visuais compatíveis aparecem simultaneamente e respeitam prioridade. |
| RF-07 | Reversão visual | Remover/substituir efeito restaura visual correto sem duplicações ou resíduos. |
| RF-08 | Skills modulares | Jogador equipa e substitui skill em contexto seguro, com UI de confirmação e estado consistente. |
| RF-09 | Skill e boon compatíveis | Oferta inválida é filtrada ou transformada em opção explicitamente válida. |
| RF-10 | Não regressão de combate | Básicos não interrompem indevidamente IA; finisher mantém comportamento intencional, inclusive Stagger quando aplicável. |
| RF-11 | Prevenção de proc infinito | Combinação cíclica potencial é limitada/recusada sem travar ou gerar dano ilimitado. |
| RF-12 | Identidade de arma | Mutações universais respeitam padrão, animação e geometria de cada arma testada. |
| RF-13 | Legibilidade de combate | Telegraphs de elites/bosses permanecem reconhecíveis com build avançada. |
| RF-14 | HUD | Vida, recurso e ações ficam legíveis durante combate com efeitos acumulados. |
| RF-15 | Telemetria correta | Total de dano se reconcilia com danos de básico, skills e subefeitos, sem dupla contagem. |
| RF-16 | Reprodutibilidade | Um cenário de build/seed de teste pode ser repetido para diagnóstico. |
| RF-17 | Progressão justa | Sala inicial funciona; opções essenciais de skill não dependem só de sorte extrema. |
| RF-18 | Diferenciação entre runs | Duas builds deliberadamente diferentes da Gauntlet produzem diferenças visuais e táticas observáveis. |
| RF-19 | Ciclo de run limpo | Terminar/reiniciar run limpa modificadores, visuais transitórios, procs e loadout sem vazar estado. |
| RF-20 | Validação em Unity | Capturas/inspeções e testes ficam registrados; limitações do MCP são declaradas. |

## 13.1. Critério de aceitação da vertical slice

Considerar a primeira entrega aprovada **somente se** for possível demonstrar, com vídeos ou capturas e notas de playtest:

1. Um **antes/depois** do personagem e do ataque, do início ao estado transformado;
2. **Duas builds diferentes** de Gauntlet com decisões, ritmo e aparência distinguíveis;
3. Um caso de **sinergia de dois itens** e outro de **três itens**;
4. Uma **skill substituída em run**, sem quebrar controles/estado;
5. Um item que **substitui efetivamente** comportamento de ataque;
6. Uma sala com densidade de inimigos relevante e um encontro de elite/boss em que os telegráficos seguem visíveis;
7. Testes para prevenção de cascatas infinitas, consistência de estado e regressão de hit reaction;
8. Comparativo do game feel pré e pós-mudança; se piorar, corrigir antes de expandir.

**O protótipo não precisa de arte final.** É obrigatório, entretanto, que seu visual provisório deixe evidentes mudanças mecânicas e de silhueta.

---

# 14. ESTRATÉGIA DE TESTES E TELEMETRIA

## 14.1. Testes automatizados

- Resolução determinística de ordem/prioridade dos modificadores;
- Compatibilidade de tags, armas, skills e slots;
- Adição, remoção e substituição de boons/skills;
- Limites de recursão, procs, eventos e hit counts;
- Separação entre estado autorável e estado da run;
- Reversão da aparência/estado em restart;
- Preservação de cancel rules e rejeição de valores não finitos quando aplicável;
- Não regressão de hit reactions e aplicação correta de Stagger/Push conforme função real;
- Atribuição de dano e ausência de dupla contagem.

## 14.2. Testes visuais e game feel

Capturar, no mínimo, os seguintes estados da vertical slice:

- Gauntlet base (sem itens);
- Gauntlet após uma mutação relevante;
- Duas mutações visuais coexistindo;
- Estado com transformação maior;
- Horda sob múltiplos efeitos;
- Elite/boss telegráfico enquanto procs ocorrem;
- Menu de recompensa e troca de skill;
- Tela de fim de run com resumo de efeitos.

Comparar com referências visuais aprovadas e com o estado anterior do jogo. Verificar VFX, clipping, feedback de impacto, ritmo, foco visual, cooldown e informação de vida/recurso.

## 14.3. Telemetria de qualidade da build

Além de DPS e dano, coletar sinais que ajudam a diagnosticar design:

- Itens/skills escolhidos por sala;
- Tempo até primeira mudança de comportamento;
- Tempo até primeira mudança visual marcante;
- Quantidade de procs por ação/segundo e supressões por budget;
- Uso e acertos de básico versus skills;
- Frequência de trocas de skill;
- Ocorrências de escolhas de recompensa inválidas/inúteis;
- Mortes por sala e se perigos estavam visualmente legíveis;
- Erros/exceções e métricas de frame-time nos cenários críticos.

Metas numéricas definitivas só devem ser aprovadas após benchmark do projeto real.

---

# 15. PLANO DE ENTREGA POR FASES (COM PORTÕES DE APROVAÇÃO)

## Fase 0 — Auditoria e alinhamento (sem reescrita de gameplay)

**Objetivo:** mapear o estado real do projeto contra esta visão.

Tarefas:

1. Ler `AGENTS.md`, skills de design, documentação atual e decisões anteriores.
2. Identificar componentes de armas, ataque básico, skills Q/W/E/R, boons, hit pipeline, reações, VFX, UI e salas.
3. Identificar sistemas reaproveitáveis e pontos de acoplamento/performance.
4. Analisar como preservar o finisher E e os avanços de game feel.
5. Gerar matriz **existente / reutilizar / adaptar / novo / risco / evidência**.
6. Separar perguntas reais de arquitetura das decisões já estabelecidas neste documento.

**Entrega:** relatório de auditoria + mapa de arquitetura + requirements/design/tasks propostos.

**Gate:** aprovação do desenho da vertical slice antes de implementação extensiva.

## Fase 1 — Motor composável de mutações

**Objetivo:** fazer o básico da Gauntlet receber modificadores combináveis sem regressões.

Tarefas:

1. Definir modelo de ataque e estado runtime compatíveis com sistemas existentes.
2. Implementar um primeiro conjunto pequeno de modificadores de **forma**, **impacto**, **trigger**.
3. Resolver prioridades, compatibilidade e origens de eventos.
4. Adicionar budgets de proc e cenários de teste.
5. Mostrar duas sinergias simples e uma com três efeitos.

**Entrega:** protótipo jogável e testes.

**Gate:** golpes continuam fluidos; composição funciona sem loops nem interrupções indevidas.

## Fase 2 — Transformação visual vinculada ao comportamento

**Objetivo:** garantir que as mutações sejam visíveis no personagem e nos golpes.

Tarefas:

1. Inspecionar rig, materiais, shaders, sistema de VFX e pipeline de animação atuais.
2. Introduzir composição visual com reconciliação/reversão por estado de build.
3. Implementar pelo menos quatro assinaturas visuais marcantes no protótipo (não necessariamente quatro modelos inteiros).
4. Criar ao menos um item que substitua comportamento e apresentação do básico.
5. Aplicar budgets/legibilidade, inclusive em horda e elite/boss.

**Entrega:** capturas comparativas início/meio/final e testes de troca/limpeza.

**Gate:** o jogador enxerga a evolução sem perder leitura do combate.

## Fase 3 — Skills modulares e recompensas

**Objetivo:** construir parte do loadout na run.

Tarefas:

1. Definir política final do finisher existente e dos slots Q/W/E/R.
2. Permitir aquisição, equipagem e substituição segura de skills.
3. Integrar boons que modificam uma skill ou sinergizam com o básico.
4. Adaptar oferta de recompensas a compatibilidade e progressão garantida.
5. Criar UI que comunique troca, efeito e sinergias já ativas.

**Entrega:** run com troca de skill e duas builds funcionalmente distintas.

**Gate:** usuário entende e controla as alterações; sistema não depende de RNG punitivo.

## Fase 4 — Progressão no lobby e especializações

**Objetivo:** permitir variação de início sem diluir as escolhas da run.

Tarefas:

1. Desenhar progressão lateral e desbloqueios por arma.
2. Permitir ao menos uma escolha inicial significativa.
3. Conectar desbloqueios à pool de skills/boons sem gerar power creep indevido.
4. Criar presets e explicações adequadas, se justificadas em UX.

**Entrega:** protótipo de fluxo lobby → run → lobby.

**Gate:** início continua divertido sem muitos desbloqueios e escolhas do lobby não determinam resultado inteiro.

## Fase 5 — Expansão para Spear e Bow

**Objetivo:** testar a generalidade da arquitetura.

Tarefas:

1. Adaptar um subconjunto de mutações sem copiar mecanicamente o comportamento da Gauntlet.
2. Testar boons universais como `Fragmentação`, `Eco` ou `Eletricidade` em três identidades de arma.
3. Validar exceções por tipo de ataque e animação.
4. Inspecionar custo de manutenção de cada novo boon.

**Entrega:** demonstração comparável nas três armas.

**Gate:** sistema permite expansão sem crescimento explosivo de casos especiais.

## Fase 6 — Balanceamento, polimento e escala de conteúdo

**Objetivo:** ampliar conteúdo **após** validar a fundação.

Tarefas:

1. Ajustar pools, distribuição de recompensas, tiers, sinergias e progressão;
2. Refinar VFX/SFX/arte final;
3. Integrar telemetria de dano e relatório de run;
4. Testar runs extremas com muitos efeitos;
5. Revisar acessibilidade, performance, bosses e clareza do HUD;
6. Adicionar novos boons/skills com testes de compatibilidade.

**Entrega:** sistema pronto para crescimento contínuo.

**Gate:** regressões controladas, run legível e custo de novos conteúdos aceitável.

---

# 16. RISCOS E CONTROLES

| Risco | Sinal de alerta | Mitigação obrigatória/recomendada |
|---|---|---|
| **Início sem diversão** | Jogador apenas espera skill rara | Core completo, starter útil e recompensas essenciais garantidas |
| **Boons cosméticos disfarçados** | Só mudam cor e percentual | Exigir mudança observável de comportamento para itens transformadores |
| **Explosão combinatória** | Cada novo item requer dezenas de `if`s | Tags, pipeline composável e exceções declaradas |
| **Proc infinito/queda de FPS** | Hits geram hits sem limite | Orçamento de eventos, regras de origem, pooling e testes |
| **Visual ilegível** | Boss/telegraph escondido | Hierarquia visual, budgets, culling, acessibilidade |
| **Perda de identidade da arma** | Três armas terminam iguais | Core e resolvers específicos de arma, variantes de efeitos |
| **Run determinada pelo lobby** | Starter resolve toda build | Progresso lateral e alto espaço de escolhas durante run |
| **Migração destrutiva** | Combate aprovado piora | Vertical slice Gauntlet, benchmark e plano de rollback |
| **Skill swap inconsistente** | Buff/visual/cooldown fica órfão | Troca transacional + reconciliação total de estado |
| **Telemetria enganosa** | Proc contado em múltiplas categorias | Identificador de origem, atribuição hierárquica e teste de reconciliação |
| **Arte muito cara** | Cada combinação exige personagem novo | VFX/material/malha modular e poucas transformações especiais curadas |

---

# 17. DECISÕES ABERTAS E PADRÕES RECOMENDADOS

Estes pontos **não** devem ser assumidos como resolvidos pela IA. Na ausência de decisão do criador, utilizar o padrão recomendado apenas no protótipo e registrar a hipótese.

| Questão | Padrão para protótipo | Exige aprovação para versão definitiva? |
|---|---|---|
| Finisher E vira ação inerente ou skill modular? | **Preservar E existente** e sua semântica | **Sim** |
| Quantas skills iniciais? | Core funcional + ao menos uma opção ativa útil | Sim, após playtest |
| Skills são específicas de arma ou universais? | Específicas por padrão; universais apenas com comportamento projetado | Sim |
| Raridade e tiers | Separar raridade da mutação, especialização e poder | Sim, para economia |
| Remover/substituir itens durante run? | Suportar tecnicamente reversão; UI de remoção é opcional | Sim |
| Até onde mutações podem alterar rig/animação? | Começar com camadas modulares e pequenas substituições | Sim, após validação visual |
| Quantidade de salas, duração e drop rates? | **Medir estado atual**, depois propor números | Sim |
| Quais interações geram novos procs? | Só regras explícitas e com budget | Sim, por classe de evento |
| Meta-progresso permite starter boon? | Opção limitada, preferencialmente sidegrade | Sim |

---

# 18. CHECKLIST DE ACEITE DE DESIGN ANTES DE IMPLEMENTAR

A IA responsável pelo planejamento deve confirmar e registrar:

- [ ] Leu as regras reais do repositório e as skills de design existentes.
- [ ] Inspecionou o projeto Unity atual; não inventou caminhos/classes.
- [ ] Mapeou o combate/boons/skills atuais para a nova visão.
- [ ] Explicou exatamente como preservar o game feel da Gauntlet e o finisher E.
- [ ] Definiu contratos de modificação e regras de ordem/prioridade/conflito.
- [ ] Definiu forma de combinar efeitos sem codificar todos os pares manualmente.
- [ ] Incluiu política contra recursão/procs infinitos e duplicação de dano.
- [ ] Projetou aparência modular e reversível ligada ao estado real da build.
- [ ] Incluiu proteção de telegráficos e leitura de combate.
- [ ] Especificou UX de recompensas e skill swap durante a run.
- [ ] Evitou dependência excessiva de raridade/álgebra de dano.
- [ ] Projetou progressão do lobby como expansão de possibilidades.
- [ ] Restringiu a primeira implementação a uma vertical slice auditável.
- [ ] Definiu testes automatizados, capturas Unity MCP e evidências de playtest.
- [ ] Criou requirements/design/tasks mapeáveis sem fazer migração massiva antecipada.

---

# 19. PROMPT DE REPASSE PARA O CHAT DE PLANEJAMENTO / CODEX / CLAUDE

> **Instrução para o agente que receber este documento:**
>
> Você está ajudando a evoluir um ARPG roguelite feito em Unity 3D/C#. Leia integralmente este documento como **visão de produto** e use-o para elaborar um plano de implementação que respeite o repositório real.
>
> **Antes de propor ou implementar**, descubra e leia `AGENTS.md`, a documentação do projeto e **as skills de design criadas pelo autor no repositório**. Não invente nomes, caminhos, classes, mecânicas já implementadas nem afirme ter executado validações que não ocorreram.
>
> Primeiro execute uma **auditoria do estado atual**: Gauntlet, Spear, Bow, ataque básico, Q/W/E/R, boons, fluxo de salas, recompensas, modelos 3D, animações, VFX, HUD, eventos e testes. Preserve o combate já aprovado, incluindo a diferença intencional entre reações de ataques básicos e finalizadores da Gauntlet.
>
> Depois gere uma **análise de lacunas**, uma **proposta de arquitetura compatível**, **requirements**, **design**, **tasks faseadas**, **matriz de riscos**, **critérios de aceite** e **plano de validação por Unity MCP/playtests**. Priorize uma vertical slice da Gauntlet que prove mutações de ataque, sinergias emergentes, transformação visual e skills trocáveis durante a run.
>
> **Não reescreva todos os boons, armas ou inimigos imediatamente.** Não substitua o combate existente por um protótipo tecnicamente mais simples porém menos divertido. Marque hipóteses e decisões abertas, mostre trade-offs e peça aprovação nos gates antes de mudanças estruturais irreversíveis.
>
> O resultado esperado não é apenas aumentar números: o jogador deve conseguir terminar uma run **visualmente e mecanicamente transformado** pelos itens adquiridos, em um jogo que preserva impacto, ação, identidade de arma e leitura dos inimigos.

---

# 20. RESUMO EXECUTIVO PARA EVITAR DESVIO DE ESCOPO

**O jogo desejado combina:**

- **Lost Ark** para a execução e o peso do combate;
- **The Binding of Isaac** para alterações radicais, cumulativas e visualmente marcantes dos ataques/personagem;
- **Shape of Dreams** para adquirir, evoluir e trocar skills durante a run, além de personalização inicial no lobby;
- **Diablo** para leitura de ARPG/HUD e estados de combate;
- **Direção de arte estilizada** e leitura visual coerente com a proposta atual do projeto.

**A prioridade NÃO é:** fazer mais 46 boons com porcentagens diferentes, implementar árvore enorme imediatamente, mudar todas as armas ao mesmo tempo ou encher a tela de partículas.

**A prioridade É:** provar com uma Gauntlet jogável que poucos itens bem desenhados podem alterar **o comportamento do básico**, criar **sinergias reais**, transformar **a aparência do personagem**, interagir com **skills modulares**, preservar **o game feel** e entregar runs fundamentalmente diferentes.

> **Critério final de direção:** uma pessoa vendo a mesma Gauntlet no início e no fim da run deve perceber, sem ler a lista de itens, que aquele lutador se transformou profundamente — e, ao jogar, sentir que as decisões da run mudaram suas regras de combate.
