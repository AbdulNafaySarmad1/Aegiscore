// Supervise both runtime processes; terminate the container when either exits.
const { spawn } = require('node:child_process');
const { existsSync } = require('node:fs');
const initiallyInstalled = existsSync('/app/config/installation.json');
const children = [
  spawn('dotnet', ['/app/AegisCore.API.dll'], { cwd: '/app', stdio: 'inherit', env: { ...process.env, ASPNETCORE_URLS: 'http://+:8081' } }),
  spawn('node', ['server.js'], { cwd: '/app/portal', stdio: 'inherit', env: { ...process.env, PORT: '8080', HOSTNAME: '0.0.0.0', AEGISCORE_API_INTERNAL_URL: 'http://127.0.0.1:8081' } }),
];
let stopping = false;
function stop(code) {
  if (stopping) return;
  stopping = true;
  for (const child of children) child.kill('SIGTERM');
  setTimeout(() => { for (const child of children) child.kill('SIGKILL'); process.exit(code); }, 25000).unref();
  Promise.all(children.map(child => child.exitCode !== null ? Promise.resolve() : new Promise(resolve => child.once('exit', resolve)))).then(() => process.exit(code));
}
for (const child of children) { child.on('error', () => stop(1)); child.on('exit', code => stop(code || 1)); }
process.on('SIGTERM', () => stop(0));
process.on('SIGINT', () => stop(0));
// Activation changes API startup routing. Restart after the success response is sent.
if (!initiallyInstalled) setInterval(() => {
  if (existsSync('/app/config/installation.json')) setTimeout(() => stop(0), 3000);
}, 2000).unref();
