// Reteste da rodada 2 (QA-06 e QA-07) contra o compose no ar. Uso: node docs/esteira/evidencias/qa-reteste-rodada2.cjs
const { createRequire } = require('module');
const path = require('path');
const raiz = path.resolve(__dirname, '..', '..', '..');
const { chromium } = createRequire(path.join(raiz, 'e2e', 'package.json'))('@playwright/test');
const out = __dirname;
const clean = (s) => s.replace(/\s+/g, ' ');
(async () => {
  const browser = await chromium.launch({ channel: 'chrome' });
  const page = await (await browser.newContext({ locale: 'pt-BR', viewport: { width: 1280, height: 900 } })).newPage();
  const erros = [];
  page.on('pageerror', e => erros.push(e.message));
  await page.goto('http://localhost:4200/pedidos');
  await page.getByRole('table').waitFor();
  const filtro = page.locator('select').first();
  console.log('QA-06 FILTRO opcoes:', JSON.stringify((await filtro.locator('option').allInnerTexts()).map(clean)));
  const cels = await page.getByRole('table').locator('tbody th').allInnerTexts();
  console.log('QA-06 TABELA clientes (1a pagina):', JSON.stringify([...new Set(cels.map(clean))]));
  await page.screenshot({ path: out + '/qa-r2-01-listagem-homonimos.png', fullPage: true });

  await page.goto('http://localhost:4200/pedidos/novo');
  const selCli = page.getByLabel("Cliente", { exact: true });
  await selCli.waitFor();
  await page.waitForTimeout(800);
  await selCli.selectOption({ index: 1 });
  console.log('QA-06 FORM cliente opcoes:', JSON.stringify((await selCli.locator('option').allInnerTexts()).map(clean)));
  await page.getByRole("button", { name: "Adicionar item" }).click();
  const selProd = page.getByLabel("Produto (item 1)");
  const opts = await selProd.locator('option').evaluateAll(os => os.map(o => ({ t: o.textContent.replace(/\s+/g, ' ').trim(), d: o.disabled })));
  console.log('QA-07 PRODUTOS:', JSON.stringify(opts));
  const zero = opts.filter(o => /estoque 0\)/.test(o.t));
  console.log('QA-07 produtos estoque 0:', zero.length, 'todos desabilitados:', zero.every(o => o.d));
  // aviso: quantidade acima do estoque
  const comEstoque = opts.findIndex(o => !o.d && /estoque \d+\)/.test(o.t) && o.t !== 'Selecione um produto');
  await selProd.selectOption({ index: comEstoque });
  const m = opts[comEstoque].t.match(/estoque (\d+)\)/);
  const est = Number(m[1]);
  const qtd = page.getByLabel("Quantidade (item 1)");
  await qtd.fill(String(est + 1));
  await qtd.blur();
  const aviso = page.locator('[role="status"]:visible');
  console.log('QA-07 AVISO visivel:', await aviso.count(), JSON.stringify(await aviso.allInnerTexts()));
  console.log('QA-07 aria-describedby:', await qtd.getAttribute('aria-describedby'));
  const btn = page.getByRole('button', { name: 'Confirmar pedido' });
  console.log('QA-07 botao habilitado (nao bloqueia, 409 e a garantia):', await btn.isEnabled());
  await page.screenshot({ path: out + '/qa-r2-02-aviso-estoque.png', fullPage: true });
  if (await btn.isEnabled()) {
    await btn.click();
    await page.locator('[role="alert"]:visible').first().waitFor({ timeout: 8000 }).catch(() => console.log('sem alerta'));
    console.log('QA-07 409 TELA:', clean(await page.locator('main').innerText()).slice(0, 400));
    await page.screenshot({ path: out + '/qa-r2-03-409-apos-aviso.png', fullPage: true });
  }
  console.log('ERROS DE PAGINA:', JSON.stringify(erros));
  await browser.close();
})().catch(e => { console.error('FALHA', e); process.exit(1); });
