import { nomesRepetidos, rotuloCliente } from './rotulo-cliente';

describe('rotuloCliente (QA-06)', () => {
  const clientes = [
    { id: 1, nome: 'Maria Silva' },
    { id: 2, nome: 'maria silva ' },
    { id: 3, nome: 'João Souza' },
  ];

  it('rotuloCliente_NomeRepetido_AcrescentaId', () => {
    const repetidos = nomesRepetidos(clientes);
    expect(rotuloCliente(clientes[0], repetidos)).toBe('Maria Silva (#1)');
    expect(rotuloCliente(clientes[1], repetidos)).toBe('maria silva  (#2)');
  });

  it('rotuloCliente_NomeUnico_MantemSoONome', () => {
    expect(rotuloCliente(clientes[2], nomesRepetidos(clientes))).toBe('João Souza');
  });

  it('nomesRepetidos_ListaVazia_RetornaVazio', () => {
    expect(nomesRepetidos([]).size).toBe(0);
  });
});
