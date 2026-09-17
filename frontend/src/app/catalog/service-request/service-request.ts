import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  ServiceRequestService,
  ServiceRequest
} from '../services/service-request';

import {
  ServiceCatalogService,
  ServiceCatalogItem
} from '../services/service-catalog';

@Component({
  selector: 'app-service-request',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './service-request.html',
  styleUrl: './service-request.css'
})
export class ServiceRequestComponent implements OnInit {

  services: ServiceCatalogItem[] = [];
  requests: ServiceRequest[] = [];

  selectedServiceId: number | null = null;

  requestedBy = '';
  currentEmployee = '';

  title = '';
  description = '';
  priority = 'Medium';


  constructor(
    private serviceRequestService: ServiceRequestService,
    private serviceCatalogService: ServiceCatalogService
  ) {}


  ngOnInit(): void {
    this.loadServices();
    this.loadRequests();
  }


  loadServices(): void {

    this.serviceCatalogService.getServices().subscribe({

      next: (data) => {

        console.log('Services received by Angular:', data);

        // Only active services are available
        this.services = data.filter(service => service.isActive);

        console.log('Active services:', this.services);

      },

      error: (error) => {

        console.error('Error loading services:', error);

      }

    });

  }


  loadRequests(): void {

    this.serviceRequestService.getRequests().subscribe({

      next: (data) => {

        this.requests = data;

      },

      error: (error) => {

        console.error('Error loading service requests:', error);

      }

    });

  }


  onServiceChange(): void {

    if (this.selectedServiceId === null) {

      this.priority = 'Medium';

      return;

    }


    const selectedService = this.services.find(
      service => service.id === this.selectedServiceId
    );


    if (selectedService) {

      this.priority = selectedService.defaultPriority;

    }

  }


  submitRequest(): void {

    if (
      this.selectedServiceId === null ||
      !this.requestedBy.trim() ||
      !this.title.trim() ||
      !this.description.trim()
    ) {

      alert('Please fill all required fields.');

      return;

    }


    /*
     * Store the employee name as the current employee.
     *
     * For now, because our project does not have
     * login/authentication, we use the name entered
     * in "Requested By".
     */
    this.currentEmployee = this.requestedBy.trim();


    const request: ServiceRequest = {

      id: 0,

      title: this.title.trim(),

      description: this.description.trim(),

      priority: this.priority,

      status: 'Open',

      requestedBy: this.currentEmployee,

      serviceCatalogId: this.selectedServiceId

    };


    this.serviceRequestService.createRequest(request).subscribe({

      next: (response) => {

        console.log('Request created:', response);

        alert('Service request created successfully!');


        // Add the new request to the list
        this.requests.push(response);


        // Clear only the request fields
        this.selectedServiceId = null;
        this.title = '';
        this.description = '';
        this.priority = 'Medium';

      },


      error: (error) => {

        console.error('Error creating service request:', error);

        alert('Failed to create service request.');

      }

    });

  }


  getServiceName(serviceCatalogId: number): string {

    const service = this.services.find(
      service => service.id === serviceCatalogId
    );


    return service
      ? service.serviceName
      : 'Unknown Service';

  }


  /*
   * Return only requests belonging to
   * the current employee.
   */
  getMyRequests(): ServiceRequest[] {

    if (!this.currentEmployee.trim()) {

      return [];

    }


    return this.requests.filter(

      request =>
        request.requestedBy.trim().toLowerCase() ===
        this.currentEmployee.trim().toLowerCase()

    );

  }

}