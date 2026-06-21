import { InjectionToken } from '@angular/core';

/** Provide this token in Angular with the API base URL, e.g. `https://localhost:5001` */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
