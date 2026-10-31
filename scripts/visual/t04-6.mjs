// Opt-in browser validation; external Playwright/Chromium, synthetic data only.
import { createRequire } from 'node:module';
import { mkdir, writeFile, readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
const {chromium}=createRequire(process.env.PLAYWRIGHT_PACKAGE || '/tmp/echo-visual-browser/package.json')('playwright');
const out=process.env.CAPTURE_DIR || 'docs/releases/t04-6-captures';
const base=process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3216';
await mkdir(out,{recursive:true});
const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH,headless:true});
const report={synthetic:true,viewports:[],checks:[],errors:[],requests:[],accessibility:[]};
const names=['Ana','Beto','Carla','Diego','Eva','Fabio','Gina','Hugo','Iris','Juan'];
const champions=['Ahri','Ashe','Lux','Vi','Jinx','Caitlyn','Sona','Garen','Annie','Janna'];
const participants=names.map((name,i)=>({puuid:`fixture-player-${i}`,gameName:name,tagLine:'TEST',championId:i+10,championName:champions[i],kills:i+1,deaths:i%4,assists:i+3,win:i<5}));
const data={
 owner:{puuid:'fixture-player-0',gameName:'Ana',tagLine:'TEST',platformRegion:'la1'},
 summary:{puuid:'fixture-player-0',gameName:'Ana',tagLine:'TEST',matchesAnalyzed:5,wins:3,losses:2,winRate:60,uniquePlayersEncountered:9,repeatedPlayers:2,dataUpdatedAt:'2026-10-27T18:00:00Z'},encounters:[],
 relationships:[{firstPuuid:'fixture-player-0',secondPuuid:'fixture-player-1',matchesTogether:4,allyMatches:3,enemyMatches:1,score:2,confidence:'HIGH'},{firstPuuid:'fixture-player-0',secondPuuid:'fixture-player-5',matchesTogether:2,allyMatches:1,enemyMatches:1,score:1,confidence:'LOW'}],
 coverage:{status:'complete',participants:10,matchIdsObtained:32,uniqueMatchIds:22,matchesRecovered:22,players:participants.map((p,i)=>({puuid:p.puuid,requested:i===0?5:3,matchIds:i===0?5:3,eligibleMatches:i===0?5:3,limited:false})),failures:[]},
 match:{riotMatchId:'LA1_FIXTURE',queueId:420,gameStartTimestamp:'2026-10-27T18:00:00Z',gameDurationSeconds:1800,teams:[{teamId:100,participants:participants.slice(0,5)},{teamId:200,participants:participants.slice(5)}],premadeGroups:[{groupNumber:1,teamId:100,classification:'PossiblePremade',label:'possible premade',members:participants.slice(0,2).map(p=>({puuid:p.puuid,gameName:p.gameName,tagLine:p.tagLine}))}],familiarity:{knownPlayers:2,unknownPlayers:7,evaluablePlayers:9,familiarityPercentage:22.2,status:'Available',knownPlayerDetails:[]}},
};
const svg='<svg xmlns="http://www.w3.org/2000/svg" width="96" height="96"><rect width="96" height="96" rx="12" fill="#122237"/><path d="M22 68L48 15 74 68 48 55Z" fill="#d8bd78"/><text x="48" y="88" text-anchor="middle" fill="white" font-size="12">DEMO</text></svg>';
async function open(width,scenario='complete'){
 const context=await browser.newContext({viewport:{width,height:1000},reducedMotion:'reduce'});
 const page=await context.newPage();let attempts=0;
 page.on('pageerror',error=>report.errors.push(error.message));
 await context.route('**/*',async route=>{
  const url=new URL(route.request().url());
  if(url.origin!==base){report.errors.push(`Solicitud externa: ${url.host}`);return route.abort();}
  if(url.pathname.startsWith('/_next/image')||url.pathname.startsWith('/api/assets/'))return route.fulfill({status:200,contentType:'image/svg+xml',body:svg});
  if(url.pathname.startsWith('/api/')){
   report.requests.push(url.pathname);
   if(url.pathname.endsWith('/analyze')){
    attempts++;
    if(scenario==='error' && attempts===1)return route.fulfill({status:503,contentType:'application/json',body:'{}'});
    const result=structuredClone(data);
    if(scenario==='long'){for(const t of result.match.teams) for(const p of t.participants) p.gameName+=' con nombre extenso de demostración';}
    if(scenario==='partial'){result.coverage.status='partial';result.coverage.failures=[{stage:'match_detail',code:'fixture_failure'}];result.coverage.players[1].limited=true;result.match.teams[1].participants=result.match.teams[1].participants.slice(0,3);}
    if(scenario==='empty'){result.relationships=[];result.match.premadeGroups=[];result.coverage.status='insufficient';result.coverage.matchesRecovered=0;}
    return route.fulfill({status:200,contentType:'application/json',body:JSON.stringify(result)});
   }
   report.errors.push(`API inesperada: ${url.pathname}`);return route.abort();
  }
  return route.continue();
 });
 await page.goto(`${base}/match/LA1_FIXTURE?ownerPuuid=fixture-player-0&platformRegion=la1&returnGameName=Ana&returnTagLine=TEST`);
 return {page,context};
}
async function shot(page,name,selector){await (selector?page.locator(selector):page).screenshot({path:`${out}/${name}.png`,...(!selector?{fullPage:true}:{})});}
try {
 for(const width of [320,390,768,1024,1440]){
  const {page,context}=await open(width);
  await page.getByRole('heading',{name:'Relaciones históricas'}).waitFor();
  const metrics=await page.evaluate(()=>({viewport:innerWidth,scroll:document.documentElement.scrollWidth}));
  assert.ok(metrics.scroll<=width,`Overflow ${width}: ${metrics.scroll}`);report.viewports.push({width,...metrics});
  for(const title of ['Resumen de partida','Equipos y participantes','Relaciones históricas','Grupos con coincidencias históricas','Cobertura y transparencia'])assert.equal(await page.getByRole('heading',{name:title}).count(),1);
  assert.equal(await page.getByRole('heading',{name:'Equipo azul'}).count(),1);assert.equal(await page.getByRole('heading',{name:'Equipo rojo'}).count(),1);
  assert.ok(!await page.locator('body').innerText().then(t=>t.includes('fixture-player-')));
  const details=page.getByText('Ver detalle de cobertura',{exact:true});
  await details.focus();
  const focus=await details.evaluate(e=>({focused:document.activeElement===e,outline:getComputedStyle(e).outlineStyle,width:getComputedStyle(e).outlineWidth,shadow:getComputedStyle(e).boxShadow}));
  assert.ok(focus.focused && ((focus.outline!=='none' && focus.width!=='0px') || focus.shadow!=='none'),`focus invisible at ${width}`);
  report.viewports.at(-1).focus=focus;
  await page.keyboard.press('Enter');
  assert.equal(await details.evaluate(e=>e.closest('details').open),true);
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
  if(width===390||width===1440){
   const axePath=createRequire(process.env.PLAYWRIGHT_PACKAGE || '/tmp/echo-visual-browser/package.json').resolve('axe-core/axe.min.js');
   await page.addScriptTag({content:await readFile(axePath,'utf8')});
   const result=await page.evaluate(async()=>{const r=await axe.run(document,{runOnly:{type:'tag',values:['wcag2a','wcag2aa','wcag21aa','wcag22aa']}});return {violations:r.violations.map(v=>({id:v.id,impact:v.impact,nodes:v.nodes.map(n=>n.target)})),incomplete:r.incomplete.map(v=>v.id)};});
   report.accessibility.push({width,...result});
  }
  await details.evaluate(e=>e.blur());
  await page.evaluate(()=>window.scrollTo(0,0));
  if(width===390)await shot(page,'02-mobile');
  if(width===1440){
   await shot(page,'01-desktop');
   await shot(page,'03-resumen','[aria-labelledby="match-summary-title"]');
   await shot(page,'04-equipos','[aria-labelledby="match-teams-title"]');
   await shot(page,'05-relaciones','[aria-labelledby="match-relationships-title"]');
   await shot(page,'06-grupos','[aria-labelledby="match-groups-title"]');
   await shot(page,'07-cobertura','[aria-labelledby="match-coverage-title"]');
  }
  report.checks.push(`cinco secciones, cobertura por teclado y overflow ${width}`);await context.close();
 }
 for(const scenario of ['partial','empty','error','long']){
  const {page,context}=await open(scenario==='long'?320:390,scenario);
  if(scenario==='error'){await page.getByRole('button',{name:'Reintentar'}).click();}
  await page.getByRole('heading',{name:'Relaciones históricas'}).waitFor();
  if(scenario==='partial')assert.ok(await page.getByText(/información es incompleta/i).count());
  if(scenario==='empty')assert.ok(await page.getByText(/no hay información suficiente/i).count());
  assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`overflow in ${scenario}`);
  await shot(page,`08-${scenario}`);report.checks.push(scenario==='error'?'error y reintento':scenario);await context.close();
 }
 assert.deepEqual(report.errors,[]);
} finally {await writeFile(`${out}/results.json`,JSON.stringify(report,null,2));await browser.close();}
console.log(JSON.stringify(report));
