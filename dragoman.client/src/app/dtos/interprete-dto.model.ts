export interface InterpreteSearchDto {
  tolkcode: string;
  nom?: string;
  prenom?: string;
  languesDestination: string[];
  languesSource: string[];
}

export interface AudienceDto {
  nroRoleGen: number;
  langueRole: string;
  proc: string;
  dateAudience: string;
  nom: string;
  salleAudience: string;
  heureAudience: string;
  langueRequete: string;
  libelleFr: string;
  langueCgoe: string;
  idAffAudience: number;
  tolkcode?: number | null;
}

// Utilisé par /api/interpretes/match
export interface InterpreteMatchDto {
  tolkcode: number;
  nom?: string;
  prenom?: string;
  tel?: string;
  telbis?: string;
  gsm?: string;
  languesDestination: string[];
  distanceKm?: number | null;
}
