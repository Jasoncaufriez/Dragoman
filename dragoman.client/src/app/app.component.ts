import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

interface WeatherForecast {
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string;
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'] // ✅ corrige styleUrl -> styleUrls
})
export class AppComponent implements OnInit {
  public forecasts: WeatherForecast[] = [];
  title = 'dragoman.client';

  // ✅ router en public pour pouvoir l'utiliser dans le template
  constructor(public router: Router, private http: HttpClient) { }

  ngOnInit(): void {
    this.getForecasts();
  }

  getForecasts(): void {
    this.http.get<WeatherForecast[]>('/weatherforecast').subscribe({
      next: (result) => (this.forecasts = result),
      error: (error) => console.error(error)
    });
  }
}
