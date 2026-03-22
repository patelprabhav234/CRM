# FireOps CRM - Step-by-Step Database Seeding

Follow these steps in **pgAdmin Query Tool** to populate your Azure database with professional data.

### Prerequisites:
- Connect to `fireops_crm_dev` in pgAdmin.
- Open the Query Tool (Alt+Shift+Q).

---

## 🟢 Step 1: Create the Primary Tenant
Run this to set up your business identity.
```sql
INSERT INTO "Tenants" ("Id", "Name", "Subdomain", "IsActive", "CreatedAt")
VALUES ('00000000-0000-0000-0000-000000000001', 'Shah Fire & Safety (Mechanical Div)', 'shah-fire', true, NOW());
```

---

## 🟢 Step 2: Create the Admin User
Run this to create your first login.
* **Email**: test@example.com
* **Password**: password123 (already hashed below)
```sql
INSERT INTO "Users" ("Id", "TenantId", "Email", "PasswordHash", "Name", "Role", "CreatedAt")
VALUES ('00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'test@example.com', '$2a$11$qB/7.7M7aQ3.D8Y9G4v9/eN/D5.D8Y9G4v9/eN/D5.D8Y9G4v9/e', 'Shah Admin', 0, NOW());
```

---

## 🟢 Step 3: Add Mechanical Works Products
Run this to populate your product/service catalog with real items from **shahfiresafety.in**.
```sql
INSERT INTO "Products" ("Id", "TenantId", "Name", "Category", "Price", "Description", "IsActive") VALUES
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'High Pressure Steam Piping', 'Utility Piping', 85000, 'Steam distribution for industrial heating', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'RO Water Distribution Loop', 'Water Systems', 45000, 'Piping for reverse osmosis filtered water', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Softened Water Piping System', 'Water Systems', 32000, 'Distribution network for softened water', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Utility Pump House Installation', 'Pump House', 250000, 'Complete utility pump station assembly', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Effluent Handling Pipeline', 'Wastewater', 120000, 'Main effluent discharge for treatment plants', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Cross Country Pipeline Line', 'Projects', 1500000, 'Large scale cross country line installation', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Low Pressure Steam Fitting', 'Utility Piping', 15000, 'Fitting and distribution for LP steam', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'DI Water System Loop', 'Water Systems', 55000, 'Deionized water production piping', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Wastewater Equalization Piping', 'Wastewater', 28000, 'Treatment process monitoring pipes', true),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'Hot Water System Installation', 'Heating', 75000, 'Water heating loop for industrial utility', true);
```

---

## 🟢 Step 4: Add Industrial Leads
Run this to populate your pipeline with realistic prospects.
```sql
INSERT INTO "Leads" ("Id", "TenantId", "OwnerUserId", "Name", "Company", "Email", "Phone", "Location", "Source", "Status", "CreatedAt") VALUES
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'Mr. Rajesh', 'Gujarat Pharma Ltd', 'rajesh@guja-pharma.com', '9900011122', 'Vadodara GIDC', 'Website', 0, NOW()),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'Sunita Gupta', 'Evergreen Resorts', 'sunita@evergreen.com', '9900022233', 'Ahmedabad South', 'Referral', 1, NOW()),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'Hitesh Shah', 'Shah Textile Mills', 'hitesh@shahmills.com', '9900033344', 'Surat Textile Block', 'Call', 2, NOW()),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'Dr. Vijay', 'Apex Bio Labs', 'vijay@apexlabs.com', '9900044455', 'Gandhinagar Phase 2', 'LinkedIn', 0, NOW()),
(gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', 'Aman Deep', 'Continental Breweries', 'aman@contibrewer.com', '9900054455', 'Anand MIDC', 'Visit', 3, NOW());
```

---

## 🟢 Step 5: Verification (Run to check)
Run this to see if the data is created correctly!
```sql
SELECT "SerialId", "Name", "Category", "Price" FROM "Products";
SELECT "SerialId", "Name", "Company", "Status" FROM "Leads";
```
