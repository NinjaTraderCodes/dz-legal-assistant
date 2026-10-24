const {chromium}=require(process.env.PLAYWRIGHT_MODULE || 'playwright');
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'..');
(async()=>{
 const browser=await chromium.launch({headless:true,executablePath:process.env.CHROMIUM_PATH});
 try{
  const page=await browser.newPage({viewport:{width:1600,height:1200}});const errors=[],external=[];
  page.on('pageerror',e=>errors.push(e.message));page.on('request',r=>{if(/^https?:/.test(r.url()))external.push(r.url());});
  await page.goto('file:///'+path.join(root,'index.html').replaceAll('\\','/'));
  for(const language of ['ar','en'])for(const size of ['1366x768','1920x1080'])for(const state of ['editing','blocked']){
   await page.selectOption('#language',language);await page.selectOption('#size',size);await page.selectOption('#state',state);
   for(const mode of ['pair','proposed','wipe']){
    await page.click(`[data-view=${mode}]`);
    await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));
    assert.equal(await page.locator(`[data-view=${mode}]`).getAttribute('aria-pressed'),'true');
    if(mode==='wipe'){await page.locator('#divider').focus();await page.keyboard.press('End');assert.equal(await page.locator('#wipe').evaluate(e=>e.style.getPropertyValue('--split')),'100%');}
   }
  }
  await page.selectOption('#language','ar');await page.selectOption('#size','1366x768');await page.selectOption('#state','editing');await page.click('[data-view=pair]');
  await page.waitForFunction(()=>[...document.images].every(i=>i.complete&&i.naturalWidth>0));
  await page.screenshot({path:path.join(root,'comparison-overview.png'),fullPage:true});
  assert.deepEqual(errors,[]);assert.deepEqual(external,[]);
  fs.writeFileSync(path.join(root,'qa/results.json'),JSON.stringify({selectorCombinations:8,comparisonModes:3,checks:24,pageErrors:errors,externalRequests:external},null,2));
  console.log('All 8 scene combinations and 3 comparison modes passed; no page errors or external requests.');
 }finally{await browser.close();}
})().catch(e=>{console.error(e);process.exitCode=1;});
