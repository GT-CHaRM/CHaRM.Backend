# CHaRM Application Version 1.0.0  -- Backend Server


## Release Notes 

### Features

Account Management
- Registration and Login for users of 3 types: visitors, employees, and admin
  
- Ability to delete or modify accounts
  
  
- Ability to change user preferences

- Ability to Log Items that are able to be handed off the the CHaRM facility to be recycled.

- Ability to Edit log items entries.

- Ability for employees to view previous submissions.

- Export to Excel Functionality.


### Currently, No Known Defects

### Previous Defects - FIXED

- UI styling error when upgrading react native versions.

- Crash when trying to change user's password or zipcode


## Installation Guide

This repository is for running the backend server. It should be ran on a machine that is up whenever you believe the application may be accessed, recommendations being either 24/7 or CHaRM's operational hours. The choice to use a 3rd party server provider or a locally maintained server at the CHaRM Facility is up to your discretion.

### Running the Server

-   Make sure to have .NET Core SDK 2.2 installed. https://dotnet.microsoft.com/download/dotnet-core/2.2
-   Clone the repository

    ```sh
    git clone --recurse-submodules -j8 git://github.com/GT-CHaRM/CHaRM.Backend.git
    ```

-   Install all the dependencies

    ```sh
    dotnet restore
    ```

-   Run the server

    ```sh
    dotnet run
    ```
 Currently We have experience no errors with the above series of commands, if issue occurs, ensure the commands above all were    sucessfully executed and .NET Core SDK 2.2 succesfully installed, a restart may be required.
### For Information on Running the UI component (what visitors and most users will be purely interacting with)
https://github.com/GT-CHaRM/CHaRM.UI

## FULL CHANGELOG

### Version 0.1.0

Added Feature: Log Items (as visitor)

Added Feature: Log Items (as guest)

Added Feature: View Submission History (as guest)

### Version 0.2.0

Added Feature: Create Account (as visitor)

Added Feature: Login (as visitor)

Added Feature: Login (as guest)

Fixed Bug: N/A

### Version 0.3.0
Added Feature: View item logs (as employee)

Added Feature: Modify item log (as employee)

Added Feature: Remove item log (as employee)

Fixed Bug:  UI styling error when upgrading react native versions

### Version 0.4.0
Added Feature: Change own preferences (as user)

Added Feature: Change user preferences (as employee)

Added Feature: Register new user (as employee)

Added Feature: Delete user (as admin)

Added Feature: Delete own account (as user)

Added Feature: Create/Modify/Delete employee accounts (as admin)

Fixed Bug:  Crash when trying to change user's password or zipcode

### Version 1.0.0

Added Feature: Add dashboard for displaying submission data in table format (as employee)

Export data to excel option from dashboard (as employee)

Fixed Bug: N/A

Known Bugs: No Known Bugs
