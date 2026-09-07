import { spawn } from 'node:child_process';
const env={...process.env,NEXT_PUBLIC_DEMO_MODE:'true',AEGISCORE_API_INTERNAL_URL:'http://127.0.0.1:4319',APP_URL:'http://127.0.0.1:3000'};
const children=[spawn(process.execPath,['tests/capture/fixture-server.mjs'],{stdio:'inherit',env}),spawn(process.execPath,['node_modules/next/dist/bin/next','dev','--hostname','127.0.0.1'],{stdio:'inherit',env})];
let stopping=false;
function stop(code=0){if(stopping)return;stopping=true;for(const child of children)child.kill('SIGTERM');setTimeout(()=>process.exit(code),1500);}
for(const child of children)child.on('exit',code=>stop(code??1));
for(const signal of ['SIGINT','SIGTERM'])process.on(signal,()=>stop());
