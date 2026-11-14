import { expect, test } from '@playwright/test';

// SEC-02: os cabeçalhos de segurança precisam chegar em todas as respostas do Nginx.
test('CabecalhosSeguranca_RotasDaSpaEAtivos_TrazemCspEDemais (SEC-02)', async ({ request }) => {
  const raiz = await request.get('/');
  const html = await raiz.text();
  const script = /src="([^"]+\.js)"/.exec(html)?.[1];
  expect(script, 'index.html deve referenciar um arquivo .js').toBeTruthy();

  const rotas = ['/', '/pedidos', `/${String(script).replace(/^\//, '')}`, '/api/clientes'];

  for (const rota of rotas) {
    const resposta = await request.get(rota);
    const h = resposta.headers();
    expect(resposta.status(), rota).toBeLessThan(500);
    expect(h['x-content-type-options'], rota).toBe('nosniff');
    expect(h['x-frame-options'], rota).toBe('DENY');
    expect(h['referrer-policy'], rota).toBe('no-referrer');
    const csp = h['content-security-policy'] ?? '';
    expect(csp, rota).toContain("default-src 'self'");
    expect(csp, rota).toContain("frame-ancestors 'none'");
    expect(csp, rota).toContain("object-src 'none'");
    expect(csp, rota).not.toContain('unsafe-eval');
  }
});
