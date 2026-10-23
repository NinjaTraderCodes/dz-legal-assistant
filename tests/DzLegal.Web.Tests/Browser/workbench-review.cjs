// Real browser regression/visual review. No network model is needed. Use a local server with API key variables empty.
// PLAYWRIGHT_MODULE, CHROMIUM_PATH, optional AXE_PATH, R3_WEB_URL, R3_BROWSER_OUTPUT.
const {chromium} = require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const output = process.env.R3_BROWSER_OUTPUT || path.resolve('redteam-output/r3-web-qa');
const url = process.env.R3_WEB_URL || 'http://127.0.0.1:5169';
const inputs = JSON.parse(fs.readFileSync(path.resolve('tests/drafting-cases/01-debt-arabic.json'),'utf8')).inputs;
const documentFixture = {documentTypeId:'requete-introductive',definitionVersion:'0.9.0',inputs,
  sections:[{id:'grounds',text:inputs['notes.grounds'],aiWritten:true,reviewed:false}],createdAt:new Date().toISOString()};
const wait = page => page.waitForTimeout(180);
(async()=>{
 fs.mkdirSync(output,{recursive:true});
 const browser = await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH});
 const results=[];
 try {
  const healthContext=await browser.newContext();
  const health = await healthContext.request.get(url+'/healthz');
  assert.equal(health.status(),200); assert.equal((await health.json()).status,'ok');
  assert.equal((await healthContext.request.head(url+'/healthz')).status(),200);await healthContext.close();
  for(const [width,height] of [[1366,768],[1920,1080]]) for(const scale of [1,1.25]) for(const lang of ['ar','en']) for(const theme of ['light','dark']) {
   const key=`${width}x${height}-${scale*100}-${lang}-${theme}`;
   const context=await browser.newContext({viewport:{width:Math.round(width/scale),height:Math.round(height/scale)},deviceScaleFactor:scale,colorScheme:theme,acceptDownloads:true});
   const page=await context.newPage(), errors=[], external=[];
   page.setDefaultTimeout(12000);
   page.on('pageerror',e=>errors.push(e.message));
   page.on('request',r=>{if(/^https?:/.test(r.url())&&!r.url().startsWith(url))external.push(r.url());});
   await page.goto(url+'/?lang='+lang); await page.locator('.steps .step').first().waitFor(); await wait(page);
   const shot=async state=>{await wait(page);await page.screenshot({path:path.join(output,`${state}-${key}.png`)});};
   await page.locator('.steps .step').nth(4).click();
   await shot('filled');
   const geometry=await page.locator('.columns').evaluate(e=>({w:e.clientWidth,s:e.scrollWidth,
     form:e.querySelector('.editor-stack').getBoundingClientRect().toJSON(),draft:e.querySelector('.draft-panel').getBoundingClientRect().toJSON(),
     tabs:e.querySelector('.tool-tabs').getBoundingClientRect().toJSON()}));
   assert.ok(geometry.s<=geometry.w+1); assert.ok(geometry.draft.x>=geometry.form.right); assert.ok(geometry.tabs.bottom<=height/scale);
   assert.equal(await page.locator('#f-notes-facts').getAttribute('dir'),'rtl');
   // Disclosure works on focus, Escape, hover and pinned click.
   const summary=page.locator('.field-help>summary').first(), disclosure=page.locator('.field-help').first();
   await summary.focus(); assert.equal(await disclosure.getAttribute('open'),'');
   await page.keyboard.press('Escape'); assert.equal(await disclosure.getAttribute('open'),null);
   await summary.hover(); assert.equal(await disclosure.getAttribute('open'),'');
   await summary.click(); await page.locator('.editor-head h3').hover(); assert.equal(await disclosure.getAttribute('open'),'');
   await page.keyboard.press('Escape');
   // Palette exact section ranking and focus restoration in either language.
   const prior=page.locator('#f-notes-facts'); await prior.focus(); await page.keyboard.press('Control+k');
   const query=page.locator('#command-query'); await query.waitFor();
   const label=await page.locator('.steps .step').first().getAttribute('aria-label');
   await query.fill(label); await wait(page); assert.equal(await page.locator('.command-results button').first().innerText(),label);
   await shot('palette'); await page.keyboard.press('Escape'); await query.waitFor({state:'hidden'});
   try { await page.waitForFunction(()=>document.activeElement.id==='f-notes-facts'); }
   catch(e) { console.log(await page.evaluate(()=>({active:document.activeElement.outerHTML,previous:window.dzlegal._workbench.returnFocus?.outerHTML,last:window.dzlegal._workbench.lastNonPaletteFocus?.outerHTML,was:window.dzlegal._workbench.wasPalette})));throw e; }
   // All three splitters: key resize, reset, preview drag, commit and persistence.
   const editor=page.locator('[data-resize=editor]'); await editor.focus();
   await page.keyboard.press('ArrowRight'); await wait(page);
   assert.equal(await editor.getAttribute('aria-valuenow'),'456');
   await editor.dblclick(); await wait(page);
   const b=await editor.boundingBox(); const original=await page.locator('.editor-stack').evaluate(e=>e.clientWidth);
   await page.mouse.move(b.x+3,b.y+60);await page.mouse.down();await page.mouse.move(b.x+43,b.y+60);
   assert.equal(await page.locator('.editor-stack').evaluate(e=>e.clientWidth),original); assert.equal(await page.locator('.resize-ghost').count(),1);
   await page.mouse.up(); await wait(page); assert.ok(await page.locator('.editor-stack').evaluate(e=>e.clientWidth)>original);
   await editor.dblclick();
   const compact=await page.locator('#workbench').getAttribute('data-compact')==='true';
   if(compact) {await page.locator('.outline>.quiet').click();await page.waitForFunction(()=>document.getElementById('workbench').dataset.compact==='false');}
   const outline=page.locator('[data-resize=outline]');await outline.focus();await page.keyboard.press('ArrowRight');
   assert.equal(await outline.getAttribute('aria-valuenow'),'240');await outline.dblclick();
   if(compact) {await page.locator('.outline>.quiet').click();await page.waitForFunction(()=>document.getElementById('workbench').dataset.compact==='true');}
   await editor.focus();await page.keyboard.press('ArrowRight');
   for(const [index,state] of [[0,'review'],[1,'source'],[2,'assistant'],[3,'activity']]) {
    await page.locator('.tool-tabs>button').nth(index).click(); await shot(state);
   }
   const tools=page.locator('[data-resize=tools]'); await tools.focus(); await page.keyboard.press('ArrowUp'); await tools.dblclick();
   await page.locator('.tool-tabs>button').first().click();
   // Open a fictitious saved draft whose AI section still needs review. Use the real browser storage recovery offer.
   await page.evaluate(json=>localStorage.setItem('dzlegal.case',JSON.stringify({savedAt:new Date().toISOString(),json})),JSON.stringify(documentFixture));
   await page.reload(); await page.locator('.strip .notice.info button.primary').waitFor(); await shot('recovery');
   assert.equal(await page.locator('[data-resize=editor]').getAttribute('aria-valuenow'),'456');
   await page.locator('.strip .notice.info button.primary').click();
   await page.locator('.reviewed input').waitFor();
   let downloads=0; page.on('download',()=>downloads++);
   const pdf=page.locator('.draft-panel .toolbar button').filter({hasText:/PDF/});
   await pdf.click(); await wait(page); assert.equal(downloads,0); assert.equal(await page.locator('#workbench').getAttribute('data-tool'),'review');
   assert.equal(await page.locator('#workbench').getAttribute('data-tools-open'),'true'); await shot('blocked');
   await page.locator('.reviewed input').check();
   // Run actual exports once per browser configuration. Native tests cover the same guard independently.
   const d1=page.waitForEvent('download'); await pdf.click(); const pdfFile=await d1;
   await pdfFile.saveAs(path.join(output,`export-${key}.pdf`));
   assert.ok(fs.statSync(path.join(output,`export-${key}.pdf`)).size>1000);
   const d2=page.waitForEvent('download');await page.locator('.draft-panel .toolbar button').filter({hasText:/Word/}).click();
   await (await d2).saveAs(path.join(output,`export-${key}.docx`)); await shot('exported');
   assert.equal(downloads,2);
   // No text smaller than the specified minimum, including nested section metadata.
   const tiny=await page.locator('#workbench *').evaluateAll(es=>es.filter(e=>e.getClientRects().length&&[...e.childNodes].some(n=>n.nodeType===3&&n.textContent.trim())&&parseFloat(getComputedStyle(e).fontSize)<12.5).map(e=>({tag:e.tagName,c:e.className,size:getComputedStyle(e).fontSize})));
   assert.deepEqual(tiny,[]);
   let axe=[];
   if(process.env.AXE_PATH){await page.evaluate(fs.readFileSync(process.env.AXE_PATH,'utf8'));axe=(await page.evaluate(async()=>await window.axe.run(document.getElementById('workbench'),{runOnly:{type:'tag',values:['wcag2a','wcag2aa']}}))).violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.map(n=>n.target)}));}
   assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
   await page.locator('.steps .step').first().click();await page.locator('#f-court-name').fill('');await wait(page);
   await page.locator('.appbar .tools>button').nth(1).click();
   await page.locator('.notice[role=alertdialog]').waitFor();await shot('unsaved');
   await page.locator('.notice[role=alertdialog] button').nth(1).click();await wait(page);
   assert.equal(await page.locator('#f-court-name').inputValue(),'');await shot('empty');
   await page.evaluate(()=>localStorage.setItem('dzlegal.workbench.layout','null'));await page.reload();
   await page.locator('.steps .step').first().waitFor();await wait(page);
   assert.equal(await page.locator('[data-resize=editor]').getAttribute('aria-valuenow'),'440');
   assert.deepEqual(errors,[]);
   results.push({key,geometry,errors,external,axe,downloads}); fs.writeFileSync(path.join(output,'results.json'),JSON.stringify(results,null,2));
   console.log(key,'passed',axe.length,'accessibility findings');await context.close();
  }
  for(const lang of ['ar','en']) for(const theme of ['light','dark']) {
   const context=await browser.newContext({viewport:{width:390,height:844},colorScheme:theme});
   const page=await context.newPage();await page.goto(url+'/?lang='+lang);await page.locator('.steps .step').first().waitFor({state:'attached'});await wait(page);
   for(const tab of [0,1,2]) {
    await page.locator('.mobile-tabs button').nth(tab).click();await wait(page);
    const target=page.locator(tab===0?'.form-panel':tab===1?'.draft-panel':'.workbench-tools');assert.equal(await target.isVisible(),true);
    assert.ok((await target.boundingBox()).y<844);
    const overlaps=await page.locator('.check-panel .finding').evaluateAll(es=>es.filter(e=>e.getClientRects().length).flatMap(e=>[...e.children].filter(c=>c.getBoundingClientRect().bottom>e.getBoundingClientRect().bottom+1).map(c=>c.textContent)));
    assert.deepEqual(overlaps,[]);
    for(const button of await page.locator('.appbar .tools>button').all()) {const rect=await button.boundingBox();assert.ok(rect.x>=0&&rect.x+rect.width<=391);}
    await page.screenshot({path:path.join(output,`mobile-${tab}-390-${lang}-${theme}.png`)});
   }
   assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=390));
   await context.close();console.log('mobile',lang,theme,'passed');
  }
 } finally {await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
