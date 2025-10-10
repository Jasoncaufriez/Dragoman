// src/app/services/reports.service.ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ReportsService {
  constructor(private http: HttpClient) { }

  getInterpretes(dateISO: string) {
    const params = new HttpParams().set('date', dateISO);
    return this.http.get<any[]>('/api/reports/interpretes', { params });
  }

  downloadExcel(dateISO: string) {
    const params = new HttpParams().set('date', dateISO);
    return this.http.get('/api/reports/interpretes/excel', { params, responseType: 'blob' });
  }

  downloadWord(dateISO: string) {
    const params = new HttpParams().set('date', dateISO);
    return this.http.get('/api/reports/interpretes/word', { params, responseType: 'blob' });
  }
}
