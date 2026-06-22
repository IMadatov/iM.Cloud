import { inject, Injectable } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { NavigationClient, NavigationItemDto } from '@im-cloud/api';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  private readonly client = inject(NavigationClient);

  private readonly itemsSubject = new BehaviorSubject<NavigationItemDto[]>([]);

  readonly items$ = this.itemsSubject.asObservable();
  readonly menuItems$ = this.items$.pipe(map((items) => toMenuItems(items)));

  load(): Observable<void> {
    return this.client.my().pipe(
      tap((items) => this.itemsSubject.next(items)),
      map(() => undefined),
    );
  }

  clear(): void {
    this.itemsSubject.next([]);
  }
}

function toMenuItems(items: NavigationItemDto[]): MenuItem[] {
  return items.map((item) => ({
    label: item.label ?? '',
    icon: item.icon,
    routerLink: item.path,
  }));
}
