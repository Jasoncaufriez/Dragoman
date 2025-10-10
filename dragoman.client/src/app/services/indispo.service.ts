import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { switchMap } from 'rxjs/operators'; // ✅ import opérateur
import { IndispoRowDto, NewIndispoDto } from '../dtos/indispo-dto.model';
import { AuthentificationService } from './authentification.service';

@Injectable({ providedIn: 'root' })
export class IndispoService {
  private base = '/api/interpretes';

  constructor(
    private http: HttpClient,
    private auth: AuthentificationService // ✅ injection du service d’auth
  ) { }

  list(tolkcode: number): Observable<IndispoRowDto[]> {
    return this.http.get<IndispoRowDto[]>(`${this.base}/${tolkcode}/indispo`);
  }

  add(tolkcode: number, dto: NewIndispoDto): Observable<void> {
    return this.auth.getLogin().pipe(
      switchMap((login: string) => {
        const user = login || 'anonymous';
        const payload = {
          ...dto,
          createuser: user,
          modifuser: user
        } as any;
        return this.http.post<void>(`${this.base}/${tolkcode}/indispo`, payload);
      })
    );
  }

  remove(tolkcode: number, id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${tolkcode}/indispo/${id}`);
  }
}
