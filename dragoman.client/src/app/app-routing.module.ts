// app-routing.module.ts
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CalendarComponent } from './calendar/calendar.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InterpreteListComponent } from './interpretes/interprete-list/interprete-list.component';
import { InterpreteDetailComponent } from './interpretes/interprete-detail/interprete-detail.component';
import { AdressesComponent } from './adresses/adresses.component';
import { LanguesComponent } from './langues/langues.component';
import { TvaComponent } from './tva/tva.component';
import { IndispoComponent } from './indispo/indispo.component';
import { InterpreteAudiencesComponent } from './interprete-audiences/interprete-audiences.component';
import { PresenceInterpretesComponent } from './presence-interpretes/presence-interpretes.component';
import { PrestationsComponent } from './prestations/prestations.component';
import { HDComponent } from './hd/hd/hd.component';

const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Routes existantes
  { path: 'dashboard', component: DashboardComponent },
  { path: 'calendar', component: CalendarComponent },
  { path: 'interpretes', component: InterpreteListComponent },
  { path: 'interpretes/:tolkcode/detail', component: InterpreteDetailComponent },
  { path: 'interpretes/:tolkcode/audiences', component: InterpreteAudiencesComponent },
  { path: 'interpretes/:tolkcode/adresses', component: AdressesComponent },
  { path: 'interpretes/:tolkcode/langues', component: LanguesComponent },
  { path: 'interpretes/:tolkcode/tva', component: TvaComponent },
  { path: 'interpretes/:tolkcode/indispo', component: IndispoComponent },
  { path: 'presence-interpretes', component: PresenceInterpretesComponent },
  { path: 'prestations', component: PrestationsComponent },
  { path: 'hd', component: HDComponent },
  // 🔹 Routes Helpdesk (standalone via loadComponent)
  {
    path: 'hd/fiche-jour',
    loadComponent: () =>
      import('./hd/hd-prestation-jour/hd-prestation-jour.component')
        .then(m => m.HDPrestationJourComponent)
  },
  {
    path: 'hd/recap-semaine',
    loadComponent: () =>
      import('./hd/hd-recap-semaine/hd-recap-semaine.component')
        .then(m => m.HDRecapSemaineComponent)
  },

  // ⚠️ Wildcard en DERNIER
  { path: '**', redirectTo: 'dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes, {
    scrollPositionRestoration: 'enabled',
    // enableTracing: true, // utile en debug
  })],
  exports: [RouterModule]
})
export class AppRoutingModule { }
