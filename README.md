# CHaRM Application Version 1.0  -- Backend Server


## Release Notes 

### Features

Account Management
Registration and Login for users of 3 types: visitors, employees, and admin
  
Ability to delete or modify accounts
  
  
Ability to change user preferences

Ability to Log Items that are able to be handed off the the CHaRM facility to be recycled.

Ability to Edit log items entries.

Ability for employees to view previous submissions.

Export to Excel Functionality.


### Currently, No Known Defects

### Previous Defects - FIXED

UI styling error when upgrading react native versions.

Crash when trying to change user's password or zipcode


## Installation Guide

This repository is for running the backend database server. It should be ran on a machine that is up whenever you believe the application may be accessed, recommendations being either 24/7 or CHaRM's operational hours. The choice to use a 3rd party server provider or a locally maintained server at the CHaRM Facility is up to your discretion.

### Running the Server

-   Make sure to have .NET Core SDK 2.2 installed.
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
