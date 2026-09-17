import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Router, NavigationEnd } from '@angular/router';
import { RouterLink } from '@angular/router';
import { DatabricksService } from '../../services/databricks.service';
import { timeout, filter, Subject, takeUntil, timer } from 'rxjs';
import { AuthService } from '../../employee-management/services/auth.service';

interface DashboardData { total:number; open:number; inProgress:number; resolved:number; closed:number; slaBreached:number; byPriority:{name:string,count:number}[]; byCategory:{name:string,count:number}[]; engineerWorkload:{id:number,name:string,isActive:boolean,count:number}[]; }
@Component({ selector:'app-dashboard', standalone:true, imports:[CommonModule,FormsModule,RouterLink], templateUrl:'./dashboard.component.html', styleUrl:'./dashboard.component.scss' })
export class DashboardComponent implements OnInit, OnDestroy {
  from = ''; to = ''; loading = false; error = ''; data: DashboardData = { total:0,open:0,inProgress:0,resolved:0,closed:0,slaBreached:0,byPriority:[],byCategory:[],engineerWorkload:[] };
  databricksLoading = false;
  databricksError = '';
  databricksResult: any = null;
  private readonly destroy$ = new Subject<void>();

  constructor(private http: HttpClient, private databricks: DatabricksService, private auth: AuthService, private router: Router, private changeDetector: ChangeDetectorRef) {}
  get isAdmin(): boolean { return this.auth.isAdmin(); }
  ngOnInit(): void {
    this.load();
    timer(30000, 30000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => this.load());
    this.router.events
      .pipe(filter(event => event instanceof NavigationEnd), takeUntil(this.destroy$))
      .subscribe((event: NavigationEnd) => {
        const url = event.urlAfterRedirects || event.url;
        if (url.includes('/admin') || url.includes('/dashboard')) {
          this.load();
        }
      });
  }
  load(): void { this.loading=true; this.error=''; let params=new HttpParams(); if(this.from) params=params.set('from',this.from); if(this.to) params=params.set('to',this.to); this.http.get<DashboardData>('http://localhost:5103/api/dashboard',{params}).pipe(timeout(15000),takeUntil(this.destroy$)).subscribe({next:x=>{this.data=x;this.loading=false;this.changeDetector.detectChanges();},error:e=>{this.loading=false;this.error=e?.error?.error||e?.error||'Dashboard could not be loaded. Check that the API is running.';this.changeDetector.detectChanges();}}); }
  clear(): void { this.from=''; this.to=''; this.load(); }
  barWidth(count: number, items: { count: number }[]): number { const max = Math.max(...items.map(item => item.count), 1); return Math.max(8, Math.round((count / max) * 100)); }

  processWithDatabricks(): void {
    this.databricksLoading = true;
    this.databricksError = '';
    this.databricksResult = null;
    this.databricks.process({ from: this.from || null, to: this.to || null }).subscribe({
      next: response => { this.databricksLoading = false; this.databricksResult = response.result; this.load(); },
      error: err => { this.databricksLoading = false; this.databricksError = err?.error?.error || err?.error?.message || err?.message || 'Databricks processing failed.'; }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
