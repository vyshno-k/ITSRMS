import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-register', standalone: true, imports: [CommonModule, FormsModule],
  templateUrl: './register.html', styleUrls: ['./register.css']
})
export class Register {
  private readonly auth = inject(AuthService); private readonly router = inject(Router);
  username=''; fullName=''; email=''; password=''; department=''; role='Employee';
  loading=false; success=''; error='';

  register(): void {
    this.success=''; this.error='';
    if(this.username.trim().length<3){this.error='Username must be at least 3 characters.';return;}
    if(!this.fullName.trim()){this.error='Full name is required.';return;}
    if(!/^\S+@\S+\.\S+$/.test(this.email.trim())){this.error='Please enter a valid email address.';return;}
    if(this.password.length<6){this.error='Password must be at least 6 characters.';return;}
    if(!this.department.trim()){this.error='Department is required.';return;}
    if(this.loading)return; this.loading=true;
    this.auth.register({username:this.username.trim(),fullName:this.fullName.trim(),email:this.email.trim(),password:this.password,department:this.department.trim(),role:this.role}).subscribe({
      next:(response)=>{this.loading=false;this.success='Account successfully created.';this.username='';this.fullName='';this.email='';this.password='';this.department='';setTimeout(()=>this.router.navigate(['/login']),1500);},
      error:(err:HttpErrorResponse)=>{this.loading=false;this.error=err.error?.message || (err.status===0?'Cannot connect to backend. Start the .NET API on port 5103.':'Account creation failed.');}
    });
  }
  goToLogin():void{this.router.navigate(['/login']);}
}
