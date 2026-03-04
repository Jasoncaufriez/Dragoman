import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AdUserStatus } from '../models/ad-status.model';

export interface AdUserCommentDto {
  samAccountName: string;
  comment: string;
}

// DTO pour le statut Normal
export interface AdUserNormalStatusDto {
  samAccountName: string;
  isNormal: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdStatusService {
  private readonly baseUrl = '/api/adstatus';

  constructor(private http: HttpClient) { }

  getAll(): Observable<AdUserStatus[]> {
    return this.http.get<AdUserStatus[]>(this.baseUrl);
  }

  saveComment(dto: AdUserCommentDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/comment`, dto);
  }

  // Méthode de sauvegarde du statut Normal
  saveNormalStatus(dto: AdUserNormalStatusDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/normalstatus`, dto);
  }
}
