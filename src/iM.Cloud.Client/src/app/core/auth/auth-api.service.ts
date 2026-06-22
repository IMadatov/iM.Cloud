import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AuthClient,
  LoginRequest,
  LoginResponse,
  MeResponse,
  RefreshRequest,
} from '@im-cloud/api';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly client = inject(AuthClient);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.client.login(
      new LoginRequest({
        email,
        password,
      }),
    );
  }

  refresh(refreshToken: string): Observable<LoginResponse> {
    return this.client.refresh(
      new RefreshRequest({
        refreshToken,
      }),
    );
  }

  me(): Observable<MeResponse> {
    return this.client.me();
  }
}
