import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const mode = process.argv[2];
if (!['screenshots', 'demo', 'visual-demo', 'lifecycle', 'install', 'form-integration-prepare', 'form-integration', 'course-evaluation-prepare', 'course-evaluation'].includes(mode)) {
  throw new Error('Specify screenshots, demo, visual-demo, lifecycle, install, form-integration-prepare, form-integration, course-evaluation-prepare or course-evaluation');
}
const result = spawnSync(process.execPath, ['node_modules/@playwright/test/cli.js', 'test', ...process.argv.slice(3)], {
  stdio: 'inherit',
  cwd: fileURLToPath(new URL('../../', import.meta.url)),
  env: { ...process.env, CAPTURE_MODE: mode },
});
if (result.error) throw result.error;
process.exit(result.status ?? 1);
