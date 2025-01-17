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


-- Insert data into Roles
INSERT INTO [Roles] ([RoleName]) VALUES 
('Customer'),
('Admin'),
('Staff');

-- Insert data into Users
INSERT INTO [Users] ([FullName], [Email], [PhoneNumber], [PasswordHash], [RoleID]) VALUES 
('Alice Johnson', 'alice.johnson@example.com', '1234567890', 'passwordhash1', 1),
('Bob Smith', 'bob.smith@example.com', '9876543210', 'passwordhash2', 2),
('Charlie Brown', 'charlie.brown@example.com', '4561237890', 'passwordhash3', 3);

-- Insert data into Children
INSERT INTO [Children] ([ParentID], [FullName], [DateOfBirth], [Gender], [MedicalHistory]) VALUES 
(1, 'Emily Johnson', '2015-06-15', 'Female', 'None'),
(1, 'Ethan Johnson', '2018-09-20', 'Male', 'Allergy to peanuts'),
(3, 'Sophia Brown', '2016-03-12', 'Female', 'Asthma');

-- Insert data into Services
INSERT INTO [Services] ([ServiceName], [Description], [PicUrl], [Type], [Price]) VALUES 
('Polio Vaccine', 'Polio vaccination service', NULL, 'Vaccine', 25.00),
('MMR Vaccine', 'Measles, Mumps, Rubella vaccination service', NULL, 'Vaccine', 30.00),
('Flu Vaccine', 'Influenza vaccination service', NULL, 'Vaccine', 20.00);

-- Insert data into VaccineSuggestions
INSERT INTO [VaccineSuggestions] ([ChildID], [ServiceID], [SuggestedVaccine], [Status]) VALUES 
(1, 1, 'Polio Vaccine', 'Pending'),
(2, 2, 'MMR Vaccine', 'Completed'),
(3, 3, 'Flu Vaccine', 'Pending');

-- Insert data into Appointments
INSERT INTO [Appointments] ([ParentID], [ChildID], [PolicyID], [AppointmentDate], [Status], [Notes], [ServiceType], [Duration], [Room], [ReminderSent], [CancellationReason], [Confirmed], [TotalPrice], [PreferredTimeSlot]) VALUES 
(1, 1, NULL, '2025-01-20 10:00:00', 'Scheduled', 'Bring vaccination records', 'Vaccine', 30, 'Room A', 0, NULL, 1, 25.00, 'Morning'),
(1, 2, NULL, '2025-01-25 14:00:00', 'Completed', NULL, 'Vaccine', 45, 'Room B', 1, NULL, 1, 30.00, 'Afternoon');

-- Insert data into ServiceAvailability
INSERT INTO [ServiceAvailability] ([ServiceID], [Date], [TimeSlot], [Capacity], [Booked]) VALUES 
(1, '2025-01-20', 'Morning', 10, 5),
(2, '2025-01-25', 'Afternoon', 15, 10);

-- Insert data into AppointmentsServices
INSERT INTO [AppointmentsServices] ([AppointmentID], [ServiceID], [Quantity], [TotalPrice]) VALUES 
(1, 1, 1, 25.00),
(2, 2, 1, 30.00);

-- Insert data into UsersVaccinationServices
INSERT INTO [UsersVaccinationServices] ([UserID], [ServiceID]) VALUES 
(1, 1),
(1, 2),
(3, 3);

-- Insert data into VaccinationRecords
INSERT INTO [VaccinationRecords] ([ChildID], [VaccinationDate], [ReactionDetails]) VALUES 
(1, '2025-01-20', 'No reaction'),
(2, '2025-01-25', 'Mild fever'),
(3, '2025-01-30', 'No reaction');

-- Insert data into Notifications
INSERT INTO [Notifications] ([AppointmentID], [Message], [ReadStatus]) VALUES 
(1, 'Your appointment is confirmed for 2025-01-20', 'Unread'),
(2, 'Thank you for attending your appointment', 'Read');

-- Insert data into Feedback
INSERT INTO [Feedback] ([AppointmentID], [Rating], [Comments]) VALUES 
(2, 5, 'Excellent service');

-- Insert data into Payments
INSERT INTO [Payments] ([AppointmentID], [Amount], [PaymentStatus]) VALUES 
(1, 25.00, 'Paid'),
(2, 30.00, 'Paid');

-- Insert data into Invoices
INSERT INTO [Invoices] ([UserID], [PaymentID], [TotalAmount]) VALUES 
(1, 1, 25.00),
(1, 2, 30.00);

-- Insert data into CancellationPolicies
INSERT INTO [CancellationPolicies] ([PolicyName], [Description], [CancellationDeadline], [PenaltyFee]) VALUES 
('Standard Policy', 'Cancel within 24 hours for full refund', 24, 5.00);

-- Insert data into VaccinePackages
INSERT INTO [VaccinePackages] ([PackageName], [Description], [Price]) VALUES 
('Child Vaccination Package', 'Includes essential vaccines for children', 100.00);

-- Insert data into VaccinePackageDetails
INSERT INTO [VaccinePackageDetails] ([PackageID], [ServiceID], [DoseOrder]) VALUES 
(1, 1, 1),
(1, 2, 2),
(1, 3, 3);

-- Insert data into PackageProgress
INSERT INTO [PackageProgress] ([ParentID], [PackageID], [ChildID], [DosesCompleted], [DosesRemaining]) VALUES 
(1, 1, 1, 2, 1);
