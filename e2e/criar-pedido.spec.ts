import { expect, test } from '@playwright/test';

const formatarBrl = (valor: number): string =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
    .format(valor)
    .replace(/\s/g, ' ');

const normalizar = (texto: string): string => texto.replace(/\s/g, ' ').trim();

test('CriarPedido_FluxoCompleto_ApareceNoTopoDaListagem (CA-30)', async ({ page }) => {
  await page.goto('/pedidos/novo');
  await expect(page.getByRole('heading', { name: 'Novo pedido' })).toBeVisible();

  const cliente = page.getByLabel('Cliente', { exact: true });
  await expect(cliente.locator('option').nth(1)).toBeAttached();
  const nomesClientes = (await cliente.locator('option').allTextContents()).slice(1);
  const nomeCliente = normalizar(nomesClientes[0]);
  await cliente.selectOption({ index: 1 });

  await page.getByRole('button', { name: 'Adicionar item' }).click();
  const produto = page.getByLabel('Produto (item 1)');
  await expect(produto.locator('option').nth(1)).toBeAttached();
  const textos = (await produto.locator('option').allTextContents()).map(normalizar);
  // Escolhe pela tela o primeiro produto com estoque >= 2 e lê o preço exibido.
  let indice = -1;
  let preco = 0;
  for (let i = 1; i < textos.length; i++) {
    const m = /R\$ ([\d.]+,\d{2}) \(estoque (\d+)\)/.exec(textos[i]);
    if (m && Number(m[2]) >= 2) {
      indice = i;
      preco = Number(m[1].replace(/\./g, '').replace(',', '.'));
      break;
    }
  }
  expect(indice, 'produto com estoque disponível').toBeGreaterThan(0);
  await produto.selectOption({ index: indice });
  await page.getByLabel('Quantidade (item 1)').fill('2');

  await expect(page.getByText(`Total estimado: ${formatarBrl(preco * 2)}`)).toBeVisible();
  await page.getByRole('button', { name: 'Confirmar pedido' }).click();

  await expect(page).toHaveURL(/\/pedidos$/);
  const aviso = page.getByRole('status').filter({ hasText: /Pedido #\d+ criado com sucesso/ });
  await expect(aviso).toBeVisible();

  const linha = page.getByRole('table').getByRole('row').nth(1);
  await expect(linha.getByRole('rowheader')).toHaveText(nomeCliente);
  await expect(linha.getByRole('cell').last()).toHaveText(formatarBrl(preco * 2), {
    useInnerText: true,
  });
});
