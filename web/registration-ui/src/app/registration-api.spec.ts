import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RegistrationApi } from './registration-api';

describe('RegistrationApi', () => {
  let api: RegistrationApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(RegistrationApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('register posts the email and returns the created id', () => {
    let result: { status: number; body: string | null } | undefined;
    api.register('ana@example.com').subscribe((r) => (result = r));

    const request = http.expectOne('/api/registrations');
    expect(request.request.body).toEqual({ email: 'ana@example.com' });
    request.flush('f4100000-a119-33e1-668c-08df36ad00a8', { status: 201, statusText: 'Created' });

    expect(result).toEqual({ status: 201, body: 'f4100000-a119-33e1-668c-08df36ad00a8', errors: [] });
  });

  it('register turns a validation failure into readable errors', () => {
    let result: { status: number; errors: string[] } | undefined;
    api.register('ana').subscribe((r) => (result = r));

    http.expectOne('/api/registrations').flush(
      [{ identifier: 'Email', errorMessage: "'Email' is not a valid email address.", errorCode: 'EmailValidator', severity: 0 }],
      { status: 400, statusText: 'Bad Request' },
    );

    expect(result).toEqual({ status: 400, body: null, errors: ["Email: 'Email' is not a valid email address."] });
  });

  it('confirm reports a conflict as its detail', () => {
    let result: { status: number; errors: string[] } | undefined;
    api.confirm('id', '123456').subscribe((r) => (result = r));

    http.expectOne('/api/registrations/id/confirm').flush(
      { status: 409, title: 'There was a conflict.', detail: 'The account is already active.' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(result?.errors).toEqual(['The account is already active.']);
  });

  it('emails returns an empty list when the worker is down', () => {
    let result: unknown[] | undefined;
    api.emails().subscribe((r) => (result = r));

    http.expectOne('/worker/emails').error(new ProgressEvent('error'), { status: 0 });

    expect(result).toEqual([]);
  });
});
