import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InterpretesService } from '../services/interpretes.service';
import { AudienceDto } from '../dtos/interprete-dto.model';

@Component({
  selector: 'app-interprete-audiences',
  templateUrl: './interprete-audiences.component.html'
})
export class InterpreteAudiencesComponent implements OnInit {
  tolkcode = '';
  rows: AudienceDto[] = [];
  loading = false; error?: string;

  constructor(private route: ActivatedRoute, private api: InterpretesService) { }

  ngOnInit(): void {
    this.tolkcode = this.route.snapshot.paramMap.get('tolkcode') ?? '';
    this.load();
  }

  load() {
    if (!this.tolkcode) { this.error = 'Tolkcode manquant'; return; }
    this.loading = true; this.error = undefined;
    this.api.audiencesExact(this.tolkcode).subscribe({
      next: r => { this.rows = r; this.loading = false; },
      error: _ => { this.error = 'Erreur de chargement.'; this.loading = false; }
    });
  }
}
