import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PrestationsService, PrestationJourRowDto, NewPrestationDto } from '../services/prestations.service';

@Component({
  selector: 'app-prestations',
  templateUrl: './prestations.component.html',
  styleUrls: ['./prestations.component.css']
})
export class PrestationsComponent implements OnInit {
  date = this.todayISO();
  rows: PrestationJourRowDto[] = [];
  loading = false;
  error?: string;
  saving = false;

  form!: FormGroup;
  selected?: PrestationJourRowDto;

  constructor(
    private api: PrestationsService,
    private fb: FormBuilder
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      start: ['', Validators.required],
      end: ['', Validators.required]
    });
    this.load();
  }

  todayISO(): string {
    const d = new Date();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${m}-${dd}`;
  }

  load(): void {
    this.loading = true;
    this.error = undefined;
    this.api.liste(this.date).subscribe({
      next: r => {
        this.rows = r;
        this.loading = false;
      },
      error: () => {
        this.error = 'Impossible de charger les données.';
        this.loading = false;
      }
    });
  }

  pick(row: PrestationJourRowDto): void {
    this.selected = row;
    // Pré-remplir avec l'heure suggérée si disponible
    if (row.heureAudienceSuggee) {
      const suggested = this.subtractMinutes(row.heureAudienceSuggee, 15);
      this.form.patchValue({ start: suggested });
    }
  }

  save(): void {
    if (!this.selected || this.form.invalid || this.saving) return;

    this.saving = true;
    const start = this.form.value.start as string;
    const end = this.form.value.end as string;

    const dto: NewPrestationDto = {
      tolkcode: this.selected.tolkcode,
      datePrestation: this.date,
      startheure: `${this.date}T${start}:00`,
      endheure: `${this.date}T${end}:00`,
      idAffAudiences: this.selected.idAffAudiences
    };

    this.api.create(dto).subscribe({
      next: () => {
        this.saving = false;
        this.selected = undefined;
        this.form.reset();
        this.load();
      },
      error: (e) => {
        this.saving = false;
        this.error = e?.error || 'Échec de l\'enregistrement.';
      }
    });
  }

  absent(row: PrestationJourRowDto): void {
    if (!confirm(`Marquer ${row.nom} ${row.prenom} absent(e) ?`)) return;

    const payload = {
      tolkcode: row.tolkcode,
      idAffAudience: row.idAffAudiences[0],
      datePrestation: this.date
    };

    this.api.absence(payload).subscribe({
      next: () => this.load(),
      error: () => (this.error = 'Impossible de marquer l\'absence.')
    });
  }

  trackByTolkcode(index: number, row: PrestationJourRowDto): string {
    return row.tolkcode;
  }

  private subtractMinutes(timeStr: string, minutes: number): string {
    const [h, m] = timeStr.split(':').map(Number);
    const totalMinutes = h * 60 + m - minutes;
    const newH = Math.floor(totalMinutes / 60);
    const newM = totalMinutes % 60;
    return `${String(newH).padStart(2, '0')}:${String(newM).padStart(2, '0')}`;
  }
}
