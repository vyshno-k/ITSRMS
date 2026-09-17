# Angular cache note

This ZIP intentionally does not include Angular's `.angular` cache or `node_modules`.

If the browser appears to run an older version after extracting the project, stop `ng serve` and run:

```powershell
Remove-Item -Recurse -Force .angular -ErrorAction SilentlyContinue
npm install
npm start
```

Then hard-refresh Chrome with `Ctrl + Shift + R`.

The Service Request details flow is:

```text
POST /api/service-requests
    -> 201 Created
    -> navigate to /service-requests/{id}
    -> GET /api/service-requests/{id}
    -> GET /api/service-requests/{id}/history
    -> GET /api/service-requests/{id}/comments
```

The details screen clears the loading state as soon as the ticket GET succeeds, so history/comments cannot leave the page stuck on "Loading ticket details...".
