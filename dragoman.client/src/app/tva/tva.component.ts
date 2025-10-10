import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TvaService } from '../services/tva.service';
import { TvaRowDto, StatutDto, NewTvaDto } from '../dtos/tva-dto.model';

@Component({
  selector: 'app-tva',
  templateUrl: './tva.component.html',
  styleUrls: ['./tva.component.css']
})
export class TvaComponent implements OnInit {
  tolkcode = 0;
  tvas: TvaRowDto[] = [];
  statuts: StatutDto[] = [];

  loading = false;
  saving = false;
  error?: string;

  form!: FormGroup;
  showForm = false;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private tvaService: TvaService
  ) { }

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('tolkcode');
    this.tolkcode = raw ? Number(raw) : NaN;
    if (Number.isNaN(this.tolkcode)) {
      this.error = 'Paramètre tolkcode manquant dans l’URL.';
      return;
    }
    this.buildForm();
    this.load();
  }

  private buildForm() {
    this.form = this.fb.group({
      idStatut: [null, Validators.required],
      startdate: [this.todayISO(), Validators.required]
    });
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

    this.tvaService.getStatuts().subscribe({ next: s => this.statuts = s });

    this.tvaService.getTvaList(this.tolkcode).subscribe({
      next: rows => { this.tvas = rows; this.loading = false; },
      error: () => { this.error = 'Erreur lors du chargement des statuts TVA.'; this.loading = false; }
    });
  }

  addTva() {
    if (this.form.invalid || this.saving) return;
    this.saving = true;

    const payload = this.form.value as NewTvaDto;
    this.tvaService.addTva(this.tolkcode, payload).subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.form.reset({ idStatut: null, startdate: this.todayISO() });
        this.load();
      },
      error: () => {
        this.saving = false;
        this.error = 'Erreur lors de l’enregistrement.';
      }
    });
  }
}
