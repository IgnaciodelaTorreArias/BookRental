# Book Rental

This project has the purpose of getting used to .NET/ASP.NET/C#.

The project is the backend of a Book Rental Company.

## Patterns

~~~markdown
├─ Decomposition
│  └─ Business Capability
│    └─ Subdomain (Domain Driven Design)
├─ Inter-Service Communication
│  ├─ Synchronous (gRPC)
│  └─ Asynchronous (Event driven: Kafka)
├─ Integration
│  ├─ API Gateway
│  └─ Direct client-to-service
├─ Database
│  └─ Database per Service
├─ Security
│  └─ Access token
│    └─ Claims-Based Authorization/RBAC
└─ Observability
   ├─ Centralized Logging (Loki)
   ├─ Control panel (Grafana)
   ├─ Distributed Tracing (Tempo)
   └─ Centralized Metrics (Prometheus)
~~~

## Security

### Roles

- Acquisitions: Responsible for registering books and books acquisitions.

- Operations: Responsible for managing operations like receiving a confirming book acquisitions or schedule mailing books.

- Admin: Responsible for managing personal and correcting mistakes.

- System: A special role for the system. Some services like inventory are client facing services, meaning clients can call the service directly, but other services shouldn't be called by users but by other services. When a service (A) needs to call one of those non-user services (B), then the service (A) gets a token with the "System" role and uses it to authorize its call to the non-user service (B).

### forging

The public Key used in appsettings comes from:
<https://token.dev/>

## Services

### Current

- Inventory: Manages Books inventory.
- Rental. Manages Book Rental.

### Planned

- Recommendation. Given the books description uses AI to get embeddings and stores them in a Vector DB. I plan to create this service using python and "sentence-transformers/all-MiniLM-L6-v2".

### Never

To "complete" the project the system would need other services, but currently i don't plan on implementing the complete system.

- Notifications. Although i created a Kafka topic for this purpose.

- Returns. Returning books requieres complicated logic, like verifications, when the user marks they want to return a book, tracking the delivery, receiving the delivery.

## Analytics

The Database for the rental service already as some tables specific for analytics. I will use PowerBI for data analysis over these and other tables.

## Optimizations?

While designing the system i made some "questionable" decisions like purposely leaving tables without FK's.

In the Inventory Database i have a `stock` table it has a field `book_id`, that references to the table's field: `books.book_id`. I didn't add the FK because this is an auto-populated table it's populated based on acquisitions, since i think acquisitions would be done in batches, a batch insert would be slowed by FK constraints. But this is only an OPINION.

In the Rental Database i have a `waiting_list` table `notified`, etc. These tables are constantly being "purged", and represent different stages of a rental waiting_list->notified->confirmed. One could argue about this design, but again this is just what i wanted.

A connection pooling mechanism for the databases would be nice.
