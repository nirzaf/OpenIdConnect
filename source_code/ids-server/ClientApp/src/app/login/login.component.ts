import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent implements OnInit {
  username: string;
  password: string;
  returnUrl: string;
  error: string;

  constructor(
    private http: HttpClient,
    private router: ActivatedRoute,
    private route: Router
  ) {}

  ngOnInit(): void {
    this.returnUrl = this.router.snapshot.queryParams['ReturnUrl'];
  }

  login() {
    this.error = '';
    this.http
      .post('auth/login', {
        username: this.username,
        password: this.password,
        rememberLogin: false,
        returnUrl: this.returnUrl,
      })
      .subscribe(
        (rsp) => {
          window.location.href = (rsp as any).returnUrl;
        },
        (error) => {
          if (error.status === 401) {
            if (error.error?.require2fa) {
              this.route.navigate(['/mfa'], {
                queryParams: {
                  ReturnUrl: error.error.returnUrl,
                  RememberMe: error.error.rememberMe,
                },
              });
            }
          } else {
            console.log('error', error);
            this.error = `Login failed!`;
          }
        }
      );
  }
}
