import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Toast } from 'primeng/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast, ConfirmDialog],
  template: `
    <p-toast position="top-right" />
    <p-confirmdialog />
    <router-outlet />
  `,
  styleUrl: './app.component.scss',
})
export class AppComponent {}
