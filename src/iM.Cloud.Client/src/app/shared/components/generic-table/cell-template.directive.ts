import { Directive, inject, input, TemplateRef } from '@angular/core';

@Directive({
  selector: 'ng-template[imCellTemplate]',
  standalone: true,
})
export class CellTemplateDirective {
  readonly imCellTemplate = input.required<string>({ alias: 'imCellTemplate' });
  readonly templateRef = inject(TemplateRef<unknown>);
}
