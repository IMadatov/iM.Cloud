import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Menubar } from 'primeng/menubar';
import { MenuItem } from 'primeng/api';

@Component({
  selector: 'app-main-layout',
  imports: [RouterOutlet, Menubar],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.scss',
})
export class MainLayoutComponent {
  readonly menuItems: MenuItem[] = [
    {
      label: 'Home',
      icon: 'pi pi-home',
      routerLink: '/',
    },
  ];
}
