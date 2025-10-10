import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { IndispoService } from '../services/indispo.service';
import { IndispoRowDto, NewIndispoDto } from '../dtos/indispo-dto.model';

@Component({
  selector: 'app-indispo',
  templateUrl: './indispo.component.html',
  styleUrls: ['./indispo.component.css']
})
export class IndispoComponent implements OnInit {
  tolkcode = 0;
  rows: IndispoRowDto[] = [];

  form!: FormGroup;
  showForm = false;

  loading = false;
  saving = false;
  error?: string;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private api: IndispoService
  ) { }

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('tolkcode');
    this.tolkcode = raw ? Number(raw) : NaN;
    if (Number.isNaN(this.tolkcode)) {
      this.error = 'Paramètre tolkcode manquant.';
      return;
    }
    this.buildForm();
    this.load();
  }

  private buildForm() {
    this.form = this.fb.group({
      startindispo: [this.todayISO(), Validators.required],
      endindispo: [''], // optionnel
      motifindispo: [''],
      commentaire: ['']
    }, { validators: [this.endAfterStart] });
  }

  private endAfterStart(group: AbstractControl) {
    const s = (group.get('startindispo')?.value ?? '').toString();
    const e = (group.get('endindispo')?.value ?? '').toString();
    if (!s || !e) return null; // si end vide => OK (période ouverte)
    return e >= s ? null : { range: 'end < start' };
  }

  private todayISO(): string {
    const d = new Date();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${d.getFullYear()}-${m}-${day}`;
  }

  load() {
    this.loading = true;
    this.error = undefined;
    this.api.list(this.tolkcode).subscribe({
      next: r => { this.rows = r; this.loading = false; },
      error: () => { this.error = 'Erreur de chargement.'; this.loading = false; }
    });
  }

  add() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;

    const payload = this.form.value as NewIndispoDto;
    // (endindispo '' => back la traitera comme NULL)
    if (payload.endindispo === '') payload.endindispo = null;

    this.api.add(this.tolkcode, payload).subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.form.reset({ startindispo: this.todayISO(), endindispo: '', motifindispo: '', commentaire: '', });
        this.load();
      },
      error: err => {
        this.saving = false;
        this.error = err?.error ?? 'Erreur lors de l’enregistrement.';
      }
    });
  }

  remove(id: number) {
    if (!confirm('Supprimer cette indisponibilité ?')) return;
    this.api.remove(this.tolkcode, id).subscribe({
      next: () => this.load(),
      error: () => this.error = 'Suppression impossible.'
    });
  }
}
