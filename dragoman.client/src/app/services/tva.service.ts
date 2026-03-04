import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TvaRowDto, StatutDto, NewTvaDto } from '../dtos/tva-dto.model';

@Injectable({ providedIn: 'root' })
export class TvaService {
  private baseUrl = '/api';

  constructor(private http: HttpClient) { }

  getTvaList(tolkcode: number): Observable<TvaRowDto[]> {
    return this.http.get<TvaRowDto[]>(`${this.baseUrl}/interpretes/${tolkcode}/tva`);
  }

  getStatuts(): Observable<StatutDto[]> {
    return this.http.get<StatutDto[]>(`${this.baseUrl}/tva/statuts`);
  }

  addTva(tolkcode: number, payload: NewTvaDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/interpretes/${tolkcode}/tva`, payload);
  }
}
