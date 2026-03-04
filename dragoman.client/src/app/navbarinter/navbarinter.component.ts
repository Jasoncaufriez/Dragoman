import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { InterpretesService } from '../services/interpretes.service';

@Component({
  selector: 'app-navbarinter',
  templateUrl: './navbarinter.component.html',
  styleUrls: ['./navbarinter.component.css']
})
export class NavbarInterComponent implements OnInit {
  @Input() tolkcode?: number;

  nom = '';
  prenom = '';
  email = '';
  telephone = '';
  languesSrc: string[] = [];
  languesDst: string[] = [];

  get initiales(): string {
    const p = this.prenom?.[0]?.toUpperCase() ?? '';
    const n = this.nom?.[0]?.toUpperCase() ?? '';
    return (p + n) || '#';
  }

  get telTeamsUrl(): string {
    const clean = this.telephone.replace(/[^\d+]/g, '');
    return `msteams://call?phone=${clean}`;
  }

  constructor(
    private route: ActivatedRoute,
    private service: InterpretesService
  ) { }

  ngOnInit(): void {
    if (this.tolkcode == null) {
      const p = this.route.snapshot.paramMap.get('tolkcode');
      this.tolkcode = p ? Number(p) : undefined;
    }
    if (this.tolkcode) {
      this.loadInfo();
    }
  }

  private loadInfo(): void {
    this.service.getIdentite(this.tolkcode!).subscribe({
      next: (data: any) => {
        this.nom    = data.nom    ?? data.Nom    ?? '';
        this.prenom = data.prenom ?? data.Prenom ?? '';
        this.email  = data.email  ?? data.Email  ?? '';
        const gsm   = data.gsm    ?? data.Gsm    ?? '';
        const tel   = data.tel    ?? data.Tel    ?? '';
        this.telephone = gsm || tel || '';
      },
      error: () => { /* silencieux — la navbar reste fonctionnelle */ }
    });

    this.service.listLangSource(this.tolkcode!).subscribe({
      next: (r: any) => {
        this.languesSrc = (r as any[]).map(l => l.codeIso ?? l.libelleFr ?? '').filter(Boolean);
      },
      error: () => {}
    });

    this.service.listLangDest(this.tolkcode!).subscribe({
      next: (r: any) => {
        this.languesDst = (r as any[]).map(l => l.codeIso ?? l.libelleFr ?? '').filter(Boolean);
      },
      error: () => {}
    });
  }
}

