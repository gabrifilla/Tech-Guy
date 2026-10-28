# ADR-0002 — Separar stagger, launch, push, knockback e stance

- **Status:** ACEITA (direção de gameplay)
- **Contexto:** desejo de combates com interrupção e air juggle à Lost Ark, mas não de empurrão exagerado em cada hit.
- **Decisão:** básicos apropriados podem gerar micro-stagger/interrupção em comuns; launch/air juggle apenas quando a skill e o perfil do alvo permitem, tipicamente em condições de stance break. Push controlado é diferente de knockback. Elite/boss possuem resistência/fallback específicos; não impor imunidade geral ou stunlock universal.
- **Consequência:** comportamento deve ser configurado e validado por categoria, sem misturar animação cosmética e controle mecânico.
- **Validação:** timecodes de vídeo e eventos do runtime por tipo de alvo.
