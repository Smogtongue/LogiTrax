# LogiTrax API – Build Guide & System Overview

This README documents how to recreate the LogiTrax API from scratch. It covers the initial business logic and database integration (Activity 1), the development of RESTful API endpoints (Activity 2), and the implementation of secure authentication with JWT and role‑based authorization (Activity 3). 

Each section details not only the original planned steps but also the modifications made to enhance the design.

---

# Table of Contents
1. [Activity 1: Structuring Business Logic and Database Integration](#activity-1-structuring-business-logic-and-database-integration)
2. [Activity 2: Building RESTful API Endpoints](#activity-2-building-restful-api-endpoints)
3. [Activity 3: JWT Authentication & Securing the API](#activity-3-jwt-authentication--securing-the-api)
4. [Activity 4: Optimizing Performance with Caching and Copilot](#activity-4-optimizing-performance-with-caching-and-copilot)
5. [Activity 5: Strengthening Security with ASP.NET Identity and JWT](#activity-5-strengthening-security-with-aspnet-identity-and-jwt)

---

<details>
<summary><b style="font-size: 1.25em;">Activity 1: Structuring Business Logic and Database Integration</b></summary>

### 1. **Project Setup:**  
   - Open Visual Studio Code.
   - Create a new ASP.NET Core Web API project using the CLI: 
        - `dotnet new webapi -n LogiTrax`

   - Create a **Models** folder and add two new class files:  
     - *InventoryItem.cs*  
     - *Order.cs*

### 2. **Define the Core Models:**  
   - In *InventoryItem.cs*, define a product with properties such as:
        - `ItemId`, `Name`, `Quantity`, and `Location`. 
   - In *InventoryItem.cs*, implement a `DisplayInfo()` method: 
        - *EX:* `[ Item: GamePal | Quantity: 12 | Location: Warehouse A ]`
   - In *Order.cs*, define an order with properties like:
        - `OrderId`, `CustomerName`, `DatePlaced`, and a list to hold ordered items.
   - ***Test these models in a sample block of code (in Program.cs or a dedicated test controller).***

### 3. **Database Integration with EF Core:**  
   - Add EF Core packages via NuGet. 
        - `dotnet add package Microsoft.EntityFrameworkCore.Sqlite`
        - `dotnet add package Microsoft.EntityFrameworkCore.Tools`
   - Create the *LogiTraxContext.cs* file to establish a connection to a SQLite database and configure the model relationships.
  
   - ***Ensure migrations run and the database is created successfully.***

**Modifications and Enhancements Made:**  
- We identified design issues with the original Order model that directly held a list of InventoryItem objects. This led to duplicate key conflicts, orphaned records, and foreign key constraint issues.

- **Improvement:** We refactored the design to introduce a new entity, *OrderItem.cs*. Instead of duplicating product data, each OrderItem now holds a reference (by ID) to an InventoryItem along with the ordered quantity. The *Order.cs* file was updated to include a list of OrderItem objects rather than full InventoryItem objects.

- The EF Core context (*LogiTraxContext.cs*) was updated to define proper relationships and cascade delete rules between InventoryItem, Order, and OrderItem.

### **Submission Checklist for Activity 1:**
**1. Two working classes: InventoryItem and Order**
   - Created the C# class "InventoryItem.cs" with properties:
       • ItemId (int)
       • Name (string)
       • Quantity (int)
       • Location (string)
     Plus a DisplayInfo() method to format output.
   - Developed the "Order.cs" class featuring:
       • OrderId (int)
       • CustomerName (string)
       • DatePlaced (DateTime)
       • A list for order items
     And methods to add items, remove items, and generate an order summary.

**2. Test data with basic output shown in console or controller**
   - We implemented test code (in Program.cs or a dedicated test controller) that instantiates InventoryItem and Order objects.
   - Basic output was verified via console logs (e.g., calling DisplayInfo() on a sample InventoryItem).

**3. LogiTrackContext with EF Core integration**
   - The "LogiTrackContext.cs" file was created to setup the EF Core context.
   - This context includes DbSet properties for InventoryItem and Order.
   - EF Core relationships & configuration settings were defined (via Fluent API or Data Annotations).

**4. Database successfully created and tested**
   - EF Core migrations were applied to create the database.
   - The database structure was verified with test data insertions and retrievals.
   - Basic CRUD operations were demonstrated to confirm the database works as expected.

**5. Copilot-assisted review applied**
   - Microsoft Copilot was leveraged to review and refine our class implementations.
   - Its suggestions improved naming conventions, error handling, and performance aspects.
   - The final code reflects these improvements and adheres to best practices.

</details>

---
<details>
<summary><b style="font-size: 1.25em;"> Activity 2: Building RESTful API Endpoints</b></summary>

1. **Creating the Controllers:**  
   - In the **Controllers** folder, create two controller files:  
     - *InventoryController.cs*  
     - *OrderController.cs*

2. **Developing Inventory Endpoints:**  
   - In *InventoryController.cs*, implement endpoints for:
     - GET `/api/inventory` to return all inventory items.
     - POST `/api/inventory` to add a new inventory item.
     - DELETE `/api/inventory/{id}` to remove an inventory item.
   - Use dependency injection to access *LogiTraxContext.cs*.

3. **Developing Order Endpoints:**  
   - In *OrderController.cs*, implement endpoints for:
     - GET `/api/orders` to list all orders.
     - GET `/api/orders/{id}` to return the full details of a specific order.
     - POST `/api/orders` to create a new order (accepting a JSON payload with customer name, date placed, and an array of order items including inventoryItem IDs and quantities).
     - DELETE `/api/orders/{id}` to remove an order.

4. **Endpoint Testing:**  
   - Test these endpoints using Swagger or Postman to verify that the inventory and orders are created and persisted as expected.

**Modifications and Enhancements Made:**  
- We enhanced the order operations so that when a POST order request is received, the API verifies stock availability in the referenced inventory item(s), subtracts the ordered quantities, and creates the order with correctly associated OrderItem records.

- When an order is deleted, the system now restores the deducted inventory quantities before removing the order and its associated OrderItem records.

- In addition, we implemented an inventory cleanup endpoint in *InventoryController.cs* to consolidate duplicate or orphaned inventory records.

- Copilot suggestions were applied to improve error handling, asynchronous controller operations, and route design.


### Submission Checklist for Activity 2:
---------------------
**1. Two API Controllers: Inventory and Order**
   - Developed both InventoryController and OrderController.
   - Each controller handles domain-specific routes.

**2. Working GET, POST, DELETE Routes for Both Controllers**
   - Implemented RESTful endpoints for retrieving, creating, and deleting inventory items and orders.
   - Ensured all routes support the expected CRUD operations.

**3. Routes Tested in Swagger or Postman**
   - Verified functionality and responses using Swagger and/or Postman.
   - Confirmed each route’s proper operation and data integrity.

**4. Microsoft Copilot Suggestions Applied**
   - Integrated Copilot's recommendations to improve route logic, error handling, and query optimizations.
   - Code refined to follow best practices and boost performance.

</details>

---

<details>
<summary><b style="font-size: 1.25em;">Activity 3: JWT Authentication & Securing the API</b></summary
>

1. **Configuring ASP.NET Identity and JWT:**  
   - Register ASP.NET Identity services using a custom user model (e.g., *ApplicationUser*).
   - Configure EF Core to work with Identity.
   - Set up JWT authentication in *Program.cs*, including validation parameters (issuer, audience, secret), and specify claim mapping (NameClaimType and RoleClaimType).
   - Configure necessary JWT settings (Secret, ValidIssuer, ValidAudience) in the configuration file (e.g., *appsettings.json*).

2. **Implementing Authentication Endpoints:**  
   - Create *AuthController.cs* with:
     - A registration endpoint (`POST /api/auth/register`) for new user creation.
     - A login endpoint (`POST /api/auth/login`) that authenticates the user and generates a JWT token that includes role claims.
   - Ensure the JWT token properly includes the user’s roles (especially the "Manager" role for administrative endpoints).

3. **Integrating Swagger with JWT:**  
   - Update Swagger configuration in *Program.cs* to define a JWT security scheme.
   - Ensure an **Authorize** button appears in the Swagger UI so that a JWT token (prefixed with “Bearer”) can be easily applied for testing secured endpoints.

4. **Securing Endpoints:**  
   - Secure specific endpoints (for example, the user search endpoint in *UsersController.cs*) using `[Authorize(Roles = "Manager")]`. This ensures that only users with the appropriate role (e.g., a manager) can access these routes.

5. **Database Seeding for Security:**  
   - Update the seeding logic in *Program.cs* to apply EF Core migrations, seed sample inventory data, create the "Manager" role if it does not exist, and create a test manager user (e.g., manager@example.com).

**Modifications and Enhancements Made:**  
- We ensured that the JWT token includes role claims by modifying the login endpoint and explicitly mapping claims in the JWT authentication configuration.
- Swagger was configured to support the JWT bearer token, allowing for an easy testing method during development.
- Additional endpoints (such as a paginated, partial-match user search) were secured using role-based authorization, providing a robust design for administrative operations.

### Submission Checklist:
---------------------
**1. ASP.NET Identity installed and configured**
   - ASP.NET Identity has been integrated into the project.
   - Relevant services and middleware have been registered in Program.cs.

**2. ApplicationUser model created**
   - A custom ApplicationUser model was developed, extending IdentityUser to meet project needs.

**3. Registration and login routes added**
   - An AuthController was implemented with endpoints for user registration and login.
   - These endpoints handle user creation and JWT token generation.

**4. JWT token-based authentication implemented**
   - JWT authentication was configured in Program.cs with proper token validation parameters.
   - Generated tokens include role claims for secure, role-based access control.

**5. Routes secured with [Authorize] and roles**
   - Controllers and endpoints are decorated with [Authorize] attributes.
   - Role-based restrictions (e.g., [Authorize(Roles = "Manager")]) have been applied to sensitive routes.

**6. Copilot suggestions reviewed and applied**
   - Microsoft Copilot was used to review and refine the security implementation.
   - Its insights helped improve error handling, endpoint logic, and overall security.

**7. Database updated with Identity schema**
   - EF Core migrations were run to update the database, including the Identity schema.
   - The Identity-related tables are now part of the database structure.

</details>

---------------------

<details>
<summary><b style="font-size: 1.25em;">Activity 4: Optimizing Performance with Caching and Copilot</b></summary>

1. **Enable In-Memory Caching:**  
   - Register caching in *Program.cs* using `builder.Services.AddMemoryCache()`.
   - Inject `IMemoryCache` in the controllers that require caching (e.g., *InventoryController*).

2. **Cache Frequently Accessed Data:**  
   - Modify the GET endpoint in *InventoryController* to first check for a cached inventory list.
   - If the cache key (e.g., "InventoryList") is not found, query the database using a no‑tracking approach and then store the result in the cache with an absolute expiration (e.g., 30 seconds).

3. **Analyze and Optimize Queries:**  
   - In controllers (such as *OrderController*), use `.Include()` and `.ThenInclude()` to eager-load related data, reducing N+1 issues.
   - Use `.AsNoTracking()` for read-only queries to boost performance.
   
4. **Measure Improvements:**  
   - Instrument endpoints using timing methods (like the `Stopwatch` class) or profiling tools.
   - Log the elapsed time for cache hits versus direct DB calls to verify enhancements.

5. **Use Copilot to Review and Enhance Performance:**  
   - Pose prompts such as “Suggest performance improvements in this controller” and “Improve the speed of this API endpoint.”
   - Apply Microsoft Copilot’s suggestions to refine the caching logic, query optimizations, and overall controller performance.

**Modifications and Enhancements Made:**  
- Caching was implemented in the GET endpoint of *InventoryController*, reducing repeated database calls and lowering response times.
- Query optimizations were applied across controllers using `.Include()`, `.ThenInclude()`, and `.AsNoTracking()` to eliminate redundant calls and accelerate data retrieval.
- Diagnostic logging (using `Stopwatch`) was added to compare performance between cached and non-cached requests.
- Copilot suggestions were integrated to further streamline controller logic and enhance error handling.
- Cache invalidation was added to update or delete endpoints (such as inventory cleanup and posting new inventory) ensuring fresh data on subsequent GET requests.

### Submission Checklist:
---------------------
**1. In-memory caching implemented on at least one route**  
   - The *InventoryController* GET endpoint uses in-memory caching with a 30-second expiration.

**2. Query optimizations applied in controller logic**  
   - Use of `.AsNoTracking()` for read-only operations.
   - Eager-loading with `.Include()` and `.ThenInclude()` in controllers (e.g., in *OrderController*) to mitigate N+1 issues.

**3. Slow or repeated queries reduced or eliminated**  
   - Optimized logic in controllers reduces database calls, confirmed via testing and logging.

**4. Cache expiration policy in place**  
   - Implemented an absolute expiration policy (30 seconds) for cached data ensuring timely refresh.

**5. Copilot prompts used to refine logic**  
   - Copilot suggestions were leveraged to improve performance, error handling, and endpoint efficiency.

**6. Improvements verified through testing or time measurement**  
   - Performance measurements using `Stopwatch` indicate significant speed-ups (e.g., cache hits in near-zero ms versus direct DB queries).

</details>

---------------------

<details>
<summary><b style="font-size: 1.25em;">Activity 5: Strengthening Security with ASP.NET Identity and JWT</b></summary>

1. **ASP.NET Identity Configuration:**  
   - Integrated ASP.NET Identity by extending the DbContext from `IdentityDbContext<ApplicationUser>`.
   - Configured Identity options and updated the database with the Identity schema.
   
2. **User Model and Authentication Endpoints:**  
   - Created the `ApplicationUser` model inheriting from `IdentityUser`, allowing for custom user properties.
   - Developed `AuthController` with dedicated endpoints for user registration and login.
   - Defined `RegisterModel` and `LoginModel` to handle registration and login data respectively.
   
3. **JWT Token Generation:**  
   - Configured JWT settings (issuer, audience, expiration, and signing credentials) in the application configuration.
   - The login endpoint generates a JWT token that includes user identity and role claims.
   - Tokens are set to expire after 3 hours, providing a secure session without undue persistence.

4. **Securing Routes:**  
   - Applied the `[Authorize]` attribute on protected routes, ensuring that only authenticated users and authorized roles (e.g., "Manager") can access them.
   - Implemented role-based claims extraction so that secured endpoints can enforce fine-grained access.

5. **Copilot Guidance and Testing:**  
   - Leveraged Copilot’s suggestions to optimize registration and login logic.
   - Verified the complete authentication workflow using Swagger: registering a user, logging in, and passing the Bearer token to secured endpoints.
   - Confirmed that both regular users and managers receive valid JWT tokens that enforce appropriate route restrictions.

**Modifications and Enhancements Made:**  
- Enabled full integration of ASP.NET Identity and updated the database with Identity schema.
- Created and tested registration and login endpoints with comprehensive input validation.
- Implemented secure JWT token generation with embedded identity and role claims.
- Secured API endpoints using `[Authorize]` attributes, ensuring robust access control.
- Incorporated Copilot suggestions to improve code clarity and strengthen security measures.
- Confirmed end-to-end functionality through Swagger testing, ensuring that JWT tokens work as expected.

### Submission Checklist:
---------------------
**1. ASP.NET Identity configured and operational**  
   - Identity services are added, and the database reflects the Identity schema.

**2. ApplicationUser model and required data models created**  
   - `ApplicationUser`, `RegisterModel`, and `LoginModel` are implemented correctly.

**3. Registration and login routes added and tested**  
   - `AuthController` exposes endpoints for user registration and login, returning JWT tokens.

**4. JWT token-based authentication implemented**  
   - Tokens are generated with proper claims (including roles) and have a defined expiration.

**5. Routes secured with [Authorize] and role restrictions**  
   - Secured endpoints enforce authentication and restrict access based on user roles.

**6. Copilot suggestions reviewed and integrated**  
   - Machine-generated guidance was applied to enhance security and code organization.

**7. Successful end-to-end testing with Swagger**  
   - All aspects of the authentication flow have been verified via Swagger testing.

</details>

---

Following this guide will allow anyone to recreate the LogiTrax API from scratch with a robust, modern, and secure architecture.

---

