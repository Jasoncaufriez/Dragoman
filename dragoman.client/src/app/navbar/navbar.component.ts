import { Component, OnInit } from '@angular/core';
import { AuthentificationService } from '../services/authentification.service';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent implements OnInit {
  user: string = 'anonymous';

  constructor(private authService: AuthentificationService) { }

  ngOnInit(): void {
    this.authService.getLogin().subscribe(name => {
      this.user = name;
    });
  }
}
