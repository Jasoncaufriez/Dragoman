import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-navbarinter',
  templateUrl: './navbarinter.component.html',
  styleUrls: ['./navbarinter.component.css']
})
export class NavbarInterComponent implements OnInit {
  @Input() tolkcode?: number;

  constructor(private route: ActivatedRoute) { }

  ngOnInit(): void {
    // Si l'input n'est pas fourni, on le récupère depuis l'URL : /interpretes/:tolkcode/...
    if (this.tolkcode == null) {
      const p = this.route.snapshot.paramMap.get('tolkcode');
      this.tolkcode = p ? Number(p) : undefined;
    }
  }
}
