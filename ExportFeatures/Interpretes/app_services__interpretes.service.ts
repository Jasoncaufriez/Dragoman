import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../environments/environment';
import { InterpreteSearchDto, AudienceDto } from '../dtos/interprete-dto.model';
import { Observable } from 'rxjs/internal/Observable';


@Injectable({ providedIn: 'root' })
export class InterpretesService {
  private base = `${environment.apiUrl}/interpretes`;

  constructor(private http: HttpClient) { }

  // getIdentite(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}`); }
  getIdentite(tolkcode: number) {
    const url = `${this.base}/${tolkcode}`;
    console.log('GET', url);
    return this.http.get(url);
  }
  saveIdentite(tolkcode: number, payload: any) { return this.http.put(`${this.base}/${tolkcode}`, payload); }

  listAdresses(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}/adresses`); }
  addAdresse(tolkcode: number, payload: any) { return this.http.post(`${this.base}/${tolkcode}/adresses`, payload); }

  listIndispos(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}/indispos`); }
  addIndispo(tolkcode: number, payload: any) { return this.http.post(`${this.base}/${tolkcode}/indispos`, payload); }

  getTva(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}/tva`); }
  saveTva(tolkcode: number, payload: any) { return this.http.post(`${this.base}/${tolkcode}/tva`, payload); }

  listLangSource(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}/langues/source`); }
  listLangDest(tolkcode: number) { return this.http.get(`${this.base}/${tolkcode}/langues/destination`); }


  search(mode: 'tolkcode' | 'nom' | 'prenom' | 'langue', q: string): Observable<InterpreteSearchDto[]> {
    const params = new HttpParams().set('mode', mode).set('q', q);
    return this.http.get<InterpreteSearchDto[]>(`${this.base}/search`, { params });
  }

  audiencesExact(tolkcode: string): Observable<AudienceDto[]> {
    return this.http.get<AudienceDto[]>(`${this.base}/${tolkcode}/audiences-exact`);
  }

}
