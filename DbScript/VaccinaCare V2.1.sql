Create database VaccinaCareDb
Go

Use VaccinaCareDb
GO

CREATE TABLE [Users] (
  [UserID] int PRIMARY KEY IDENTITY(1, 1),
  [FullName] nvarchar(255),
  [Email] nvarchar(255) UNIQUE,
  [PhoneNumber] nvarchar(255),
  [PasswordHash] nvarchar(255),
  [RoleID] int
)
GO

CREATE TABLE [Roles] (
  [RoleID] int PRIMARY KEY IDENTITY(1, 1),
  [RoleName] nvarchar(255)
)
GO

CREATE TABLE [Children] (
  [ChildID] int PRIMARY KEY IDENTITY(1, 1),
  [ParentID] int,
  [FullName] nvarchar(255),
  [DateOfBirth] date,
  [Gender] nvarchar(255),
  [MedicalHistory] text
)
GO

CREATE TABLE [VaccineSuggestions] (
  [SuggestionID] int PRIMARY KEY IDENTITY(1, 1),
  [ChildID] int,
  [ServiceID] int,
  [SuggestedVaccine] text,
  [Status] nvarchar(255)
)
GO

CREATE TABLE [Appointments] (
  [AppointmentID] int PRIMARY KEY IDENTITY(1, 1),
  [ParentID] int,
  [ChildID] int,
  [PolicyID] int,
  [AppointmentDate] datetime,
  [Status] nvarchar(255),
  [Notes] text,
  [ServiceType] nvarchar(255),
  [Duration] int,
  [Room] nvarchar(255),
  [ReminderSent] bit,
  [CancellationReason] text,
  [Confirmed] bit,
  [TotalPrice] decimal,
  [PreferredTimeSlot] nvarchar(255)
)
GO

CREATE TABLE [Services] (
  [ServiceID] int PRIMARY KEY IDENTITY(1, 1),
  [ServiceName] nvarchar(255),
  [Description] text,
  [PicUrl] nvarchar(255),
  [Type] nvarchar(255),
  [Price] decimal
)
GO

CREATE TABLE [ServiceAvailability] (
  [AvailabilityID] int PRIMARY KEY IDENTITY(1, 1),
  [ServiceID] int,
  [Date] date,
  [TimeSlot] nvarchar(255),
  [Capacity] int,
  [Booked] int
)
GO

CREATE TABLE [AppointmentsServices] (
  [AppointmentServiceID] int PRIMARY KEY IDENTITY(1, 1),
  [AppointmentID] int,
  [ServiceID] int,
  [Quantity] int,
  [TotalPrice] decimal
)
GO

CREATE TABLE [UsersVaccinationServices] (
  [UserServiceID] int PRIMARY KEY IDENTITY(1, 1),
  [UserID] int,
  [ServiceID] int
)
GO

CREATE TABLE [VaccinationRecords] (
  [RecordID] int PRIMARY KEY IDENTITY(1, 1),
  [ChildID] int,
  [VaccinationDate] datetime,
  [ReactionDetails] text
)
GO

CREATE TABLE [Notifications] (
  [NotificationID] int PRIMARY KEY IDENTITY(1, 1),
  [AppointmentID] int,
  [Message] text,
  [ReadStatus] nvarchar(255)
)
GO

CREATE TABLE [Feedback] (
  [FeedbackID] int PRIMARY KEY IDENTITY(1, 1),
  [AppointmentID] int,
  [Rating] int,
  [Comments] text
)
GO

CREATE TABLE [Payments] (
  [PaymentID] int PRIMARY KEY IDENTITY(1, 1),
  [AppointmentID] int,
  [Amount] decimal,
  [PaymentStatus] nvarchar(255)
)
GO

CREATE TABLE [Invoices] (
  [InvoiceID] int PRIMARY KEY IDENTITY(1, 1),
  [UserID] int,
  [PaymentID] int,
  [TotalAmount] decimal
)
GO

CREATE TABLE [CancellationPolicies] (
  [PolicyID] int PRIMARY KEY IDENTITY(1, 1),
  [PolicyName] nvarchar(255),
  [Description] text,
  [CancellationDeadline] int,
  [PenaltyFee] decimal
)
GO

CREATE TABLE [VaccinePackages] (
  [PackageID] int PRIMARY KEY IDENTITY(1, 1),
  [PackageName] nvarchar(255),
  [Description] text,
  [Price] decimal
)
GO

CREATE TABLE [VaccinePackageDetails] (
  [PackageDetailID] int PRIMARY KEY IDENTITY(1, 1),
  [PackageID] int,
  [ServiceID] int,
  [DoseOrder] int
)
GO

CREATE TABLE [PackageProgress] (
  [ProgressID] int PRIMARY KEY IDENTITY(1, 1),
  [ParentID] int,
  [PackageID] int,
  [ChildID] int,
  [DosesCompleted] int,
  [DosesRemaining] int
)
GO


ALTER TABLE [VaccineSuggestions] ADD FOREIGN KEY ([ChildID]) REFERENCES [Children] ([ChildID])
GO

ALTER TABLE [VaccineSuggestions] ADD FOREIGN KEY ([ServiceID]) REFERENCES [Services] ([ServiceID])
GO

ALTER TABLE [ServiceAvailability] ADD FOREIGN KEY ([ServiceID]) REFERENCES [Services] ([ServiceID])
GO

ALTER TABLE [AppointmentsServices] ADD FOREIGN KEY ([AppointmentID]) REFERENCES [Appointments] ([AppointmentID])
GO

ALTER TABLE [AppointmentsServices] ADD FOREIGN KEY ([ServiceID]) REFERENCES [Services] ([ServiceID])
GO

ALTER TABLE [UsersVaccinationServices] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [UsersVaccinationServices] ADD FOREIGN KEY ([ServiceID]) REFERENCES [Services] ([ServiceID])
GO

ALTER TABLE [Notifications] ADD FOREIGN KEY ([AppointmentID]) REFERENCES [Appointments] ([AppointmentID])
GO

ALTER TABLE [Invoices] ADD FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [Invoices] ADD FOREIGN KEY ([PaymentID]) REFERENCES [Payments] ([PaymentID])
GO

ALTER TABLE [VaccinePackageDetails] ADD FOREIGN KEY ([PackageID]) REFERENCES [VaccinePackages] ([PackageID])
GO

ALTER TABLE [VaccinePackageDetails] ADD FOREIGN KEY ([ServiceID]) REFERENCES [Services] ([ServiceID])
GO

ALTER TABLE [PackageProgress] ADD FOREIGN KEY ([ParentID]) REFERENCES [Users] ([UserID])
GO

ALTER TABLE [PackageProgress] ADD FOREIGN KEY ([PackageID]) REFERENCES [VaccinePackages] ([PackageID])
GO

ALTER TABLE [PackageProgress] ADD FOREIGN KEY ([ChildID]) REFERENCES [Children] ([ChildID])
GO
