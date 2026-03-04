export interface TvaRowDto {
  idTva: number;
  tolkcode: number;
  idStatut: number;
  statut: string;               // libellé à afficher
  startdate: string | null;     // ISO
  enddate: string | null;       // ISO
}

export interface StatutDto {
  idStatut: number;
  typeStatut: string;
}

export interface NewTvaDto {
  idStatut: number;
  startdate: string;            // "YYYY-MM-DD"
}
