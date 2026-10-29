// Opt-in browser validation. Playwright is supplied externally; no project dependency.
import { createRequire } from 'node:module';
import { mkdir, writeFile, readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
const { chromium } = createRequire(process.env.PLAYWRIGHT_PACKAGE || '/tmp/echo-visual-browser/package.json')('playwright');
const out = process.env.CAPTURE_DIR || 'docs/releases/release04-captures';
await mkdir(out, { recursive: true });
const browser = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH, headless: true });
const report = { synthetic: true, checks: [], viewports: [], errors: [], requests: [] };
const fixture = (start = 0, count = 5, status = 'complete', icon = true) => {
  const items = Array.from({length: count}, (_, i) => {
    const n = start+i;
    return { riotMatchId: `LA1_${1000+n}`, queueId:420, gameStartTimestamp: new Date(Date.UTC(2026,9,27,18-n)).toISOString(), gameDurationSeconds:1800+n*45, championId:[103,22,99][n%3], championName:['Ahri','Ashe','Lux'][n%3], kills: n+3, deaths:n%4, assists:n+7, win:n%2===0, evidence:{ ownerPuuid:'synthetic-owner', platformRegion:'la1', status:'complete', participants:[{puuid:'synthetic-owner',gameName:'Echo Demo',tagLine:'TEST',teamId:100}, ...Array.from({length:9},(_,p)=>({puuid:`synthetic-${p}`, gameName:`Jugador de demostración ${p+1}`, tagLine:'DEMO', teamId: (p+n)%2===0?100:200}))] } };
  });
  return { player:{puuid:'synthetic-owner',gameName:'Echo Demo',tagLine:'TEST',platformRegion:'la1',profileIconId:icon?29:null,profileIconObservedAt:'2026-10-27T18:00:00Z'}, summary:{puuid:'synthetic-owner',gameName:'Echo Demo',tagLine:'TEST',matchesAnalyzed:count,wins:items.filter(m=>m.win).length,losses:items.filter(m=>!m.win).length,winRate:60,uniquePlayersEncountered:9,repeatedPlayers:9, dataUpdatedAt:'2026-10-27T18:00:00Z'}, matches:{page:1,pageSize:5,totalCount:count,items}, encounters:[],coverage:{status,participants:10,matchIdsObtained:status==='insufficient'?count:5,uniqueMatchIds:count,matchesRecovered:count,players:[{puuid:'synthetic-owner',requested:5,matchIds:status==='insufficient'?count:5,eligibleMatches:count,limited:status!=='complete'}],failures:status==='partial'?[{stage:'match_detail',code:'fixture_unavailable'}]:[]} };
};
const artwork = '<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64"><rect width="64" height="64" rx="8" fill="#122237"/><path d="M18 45L32 12 46 45 32 38Z" fill="#d8bd78"/><text x="32" y="59" text-anchor="middle" fill="white" font-size="8">DEMO</text></svg>';
async function open({width=1440,status='complete',count=5,active=false,activeError=false,icon=true,failedWindow=false,initialError=false,slow=false}={}) {
  const context=await browser.newContext({viewport:{width,height:1000}, reducedMotion:'reduce'});
  const page=await context.newPage(); let attempts=0;
  page.on('pageerror',e=>report.errors.push(e.message));
  await context.route('**/*', async route=>{
    const url=new URL(route.request().url());
    if(url.origin!==(process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3187')) { report.errors.push(`Unexpected external request: ${url.host}`); return route.abort(); }
    if(url.pathname.startsWith('/api/')) {
      report.requests.push(url.pathname+url.search);
      if(url.pathname.includes('/assets/')) return route.fulfill({status:icon?200:404,contentType:'image/svg+xml',body:icon?artwork:''});
      if(url.pathname.endsWith('/analysis')) {
        if(slow) await new Promise(r=>setTimeout(r,700));
        return route.fulfill({status:initialError?503:200,contentType:'application/json',body:JSON.stringify(initialError?{message:'Unavailable'}:fixture(0,count,status,icon))});
      }
      if(url.pathname.includes('active-game')) return route.fulfill({status:activeError?503:200,contentType:'application/json',body:JSON.stringify(active?{gameId:7,queueId:420,gameStartTime:'2026-10-28T08:00:00Z',gameLengthSeconds:900,participants:[]}:null)});
      if(url.pathname.includes('recent-matches')) {
        attempts++;
        return route.fulfill({status:failedWindow&&attempts===1?503:200,contentType:'application/json',body:JSON.stringify(fixture(Number(url.searchParams.get('start')),5))});
      }
      report.errors.push(`Unexpected API route: ${url.pathname}`);return route.abort();
    }
    return route.continue();
  });
  await page.goto(`${process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3187'}/player/Echo%20Demo/TEST?platformRegion=la1`);
  return {page,context};
}
const shot=async(page,name,locator)=>{await (locator||page).screenshot({path:`${out}/${name}.png`,...(!locator?{fullPage:true}:{})});};
try {
  if (!process.env.VISUAL_SUPPLEMENT_ONLY) {
  for (const width of [320,390,768,1024,1440]) {
    const {page,context}=await open({width});
    await page.getByRole('img',{name:/Win rate:/}).waitFor();
    await page.evaluate(()=>document.fonts.ready);
    const dimensions=await page.evaluate(()=>({viewport:innerWidth,scroll:document.documentElement.scrollWidth,columns:getComputedStyle(document.querySelector('.competitive-header')).gridTemplateColumns}));
    assert.ok(dimensions.scroll<=width,`horizontal overflow at ${width}: ${dimensions.scroll}`);
    report.viewports.push({width,...dimensions});
    if(width===1440) {
      const cols=dimensions.columns.split(' ').map(parseFloat); assert.ok(Math.abs(cols[0]/cols[1]-1.5)<0.02);
      await shot(page,'01-desktop'); await shot(page,'02-header',page.locator('.competitive-header'));
      await shot(page,'03-performance',page.locator('section[aria-labelledby="performance-title"]'));
      await page.getByRole('button',{name:'Ver más',exact:true}).click();
      await page.getByText('Partidas compartidas (5)',{exact:true}).first().click();
      assert.equal(await page.locator('.competitive-recurrent > li').count(),9);
      await shot(page,'04-relationships-expanded',page.locator('section[aria-labelledby="relationships-title"]'));
      await page.getByRole('button',{name:'Derrotas',exact:true}).click(); assert.equal(await page.locator('.competitive-match').count(),2);
      assert.match(await page.getByRole('img',{name:/Win rate:/}).getAttribute('aria-label'),/60/);
      await page.getByRole('button',{name:'Todas',exact:true}).click();
      await page.locator('.competitive-match summary').first().click();
      await shot(page,'05-history',page.locator('.player-match-history'));
      const link=page.locator('.competitive-recurrent a').first();assert.match(await link.getAttribute('href'),/^\/player\//);
      await page.getByRole('button',{name:'Cargar cinco partidas más'}).click();
      await page.getByText('10 partidas cargadas',{exact:true}).first().waitFor();
      assert.match(await page.getByRole('img',{name:/Win rate:/}).getAttribute('aria-label'),/50/);
      await shot(page,'09-ten-matches');
      await page.goto(`${process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3187'}/player/Echo%20Demo/TEST?platformRegion=la1`);
      await page.keyboard.press('Tab'); assert.match(await page.locator(':focus').innerText(),/contenido/i);
      await page.keyboard.press('Enter');
      report.checks.push('desktop: 60/40, expanded recurrent/shared links, filters preserve summary, details, 5→10 sample, keyboard skip link');
    }
    if(width===390) await shot(page,'07-mobile');
    await context.close();
  }
  for(const scenario of [
    {name:'06-partial',status:'partial',count:3},
    {name:'08-active-game',active:true},
    {name:'10-empty',status:'insufficient',count:0,icon:false},
    {name:'11-error',initialError:true},
    {name:'12-active-error',activeError:true},
    {name:'13-window-error',failedWindow:true},
    {name:'14-coverage-error',status:'error',count:0},
    {name:'15-loading',slow:true},
  ]) {
    const {page,context}=await open(scenario);
    if(scenario.slow) await shot(page,scenario.name); else if(scenario.initialError) {await page.getByRole('button',{name:'Reintentar',exact:true}).waitFor(); await shot(page,scenario.name);}
    else {
      await page.getByRole('img',{name:/Win rate:/}).waitFor();
      if(scenario.active) await page.getByRole('link',{name:/Ver partida en curso/}).waitFor();
      if(scenario.activeError) await page.getByRole('button',{name:'Volver a comprobar'}).waitFor();
      if(scenario.status) {await page.getByText('Consultar detalle de cobertura',{exact:true}).click();}
      if(scenario.failedWindow) {
        await page.getByRole('button',{name:'Cargar cinco partidas más'}).click();
        await page.getByText(/La última ventana falló/).waitFor();
        await shot(page,scenario.name);
        await page.getByRole('button',{name:'Reintentar este bloque de cinco'}).click();
        await page.getByText('10 partidas cargadas',{exact:true}).first().waitFor();
      } else await shot(page,scenario.name);
      const size=await page.evaluate(()=>[document.documentElement.scrollWidth,innerWidth]);assert.ok(size[0]<=size[1]);
    }
    report.checks.push(scenario.name);await context.close();
  }
  }
  if (process.env.VISUAL_SUPPLEMENT_ONLY) {
    report.accessibility = [];
    for (const width of [320,390,768,1024,1440]) {
      const {page,context}=await open({width});
      await page.getByRole('img',{name:/Win rate:/}).waitFor();
      await page.getByRole('button',{name:'Ver más',exact:true}).click();
      const summary=page.locator('.competitive-recurrent summary').first();
      await summary.focus(); await page.keyboard.press('Enter');
      assert.equal(await summary.evaluate(e=>e.parentElement.open),true);
      const focus=await summary.evaluate(e=>({style:getComputedStyle(e).outlineStyle,width:getComputedStyle(e).outlineWidth}));
      assert.notEqual(focus.style,'none');
      await page.locator('.competitive-match summary').first().click();
      await page.getByText('Consultar detalle de cobertura',{exact:true}).click();
      await page.evaluate(()=>{
        const heading=document.querySelector('h1');heading.textContent+=' Identidad de demostración con un nombre considerablemente más largo';
        document.querySelectorAll('.ui-stat-card p, .ui-stat-card dt').forEach(e=>e.textContent+=' Texto ampliado para futura traducción');
      });
      assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),`expanded long text overflow ${width}`);
      const axePath=createRequire(process.env.PLAYWRIGHT_PACKAGE || '/tmp/echo-visual-browser/package.json').resolve('axe-core/axe.min.js');
      await page.addScriptTag({content:await readFile(axePath,'utf8')});
      const accessibility=await page.evaluate(async()=>{
        const results=await axe.run(document,{runOnly:{type:'tag',values:['wcag2a','wcag2aa','wcag21aa','wcag22aa']}});
        return {violations:results.violations.map(v=>({id:v.id,impact:v.impact,help:v.help,nodes:v.nodes.map(n=>({target:n.target,summary:n.failureSummary}))})),incomplete:results.incomplete.map(v=>v.id),passes:results.passes.length};
      });
      report.accessibility.push({width,focus,...accessibility});
      if(width===390) await shot(page,'16-mobile-expanded-long-text');
      if(width===1440) {
        await page.getByRole('button',{name:'Actualizar cinco partidas'}).click();
        await page.getByRole('img',{name:/Win rate:/}).waitFor();
        await page.locator('.competitive-recurrent a').first().click();
        await page.waitForURL(/\/player\/Jugador/);
        await page.getByRole('img',{name:/Win rate:/}).waitFor();
        report.checks.push('refresh and recurrent profile navigation');
        await page.goto(`${process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3187'}/`);
        await page.getByRole('button',{name:'Buscar jugador'}).waitFor();
        const response=await page.goto(`${process.env.VISUAL_BASE_URL || 'http://127.0.0.1:3187'}/design-system`);
        assert.equal(response.status(),404);
        report.checks.push('Home search renders and production gallery remains hidden');
      }
      report.checks.push(`expanded keyboard, focus, long text, axe: ${width}`);
      await context.close();
    }
  }
  assert.equal(report.errors.length,0,JSON.stringify(report.errors));
} finally {
  await writeFile(`${out}/${process.env.VISUAL_SUPPLEMENT_ONLY ? "accessibility" : "results"}.json`,JSON.stringify(report,null,2));await browser.close();
}
console.log(JSON.stringify({checks:report.checks,viewports:report.viewports,errors:report.errors},null,2));
