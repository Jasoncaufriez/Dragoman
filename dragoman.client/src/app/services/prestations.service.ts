import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PrestationJourRowDto {
  tolkcode: string;
  nom: string;
  prenom: string;
  telephone?: string | null;
  idAffAudiences: number[];
  heureAudienceSuggee?: string | null;
  hasPrestation: boolean;
}

export interface NewPrestationDto {
  tolkcode: string;
  datePrestation: string;
  startheure: string;
  endheure: string;
  idAffAudiences: number[];
}

@Injectable({ providedIn: 'root' })
export class PrestationsService {
  private base = '/api/prestations';

  constructor(private http: HttpClient) {}

  liste(dateISO: string): Observable<PrestationJourRowDto[]> {
    const params = new HttpParams().set('date', dateISO);
    return this.http.get<PrestationJourRowDto[]>(`${this.base}/jour`, { params });
  }

  create(dto: NewPrestationDto): Observable<void> {
    return this.http.post<void>(this.base, dto);
  }

  absence(payload: { tolkcode: string; idAffAudience: number; datePrestation: string }): Observable<void> {
    return this.http.post<void>(`${this.base}/absence`, payload);
  }

  remplacement(payload: { idAffAudience: number; ancienTolkcode: string; nouveauTolkcode: string }): Observable<void> {
    return this.http.post<void>(`${this.base}/remplacement`, payload);
  }
}
