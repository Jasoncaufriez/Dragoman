import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface GenererFacturesRequest {
  annee: number;
  mois: number;
  dateDebut?: string;  // "YYYY-MM-DD" — si renseigné, génère par période
  dateFin?: string;    // "YYYY-MM-DD"
}

export interface GenererFacturesResult {
  created: number;
  linked: number;
}

export interface FactureListItem {
  idFacture: number;
  reference: string;
  tolkcode: string;
  nom: string;
  prenom: string;
  dateGeneration: string;
  dateValidationFedcom: string | null;
  dateTransmission: string | null;
  statutFacture: string;
  totalTtc: number;
  nbPaiements: number;
}

export interface FactureFilters {
  month?: string;
  statut?: string;
  tolkcode?: string;
}

export interface UpdateStatutResult {
  idFacture: number;
  reference: string;
  statutFacture: string;
  dateValidationFedcom: string | null;
}

export interface TransmettreResult {
  idFacture: number;
  reference: string;
  statutFacture: string;
  dateTransmission: string | null;
}

@Injectable({ providedIn: 'root' })
export class FacturesGenService {
  private base = '/api/factures';

  constructor(private http: HttpClient) {}

  generer(req: GenererFacturesRequest): Observable<GenererFacturesResult> {
    return this.http.post<GenererFacturesResult>(`${this.base}/generer`, req);
  }

  list(filters: FactureFilters = {}): Observable<FactureListItem[]> {
    let params = new HttpParams();
    if (filters.month) params = params.set('month', filters.month);
    if (filters.statut) params = params.set('statut', filters.statut);
    if (filters.tolkcode) params = params.set('tolkcode', filters.tolkcode);
    return this.http.get<FactureListItem[]>(this.base, { params });
  }

  updateStatut(id: number, statutFacture: string): Observable<UpdateStatutResult> {
    return this.http.patch<UpdateStatutResult>(`${this.base}/${id}/statut`, { statutFacture });
  }

  transmettre(id: number): Observable<TransmettreResult> {
    return this.http.patch<TransmettreResult>(`${this.base}/${id}/transmettre`, {});
  }

  downloadPdf(month: string, po: string): Observable<Blob> {
    const params = new HttpParams().set('month', month).set('po', po);
    return this.http.get(`${this.base}/pdf`, { params, responseType: 'blob' });
  }

  downloadEml(id: number, po: string): Observable<Blob> {
    const params = new HttpParams().set('po', po);
    return this.http.get(`${this.base}/${id}/eml`, { params, responseType: 'blob' });
  }
}
