import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

import { CalendarComponent } from './calendar/calendar.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { InterpreteListComponent } from './interpretes/interprete-list/interprete-list.component';
import { InterpreteDetailComponent } from './interpretes/interprete-detail/interprete-detail.component';
import { AdressesComponent } from './adresses/adresses.component';
import { LanguesComponent } from './langues/langues.component';
import { TvaComponent } from './tva/tva.component';
import { IndispoComponent } from './indispo/indispo.component';
import { InterpreteAudiencesComponent } from './interprete-audiences/interprete-audiences.component';
import { ConvocationComponent } from './convocation/convocation.component';
import { NavbarInterComponent } from './navbarinter/navbarinter.component';
import { NavbarComponent } from './navbar/navbar.component';
import { PresenceInterpretesComponent } from './presence-interpretes/presence-interpretes.component';
import { PrestationsComponent } from './prestations/prestations.component';
import { InventoryComponent } from './inventory/inventory.component';
import { AdStatusDashboardComponent } from './ad-status/ad-status-dashboard/ad-status-dashboard.component';
import { FacturesComponent } from './factures/factures.component';
import { GenerationFacturesComponent } from './generation-factures/generation-factures.component';

@NgModule({
  declarations: [
    AppComponent,
    CalendarComponent,
    DashboardComponent,
    InterpreteListComponent,
    InterpreteDetailComponent,
    AdressesComponent,
    LanguesComponent,
    TvaComponent,
    IndispoComponent,
    InterpreteAudiencesComponent,
    ConvocationComponent,
    NavbarInterComponent,
    NavbarComponent,
    PresenceInterpretesComponent,
    PrestationsComponent,
    InventoryComponent,
    AdStatusDashboardComponent,
    FacturesComponent,
    GenerationFacturesComponent
  ],
  imports: [
    BrowserModule,
    RouterModule,
    AppRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    HttpClientModule
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
