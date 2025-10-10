import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';


@Component({
  selector: 'app-hd',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './hd.component.html',
  styleUrls: ['./hd.component.css']
})
export class HDComponent { }
