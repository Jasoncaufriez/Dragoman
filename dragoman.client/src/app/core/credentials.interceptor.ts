import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class CredentialsInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Envoie les cookies/NTLM pour les routes backend
    if (req.url.startsWith('/api/') || req.url.includes('://rvv-ccesrv21/')) {
      req = req.clone({ withCredentials: true });
    }
    return next.handle(req);
  }
}
