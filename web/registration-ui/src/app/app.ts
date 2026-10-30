import { DatePipe } from '@angular/common';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { interval } from 'rxjs';
import { ApiResult, QueueInfo, RegistrationApi, RegistrationStatus, SentEmail } from './registration-api';

export interface LogEntry {
  at: Date;
  kind: 'action' | 'ok' | 'warn' | 'error' | 'info';
  text: string;
}

@Component({
  imports: [DatePipe],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
})
export class App {
  private readonly api = inject(RegistrationApi);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly email = signal(App.newEmail());
  protected readonly code = signal('');
  protected readonly userId = signal<string | null>(null);
  protected readonly status = signal<RegistrationStatus | null>(null);
  protected readonly lastStatusCode = signal<number | null>(null);
  protected readonly emails = signal<SentEmail[]>([]);
  protected readonly queues = signal<QueueInfo[]>([]);
  protected readonly log = signal<LogEntry[]>([]);
  protected readonly now = signal(new Date());

  protected readonly mailboxCode = computed(() => {
    const mail = this.emails().filter((m) => m.to === this.email() && m.subject === 'Confirma tu email').at(-1);
    return mail ? mail.body.replace(/\D/g, '') : null;
  });

  protected readonly secondsToExpiry = computed(() => {
    const current = this.status();
    if (!current?.expiresAt || current.sagaState === null) {
      return null;
    }
    return Math.max(0, Math.round((new Date(current.expiresAt).getTime() - this.now().getTime()) / 1000));
  });

  protected readonly phase = computed(() => {
    const current = this.status();
    if (!this.userId()) {
      return 'Sin registro';
    }
    if (this.lastStatusCode() === 404) {
      return 'Registro descartado: venció el plazo';
    }
    if (!current) {
      return 'Esperando a la saga...';
    }
    return current.userStatus === 'Active' ? 'Cuenta activa' : 'Pendiente de confirmación';
  });

  constructor() {
    interval(2000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.refresh());
    interval(1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.now.set(new Date()));
    this.refresh();
  }

  protected register(): void {
    this.add('action', `POST /registrations { email: ${this.email()} }`);
    this.api.register(this.email()).subscribe((result) => {
      this.report(result, 201, `usuario ${result.body} creado; la saga programa el plazo y pide el email de verificación`);
      if (result.status === 201 && result.body) {
        this.userId.set(result.body);
        this.status.set(null);
        this.lastStatusCode.set(null);
        this.refresh();
      }
    });
  }

  protected registerInvalidEmail(): void {
    this.add('action', 'POST /registrations { email: "no-es-un-email" }');
    this.api.register('no-es-un-email').subscribe((result) => this.report(result, 400, 'rechazado por FluentValidation antes de publicar nada'));
  }

  protected confirmWithMailboxCode(): void {
    const code = this.mailboxCode();
    if (!code) {
      this.add('warn', 'Todavía no hay código en el buzón del worker (¿está arrancado?)');
      return;
    }
    this.code.set(code);
    this.confirm(code, 'la saga acepta el código: activa el usuario, pide la bienvenida y termina');
  }

  protected confirmWrongCode(): void {
    this.confirm('000000', 'la API lo acepta pero la saga lo ignora y sigue esperando');
  }

  protected confirmMalformedCode(): void {
    this.confirm('12ab', 'rechazado por FluentValidation: tienen que ser seis dígitos', 400);
  }

  protected confirmTyped(): void {
    this.confirm(this.code(), 'enviado a la saga');
  }

  protected reset(): void {
    this.userId.set(null);
    this.status.set(null);
    this.lastStatusCode.set(null);
    this.code.set('');
    this.email.set(App.newEmail());
    this.add('info', 'Nuevo escenario');
  }

  protected updateEmail(event: Event): void {
    this.email.set((event.target as HTMLInputElement).value);
  }

  protected updateCode(event: Event): void {
    this.code.set((event.target as HTMLInputElement).value);
  }

  private confirm(code: string, meaning: string, expected = 200): void {
    const id = this.userId();
    if (!id) {
      this.add('warn', 'Primero registra un usuario');
      return;
    }
    this.add('action', `POST /registrations/${id}/confirm { code: ${code} }`);
    this.api.confirm(id, code).subscribe((result) => {
      this.report(result, expected, meaning);
      this.refresh();
    });
  }

  private refresh(): void {
    const id = this.userId();
    if (id) {
      this.api.status(id).subscribe((result) => {
        const previous = this.status();
        this.lastStatusCode.set(result.status);
        this.status.set(result.body);
        if (result.body && previous && previous.sagaState !== result.body.sagaState) {
          this.add('info', `saga: ${previous.sagaState ?? 'terminada'} → ${result.body.sagaState ?? 'terminada'}; usuario ${result.body.userStatus}`);
        }
        if (result.status === 404 && previous) {
          this.add('warn', 'GET /registrations devuelve 404: el plazo venció y RegistrationDiscarded borró al usuario');
        }
      });
    }
    this.api.emails().subscribe((emails) => {
      emails.slice(this.emails().length).forEach((m) => this.add('ok', `worker envió "${m.subject}" a ${m.to}: ${m.body}`));
      this.emails.set(emails);
    });
    this.api.queues().subscribe((queues) => this.queues.set(queues));
  }

  private report(result: ApiResult<unknown>, expected: number, meaning: string): void {
    const detail = result.errors.length ? ` (${result.errors.join('; ')})` : '';
    if (result.status === expected) {
      this.add('ok', `${result.status}: ${meaning}${detail}`);
    } else {
      this.add('error', `${result.status} (se esperaba ${expected})${detail}`);
    }
  }

  private add(kind: LogEntry['kind'], text: string): void {
    this.log.update((entries) => [{ at: new Date(), kind, text }, ...entries].slice(0, 200));
  }

  private static newEmail(): string {
    return `ana+${Date.now().toString().slice(-5)}@example.com`;
  }
}
