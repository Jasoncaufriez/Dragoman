import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { HDPrestationJour, HDSaveResultat } from './hd-prestations.model';

@Injectable({ providedIn: 'root' })
export class HDPrestationsService {
  // IMPORTANT : correspond à HdPrestationsController.cs [Route("api/hd-prestations")]
  private base = '/api/hd-prestations';
  private nocache = new HttpHeaders({ 'Cache-Control': 'no-cache' });

  constructor(private http: HttpClient) { }

  lireJour(hdUser: string, hdDate: string): Observable<HDPrestationJour | null> {
    const params = new HttpParams().set('hdUser', hdUser).set('hdDate', hdDate);
    return this.http.get<HDPrestationJour | null>(`${this.base}/jour`, { params, headers: this.nocache });
  }

  enregistrerJour(dto: HDPrestationJour): Observable<HDSaveResultat> {
    return this.http.post<HDSaveResultat>(`${this.base}/jour`, dto, { headers: this.nocache });
  }

  lireSemaine(hdUser: string, hdSemaineISO: string): Observable<HDPrestationJour[]> {
    const params = new HttpParams().set('hdUser', hdUser).set('hdSemaineISO', hdSemaineISO);
    return this.http.get<HDPrestationJour[]>(`${this.base}/semaine`, { params, headers: this.nocache });
  }

  telechargerSemaineWord(hdUser: string, hdSemaineISO: string): Observable<Blob> {
    const params = new HttpParams().set('hdUser', hdUser).set('hdSemaineISO', hdSemaineISO);
    return this.http.get(`${this.base}/semaine/export/word`, {
      params,
      headers: this.nocache,
      responseType: 'blob'
    });
  }
}
