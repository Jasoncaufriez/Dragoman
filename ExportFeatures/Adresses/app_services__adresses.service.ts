import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

export interface Tolkadresse {
  idAdresse: number;
  tolkcode: number;           // ou string si c'est textuel chez toi
  land: string;
  cp: string;
  commune: string;
  rue?: string | null;
  numero?: string | null;
  boite?: string | null;
  km?: number | null;
  startdate: string;          // yyyy-MM-dd
  enddate?: string | null;

  // audit (renvoyés par l'API)
  datecreate?: string;
  usercreate?: string | null;
  datemodif?: string | null;
  usermodif?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AdressesService {
  constructor(private http: HttpClient) { }

  list(tolkcode: number, onlyActive = true) {
    return this.http.get<Tolkadresse[]>(`/api/interpretes/${tolkcode}/adresses`, { params: { onlyActive } as any });
  }
  create(tolkcode: number, body: Partial<Tolkadresse>) {
    return this.http.post<Tolkadresse>(`/api/interpretes/${tolkcode}/adresses`, body);
  }
  getOne(id: number) {
    return this.http.get<Tolkadresse>(`/api/adresses/${id}`);
  }
  update(id: number, body: Partial<Tolkadresse>) {
    return this.http.put<void>(`/api/adresses/${id}`, body);
  }
  remove(id: number) {
    return this.http.delete<void>(`/api/adresses/${id}`);
  }
  replace(tolkcode: number, body: Partial<Tolkadresse>) {
    return this.http.post<void>(`/api/interpretes/${tolkcode}/adresses/replace`, body);
  }
}
