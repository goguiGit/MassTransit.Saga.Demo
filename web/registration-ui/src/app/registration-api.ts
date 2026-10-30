import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, map, of } from 'rxjs';

export interface RegistrationStatus {
  userId: string;
  email: string;
  userStatus: 'PendingConfirmation' | 'Active';
  sagaState: string | null;
  createdAt: string;
  activatedAt: string | null;
  expiresAt: string | null;
}

export interface SentEmail {
  to: string;
  subject: string;
  body: string;
  sentAt: string;
}

export interface QueueInfo {
  name: string;
  ready: number;
  unacked: number;
  consumers: number;
}

export interface ApiResult<T> {
  status: number;
  body: T | null;
  errors: string[];
}

interface RabbitQueue {
  name: string;
  messages_ready: number;
  messages_unacknowledged: number;
  consumers: number;
}

@Injectable({ providedIn: 'root' })
export class RegistrationApi {
  private readonly http = inject(HttpClient);

  register(email: string): Observable<ApiResult<string>> {
    return this.call(this.http.post<string>('/api/registrations', { email }, { observe: 'response' }));
  }

  confirm(userId: string, code: string): Observable<ApiResult<void>> {
    return this.call(this.http.post<void>(`/api/registrations/${userId}/confirm`, { code }, { observe: 'response' }));
  }

  status(userId: string): Observable<ApiResult<RegistrationStatus>> {
    return this.call(this.http.get<RegistrationStatus>(`/api/registrations/${userId}`, { observe: 'response' }));
  }

  emails(): Observable<SentEmail[]> {
    return this.http.get<SentEmail[]>('/worker/emails').pipe(catchError(() => of([])));
  }

  queues(): Observable<QueueInfo[]> {
    return this.http.get<RabbitQueue[]>('/rabbit/api/queues').pipe(
      map((queues) =>
        queues
          .filter((q) => !q.name.includes('_bus_'))
          .map((q) => ({ name: q.name, ready: q.messages_ready, unacked: q.messages_unacknowledged, consumers: q.consumers }))
          .sort((a, b) => a.name.localeCompare(b.name)),
      ),
      catchError(() => of([])),
    );
  }

  private call<T>(request: Observable<{ status: number; body: T | null }>): Observable<ApiResult<T>> {
    return request.pipe(
      map((response) => ({ status: response.status, body: response.body, errors: [] })),
      catchError((error: HttpErrorResponse) => of({ status: error.status, body: null, errors: describe(error) })),
    );
  }
}

function describe(error: HttpErrorResponse): string[] {
  const payload = error.error;
  if (Array.isArray(payload)) {
    return payload.map((x) => `${x.identifier}: ${x.errorMessage}`);
  }
  if (payload?.errors) {
    return Object.entries(payload.errors as Record<string, string[]>).flatMap(([field, messages]) => messages.map((m) => `${field}: ${m}`));
  }
  if (payload?.detail) {
    return [payload.detail];
  }
  return [error.status === 0 ? 'API not reachable' : `${error.status} ${error.statusText}`];
}
