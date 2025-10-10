import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment'

@Injectable({
  providedIn: 'root'
})
export class UserService {
 // private apiUrl = '/api/user';  // lance service user

  private apiUrl = `${environment.apiUrl}/user`;

  constructor(private http: HttpClient) { }

  getCurrentUser(): Observable<{ username: string }> {
    return this.http.get<{ username: string }>(`${this.apiUrl}/current`, { withCredentials: true });
  }

  addUserWindows(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/addUser`, {}, { withCredentials: true });
  }
}
