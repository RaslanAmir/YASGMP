# Fix for InvalidOperationException in Microsoft.Extensions.DependencyInjection

## Problem
Users were experiencing `System.InvalidOperationException` during application startup, originating from `Microsoft.Extensions.DependencyInjection.dll`. This typically indicates an issue with service registration in the dependency injection container.

## Root Cause
The `AuditService` was being registered explicitly using `services.AddSingleton<AuditService>()` in both:
- `MauiProgram.cs` (line 190)
- `YasGMP.Wpf/App.xaml.cs` (line 98)

While a guard method `YasGmpCoreServiceGuards.EnsureAuditServiceSingleton()` existed to prevent duplicate registrations, it was never being called in production code - only in unit tests.

## Solution
Replaced explicit `AuditService` registration with the guard method that:
1. Removes any pre-existing registrations for `AuditService`
2. Adds a clean singleton registration
3. Ensures the service is only registered once, preventing potential duplicate registration errors

### Changes Made

#### MauiProgram.cs
```diff
 var services = core.Services;

+// Ensure AuditService is registered only once as singleton (guard against duplicates)
+YasGmpCoreServiceGuards.EnsureAuditServiceSingleton(services);
+
 // Core Services
 services.AddSingleton<IPlatformService, MauiPlatformService>();
-services.AddSingleton<AuditService>();
 services.AddSingleton<ExportService>();
```

#### YasGMP.Wpf/App.xaml.cs
```diff
 var svc = core.Services;
 svc.AddSingleton(databaseOptions);
 svc.AddSingleton(TimeProvider.System);
-svc.AddSingleton<AuditService>();
+
+// Ensure AuditService is registered only once as singleton (guard against duplicates)
+YasGmpCoreServiceGuards.EnsureAuditServiceSingleton(svc);
+
 svc.AddSingleton<ExportService>();
```

## Benefits
- **Defensive**: Guards against duplicate registrations even if code is called multiple times
- **Minimal**: Only 2 files changed with minimal modifications
- **Centralized**: Uses existing guard infrastructure from `YasGmpCoreServiceGuards`
- **Consistent**: Applied to both MAUI and WPF applications

## Service Registration Order
The fix maintains correct dependency order:
1. `DatabaseService` (registered via `UseDatabaseService`)
2. `YasGmpDbContext` (registered via `AddDbContext`)
3. `AuditService` (depends on DatabaseService)
4. `RBACService` (depends on DatabaseService + AuditService)
5. `UserService` (depends on DatabaseService + AuditService + IRBACService)
6. `AuthService` (depends on UserService + AuditService)

All dependencies are properly satisfied with this registration order.

## Testing
After applying this fix:
- MAUI application should start without DI exceptions
- WPF application should start without DI exceptions
- All services depending on `AuditService` should resolve correctly

## References
- `YasGMP.AppCore/DependencyInjection/YasGmpCoreServiceCollectionExtensions.cs:278` - Guard implementation
- `YasGMP.Wpf.Tests/ServiceRegistrationTests.cs:117` - Unit test demonstrating guard usage
