#!/usr/bin/env node
// Lançador do servidor MCP do SQL Server (DBHub).
//
// Por que existe: o .mcp.json precisa funcionar igual em Windows, macOS e Linux.
// No Windows, `npx` é um script .cmd e não pode ser iniciado diretamente como
// processo; em vez de exigir `cmd /c` (que quebraria nos outros sistemas), o
// .mcp.json chama `node` — presente em todos — e este arquivo resolve a diferença.
// Também fixa a versão do DBHub e localiza o dbhub.toml pelo caminho deste arquivo,
// sem depender do diretório de trabalho.

import { spawn } from 'node:child_process';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const DBHUB_VERSION = '1.6.1';
const configPath = join(dirname(fileURLToPath(import.meta.url)), 'dbhub.toml');
const isWindows = process.platform === 'win32';

const child = spawn(
  isWindows ? 'npx.cmd' : 'npx',
  ['-y', `@bytebase/dbhub@${DBHUB_VERSION}`, '--transport', 'stdio', '--config', `"${configPath}"`],
  { stdio: 'inherit', shell: true, windowsHide: true },
);

child.on('error', (error) => {
  console.error(`[mcp:mssql] não foi possível iniciar o DBHub: ${error.message}`);
  process.exit(1);
});

child.on('exit', (code, signal) => {
  process.exit(signal ? 1 : (code ?? 1));
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => child.kill(signal));
}
