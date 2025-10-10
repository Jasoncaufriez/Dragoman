// src/app/services/authentification.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthentificationService {
  private http = inject(HttpClient);
  private baseUrl = '/api/auth';
 
  /** Cache du login courant */
  private loginSubject = new BehaviorSubject<string | null>(null);
  /** Observable pour la navbar (ex: async pipe) */
  readonly login$ = this.loginSubject.asObservable();

  /** Appelé au démarrage via APP_INITIALIZER pour amorcer NTLM */
  warmup(): Promise<void> {
    return firstValueFrom(
      this.http.get(`${this.baseUrl}/whoami`, {
        responseType: 'text',
        withCredentials: true
      }).pipe(
        tap(login => this.loginSubject.next(login)),
        catchError(() => {
          // on ignore les erreurs du handshake, l’appli peut quand même démarrer
          this.loginSubject.next(null);
          return of('');
        }),
        map(() => void 0)
      )
    );
  }

  /** Récupère le login (utilise le cache si déjà connu) */
  getLogin(): Observable<string> {
    const cached = this.loginSubject.value;
    if (cached) return of(cached);

    return this.http.get(`${this.baseUrl}/whoami`, {
      responseType: 'text',
      withCredentials: true
    }).pipe(
      tap(login => this.loginSubject.next(login))
    );
  }

  /** Accès direct si tu en as besoin */
  get cachedLogin(): string | null {
    return this.loginSubject.value;
  }
}
