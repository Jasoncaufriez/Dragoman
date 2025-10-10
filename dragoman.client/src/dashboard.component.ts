
import { Component, OnInit } from '@angular/core';
import { DashboardService } from './app/services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  resume: any = {};
  interpretesNonEncodes: any[] = [];
  audiencesSupprimees: any[] = [];

  constructor(private dashboardService: DashboardService) { }

  ngOnInit() {
    this.dashboardService.getResume().subscribe(data => this.resume = data);
    this.dashboardService.getInterpretesNonEncodes().subscribe(data => this.interpretesNonEncodes = data);
    this.dashboardService.getAudiencesSupprimees().subscribe(data => this.audiencesSupprimees = data);
  }
}
