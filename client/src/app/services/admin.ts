import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment.development';
import { AdminProduct, AdminCategory, AdminStyle } from '../models/admin.model';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // Products
  addProduct(product: AdminProduct): Observable<AdminProduct> {
    return this.http.post<AdminProduct>(`${this.apiUrl}/Product`, product);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Product/${id}`);
  }

  // Categories
  addCategory(category: AdminCategory): Observable<AdminCategory> {
    return this.http.post<AdminCategory>(`${this.apiUrl}/Category`, category);
  }

  deleteCategory(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Category/${id}`);
  }

  // Styles
  addStyle(style: AdminStyle): Observable<AdminStyle> {
    return this.http.post<AdminStyle>(`${this.apiUrl}/Style`, style);
  }

  deleteStyle(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/Style/${id}`);
  }

  // Check if user is admin
  checkIsAdmin(): Observable<boolean> {
    return this.http.get<boolean>(`${this.apiUrl}/Users/IsAdmin`);
  }

  uploadProductImages(frontImage: File, backImage: File, product: AdminProduct): Observable<any> {
    console.log('Product data:', product);
    
    const formData = new FormData();
    formData.append('frontImage', frontImage);
    formData.append('backImage', backImage);
    formData.append('name', product.name || '');
    formData.append('description', product.description || '');
    formData.append('price', product.price?.toString() || '0');
    formData.append('categoryId', product.categoryId?.toString() || '0');
    
    console.log('Sending FormData to:', `${this.apiUrl}/Product/upload`);

    return this.http.post(`${this.apiUrl}/Product/upload`, formData);
  }

  uploadCategoryImage(image: File, category: AdminCategory): Observable<any> {
    const formData = new FormData();
    formData.append('image', image);
    formData.append('name', category.name || '');
    formData.append('description', category.description || '');

    return this.http.post(`${this.apiUrl}/Category/upload`, formData);
  }

  uploadStyleImage(image: File, style: AdminStyle): Observable<any> {
    const formData = new FormData();
    formData.append('image', image);
    formData.append('name', style.name || '');
    formData.append('description', style.description || '');

    return this.http.post(`${this.apiUrl}/Style/upload`, formData);
  }
}
