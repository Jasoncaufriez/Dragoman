import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

// Modèle de données (facultatif) pour correspondre à la structure de ton JSON
export interface CalendarData {
  nroRoleGen: number;
  langueRole: string;
  proc: string;
  dateAudience: string;
  nom: string;
  salleAudience: string;
  heureAudience: string;
  langueRequete: string;
  libelleFr: string;
  langueCgoe: string;
  idAffAudience: number;
  tolkcode: number;
}


 
  import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private apiUrl = `${environment.apiUrl}/calendar`;

  constructor(private http: HttpClient) { }

  getCalendarData(): Observable<CalendarData[]> {
    return this.http.get<CalendarData[]>(this.apiUrl, { withCredentials: true });
  }
}

