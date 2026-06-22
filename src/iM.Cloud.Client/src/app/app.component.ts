import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Toast],
  template: `
    <p-toast position="top-right" />
    <router-outlet />
  `,
  styleUrl: './app.component.scss',
})
export class AppComponent {}
