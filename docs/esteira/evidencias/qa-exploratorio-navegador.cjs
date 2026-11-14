// Roteiro exploratorio do QA (etapa 6). Uso, a partir da raiz do repositorio, com o compose no ar:
//   node docs/esteira/evidencias/qa-exploratorio-navegador.cjs
// Usa o Chrome do sistema via o Playwright de e2e/node_modules (nenhum navegador e baixado).
const { createRequire } = require('module');
const path = require('path');
const raiz = path.resolve(__dirname, '..', '..', '..');
const req = createRequire(path.join(raiz, 'e2e', 'package.json'));
const { chromium } = req('@playwright/test');
const out = __dirname;
const log = [];
const L = (...a) => { const s = a.join(' '); console.log(s); log.push(s); };
const clean = (s) => s.replace(/\s+/g, ' ');
(async () => {
  const browser = await chromium.launch({ channel: 'chrome' });
  const ctx = await browser.newContext({ locale: 'pt-BR', viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage();
  const consoleMsgs = [];
  page.on('console', m => { if (['error', 'warning'].includes(m.type())) consoleMsgs.push(m.type() + ': ' + m.text()); });
  page.on('pageerror', e => consoleMsgs.push('pageerror: ' + e.message));

  // 1. Listagem padrao
  await page.goto('http://localhost:4200/pedidos');
  await page.getByRole('table').waitFor();
  await page.screenshot({ path: out + '/qa-01-listagem.png', fullPage: true });
  L('LISTAGEM linhas (incl. cabecalho):', await page.getByRole('table').getByRole('row').count());
  L('PRIMEIRA LINHA:', clean(await page.getByRole('table').getByRole('row').nth(1).innerText()));
  L('HEADERS:', (await page.locator('th').allInnerTexts()).join(' | '));
  const bodyText = await page.locator('body').innerText();
  L('TEXTO PAGINACAO:', JSON.stringify(bodyText.split('\n').filter(l => /p[áa]gina|Próxima|Anterior|total/i.test(l))));

  // 2. Paginacao
  const prox = page.getByRole('button', { name: /Próxima/i }).or(page.getByRole('link', { name: /Próxima/i }));
  if (await prox.count()) {
    await prox.first().click();
    await page.waitForLoadState('networkidle');
    L('URL APOS PROXIMA:', page.url());
    await page.screenshot({ path: out + '/qa-02-paginacao-pagina2.png', fullPage: true });
  } else L('SEM BOTAO PROXIMA');

  // 3. Filtros
  await page.goto('http://localhost:4200/pedidos');
  await page.getByRole('table').waitFor();
  L('OPCOES CLIENTE:', JSON.stringify((await page.locator('#filtro-cliente option').allInnerTexts())));
  await page.locator('#filtro-cliente').selectOption({ index: 1 });
  await page.locator('#filtro-data-inicio').fill('2026-01-01');
  await page.locator('#filtro-data-fim').fill('2026-12-31');
  await page.locator('button[type="submit"]').click();
  await page.waitForLoadState('networkidle');
  L('URL FILTRADA:', page.url());
  await page.screenshot({ path: out + '/qa-03-filtros.png', fullPage: true });
  await page.reload();
  await page.waitForLoadState('networkidle');
  L('APOS RELOAD cliente:', await page.locator('#filtro-cliente').inputValue(), 'inicio:', await page.locator('#filtro-data-inicio').inputValue(), 'fim:', await page.locator('#filtro-data-fim').inputValue());

  // 4. Vazio
  await page.goto('http://localhost:4200/pedidos?dataInicio=2000-01-01&dataFim=2000-01-31');
  await page.waitForLoadState('networkidle');
  L('VAZIO msg visivel:', await page.getByText('Nenhum pedido encontrado').isVisible());
  await page.screenshot({ path: out + '/qa-04-vazio.png', fullPage: true });

  // 5. Carregando
  await page.route('**/api/pedidos?*', async route => { await new Promise(r => setTimeout(r, 2500)); await route.continue(); });
  await page.goto('http://localhost:4200/pedidos');
  await page.getByText(/Carregando pedidos/).waitFor({ timeout: 3000 });
  await page.screenshot({ path: out + '/qa-05-carregando.png' });
  L('CARREGANDO visivel: true');
  await page.waitForLoadState('networkidle');
  await page.unroute('**/api/pedidos?*');

  // 6. Erro (500 simulado) e tentar novamente
  let falhar = true;
  await page.route('**/api/pedidos?*', async route => {
    if (falhar) await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{"code":"ERRO_INTERNO","status":500}' });
    else await route.continue();
  });
  await page.goto('http://localhost:4200/pedidos');
  await page.locator('[role="alert"]:visible').first().waitFor();
  L('ERRO alerta:', clean(await page.locator('[role="alert"]:visible').first().innerText()));
  await page.screenshot({ path: out + '/qa-06-erro.png' });
  falhar = false;
  await page.getByRole('button', { name: /Tentar novamente/i }).click();
  await page.getByRole('table').waitFor();
  L('APOS TENTAR NOVAMENTE tabela visivel: true');
  await page.unroute('**/api/pedidos?*');

  // 7. Criar - 409 real: produto com estoque 0
  await page.goto('http://localhost:4200/pedidos/novo');
  await page.getByLabel('Cliente', { exact: true }).selectOption({ index: 1 });
  await page.getByRole('button', { name: 'Adicionar item' }).click();
  const prodOpts = (await page.getByLabel('Produto (item 1)').locator('option').allInnerTexts()).map(clean);
  L('PRODUTOS:', JSON.stringify(prodOpts));
  const idxZero = prodOpts.findIndex(t => /estoque 0\)/.test(t));
  L('INDICE produto estoque 0:', idxZero);
  if (idxZero > 0) {
    await page.getByLabel('Produto (item 1)').selectOption({ index: idxZero });
    await page.getByLabel('Quantidade (item 1)').fill('1');
    await page.screenshot({ path: out + '/qa-07a-form-antes-409.png', fullPage: true });
    const btn = page.getByRole('button', { name: 'Confirmar pedido' });
    L('BOTAO confirmar habilitado (estoque 0, qtd 1):', await btn.isEnabled());
    if (await btn.isEnabled()) {
      await btn.click();
      await page.locator('[role="alert"]:visible').first().waitFor({ timeout: 5000 }).catch(() => L('sem alerta 409'));
      L('409 TELA:', clean(await page.locator('main').innerText()).slice(0, 600));
      await page.screenshot({ path: out + '/qa-07b-form-409.png', fullPage: true });
      L('URL pos-409:', page.url());
      L('qtd preservada:', await page.getByLabel('Quantidade (item 1)').inputValue());
    }
  }

  // 8. 422 simulado
  await page.goto('http://localhost:4200/pedidos/novo');
  await page.getByLabel('Cliente', { exact: true }).selectOption({ index: 1 });
  await page.getByRole('button', { name: 'Adicionar item' }).click();
  const o2 = (await page.getByLabel('Produto (item 1)').locator('option').allInnerTexts()).map(clean);
  const idxOk = o2.findIndex(t => /estoque ([1-9]\d*)\)/.test(t));
  await page.getByLabel('Produto (item 1)').selectOption({ index: idxOk });
  await page.getByLabel('Quantidade (item 1)').fill('1');
  await page.route('**/api/pedidos', async route => {
    if (route.request().method() === 'POST') await route.fulfill({ status: 422, contentType: 'application/problem+json', body: JSON.stringify({ type: '/erros/cliente-inativo', title: 'Cliente inativo', status: 422, detail: 'O cliente 11 esta inativo.', code: 'CLIENTE_INATIVO' }) });
    else await route.continue();
  });
  await page.getByRole('button', { name: 'Confirmar pedido' }).click();
  await page.locator('[role="alert"]:visible').first().waitFor();
  L('422 TELA:', clean(await page.locator('main').innerText()).slice(0, 500));
  await page.screenshot({ path: out + '/qa-08-form-422-simulado.png', fullPage: true });
  await page.unroute('**/api/pedidos');

  // 9. CA-31
  await page.goto('http://localhost:4200/pedidos/novo');
  const b = page.getByRole('button', { name: 'Confirmar pedido' });
  L('CA-31 botao desabilitado sem cliente/itens:', !(await b.isEnabled()));
  await page.getByLabel('Cliente', { exact: true }).selectOption({ index: 1 });
  await page.getByRole('button', { name: 'Adicionar item' }).click();
  await page.getByLabel('Quantidade (item 1)').fill('0');
  await page.getByLabel('Quantidade (item 1)').blur();
  L('CA-31 qtd 0 botao desabilitado:', !(await b.isEnabled()));
  await page.screenshot({ path: out + '/qa-09-form-invalido.png', fullPage: true });

  // 10. Teclado
  await page.goto('http://localhost:4200/pedidos');
  await page.getByRole('table').waitFor();
  const trilha = [];
  for (let i = 0; i < 10; i++) {
    await page.keyboard.press('Tab');
    trilha.push(await page.evaluate(() => {
      const e = document.activeElement; const cs = getComputedStyle(e);
      return e.tagName + '#' + (e.id || '') + ' "' + (e.innerText || e.getAttribute('aria-label') || '').slice(0, 25).replace(/\n/g, ' ') + '" outline=' + cs.outlineStyle + '/' + cs.outlineWidth + ' boxShadow=' + (cs.boxShadow === 'none' ? 'none' : 'sim');
    }));
  }
  L('TAB LISTAGEM:\n  ' + trilha.join('\n  '));
  await page.goto('http://localhost:4200/pedidos/novo');
  await page.getByLabel('Cliente', { exact: true }).waitFor();
  await page.getByLabel('Cliente', { exact: true }).focus();
  await page.keyboard.press('ArrowDown');
  await page.keyboard.press('Tab');
  L('FOCO apos Tab do select cliente:', await page.evaluate(() => document.activeElement.tagName + ' ' + (document.activeElement.innerText || '').slice(0, 30)));
  await page.keyboard.press('Enter');
  L('FOCO apos Enter em Adicionar item:', await page.evaluate(() => document.activeElement.tagName + ' ' + (document.activeElement.getAttribute('aria-label') || document.activeElement.id)));
  await page.screenshot({ path: out + '/qa-10-teclado-form.png', fullPage: true });

  // pt-BR
  await page.goto('http://localhost:4200/pedidos');
  await page.getByRole('table').waitFor();
  L('FORMATO pt-BR 1a linha:', clean(await page.getByRole('table').getByRole('row').nth(1).innerText()));
  L('html lang:', await page.evaluate(() => document.documentElement.lang));

  L('CONSOLE erros/avisos:', JSON.stringify(consoleMsgs));
  require('fs').writeFileSync(out + '/qa-exploratorio-navegador.log', log.join('\n'));
  await browser.close();
})().catch(e => { console.error('FALHA', e); process.exit(1); });
