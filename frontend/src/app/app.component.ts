import { ChangeDetectionStrategy, Component, LOCALE_ID, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="border-b border-slate-200 bg-white">
      <div class="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
        <a routerLink="/pedidos" class="text-lg font-semibold text-slate-900">Gestão de Pedidos</a>
      </div>
    </header>
    <main class="mx-auto max-w-5xl px-4 py-6">
      <router-outlet />
    </main>
  `,
})
export class AppComponent {
  readonly locale = inject(LOCALE_ID);
}
