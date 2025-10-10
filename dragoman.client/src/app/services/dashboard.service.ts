// src/app/services/dashboard.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

// --- DTOs/Interfaces renvoyés par l'API ---
export interface Resume {
  audiences: number;
  langues: number;
  interpretes: number;
}

export interface InterpreteNonEncode {
  code: string;   // ex: "1518 TEST21/02"
  date: string;   // ex: "16/02" (format front; l’API peut renvoyer ISO)
}

export interface AudienceSupprimee {
  numero: string; // ex: "733"
  date: string;   // ex: "2025-08-27"
}

export interface LangueToday {
  langue: string;
  nb: number;
}

export interface AudienceItem {
  instance: 'VRM' | 'ANN';
  date: string;      // ISO ou format front
  heure?: string;
  salle?: string;
  numero?: string;   // si utile
  langue?: string;   // langue demandée si utile
}

// Réponses simples “count”
interface AudienceCountDto { nbAudiences: number; }
interface InterpretesCountDto { nbInterpretes: number; }

// Service
@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  /**
   * Base API :
   * - en prod derrière Apache reverse proxy : garder l'URL relative '/api'
   * - en dev sans proxy : remplace par `environment.apiBaseUrl` (ex: 'http://rvv-ccesrv21/api')
   */
  private base = '/api/dashboard';

  // ===== MOCKS (à garder pour dev rapide si besoin) =====
  getResumeMock(): Observable<Resume> {
    return of({ audiences: 6, langues: 6, interpretes: 6 });
  }
  getInterpretesNonEncodesMock(): Observable<InterpreteNonEncode[]> {
    return of([
      { code: '1518 TEST21/02', date: '16/02' },
      { code: '1022 TEST21/02', date: '27/02' },
      { code: '1841 TEST21/02', date: '28/02' }
    ]);
  }
  getAudiencesSupprimeesMock(): Observable<AudienceSupprimee[]> {
    return of([
      { numero: '733', date: '2017-10-18' },
      { numero: '1512', date: '2022-09-23' }
    ]);
  }

  // ===== API RÉELLE =====
  getAudiencesToday(): Observable<AudienceItem[]> {
    return this.http
      .get<AudienceItem[]>(`${this.base}/audiences/today`)
      .pipe(catchError(() => of([])));
  }
  getAudiencesDetailToday() {
    return this.http.get<any[]>('/api/dashboard/audiences/detail-today');
  }

  getAudienceCountToday(): Observable<AudienceCountDto> {
    return this.http
      .get<AudienceCountDto>(`${this.base}/audiences/count-today`)
      .pipe(catchError(() => of({ nbAudiences: 0 })));
  }

  getInterpretesCountToday(): Observable<InterpretesCountDto> {
    return this.http
      .get<InterpretesCountDto>(`${this.base}/interpretes/count-today`)
      .pipe(catchError(() => of({ nbInterpretes: 0 })));
  }

  getLanguesToday(): Observable<LangueToday[]> {
    return this.http
      .get<LangueToday[]>(`${this.base}/langues/today`)
      .pipe(catchError(() => of([])));
  }

  getAudiencesSupprimeesToday(): Observable<AudienceSupprimee[]> {
    return this.http
      .get<AudienceSupprimee[]>(`${this.base}/audiences-supprimees/today`)
      .pipe(catchError(() => of([])));
  }

  /**
   * Charge le résumé agrégé côté front (à partir des 3 endpoints).
   * Note : côté backend on filtre déjà la pseudo-langue "*Aucun interprète demandé".
   */
  loadResume(): Observable<Resume> {
    return forkJoin({
      aud: this.getAudienceCountToday(),
      int: this.getInterpretesCountToday(),
      lng: this.getLanguesToday()
    }).pipe(
      map(({ aud, int, lng }) => ({
        audiences: aud.nbAudiences,
        interpretes: int.nbInterpretes,
        langues: lng.length
      }))
    );
  }
}
