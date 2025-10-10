import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Observable, combineLatest, map, startWith } from 'rxjs';
import { Router } from '@angular/router';

import { CalendarService, CalendarData } from '../services/calendar.service';
import { UserService } from '../services/user.service';

@Component({
  selector: 'app-calendar',
  templateUrl: './calendar.component.html',
  styleUrls: ['./calendar.component.css']
})
export class CalendarComponent implements OnInit {
  calendarData: CalendarData[] = [];

  // Flux final pour le *ngFor
  filteredRows$!: Observable<CalendarData[]>;

  // Filtres d’en-tête
  filterForm!: FormGroup;

  // Carte de recherche “N° audience / N° affaire”
  searchForm!: FormGroup;
  showSearchCard = false;

  username = '';

  constructor(
    private calendarService: CalendarService,
    private userService: UserService,
    private fb: FormBuilder,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.buildForms();
    this.getCalendarData();
    this.getCurrentUser();
    this.addUserWindows();
  }

  private buildForms() {
    this.filterForm = this.fb.group({
      dateFrom: [''],
      dateTo: [''],
      heure: [''],
      langueRole: [''],
      langueRequete: [''],
      langueCgoe: [''],
      salle: [''],
      nom: [''],
      proc: [''],
      tolkcode: ['']
    });

    // Recherche dédiée (pas dans l’en-tête)
    this.searchForm = this.fb.group({
      nroRoleGen: [''],    // N° affaire
      idAffAudience: ['']  // N° audience
    });
  }

  getCalendarData(): void {
    this.calendarService.getCalendarData().subscribe({
      next: (data) => {
        this.calendarData = data;
        this.initFilterStream();
      },
      error: (error) => {
        console.error('Erreur lors de la récupération des données :', error);
      }
    });
  }

  private initFilterStream() {
    // Combine: filtres d’en-tête + recherche carte
    this.filteredRows$ = combineLatest([
      this.filterForm.valueChanges.pipe(startWith(this.filterForm.value)),
      this.searchForm.valueChanges.pipe(startWith(this.searchForm.value))
    ]).pipe(
      map(([f, s]) => this.applyFilters(this.calendarData, f, s))
    );
  }

  // Normalisations utilitaires
  private norm(v: any) { return (v ?? '').toString().toLowerCase().trim(); }
  private dateISO(v: any): string {
    if (!v) return '';
    if (typeof v === 'string') return v.slice(0, 10);
    if (v instanceof Date) return v.toISOString().slice(0, 10);
    try { return new Date(v).toISOString().slice(0, 10); } catch { return ''; }
  }
  private toISO(v: string) { return v ? new Date(v).toISOString().slice(0, 10) : ''; }

  private applyFilters(rows: CalendarData[], f: any, s: any): CalendarData[] {
    const fromISO = this.toISO(f.dateFrom);
    const toISOv = this.toISO(f.dateTo);

    const heure = this.norm(f.heure);
    const lr = this.norm(f.langueRole);
    const lreq = this.norm(f.langueRequete);
    const lcgoe = this.norm(f.langueCgoe);
    const salle = this.norm(f.salle);
    const nom = this.norm(f.nom);
    const proc = this.norm(f.proc);
    const tolkode = this.norm(f.tolkcode);

    const nrAffaire = this.norm(s.nroRoleGen);
    const nrAudience = this.norm(s.idAffAudience);

    return rows.filter((r: any) => {
      // Dates
      const d = this.dateISO(r.dateAudience);
      if (fromISO && d < fromISO) return false;
      if (toISOv && d > toISOv) return false;

      // Filtres d’en-tête
      if (heure && !this.norm(r.heureAudience).includes(heure)) return false;
      if (lr && !this.norm(r.langueRole).includes(lr)) return false;
      if (lreq && !this.norm(r.langueRequete).includes(lreq)) return false;
      if (lcgoe && !this.norm(r.langueCgoe).includes(lcgoe)) return false;
      if (salle && !this.norm(r.salleAudience).includes(salle)) return false;
      if (nom && !this.norm(r.nom).includes(nom)) return false;
      if (proc && !this.norm(r.proc).includes(proc)) return false;
      if (tolkode && !this.norm(r.tolkcode).includes(tolkode)) return false;

      // Recherche dédiée (carte)
      if (nrAffaire && !this.norm(r.nroRoleGen).includes(nrAffaire)) return false;
      if (nrAudience && !this.norm(r.idAffAudience).includes(nrAudience)) return false;

      return true;
    });
  }

  // Carte
  runSearch() {
    console.log('Recherche:', this.searchForm.value);
  }
  clearSearch() {
    this.searchForm.reset({ nroRoleGen: '', idAffAudience: '' });
  }

  resetFilters() {
    this.filterForm.reset({
      dateFrom: '',
      dateTo: '',
      heure: '',
      langueRole: '',
      langueRequete: '',
      langueCgoe: '',
      salle: '',
      nom: '',
      proc: '',
      tolkcode: ''
    });
  }

  getCurrentUser(): void {
    this.userService.getCurrentUser().subscribe({
      next: (data) => this.username = data.username ?? '',
      error: (error) => console.error('Erreur lors de la récupération de l’utilisateur :', error)
    });
  }

  addUserWindows(): void {
    this.userService.addUserWindows().subscribe({
      next: () => console.log('Utilisateur ajouté dans la table Test'),
      error: (error) => console.error("Erreur lors de l'ajout de l'utilisateur dans la table Test :", error)
    });
  }

  // --------- Actions ---------

  /** Libellé pour langueRole → libellé FR attendu côté Interprètes */
  private roleToLabel(role?: string | null): string | undefined {
    if (!role) return undefined;
    const r = role.toUpperCase();
    if (r === 'F') return 'Français';
    if (r === 'N') return 'Néerlandais';
    return undefined; // autre cas (on ne pré-remplit pas)
  }

  private toYmd(isoOrDate?: string | null): string | undefined {
    if (!isoOrDate) return undefined;
    return isoOrDate.substring(0, 10);
  }

  goToSearch(r: CalendarData) {
    this.router.navigate(
      ['/interpretes'], // ← route de ta page du screenshot
      {
        queryParams: {
          date: this.toYmd(r.dateAudience),
          // source = langue du requérant (rôle) -> libellé
          langSrcLbl: r.langueRequete ?? '',
          // destination = langue de la requête (déjà un libellé FR)
          langDstLbl: this.roleToLabel(r.langueRole) ?? '',
          // Contexte technique (si tu en as besoin plus tard)
          idAffAudience: r.idAffAudience,
          nroRoleGen: r.nroRoleGen
        }
      }
    );
  }

  removeAssignment(r: CalendarData) {
    if (!r.tolkcode) return;
    if (!confirm(`Supprimer l'affectation (tolkcode ${r.tolkcode}) ?`)) return;
    // TODO: brancher sur l'API de suppression quand prête
    console.log('TODO suppression affectation', {
      idAffAudience: r.idAffAudience, nroRoleGen: r.nroRoleGen, tolkcode: r.tolkcode
    });
  }
}
