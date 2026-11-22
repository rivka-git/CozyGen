import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../environments/environment.development';
import { AuthResponse, UserLogin, UserRegister, UserResponse, UserUpdate } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/Users`;
  
  private currentUserSubject = new BehaviorSubject<UserResponse | null>(this.getUserFromStorage());
  public currentUser$ = this.currentUserSubject.asObservable();
  
  private isAdminSubject = new BehaviorSubject<boolean>(this.checkInitialAdminStatus());
  public isAdmin$ = this.isAdminSubject.asObservable();

  private getUserFromStorage(): UserResponse | null {
    const user = localStorage.getItem('currentUser');
    return user ? JSON.parse(user) : null;
  }

  private checkInitialAdminStatus(): boolean {
    const adminStatus = localStorage.getItem('isAdmin');
    if (adminStatus === 'true') return true;
    
    const user = this.getUserFromStorage();
    return user?.role === 'Admin';
  }

  // The server returns the user plus a JWT and also sets the JWT as an HttpOnly cookie.
  // Only the public user details are kept in localStorage (never the password or the token).
  login(credentials: UserLogin): Observable<UserResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/Login`, credentials).pipe(
      map(res => res.user),
      tap(user => this.setCurrentUser(user))
    );
  }

  register(userData: UserRegister): Observable<UserResponse> {
    return this.http.post<AuthResponse>(this.apiUrl, userData).pipe(
      map(res => res.user),
      tap(user => this.setCurrentUser(user))
    );
  }

  logout(): void {
    // Ask the server to clear the auth cookie; local state is cleared either way.
    this.http.post(`${this.apiUrl}/Logout`, {}).subscribe({ error: () => {} });
    localStorage.removeItem('currentUser');
    localStorage.removeItem('isAdmin');
    this.currentUserSubject.next(null);
    this.isAdminSubject.next(false);
  }

  private setCurrentUser(user: UserResponse): void {
    localStorage.setItem('currentUser', JSON.stringify(user));
    this.currentUserSubject.next(user);
    const isAdmin = user.role === 'Admin';
    this.isAdminSubject.next(isAdmin);
    localStorage.setItem('isAdmin', String(isAdmin));
  }

  isLoggedIn(): boolean {
    return !!this.currentUserSubject.value;
  }

  getCurrentUser(): UserResponse | null {
    return this.currentUserSubject.value;
  }

  checkAdminStatus(): void {
    const user = this.getCurrentUser();
    if (user) {
      this.http.get<any>(`${this.apiUrl}/${user.userId}`).subscribe(
        fullUser => {
          const isAdmin = fullUser.role === 'Admin';
          this.isAdminSubject.next(isAdmin);
          localStorage.setItem('isAdmin', String(isAdmin));
          console.log('Admin status:', isAdmin, 'Role:', fullUser.role);
        },
        (error) => {
          console.warn('Admin check failed, defaulting to false', error);
          this.isAdminSubject.next(false);
          localStorage.removeItem('isAdmin');
        }
      );
    } else {
      this.isAdminSubject.next(false);
      localStorage.removeItem('isAdmin');
    }
  }

  isAdmin(): boolean {
    return this.isAdminSubject.value;
  }

  updateUser(userId: string, userData: UserUpdate): Observable<UserResponse> {
    return this.http.put<UserResponse>(`${this.apiUrl}/${userId}`, userData).pipe(
      tap(updatedUser => {
        localStorage.setItem('currentUser', JSON.stringify(updatedUser));
        this.currentUserSubject.next(updatedUser);
      })
    );
  }

  getUserDetails(userId: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${userId}`);
  }
}
