import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-mfa',
  templateUrl: './mfa.component.html',
  styleUrls: ['./mfa.component.scss'],
})
export class MfaComponent implements OnInit {
  twoFactorCode: string;
  returnUrl: string;
  error: string;
  rememberMe: any;

  constructor(
    private http: HttpClient,
    private router: ActivatedRoute,
    private route: Router
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.router.snapshot.queryParams['ReturnUrl'];
    this.rememberMe = this.router.snapshot.queryParams['RememberMe'];
  }

  login() {
    this.error = '';
    this.http
      .post('auth/LoginWith2fa', {
        twoFactorCode: this.twoFactorCode,
        rememberMe: !!this.rememberMe,
        // todo: map this
        rememberMachine: false,
        returnUrl: this.returnUrl,
      })
      .subscribe(
        (rsp) => {
          window.location.href = (rsp as any).returnUrl;
        },
        (error) => {
          this.error = `Login failed!`;
        }
      );
  }
}
