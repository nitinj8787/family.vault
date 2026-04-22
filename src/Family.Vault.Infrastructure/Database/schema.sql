PRAGMA foreign_keys = ON;

-- =========================================
-- USERS
-- =========================================
CREATE TABLE IF NOT EXISTS Users (
    Id TEXT PRIMARY KEY,
    Email TEXT NOT NULL UNIQUE,
    FullName TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt TEXT,
    IsActive INTEGER DEFAULT 1
);

-- =========================================
-- FAMILY MEMBERS
-- =========================================
CREATE TABLE IF NOT EXISTS FamilyMembers (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Relationship TEXT,
    Email TEXT,
    AccessLevel TEXT, -- Viewer, Editor, Emergency
    IsActive INTEGER DEFAULT 1,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_FamilyMembers_UserId ON FamilyMembers(UserId);

-- =========================================
-- USER ROLES
-- =========================================
CREATE TABLE IF NOT EXISTS UserRoles (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Role TEXT NOT NULL, -- PrimaryUser, Spouse, EmergencyAccess, Admin
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_UserRoles_UserId ON UserRoles(UserId);

-- =========================================
-- PROFILES
-- Extended with FullName, SpouseName, ChildrenJson for domain model compatibility.
-- =========================================
CREATE TABLE IF NOT EXISTS Profiles (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL UNIQUE,
    FullName TEXT NOT NULL DEFAULT '',
    SpouseName TEXT,
    DOB TEXT,
    Address TEXT,
    Country TEXT,
    ChildrenJson TEXT NOT NULL DEFAULT '[]',
    UpdatedAt TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- =========================================
-- CONTACTS (Emergency Contacts)
-- =========================================
CREATE TABLE IF NOT EXISTS Contacts (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Phone TEXT,
    Email TEXT,
    Relationship TEXT,
    PriorityOrder INTEGER,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_Contacts_UserId ON Contacts(UserId);

-- =========================================
-- NOMINEES
-- Extended with AssetType and Institution for domain model compatibility.
-- ContactDetails is reused to hold the institution name.
-- =========================================
CREATE TABLE IF NOT EXISTS Nominees (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Name TEXT NOT NULL,
    Relationship TEXT,
    AssetType TEXT NOT NULL DEFAULT '',
    ContactDetails TEXT, -- holds the institution/platform name
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_Nominees_UserId ON Nominees(UserId);

-- =========================================
-- ASSETS (BASE TABLE)
-- =========================================
CREATE TABLE IF NOT EXISTS Assets (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    AssetType TEXT NOT NULL, -- Bank, Investment, Property, Insurance, IndiaAsset, UkAsset
    Name TEXT,
    Country TEXT,
    Provider TEXT,
    NomineeId TEXT,
    IsActive INTEGER DEFAULT 1,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY(NomineeId) REFERENCES Nominees(Id)
);

CREATE INDEX IF NOT EXISTS idx_Assets_UserId ON Assets(UserId);
CREATE INDEX IF NOT EXISTS idx_Assets_Type ON Assets(AssetType);

-- =========================================
-- BANK ACCOUNTS
-- AccountNumber stores the full account number; masking is applied at the application layer.
-- Extended with Nominee for domain model compatibility.
-- =========================================
CREATE TABLE IF NOT EXISTS BankAccounts (
    Id TEXT PRIMARY KEY,
    AccountNumber TEXT,
    AccountType TEXT,
    IFSC_SWIFT TEXT,
    IsPrimary INTEGER DEFAULT 0,
    Nominee TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- INVESTMENTS
-- AccountId stores the full account/folio reference; masking is applied at the application layer.
-- Extended with Nominee for domain model compatibility.
-- =========================================
CREATE TABLE IF NOT EXISTS Investments (
    Id TEXT PRIMARY KEY,
    InvestmentType TEXT,
    Platform TEXT,
    AccountId TEXT,
    CurrentValue REAL,
    Nominee TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- PROPERTIES
-- Extended with DocumentsLocation for domain model compatibility.
-- AssetName is stored in Assets.Name.
-- =========================================
CREATE TABLE IF NOT EXISTS Properties (
    Id TEXT PRIMARY KEY,
    OwnershipType TEXT,
    Address TEXT,
    LoanLinked INTEGER,
    DocumentsLocation TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- INSURANCE POLICIES
-- PolicyNumber stores the full policy number; masking is applied at the application layer.
-- Extended with PolicyType, Coverage, Nominee for domain model compatibility.
-- Provider is stored in Assets.Provider.
-- =========================================
CREATE TABLE IF NOT EXISTS InsurancePolicies (
    Id TEXT PRIMARY KEY,
    PolicyNumber TEXT,
    PolicyType TEXT NOT NULL DEFAULT '',
    Coverage TEXT NOT NULL DEFAULT '',
    CoverageAmount REAL,
    ExpiryDate TEXT,
    ClaimContact TEXT,
    Nominee TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- INDIA ASSETS
-- Extended with Category and Nominee for domain model compatibility.
-- BankOrPlatform is stored in Assets.Provider.
-- =========================================
CREATE TABLE IF NOT EXISTS IndiaAssets (
    Id TEXT PRIMARY KEY,
    AccountType TEXT, -- NRE / NRO
    Repatriation TEXT,
    TaxStatus TEXT,
    Category TEXT NOT NULL DEFAULT '',
    Nominee TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- UK ASSETS
-- Extended with AccountNumber and Nominee for domain model compatibility.
-- Provider is stored in Assets.Provider.
-- =========================================
CREATE TABLE IF NOT EXISTS UkAssets (
    Id TEXT PRIMARY KEY,
    Category TEXT,
    TaxNotes TEXT,
    AccountNumber TEXT NOT NULL DEFAULT '',
    Nominee TEXT,
    FOREIGN KEY(Id) REFERENCES Assets(Id) ON DELETE CASCADE
);

-- =========================================
-- EMERGENCY FUND
-- =========================================
CREATE TABLE IF NOT EXISTS EmergencyFunds (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Location TEXT,
    Amount REAL,
    Currency TEXT,
    AccessInstructions TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_EmergencyFunds_UserId ON EmergencyFunds(UserId);

-- =========================================
-- TAX ENTRIES
-- =========================================
CREATE TABLE IF NOT EXISTS TaxEntries (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Country TEXT,
    IncomeType TEXT,
    TaxPaid REAL,
    DeclaredInUK INTEGER,
    Year INTEGER,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_TaxEntries_UserId ON TaxEntries(UserId);

-- =========================================
-- WILL ENTRIES
-- =========================================
CREATE TABLE IF NOT EXISTS WillEntries (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Country TEXT,
    ExistsFlag INTEGER,
    Location TEXT,
    ExecutorName TEXT,
    LastUpdated TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- =========================================
-- DOCUMENTS
-- =========================================
CREATE TABLE IF NOT EXISTS Documents (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    FileName TEXT,
    Category TEXT,
    BlobUrl TEXT,
    FileSize INTEGER,
    UploadedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_Documents_UserId ON Documents(UserId);

-- =========================================
-- DOCUMENT LINKS
-- =========================================
CREATE TABLE IF NOT EXISTS DocumentLinks (
    Id TEXT PRIMARY KEY,
    DocumentId TEXT NOT NULL,
    AssetId TEXT,
    LinkedEntityType TEXT,
    FOREIGN KEY(DocumentId) REFERENCES Documents(Id) ON DELETE CASCADE,
    FOREIGN KEY(AssetId) REFERENCES Assets(Id)
);

-- =========================================
-- ACCESS INSTRUCTIONS
-- =========================================
CREATE TABLE IF NOT EXISTS AccessInstructions (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Title TEXT,
    Description TEXT,
    Category TEXT,
    Priority INTEGER,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- =========================================
-- EMERGENCY PLANS
-- =========================================
CREATE TABLE IF NOT EXISTS EmergencyPlans (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    StepOrder INTEGER,
    Title TEXT,
    Description TEXT,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- =========================================
-- INSIGHTS (OPTIONAL CACHE)
-- =========================================
CREATE TABLE IF NOT EXISTS Insights (
    Id TEXT PRIMARY KEY,
    UserId TEXT NOT NULL,
    Type TEXT,
    Severity TEXT,
    Message TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY(UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- =========================================
-- AUDIT LOGS
-- =========================================
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id TEXT PRIMARY KEY,
    UserId TEXT,
    Action TEXT,
    EntityType TEXT,
    EntityId TEXT,
    Timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
    Metadata TEXT
);

CREATE INDEX IF NOT EXISTS idx_AuditLogs_UserId ON AuditLogs(UserId);
