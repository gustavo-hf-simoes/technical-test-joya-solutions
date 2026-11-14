import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { BehaviorSubject, catchError, combineLatest, map, of, startWith, switchMap } from 'rxjs';

import { ClientesApi } from '../core/api/clientes-api';
import { PedidosApi } from '../core/api/pedidos-api';
import { FiltroPedidos, PaginaResponse, PedidoResumoResponse } from '../core/api/pedidos.models';

const TAMANHO_PAGINA = 20;

type EstadoLista =
  | { tipo: 'carregando' }
  | { tipo: 'erro' }
  | { tipo: 'sucesso'; pagina: PaginaResponse<PedidoResumoResponse> };

@Component({
  selector: 'app-pedido-lista',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pedido-lista.component.html',
})
export class PedidoListaComponent {
  private readonly pedidosApi = inject(PedidosApi);
  private readonly clientesApi = inject(ClientesApi);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly tentarNovamente$ = new BehaviorSubject<void>(undefined);

  readonly clientes = toSignal(
    this.clientesApi.listar().pipe(catchError(() => of([]))),
    { initialValue: [] },
  );

  readonly filtros = inject(NonNullableFormBuilder).group({
    clienteId: [''],
    dataInicio: [''],
    dataFim: [''],
  });

  readonly estado = signal<EstadoLista>({ tipo: 'carregando' });
  readonly periodoInvalido = signal(false);

  constructor() {
    const filtroDaUrl$ = this.route.queryParamMap.pipe(map((q): FiltroPedidos => this.lerFiltro(q)));

    filtroDaUrl$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((filtro) => {
      this.filtros.setValue({
        clienteId: filtro.clienteId?.toString() ?? '',
        dataInicio: filtro.dataInicio ?? '',
        dataFim: filtro.dataFim ?? '',
      });
    });

    combineLatest([filtroDaUrl$, this.tentarNovamente$])
      .pipe(
        switchMap(([filtro]) =>
          this.pedidosApi.listar({ ...filtro, pageSize: TAMANHO_PAGINA }).pipe(
            map((pagina): EstadoLista => ({ tipo: 'sucesso', pagina })),
            catchError(() => of<EstadoLista>({ tipo: 'erro' })),
            startWith<EstadoLista>({ tipo: 'carregando' }),
          ),
        ),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((estado) => this.estado.set(estado));
  }

  aplicar(): void {
    const { clienteId, dataInicio, dataFim } = this.filtros.getRawValue();
    this.periodoInvalido.set(!!dataInicio && !!dataFim && dataFim < dataInicio);
    if (this.periodoInvalido()) return;

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        clienteId: clienteId || null,
        dataInicio: dataInicio || null,
        dataFim: dataFim || null,
        page: null,
      },
      queryParamsHandling: 'merge',
    });
  }

  limpar(): void {
    this.periodoInvalido.set(false);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { clienteId: null, dataInicio: null, dataFim: null, page: null },
      queryParamsHandling: 'merge',
    });
  }

  irParaPagina(page: number): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: page > 1 ? page : null },
      queryParamsHandling: 'merge',
    });
  }

  tentarNovamente(): void {
    this.tentarNovamente$.next();
  }

  private lerFiltro(q: { get(nome: string): string | null }): FiltroPedidos {
    const filtro: FiltroPedidos = {};
    const clienteId = Number(q.get('clienteId'));
    if (Number.isInteger(clienteId) && clienteId >= 1) filtro.clienteId = clienteId;
    const dataInicio = q.get('dataInicio');
    if (dataInicio && /^\d{4}-\d{2}-\d{2}$/.test(dataInicio)) filtro.dataInicio = dataInicio;
    const dataFim = q.get('dataFim');
    if (dataFim && /^\d{4}-\d{2}-\d{2}$/.test(dataFim)) filtro.dataFim = dataFim;
    const page = Number(q.get('page'));
    if (Number.isInteger(page) && page >= 1) filtro.page = page;
    return filtro;
  }
}
