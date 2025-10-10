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
import { NavbarInterComponent } from './navbarinter/navbarinter.component';
import { NavbarComponent } from './navbar/navbar.component';
import { PresenceInterpretesComponent } from './presence-interpretes/presence-interpretes.component';
import { PrestationsComponent } from './prestations/prestations.component';



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
    NavbarInterComponent,
    NavbarComponent,
    PresenceInterpretesComponent,
    PrestationsComponent
    
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
