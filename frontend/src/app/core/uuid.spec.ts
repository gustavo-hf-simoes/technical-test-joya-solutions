import { gerarUuid } from './uuid';

const V4 = /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/;

describe('gerarUuid', () => {
  it('GerarUuid_ComRandomUUID_UsaONativo', () => {
    const c = { randomUUID: () => 'nativo' } as unknown as Crypto;
    expect(gerarUuid(c)).toBe('nativo');
  });

  it('GerarUuid_SemRandomUUID_MontaUuidV4ComGetRandomValues', () => {
    const c = {
      getRandomValues: (a: Uint8Array) => {
        a.fill(0xff);
        return a;
      },
    } as unknown as Crypto;
    const u = gerarUuid(c);
    expect(u).toMatch(V4);
    expect(u).toBe('ffffffff-ffff-4fff-bfff-ffffffffffff');
  });

  it('GerarUuid_Padrao_RetornaUuidV4', () => {
    expect(gerarUuid()).toMatch(V4);
  });
});
