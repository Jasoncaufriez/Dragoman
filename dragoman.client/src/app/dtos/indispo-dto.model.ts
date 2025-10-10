export interface IndispoRowDto {
  idIndispo: number;
  tolkcode: string;
  startindispo: string;      // "YYYY-MM-DD"
  endindispo?: string | null;
  motifindispo?: string | null;
  commentaire?: string | null;


}

export interface NewIndispoDto {
  startindispo: string;       // requis
  endindispo?: string | null; // optionnel => période ouverte
  motifindispo?: string | null;
  commentaire?: string | null;

  
  createuser?: string | null;
  
}
