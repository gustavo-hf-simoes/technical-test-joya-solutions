import { formatCurrency } from '@angular/common';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { appConfig } from './app.config';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [...appConfig.providers, provideRouter([])],
    });
  });

  it('AppComponent_Inicializacao_UsaLocalePtBr', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const locale = TestBed.inject(LOCALE_ID);
    expect(locale).toBe('pt-BR');
    expect(formatCurrency(1234.5, locale, 'R$', 'BRL').replace(/\s/g, ' ')).toBe('R$ 1.234,50');
    expect(fixture.nativeElement.textContent).toContain('Gestão de Pedidos');
  });
});
