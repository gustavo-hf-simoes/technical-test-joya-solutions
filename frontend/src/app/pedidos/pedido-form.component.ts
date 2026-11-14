import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { forkJoin, map, startWith } from 'rxjs';

import { ClientesApi } from '../core/api/clientes-api';
import { ClienteResponse } from '../core/api/clientes.models';
import { PedidosApi } from '../core/api/pedidos-api';
import { CriarPedidoRequest } from '../core/api/pedidos.models';
import { ProblemaApi } from '../core/api/problema-api.model';
import { ProdutosApi } from '../core/api/produtos-api';
import { ProdutoResponse } from '../core/api/produtos.models';

const MAX_ITENS = 50; // RN-01
const QTD_MIN = 1; // RN-02
const QTD_MAX = 10_000;

type ItemForm = FormGroup<{
  produtoId: FormControl<string>;
  quantidade: FormControl<number | null>;
}>;

type EstadoApoio = 'carregando' | 'erro' | 'pronto';

interface ErroServidor {
  titulo: string;
  detalhe: string;
  /** Mensagem por `produtoId` (409 estoque, 422 produto inexistente). */
  porProduto: Record<number, string>;
  /** Mensagem do campo cliente (422 cliente). */
  cliente?: string;
}

const inteiro: ValidatorFn = (c: AbstractControl): ValidationErrors | null =>
  c.value === null || c.value === '' || Number.isInteger(c.value) ? null : { inteiro: true };

/** RN-01: ao menos um item (Validators.minLength ignora lista vazia). */
const comItens: ValidatorFn = (c: AbstractControl): ValidationErrors | null =>
  (c as FormArray).length > 0 ? null : { semItens: true };

/** RN-03: o mesmo produto não pode aparecer em dois itens. */
const semProdutoRepetido: ValidatorFn = (c: AbstractControl): ValidationErrors | null => {
  const ids = (c as FormArray<ItemForm>).controls
    .map((g) => g.controls.produtoId.value)
    .filter((id) => id !== '');
  return new Set(ids).size === ids.length ? null : { produtoRepetido: true };
};

