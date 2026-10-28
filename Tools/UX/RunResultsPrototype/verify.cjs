/* Standalone design checks, not Unity/gameplay tests. NODE_PATH must expose Playwright. */
const { chromium } = require('playwright');
const { resolve, join } = require('node:path');
const { mkdirSync, writeFileSync } = require('node:fs');
const { pathToFileURL } = require('node:url');
const assert = require('node:assert/strict');
const output = resolve(__dirname, '../../../Library/GameplayObservation/vs02b1');
mkdirSync(output, { recursive:true });
(async () => {
  const browser = await chromium.launch({ headless:true, channel:process.env.TG_BROWSER_CHANNEL || 'msedge' });
  const page = await browser.newPage(); const errors=[]; const checks=[];
  page.on('pageerror', e=>errors.push(e.message));
  await page.goto(pathToFileURL(join(__dirname,'index.html')).href);
  const screenshot = async name => { await page.evaluate(()=>document.fonts.ready); await page.evaluate(()=>new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)))); await page.screenshot({path:join(output,name),animations:'disabled'}); };
  const scenarios=['real','victory','death','aborted','partial','empty','invalid','zero','many','skew','long'];
  for (const size of [{width:1280,height:720},{width:1920,height:1080}]) {
    await page.setViewportSize(size);
    for (const scenario of scenarios) {
      await page.selectOption('#scenario', scenario);
      const audit=await page.evaluate(()=>window.prototypeAudit());
      assert.equal(audit.runtimeConnected,false);
      if (audit.valid) assert.ok(Math.abs(audit.groups.reduce((n,g)=>n+g.total,0)-audit.total)<.001,'Ability groups must partition the player total.');
      const tabs=audit.valid?['summary','sources','build']:['summary'];
      for (const tab of tabs) {
        if(audit.valid)await page.locator(`#tab-${tab}`).click();
        if(audit.valid)assert.equal(await page.locator(`#tab-${tab}`).getAttribute('aria-selected'),'true');
        const layout=await page.evaluate(()=>({
          horizontal:document.documentElement.scrollWidth>innerWidth,
          bodyVertical:document.documentElement.scrollHeight>innerHeight,
          clipped:[...document.querySelectorAll('h1,.metrics strong,.ability,.chip,.build-card')].filter(e=>e.scrollWidth>e.clientWidth+1).map(e=>e.className||e.tagName),
          panelScroll:document.querySelector('#panel').scrollHeight>document.querySelector('#panel').clientHeight,
          footerVisible:document.querySelector('footer').getBoundingClientRect().bottom<=innerHeight+1
        }));
        assert.equal(layout.horizontal,false,`${scenario}/${tab}: horizontal overflow`);
        assert.equal(layout.bodyVertical,false,`${scenario}/${tab}: page overflow`);
        assert.deepEqual(layout.clipped,[],`${scenario}/${tab}: clipped text`);
        assert.equal(layout.footerVisible,true,'Primary action must remain visible');
        checks.push({size,scenario,tab,...layout});
      }
    }
    await page.selectOption('#scenario','real');
    await screenshot(`overview-${size.height}.png`);
  }
  await page.setViewportSize({width:1280,height:720});
  for(const scenario of ['victory','death','aborted','partial','empty','zero','skew']){
    await page.selectOption('#scenario',scenario);
    await screenshot(`${scenario}-720.png`);
  }
  for(const [scenario,tab,name] of [['real','sources','sources-720'],['many','build','build-extensive-720'],['long','build','localization-720']]){
    await page.selectOption('#scenario',scenario);await page.locator(`#tab-${tab}`).click();
    await screenshot(`${name}.png`);
  }
  await page.selectOption('#scenario','real');
  await page.locator('[data-group]').first().click(); assert.ok(await page.locator('#detail').isVisible());
  await page.keyboard.press('Escape'); assert.equal(await page.locator('#detail').isVisible(),false);
  assert.equal(await page.evaluate(()=>document.activeElement.hasAttribute('data-group')),true,'Modal returns focus');
  await page.locator('#tab-summary').focus(); await page.keyboard.press('ArrowRight');
  assert.equal(await page.locator('#tab-sources').getAttribute('aria-selected'),'true');
  assert.equal(await page.evaluate(()=>document.activeElement.id),'tab-sources');
  await page.locator('#continue').click();assert.ok(await page.locator('#nexus').isVisible());
  await page.keyboard.press('Enter');assert.ok(await page.locator('#app').isVisible());
  await page.selectOption('#scenario','many'); await page.locator('#tab-build').click(); await page.locator('#panel').focus(); await page.keyboard.press('PageDown');
  assert.ok(await page.locator('#panel').evaluate(e=>e.scrollTop)>0,'Long builds must scroll with keyboard');
  await page.selectOption('#scenario','zero');assert.equal(await page.locator('#damage').textContent(),'0');
  assert.equal(await page.locator('.ability').count(),0);
  await page.selectOption('#scenario','empty');assert.equal(await page.locator('#damage').textContent(),'—');
  await page.selectOption('#scenario','skew');assert.ok((await page.locator('.amount').allTextContents()).some(t=>t.includes('<0,1')),'Tiny positive values must not be presented as zero.');
  const contrast=await page.evaluate(()=>{
    const lum=hex=>{const c=hex.match(/[a-f\d]{2}/gi).map(x=>parseInt(x,16)/255).map(c=>c<=.04045?c/12.92:((c+.055)/1.055)**2.4);return c[0]*.2126+c[1]*.7152+c[2]*.0722;};
    const ratio=(a,b)=>{let x=lum(a),y=lum(b);return (Math.max(x,y)+.05)/(Math.min(x,y)+.05);};
    return ['#f1e9d7','#b2bdc9','#d4b476','#82d8e6','#f0c487','#e5a19a'].map(foreground=>({foreground,background:'#131c2a',ratio:ratio(foreground,'#131c2a')}));
  });
  contrast.forEach(c=>assert.ok(c.ratio>=4.5));assert.deepEqual(errors,[]);
  const result={status:'passed',checks:checks.length,layouts:checks,keyboard:['tabs with arrows','modal Escape and focus restoration','Enter reopens last result'],contrast,errors,limits:['Browser prototype only; not Unity Text rendering or input validation.','No screen-reader, controller, HDR, color-vision simulation or human comprehension certification.','Fictional presets are layout stress studies, not valid production builds or gameplay runs.']};
  writeFileSync(join(output,'ux-checks.json'),JSON.stringify(result,null,2));
  console.log(JSON.stringify({status:result.status,layouts:checks.length,contrastMin:Math.min(...contrast.map(c=>c.ratio)),errors}));
  await browser.close();
})().catch(e=>{console.error(e);process.exit(1);});
