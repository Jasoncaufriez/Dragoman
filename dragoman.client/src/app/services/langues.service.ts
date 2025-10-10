import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface LangueRef {
  idlangue: number;           // on re-map le byte? en number côté TS
  codeIso?: string | null;
  libelleFr?: string | null;
  libelleNl?: string | null;
}

export interface LangueSource {
  idLangueSource: number;     // FIX: Correspondre exactement au DTO serveur
  tolkcode: number;
  nrLangue: number;
  codeIso?: string | null;
  libelleFr?: string | null;
  libelleNl?: string | null;
}

export interface LangueDestination {
  idLanguedestination: number;
  tolkcode: number;
  nrLangue: number;
  codeIso?: string | null;
  libelleFr?: string | null;
  libelleNl?: string | null;
}

@Injectable({ providedIn: 'root' })
export class LanguesService {
  constructor(private http: HttpClient) { }

  // référentiel
  listRef(destOnly = false) {
    const qs = destOnly ? '?destOnly=true' : '';
    return this.http.get<LangueRef[]>(`/api/langues${qs}`);
  }

  // source
  listSources(tolkcode: number) {
    return this.http.get<LangueSource[]>(`/api/interpretes/${tolkcode}/langues/sources`);
  }
  addSource(tolkcode: number, nrLangue: number) {
    return this.http.post<void>(`/api/interpretes/${tolkcode}/langues/source`, { nrLangue });
  }
  removeSource(tolkcode: number, id: number) {
    return this.http.delete<void>(`/api/interpretes/${tolkcode}/langues/source/${id}`);
  }

  // destination

  listDestinations(tolkcode: number) {
    return this.http.get<LangueDestination[]>(`/api/interpretes/${tolkcode}/langues/destination`);
  }
  addDestination(tolkcode: number, nrLangue: number) {
    return this.http.post<void>(`/api/interpretes/${tolkcode}/langues/destination`, { nrLangue });
  }
  removeDestination(tolkcode: number, id: number) {
    return this.http.delete<void>(`/api/interpretes/${tolkcode}/langues/destination/${id}`);
  }
}
