import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';

/**
 * Sends the HttpOnly "auth_token" cookie (JWT) with every request to our API.
 * The token itself is never readable from JavaScript, which protects it from XSS.
 */
@Injectable()
export class CredentialsInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (req.url.startsWith(environment.apiUrl)) {
      req = req.clone({ withCredentials: true });
    }
    return next.handle(req);
  }
}
