import { AsyncPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Button } from 'primeng/button';
import { Menubar } from 'primeng/menubar';
import { AuthService } from '../../core/auth/auth.service';
import { NavigationService } from '../../core/navigation/navigation.service';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Menubar, Button, AsyncPipe],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {
  readonly auth = inject(AuthService);
  readonly navigation = inject(NavigationService);

  readonly menuItems$ = this.navigation.menuItems$;

  logout(): void {
    this.auth.logout();
  }
}
