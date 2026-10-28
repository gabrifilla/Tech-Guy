/* Design prototype only. Reads an exported, Store-validated DTO. Never talks to Unity or writes a save. */
(() => {
  'use strict';
  const $ = s => document.querySelector(s);
  const clone = v => JSON.parse(JSON.stringify(v));
  const original = clone(window.RECORDED_SUMMARY);
  const names = {Basic:'Ataque básico',Q:'Ação Q',W:'Ação W',E:'Ação E',R:'Ação R',Unattributed:'Origem de habilidade incerta'};
  const weapons = {Gauntlet:'Manopla',Bow:'Arco',Spear:'Lança',Unknown:'Arma não identificada'};
  const titles = {weapon_FlurryEcho:'Mil punhos',conductor:'Conductor',ignite:'Ignite',frost:'Frost',detonation:'Detonation',reactor:'Reactor',resonance:'Resonance',overflow:'Overflow',mutation_gauntlet_eclipse_uppercut:'Eclipse Uppercut',augment_gauntlet_violent_fall:'Queda Violenta'};
  const families = {Mutation:'Mutation',Augment:'Augment',Protocol:'Protocol',Resonance:'Resonance',Mythic:'Mythic',TechniqueOrLegacy:'Outros efeitos'};
  const outcome = {
    Victory:['Extração concluída','Run concluída por extração.','#dfc38b'],
    Death:['Morte na incursão','Seu personagem foi derrotado.','#e5a19a'],
    Aborted:['Incursão abandonada','Você encerrou o percurso antes da extração.','#b8cbd9']
  };
  const fmt = n => Number.isFinite(n) ? n>0 && n<.1 ? '<0,1' : n.toLocaleString('pt-BR',{maximumFractionDigits:1}) : '—';
  const escape = s => String(s ?? '').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const time = n => Number.isFinite(n) ? `${Math.floor(n/60).toString().padStart(2,'0')}:${Math.floor(n%60).toString().padStart(2,'0')}` : '—';
  const percent = (n,total) => !total ? '—' : n>0 && n/total*100<.1 ? '<0,1%' : `${fmt(n/total*100)}%`;
  const label = id => titles[id] || id || 'Identidade indisponível';
  let data=clone(original), tab='summary', sourceFilter='secondary', scenario='real';
  let valid=true, longText=false;
  const validSummary = s => s && s.Finalized && s.SchemaVersion===1 && outcome[s.Outcome] &&
    [s.EffectiveDamage,s.DamageReceived,s.EnemiesDefeated,s.WallSeconds,s.ObservedEnemyHpLoss,s.UnattributedEnemyHpLoss].every(n=>Number.isFinite(n)&&n>=0) &&
    Array.isArray(s.Damage) && s.Damage.every(r=>r&&Number.isFinite(r.Effective)&&r.Effective>=0) &&
    Math.abs(s.Damage.filter(r=>r.PlayerAttributed).reduce((n,r)=>n+r.Effective,0)-s.EffectiveDamage)<.001 &&
    Math.abs(s.Damage.reduce((n,r)=>n+r.Effective,0)-s.ObservedEnemyHpLoss)<.001;
  function groups(){
    const map=new Map();
    for(const r of data.Damage.filter(r=>r.PlayerAttributed)){
      const id=`${r.Weapon}:${r.Ability}`;
      if(!map.has(id))map.set(id,{id,weapon:r.Weapon,ability:r.Ability,total:0,native:0,child:0,rows:[]});
      const g=map.get(id);g.total+=r.Effective;g[r.Secondary?'child':'native']+=r.Effective;g.rows.push(r);
    }
    return [...map.values()].sort((a,b)=>b.total-a.total||a.id.localeCompare(b.id));
  }
  const groupName=g=>`${names[g.ability]||g.ability}${g.weapon!=='Unknown' ? ' · '+(weapons[g.weapon]||g.weapon):''}`;
  function warning(){
    const errors=data.ObserverErrors>0;
    const unknown=data.Damage.filter(r=>r.PlayerAttributed&&r.Ability==='Unattributed').reduce((n,r)=>n+r.Effective,0);
    const outside=data.UnattributedEnemyHpLoss;
    const notes=[];
    if(errors)notes.push('<strong>Registro parcial.</strong> A coleta pode não cobrir toda a run.');
    if(unknown>0)notes.push(`${fmt(unknown)} HP incluídos no total têm habilidade de origem incerta.`);
    if(outside>0)notes.push(`${fmt(outside)} HP sem autoria confirmada estão <strong>fora do seu total.</strong>`);
    return notes.length?`<div class="hint">${notes.join(' ')}</div>`:'';
  }
  function abilityRow(g){
    return `<button class="ability ${g.ability==='Unattributed'?'ambiguous':''}" data-group="${escape(g.id)}" aria-label="${escape(groupName(g))}, ${fmt(g.total)} de dano, ${percent(g.total,data.EffectiveDamage)} do total. Abrir detalhes."><span class="key">${g.ability==='Basic'?'ATQ':g.ability==='Unattributed'?'?':escape(g.ability)}</span><span class="name">${escape(groupName(g))}</span><span class="amount">${fmt(g.total)}<span>${percent(g.total,data.EffectiveDamage)}</span></span><span class="track" aria-hidden="true"><span class="native-fill" style="width:${g.native/data.EffectiveDamage*100}%"></span><span class="child-fill" style="width:${g.child/data.EffectiveDamage*100}%"></span></span></button>`;
  }
  function peek(){
    if(!Array.isArray(data.Build))return '<p class="note">Dados de aquisição indisponíveis.</p>';
    if(!data.Build.length)return '<p class="note">Nenhum boon adquirido nesta run.</p>';
    return `<div class="build-peek">${data.Build.slice(0,3).map(x=>`<span class="chip ${x.Family==='Mutation'||x.Family==='Augment'?'violet':''}" title="${escape(x.Id)}"><b>${escape(families[x.Family]||'Categoria não identificada')}</b>${escape(label(x.Id))} ×${x.Acquisitions}</span>`).join('')}</div>${data.Build.length>3?`<p class="note">Mais ${data.Build.length-3} itens no registro.</p>`:''}`;
  }
  function renderOverview(){
    const gs=groups(), leader=gs.find(g=>g.ability!=='Unattributed'&&g.total>0);
    return `${warning()}<div class="overview"><section><div class="section-head"><h2>Dano por habilidade</h2><small>Participação no seu total</small></div><div class="legend"><span><i class="native"></i>Nativo</span><span><i class="child"></i>Secundário incluído</span></div>${data.EffectiveDamage>0?gs.map(abilityRow).join(''):'<div class="empty"><span class="mark">0</span><h3>Nenhum dano efetivo registrado</h3><p>Sem porcentagens ou habilidade de maior dano.</p></div>'}<p class="note">Efeitos já incluídos no total. Selecione uma habilidade para detalhar.</p></section><aside class="right"><div class="highlight"><span class="eyebrow">MAIOR DANO REGISTRADO</span><strong>${leader?escape(names[leader.ability]||leader.ability):'Sem destaque de dano'}</strong><p>${leader?`${fmt(leader.total)} HP · ${percent(leader.total,data.EffectiveDamage)} do total, incluindo efeitos filhos.`:'A run não registrou dano atribuível a uma habilidade.'}</p></div><h3>Adquiridos na run</h3>${peek()}<button class="link" data-open-tab="build">Explorar build e percurso →</button><p class="note">Nem todo item adquirido causou dano. Consulte as fontes confirmadas.</p></aside></div>`;
  }
  function renderSources(){
    let rows=data.Damage.filter(r=>r.PlayerAttributed);
    if(sourceFilter==='secondary')rows=rows.filter(r=>r.Secondary);
    if(sourceFilter==='native')rows=rows.filter(r=>!r.Secondary);
    rows=[...rows].sort((a,b)=>b.Effective-a.Effective);
    const subtotal=rows.reduce((n,r)=>n+r.Effective,0);
    return `${warning()}<div class="source-header"><div><h2>Efeitos e impactos confirmados</h2><p>Esta é outra leitura do mesmo dano. Não some ao total por habilidade.</p></div><label>Mostrar <select id="source-filter"><option value="secondary" ${sourceFilter==='secondary'?'selected':''}>Efeitos secundários</option><option value="native" ${sourceFilter==='native'?'selected':''}>Impactos nativos</option><option value="all" ${sourceFilter==='all'?'selected':''}>Todas as parcelas</option></select></label></div>${rows.length?`<table class="data-table"><thead><tr><th scope="col">Fonte</th><th scope="col">Habilidade de origem</th><th scope="col">Tipo</th><th scope="col">Dano efetivo</th></tr></thead><tbody>${rows.map(r=>`<tr><td>${escape(r.Producer==='Direct'?'Impacto nativo':r.Producer==='Echo'?label(r.ProducerId):r.Producer==='BurnTick'?'Ignite · queimadura':r.Producer)}${r.MutationId?`<small> · ${escape(label(r.MutationId))}</small>`:''}${r.Attribution==='mixed_burn_origins'?'<small> · aplicações misturadas</small>':''}</td><td>${escape(names[r.Ability]||r.Ability)}</td><td>${r.Secondary?'Secundário':'Nativo'}</td><td>${fmt(r.Effective)}</td></tr>`).join('')}</tbody><tfoot><tr><td colspan="3">Subtotal desta leitura · já incluído no total</td><td>${fmt(subtotal)}</td></tr></tfoot></table>`:'<div class="empty"><h2>Nenhum impacto deste tipo registrado</h2><p>Um efeito pode ter sido adquirido sem causar dano nesta run. Não há contribuição presumida.</p></div>'}`;
  }
  function renderBuild(){
    const acquisition=Array.isArray(data.Build)?data.Build:null;
    const rewards=Array.isArray(data.Rewards)?data.Rewards:null;
    const sort=['Mutation','Augment','Protocol','Resonance','TechniqueOrLegacy','Mythic'];
    return `${warning()}<div class="build-layout"><section><div class="section-head"><h2>Adquiridos na run</h2><small>Boons obtidos ao longo do percurso</small></div>${!acquisition?'<div class="empty"><h3>Build indisponível</h3><p>O registro não contém a lista de aquisições. Isso não significa uma build vazia.</p></div>':!acquisition.length?'<div class="empty"><h3>Nenhum boon adquirido</h3><p>Lista disponível e vazia nesta run.</p></div>':sort.concat([...new Set(acquisition.map(r=>r.Family))].filter(f=>!sort.includes(f))).filter(f=>acquisition.some(x=>x.Family===f)).map(f=>`<h3 class="category">${escape(families[f]||'Categoria não identificada')}</h3><div class="build-grid">${acquisition.filter(x=>x.Family===f).map(x=>`<article class="build-card" title="${escape(x.Id)}"><h3>${escape(label(x.Id))}${longText?' — configuração de apresentação para testar nomes extensos e expansão de localização':''}</h3><small>${x.Acquisitions} aquisição${x.Acquisitions!==1?'s':''}</small></article>`).join('')}</div>`).join('')}</section><aside class="right"><h2>Recompensas registradas</h2>${!rewards?'<p class="note">Histórico indisponível.</p>':!rewards.length?'<p class="note">Nenhuma recompensa adquirida.</p>':`<ol class="progress-list">${rewards.map((r,i)=>`<li><strong>${i+1}. ${escape(label(r.Id))}</strong>${escape(r.Kind)} · ${time(r.ScaledSeconds)} de tempo simulado${r.Alternative?' · alternativa confirmada':''}</li>`).join('')}</ol>`}<p class="note">A lista mostra o que foi adquirido. Não certifica configuração ativa por slot no fim da run.</p><details class="technical"><summary>Identidade e dados adicionais</summary><p>Run ${escape(data.SessionId)}<br>Seed ${escape(data.Seed)}<br>Início UTC ${escape(data.StartedUtc)}<br>Tempo simulado: ${time(data.ScaledSeconds)}<br>Crédito letal confirmado: ${fmt(data.PlayerCreditedKills)}<br>Maior impacto efetivo: ${fmt(data.LargestImpact)} HP<br>Overkill: ${data.OverkillComplete?fmt(data.Overkill)+' HP':'dado parcial'}<br>Fases registradas: ${Array.isArray(data.Progress)?data.Progress.length:'indisponível'}</p></details></aside></div>`;
  }
  function openGroup(id,button){
    const g=groups().find(g=>g.id===id);if(!g)return;
    const a=data.Abilities?.find(x=>x.Weapon===g.weapon&&x.Ability===g.ability);
    $('#detail-title').textContent=groupName(g);
    const mutations=[...new Set(g.rows.map(x=>x.MutationId).filter(Boolean))];
    $('#detail-body').innerHTML=`<dl class="detail-facts"><dt>Dano efetivo total</dt><dd>${fmt(g.total)} HP</dd><dt>Impactos nativos</dt><dd>${fmt(g.native)} HP</dd><dt>Efeitos secundários incluídos</dt><dd>${fmt(g.child)} HP</dd><dt>Usos registrados</dt><dd>${a?fmt(a.Uses):'— · indisponível'}</dd><dt>Acertos nativos / secundários</dt><dd>${a?fmt(a.NativeHpTransactions)+' / '+fmt(a.SecondaryHpTransactions):'— · indisponível'}</dd></dl><p>Acertos contam cada alvo que perdeu vida. Um golpe pode acertar mais de um inimigo; isso não mede precisão de mira.</p>${mutations.length?`<p>Transformação registrada: ${mutations.map(id=>escape(label(id))).join(', ')}. O dano continua pertencendo à habilidade; não há uma parcela exclusiva atribuída à Mutation.</p>`:''}<p>Maior dano não equivale a maior importância tática. Controle, defesa e criação de oportunidades não foram pontuados.</p>${g.ability==='Unattributed'?'<p class="hint">O dano é confirmado, mas a habilidade de origem não foi resolvida. Não há divisão estimada entre possíveis pais.</p>':''}`;
    $('#detail').showModal();$('#close-detail').focus();
  }
  function render(){
    valid=validSummary(data);
    const state=valid?outcome[data.Outcome]:['Resultado indisponível','Não foi possível recuperar um resumo válido.','#b8cbd9'];
    document.documentElement.style.setProperty('--outcome',state[2]);
    $('#outcome').textContent=state[0];$('#outcome-copy').textContent=(longText?state[1]+' Texto expandido para avaliação de localização e leitura.':state[1])+(valid&&data.ObserverErrors>0?' Registro parcial.':'');
    $('#weapon').textContent=valid?`${weapons[data.InitialWeapon]||data.InitialWeapon} / ARMA INICIAL`:'ARMA —';
    $('#duration').textContent=valid?time(data.WallSeconds):'—';
    $('#run-id').textContent=valid?`REGISTRO ${data.SessionId.slice(0,8).toUpperCase()}`:'SEM REGISTRO DISPONÍVEL';
    $('#run-id').title=valid?data.SessionId:'';
    $('#damage').textContent=valid?fmt(data.EffectiveDamage):'—';$('#kills').textContent=valid?fmt(data.EnemiesDefeated):'—';$('#received').textContent=valid?fmt(data.DamageReceived):'—';
    $('#provenance').textContent=scenario==='real'?'SNAPSHOT REAL · STORE':scenario==='empty'||scenario==='invalid'?'ESTADO DE EXCEÇÃO SIMULADO':'DADOS FICTÍCIOS · ESTUDO';
    document.querySelectorAll('[role=tab]').forEach(b=>{b.disabled=!valid;b.setAttribute('aria-selected',String(b.dataset.tab===tab));b.tabIndex=b.dataset.tab===tab?0:-1;});
    $('#panel').setAttribute('aria-labelledby',`tab-${tab}`);
    $('#panel').innerHTML=!valid?'<div class="empty"><span class="mark">—</span><h2>Sem estatísticas disponíveis</h2><p>A ausência de um resumo não revela se houve vitória, morte ou abandono. Nenhum valor foi preenchido com zero.</p><p>Você pode continuar no Nexus.</p></div>':tab==='summary'?renderOverview():tab==='sources'?renderSources():renderBuild();
    $('#panel').scrollTop=0;
    document.querySelectorAll('[data-group]').forEach(b=>b.addEventListener('click',()=>openGroup(b.dataset.group,b)));
    document.querySelectorAll('[data-open-tab]').forEach(b=>b.addEventListener('click',()=>switchTab(b.dataset.openTab,true)));
    if($('#source-filter'))$('#source-filter').addEventListener('change',e=>{sourceFilter=e.target.value;render();$('#source-filter').focus();});
    $('#announcement').textContent=`${state[0]}. ${valid?'Dano efetivo '+fmt(data.EffectiveDamage)+'.':''}`;
  }
  function switchTab(next,focus){tab=next;render();if(focus)$(`#tab-${tab}`).focus();}
  function preset(value){
    scenario=value;data=clone(original);tab='summary';sourceFilter='secondary';longText=false;
    if(value!=='real'){data.SessionId='00000000-0000-4000-8000-000000000001';data.Seed='0';data.StartedUtc='2026-10-21T12:00:00.000Z';}
    $('#nexus').hidden=true;$('#app').hidden=false;
    if(['victory','death','aborted'].includes(value))data.Outcome={victory:'Victory',death:'Death',aborted:'Aborted'}[value];
    if(value==='empty')data=null;
    if(value==='invalid')data.SchemaVersion=999;
    if(value==='partial'){
      data.ObserverErrors=1;data.Diagnostic='synthetic_partial_record';data.Build=null;
      const row=data.Damage.find(r=>r.PlayerAttributed);row.Ability='Unattributed';row.Weapon='Unknown';row.Attribution='mixed_burn_origins';
      data.Abilities=data.Abilities.filter(a=>data.Damage.some(r=>r.Ability===a.Ability&&r.Weapon===a.Weapon));
      data.UnattributedEnemyHpLoss=12;data.ObservedEnemyHpLoss=data.EffectiveDamage+12;
      data.Damage.push({Weapon:'Unknown',Ability:'Unattributed',Producer:'Unattributed',ProducerId:'',PlayerAttributed:false,Secondary:false,Attribution:'no_player_provenance',Effective:12,Overkill:0,HpTransactions:1});
      data.Abilities.forEach(a=>{const rows=data.Damage.filter(r=>r.PlayerAttributed&&r.Ability===a.Ability&&r.Weapon===a.Weapon);a.EffectiveIncludingChildren=rows.reduce((n,r)=>n+r.Effective,0);a.NativeHpTransactions=rows.filter(r=>!r.Secondary).reduce((n,r)=>n+r.HpTransactions,0);a.SecondaryHpTransactions=rows.filter(r=>r.Secondary).reduce((n,r)=>n+r.HpTransactions,0);});
    }
    if(value==='zero'){
      data.EffectiveDamage=data.ObservedEnemyHpLoss=data.UnattributedEnemyHpLoss=data.DamageReceived=data.Overkill=data.LargestImpact=data.EnemiesDefeated=data.PlayerCreditedKills=data.ConfirmedHpTransactions=0;
      data.Damage=[];data.Abilities=[];data.Build=[];data.Rewards=[];data.HighestDamageAbility='';data.HighestDamageSource='';
    }
    if(value==='many')data.Build=[...data.Build,{Id:'mutation_gauntlet_eclipse_uppercut',Family:'Mutation',Acquisitions:1},{Id:'augment_gauntlet_violent_fall',Family:'Augment',Acquisitions:1},...['ignite','frost','detonation','reactor','resonance','overflow'].map(Id=>({Id,Family:'Protocol',Acquisitions:3}))];
    if(value==='skew'){
      const own=data.Damage.filter(x=>x.PlayerAttributed);own.forEach((r,i)=>r.Effective=i===0?999999.9:.01);
      data.EffectiveDamage=own.reduce((n,r)=>n+r.Effective,0);data.ObservedEnemyHpLoss=data.EffectiveDamage;data.UnattributedEnemyHpLoss=0;data.Abilities=[];
    }
    if(value==='long'){longText=true;data.Build.push({Id:'synthetic_localization_test_only',Family:'Augment',Acquisitions:1});titles.synthetic_localization_test_only='Nome fictício excepcionalmente extenso para avaliar quebra de linha e localização';}
    render();
  }
  document.querySelectorAll('[role=tab]').forEach(b=>{
    b.addEventListener('click',()=>switchTab(b.dataset.tab,false));
    b.addEventListener('keydown',e=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(e.key))return;e.preventDefault();const tabs=['summary','sources','build'];let i=tabs.indexOf(tab);i=e.key==='Home'?0:e.key==='End'?2:(i+(e.key==='ArrowRight'?1:2))%3;switchTab(tabs[i],true);});
  });
  $('#scenario').addEventListener('change',e=>preset(e.target.value));
  $('#close-detail').addEventListener('click',()=>$('#detail').close());
  $('#continue').addEventListener('click',()=>{$('#app').hidden=true;$('#nexus').hidden=false;$('#reopen').focus();});
  $('#reopen').addEventListener('click',()=>{$('#app').hidden=false;$('#nexus').hidden=true;$('#eyebrow').textContent='NEXUS / CONSULTA DO ÚLTIMO RESULTADO';render();$('#continue').focus();});
  window.prototypeAudit=()=>({scenario,valid,tab,total:data?.EffectiveDamage,groups:valid?groups().map(g=>({id:g.id,total:g.total,native:g.native,child:g.child})):[],sourceRows:valid?clone(data.Damage):[],runtimeConnected:false});
  preset('real');
})();