@Component({
  selector: 'app-pedido-form',
  imports: [ReactiveFormsModule, RouterLink, CurrencyPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pedido-form.component.html',
})
export class PedidoFormComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly pedidosApi = inject(PedidosApi);
  private readonly clientesApi = inject(ClientesApi);
  private readonly produtosApi = inject(ProdutosApi);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly qtdMin = QTD_MIN;
  readonly qtdMax = QTD_MAX;
  readonly maxItens = MAX_ITENS;

  /** HL-22 / RN-14: gerada uma vez ao abrir o formulário e reutilizada em todo reenvio. */
  private readonly idempotencyKey = crypto.randomUUID();

  readonly clientes = signal<ClienteResponse[]>([]);
  readonly produtos = signal<ProdutoResponse[]>([]);
  readonly estadoApoio = signal<EstadoApoio>('carregando');
  readonly enviando = signal(false);
  readonly erroServidor = signal<ErroServidor | null>(null);
  private readonly resumoErro = viewChild<ElementRef<HTMLElement>>('resumoErro');

  readonly form = this.fb.group({
    clienteId: this.fb.control('', Validators.required),
    itens: this.fb.array<ItemForm>(
      [],
      [comItens, Validators.maxLength(MAX_ITENS), semProdutoRepetido],
    ),
  });

  private readonly valor = toSignal(
    this.form.valueChanges.pipe(
      startWith(null),
      map(() => this.form.getRawValue()),
    ),
    { initialValue: this.form.getRawValue() },
  );

  /** Estimativa só informativa: o preço oficial vem do servidor (RN-09). */
  readonly totalEstimado = computed(() => {
    const precos = new Map(this.produtos().map((p) => [String(p.id), p.preco]));
    return this.valor().itens.reduce(
      (soma, i) => soma + (precos.get(i.produtoId) ?? 0) * (Number(i.quantidade) || 0),
      0,
    );
  });

  /** Por que o botão está desabilitado (o motivo não fica só implícito). */
  readonly motivoBloqueio = computed(() => {
    this.valor();
    if (this.form.valid) return null;
    if (this.form.controls.clienteId.invalid) return 'Selecione um cliente para confirmar.';
    if (this.form.controls.itens.length === 0) return 'Adicione ao menos um item para confirmar.';
    return 'Corrija os itens destacados para confirmar.';
  });

  constructor() {
    this.carregarApoio();
  }

  carregarApoio(): void {
    this.estadoApoio.set('carregando');
    forkJoin([this.clientesApi.listar(true), this.produtosApi.listar(true)])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ([clientes, produtos]) => {
          this.clientes.set(clientes);
          this.produtos.set(produtos);
          this.estadoApoio.set('pronto');
        },
        error: () => this.estadoApoio.set('erro'),
      });
  }

  adicionarItem(): void {
    if (this.form.controls.itens.length >= MAX_ITENS) return;
    this.form.controls.itens.push(
      this.fb.group({
        produtoId: this.fb.control('', Validators.required),
        quantidade: this.fb.control<number | null>(1, [
          Validators.required,
          Validators.min(QTD_MIN),
          Validators.max(QTD_MAX),
          inteiro,
        ]),
      }),
    );
  }

  removerItem(indice: number): void {
    this.form.controls.itens.removeAt(indice);
  }

  /** Mensagem do campo quantidade, ou `null` se válido (ou ainda intocado). */
  erroQuantidade(item: ItemForm): string | null {
    const c = item.controls.quantidade;
    if (c.valid || !(c.dirty || c.touched)) return null;
    if (c.hasError('required')) return 'Informe a quantidade.';
    if (c.hasError('inteiro')) return 'A quantidade deve ser um número inteiro.';
    return `A quantidade deve estar entre ${QTD_MIN} e ${QTD_MAX}.`;
  }

  erroProduto(item: ItemForm): string | null {
    const c = item.controls.produtoId;
    const id = c.value;
    const doServidor = id ? this.erroServidor()?.porProduto[Number(id)] : undefined;
    if (doServidor) return doServidor;
    if (id && this.produtoRepetido(id)) return 'Este produto já foi adicionado ao pedido.';
    if (c.invalid && (c.dirty || c.touched)) return 'Selecione um produto.';
    return null;
  }

  erroCliente(): string | null {
    const doServidor = this.erroServidor()?.cliente;
    if (doServidor) return doServidor;
    const c = this.form.controls.clienteId;
    return c.invalid && (c.dirty || c.touched) ? 'Selecione um cliente.' : null;
  }

  confirmar(): void {
    if (this.form.invalid || this.enviando()) return;
    this.enviando.set(true);
    this.erroServidor.set(null);

    const { clienteId, itens } = this.form.getRawValue();
    const corpo: CriarPedidoRequest = {
      clienteId: Number(clienteId),
      itens: itens.map((i) => ({ produtoId: Number(i.produtoId), quantidade: Number(i.quantidade) })),
    };

    this.pedidosApi
      .criar(corpo, this.idempotencyKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (pedido) => {
          void this.router.navigate(['/pedidos'], { state: { pedidoCriadoId: pedido.id } });
        },
        error: (e: unknown) => this.tratarErro(e),
      });
  }

  private tratarErro(e: unknown): void {
    this.enviando.set(false);
    const resposta = e instanceof HttpErrorResponse ? e : null;
    const problema = resposta ? (resposta.error as ProblemaApi | null) : null;
    const status = resposta?.status ?? 0;
    const erro: ErroServidor = {
      titulo: 'Não foi possível criar o pedido.',
      detalhe: 'Verifique a conexão e tente confirmar novamente. Seus dados foram mantidos.',
      porProduto: {},
    };

    if (status === 409 && problema?.itens) {
      erro.titulo = 'Estoque insuficiente.';
      erro.detalhe = 'Ajuste as quantidades dos itens destacados. A lista de produtos foi atualizada.';
      for (const i of problema.itens) {
        erro.porProduto[i.produtoId] =
          `Estoque insuficiente: pedido ${i.quantidadeSolicitada}, disponível ${i.estoqueDisponivel}.`;
      }
      this.recarregarProdutos();
    } else if (status === 422) {
      erro.titulo = 'O pedido não pôde ser aceito.';
      erro.detalhe = problema?.detail ?? 'Revise os dados informados.';
      if (problema?.code === 'CLIENTE_NAO_ENCONTRADO' || problema?.code === 'CLIENTE_INATIVO') {
        erro.cliente = problema.detail ?? 'Este cliente não pode receber pedidos.';
      }
      for (const id of problema?.produtoIds ?? []) {
        erro.porProduto[id] = 'Produto inexistente ou inativo. Escolha outro.';
      }
      if (problema?.produtoIds?.length) this.recarregarProdutos();
    } else if (status === 400) {
      erro.titulo = 'Dados inválidos.';
      erro.detalhe = problema?.detail ?? 'Revise os campos do pedido.';
    }

    this.erroServidor.set(erro);
    afterNextRender(() => this.resumoErro()?.nativeElement.focus(), { injector: this.injector });
  }

  /** Atualiza preço e estoque sem perder o que o usuário digitou (CA-32). */
  private recarregarProdutos(): void {
    this.produtosApi
      .listar(true)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ next: (p) => this.produtos.set(p) });
  }

  private produtoRepetido(id: string): boolean {
    return this.form.controls.itens.controls.filter((g) => g.controls.produtoId.value === id).length > 1;
  }
}
