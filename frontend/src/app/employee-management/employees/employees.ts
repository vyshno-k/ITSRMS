import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService, LoggedInUser } from '../services/auth.service';

export interface Employee { id:number; employeeCode:string; firstName:string; lastName:string; email:string; phone:string; department:string; designation:string; dateOfJoining:string; isActive:boolean; }

@Component({selector:'app-employees',standalone:true,imports:[CommonModule,FormsModule],templateUrl:'./employees.html',styleUrl:'./employees.css'})
export class Employees implements OnInit {
  private readonly http=inject(HttpClient); private readonly router=inject(Router); private readonly auth=inject(AuthService); private readonly changeDetector=inject(ChangeDetectorRef);
  readonly apiUrl='http://localhost:5103/api/employees';
  employees:Employee[]=[]; filteredEmployees:Employee[]=[]; searchText=''; loading=true; errorMessage=''; toastMessage=''; toastType:'success'|'error'='success'; user:LoggedInUser|null=null;
  pageSize=10; currentPage=1; deletingId:number|null=null;
  ngOnInit():void{this.user=this.auth.getUser();if(!this.user){this.router.navigate(['/login']);return;}if(!this.auth.isAdmin()){this.router.navigate(['/dashboard']);return;}this.loadEmployees();}
  get activeEmployeeCount():number{ return this.employees.filter(e=>e.isActive).length; }
  get departmentCount():number{ return new Set(this.employees.map(e=>(e.department||'').trim()).filter(Boolean)).size; }
  loadEmployees():void{this.errorMessage='';this.loading=true;this.http.get<Employee[]>(this.apiUrl).subscribe({next:data=>{this.employees=data||[];this.filter();this.loading=false;this.changeDetector.detectChanges();},error:err=>{this.loading=false;this.showToast((err.error?.message||'Unable to refresh employees.').replace(/^/,''),'error');this.changeDetector.detectChanges();}});}
  filter():void{const q=this.searchText.trim().toLowerCase();this.filteredEmployees=!q?[...this.employees]:this.employees.filter(e=>[e.employeeCode,e.firstName,e.lastName,e.email,e.department,e.designation].some(v=>(v||'').toLowerCase().includes(q)));this.currentPage=1;}
  get totalPages():number{return Math.max(1,Math.ceil(this.filteredEmployees.length/this.pageSize));}
  get pagedEmployees():Employee[]{const start=(this.currentPage-1)*this.pageSize;return this.filteredEmployees.slice(start,start+this.pageSize);}
  get rangeStart():number{return this.filteredEmployees.length ? (this.currentPage-1)*this.pageSize+1 : 0;}
  get rangeEnd():number{return Math.min(this.currentPage*this.pageSize,this.filteredEmployees.length);}
  goToPage(page:number):void{this.currentPage=Math.max(1,Math.min(page,this.totalPages));}
  addEmployee():void{this.router.navigate(['/employees/add']);}
  editEmployee(employee:Employee):void{this.router.navigate(['/employees/edit', employee.id]);}
  logout():void{this.auth.logout();this.router.navigate(['/login']);}
  deleteEmployee(employee:Employee):void{if(this.deletingId!==null)return;this.deletingId=employee.id;this.http.delete(`${this.apiUrl}/${employee.id}`).subscribe({next:()=>{this.deletingId=null;this.showToast('Employee deleted successfully');this.changeDetector.detectChanges();this.loadEmployees();},error:err=>{this.deletingId=null;this.showToast(err.error?.message||'Unable to delete employee','error');this.changeDetector.detectChanges();}});}
  showToast(message:string,type:'success'|'error'='success'):void{this.toastMessage=message;this.toastType=type;setTimeout(()=>this.toastMessage='',2600);}
}
