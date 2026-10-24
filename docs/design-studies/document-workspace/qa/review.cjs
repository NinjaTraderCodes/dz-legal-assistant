const {chromium}=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1200}}),errors=[],external=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))external.push(r.url());});
  await page.goto('file:///'+path.join(root,'index.html').replaceAll('\\','/'));
  let checks=0;
  for(const language of ['ar','en'])for(const size of ['1366x768','1920x1080'])for(const state of ['editing','blocked'])for(const finish of ['solid','glass']){
   await page.click('[data-view=single]');
   await page.selectOption('#language',language);await page.selectOption('#size',size);await page.selectOption('#state',state);await page.selectOption('#finish',finish);
   for(const mode of ['pair','single','wipe','finishes']){
    await page.click(`[data-view=${mode}]`);
    await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));
    assert.equal(await page.locator(`[data-view=${mode}]`).getAttribute('aria-pressed'),'true');
    assert.equal(await page.locator('#pair').isVisible(),mode!=='wipe');
    assert.equal(await page.locator('#before-frame').isVisible(),mode==='pair'||mode==='finishes');
    assert.equal(await page.locator('#after').getAttribute('src'),`renders/${mode==='finishes'?'glass':finish}-${language}-${state}-${size}.png`);
    if(mode==='finishes') assert.equal(await page.locator('#before').getAttribute('src'),`renders/solid-${language}-${state}-${size}.png`);
    if(mode==='wipe'){
     await page.locator('#divider').focus();await page.keyboard.press('End');assert.equal(await page.locator('#wipe').evaluate(e=>e.style.getPropertyValue('--split')),'100%');
     await page.keyboard.press('Home');assert.equal(await page.locator('#wipe').evaluate(e=>e.style.getPropertyValue('--split')),'0%');
    }
    checks++;
   }
  }
  for(const language of ['ar','en'])assert.ok(fs.existsSync(path.join(root,`renders/solid-${language}-manual-1920x1080.png`)));
  const proof=fs.readFileSync(path.join(root,'renders/page-proof.png'));
  assert.equal(proof.readUInt32BE(16),794);assert.equal(proof.readUInt32BE(20),1123);
  await page.click('[data-view=single]');await page.selectOption('#language','en');await page.selectOption('#size','1920x1080');await page.selectOption('#state','editing');await page.selectOption('#finish','glass');
  await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));
  await page.screenshot({path:path.join(root,'refinement-overview.png'),fullPage:true});
  await page.setViewportSize({width:720,height:1000});
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=window.innerWidth+1));
  assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
  fs.writeFileSync(path.join(root,'qa/results.json'),JSON.stringify({selectorCombinations:16,comparisonModes:4,checks,pageErrors:errors,externalRequests:external,narrowGalleryNoHorizontalOverflow:true,pageProofDimensions:[794,1123]},null,2));
  console.log(`${checks} gallery combinations passed; manual-state links and page proof present; no page errors or external requests.`);
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
