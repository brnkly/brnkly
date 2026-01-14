Brnkly
==============

> OBSOLETE. This repo is very old, very out of date, and archived.

What is Brnkly?
--------------
Brnkly is a set of code originally developed at NBCNews.com.  It includes:
- A simple, opinionated framework and tools for ASP.NET MVC applications.
- A service bus for inter-application communication.
- An Administration web app, for managing other Brnkly-based applications, as well as RavenDB indexes and replication.
- A Demo web app showing how to consume the Brnkly framework.
- PowerShell deployment scripts.

Getting started
--------------

To see Brnkly in action:

1. Clone the repo to your machine and open Brnkly.sln in VS 2012 (Brnkly uses .NET 4.5).
2. Make sure that Visual Studio is configured to allow package restore, as described here: http://docs.nuget.org/docs/workflows/using-nuget-without-committing-packages
3. Build the solution.
4. Start a Raven instance at http://localhost:8081  The StartRaven.ps1 script in the root of the repo will do this for you.  Make sure you have successfully built the solution and that the repo root is the current working directory.
4. Run the solution.

The initial release of Brnkly is now in the "as-released" branch. That release was pretty close to how Brnkly is used at NBC News Digital.  However, it was too opinionated to be easily used for other purposes.  

Work is now underway to make the functionality more easily consumable in a wider variety of environments.  So far, the Raven configuration functionality has been re-created, and changed to target Raven 2.0.

Much more to do!  Stay tuned.


License
--------------
Brnkly is licensed under the Apache 2.0 license.  You can get a copy of the license here:
    http://www.apache.org/licenses/LICENSE-2.0

