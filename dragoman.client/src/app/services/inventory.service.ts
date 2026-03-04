import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MachineRecord } from '../models/machine-record';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {

  private baseUrl = '/api/inventory';

  constructor(private http: HttpClient) { }

  getAll(): Observable<MachineRecord[]> {
    return this.http.get<MachineRecord[]>(this.baseUrl);
  }

  import(file: File): Observable<MachineRecord[]> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<MachineRecord[]>(`${this.baseUrl}/import`, formData);
  }

  updateMachine(
    computerName: string,
    data: { verifiedByTeam: boolean; remark: string | null }
  ): Observable<MachineRecord> {
    return this.http.put<MachineRecord>(
      `${this.baseUrl}/${encodeURIComponent(computerName)}`,
      data
    );
  }
}
