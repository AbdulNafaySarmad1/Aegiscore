import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { mkdir, writeFile, chmod } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import path from 'node:path';

const root=path.resolve(__dirname,'../../..');
const baseURL='http://127.0.0.1:3000';
const required=['/login','/dashboard','/students','/grades','/attendance','/finance','/timetable','/admissions','/reports','/admin/settings'];
const aliases:Record<string,string>={'/grades':'/examinations','/timetable':'/classes'};
const roles={admin:'Vice Chancellor',faculty:'Faculty',student:'Student'};
async function mock(context:BrowserContext){
 await context.route('**/api/**',async route=>{
   const url=new URL(route.request().url());
   const response=await context.request.fetch(`http://127.0.0.1:4319${url.pathname}${url.search}`,{method:route.request().method(),data:route.request().postData()??undefined});
   await route.fulfill({response});
 });
 await context.addInitScript(()=>sessionStorage.setItem('aegiscore_setup_token','synthetic-demo-only'));
}
async function settle(page:Page){await page.evaluate(()=>document.fonts.ready);await page.waitForTimeout(1200);}
async function login(page:Page,label:string){await page.goto('/login');await page.getByRole('button',{name:new RegExp(label)}).click();await expect(page).toHaveURL(/\/$/,{timeout:180000});await expect(page.locator('h1')).toBeVisible({timeout:180000});}

