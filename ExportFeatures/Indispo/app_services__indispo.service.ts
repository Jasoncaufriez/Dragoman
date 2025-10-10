import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { IndispoRowDto, NewIndispoDto } from '../dtos/indispo-dto.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class IndispoService {
  private base = '/api/interpretes';

  constructor(private http: HttpClient) { }

  list(tolkcode: number): Observable<IndispoRowDto[]> {
    return this.http.get<IndispoRowDto[]>(`${this.base}/${tolkcode}/indispo`);
  }

  add(tolkcode: number, dto: NewIndispoDto): Observable<void> {
    return this.http.post<void>(`${this.base}/${tolkcode}/indispo`, dto);
  }

  remove(tolkcode: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${tolkcode}/indispo/${id}`);
  }
}
