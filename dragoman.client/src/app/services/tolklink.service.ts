import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class TolklinkService {
  private base = '/api/interpretes';

  constructor(private http: HttpClient) { }

  addOne(tolkcode: number, idAffAudience: number): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(`${this.base}/${tolkcode}/tolklink`, { nrAffAudience: idAffAudience });
  }

  addBulk(tolkcode: number, ids: number[]): Observable<{ inserted: number, skipped: number }> {
    return this.http.post<{ inserted: number, skipped: number }>(`${this.base}/${tolkcode}/tolklink/bulk`, { ids });
  }
}