test('synthetic role screenshots and BSCS applicant-to-alumni MP4',async({browser})=>{
 test.setTimeout(30*60_000);
 // Synthetic fixture output is quarantined under _synthetic/ so it never overwrites the
 // real Task 18A/18B deliverables (screenshots/INDEX.md, recordings/aegiscore-demo.webm),
 // which are produced only by screenshots.spec.ts / demo.spec.ts against a live install.
 const screenshots=path.join(root,'screenshots','_synthetic');const recordings=path.join(root,'recordings','_synthetic');
 await mkdir(screenshots,{recursive:true});await mkdir(recordings,{recursive:true});await mkdir(path.join(root,'AegisCore.Portal/.auth'),{recursive:true,mode:0o700});
 const rows:string[]=[];
 const index=()=>writeFile(path.join(screenshots,'INDEX.md'),`# AegisCore synthetic demo screenshots\n\nSynthetic-only; NOT the Task 18A deliverable. The real role-authenticated capture is \`screenshots/INDEX.md\` (produced by screenshots.spec.ts against a live install).\n\n1920×1080 viewport. Fictional data; HTTP statuses are local document/fixture responses, not production authorization evidence. Admin uses the built-in Vice Chancellor demo persona. Unsupported module fixtures are captured as ERROR rather than disguised as successful data.\n\n| Role | Page | File | HTTP status | Notes |\n|---|---|---|---|---|\n${rows.join('\n')}\n`);
 async function capture(page:Page,role:string,url:string,name?:string,action?:()=>Promise<void>){
  const failures:string[]=[];let status='N/A';let error='';
  const response=(r:import('@playwright/test').Response)=>{if(r.url().includes('/api/')&&r.status()>=400)failures.push(`${new URL(r.url()).pathname}: ${r.status()}`);};
  const pageError=(e:Error)=>{error=e.message;};page.on('response',response);page.on('pageerror',pageError);
  try{if(action){await action();status='200 (fixture step)';}else{const r=await page.goto(url,{timeout:180000});status=String(r?.status()??'N/A');if(r&&r.status()>=400)error=`HTTP ${r.status()}`;}
   if(url==='/grades'||url==='/attendance')await page.getByRole('combobox',{name:'Course',exact:true}).selectOption('course-1');
   if(url==='/reports')await page.getByRole('button',{name:'Generate report'}).click();
   await settle(page);
   if(new URL(page.url()).pathname!==(aliases[url]??url))error||='Unexpected redirect';
   const alerts=page.locator('[role="alert"],.error-display,.wizard-error').filter({visible:true});
   if(await alerts.evaluateAll(nodes=>nodes.some(node=>!node.closest('next-route-announcer')&&node.id!=='__next-route-announcer__'&&Boolean(node.textContent?.trim()))))error||='Visible page error';
  }catch(e){error=String(e).split('\n')[0];}finally{page.off('response',response);page.off('pageerror',pageError);}
  if(failures.length)error||='Unsupported fixture';
  const file=`${role}/${name??url.slice(1).replaceAll('/','_')??'home'}${error?'_ERROR':''}.png`;
  await page.screenshot({path:path.join(screenshots,file),fullPage:true,animations:'disabled',timeout:30000});
  rows.push(`| ${role} | ${name??url} | [${file}](${file}) | ${[status,...failures].join('; ')} | ${error.replaceAll('|','/')} |`);await index();
 }
 // Capture the original requested routes for all three roles, plus all seven setup steps.
 for(const [role,label]of Object.entries(roles)){
  await mkdir(path.join(screenshots,role),{recursive:true});
  const auth=await browser.newContext({baseURL,viewport:{width:1920,height:1080}});await mock(auth);const page=await auth.newPage();
  await capture(page,role,'/login');await login(page,label);
  const state=path.join(root,`AegisCore.Portal/.auth/visual-${role}.json`);await auth.storageState({path:state});await chmod(state,0o600);await auth.close();
  const context=await browser.newContext({baseURL,storageState:state,viewport:{width:1920,height:1080}});await mock(context);const current=await context.newPage();
  for(const url of required.filter(p=>p!=='/login'))await capture(current,role,url);
  for(let step=1;step<=7;step++)await capture(current,role,'/setup',`setup_step_${step}`,async()=>{
   if(step===1)await current.goto('/setup');else await current.getByRole('button',{name:/Save and continue/}).click();
   await expect(current.getByText(`STEP ${step} OF 7`,{exact:true})).toBeVisible();
  });
  await context.close();
 }
 // Precompile the demo before starting the uninterrupted recording.
 const warm=await browser.newContext({baseURL});await mock(warm);const warmPage=await warm.newPage();await warmPage.goto('/demo/student-journey',{timeout:180000});await expect(warmPage.getByRole('heading',{name:'Big ideas start here.'})).toBeVisible();await warm.close();
 const context=await browser.newContext({baseURL,viewport:{width:1920,height:1080},recordVideo:{dir:path.join(recordings,'.raw'),size:{width:1920,height:1080}}});await mock(context);
 const page=await context.newPage();const video=page.video()!;let complete=false;
 const pause=()=>page.waitForTimeout(6000);
 const shot=async(name:string)=>{await settle(page);const file=`student/lifecycle_${name}.png`;await page.screenshot({path:path.join(screenshots,file),fullPage:true,animations:'disabled'});rows.push(`| student | Lifecycle: ${name} | [${file}](${file}) | 200 (demo page) | Simulated student lifecycle |`);await index();};
 try{
  await page.goto('/demo/student-journey');await shot('01_signup');await page.waitForTimeout(1800);
  await page.getByLabel('Full name',{exact:true}).pressSequentially('Ali Hassan',{delay:95});
  await page.getByLabel('Email address').pressSequentially('ali.hassan@example.invalid',{delay:65});
  await page.getByLabel('Password',{exact:true}).fill('fictional-demo-password');await page.waitForTimeout(1200);
  await page.getByRole('button',{name:'Create account & apply'}).click();await expect(page.getByRole('heading',{name:'Your potential, recognized.'})).toBeVisible();await shot('02_entry_test_results');await pause();
  await page.getByRole('button',{name:'View admission offer'}).click();await shot('03_admission');await pause();await page.getByLabel('I accept my admission offer.').check();await page.waitForTimeout(1500);
  await page.getByRole('button',{name:'Continue to enrollment'}).click();await pause();await page.getByRole('button',{name:'Pay with demo wallet'}).click();await page.waitForTimeout(2500);await page.getByRole('button',{name:'Confirm course registration'}).click();await shot('04_enrollment');await pause();
  await page.getByRole('button',{name:'Open academic progress'}).click();await shot('05_academics');await pause();
  await page.getByRole('button',{name:'Continue to final year'}).click();await shot('06_final_year');await pause();
  await page.getByRole('button',{name:'Review graduation clearance'}).click();await shot('07_graduation');await pause();
  await page.getByRole('button',{name:'Activate alumni profile'}).click();await shot('08_alumni');await pause();
  await page.getByRole('button',{name:'Join the alumni mentoring network'}).click();await expect(page.getByRole('status')).toHaveText('You are ready to guide the next generation of BSCS students.');await pause();complete=true;
 }finally{await context.close();await video.saveAs(path.join(recordings,complete?'aegiscore-student-lifecycle.webm':'aegiscore-student-lifecycle_FAILED.webm'));}
 expect(complete).toBe(true);
 const mp4=path.join(recordings,'aegiscore-demo-synthetic.mp4');
 execFileSync('ffmpeg',['-y','-i',path.join(recordings,'aegiscore-student-lifecycle.webm'),'-c:v','libx264','-preset','fast','-crf','20','-pix_fmt','yuv420p','-movflags','+faststart','-an',mp4],{stdio:'pipe',timeout:180000});
 const metadata=JSON.parse(execFileSync('ffprobe',['-v','error','-show_streams','-show_format','-of','json',mp4],{encoding:'utf8'}));
 const stream=metadata.streams.find((s:{codec_type:string})=>s.codec_type==='video');
 expect(stream.width).toBe(1920);expect(stream.height).toBe(1080);expect(stream.codec_name).toBe('h264');expect(Number(metadata.format.duration)).toBeGreaterThan(30);expect(Number(metadata.format.duration)).toBeLessThan(240);
 await writeFile(path.join(recordings,'run.json'),JSON.stringify({complete:true,synthetic:true,story:'BSCS signup → entry-test result → admission → enrollment and demo payment → academics → final year → graduation → alumni',durationSeconds:Number(metadata.format.duration),width:stream.width,height:stream.height,codec:stream.codec_name,screenshots:rows.length},null,2));
 await writeFile(path.join(recordings,'README.md'),'# AegisCore synthetic student lifecycle demo\n\nThis directory is synthetic-only and is NOT the Task 18B deliverable. The real admin\ndemo (`recordings/aegiscore-demo.webm`) is produced by `demo.spec.ts` against a live\ninstall.\n\n`aegiscore-demo-synthetic.mp4` is a raw 1920×1080 H.264 screen recording with no added\ntitles, overlays, music or cuts. All records are fictional. The applicant-to-alumni\njourney is a gated demo-only UI, not evidence that production signup, result\npublication, payment, graduation or alumni workflows executed. Four academic years are\nsimulated. The screenshots here use synthetic fixtures, not production role authorization.\n');
 expect(rows).toHaveLength(59);
});
