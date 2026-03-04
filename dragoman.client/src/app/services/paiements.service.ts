import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface PaiementMoisInterpreteRowDto {
  tolkcode: string;
  nom: string;
  prenom: string;
  nbPrestations: number;
  montant: number;
  transport: number;
  montantTva: number;
  total: number;
}

export interface PaiementMoisDetailRowDto {
  idPaiement: number;
  date: string;      // ISO
  debut: string;     // HH:mm
  fin: string;       // HH:mm
  duree: number;     // minutes
  km: number;
  montant: number;
  transport: number;
  idFacture: number | null;
}

export interface PaiementMoisTotauxDto {
  montant: number;
  transport: number;
  baseHt: number;
  montantTva: number;
  total: number;
}

export interface PaiementMoisDetailDto {
  tolkcode: string;
  nom: string;
  prenom: string;
  rows: PaiementMoisDetailRowDto[];
  totaux: PaiementMoisTotauxDto;
}

@Injectable({ providedIn: 'root' })
export class PaiementsService {
  private base = '/api/paiements';
  constructor(private http: HttpClient) { }

  listInterpretes(month: string): Observable<PaiementMoisInterpreteRowDto[]> {
    const params = new HttpParams().set('month', month);
    return this.http.get<PaiementMoisInterpreteRowDto[]>(`${this.base}/mois`, { params });
  }

  detail(month: string, tolkcode: string): Observable<PaiementMoisDetailDto> {
    const params = new HttpParams().set('month', month);
    return this.http.get<PaiementMoisDetailDto>(`${this.base}/mois/${tolkcode}`, { params });
  }
  deletePaiement(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  downloadMonthPdf(month: string) {
    return this.http.get(`/api/paiements/mois/pdf`, {
      params: { month },
      responseType: 'blob'
    });
  }

}
