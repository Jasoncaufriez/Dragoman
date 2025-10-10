import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { InterpretesService } from '../../services/interpretes.service';
import { InterpreteSearchDto } from '../../dtos/interprete-dto.model';

@Component({
  selector: 'app-interprete-list',
  templateUrl: './interprete-list.component.html'
})
export class InterpreteListComponent {
  mode: 'tolkcode' | 'nom' | 'prenom' | 'langue' = 'tolkcode';
  q = '';
  rows: InterpreteSearchDto[] = [];
  loading = false;
  error?: string;

  constructor(private api: InterpretesService, private router: Router) { }

  run(): void {
    if (!this.q.trim()) return;

    this.loading = true;
    this.error = undefined;

    this.api.search(this.mode, this.q.trim()).subscribe({
      next: (r: InterpreteSearchDto[]) => {
        this.rows = r;
        this.loading = false;
      },
      error: (_: any) => {
        this.error = 'Erreur de recherche.';
        this.loading = false;
      }
    });
  }

  openDetail(tolkcode: string): void {
    this.router.navigate(['/interpretes', tolkcode, 'detail']);
  }

  openAudiences(tolkcode: string): void {
    this.router.navigate(['/interpretes', tolkcode, 'audiences']);
  }
}
